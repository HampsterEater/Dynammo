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
    ///     This class provides the code required to encode and decode buffers
    ///     of data using delta-encoding. 
    ///     
    ///     We use the traditional quake method, namely a copy of the latest
    ///     packet we encoded/decoded, and working out the encoding/decoding to
    ///     sent based on that.
    ///     
    ///     The idea is that if we recieve 2 buffer in a row, in the second buffer
    ///     we only send the differences from the first, and 0'ing out the values that haven't changed.
    ///     This leaves us with a buffer that is comprised mostly of zero's giving better compression entropy.
    ///     This buffer can then be put through a compression routine (in our case our StreamCompressor class)
    ///     that will produce more efficient output than if our buffer contained the full data.
    /// </summary>
    public sealed class StreamDeltaEncoder
    {
        #region Members

        private Dictionary<int, byte[]> m_previouslyDecoded;
        private Dictionary<int, byte[]> m_previouslyEncoded;

        private object m_threadLock = new object();

        #endregion
        #region Properties

        #endregion
        #region Methods

        /// <summary>
        ///     Constructs an instance of the stream delta encoder.
        /// </summary>
        public StreamDeltaEncoder()
        {
            m_previouslyDecoded = new Dictionary<int, byte[]>();
            m_previouslyEncoded = new Dictionary<int, byte[]>();
        }

        /// <summary>
        ///     Makes a copy of this encoder.
        /// </summary>
        /// <returns>Copy of encoder.</returns>
        public StreamDeltaEncoder Clone()
        {
            StreamDeltaEncoder e = new StreamDeltaEncoder();

            foreach (int key in m_previouslyEncoded.Keys)
            {
                e.m_previouslyEncoded[key] = m_previouslyEncoded[key];
            }

            foreach (int key in m_previouslyDecoded.Keys)
            {
                e.m_previouslyDecoded[key] = m_previouslyDecoded[key];
            }

            return e;
        }

        /// <summary>
        ///     Decodes a buffer thats previously been encoded with another delta encoder.
        /// </summary>
        /// <param name="array">Array to bytes to decode.</param>
        /// <param name="hashcode">Hash code that represents type of array to decode. This is used to match arrays up with previously decoded ones.</param>
        public void DecodeInPlace(byte[] array, int hashcode)
        {
            lock (m_threadLock)
            {
                // Initialize with empty array so we will just encode using a 0'd out array - leaving the encoded array untouched.
                byte[] previousArray = new byte[array.Length];
                byte[] output = new byte[array.Length]; // We create another array to initially encode into so we can keep a copy without other code modifying it after we return.

                // See if there is a previously encoded buffer to use.
                if (m_previouslyDecoded.ContainsKey(hashcode))
                {
                    previousArray = m_previouslyDecoded[hashcode];
                    m_previouslyDecoded.Remove(hashcode);
                }

                // XOr encode the result.
                for (int i = 0; i < array.Length; i++)
                {
                    byte srcByte = i < previousArray.Length ? srcByte = previousArray[i] : (byte)0;
                    byte dstByte = array[i];

                    output[i] = (byte)(srcByte ^ dstByte);
                }

                // Store this array as the latest to be decoded.
                m_previouslyDecoded.Add(hashcode, output);
                Array.Copy(output, array, output.Length);
            }
        }

        /// <summary>
        ///     Encodes a buffer of bytes in place.
        /// </summary>
        /// <param name="array">Array to bytes to encode.</param>
        /// <param name="hashcode">Hash code that represents type of array to decode. This is used to match arrays up with previously encoded ones.</param>
        public void EncodeInPlace(byte[] array, int hashcode)
        {
            lock (m_threadLock)
            {
                // Initialize with empty array so we will just encode using a 0'd out array - leaving the encoded array untouched.
                byte[] previousArray = new byte[array.Length];
                byte[] output = new byte[array.Length]; // We create another array to initially encode into so we can keep a copy without other code modifying it after we return.

                // See if there is a previously encoded buffer to use.
                if (m_previouslyEncoded.ContainsKey(hashcode))
                {
                    previousArray = m_previouslyEncoded[hashcode];
                    m_previouslyEncoded.Remove(hashcode);
                }

                // Store this array as the latest to be encoded.
                Array.Copy(array, output, output.Length);
                m_previouslyEncoded.Add(hashcode, output);

                // XOr encode the result.
                for (int i = 0; i < array.Length; i++)
                {
                    byte srcByte = i < previousArray.Length ? srcByte = previousArray[i] : (byte)0;
                    byte dstByte = array[i];

                    array[i] = (byte)(srcByte ^ dstByte);
                }
            }
        }

        #endregion
    }
}
