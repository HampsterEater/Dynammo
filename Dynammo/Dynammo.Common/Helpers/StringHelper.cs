/// -----------------------------------------------------------------
///  Dynamic MMO (Dynammo)
///  Dissertation project at University of Derby
/// -----------------------------------------------------------------
///  Written by Timothy Leonard
/// -----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynammo.Common
{

    /// <summary>
    ///     Contains some general use helper functions for working with strings.
    /// </summary>
    public static class StringHelper
    {

        /// <summary>
        ///     Turns a string into a byte array.
        /// </summary>
        /// <param name="str">String to convert.</param>
        /// <returns>Byte array representation of string.</returns>
        public static byte[] StringToByteArray(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        ///     Turns a byte array into a string.
        /// </summary>
        /// <param name="bytes">Byte array to convert.</param>
        /// <returns>String representation of byte array.</returns>
        public static string ByteArrayToString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        /// <summary>
        ///     Turns a byte array into a hex string.
        /// </summary>
        /// <param name="bytes">Byte array to convert.</param>
        /// <returns>Hex string representation of byte array.</returns>
        public static string ByteArrayToHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes, 0);
        }

        /// <summary>
        ///     Casts a string to the given type and returns it.
        /// </summary>
        /// <param name="value">Value to cast.</param>
        /// <param name="type">Type to cast to.</param>
        /// <returns>Casted value, or null if it could not be casted.</returns>
        public static object CastStringToType(string value, Type type)
        {
            if (type == typeof(string))
            {
                return value;
            }
            else if (type == typeof(int))
            {
                return int.Parse(value);
            }
            else if (type == typeof(float))
            {
                return float.Parse(value);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        ///     Escapes a string that will be entered into a database.
        /// </summary>
        /// <param name="query">Query to escape.</param>
        /// <returns>Escaped query.</returns>
        public static string Escape(string query)
        {
            query = query.Replace("'", "\\'");
            query = query.Replace("\"", "\\\"");
            return query;
        }

    }
}
