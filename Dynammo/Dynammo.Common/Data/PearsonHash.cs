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
    ///     Provides the code required to calculate a pearson (1 byte) hashs of a 
    ///     given amount of bytes.
    /// </summary>
    public sealed class PearsonHash
    {
        #region Members

        private byte[] LOOKUP_TABLE = {
            107, 54,  177, 38,  146, 43,  194, 93,  116, 150, 
            252, 163, 153, 63,  149, 148, 162, 207, 152, 120, 
            203, 0,   199, 13,  59,  197, 10,  178, 92,  113, 
            201, 200, 187, 154, 218, 48,  147, 77,  215, 193, 
            206, 214, 57,  204, 78,  70,  166, 126, 234, 61,  
            55,  212, 94,  69,  255, 21,  213, 173, 91,  239, 
            60,  100, 3,   98,  80,  110, 16,  51,  137, 90,  
            144, 28,  81,  71,  143, 165, 132, 2,   142, 171, 
            64,  229, 76,  27,  40,  254, 11,  225, 118, 217, 
            245, 169, 5,   7,   198, 127, 246, 29,  105, 36,  
            9,   37,  52,  56,  250, 14,  222, 209, 241, 155, 
            227, 117, 25,  188, 20,  138, 129, 6,   62,  17,  
            102, 221, 242, 136, 41,  88,  119, 167, 72,  141, 
            33,  74,  24,  249, 53,  237, 109, 4,   205, 182, 
            23,  176, 19,  124, 83,  230, 196, 86,  32,  1,   
            130, 34,  179, 220, 251, 131, 18,  219, 164, 101, 
            35,  22,  135, 157, 79,  67,  191, 170, 208, 97,  
            168, 226, 232, 134, 42,  139, 192, 39,  189, 158, 
            111, 95,  195, 31,  89,  156, 125, 183, 50,  185, 
            26,  96,  175, 114, 45,  161, 65,  46,  99,  73,  
            66,  58,  140, 68,  238, 180, 172, 15,  235, 108, 
            210, 123, 160, 84,  47,  159, 228, 87,  85,  75,  
            253, 112, 151, 103, 115, 202, 181, 174, 128, 133, 
            247, 8,   248, 240, 243, 106, 44,  104, 233, 145, 
            122, 236, 186, 184, 224, 12,  121, 190, 82,  244, 
            231, 49,  30,  223, 216, 211  
        };

        private byte m_hash  = 0;
        private byte m_index = 0;

        #endregion
        #region Methods

        /// <summary>
        ///     Resets the class so you can calculate another hash.
        /// </summary>
        public void Reset()
        {
            m_hash  = 0;
            m_index = 0;
        }

        /// <summary>
        ///     Adds another buffer of bytes that are part of the hash calculated.
        /// </summary>
        /// <param name="array">Array of bytes to generate hash from.</param>
        /// <param name="offset">Offset into array to hash.</param>
        /// <param name="length">Number of bytes in buffer to hash.</param>
        public void AddBuffer(byte[] array, int offset, int length)
        {
            for (int i = offset; i < length; i++)
            {
                m_index = (byte)(m_hash ^ array[i]);
                m_hash  = LOOKUP_TABLE[m_index];
            }
        }

        /// <summary>
        ///     Calculates the hash of all buffers added. 
        /// </summary>
        /// <returns>Returns the final hash of the buffers added.</returns>
        public byte Calculate()
        {
            return m_hash;
        }

        #endregion
    }

}
