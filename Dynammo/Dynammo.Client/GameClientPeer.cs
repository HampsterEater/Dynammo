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
using Dynammo.Common;
using Dynammo.Networking;

namespace Dynammo.Client
{
    /// <summary>
    ///     This class essentially works as a proxy. It routes packets recieved from a peer connected
    ///     to us, to the super-peers that the peer wishs to communicate with.
    /// </summary>
    public class GameClientPeer
    {
        #region Private Members

        // General settings.
        private GameClientService                   m_gameClient;
        private Connection                          m_connection;

        // Information about this peer.
        private List<SuperPeerToClientConnection>   m_superPeerConnections = new List<SuperPeerToClientConnection>();

        #endregion
        #region Public Properties

        /// <summary>
        ///     Gets a list of superpeers connections this peer is currently conencted to 
        /// </summary>
        public List<SuperPeerToClientConnection> SuperPeerConnections
        {
            get { return m_superPeerConnections; }
        }

        /// <summary>
        ///     Returns true if this peer is active and still processing connections. When
        ///     set to false this peer will stop being polled and be remove from the game.
        /// </summary>
        public bool IsActive
        {
            get { return m_superPeerConnections.Count > 0; }
        }

        #endregion
        #region Private Methods

        /// <summary>
        ///     Processes a packet that has been recieved from a peer.
        /// </summary>
        /// <param name="packet">Packet that we recieved.</param>
        private void ProcessIncomingPacket(Packet packet)
        {
            // Peer is registering for events from a superpeer.
            if (packet is SuperPeerRegisterPacket)
            {
                SuperPeerRegisterPacket specificPacket = packet as SuperPeerRegisterPacket;

                SuperPeer peer = m_gameClient.FindSuperPeerByID(specificPacket.SuperPeerID);

                // Wait on arbitrator until we get information about the super-peer.
                while (peer == null)
                {
                    if (m_gameClient.ConnectedToArbitrator == false)
                    {
                        return;
                    }
                    m_gameClient.PollArbitrator();
                    peer = m_gameClient.FindSuperPeerByID(specificPacket.SuperPeerID);
                }

                if (peer != null)
                {
                    /*
                    foreach (SuperPeerToClientConnection p in m_superPeerConnections)
                    {
                        if (p.SuperPeer.ID == peer.ID &&
                            p.ClientID == specificPacket.ClientID)
                        {
                            p.BeginRegister();
                            return;
                        }
                    }*/

                    SuperPeerToClientConnection conn = new SuperPeerToClientConnection(m_gameClient, peer, m_connection);
                    conn.ClientID = specificPacket.ClientID;
                    m_superPeerConnections.Add(conn);

                    conn.BeginRegister();

                    return;
                }

                throw new Exception("Attempt to register to non-existing superpeer.");
            }

            // Peer is unregistering for events from a superpeer.
            else if (packet is SuperPeerUnregisterPacket)
            {
                SuperPeerUnregisterPacket specificPacket = packet as SuperPeerUnregisterPacket;

                bool found = false;
                foreach (SuperPeerToClientConnection conn in m_superPeerConnections)
                {
                    if (conn.SuperPeer.ID == specificPacket.SuperPeerID)
                    {
                        conn.BeginUnregister(specificPacket.ChangeZoneSuperPeerCount);
                        found = true;

                        // Do not break out of this loop, we may need to send packet to lingering connections as well!
                    }
                }

                if (found == false)
                {
                    throw new Exception("Attempt to unregister from non-existing superpeer.");
                }
            }

            // Peer is sending a super peer a message.
            else if ((packet as SuperPeerPacket) != null)
            {
                SuperPeerPacket specificPacket = packet as SuperPeerPacket;

                bool found = false;
                foreach (SuperPeerToClientConnection peer in m_superPeerConnections)
                {
                    if (peer.SuperPeer.ID == specificPacket.SuperPeerID)
                    {
                        peer.RecievedPacket(specificPacket);
                        found = true;

                        // Do not break out of this loop, we may need to send packet to lingering connections as well!
                    }
                }

                if (found == false)
                {
                    throw new Exception("Attempt to send-packet to non-existing superpeer.");
                }
            }
        }

        #endregion
        #region Public Methods

        /// <summary>
        ///     Constructs a new instance of this class.
        /// </summary>
        /// <param name="client">Game client that this peer is connected to.</param>
        /// <param name="peer_connection">Connection through which this peer is connected.</param>
        public GameClientPeer(GameClientService client, Connection peer_connection)
        {
            m_gameClient = client;
            m_connection = peer_connection;
        }

        /// <summary>
        ///     Invoked when this peer is connected.
        /// </summary>
        public void Connected()
        {
            // Dum de dum do.
        }

        /// <summary>
        ///     Invoked when this peer is disconnected.
        /// </summary>
        public void Disconnected()
        {
            // Unregister from all super peers.
            foreach (SuperPeerToClientConnection peer in m_superPeerConnections)
            {
                peer.BeginUnregister(m_gameClient.Settings.ZoneSuperPeerCount);
            }
        }

        /// <summary>
        ///     Polls this peer connections, processing packets, updating registrations, etc.
        /// </summary>
        public void Poll()
        {
            // Read in packets.
            Packet packet = m_connection.DequeuePacket();
            if (packet != null)
            {
                ProcessIncomingPacket(packet);
            }

            // Update all super peer connections.
            foreach (SuperPeerToClientConnection peer in m_superPeerConnections.ToList())
            {
                peer.Poll();

                if (peer.IsActive == false)
                {
                    if (peer.IdleTimer == 0)
                    {
                        peer.IdleTimer = Environment.TickCount;
                    }
                    else
                    {
                        if (Environment.TickCount - peer.IdleTimer > 30 * 1000)
                        {
                            m_superPeerConnections.Remove(peer);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
