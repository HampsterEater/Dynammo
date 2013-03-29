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
using System.Threading;
using System.Threading.Tasks;
using Dynammo;
using Dynammo.Common;
using Dynammo.Networking;

namespace Dynammo.Client
{

    /// <summary>
    ///     Primary class that deals with connecting to an arbitrator and recieving game state updates.
    /// </summary>
    public class GameClientService
    {
        #region Private Members

        // General constants.
        private const string LISTEN_HOST = "localhost";
        private const ushort LISTEN_PORT = 0; // Listening on port 0 causes the OS to allocate us the first free port.

        // Connection related variables.
        private Connection m_arbitratorConnection;
        private Connection m_listenConnection;
        private List<Connection> m_disconnectedPeers = new List<Connection>();

        // Settings/Arbitrator connection information.
        private string m_arbitratorHost;
        private ushort m_arbitratorPort;
        private Settings m_settings;

        // World state.
        private ZoneGrid m_zoneGrid = new ZoneGrid();

        // Our own client state.
        private UserAccount                 m_account               = null;
        private int                         m_clientID              = 0;
        private Zone                        m_currentZone           = null;
        private SuperPeerWorldStatePacket   m_worldState            = null;
        private int                         m_changeZoneSuperPeerCount = 0;

        private bool                        m_threadNameSet = false;

        // SuperPeer state.
        private List<SuperPeer> m_superPeers = new List<SuperPeer>();

        // SuperPeer connections on end-user.
        private List<ClientToSuperPeerConnection> m_superPeerConnections    = new List<ClientToSuperPeerConnection>();
        private List<ZoneSuperPeer> m_registeredSuperPeers          = new List<ZoneSuperPeer>();

        private List<ZoneSuperPeer> m_unregisteringSuperPeers       = new List<ZoneSuperPeer>();
        private List<ZoneSuperPeer> m_registeringSuperPeers         = new List<ZoneSuperPeer>();

        private List<string> m_registerEventList = new List<string>();

        private bool m_superPeersDirty = false;

        #endregion
        #region Public Properties

        /// <summary>
        ///     Gets if the super-peers states have been modified since this value was last retrieved.
        /// </summary>
        public bool SuperPeersDirty
        {
            get
            {
                bool val = m_superPeersDirty;
                m_superPeersDirty = false;
                return val;
            }
        }

        /// <summary>
        ///     Gets the number of bytes sent via this client since last call.
        /// </summary>
        public int DeltaBandwidthIn
        {
            get
            {
                int final = 0;

                if (m_arbitratorConnection != null)
                {
                    final += m_arbitratorConnection.DeltaBandwidthIn;
                }
                if (m_listenConnection != null)
                {
                    final += m_listenConnection.DeltaBandwidthIn;
                }
                for (int i = 0; i < m_superPeerConnections.Count; i++)
                {
                    final += m_superPeerConnections[i].Connection.DeltaBandwidthIn;
                }

                return final;
            }
        }

        /// <summary>
        ///     Gets the number of bytes recieved via this client since last call.
        /// </summary>
        public int DeltaBandwidthOut
        {
            get
            {
                int final = 0;

                if (m_arbitratorConnection != null)
                {
                    final += m_arbitratorConnection.DeltaBandwidthOut;
                }
                if (m_listenConnection != null)
                {
                    final += m_listenConnection.DeltaBandwidthOut;
                }
                for (int i = 0; i < m_superPeerConnections.Count; i++)
                {
                    final += m_superPeerConnections[i].Connection.DeltaBandwidthOut;
                }

                return final;
            }
        }

        /// <summary>
        ///     Returns true if we are currently connected to an arbitrator.
        /// </summary>
        public bool ConnectedToArbitrator
        {
            get
            {
                if (m_arbitratorConnection == null)
                {
                    return false;
                }
                else
                {
                    return m_arbitratorConnection.Connected;
                }
            }
        }

        /// <summary>
        ///     Returns true if we are currently listening for peer connections.
        /// </summary>
        public bool ListeningForPeers
        {
            get
            {
                if (m_listenConnection == null)
                {
                    return false;
                }
                else
                {
                    return m_listenConnection.Connected;
                }
            }
        }

        /// <summary>
        ///     Returns the connection to the arbitrator.
        /// </summary>
        internal Connection ArbitratorConnection
        {
            get { return m_arbitratorConnection; }
        }

        /// <summary>
        ///     Returns a list of super peers hosted locally.
        /// </summary>
        public List<SuperPeer> SuperPeers
        {
            get { return m_superPeers; }
        }

        /// <summary>
        ///     Returns a list of all peers connected to this service.
        /// </summary>
        public List<GameClientPeer> Peers
        {
            get
            {
                List<GameClientPeer> list = new List<GameClientPeer>();

                if (m_listenConnection != null)
                {
                    foreach (Connection connection in m_listenConnection.Peers)
                    {
                        if (connection.MetaData != null)
                        {
                            list.Add((GameClientPeer)connection.MetaData);
                        }
                    }
                }

                return list;
            }
        }

        /// <summary>
        ///     Gets the settings that this client is running with.
        /// </summary>
        internal Settings Settings
        {
            get { return m_settings; }
        }

        /// <summary>
        ///     Returns true if we are currently in the process of changing superpeers.
        /// </summary>
        public bool UnregisteringWithSuperPeers
        {
            get
            {
                return (m_unregisteringSuperPeers.Count > 0);
            }
        }

