using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynammo.Common
{

    /// <summary>
    ///     Contains some helpful methods for random number generation.
    /// </summary>
    public static class RandomHelper
    {
        #region Private Members

        private static Random m_random = new Random(Environment.TickCount);

        #endregion
        #region Public Properties

        public static Random RandomInstance
        {
            get { return m_random; }
        }

        #endregion
    }
}
