/// -----------------------------------------------------------------
///  Dynamic MMO (Dynammo)
///  Dissertation project at University of Derby
/// -----------------------------------------------------------------
///  Written by Timothy Leonard
/// -----------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.Serialization;
using Dynammo.Common;

namespace Dynammo.Networking
{

    /// <summary>
    ///     Used to determine what kind of field is packed inside a packet.
    /// </summary>
    public enum PacketPackedFieldType 
    {
        // Primitive types.
        String          = 1,
        Byte            = 2,
        UShort          = 3,
        Short           = 4,
        UInt            = 5,
        Int             = 6,
        ULong           = 7,
        Long            = 8,
        Float           = 9,
        Double          = 10,
        Struct          = 11,
        Null            = 12,
        Bool            = 13,

        // Array types.
        Array           = 128,
    }

    /// <summary>
    ///     Base class for all packet data sent and recieved from hosts.
    ///     All public properties must be serialisable as they will be sent across the network.
    ///     
    ///     Format of a packet when converted to a buffer (to send across the network) is fairly simple, 
    ///     it consists of two parts, the fixed-size "header" and the payload. The header contains
    ///     just the name (actually a hash to save space) of the packet class so it can be recreated, the size
    ///     of the payload and a basic checksum. 
    ///     
    ///     The payload follows the header which is made up of the type's and values of all public fields in the class.
    ///     Public fields can only be made up of primitive types and arrays of primitive types.
    /// </summary>
    public abstract class Packet
    {
        #region Constants

        public const int HEADER_SIZE = 9;

        #endregion
        #region Members

        // Holds a table of hash -> Type translations.
        private static Dictionary<int, Type> m_hashToTypeTable      = new Dictionary<int, Type>();
        private static object                m_hashToTypeTable_lock = new object();

        // Overhead information.
        private int     m_payloadSize;
        private int     m_checksum;
        private int     m_replyToPacketID;
        private int     m_packetID;

        #endregion
        #region Properties

        /// <summary>
        ///     Gets the size of this packets payload.
        /// </summary>
        public int PayloadSize
        {
            get { return m_payloadSize; }
        }

        /// <summary>
        ///     Gets the ID of the packet that this packet is a reply to.
        /// </summary>
        public int ReplyToPacketID
        {
            get { return m_replyToPacketID; }
        }

        /// <summary>
        ///     Gets the semi-unique ID for this packet. Used to link replies to original packets.
        /// </summary>
        public int PacketID
        {
            get { return m_packetID; }
            set { m_packetID = value; }
        }

        #endregion
        #region Private Methods