        /// <summary>
        ///     Returns true if we are currently in the process of changing superpeers.
        /// </summary>
        public bool RegisteringWithSuperPeers
        {
            get
            {
                return (m_currentZone == null || m_registeringSuperPeers.Count > 0);// !ConnectedToAllZoneSuperPeers(m_currentZone));
            }
        }

        /// <summary>
        ///     Returns a list of super peers we are registered to.
        /// </summary>
        public List<ZoneSuperPeer> RegisteredSuperPeers
        {
            get { return m_registeredSuperPeers.ToList(); }
        }

        /// <summary>
        ///     Gets our client-id within the game world.
        /// </summary>
        public int ClientID
        {
            get { return m_clientID; }
        }

        /// <summary>
        ///     Gets the zone we are currently in.
        /// </summary>
        public Zone CurrentZone
        {
            get { return m_currentZone; }
        }

        /// <summary>
        ///     Gets the account we are logged in with.
        /// </summary>
        public UserAccount Account
        {
            get { return m_account; }
        }

        /// <summary>
        ///     Gets the last world state recieved from a super peer.
        /// </summary>
        public SuperPeerWorldStatePacket WorldState
        {
            get { return m_worldState; }
        }

        /// <summary>
        ///     Gets the current zone-grid.
        /// </summary>
        public ZoneGrid ZoneGrid
        {
            get { return m_zoneGrid; }
        }

        #endregion
        #region Private Methods

        /// <summary>
        ///     Sets up the connection to an arbitrator to recieve information about the game world.
        /// </summary>
        /// <returns>Returns true if successful, else false.</returns>
        private async Task<bool> SetupArbitratorConnection()
        {
            bool result = false;

            if (m_arbitratorConnection == null)
            {
                Logger.Info("Attempting to connect to arbitrator on {0}:{1} ...", LoggerVerboseLevel.High, m_arbitratorHost, m_arbitratorPort);

                m_arbitratorConnection = new Connection();
                result = await m_arbitratorConnection.ConnectAsync(m_arbitratorHost, m_arbitratorPort);
            }
            else
            {
                Logger.Error("Lost connection to arbitrator, attempting to setup connection again ...", LoggerVerboseLevel.Normal);
                result = await m_arbitratorConnection.ReconnectAsync();
            }

            if (result == false)
            {
                Logger.Error("Failed to connect to arbitrator on {0}:{1}", LoggerVerboseLevel.Normal, m_arbitratorHost, m_arbitratorPort);
            }
            else
            {
                Logger.Info("Successfully connected to arbitrator.", LoggerVerboseLevel.High);
            }

            return result;
        }

        /// <summary>
        ///     Sets up the listen connection to accept incoming connections from peers.
        /// </summary>
        /// <returns>Returns true if successful, else false.</returns>
        private async Task<bool> SetupListenConnection()
        {
            bool result = false;

            if (m_listenConnection == null)
            {
                Logger.Info("Attempting to listen on {0}:{1} ...", LoggerVerboseLevel.High, LISTEN_HOST, LISTEN_PORT);

                m_listenConnection = new Connection();
                result = await m_listenConnection.ListenAsync(LISTEN_PORT, LISTEN_HOST);
            }
            else
            {
                Logger.Error("Lost listening connection, attempting to setup connection again ...", LoggerVerboseLevel.Normal);
                result = await m_listenConnection.RelistenAsync();
            }

            if (result == false)
            {
                Logger.Error("Failed to listen on {0}:{1}", LoggerVerboseLevel.Normal, LISTEN_HOST, LISTEN_PORT);
            }
            else
            {
                Logger.Info("Successfully began listening.", LoggerVerboseLevel.High);
            }

            return result;
        }

        /// <summary>
        ///     Invoked when we recieve a world state from the superpeers controlling our zone.
        /// </summary>
        /// <param name="state">World state recieved.</param>
        internal void RecievedWorldState(SuperPeerWorldStatePacket state)
        {
            if (m_currentZone == null)
            {
                return;
            }
            
            // Check packet comes from a superpeer simulating our current zone.
            int lowest_id = 0;
            foreach (ZoneSuperPeer p in m_currentZone.SuperPeers)
            {
                if (p.ID < lowest_id || lowest_id == 0)
                {
                    lowest_id = p.ID;
                }
            }

            // If it dosen't then ignore it, as its probably a left over from a superpeer we are
            // currently unregistering from.
            if (lowest_id != state.SuperPeerID)
            {
                return;
            }

            // Check for jumps.
            /*
            if (m_worldState != null)
            {
                foreach (SuperPeerWorldStatePlayerInfo newInfo in state.Peers)
                {
                    foreach (SuperPeerWorldStatePlayerInfo oldInfo in m_worldState.Peers)
                    {
                        if (newInfo.ClientID == oldInfo.ClientID)
                        {
                            float dx = Math.Abs(newInfo.Account.PeristentState.X - oldInfo.Account.PeristentState.X);
                            float dy = Math.Abs(newInfo.Account.PeristentState.Y - oldInfo.Account.PeristentState.Y);

                            if (dx > 26.0f || dy > 26.0f)
                            {
                                System.Console.WriteLine("WUT!");
                            }
                        }
                    }
                }
            }
            */

            m_worldState = state;

            foreach (SuperPeerWorldStatePlayerInfo peer in state.Peers)
            {
                if (peer.ClientID == m_clientID)
                {
                    m_account = peer.Account;
                }
            }
        }

