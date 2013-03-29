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
    ///     Represents a connection between a super peer and an individual end-user.
    /// </summary>
    public class SuperPeerToClientConnection
    {
        #region Members

        private GameClientService m_gameClient  = null;
        private SuperPeer   m_superpeer         = null;
        private Connection  m_connection        = null;
        private UserAccount m_account           = null;

        private bool        m_isActive          = true;

        private int         m_clientID          = 0;

        private bool        m_registered        = false;
        private bool        m_registering       = false;
        private bool        m_unregistered      = false;
        private bool        m_unregistering     = false;

        private int         m_unregisterTimer   = Environment.TickCount;

        private float       m_movementVectorX       = 0;
        private float       m_movementVectorY       = 0;
        private float       m_movementVectorSpeed   = 0;
        private int         m_movementTimer         = Environment.TickCount;

        private int         m_idleTimer             = 0;

        #endregion
        #region Properties

        /// <summary>
        ///     Returns the superpeer we are connected to.
        /// </summary>
        public SuperPeer SuperPeer
        {
            get { return m_superpeer; }
        }

        /// <summary>
        ///     Gets the network connection used to communicate with this super peer.
        /// </summary>
        public Connection Connection
        {
            get { return m_connection; }
        }

        /// <summary>
        ///     Gets or sets the ID of the client this super peer is handling.
        /// </summary>
        public int ClientID
        {
            get { return m_clientID; }
            set { m_clientID = value; }
        }

        /// <summary>
        ///     Returns true when this connection is active and being used. When it returns
        ///     false this connection can be cleaned up and stop being polled.
        /// </summary>
        public bool IsActive
        {
            get { return m_isActive; }
        }

        /// <summary>
        ///     Gets the movement vector this client is currently moving at.
        /// </summary>
        public float MovementVectorX
        {
            get { return m_movementVectorX; }
        }

        /// <summary>
        ///     Gets the movement vector this client is currently moving at.
        /// </summary>
        public float MovementVectorY
        {
            get { return m_movementVectorY; }
        }

        /// <summary>
        ///     Gets the movement vector speed this client is currently moving at.
        /// </summary>
        public float MovementVectorSpeed
        {
            get { return m_movementVectorSpeed; }
        }

        /// <summary>
        ///     Gets the account information for this peer.
        /// </summary>
        public UserAccount Account
        {
            get { return m_account; }
        }

        /// <summary>
        ///     Gets or sets the timer used to check when this connection is idle.
        /// </summary>
        public int IdleTimer
        {
            get { return m_idleTimer; }
            set { m_idleTimer = value; }
        }

        #endregion
        #region Methods

        /// <summary>
        ///     Processes an incoming packet from the client to the super peer.
        /// </summary>
        /// <param name="packet">Packet to process.</param>
        public void RecievedPacket(SuperPeerPacket packet)
        {
            // Peer is giving us an update of their movement speed.
            if (packet is SuperPeerSetMovementVectorPacket)
            {
                SuperPeerSetMovementVectorPacket specificPacket = packet as SuperPeerSetMovementVectorPacket;

                m_movementVectorX       = specificPacket.VectorX;
                m_movementVectorY       = specificPacket.VectorY;
                m_movementVectorSpeed   = specificPacket.Speed;
            }
        }

        /// <summary>
        ///     Processes an incoming packet from the arbitrator that relates to this client.
        /// </summary>
        /// <param name="packet">Packet to process.</param>
        public void RecievedArbitratorPacket(SuperPeerPacket packet)
        {
            // Has arbitrator finished retrieving our account.
            if (packet is SuperPeerRetrieveAccountReplyPacket)
            {
                if (IsActive == true)
                {
                    SuperPeerRetrieveAccountReplyPacket specificPacket = packet as SuperPeerRetrieveAccountReplyPacket;
                    FinishRegister(specificPacket.Account);
                }
            }

            // Has arbitrator finished storing our account.
            else if (packet is SuperPeerStoreAccountReplyPacket)
            {
                if (IsActive == true)
                {
                    SuperPeerStoreAccountReplyPacket specificPacket = packet as SuperPeerStoreAccountReplyPacket;
                    FinishUnregister();
                }
            }
        }

        /// <summary>
        ///     Begins registering this client with the super-peer, this is done
        ///     by downloading the clients current game-state from the arbitrator.
        /// </summary>
        public void BeginRegister()
        {
            if (m_registered == true || m_registering == true || m_unregistered == true)
            {
                return;
            }

            m_isActive      = true;
            m_registering   = true;
            m_registered    = false;

            // Send a request to the arbitrator for the clients account.
            SuperPeerRetrieveAccountPacket packet = new SuperPeerRetrieveAccountPacket();
            packet.SuperPeerID                    = m_superpeer.ID;
            packet.ClientID                       = m_clientID;
            m_gameClient.ArbitratorConnection.SendPacket(packet);
        }

        /// <summary>
        ///     Finishes the process of registering a peer.
        /// </summary>
        private void FinishRegister(UserAccount account)
        {
            if (m_registered == true || m_registering == false || m_unregistered == true || m_unregistering == true)
            {
                return;
            }

            m_unregistered      = false;
            m_registering       = false;
            m_registered        = true;
            m_unregistering     = false;
            m_isActive          = true;
            m_account           = account;

            // Send a success reply!
            SuperPeerRegisterReplyPacket reply = new SuperPeerRegisterReplyPacket();
            reply.SuperPeerID = m_superpeer.ID;
            m_connection.SendPacket(reply);

            // And register this peer with the superpeer.
            m_superpeer.PeerRegistered(this);
        }

        /// <summary>
        ///     Begins unregister this client form the super-peer, which involves
        ///     uploading the clients current game-state to the arbitrator.
        /// </summary>
        /// <param name="superPeerCount">Number of superpeers the client was connected to when he started unregistering.</param>
        public void BeginUnregister(int superPeerCount)
        {
            if (m_unregistering == true || 
                m_unregistered == true)
            {
                return;
            }

            m_unregistering = true;
            m_registered    = false;
            m_unregisterTimer = Environment.TickCount;

            // We call this before we have finished unregistering so that
            // the superpeer dosen't try and update our state while we
            // are sending our state to the arbitrator.
            m_superpeer.PeerUnregistered(this);

            // If we haven't even loaded an account yet, we can unregister right now.
            if (m_account == null || 
                (superPeerCount < 2))//m_gameClient.Settings.ZoneSuperPeerCount))
            {
                FinishUnregister();
            }

            // Otherwise lets store the account with the arbitrator before we unregister.
            else
            {
                Zone zone = m_gameClient.ZoneGrid.GetZoneByID(m_superpeer.ZoneID);
                StoreAccount("Unregister Request, SuperPeers=" + (zone != null ? zone.SuperPeers.Count.ToString() : "?") + " Peers=" + m_superpeer.RegisteredPeers.Count);
            }
        }

        /// <summary>
        ///     Sends a message to the arbitrator that attempts to store this clients state.
        /// </summary>
        internal void StoreAccount(string reason)
        {
            if (m_account == null || m_superpeer == null)
            {
                return;
            }

            SuperPeerStoreAccountPacket packet = new SuperPeerStoreAccountPacket();
            packet.SuperPeerID = m_superpeer.ID;
            packet.ClientID = m_clientID;
            packet.Account = m_account;
            packet.ZoneID = m_superpeer.ZoneID;
            packet.Reason = reason;
            m_gameClient.ArbitratorConnection.SendPacket(packet);
        }

        /// <summary>
        ///     Finishes the process of unregistering a peer.
        /// </summary>
        private void FinishUnregister()
        {
            if (m_unregistered == true)
            {
                throw new InvalidOperationException("Recieved invalid finish-unregister request, already unregistered.");
            }

            m_unregistered      = true;
            m_registering       = false;
            m_registered        = false;
            m_unregistering     = false;
            m_isActive          = false;
            m_account           = null;

            SuperPeerUnregisterReplyPacket reply = new SuperPeerUnregisterReplyPacket();
            reply.SuperPeerID = m_superpeer.ID;
            m_connection.SendPacket(reply);
        }

        /// <summary>
        ///     Updates the clients current persistent state.
        /// </summary>
        private void UpdateClientState()
        {
            // Do not update state if we are currently in the middle of registering or unregistering.
            if (m_unregistering         == true ||
                m_registering           == true ||
                m_unregistered          == true)
            {
                return;
            }

            // Do not update state if the zone we are updating does not have the minimum amount of peers yet.
            Zone zone = m_gameClient.ZoneGrid.GetZoneByID(m_superpeer.ZoneID);
            if (zone == null || zone.SuperPeers.Count < m_gameClient.Settings.ZoneSuperPeerCount)
            {
                return;
            }

            float delta = (((float)Environment.TickCount - m_movementTimer) / 1000.0f) * m_movementVectorSpeed;
            delta = Math.Min(1.0f, Math.Max(0.0f, delta));

            m_movementTimer = Environment.TickCount;

            m_account.PeristentState.X += m_movementVectorX * delta;
            m_account.PeristentState.Y += m_movementVectorY * delta;

            m_account.PeristentState.X = Math.Max(5, Math.Min(m_gameClient.Settings.WorldWidth - 5, m_account.PeristentState.X));
            m_account.PeristentState.Y = Math.Max(5, Math.Min(m_gameClient.Settings.WorldHeight - 5, m_account.PeristentState.Y));
        }

        /// <summary>
        ///     Polls this connection for changes.
        /// </summary>
        public void Poll()
        {
            // Update client position.
            if (m_account != null)
            {
                UpdateClientState();
            }

            // If superpeer is being deinitialized then start unregistering.
            //if (m_superpeer.IsActive == false)
            //{
           //     BeginUnregister();
            //}
        }

        /// <summary>
        ///     Constructs a new instance of this class.
        /// </summary>
        /// <param name="gameClient">Game client hosting this connection.</param>
        /// <param name="service">Superpeer a client is connected to..</param>
        /// <param name="connection">Connection used to communicate with the super peer.</param>
        public SuperPeerToClientConnection(GameClientService gameClient, SuperPeer superpeer, Connection connection)
        {
            m_superpeer = superpeer;
            m_connection = connection;
            m_gameClient = gameClient;
        }

        #endregion
    }

}
