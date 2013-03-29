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
using Dynammo.Networking;
using Dynammo.Common;

namespace Dynammo.Client
{

    /// <summary>
    ///     Contains the code that processes a connection between the client and a computer that hosts super peers.
    /// </summary>
    public class ClientToSuperPeerConnection
    {
        #region Members

        private GameClientService   m_service    = null;
        private Connection          m_connection = null;
        private int                 m_clientID   = 0;

        #endregion
        #region Properties

        /// <summary>
        ///     Gets the network connection used to communicate with this super peer.
        /// </summary>
        public Connection Connection
        {
            get { return m_connection; }
        }

        /// <summary>
        ///     Gets the Client-ID of the superpeer on the end of this connection.
        /// </summary>
        public int ClientID
        {
            get { return m_clientID; }
        }

        #endregion
        #region Methods

        /// <summary>
        ///     Processes an incoming packet from the super peer.
        /// </summary>
        /// <param name="packet">Packet to process.</param>
        public void ProcessIncomingPacket(Packet packet)
        {
            // Has superpeer aknowledged our registering of them.
            if (packet is SuperPeerRegisterReplyPacket)
            {
                SuperPeerRegisterReplyPacket specificPacket = packet as SuperPeerRegisterReplyPacket;

                m_service.SuperPeerRegistered(specificPacket.SuperPeerID);
            }

            // Has superpeer aknowledged our unregistering of them.
            else if (packet is SuperPeerUnregisterReplyPacket)
            {
                SuperPeerUnregisterReplyPacket specificPacket = packet as SuperPeerUnregisterReplyPacket;
                m_service.SuperPeerUnregistered(specificPacket.SuperPeerID);
            }

            // Has superpeer sent us a world state?
            else if (packet is SuperPeerWorldStatePacket)
            {
                SuperPeerWorldStatePacket specificPacket = packet as SuperPeerWorldStatePacket;
                m_service.RecievedWorldState(specificPacket);
            }
        }

        /// <summary>
        ///     Polls this connection for changes.
        /// </summary>
        public void Poll()
        {
            Packet packet = m_connection.DequeuePacket();
            if (packet != null)
            {
                ProcessIncomingPacket(packet);
            }
        }

        /// <summary>
        ///     Constructs a new instance of this class.
        /// </summary>
        /// <param name="service">Game client thats hosting this connection.</param>
        /// <param name="connection">Connection used to communicate with the super peer.</param>
        /// <param name="id">Client ID of superpeer on the other end of this connection.</param>
        public ClientToSuperPeerConnection(GameClientService service, Connection connection, int id)
        {
            m_service = service;
            m_connection = connection;
            m_clientID = id;
        }

        #endregion
    }

}