        /// <summary>
        ///     Processes a packet that has been recieved from the arbitrator.
        /// </summary>
        /// <param name="peer">The client or peer we recieved the packet from.</param>
        /// <param name="packet">Packet that we recieved.</param>
        private void ProcessArbitratorIncomingPacket(Connection peer, Packet packet)
        {
            // -----------------------------------------------------------------
            // We've been sent an updated version of the zone grid.
            // -----------------------------------------------------------------           
            if (packet is ZoneGridPacket)
            {
                ZoneGridPacket specificPacket = packet as ZoneGridPacket;
                m_zoneGrid.FromPacket(specificPacket);

                // Invoke events for all super peers who have gained control of zones.
                foreach (ZoneSuperPeer p in m_zoneGrid.GainedSuperPeers)
                {
                    Zone zone = m_zoneGrid.GetZoneByID(p.ZoneID);
                    SuperPeerGainedControl(zone, p);
                }
                
                // Invoke events for all super peers who have lost control of zones.
                foreach (ZoneSuperPeer p in m_zoneGrid.LostSuperPeers)
                {
                    Zone zone = m_zoneGrid.GetZoneByID(p.ZoneID);
                    SuperPeerLostControl(zone, p);
                }

                Logger.Info("Recieved updated zone grid from arbitrator.", LoggerVerboseLevel.High);
            }

            // -----------------------------------------------------------------
            // We've been sent our persistent state information.
            // -----------------------------------------------------------------
            else if (packet is UserAccountStatePacket)
            {
                UserAccountStatePacket specificPacket = packet as UserAccountStatePacket;

                m_account  = specificPacket.Account;
                m_clientID = specificPacket.ClientID;

                if (m_threadNameSet == false)
                {
                    System.Threading.Thread.CurrentThread.Name = "Client #" + m_clientID;
                    m_threadNameSet = true;
                }

                Logger.Info("Recieved updated account information from arbitrator.", LoggerVerboseLevel.High);
            }

            // -----------------------------------------------------------------
            // Information sent to one of our super peers about a client?
            // -----------------------------------------------------------------
            else if ((packet as SuperPeerClientPacket) != null)
            {
                SuperPeerClientPacket specificPacket = packet as SuperPeerClientPacket;
                bool found = false;

                foreach (Connection connection in m_listenConnection.Peers)
                {
                    if (connection.MetaData != null)
                    {
                        GameClientPeer p = ((GameClientPeer)connection.MetaData);
                        foreach (SuperPeerToClientConnection conn in p.SuperPeerConnections)    // ERROR IS HERE: SuperPeerConnections is filled with inactive ones that are recieving the packets too :(
                        {
                            if (conn.SuperPeer.ID == specificPacket.SuperPeerID &&
                                conn.ClientID     == specificPacket.ClientID)
                            {
                                conn.RecievedArbitratorPacket(specificPacket);
                                found = true;
                            }
                        }
                    }
                }

                if (found == false)
                {
                    throw new InvalidOperationException("Failed to find super peer to direct arbitrator packet to.");
                }
            }

            // -----------------------------------------------------------------
            // Information sent to one of our super peers?
            // -----------------------------------------------------------------
            else if ((packet as SuperPeerPacket) != null)
            {
                SuperPeerPacket specificPacket = packet as SuperPeerPacket;
                SuperPeer superpeer = FindSuperPeerByID(specificPacket.SuperPeerID);

                if (superpeer != null)
                {
                    superpeer.ArbitratorRecievedPacket(m_arbitratorConnection, specificPacket);
                }
                else
                {
                    throw new InvalidOperationException("Failed to find super peer to direct arbitrator packet to.");
                }
            }
        }

        /// <summary>
        ///     Invoked when a new peer connects.
        /// </summary>
        /// <param name="peer">Peers connection.</param>
        private void ProcessConnectedPeer(Connection peer)
        {
            GameClientPeer peerMetaData = new GameClientPeer(this, peer);
            peerMetaData.Connected();

            peer.MetaData = peerMetaData;
        }

        /// <summary>
        ///     Invoked when a new peer disconnects.
        /// </summary>
        /// <param name="peer">Peers connection.</param>
        private void ProcessDisconnectedPeer(Connection peer)
        {
            GameClientPeer peerMetaData = peer.MetaData as GameClientPeer;
            if (peerMetaData == null)
            {
                return;
            }

            peerMetaData.Disconnected();

            m_disconnectedPeers.Add(peer);
        }

        /// <summary>
        ///     Invoked when a super-peer gains control over a zone.
        /// </summary>
        /// <param name="zone">Zone that super peer gained control of.</param>
        /// <param name="superPeer">Super peer that gained control.</param>
        private void SuperPeerGainedControl(Zone zone, ZoneSuperPeer superPeer)
        {
            // Is this us thats gained control of a zone?
            if (superPeer.ClientID == m_clientID)
            {
                SuperPeer peer  = new SuperPeer(this, superPeer.ID, superPeer.ZoneID);
                peer.Initialize();

                m_superPeers.Add(peer);

                Logger.Info("Gained control of zone #{0} as SuperPeer #{0}", LoggerVerboseLevel.High, zone.ID, superPeer.ID);
            }
            
            foreach (SuperPeer peer in m_superPeers)
            {
                peer.SuperPeerGainedControl(zone, superPeer);
            }
        }