        /// <summary>
        ///     Serializes the fields of an object into a binary writer.
        /// </summary>
        /// <param name="writer">Writer to pack fields into.</param>
        /// <param name="obj">Object to pack fields of.</param>
        private void PackObjectFields(BinaryWriter writer, object obj)
        {
            // Write in field values.
            FieldInfo[] properties = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // Order fields by names in alphabetical order.
            properties = properties.OrderBy(j => j.Name).ToArray();

            // Write number of fields.
            if (properties.Length >= 255)
            {
                throw new IndexOutOfRangeException("Packet can only have a maximum of 255 public fields.");
            }
            writer.Write((byte)properties.Length);

            // Write in hash code of fields.
            PearsonHash hash = new PearsonHash();
            foreach (FieldInfo field in properties)
            {
                byte[] bytes = StringHelper.StringToByteArray(field.Name);
                hash.AddBuffer(bytes, 0, bytes.Length);
            }
            writer.Write((byte)hash.Calculate());

            // Write in the fields!
            foreach (FieldInfo field in properties)
            {
                Type fieldType    = field.FieldType;
                object fieldValue = field.GetValue(obj);

                // Null.
                if (fieldValue == null)
                {
                    writer.Write((byte)PacketPackedFieldType.Null);                    
                }

                // String.
                else if (fieldType == typeof(string))
                {
                    writer.Write((byte)PacketPackedFieldType.String);
                    writer.Write((string)fieldValue);
                }

                // Integer types.
                else if (fieldType == typeof(byte))
                {
                    writer.Write((byte)PacketPackedFieldType.Byte);
                    writer.Write((byte)fieldValue);
                }
                else if (fieldType == typeof(ushort))
                {
                    writer.Write((byte)PacketPackedFieldType.UShort);
                    writer.Write((ushort)fieldValue);
                }
                else if (fieldType == typeof(short))
                {
                    writer.Write((byte)PacketPackedFieldType.Short);
                    writer.Write((short)fieldValue);
                }
                else if (fieldType == typeof(uint))
                {
                    writer.Write((byte)PacketPackedFieldType.UInt);
                    writer.Write((uint)fieldValue);
                }
                else if (fieldType == typeof(int))
                {
                    writer.Write((byte)PacketPackedFieldType.Int);
                    writer.Write((int)fieldValue);
                }
                else if (fieldType == typeof(bool))
                {
                    writer.Write((byte)PacketPackedFieldType.Bool);
                    writer.Write((bool)fieldValue);
                }
                else if (fieldType == typeof(ulong))
                {
                    writer.Write((byte)PacketPackedFieldType.ULong);
                    writer.Write((ulong)fieldValue);
                }
                else if (fieldType == typeof(long))
                {
                    writer.Write((byte)PacketPackedFieldType.Long);
                    writer.Write((long)fieldValue);
                }

                // Floating point types.
                else if (fieldType == typeof(float))
                {
                    writer.Write((byte)PacketPackedFieldType.Float);
                    writer.Write((float)fieldValue);
                }
                else if (fieldType == typeof(double))
                {
                    writer.Write((byte)PacketPackedFieldType.Double);
                    writer.Write((double)fieldValue);
                }

                // Other types of data.
                else
                {
                    // An array.
                    if (fieldType.IsArray)
                    {
                        Array array      = ((Array)fieldValue);
                        Type elementType = fieldType.GetElementType();

                        // Null
                        if (array == null)
                        {
                            writer.Write((byte)(PacketPackedFieldType.Null | PacketPackedFieldType.Array));
                            writer.Write((byte)0);
                        }

                        // String.
                        else if (elementType == typeof(string))
                        {
                            writer.Write((byte)(PacketPackedFieldType.String | PacketPackedFieldType.Array));
                            writer.Write((byte)fieldType.GetArrayRank());

                            // Write in dimension lengths.
                            for (int rank = 0; rank < fieldType.GetArrayRank(); rank++)
                            {
                                int length = array.GetLength(rank);
                                writer.Write((int)length);
                            }

                            // Write in values.
                            MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                            foreach (MultiDimensionArrayIteratorValue val in iterator)
                            {
                                writer.Write(val.Value != null);
                                if (val.Value != null)
                                {
                                    writer.Write((string)val.Value);
                                }
                            }
                        }

                        // Integer types.
                        else if (elementType == typeof(byte))
                        {
                            writer.Write((byte)(PacketPackedFieldType.Byte | PacketPackedFieldType.Array));
                            writer.Write((byte)fieldType.GetArrayRank());

                            // Write in dimension lengths.
                            for (int rank = 0; rank < fieldType.GetArrayRank(); rank++)
                            {
                                int length = array.GetLength(rank);
                                writer.Write((int)length);
                            }

                            // Write in values.
                            MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                            foreach (MultiDimensionArrayIteratorValue val in iterator)
                            {
                                writer.Write((byte)val.Value);
                            }
                        }
                        else if (elementType == typeof(ushort))
                        {
                            writer.Write((byte)(PacketPackedFieldType.UShort | PacketPackedFieldType.Array));
                            writer.Write((byte)fieldType.GetArrayRank());

                            // Write in dimension lengths.
                            for (int rank = 0; rank < fieldType.GetArrayRank(); rank++)
                            {
                                int length = array.GetLength(rank);
                                writer.Write((int)length);
                            }

                            // Write in values.
                            MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                            foreach (MultiDimensionArrayIteratorValue val in iterator)
                            {
                                writer.Write((ushort)val.Value);
                            }
                        }
                        else if (elementType == typeof(short))
                        {
                            writer.Write((byte)(PacketPackedFieldType.Short | PacketPackedFieldType.Array));
                            writer.Write((byte)fieldType.GetArrayRank());

                            // Write in dimension lengths.
                            for (int rank = 0; rank < fieldType.GetArrayRank(); rank++)
                            {
                                int length = array.GetLength(rank);
                                writer.Write((int)length);
                            }

                            // Write in values.
                            MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                            foreach (MultiDimensionArrayIteratorValue val in iterator)
                            {
                                writer.Write((short)val.Value);
                            }
                        }
                        else if (elementType == typeof(uint))
                        {
                            writer.Write((byte)(PacketPackedFieldType.UInt | PacketPackedFieldType.Array));
                            writer.Write((byte)fieldType.GetArrayRank());

                            // Write in dimension lengths.
                            for (int rank = 0; rank < fieldType.GetArrayRank(); rank++)
                            {
                                int length = array.GetLength(rank);
                                writer.Write((int)length);
                            }

                            // Write in values.
                            MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                            foreach (MultiDimensionArrayIteratorValue val in iterator)
                            {
                                writer.Write((uint)val.Value);
                            }
                        }
                        else if (elementType == typeof(bool))
                        {
                            writer.Write((byte)(PacketPackedFieldType.Bool | PacketPackedFieldType.Array));
                            writer.Write((byte)fieldType.GetArrayRank());

                            // Write in dimension lengths.
                            for (int rank = 0; rank < fieldType.GetArrayRank(); rank++)
                            {
                                int length = array.GetLength(rank);
                                writer.Write((int)length);
                            }

                            // Write in values.
                            MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                            foreach (MultiDimensionArrayIteratorValue val in iterator)
                            {
                                writer.Write((bool)val.Value);
                            }
                        }
                        else if (elementType == typeof(int))
                        {
                            writer.Write((byte)(PacketPackedFieldType.Int | PacketPackedFieldType.Array));
                            writer.Write((byte)fieldType.GetArrayRank());

                            // Write in dimension lengths.
                            for (int rank = 0; rank < fieldType.GetArrayRank(); rank++)
                            {
                                int length = array.GetLength(rank);
                                writer.Write((int)length);
                            }

                            // Write in values.
                            MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                            foreach (MultiDimensionArrayIteratorValue val in iterator)
                            {
                                writer.Write((int)val.Value);
                            }
                        }
                        else if (elementType == typeof(ulong))
                        {
                            writer.Write((byte)(PacketPackedFieldType.ULong | PacketPackedFieldType.Array));
                            writer.Write((byte)fieldType.GetArrayRank());

                            // Write in dimension lengths.
                            for (int rank = 0; rank < fieldType.GetArrayRank(); rank++)
                            {
                                int length = array.GetLength(rank);
                                writer.Write((int)length);
                            }

                            // Write in values.
                            MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                            foreach (MultiDimensionArrayIteratorValue val in iterator)
                            {
                                writer.Write((ulong)val.Value);
                            }
                        }
                        else if (elementType == typeof(long))
                        {
                            writer.Write((byte)(PacketPackedFieldType.Long | PacketPackedFieldType.Array));
                            writer.Write((byte)fieldType.GetArrayRank());

                            // Write in dimension lengths.
                            for (int rank = 0; rank < fieldType.GetArrayRank(); rank++)
                            {
                                int length = array.GetLength(rank);
                                writer.Write((int)length);
                            }

                            // Write in values.
                            MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                            foreach (MultiDimensionArrayIteratorValue val in iterator)
                            {
                                writer.Write((long)val.Value);
                            }
                        }

                        // Floating point types.
                        else if (elementType == typeof(float))
                        {
                            writer.Write((byte)(PacketPackedFieldType.Float | PacketPackedFieldType.Array));
                            writer.Write((byte)fieldType.GetArrayRank());

                            // Write in dimension lengths.
                            for (int rank = 0; rank < fieldType.GetArrayRank(); rank++)
                            {
                                int length = array.GetLength(rank);
                                writer.Write((int)length);
                            }

                            // Write in values.
                            MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                            foreach (MultiDimensionArrayIteratorValue val in iterator)
                            {
                                writer.Write((float)val.Value);
                            }
                        }
                        else if (elementType == typeof(double))
                        {
                            writer.Write((byte)(PacketPackedFieldType.Double | PacketPackedFieldType.Array));
                            writer.Write((byte)fieldType.GetArrayRank());

                            // Write in dimension lengths.
                            for (int rank = 0; rank < fieldType.GetArrayRank(); rank++)
                            {
                                int length = array.GetLength(rank);
                                writer.Write((int)length);
                            }

                            // Write in values.
                            MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                            foreach (MultiDimensionArrayIteratorValue val in iterator)
                            {
                                writer.Write((double)val.Value);
                            }
                        }

                        // Structs maybe?
                        else if (elementType.IsValueType == true || elementType.IsClass == true)
                        {
                            writer.Write((byte)(PacketPackedFieldType.Struct | PacketPackedFieldType.Array));
                            writer.Write((byte)fieldType.GetArrayRank());

                            // Write in dimension lengths.
                            for (int rank = 0; rank < fieldType.GetArrayRank(); rank++)
                            {
                                int length = array.GetLength(rank);
                                writer.Write((int)length);
                            }

                            // Write in values.
                            MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                            foreach (MultiDimensionArrayIteratorValue val in iterator)
                            {
                                writer.Write(val.Value != null);
                                if (val.Value != null)
                                {
                                    PackObjectFields(writer, val.Value);
                                }                    
                            }
                        }

                        // Invalid data type.
                        else
                        {
                            throw new IndexOutOfRangeException("Packet contains an invalid data type for packing '" + elementType.Name + "'.");
                        }
                    }

                    // Structs maybe?
                    else if (fieldType.IsValueType == true || fieldType.IsClass == true)
                    {
                        writer.Write((byte)PacketPackedFieldType.Struct);
                        writer.Write(fieldValue != null);
                        PackObjectFields(writer, fieldValue);
                    }

                    // Invalid data type.
                    else
                    {
                        throw new IndexOutOfRangeException("Packet contains an invalid data type for packing '" + fieldType.Name + "'.");
                    }
                }
            }
        }

