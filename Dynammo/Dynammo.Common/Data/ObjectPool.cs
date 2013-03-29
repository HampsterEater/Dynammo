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
using System.Diagnostics;

namespace Dynammo.Common
{

    /// <summary>
    ///     Stores a pool of objects rather than allocating new objects each use. Its main use
    ///     is a debugging agent as it gathers stack traces for each allocation.
    /// </summary>  
    public class ObjectPool<T>
    {
        #region Private Methods

        private int m_count = 0;

        private object m_threadLock = new object();

        private T[] m_pool = null;
        private bool[] m_poolInUse = null;
        private StackTrace[] m_poolTraces = null;

        #endregion
        #region Public Methods

        /// <summary>
        ///     Constructs a new object pool.
        /// </summary>
        /// <param name="count">Maximum number of objects in pool.</param>
        public ObjectPool(int count)
        {
            m_pool = new T[count];
            m_poolTraces = new StackTrace[count];
            m_poolInUse = new bool[count];
        }

        /// <summary>
        ///     Allocates a new object.
        /// </summary>
        /// <returns>Newly allocated object.</returns>
        public T NewObject()
        {
            lock (m_threadLock)
            {
                m_count++;
                System.Console.WriteLine("Allocing, count=" + m_count);

                for (int i = 0; i < m_pool.Length; i++)
                {
                    if (m_poolInUse[i] == false)
                    {
                        //if (m_pool[i] == null)
                       // {
                            m_pool[i] = (T)Activator.CreateInstance(typeof(T));
                        //}
                        m_poolTraces[i] = new StackTrace();
                        m_poolInUse[i] = true;
                        return m_pool[i];
                    }
                }
                throw new InvalidOperationException("Object pool is exhausted.");
            }
        }

        /// <summary>
        ///     Frees an allocated object.
        /// </summary>
        public void FreeObject(T val)
        {
            lock (m_threadLock)
            {
                m_count--;
                System.Console.WriteLine("Freeing, count=" + m_count);

                for (int i = 0; i < m_pool.Length; i++)
                {
                    if (m_pool[i] != null && m_pool[i].Equals(val))
                    {
                        m_poolInUse[i] = false;
                        m_poolTraces[i] = null;
                        return;
                    }
                }
                throw new InvalidOperationException("Object is not in pool.");
            }
        }

        #endregion
    }

}