        /// <summary>
        ///     Invoked when a super-peer lose's control over a zone.
        /// </summary>
        /// <param name="zone">Zone that super peer lost control of.</param>
        /// <param name="superPeer">Super peer that lost control.</param>
        private void SuperPeerLostControl(Zone zone, ZoneSuperPeer superPeer)
        {
            // Is this us thats gained control of a zone?
            if (superPeer.ClientID == m_clientID)
            {
                foreach (SuperPeer peer in m_superPeers.ToList())
                {
                    if (peer.ID == superPeer.ID)
                    {
                        peer.Deinitialize();
                        break;
                    }
                }

                Logger.Info("Lost control of zone #{0} as SuperPeer #{0}", LoggerVerboseLevel.High, superPeer.ZoneID, superPeer.ID);
            }

            // Is this a superpeer for the zone we are inside of?
            // If so, any superpeers we are hosting, should start serializing now!
            foreach (SuperPeer peer in m_superPeers)
            {
                peer.SuperPeerLostControl(zone, superPeer);
            }
        }

        /// <summary>
        ///     Invoked when a super peer acknowledges our request to register with it.
        /// </summary>
        /// <param name="id">Superpeer to register with.</param>
        internal void SuperPeerRegistered(int id)
        {
            foreach (ZoneSuperPeer target_peer in m_registeringSuperPeers.ToList())
            {
                if (id == target_peer.ID)
                {
                    m_registeringSuperPeers.Remove(target_peer);
                    m_registeredSuperPeers.Add(target_peer);

                    m_superPeersDirty = true;

                    m_registerEventList.Add("Registered with " + target_peer.ID);
                        
                    Logger.Info("Registration request to SuperPeer #{0} was successful.", LoggerVerboseLevel.High, id);

                    return;
                }
            }

            Logger.Warning("Registration request to SuperPeer #{0} was unsuccessful, zone super peer no longer exists.", LoggerVerboseLevel.Normal, id);
        }

        /// <summary>
        ///     Invoked when a super peer acknowledges our request to unregister with it.
        /// </summary>
        /// <param name="id">Superpeer to register with.</param>
        internal void SuperPeerUnregistered(int id)
        {
            foreach (ZoneSuperPeer target_peer in m_registeredSuperPeers.ToList())
            {
                if (id == target_peer.ID)
                {
                    m_registeredSuperPeers.Remove(target_peer);
                }
            }

            foreach (ZoneSuperPeer target_peer in m_unregisteringSuperPeers.ToList())
            {
                if (id == target_peer.ID)
                {
                    //System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
                    //System.Console.WriteLine("Removed Unreg:" + target_peer.ID +" - "+m_clientID + "\n{0}{1}{2}", t.GetFrame(0), t.GetFrame(1), t.GetFrame(2));
                   
                    m_registerEventList.Add("Unregistered with " + target_peer.ID);

                    m_unregisteringSuperPeers.Remove(target_peer);
                    m_superPeersDirty = true;

                    Logger.Info("Unregistration request to SuperPeer #{0} was successful, {1} left to unregister.", LoggerVerboseLevel.High, id, m_unregisteringSuperPeers.Count);

                    return;
                }
            }

            Logger.Warning("Unregistration request to SuperPeer #{0} was unsuccessful, zone super peer no longer exists.", LoggerVerboseLevel.High, id);
        }

        /// <summary>
        ///     Gets a list of connections to super peers that are controlling
        ///     the given zone.
        /// </summary>
        /// <param name="zone">Zone that is being controlled.</param>
        /// <returns>List of connections to super peers of the zone.</returns>
        private List<ClientToSuperPeerConnection> GetZoneSuperPeerConnections(Zone zone)
        {
            List<ClientToSuperPeerConnection> connections = new List<ClientToSuperPeerConnection>();

            foreach (ZoneSuperPeer peer in zone.SuperPeers)
            {
                foreach (ClientToSuperPeerConnection c in m_superPeerConnections)
                {
                    if (c.Connection.Connected == true &&
                        c.Connection.ConnectionEstablished == true && 
                        c.Connection.IPAddress == peer.ClientIPAddress &&
                        c.Connection.Port == peer.ClientListenPort)
                    {
                        connections.Add(c);
                    }
                }
            }

            return connections;
        }

        /// <summary>
        ///     Sends a packet to all super peers looking after a zone.
        /// </summary>
        /// <param name="zone">Zone to send packet to.</param>
        /// <param name="packet">Packet to send.</param>
        private void SendPacketToZoneSuperPeers(Zone zone, SuperPeerPacket packet)
        {
            foreach (ZoneSuperPeer peer in zone.SuperPeers)
            {
                if (!RegisteredToZoneSuperPeer(peer))
                {
                    continue;
                }

                foreach (ClientToSuperPeerConnection c in m_superPeerConnections)
                {
                    if (c.Connection.Connected == true &&
                        c.Connection.ConnectionEstablished == true &&
                        c.Connection.IPAddress == peer.ClientIPAddress &&
                        c.Connection.Port == peer.ClientListenPort)
                    {
                        packet.SuperPeerID = peer.ID;
                        c.Connection.SendPacket(packet);
                    }
                }
            }
        }

        /// <summary>
        ///     Sends a packet to all super peers looking after a zone and waits for their replies.
        /// </summary>
        /// <param name="zone">Zone to send packet to.</param>
        /// <param name="packet">Packet to send.</param>
        private void SendPacketToZoneSuperPeersWaitForReply(Zone zone, SuperPeerPacket packet)
        {
            foreach (ZoneSuperPeer peer in zone.SuperPeers)
            {
                if (!RegisteredToZoneSuperPeer(peer))
                {
                    continue;
                }

                foreach (ClientToSuperPeerConnection c in m_superPeerConnections)
                {
                    if (c.Connection.Connected == true &&
                        c.Connection.ConnectionEstablished == true && 
                        c.Connection.IPAddress == peer.ClientIPAddress &&
                        c.Connection.Port == peer.ClientListenPort)
                    {
                        packet.SuperPeerID = peer.ID;
                        c.Connection.SendPacketAndWaitAsync(packet).Wait();
                    }
                }
            }
        }