        /// <summary>
        ///    Unserializes the fields of an object from a binary reader.
        /// </summary>
        /// <param name="writer">Writer to unpack fields into.</param>
        /// <param name="obj">Object to unpack fields of.</param>
        private void UnPackObjectFields(BinaryReader reader, object obj)
        {
            // Write in field values.
            FieldInfo[] properties = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // Order fields by names in alphabetical order.
            properties = properties.OrderBy(j => j.Name).ToArray();

            // Read number of fields.
            byte fieldCount = reader.ReadByte();
            if (properties.Length != fieldCount)
            {
                throw new IndexOutOfRangeException("Packet contains an invalid number of fields.");
            }

            // Read in fields hash and check if its correct.
            byte realHash = reader.ReadByte();
            PearsonHash hash = new PearsonHash();
            foreach (FieldInfo field in properties)
            {
                byte[] bytes = StringHelper.StringToByteArray(field.Name);
                hash.AddBuffer(bytes, 0, bytes.Length);
            }

            // Check hash is correct.
            if (hash.Calculate() != realHash)
            {
                throw new FormatException("Packet structure appears to be different from packet class.");
            }

            for (int i = 0; i < fieldCount; i++)
            {
                PacketPackedFieldType fieldType = (PacketPackedFieldType)reader.ReadByte();
                FieldInfo field                 = properties[i];
                Type realFieldType              = field.FieldType;

                // Is array or normal?
                if (((byte)fieldType & (byte)PacketPackedFieldType.Array) != 0)
                {
                    // Is field an array.
                    if (realFieldType.IsArray != true)
                    {
                        throw new FormatException("Packet contains invalid field data type.");
                    }

                    // Strip "Array" from field type.
                    fieldType = (PacketPackedFieldType)(((byte)fieldType) & ~((int)PacketPackedFieldType.Array));

                    // Read in array.     
                    Type elementType        = realFieldType.GetElementType();
                    int arrayDimensions     = reader.ReadByte();
                    Array array            = null;
                    int[] arrayLengths      = new int[arrayDimensions];
                    int[] arrayLowerBounds  = new int[arrayDimensions];

                    // Read in array lengths.
                    for (int l = 0; l < arrayDimensions; l++)
                    {
                        arrayLengths[l] = reader.ReadInt32();
                        arrayLowerBounds[l] = 0;
                    }

                    // Create the array.
                    array = Array.CreateInstance(elementType, arrayLengths, arrayLowerBounds);

                    if (fieldType == PacketPackedFieldType.Null)
                    {
                        array = null;
                    }
                    else if (fieldType == PacketPackedFieldType.String)
                    {
                        // Is field correct data type.
                        if (elementType != typeof(string))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }
                        
                        // Write in values.
                        MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                        foreach (MultiDimensionArrayIteratorValue val in iterator)
                        {
                            if (reader.ReadBoolean() == true)
                            {
                                array.SetValue(reader.ReadString(), val.Indicies);
                            }
                            else
                            {
                                array.SetValue(null, val.Indicies);
                            }
                        }
                    }
                    else if (fieldType == PacketPackedFieldType.Byte)
                    {
                        // Is field correct data type.
                        if (elementType != typeof(byte))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        // Write in values.
                        MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                        foreach (MultiDimensionArrayIteratorValue val in iterator)
                        {
                            array.SetValue(reader.ReadByte(), val.Indicies);
                        }
                    }
                    else if (fieldType == PacketPackedFieldType.UShort)
                    {
                        // Is field correct data type.
                        if (elementType != typeof(ushort))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        // Write in values.
                        MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                        foreach (MultiDimensionArrayIteratorValue val in iterator)
                        {
                            array.SetValue(reader.ReadUInt16(), val.Indicies);
                        }
                    }
                    else if (fieldType == PacketPackedFieldType.Short)
                    {
                        // Is field correct data type.
                        if (elementType != typeof(short))
                        {
                            throw new IndexOutOfRangeException("Packet contains invalid field data type.");
                        }

                        // Write in values.
                        MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                        foreach (MultiDimensionArrayIteratorValue val in iterator)
                        {
                            array.SetValue(reader.ReadInt16(), val.Indicies);
                        }
                    }
                    else if (fieldType == PacketPackedFieldType.UInt)
                    {
                        // Is field correct data type.
                        if (elementType != typeof(uint))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        // Write in values.
                        MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                        foreach (MultiDimensionArrayIteratorValue val in iterator)
                        {
                            array.SetValue(reader.ReadUInt32(), val.Indicies);
                        }
                    }
                    else if (fieldType == PacketPackedFieldType.Bool)
                    {
                        // Is field correct data type.
                        if (elementType != typeof(bool))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        // Write in values.
                        MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                        foreach (MultiDimensionArrayIteratorValue val in iterator)
                        {
                            array.SetValue(reader.ReadBoolean(), val.Indicies);
                        }
                    }
                    else if (fieldType == PacketPackedFieldType.Int)
                    {
                        // Is field correct data type.
                        if (elementType != typeof(int))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        // Write in values.
                        MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                        foreach (MultiDimensionArrayIteratorValue val in iterator)
                        {
                            array.SetValue(reader.ReadInt32(), val.Indicies);
                        }
                    }
                    else if (fieldType == PacketPackedFieldType.ULong)
                    {
                        // Is field correct data type.
                        if (elementType != typeof(ulong))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        // Write in values.
                        MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                        foreach (MultiDimensionArrayIteratorValue val in iterator)
                        {
                            array.SetValue(reader.ReadUInt64(), val.Indicies);
                        }
                    }
                    else if (fieldType == PacketPackedFieldType.Long)
                    {
                        // Is field correct data type.
                        if (elementType != typeof(long))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        // Write in values.
                        MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                        foreach (MultiDimensionArrayIteratorValue val in iterator)
                        {
                            array.SetValue(reader.ReadInt64(), val.Indicies);
                        }
                    }
                    else if (fieldType == PacketPackedFieldType.Float)
                    {
                        // Is field correct data type.
                        if (elementType != typeof(float))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        // Write in values.
                        MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                        foreach (MultiDimensionArrayIteratorValue val in iterator)
                        {
                            array.SetValue(reader.ReadSingle(), val.Indicies);
                        }
                    }
                    else if (fieldType == PacketPackedFieldType.Double)
                    {
                        // Is field correct data type.
                        if (elementType != typeof(double))
                        {
                            throw new IndexOutOfRangeException("Packet contains invalid field data type.");
                        }

                        // Write in values.
                        MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                        foreach (MultiDimensionArrayIteratorValue val in iterator)
                        {
                            array.SetValue(reader.ReadDouble(), val.Indicies);
                        }
                    }
                    else if (fieldType == PacketPackedFieldType.Struct)
                    {
                        // Is field correct data type.
                        if (elementType.IsValueType != true && elementType.IsClass != true)
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        // Write in values.
                        MultiDimensionArrayIterator iterator = new MultiDimensionArrayIterator(array);
                        foreach (MultiDimensionArrayIteratorValue val in iterator)
                        {
                            if (reader.ReadBoolean() == true)
                            {
                                object structResult = Activator.CreateInstance(elementType);//FormatterServices.GetUninitializedObject(realFieldType);
                                UnPackObjectFields(reader, structResult);
                                array.SetValue(structResult, val.Indicies);
                            }
                            else
                            {
                                array.SetValue(null, val.Indicies);
                            }
                        }
                    }
                    else
                    {
                        throw new FormatException("Packet contains invalid field data type.");
                    }

                    // Set the field value.
                    field.SetValue(obj, array);
                }
                else
                {
                    if (fieldType == PacketPackedFieldType.Null)
                    {
                        // Is field correct data type.
                        if (realFieldType.IsByRef == false && realFieldType.IsValueType == false && realFieldType.IsClass == false)
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        field.SetValue(obj, null);
                    }
                    else if (fieldType == PacketPackedFieldType.String)
                    {
                        // Is field correct data type.
                        if (realFieldType != typeof(string))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        field.SetValue(obj, reader.ReadString());
                    }
                    else if (fieldType == PacketPackedFieldType.Byte)
                    {
                        // Is field correct data type.
                        if (realFieldType != typeof(byte))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        field.SetValue(obj, reader.ReadByte());
                    }
                    else if (fieldType == PacketPackedFieldType.UShort)
                    {
                        // Is field correct data type.
                        if (realFieldType != typeof(ushort))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        field.SetValue(obj, reader.ReadUInt16());
                    }
                    else if (fieldType == PacketPackedFieldType.Short)
                    {
                        // Is field correct data type.
                        if (realFieldType != typeof(short))
                        {
                            throw new IndexOutOfRangeException("Packet contains invalid field data type.");
                        }

                        field.SetValue(obj, reader.ReadInt16());
                    }
                    else if (fieldType == PacketPackedFieldType.UInt)
                    {
                        // Is field correct data type.
                        if (realFieldType != typeof(uint))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        field.SetValue(obj, reader.ReadUInt32());
                    }
                    else if (fieldType == PacketPackedFieldType.Bool)
                    {
                        // Is field correct data type.
                        if (realFieldType != typeof(bool))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        field.SetValue(obj, reader.ReadBoolean());
                    }
                    else if (fieldType == PacketPackedFieldType.Int)
                    {
                        // Is field correct data type.
                        if (realFieldType != typeof(int))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        field.SetValue(obj, reader.ReadInt32());
                    }
                    else if (fieldType == PacketPackedFieldType.ULong)
                    {
                        // Is field correct data type.
                        if (realFieldType != typeof(ulong))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        field.SetValue(obj, reader.ReadUInt64());
                    }
                    else if (fieldType == PacketPackedFieldType.Long)
                    {
                        // Is field correct data type.
                        if (realFieldType != typeof(long))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        field.SetValue(obj, reader.ReadInt64());
                    }
                    else if (fieldType == PacketPackedFieldType.Float)
                    {
                        // Is field correct data type.
                        if (realFieldType != typeof(float))
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        field.SetValue(obj, reader.ReadSingle());
                    }
                    else if (fieldType == PacketPackedFieldType.Double)
                    {
                        // Is field correct data type.
                        if (realFieldType != typeof(double))
                        {
                            throw new IndexOutOfRangeException("Packet contains invalid field data type.");
                        }

                        field.SetValue(obj, reader.ReadDouble());
                    }
                    else if (fieldType == PacketPackedFieldType.Struct)
                    {
                        // Is field correct data type.
                        if (realFieldType.IsValueType != true && realFieldType.IsClass != true)
                        {
                            throw new FormatException("Packet contains invalid field data type.");
                        }

                        object structResult = null;
                        if (reader.ReadBoolean() == true)
                        {
                            structResult = Activator.CreateInstance(realFieldType); //FormatterServices.GetUninitializedObject(realFieldType); //
                            UnPackObjectFields(reader, structResult);
                        }
                        field.SetValue(obj, structResult);
                    }
                    else
                    {
                        throw new FormatException("Packet contains invalid field data type.");
                    }
                }
            }
        }

