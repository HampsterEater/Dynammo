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
using Dynammo.Arbitrator;
using Dynammo.Networking;

namespace Dynammo.Simulator
{

    /// <summary>
    ///     Stores information about a client that is part of a simulation network.
    /// </summary>
    public class SimulatorClientState
    {
        #region Private Members

        private Dictionary<string, object> m_databaseSettings = new Dictionary<string, object>();

        private object m_metaData = null;

        private UserAccount m_userAccount = null;

        private int m_createTime = Environment.TickCount;

        private UserAccountPersistentState m_lastClientState = null;

        #endregion
        #region Public Properties

        /// <summary>
        ///     Gets the database settings this client currently has associated with it.
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
        ///     Gets or sets the account this client is assigned to.
        /// </summary>
        public UserAccount Account
        {
            get { return m_userAccount; }
            set { m_userAccount = value; }
        }

        /// <summary>
        ///     Gets the time in ticks since this class was instantiated.
        /// </summary>
        public int CreateTime
        {
            get { return m_createTime; }
            set { m_createTime = value; }
        }

        /// <summary>
        ///     Gets or sets the last client state we found that refers to this client. This is used
        ///     primarily to keep track of where to render the player in the simulator.
        /// </summary>
        public UserAccountPersistentState LastClientState
        {
            get { return m_lastClientState; }
            set { m_lastClientState = value; }
        }

        #endregion
        #region Private Methods

        #endregion
        #region Public Methods

        #endregion
    }

}