        /// <summary>
        ///     Unregisters with all super peers that are not relevant to the new zone the player is moving ot.
        /// </summary>
        /// <param name="zone">Zone that player is moving to.</param>
        private void UnregisterWithOldSuperPeers(Zone zone)
        {
            foreach (ZoneSuperPeer peer in m_registeredSuperPeers)
            {
                bool found = false;
                bool alreadyUnregistering = false;

                foreach (ZoneSuperPeer target_peer in zone.SuperPeers)
                {
                    if (peer.ID == target_peer.ID)
                    {
                        found = true;
                        break;
                    }
                }
                foreach (ZoneSuperPeer target_peer in m_unregisteringSuperPeers)
                {
                    if (peer.ID == target_peer.ID)
                    {
                        alreadyUnregistering = true;
                        break;
                    }
                }

                if (found == false && alreadyUnregistering == false)
                {
                    bool foundConnection = false;

                    // Send unregister packets to the super peer.
                    foreach (ClientToSuperPeerConnection connection in m_superPeerConnections)
                    {
                        if (connection.Connection.IPAddress == peer.ClientIPAddress &&
                            connection.Connection.Port == peer.ClientListenPort)
                        {
                            Logger.Info("Unregistering with SuperPeer #{0}.", LoggerVerboseLevel.High, peer.ID);

                            SuperPeerUnregisterPacket packet = new SuperPeerUnregisterPacket();
                            packet.SuperPeerID = peer.ID;
                            packet.ChangeZoneSuperPeerCount = m_changeZoneSuperPeerCount;
                            connection.Connection.SendPacket(packet);

                            foundConnection = true;
                        }
                    }

                    // Add to unregistering list.                    
                    m_registerEventList.Add("Unregistering with " + peer.ID);
                    m_unregisteringSuperPeers.Add(peer);
                    m_superPeersDirty = true;

                    if (foundConnection == false)
                    {
                        throw new InvalidOperationException("Fialed to find connection to unregister.");
                    }
                }
            }
        }

        /// <summary>
        ///     Registers with all super peers that are relevant to the new zone the player is moving ot.
        /// </summary>
        /// <param name="zone">Zone that player is moving to.</param>
        private void RegisterWithNewSuperPeers(Zone zone)
        {
            if (zone.SuperPeers.Count < m_settings.ZoneSuperPeerCount)
            {
                return;
            }

            foreach (ZoneSuperPeer peer in zone.SuperPeers)
            {
                bool alreadyRegistered = false;
                bool alreadyRegistering = false;

                foreach (ZoneSuperPeer target_peer in m_registeredSuperPeers)
                {
                    if (peer.ID == target_peer.ID)
                    {
                        alreadyRegistered = true;
                        break;
                    }
                }
                foreach (ZoneSuperPeer target_peer in m_registeringSuperPeers)
                {
                    if (peer.ID == target_peer.ID)
                    {
                        alreadyRegistering = true;
                        break;
                    }
                }

                if (alreadyRegistered == false && alreadyRegistering == false)
                {
                    bool found = false;

                    // Send register packets to the super peer.
                    foreach (ClientToSuperPeerConnection connection in m_superPeerConnections)
                    {
                        if (connection.Connection.Connected == true &&
                            connection.Connection.ConnectionEstablished == true &&
                            connection.Connection.IPAddress == peer.ClientIPAddress &&
                            connection.Connection.Port == peer.ClientListenPort)
                        {
                            Logger.Info("Registering with SuperPeer #{0}.", LoggerVerboseLevel.High, peer.ID);

                            SuperPeerRegisterPacket packet = new SuperPeerRegisterPacket();
                            packet.SuperPeerID = peer.ID;
                            packet.ClientID = m_clientID;

                            connection.Connection.SendPacket(packet);

                            found = true;
                        }
                    }

                    // Add to registering list.
                    if (found == true)
                    {
                        m_registeringSuperPeers.Add(peer);
                        m_superPeersDirty = true;
                        m_registerEventList.Add("Registering with " + peer.ID);
                    }
                }
            } 
        }

        /// <summary>
        ///     Connects to superpeers that are required for the new zone.
        /// </summary>
        /// <param name="zone">Zone that player is moving to.</param>
        private void ConnectToNewSuperPeers(Zone zone)
        {
            foreach (ZoneSuperPeer peer in zone.SuperPeers)
            {
                bool connected = false;

                // Are we connected to the peer hosting this super peer?
                foreach (ClientToSuperPeerConnection c in m_superPeerConnections)
                {
                    if (c.ClientID == peer.ClientID)
                    {
                        connected = true;
                        break;
                    }
                }

                if (connected == false)
                {
                    Logger.Info("Connecting to peer hosting SuperPeer #{0} for zone #{1} on {2}:{3}.", LoggerVerboseLevel.High, peer.ID, peer.ZoneID, peer.ClientIPAddress, peer.ClientListenPort);

                    // Connect to super peer!
                    Connection connection = new Connection();
                    connection.ConnectOverTime(peer.ClientIPAddress, (ushort)peer.ClientListenPort);

                    m_superPeerConnections.Add(new ClientToSuperPeerConnection(this, connection, peer.ClientID));
                }
            }
        }

