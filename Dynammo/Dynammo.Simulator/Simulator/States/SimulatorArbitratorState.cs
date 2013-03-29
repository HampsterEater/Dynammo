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

namespace Dynammo.Simulator
{

    /// <summary>
    ///     Stores information about an arbitrator that is part of a simulation network.
    /// </summary>
    public class SimulatorArbitratorState
    {
        #region Private Members

        private Dictionary<string, object> m_databaseSettings = new Dictionary<string, object>();

        private object m_metaData = null;

        private bool m_isMaster = false;

        #endregion
        #region Public Properties

        /// <summary>
        ///     Gets the database settings this arbitrator currently has associated with it.
        /// </summary>
        public Dictionary<string, object> DatabaseSettings
        {
            get { return m_databaseSettings; }
        }

        /// <summary>
        ///     Gets or sets a meta data value assigned to this object.
        /// </summary>
        public object MetaData
        {
            get { return m_metaData; }
            set { m_metaData = value; }
        }

        /// <summary>
        ///     Gets or sets if this arbitrator is the master arbitrator.
        /// </summary>
        public bool IsMaster
        {
            get { return m_isMaster; }
            set { m_isMaster = value; }
        }

        #endregion
        #region Private Methods

        #endregion
        #region Public Methods

        #endregion
    }

}