        /// <summary>
        ///     Builds up the payload that describes this packets data.
        /// </summary>
        /// <returns>Byte array containing the packets payload.</returns>
        private byte[] CreatePayload()
        {
            FieldInfo[] properties = this.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            PackObjectFields(writer, this);

            writer.Close();
            stream.Close();

            byte[] byteArray = stream.ToArray();

            return byteArray;
        }

        /// <summary>
        ///     Parses a payload that describes this packets data.
        /// </summary>
        /// <returns>True if successful, or false if payload is invalid (and thus packet is too).</returns>
        /// <param name="data">Byte array containing the packets payload.</param>
        private bool ParsePayload(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(stream);

            try
            {
                UnPackObjectFields(reader, this);
            }
            catch (FormatException)
            {
                Logger.Warning("Encountered format exception when reading packet payload - possible attempt to send invalid out-of-bounds data?", LoggerVerboseLevel.Normal);
                return false;
            }
            catch (IOException)
            {
                Logger.Warning("Encountered IO exception when reading packet payload - possible attempt to send invalid out-of-bounds data?", LoggerVerboseLevel.Normal);
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Mangles the given buffer of data.
        /// </summary>
        /// <param name="data">data to mangle.</param>       
        /// <param name="cryptor">The encryption class to encrypt the buffer with.</param>
        /// <param name="compressor">Compressor used to compress the packet.</param>
        /// <param name="encoder">Encoder used for delta-encoding of packets.</param>
        private byte[] MangleData(byte[] data,  StreamDeltaEncoder encoder, StreamCompressor compressor, StreamEncryptor cryptor)
        {
            // Delta encoder data.
            if (encoder != null)
            {
                encoder.EncodeInPlace(data, GetType().FullName.GetHashCode());
            }

            // Compress payload.
            if (compressor != null)
            {
                data = compressor.Compress(data);
            }

            // Encrypt output.
            if (cryptor != null)
            {
                cryptor.EncryptInPlace(data);
            }

            return data;
        }

        #endregion
        #region Methods

        /*
        static double _noneTime = 0.0f;
        static int _noneSize = 0;

        static int _encodeSize = 0;
        static double _encodeTime = 0.0f;
        static int  _encodeCompressSize = 0;
        static double _encodeCompressTime = 0.0f;
        static int  _encodeCompressEncryptSize = 0;
        static double _encodeCompressEncryptTime = 0.0f;
          static int   _compressSize = 0;
        static double _compressTime = 0.0f;
           static int  _compressEncryptSize = 0;
        static double _compressEncryptTime = 0.0f;
          static int   _encryptSize = 0;
        static double _encryptTime = 0.0f;
        static int _encryptEncodeSize = 0;
        static double _encryptEncodeTime = 0.0f;

        static int _packetCounter = 0;
        */

        /// <summary>
        ///     Serializes this packet to a byte buffer than can be recreated by calling FromBuffer.
        /// </summary>
        /// <param name="inReplyTo">If not null this packet is marked as a reply to another packet.</param>
        /// <param name="cryptor">The encryption class to encrypt the buffer with.</param>
        /// <param name="compressor">Compressor used to compress the packet.</param>
        /// <param name="encoder">Encoder used for delta-encoding of packets.</param>
        /// <returns>Byte buffer conversion of packet.</returns>
        public byte[] ToBuffer(Packet inReplyTo, StreamEncryptor cryptor, StreamCompressor compressor, StreamDeltaEncoder encoder)
        {
            byte[] data = CreatePayload();

            // Data cannot be larger than a ushort.
            if (data.Length >= ushort.MaxValue)
            {
                throw new InvalidDataException("Packets cannot be larger than a ushort's max value.");
            }
            
            // Work out checksum of unmodified payload.
            PearsonHash hash = new PearsonHash();
            hash.AddBuffer(data, 0, data.Length);
            byte dataChecksum = hash.Calculate();

            // Mangle our data based ready for transport.
            data = MangleData(data, encoder, compressor, cryptor);


            // DEBUG TIMING
            /*
            HighPerformanceTimer timer = new HighPerformanceTimer();

            StreamEncryptor tmp_cryptor = cryptor == null ? null : cryptor.Clone();
            StreamCompressor tmp_compressor = new StreamCompressor();
            StreamDeltaEncoder tmp_encoder = encoder == null ? null : encoder.Clone();

            byte[] tmp = CreatePayload();
            timer.Start();
            _noneSize += MangleData(tmp, null, null, null).Length;
            _noneTime += timer.Stop();

            tmp = CreatePayload();
            timer.Start();
            _encodeSize += MangleData(tmp, tmp_encoder, null, null).Length;
            _encodeTime += timer.Stop();

            tmp = CreatePayload();
            timer.Start();
            _encodeCompressSize += MangleData(tmp, tmp_encoder, tmp_compressor, null).Length;
            _encodeCompressTime += timer.Stop();

            tmp = CreatePayload();
            timer.Start();
            _encodeCompressEncryptSize += MangleData(tmp, tmp_encoder, tmp_compressor, tmp_cryptor).Length;
            _encodeCompressEncryptTime += timer.Stop();

            tmp = CreatePayload();
            timer.Start();
            _compressSize += MangleData(tmp, null, tmp_compressor, null).Length;
            _compressTime += timer.Stop();

            tmp = CreatePayload();
            timer.Start();
            _compressEncryptSize += MangleData(tmp, null, tmp_compressor, tmp_cryptor).Length;
            _compressEncryptTime += timer.Stop();

            tmp = CreatePayload();
            timer.Start();
            _encryptSize += MangleData(tmp, null, null, tmp_cryptor).Length;
            _encryptTime += timer.Stop();

            tmp = CreatePayload();
            timer.Start();
            _encryptEncodeSize += MangleData(tmp, tmp_encoder, null, tmp_cryptor).Length;
            _encryptEncodeTime += timer.Stop();

            _packetCounter++;
            if (_packetCounter >= 100000)
            {
                System.Console.WriteLine("DONE!");
            }
             * */
            // DEBUG TIMING

            // Write in data checksum.
            byte[] buffer = new byte[HEADER_SIZE + data.Length];
            buffer[0] = dataChecksum; 

            // Write in payload size.
            byte[] bytes = BitConverter.GetBytes((ushort)data.Length);
            buffer[1] = bytes[0];
            buffer[2] = bytes[1];

            // Write in class hash.
            bytes = BitConverter.GetBytes(this.GetType().FullName.GetHashCode());
            buffer[3] = bytes[0];
            buffer[4] = bytes[1];
            buffer[5] = bytes[2];
            buffer[6] = bytes[3];

            // Write in a reply-to-packet-id.
            bytes = BitConverter.GetBytes(inReplyTo == null ? 0 : inReplyTo.PacketID);
            buffer[7] = bytes[0];
            buffer[8] = bytes[1];

            // Write in payload.
            for (int i = 0; i < data.Length; i++)
            {
                buffer[HEADER_SIZE + i] = data[i];
            }

            return buffer;
        }

        /// <summary>
        ///     Finishes the job that FromHeader starts, decrypts and parses the packet payload.
        ///     
        ///     Be aware that for the sake of speed, decryption of the data will be done
        ///     in-place - the value of buffer will be in its decrypted form when this
        ///     function returns!
        /// </summary>
        /// <returns>True if successful, or false if payload is invalid (and thus packet is too).</returns>
        /// <param name="cryptor">The encryption class to decrypt the buffer with.</param>
        /// <param name="compressor">Compressor used to decompress the packet.</param>
        /// <param name="encoder">Encoder used for delta-deencoding of packets.</param>
        /// <param name="buffer">Buffer to construct packet from.</param>
        public bool RecievePayload(byte[] buffer, StreamEncryptor cryptor, StreamCompressor compressor, StreamDeltaEncoder encoder)
        {
            try
            {
                // Decrypt the payload.
                cryptor.DecryptInPlace(buffer);

                // Decompress payload.
                buffer = compressor.Decompress(buffer);

                // Delta un-encode.
                encoder.DecodeInPlace(buffer, GetType().FullName.GetHashCode());
            }
            catch (Exception)
            {
                Logger.Error("Recieved packet with invalid data. Dropping packet.", LoggerVerboseLevel.Normal);
                return false;
            }

            // Check that the checksum is correct.
            PearsonHash hash = new PearsonHash();
            hash.AddBuffer(buffer, 0, buffer.Length);
            int bufferChecksum = hash.Calculate();

            if (bufferChecksum != m_checksum)
            {
                Logger.Error("Recieved packet with invalid checksum, got {0}, expected {1}. Dropping packet.", LoggerVerboseLevel.Normal, bufferChecksum, m_checksum);
                return false;
            }

            // Parse the payload!
            return ParsePayload(buffer);
        }

        /// <summary>
        ///     Constructs a packet from the given packet header.
        ///     RecievePayload should be called on the packet instance after data is recieved.
        ///     
        ///     Be aware that for the sake of speed, decryption of the header will be done
        ///     in-place - the value of buffer will be in its decrypted form when this
        ///     function returns!
        /// </summary>
        /// <returns>Skeleton packet instance holding header information. Or null if header is invalid.</returns>
        /// <param name="cryptor">The encryption class to decrypt the buffer with.</param>
        /// <param name="compressor">Compressor used to decompress the packet.</param>
        /// <param name="encoder">Encoder used for delta-deencoding of packets.</param>
        /// <param name="buffer">Buffer to construct packet from.</param>
        public static Packet FromHeader(byte[] buffer, StreamEncryptor cryptor, StreamCompressor compressor, StreamDeltaEncoder encoder)
        {
            // Make sure we have been passed a header, and not some other kind of buffer.
            if (buffer.Length != HEADER_SIZE)
            {
                Logger.Error("Recieved packet with invalid header size, got {0} bytes, expected {1}. Dropping packet.", LoggerVerboseLevel.Normal, buffer.Length, HEADER_SIZE);
                return null;
            }

            // Read header.
            byte checksum       = buffer[0];
            int payloadSize     = (int)BitConverter.ToUInt16(buffer, 1);
            int classHash       = (int)BitConverter.ToInt32(buffer, 3);
            int replyToPacketID = (int)BitConverter.ToUInt16(buffer, 7);

            // Verify class hash is correct.
            Type classType = null;
            lock (m_hashToTypeTable_lock)
            {
                if (m_hashToTypeTable.ContainsKey(classHash) == true)
                {
                    classType = m_hashToTypeTable[classHash];
                }
                else
                {
                    var subclasses = typeof(Packet).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Packet)));
                    foreach (Type type in subclasses)
                    {
                        int code = type.FullName.GetHashCode();
                        if (code == classHash)
                        {
                            m_hashToTypeTable.Add(code, type);
                            classType = type;
                            break;
                        }
                    }
                }
            }

            // No class available for hash?
            if (classType == null)
            {
                Logger.Error("Recieved packet with invalid class hash '{0}'. Dropping packet.", LoggerVerboseLevel.Normal, classHash);
                return null;
            }

            // Load the packet.
            Packet packet = null;
            try
            {
                packet = (Packet)Activator.CreateInstance(classType);
                packet.m_payloadSize        = payloadSize;
                packet.m_checksum           = checksum;
                packet.m_replyToPacketID    = replyToPacketID;
            }
            catch (Exception)
            {
                Logger.Error("Failed to create packet of type '{0}'. Dropping packet.", LoggerVerboseLevel.Normal, classType.Name);
                return null;
            }

            return packet;
        }

        #endregion
    }

}