        /// <summary>
        ///     Disconnects from any connections to superpeers that have died or are not in use.
        /// </summary>
        private void PruneDeadConnections()
        {
            foreach (ClientToSuperPeerConnection c in m_superPeerConnections.ToList())
            {
                if (c.Connection.Connected == false && 
                    c.Connection.Connecting == false &&
                    Environment.TickCount - c.Connection.DisconnectTimer > 1000)
                {
                    Logger.Warning("Lost connection to peer hosting SuperPeers on {0}:{1}.", LoggerVerboseLevel.High, c.Connection.IPAddress, c.Connection.Port);

                    m_registerEventList.Add("Found a disconnect to "+c.Connection.IPAddress+":"+c.Connection.Port);

                    // Look over both registered and registering list for superpeers to disconnect.
                    List<ZoneSuperPeer> list = m_registeredSuperPeers.ToList();
                    list.AddRange(m_registeringSuperPeers);

                    foreach (ZoneSuperPeer peer in list)
                    {
                        if (c.ClientID == peer.ClientID)
                        {
                            Logger.Info("Unregistering with SuperPeer #{0}.", LoggerVerboseLevel.High, peer.ID);

                            //System.Console.WriteLine("Removed Old:" + peer.ID+" - "+m_clientID);
                            m_registerEventList.Add("Disconencted from #" + peer.ID);

                            m_registeringSuperPeers.Remove(peer);
                            m_unregisteringSuperPeers.Remove(peer);
                            m_registeredSuperPeers.Remove(peer);
                        }
                    }

                    m_superPeersDirty = true;
                    m_superPeerConnections.Remove(c);
                }
            }
        }

        /// <summary>
        ///     Updates our connection to the zone we are currently inside of.
        /// </summary>
        private void UpdateZoneConnection()
        {
            if (m_account == null)
            {
                return;
            }

            // Work out what zone we are currently inside.
            Zone zone = m_zoneGrid.GetZoneByPosition(0, 0, m_settings.WorldWidth, m_settings.WorldHeight, (int)m_account.PeristentState.X, (int)m_account.PeristentState.Y);
            if (zone == null)
            {
                return;
            }

            // Different from the zone we are currently in?
            if (m_currentZone == null || zone.ID != m_currentZone.ID)
            {
                // Send a message to arbitrator saying we have changed zone.
                ChangeZonePacket packet = new ChangeZonePacket();
                packet.ZoneID = zone.ID;
                m_arbitratorConnection.SendPacket(packet);

                Logger.Info("Changed zone from {0} to {1}.", LoggerVerboseLevel.High, m_currentZone == null ? "null" : m_currentZone.ID.ToString(), zone.ID);

                // Count up how many super peers we were connected to when we switched. This is important
                // for unregistering, as if we are not connected to enough we can't persist our position.
                if (m_registeredSuperPeers.Count > 0)
                {
                    m_changeZoneSuperPeerCount = (m_registeredSuperPeers.Count);// + m_registeringSuperPeers.Count);
                }
                else
                {
                    m_changeZoneSuperPeerCount = 0;
                }

                // Store the zone we are now in.
                m_currentZone = zone;
            }

            // Unregister with superpeers we are no longer intrested in.
            UnregisterWithOldSuperPeers(zone);     

            // Register with superpeers we are intrested in, but only
            // do this after we have unregistered all superpeers we are not intrested in.
            if (m_unregisteringSuperPeers.Count <= 0)
            {
                RegisterWithNewSuperPeers(zone);
            }

            // Are we not connected to zone super peers?
            ConnectToNewSuperPeers(zone);

            // Prune dead connections.
            PruneDeadConnections();
        }

        #endregion
        #region Public Methods

        /// <summary>
        ///     Constructs an arbitrator with the given settings.
        /// </summary>
        /// <param name="settings">Settings used to construct arbitrator class.</param>
        /// <param name="host">Arbitratory host to connect to.</param>
        /// <param name="port">Arbitratory port to connect to.</param>
        public GameClientService(Settings settings, string host, ushort port)
        {
            m_settings          = settings;
            m_arbitratorHost    = host;
            m_arbitratorPort    = port;
        }

        /// <summary>
        ///     Finds a super peer instance that we are processing locally by its ID.
        /// </summary>
        /// <param name="id">ID of super peer to retrieve.</param>
        /// <returns>Super peer instance.</returns>
        public SuperPeer FindSuperPeerByID(int id)
        {
            foreach (SuperPeer peer in m_superPeers)
            {
                if (peer.ID == id)
                {
                    return peer;
                }
            }
            return null;
        }

