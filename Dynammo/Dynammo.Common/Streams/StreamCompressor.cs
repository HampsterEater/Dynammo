/// -----------------------------------------------------------------
///  Dynamic MMO (Dynammo)
///  Dissertation project at University of Derby
/// -----------------------------------------------------------------
///  Written by Timothy Leonard
/// -----------------------------------------------------------------

using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynammo.Common
{
    /// <summary>
    ///     This class provides the code required to compress and decompress
    ///     buffers of bytes.
    /// </summary>
    public sealed class StreamCompressor
    {
        #region Members

        #endregion
        #region Properties

        #endregion
        #region Methods

        /// <summary>
        ///     Constructs an instance of the stream compressor.
        /// </summary>
        public StreamCompressor()
        {
        }

        /// <summary>
        ///     Decompresses a buffer of data.
        /// </summary>
        /// <param name="array">Array to bytes to decompress.</param>
        public byte[] Decompress(byte[] array)
        {
            byte[] output = null;
            bool   compressed = (array[0] != 0);

            if (compressed == true)
            {
                using (MemoryStream inStream = new MemoryStream(array, 1, array.Length - 1))
                {
                    using (MemoryStream outStream = new MemoryStream())
                    {
                        using (GZipStream compressionStream = new GZipStream(inStream, CompressionMode.Decompress))
                        {
                            compressionStream.CopyTo(outStream);
                        }

                        output = outStream.ToArray();
                    }
                }
            }
            else
            {
                output = new byte[array.Length - 1];
                for (int i = 1; i < array.Length; i++)
                {
                    output[i - 1] = array[i];
                }
            }

            return output;
        }

        /// <summary>
        ///     Compresses a buffer of bytes.
        /// </summary>
        /// <param name="array">Array to bytes to compress.</param>
        public byte[] Compress(byte[] array)
        {
            byte[] output = null;

            using (MemoryStream inStream = new MemoryStream(array))
            {
                using (MemoryStream outStream = new MemoryStream())
                {
                    using (GZipStream compressionStream = new GZipStream(outStream, CompressionLevel.Fastest))
                    {
                        inStream.CopyTo(compressionStream);
                    }

                    output = outStream.ToArray();
                }
            }

            // Only use if the compressed array is smaller.
            byte[] finalArray = new byte[1 + (output.Length < array.Length ? output.Length : array.Length)];
            if (output.Length < array.Length)
            {
                finalArray[0] = 1;
                output.CopyTo(finalArray, 1);
            }
            else
            {
                finalArray[0] = 0;
                array.CopyTo(finalArray, 1);
            }

            return finalArray;
        }

        #endregion
    }
}
