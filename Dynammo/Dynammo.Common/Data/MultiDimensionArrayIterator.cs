/// -----------------------------------------------------------------
///  Dynamic MMO (Dynammo)
///  Dissertation project at University of Derby
/// -----------------------------------------------------------------
///  Written by Timothy Leonard
/// -----------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynammo.Common
{

    /// <summary>
    ///     Stores value returned by MultiDimensionArrayIterator.
    /// </summary>
    public struct MultiDimensionArrayIteratorValue
    {
        public int[]  Indicies;
        public object Value;
    };

    /// <summary>
    ///     Used to iterate over every value in a multi-dimensional array.
    /// </summary>
    public class MultiDimensionArrayIterator : IEnumerable
    {
        #region Private Members

        private Array m_array = null; 

        #endregion
        #region Public Methods

        /// <summary>
        ///     Initializes a new multi dimensions array iterator.
        /// </summary>
        /// <param name="arr">Array to iterate over.</param>
        public MultiDimensionArrayIterator(Array arr)
        {
            m_array = arr;
        }

        /// <summary>
        ///     Gets the next value in the array.
        /// </summary>
        /// <returns>Next value in array.</returns>
        public IEnumerator GetEnumerator()
        {
            int[] offsets = new int[m_array.Rank];
            while (true)
            {
                bool done = false;
                for (int e = 0; e < offsets.Length; e++)
                {
                    if (offsets[e] >= m_array.GetLength(e))
                    {
                        // Finished iterating over all values.
                        if (e >= offsets.Length - 1)
                        {
                            done = true;
                            break;
                        }
                        // Increment the offset.
                        else
                        {
                            offsets[e] = 0;
                            offsets[e + 1]++;
                        }
                    }
                }
                if (done == true)
                {
                    break;
                }

                MultiDimensionArrayIteratorValue val;
                val.Value    = m_array.GetValue(offsets);
                val.Indicies = offsets;
                yield return val;

                offsets[0]++;
            }
        }

        #endregion
    }
}
