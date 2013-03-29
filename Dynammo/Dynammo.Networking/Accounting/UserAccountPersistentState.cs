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
using System.Reflection;
using System.Threading.Tasks;
using Dynammo.Common;
using Dynammo.Networking;

namespace Dynammo.Networking
{
    /// <summary>
    ///     Used to define the amount the values in a UserAccountPersistentState can vary
    ///     and still be considered the same.
    /// </summary>
    public class SimilarityThresholdAttribute : Attribute
    {
        public float Variance;

        public bool CompareValues(object o1, object o2)
        {
            float f1 = (float)o1;
            float f2 = (float)o2;
            return (Math.Abs(f2 - f1) <= Variance);
        }

        public SimilarityThresholdAttribute(float variance)
        {
            Variance = variance;
        }
    }

    /// <summary>
    ///     Serializes, deserializes and modifies the persistent state of
    ///     a users account. 
    ///     
    ///     Things like the players last position is stored in this.
    /// </summary>
    public class UserAccountPersistentState
    {
        #region Persistent Properties

        // Position Information.
        [SimilarityThreshold(32.0f)] public float X;
        [SimilarityThreshold(32.0f)] public float Y;

        #endregion
        #region Public Methods

        /// <summary>
        ///     Checks if this persistent state is similar enough to another state that it 
        ///     can be considered the same.
        /// </summary>
        /// <param name="state">Other state to check.</param>
        /// <returns>True if states are similar, otherwise false.</returns>
        public bool IsSimilarTo(UserAccountPersistentState state)
        {
            Type type = this.GetType();

            foreach (FieldInfo info in type.GetFields())
            {
                object val1                         = info.GetValue(this);
                object val2                         = info.GetValue(state);

                SimilarityThresholdAttribute attr   = info.GetCustomAttribute(typeof(SimilarityThresholdAttribute)) as SimilarityThresholdAttribute;
                
                if (attr != null)
                {
                    if (attr.CompareValues(val1, val2) == false)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///     Serializes this account state into a byte buffer.
        /// </summary>
        public byte[] Serialize()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            Type type = this.GetType();

            foreach (FieldInfo info in type.GetFields())
            {
                // Write field name.
                writer.Write(info.Name);

                // Get value.
                object value = info.GetValue(this);

                if (info.FieldType == typeof(int))
                {
                    writer.Write((byte)0);
                    writer.Write((int)value);
                }
                else if (info.FieldType == typeof(string))
                {
                    writer.Write((byte)1);
                    writer.Write((string)value);
                }
                else if (info.FieldType == typeof(float))
                {
                    writer.Write((byte)2);
                    writer.Write((float)value);
                }
                else
                {
                    throw new InvalidDataException("Invalid data found when serializing.");
                }
            }

            stream.Close();
            return stream.ToArray();
        }

        /// <summary>
        ///     Deserializes the state recieved from the database.
        /// </summary>
        /// <param name="data">Data to deserialize.</param>
        public void Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(stream);

            Type type = this.GetType();

            while (stream.Position < stream.Length)
            {
                string name = reader.ReadString();
                int value_type = reader.ReadByte();
                object value = null;

                switch (value_type)
                {
                    case 0:
                        {
                            value = reader.ReadInt32();
                            break;
                        }
                    case 1:
                        {
                            value = reader.ReadString();
                            break;
                        }
                    case 2:
                        {
                            value = reader.ReadSingle();
                            break;
                        }
                    default:
                        {
                            throw new InvalidDataException("Invalid data found when deserializing.");
                        }
                }

                foreach (FieldInfo info in type.GetFields())
                {
                    if (info.Name.ToLower() == name.ToLower())
                    {
                        info.SetValue(this, value);
                        break;
                    }
                }
            }

            reader.Close();
            stream.Close();
        }

        /// <summary>
        ///     Constructs a user account's persistent state with the given blob data from the database.
        /// </summary>
        /// <param name="data">Blob data to load this peristent state from.</param>
        public UserAccountPersistentState(byte[] data)
        {
            if (data != null)
            {
                Deserialize(data);
            }
        }

        /// <summary>
        ///     Constructs a user account's persistent state.
        /// </summary>
        public UserAccountPersistentState()
        {
        }

        #endregion
    }

}
