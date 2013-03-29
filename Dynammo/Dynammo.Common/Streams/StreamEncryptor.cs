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
    ///     This class provides the code required to encrypt and decrypt bytes in-place.
    ///     Each time you encrypt/decrypt a byte the key is shifted so every time you encrypt/decrypt
    ///     the key used is different (hence stream encrypted).
    /// </summary>
    public sealed class StreamEncryptor
    {
        #region Members

        private string m_key = "";
        private int m_encryptKeyOffset = 0;
        private int m_decryptKeyOffset = 0;

        private object m_threadLock = new object();

        #endregion
        #region Properties

        private string Key
        {
            get { return m_key; }
            set
            {
                lock (m_threadLock)
                {
                    m_encryptKeyOffset = 0;
                    m_decryptKeyOffset = 0;
                    m_key = value;
                }
            }
        }

        #endregion
        #region Methods

        /// <summary>
        ///     Constructs an instance of the stream encryptor.
        /// </summary>
        /// <param name="key">Key to use with encrypting/decrypting.</param>
        public StreamEncryptor(string key)
        {
            m_key = key;
            m_encryptKeyOffset = 0;
            m_decryptKeyOffset = 0;
        }

        /// <summary>
        ///     Makes a copy of this encoder.
        /// </summary>
        /// <returns>Copy of encoder.</returns>
        public StreamEncryptor Clone()
        {
            StreamEncryptor e = new StreamEncryptor(m_key);
            e.m_encryptKeyOffset = m_encryptKeyOffset;
            e.m_decryptKeyOffset = m_decryptKeyOffset;
            return e;
        }

        /// <summary>
        ///     Decrypts an array of bytes in place.
        /// </summary>
        /// <param name="array">Array to bytes to decrypt.</param>
        public void DecryptInPlace(byte[] array)
        {
            lock (m_threadLock)
            {
                if (m_key == "")
                {
                    return;
                }

                for (int i = 0; i < array.Length; i++)
                {
                    byte raw = array[i];
                    byte cipher = (byte)m_key[m_decryptKeyOffset % m_key.Length];

                    array[i] = (byte)(raw ^ cipher);

                    m_decryptKeyOffset++;
                }
            }
        }

        /// <summary>
        ///     Encrypts an array of bytes in place.
        /// </summary>
        /// <param name="array">Array to bytes to encrypt.</param>
        public void EncryptInPlace(byte[] array)
        {
            lock (m_threadLock)
            {
                if (m_key == "")
                {
                    return;
                }

                for (int i = 0; i < array.Length; i++)
                {
                    byte raw = array[i];
                    byte cipher = (byte)m_key[m_encryptKeyOffset % m_key.Length];

                    array[i] = (byte)(raw ^ cipher);

                    m_encryptKeyOffset++;
                }
            }
        }

        #endregion
    }

}
