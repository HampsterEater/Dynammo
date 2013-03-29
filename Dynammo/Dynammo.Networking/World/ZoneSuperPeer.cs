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

namespace Dynammo.Networking
{

    /// <summary>
    ///     Stores information about individual super-peers.
    /// </summary>
    public class ZoneSuperPeer
    {
        #region Members

        private int m_superPeerID           = 0;
        private int m_clientDatabaseID      = 0;
        private int m_zoneDatabaseID        = 0;

        private string m_clientIPAddress    = "";
        private int    m_clientListenPort   = 0;

        #endregion
        #region Properties

        /// <summary>
        ///     Gets the database ID for this super peer.
        /// </summary>
        public int ID
        {
            get { return m_superPeerID; }
            set { m_superPeerID = value; }
        }

        /// <summary>
        ///     Gets the database ID for the zone this client is controlling.
        /// </summary>
        public int ZoneID
        {
            get { return m_zoneDatabaseID; }
            set { m_zoneDatabaseID = value; }
        }

        /// <summary>
        ///     Gets the database ID for the client acting as a super peer.
        /// </summary>
        public int ClientID
        {
            get { return m_clientDatabaseID; }
            set { m_clientDatabaseID = value; }
        }

        /// <summary>
        ///     Gets the IP address of the client acting as this super peer.
        /// </summary>
        public string ClientIPAddress
        {
            get { return m_clientIPAddress; }
            set { m_clientIPAddress = value; }
        }

        /// <summary>
        ///     Gets the port of the client acting as this super peer.
        /// </summary>
        public int ClientListenPort
        {
            get { return m_clientListenPort; }
            set { m_clientListenPort = value; }
        }

        #endregion
        #region Methods

        #endregion
    }
}