        /// <summary>
        ///     Finds a zone super peer instance by its ID.
        /// </summary>
        /// <param name="id">ID of zone super peer to retrieve.</param>
        /// <returns>Zone super peer instance.</returns>
        public ZoneSuperPeer FindZoneSuperPeerByID(int id)
        {
            foreach (Zone zone in m_zoneGrid.Zones)
            {
                foreach (ZoneSuperPeer peer in zone.SuperPeers)
                {
                    if (peer.ID == id)
                    {
                        return peer;
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///     Returns true if we are registered to the given zone super peer.
        /// </summary>
        /// <param name="id">ID of zone super peer to check.</param>
        /// <returns>True if we are registered to it.</returns>
        public bool RegisteredToZoneSuperPeer(ZoneSuperPeer peer)
        {
            foreach (ZoneSuperPeer p in m_registeredSuperPeers)
            {
                if (p.ID == peer.ID)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Tells the arbitrator we are listening and ready to be used as a super peer.
        /// </summary>
        /// <returns>Result of registration attempt.</returns>
        public RegisterAsListeningResult RegisterAsListening()
        {
            if (m_listenConnection == null)
            {
                return RegisterAsListeningResult.Failed;
            }

            int port = m_listenConnection.Port;

            Logger.Info("Attempting to register as listening on port '{0}'.", LoggerVerboseLevel.High, port);

            RegisterAsListeningPacket request = new RegisterAsListeningPacket();
            request.Port = port;

            Task<Packet> task = m_arbitratorConnection.SendPacketAndWaitAsync(request);
            task.Wait(5000);

            RegisterAsListeningResultPacket reply = task.Result as RegisterAsListeningResultPacket;
            if (reply != null)
            {
                Logger.Info("Recieved result '{0}' when registering as listening.", LoggerVerboseLevel.High, reply.Result.ToString());
                return reply.Result;
            }

            Logger.Error("Failed to recieve valid reply when registering as listening.", LoggerVerboseLevel.Normal);
            return RegisterAsListeningResult.Failed;
        }

        /// <summary>
        ///     Asks the arbitrator to log us into the given account.
        /// </summary>
        /// <param name="username">Username of account.</param>
        /// <param name="password">Password of account.</param>
        /// <returns>Result of login attempt.</returns>
        public LoginResult Login(string username, string password)
        {
            if (m_arbitratorConnection == null)
            {
                return LoginResult.Failed;
            }

            Logger.Info("Attempting to login to account '{0}' with password '{1}'.", LoggerVerboseLevel.High, username, password);

            LoginPacket request = new LoginPacket();
            request.Username = username;
            request.Password = password;

            Task<Packet> task = m_arbitratorConnection.SendPacketAndWaitAsync(request);
            task.Wait(5000);

            LoginResultPacket reply = task.Result as LoginResultPacket;
            if (reply != null)
            {
                Logger.Info("Recieved result '{0}' when logging into account '{1}'.", LoggerVerboseLevel.High, reply.Result.ToString(), username);
                return reply.Result;
            }

            Logger.Error("Failed to recieve valid reply when logging in.", LoggerVerboseLevel.Normal);
            return LoginResult.Failed;
        }

        /// <summary>
        ///     Asks the arbitrator to create an account for us.
        /// </summary>
        /// <param name="username">Username of account.</param>
        /// <param name="password">Password of account.</param>
        /// <param name="email">Email address of account.</param>
        /// <returns>Result of account creation.</returns>
        public CreateAccountResult CreateAccount(string username, string password, string email)
        {
            if (m_arbitratorConnection == null)
            {
                return CreateAccountResult.Failed;
            }

            Logger.Info("Attempting to create an account '{0}' with password '{1}' and email '{2}'.", LoggerVerboseLevel.High, username, password, email);

            CreateAccountPacket request = new CreateAccountPacket();
            request.Username = username;
            request.Password = password;
            request.Email    = email;

            Task<Packet> task = m_arbitratorConnection.SendPacketAndWaitAsync(request);
            task.Wait(5000);

            CreateAccountResultPacket reply = task.Result as CreateAccountResultPacket;
            if (reply != null)
            {
                Logger.Info("Recieved result '{0}' when creating account '{1}'.", LoggerVerboseLevel.High, reply.Result.ToString(), username);
                return reply.Result;
            }

            Logger.Error("Failed to recieve valid reply when creating account.", LoggerVerboseLevel.Normal);
            return CreateAccountResult.Failed;
        }

        /// <summary>
        ///     Initializes this arbitrator, making initial connections to database and getting ready
        ///     to begin accepting connections.
        /// </summary>
        /// <returns>True if successful, otherwise false. If false is returned then the Run/Deinit methods are never called.</returns>
        public bool Initialize()
        {
            Logger.Info("Begining setup of game client ...", LoggerVerboseLevel.High);

            m_arbitratorConnection = null;

            // Setup connection.
            SetupArbitratorConnection().Wait();
            if (m_arbitratorConnection == null || m_arbitratorConnection.Connected == false)
            {
                Logger.Error("Failed to setup game client, could not connect to arbitrator.", LoggerVerboseLevel.Normal);
                return false;
            }

            // Setup listen connection.
            SetupListenConnection().Wait();
            if (m_listenConnection == null || m_listenConnection.Listening == false)
            {
                Logger.Error("Failed to setup game client, could not setup listen connection.", LoggerVerboseLevel.Normal);
                return false;
            }

            Logger.Info("Setup game client successfully.", LoggerVerboseLevel.High);

            return true;
        }

        /// <summary>
        ///     Returns true if connected to all super peers for the given zone.
        /// </summary>
        public bool ConnectedToAllZoneSuperPeers(Zone zone)
        {
            foreach (ZoneSuperPeer peer in zone.SuperPeers)
            {
                if (!RegisteredToZoneSuperPeer(peer))
                {
                    return false;
                }
                else
                {
                    bool found = false;

                    foreach (ClientToSuperPeerConnection c in m_superPeerConnections)
                    {
                        if (c.Connection.Connected == true &&
                            c.Connection.ConnectionEstablished == true &&
                            c.Connection.IPAddress == peer.ClientIPAddress &&
                            c.Connection.Port == peer.ClientListenPort)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found == false)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///     Updates the superpeer of our current zone with a new movement vector.
        /// </summary>
        /// <param name="x">X-Axis component of vector.</param>
        /// <param name="y">Y-Axis component of vector.</param>
        /// <param name="speed">Speed in pixels per second.</param>
        public void SetMovementVector(float x, float y, float speed)
        {
            // Check we are connected to all super peers.
            if (m_currentZone == null || !ConnectedToAllZoneSuperPeers(m_currentZone))
            {
                return;
            }

            SuperPeerSetMovementVectorPacket packet = new SuperPeerSetMovementVectorPacket();
            packet.VectorX = x;
            packet.VectorY = y;
            packet.Speed = speed;

            if (m_currentZone != null)
            {
                SendPacketToZoneSuperPeers(m_currentZone, packet);
            }
        }

        /// <summary>
        ///     Processes arbitrator packets.
        /// </summary>
        public void PollArbitrator()
        {
            if (m_arbitratorConnection != null)
            {
                Packet packet = m_arbitratorConnection.DequeuePacket();
                if (packet != null)
                {
                    ProcessArbitratorIncomingPacket(m_arbitratorConnection, packet);
                }
            }
        }

        /// <summary>
        ///     Polls the arbitrator, causing it to check for connection state changes and process incoming packets until it closes.
        /// </summary>
        /// <returns>Returs true of this client has been disconnected.</returns>
        public bool Poll()
        {
            // Lost connection? Reconnect it.
            if (m_arbitratorConnection.Connected == false)
            {
                return true;
            }
            if (m_arbitratorConnection == null)
            {
                SetupArbitratorConnection().Wait();
                if (m_arbitratorConnection == null || m_arbitratorConnection.Connected == false)
                {
                    return true;
                }
            }

            // Lost listen connection? Reconnect it.
            if (m_listenConnection == null || m_listenConnection.Listening == false)
            {
                SetupListenConnection().Wait();
                if (m_listenConnection == null || m_listenConnection.Listening == false)
                {
                    return true;
                }
            }

            // Process any incoming arbitrator packets.
            PollArbitrator();

            // Process any incoming packets from peers.
            if (m_listenConnection != null)
            {
                // Invoke events when for all newly connected peers.
                foreach (Connection connection in m_listenConnection.GetConnectedPeers())
                {
                    ProcessConnectedPeer(connection);
                }

                // Invoke events when for all newly disconnected peers.
                foreach (Connection connection in m_listenConnection.GetDisconnectedPeers())
                {
                    ProcessDisconnectedPeer(connection);
                }

                // Update all active connections.
                foreach (Connection connection in m_listenConnection.Peers)
                {
                    if (connection.MetaData != null)
                    {
                        ((GameClientPeer)connection.MetaData).Poll();
                    }
                }

                // Update all connections that have disconnected but are still doing things.
                foreach (Connection connection in m_disconnectedPeers.ToList())
                {
                    if (connection.MetaData != null)
                    {
                        ((GameClientPeer)connection.MetaData).Poll();
                    }
                    if (connection.MetaData == null ||
                        ((GameClientPeer)connection.MetaData).IsActive == false)
                    {
                        m_disconnectedPeers.Remove(connection);
                    }
                }
            }

            // Update SuperPeer instances.            
            foreach (SuperPeer peer in m_superPeers.ToList())
            {
                peer.Poll();

                // Remove this superpeer from the list if its no longer active, has no
                // registered peers and has been idle for a while.
                if (peer.IsActive == false)
                {
                    if (peer.RegisteredPeers.Count == 0)
                    {
                        if (peer.IdleTimer == 0)
                        {
                            peer.IdleTimer = Environment.TickCount;
                        }
                        else
                        {
                            if (Environment.TickCount - peer.IdleTimer > 10 * 1000)
                            {
                                m_superPeers.Remove(peer);
                            }
                        }
                    }
                }
            }

            // Update SuperPeer connections.
            foreach (ClientToSuperPeerConnection connection in m_superPeerConnections)
            {
                connection.Poll();
            }

            // Update zone connections.
            UpdateZoneConnection();

            return false;
        }

        /// <summary>
        ///     Deinitializes the master server, releasing resources and disconnecting from everything.
        /// </summary>
        public void Deinitialize()
        {
            Logger.Info("Begining cleanup of game client ...", LoggerVerboseLevel.High);

            // Save states of all super-peers. This is just being polite. If we disconnect
            // ungracefully the game will be fine, the peers will just have to wait a few seconds
            // until this peer is registered as disconnected and the other superpeers storage
            // requests are validated.
            foreach (SuperPeer peer in m_superPeers)
            {
                Zone zone = m_zoneGrid.GetZoneByID(peer.ZoneID);
                if (peer.IsActive == true)
                {
                    foreach (SuperPeerToClientConnection conn in peer.RegisteredPeers)
                    {
                        conn.StoreAccount("Deinitializing SuperPeer ID="+peer.ID+", Total SuperPeers=" + (zone != null ? zone.SuperPeers.Count.ToString() : "???"));
                    }
                }
            }

            // Send graceful disconnect message and wait till we get the reply before exiting.
            // This way we know the account stores above have gone through.
            GracefulDisconnectPacket p = new GracefulDisconnectPacket();
            m_arbitratorConnection.SendPacket(p);

            while (m_arbitratorConnection.Connected)
            {
                Packet packet = m_arbitratorConnection.DequeuePacket();
                if (packet != null &&
                    packet is GracefulDisconnectReplyPacket)
                {
                    break;
                }

                Thread.Sleep(100);
            }

            // Close down listening connection.
            if (m_listenConnection != null)
            {
                m_listenConnection.DisconnectAsync(false).Wait();
            }

            // Close down arbitrator connection.
            if (m_arbitratorConnection != null)
            {
                m_arbitratorConnection.DisconnectAsync(false).Wait();
            }

            // Dispose of settings class.
            m_settings = null;

            Logger.Info("Cleaned up game client successfully.", LoggerVerboseLevel.High);
        }

        #endregion
    }

}
