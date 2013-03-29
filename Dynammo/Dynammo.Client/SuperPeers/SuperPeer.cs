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
    ///     Contains the code that is responsible for acting as a super-peer, updating all of those
    ///     connected to us as well as the arbitrator we are connected to.
    /// </summary>
    public class SuperPeer
    {
        #region Members

        private GameClientService                   m_service;
        private int                                 m_superpeer_id;
        private int                                 m_zone_id;
        private bool                                m_is_active;

        private List<SuperPeerToClientConnection>   m_registeredPeers = new List<SuperPeerToClientConnection>();

        private int                                 m_worldStateBroadcastTimer = Environment.TickCount;

        private int m_idleTimer = 0;

        #endregion
        #region Private Methods

        /// <summary>
        ///     Returns true if this super peer is active and processing the zone, it will return false
        ///     when the super peer is being deinitialized.
        /// </summary>
        public bool IsActive
        {
            get { return m_is_active; }
        }

        /// <summary>
        ///     Gets internal ID of this superpeer.
        /// </summary>
        public int ID
        {
            get { return m_superpeer_id; }
        }

        /// <summary>
        ///     Gets ID of zone this superpeer is in charge of.
        /// </summary>
        public int ZoneID
        {
            get { return m_zone_id; }
        }

        /// <summary>
        ///     Gets or sets a timer used to determine if this superpeer is idle or not.
        /// </summary>
        public int IdleTimer
        {
            get { return m_idleTimer; }
            set { m_idleTimer = value; }
        }

        /// <summary>
        ///     Gets a list of peers registered to this super peer.
        /// </summary>
        public List<SuperPeerToClientConnection> RegisteredPeers
        {
            get { return m_registeredPeers; }
        }

        #endregion
        #region Public Methods

        /// <summary>
        ///     Constructs a new instance of this class.
        /// </summary>
        /// <param name="service">Game client thats running this superpeer.</param>
        /// <param name="superpeer_id">Internal numeric ID of this superpeer.</param>
        /// <param name="zone_id">Internal numeric ID of zone we have control of.</param>
        public SuperPeer(GameClientService service, int superpeer_id, int zone_id)
        {
            m_service = service;
            m_superpeer_id = superpeer_id;
            m_zone_id = zone_id;
        }

        /// <summary>
        ///     Invoked when this super peer gains control of its zone.
        /// </summary>
        public void Initialize()
        {
            m_is_active = true;
        }

        /// <summary>
        ///     Invoked when this super peer looses control of its zone.
        /// </summary>
        public void Deinitialize()
        {
            m_is_active = false;
        }

        /// <summary>
        ///     Broadcasts the current state of the world to all clients connected to this super peer.
        /// </summary>
        public void BroadcastWorldState()
        {
            SuperPeerWorldStatePacket packet = new SuperPeerWorldStatePacket();
            packet.SuperPeerID = m_superpeer_id;

            // Count peer states we will send.
            int count = 0;
            foreach (SuperPeerToClientConnection peer in m_registeredPeers)
            {
                if (peer.Account != null)
                    count++;
            }

            // Insert peer state information.
            packet.Peers = new SuperPeerWorldStatePlayerInfo[count];

            int index = 0;
            foreach (SuperPeerToClientConnection peer in m_registeredPeers)
            {
                if (peer.Account == null)
                {
                    continue;
                }

                packet.Peers[index].ClientID = peer.ClientID;
                packet.Peers[index].Account = peer.Account.Clone();
                packet.Peers[index].Account.Email = "";
                packet.Peers[index].Account.Password = "";
                index++;
            }

            // Send to all registered peers!
            foreach (SuperPeerToClientConnection peer in m_registeredPeers)
            {
                peer.Connection.SendPacket(packet);
            }
        }

        /// <summary>
        ///     Works out if we are the current "master" superpeer. The master is the one
        ///     that sends updates to clients.
        /// </summary>
        /// <returns>True if we are the master, otherwise false.</returns>
        public bool IsMasterSuperPeer()
        {
            Zone zone = m_service.ZoneGrid.GetZoneByID(m_zone_id);
            if (zone == null)
            {
                return false;
            }

            if (zone.SuperPeers.Count < m_service.Settings.ZoneSuperPeerCount)
            {
                return false;
            }

            int min_peer_id = 0;
            foreach (ZoneSuperPeer peer in zone.SuperPeers)
            {
                if (peer.ID < min_peer_id || min_peer_id == 0)
                {
                    min_peer_id = peer.ID;
                }
            }

            return m_superpeer_id == min_peer_id;
        }

        /// <summary>
        ///     Invoked when a super-peer gains control over a zone.
        /// </summary>
        /// <param name="zone">Zone that super peer gained control of.</param>
        /// <param name="superPeer">Super peer that gained control.</param>
        internal void SuperPeerGainedControl(Zone zone, ZoneSuperPeer superPeer)
        {
        }

        /// <summary>
        ///     Invoked when a super-peer lose's control over a zone.
        /// </summary>
        /// <param name="zone">Zone that super peer lost control of.</param>
        /// <param name="superPeer">Super peer that lost control.</param>
        internal void SuperPeerLostControl(Zone zone, ZoneSuperPeer superPeer)
        {
            // Peer from same zone was lost, we should try and serialize our state now.
            if (superPeer.ZoneID == m_zone_id)
            {
                foreach (SuperPeerToClientConnection conn in m_registeredPeers)
                {
                   conn.StoreAccount("SuperPeer In Zone Lost Control, SuperPeers="+zone.SuperPeers.Count+" Peers="+m_registeredPeers.Count);
                }
            }
        }

        /// <summary>
        ///     Responsible for updating the super peer.
        /// </summary>
        public void Poll()
        {
            // Time to broadcast a new world state update.
            if (Environment.TickCount > m_worldStateBroadcastTimer)
            {
                if (m_registeredPeers.Count > 0 && IsMasterSuperPeer() && IsActive == true)
                {
                    BroadcastWorldState();
                }
                m_worldStateBroadcastTimer = Environment.TickCount + m_service.Settings.SuperPeerWorldStateBroadcastInterval;
            }
        }

        /// <summary>
        ///     Invoked when a game peer connected to us registers its interest in this super peer.
        /// </summary>
        public void PeerRegistered(SuperPeerToClientConnection peer)
        {
            if (peer.Account == null ||
                m_registeredPeers.Contains(peer))
            {
                throw new InvalidOperationException("Invalid peer, could not register. Either account is null or peer is already registered.");
            }
            m_registeredPeers.Add(peer);
            Logger.Info("New client #{0} registered to SuperPeer #{1}.", LoggerVerboseLevel.High, peer.ClientID, m_superpeer_id);
        }

        /// <summary>
        ///     Invoked when a game peer connected to us un-registers (or disconnects) its interest in this super peer.
        /// </summary>
        public void PeerUnregistered(SuperPeerToClientConnection peer)
        {
            if (!m_registeredPeers.Contains(peer))
            {
                throw new InvalidOperationException("Invalid peer, could not unregister. Peer is already unregistered.");
            }
            m_registeredPeers.Remove(peer);
            Logger.Info("Client #{0} unregistered from SuperPeer #{1}.", LoggerVerboseLevel.High, peer.ClientID, m_superpeer_id);
        }

        /// <summary>
        ///     Invoked when an arbitrator sends a packet to this super peer.
        /// </summary>
        public void ArbitratorRecievedPacket(Connection connection, SuperPeerPacket packet)
        {

        }

        #endregion
    }

}
