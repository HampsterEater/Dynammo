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
using System.Reflection;
using Dynammo.Common;
using Dynammo.Networking;

namespace Dynammo.Arbitrator
{

    /// <summary>
    ///     Primary class that deals with accepting and processing connections
    ///     by clients and coordinators.
    /// </summary>
    public class ArbitratorService 
    {
        #region Private Members

        // General constants.
        private const string LISTEN_HOST = "localhost";
        private const ushort LISTEN_PORT = 0; // Listening on port 0 causes the OS to allocate us the first free port.

        // Connection related variables.
        private Connection              m_listenConnection;
        private DBConnection            m_databaseConnection;

        private Settings                m_settings;
       
        private bool                    m_threadNameSet = false;

        // Registration settings.
        private int                     m_lastRegisterTime = Environment.TickCount;

        // Database settings.
        private long                    m_arbitratorDatabaseID = 0;
        private bool                    m_isMaster = false;
        private int                     m_lastMasterCheckTime = 0;
        private int                     m_lastZoneCheckTime = 0;
        
        // Serialization settings.
//        private int                     m_lastClientSerializeTime   = Environment.TickCount;
        private int                     m_lastStoreRequestCheckTime     = Environment.TickCount;
        private int                     m_oldAccountStorageRequestCount = 0;

        // Zone information.
        private ZoneGrid                m_zoneGrid = new ZoneGrid();

        #endregion
        #region Public Properties

        /// <summary>
        ///     Gets the number of bytes sent via this arbitrator since last call.
        /// </summary>
        public int DeltaBandwidthIn
        {
            get
            {
                int final = 0;

                if (m_listenConnection != null)
                {
                    final += m_listenConnection.DeltaBandwidthIn;
                }

                return final;
            }
        }

        /// <summary>
        ///     Gets the number of bytes recieved via this arbitrator since last call.
        /// </summary>
        public int DeltaBandwidthOut
        {
            get
            {
                int final = 0;

                if (m_listenConnection != null)
                {
                    final += m_listenConnection.DeltaBandwidthOut;
                }

                return final;
            }
        }

        /// <summary>
        ///     Gets the settings assigned to this arbitrator.
        /// </summary>
        public Settings Settings
        {
            get { return m_settings; }
        }

        /// <summary>
        ///     Gets the port this arbitrator is listening on.
        /// </summary>
        public ushort ListenPort
        {
            get
            {
                return m_listenConnection == null ? (ushort)0 : (ushort)m_listenConnection.Port;
            }
        }

        /// <summary>
        ///     Returns true if this arbitrator has begun listening yet.
        /// </summary>
        public bool IsListening
        {
            get
            {
                return m_listenConnection == null || m_listenConnection.ConnectionEndPoint == null ? false : m_listenConnection.Listening;
            }
        }

        /// <summary>
        ///     Returns the database connection used by this arbitrator.
        /// </summary>
        public DBConnection DatabaseConnection
        {
            get { return m_databaseConnection; }
        }

        /// <summary>
        ///     Returns the database ID of this arbitrator.
        /// </summary>
        public long DatabaseID
        {
            get { return m_arbitratorDatabaseID; }
        }

        /// <summary>
        ///     Returns true if this arbitrator is the master, or false if its a slave.
        /// </summary>
        public bool IsMaster
        {
            get { return m_isMaster; }
        }

        /// <summary>
        ///     Gets all the peers currently connected to this arbitrator.
        /// </summary>
        internal List<ArbitratorPeer> Peers
        {
            get 
            {
                List<ArbitratorPeer> peers = new List<ArbitratorPeer>();

                if (m_listenConnection != null)
                {
                    foreach (Connection c in m_listenConnection.Peers)
                    {
                        ArbitratorPeer metaData = c.MetaData as ArbitratorPeer;
                        if (metaData != null) 
                        {
                            peers.Add(metaData);
                        }
                    }
                }

                return peers;
            }
        }

        /// <summary>
        ///     Gets the zone grid representation that this arbitrator currently sees.
        /// </summary>
        internal ZoneGrid ZoneGrid
        {
            get { return m_zoneGrid; }
        }

        #endregion 
        #region Private Methods

        /// <summary>
        ///     Adds this arbitrator from the database list of "active" arbitrators.
        /// </summary>
        private void RegisterArbitrator()
        {
            if (IsListening == false)
            {
                return;
            }

            string local_ip   = HardwareHelper.GetLocalIPAddress();
            ushort local_port = ListenPort;

            // Delete old arbitrator entries.
            DBResults results = m_databaseConnection.Query(@"DELETE FROM {0} WHERE UNIX_TIMESTAMP()-last_active_timestamp > {1}",
                                                            Settings.DB_TABLE_ACTIVE_ARBITRATORS,
                                                            Settings.ARBITRATOR_REGISTER_TIMEOUT_INTERVAL / 1000);

            // Does row already exist.
            results = m_databaseConnection.Query(@"SELECT id FROM {0} WHERE ip_address='{1}' AND port={2}", 
                                                   Settings.DB_TABLE_ACTIVE_ARBITRATORS, 
                                                   local_ip, 
                                                   local_port);

            // Update current record.
            if (results != null && results.RowsAffected > 0)
            {
                m_arbitratorDatabaseID = (int)results[0]["id"];

                m_databaseConnection.Query(@"UPDATE {0} SET last_active_timestamp=UNIX_TIMESTAMP() WHERE id={1}",
                                            Settings.DB_TABLE_ACTIVE_ARBITRATORS,
                                            m_arbitratorDatabaseID);
            }

            // Insert new record.
            else
            {
                results = m_databaseConnection.Query(@"INSERT INTO {0}(ip_address, port, last_active_timestamp) VALUES ('{1}', {2}, UNIX_TIMESTAMP())", 
                                                      Settings.DB_TABLE_ACTIVE_ARBITRATORS,
                                                      local_ip,
                                                      local_port);
                if (results != null)
                {
                    m_arbitratorDatabaseID = results.LastInsertID;

                    if (m_threadNameSet == false)
                    {
                        System.Threading.Thread.CurrentThread.Name = "Arbitrator #" + m_arbitratorDatabaseID;
                        m_threadNameSet = true;
                    }
                }
            }

        }

        /// <summary>
        ///     Removes this arbitrator from the database list of "active" arbitrators.
        /// </summary>
        private void DeregisterArbitrator()
        {
            string local_ip   = HardwareHelper.GetLocalIPAddress();
            ushort local_port = ListenPort;

            // Remove arbitrator entry.
            m_databaseConnection.Query(@"DELETE FROM {0} WHERE ip_address='{1}' AND port={2}",
                                        Settings.DB_TABLE_ACTIVE_ARBITRATORS,
                                        local_ip,
                                        local_port);


            // Remove active clients.
            if (m_listenConnection != null)
            {
                foreach (Connection c in m_listenConnection.Peers)
                {
                    ArbitratorPeer metaData = c.MetaData as ArbitratorPeer;
                    if (metaData != null)
                    {
                        m_databaseConnection.Query(@"DELETE FROM {0} WHERE id={1}",
                                                    Settings.DB_TABLE_ACTIVE_CLIENTS,
                                                    metaData.DatabaseID);
                    }
                }
            }
        }

        /// <summary>
        ///     Serializes the clients persistent states to the database. 
        /// </summary>
        /*
        private void SerializeClientPersistentStates()
        {
            Logger.Info("Serializing client's persistent states to database.", LoggerVerboseLevel.Normal);
            foreach (Connection client in m_listenConnection.Peers)
            {
                ArbitratorPeer peer = client.MetaData as ArbitratorPeer;
                if (peer == null)
                {
                    continue;
                }

                peer.SerializePersistentStates();
            }
        }
        */

        /// <summary>
        ///     Checks with the database if we are current the master arbitrator or not, returns true if we are, else false.
        /// </summary>
        /// <returns>True if we are the master, false if we are a slave.</returns>
        private bool CheckIfIsMaster()
        {
            DBResults results = m_databaseConnection.Query(@"SELECT id FROM {0} ORDER BY id ASC LIMIT 1",
                                                            Settings.DB_TABLE_ACTIVE_ARBITRATORS,
                                                            m_arbitratorDatabaseID);

            return (int)results[0]["id"] == m_arbitratorDatabaseID;
        }

        /// <summary>
        ///     Load zone information from database.
        /// </summary>
        private void LoadZones()
        {
            // Load all zone settings from database.
            DBResults results = null;

            // We need to keep trying until we zones.
            // If we don't get them on the first try then the master arbitrator is most
            // likely about to insert them at the moment.
            int timeout = Environment.TickCount + Settings.ARBITRATOR_WAIT_FOR_ZONES_TIMEOUT_INTERVAL;
            while (true)
            {
                results = m_databaseConnection.Query(@"SELECT `id`, `child_zone_1_id`, `child_zone_2_id`, `parent_id`, `split_orientation` FROM {0}",
                                                      Settings.DB_TABLE_ZONES);

                if (results.RowsAffected > 0)
                {
                    break;
                }

                // We ensure there is a timeout here as its possible that the master arbitrator crashes before it 
                // inserts the the values and we have taken over control, in which case we would just keep looping
                // forever when our current settings are fine.
                if (Environment.TickCount > timeout)
                {
                    Logger.Error("Failed to load zones from database, has master arbitrator crashed?", LoggerVerboseLevel.Normal);
                    break;
                }
                if (CheckIfIsMaster() == true)
                {
                    Logger.Error("We have taken master control of arbitrators, ignoring loading of zones!", LoggerVerboseLevel.Normal);
                    break;

                }

                // Don't want to flood the DB do we?
                Thread.Sleep(1000);
            }

            // Clear out old zone grid.
            m_zoneGrid.Clear();

            // Load zones into grid.
            for (int i = 0; i < results.RowsAffected; i++)
            {
                int                  id                  = (int)results[i]["id"];
                int                  parent_id           = (int)results[i]["parent_id"];
                int                  child_zone_1_id     = (int)results[i]["child_zone_1_id"];
                int                  child_zone_2_id     = (int)results[i]["child_zone_2_id"];
                ZoneSplitOrientation split_orientation   = (ZoneSplitOrientation)((int)results[i]["split_orientation"]);

                m_zoneGrid.AddZone(new Zone(id, parent_id, child_zone_1_id, child_zone_2_id, split_orientation));
            }

            // Load super peer information.
            LoadZoneSuperPeers();
        }

        /// <summary>
        ///     Loads information about which super peers control which zones.
        /// </summary>
        private void LoadZoneSuperPeers()
        {
            DBResults results = null;

            results = m_databaseConnection.Query(@"SELECT 
                                                    peers.`id`, 
                                                    peers.`zone_id`, 
                                                    peers.`client_id`, 
                                                    clients.`ip_address`,
                                                    clients.`listen_port`                    
                                                   FROM 
                                                    {0} AS peers
                                                   JOIN
                                                    {1} AS clients
                                                   ON
                                                    peers.`client_id` = clients.`id`",
                                                  Settings.DB_TABLE_ZONE_SUPERPEERS,
                                                  Settings.DB_TABLE_ACTIVE_CLIENTS);

            for (int i = 0; i < results.RowsAffected; i++)
            {
                int id            = (int)results[i]["id"];
                int zone_id       = (int)results[i]["zone_id"];
                int client_id     = (int)results[i]["client_id"];
                string ip_address = results[i]["ip_address"].ToString();
                int listen_port   = (int)results[i]["listen_port"];

                Zone zone = m_zoneGrid.GetZoneByID(zone_id);
                if (zone == null)
                {
                    continue;
                }

                ZoneSuperPeer superPeer = new ZoneSuperPeer();
                superPeer.ID                = id;
                superPeer.ZoneID            = zone_id;
                superPeer.ClientID          = client_id;
                superPeer.ClientIPAddress   = ip_address;
                superPeer.ClientListenPort  = listen_port;

                //SuperPeerGainControl(zone, superPeer);
                zone.SuperPeers.Add(superPeer);
            }
        }

        /// <summary>
        ///     Saves zone information to database.
        /// </summary>
        internal void SaveZones()
        {
            // If there are no zones in our grid, then
            // create the root zone before saving.
            if (m_zoneGrid.Zones.Count <= 0)
            {
                Zone zone = AddZone(null, null, null, ZoneSplitOrientation.Horizontal);
                ZoningInformationChanged();
            }

            // Lock zone tables so nobody else attempts to access them while we are updating them.
            m_databaseConnection.Query("LOCK TABLES {0} WRITE, {1} WRITE",
                                       Settings.DB_TABLE_ZONES,
                                       Settings.DB_TABLE_ZONE_SUPERPEERS);

            // Remove old zones.
            DBResults zones = m_databaseConnection.Query(@"SELECT id FROM {0}",
                                                Settings.DB_TABLE_ZONES);
            for (int i = 0; i < zones.RowsAffected; i++)
            {
                int  zone_id = (int)zones[i]["id"];
                Zone zone    = m_zoneGrid.GetZoneByID(zone_id);

                if (zone == null)
                {
                    m_databaseConnection.Query(@"DELETE FROM {0} WHERE `id`={1}",
                                                Settings.DB_TABLE_ZONES,
                                                zone_id);
                    m_databaseConnection.Query(@"DELETE FROM {0} WHERE `zone_id`={1}",
                                                Settings.DB_TABLE_ZONE_SUPERPEERS,
                                                zone_id);

                    Logger.Info("Removed zone id={0}", LoggerVerboseLevel.High, zone_id);
                }
            }

            // Add new zones.
            foreach (Zone zone in m_zoneGrid.Zones)
            {
                DBResults results = m_databaseConnection.Query(@"SELECT id FROM {0} WHERE `id`={1}",
                                                                Settings.DB_TABLE_ZONES,
                                                                zone.ID);

                // Insert new zone.
                if (results.RowsAffected <= 0)
                {
                    results = m_databaseConnection.Query(@"INSERT INTO {0}(`child_zone_1_id`, `child_zone_2_id`, `parent_id`, `split_orientation`) VALUES({1}, {2}, {3}, {4})",
                                                         Settings.DB_TABLE_ZONES,
                                                         zone.ChildZone1ID,
                                                         zone.ChildZone2ID,
                                                         zone.ParentID,
                                                         (int)zone.SplitOrientation);

                    zone.ID = (int)results.LastInsertID;

                    // Update SuperPeer zone-id's
                    foreach (ZoneSuperPeer peer in zone.SuperPeers)
                    {
                        peer.ZoneID = zone.ID;
                    }

                    Logger.Info("Added new zone id={0}", LoggerVerboseLevel.High, zone.ID);
                }
            }

            // For any zones that we have new ID's for we need
            // to relink them (their parent zones need to have their ID's corrected).
            foreach (Zone zone in m_zoneGrid.Zones)
            {
                if (zone.ChildZone1 != null)
                {
                    zone.ChildZone1ID = zone.ChildZone1.ID;
                }
                if (zone.ChildZone2 != null)
                {
                    zone.ChildZone2ID = zone.ChildZone2.ID;
                }
                if (zone.Parent != null)
                {
                    zone.ParentID = zone.Parent.ID;
                }
            }

            // Re-add by ID into zone dictionary.
            Zone[] list = m_zoneGrid.Zones.ToArray();
            m_zoneGrid.Clear();
            foreach (Zone zone in list)
            {
                m_zoneGrid.AddZone(zone);
            }

            // Update old zones.
            foreach (Zone zone in m_zoneGrid.Zones)
            {
                DBResults results = m_databaseConnection.Query(@"SELECT id FROM {0} WHERE `id`={1}",
                                                                Settings.DB_TABLE_ZONES,
                                                                zone.ID);

                // Insert new zone.
                if (results.RowsAffected > 0)
                {
                    m_databaseConnection.Query(@"UPDATE {0} SET `child_zone_1_id`={1}, `child_zone_2_id`={2}, `parent_id`={3}, `split_orientation`={4} WHERE id={5}",
                                                Settings.DB_TABLE_ZONES,
                                                zone.ChildZone1ID,
                                                zone.ChildZone2ID,
                                                zone.ParentID,
                                                (int)zone.SplitOrientation,
                                                zone.ID);
                }
            }

            // Save super peer information.
            SaveZoneSuperPeers();

            // Unlock all tables.
            m_databaseConnection.Query("UNLOCK TABLES");
        }

        /// <summary>
        ///     Saves super peer information into the database.
        /// </summary>
        private void SaveZoneSuperPeers()
        {
            List<ZoneSuperPeer> current_peers = new List<ZoneSuperPeer>();

            // Make a list of all current super peers.
            foreach (Zone zone in m_zoneGrid.Zones)
            {
                current_peers.AddRange(zone.SuperPeers);
            }

            // Add/Update all current super peers.
            foreach (ZoneSuperPeer peer in current_peers)
            {
                DBResults results = m_databaseConnection.Query(@"SELECT id FROM {0} WHERE `id`={1}",
                                                                Settings.DB_TABLE_ZONE_SUPERPEERS,
                                                                peer.ID);

                // Update old super peer.
                if (results.RowsAffected > 0)
                {
                    m_databaseConnection.Query(@"UPDATE {0} SET 
                                                        `zone_id`   = {1},
                                                        `client_id` = {2}
                                                     WHERE id={3}",
                                                Settings.DB_TABLE_ZONE_SUPERPEERS,
                                                peer.ZoneID,
                                                peer.ClientID,
                                                peer.ID);
                }

                // Insert new super peer.
                else
                {
                    results = m_databaseConnection.Query(@"INSERT INTO {0}
                                                                (
                                                                    `zone_id`,
                                                                    `client_id`
                                                                ) 
                                                                VALUES
                                                                (
                                                                    {1},
                                                                    {2}
                                                                )",
                                                         Settings.DB_TABLE_ZONE_SUPERPEERS,
                                                         peer.ZoneID,
                                                         peer.ClientID);

                    peer.ID = (int)results.LastInsertID;

                    Logger.Info("Added new SuperPeer to database. id={0} zone_id={1}, client_id={2}", LoggerVerboseLevel.High, peer.ID, peer.ZoneID, peer.ClientID);
                }
            }

            // Remove SuperPeers from database that no longer are in use.
            DBResults old_superpeers = m_databaseConnection.Query(@"SELECT id, zone_id, client_id FROM {0}",
                                                                    Settings.DB_TABLE_ZONE_SUPERPEERS);

            for (int i = 0; i < old_superpeers.RowsAffected; i++)
            {
                int super_peer_id        = (int)old_superpeers[i]["id"];
                int super_peer_zone_id   = (int)old_superpeers[i]["zone_id"];
                int super_peer_client_id = (int)old_superpeers[i]["client_id"];
                bool found               = false;
                
                foreach (ZoneSuperPeer peer in current_peers)
                {
                    if (peer.ID == super_peer_id)
                    {
                        found = true;
                        break;
                    }
                }

                if (found == false)
                {
                    m_databaseConnection.Query(@"DELETE FROM {0} WHERE `id`={1}",
                                                 Settings.DB_TABLE_ZONE_SUPERPEERS,
                                                 super_peer_id);

                    Logger.Info("Removed old SuperPeer from database. id={0}", LoggerVerboseLevel.High, super_peer_id);          
                }
            }
        }

        /// <summary>
        ///     Adds a new zone to the grid with the given settings. The zone
        ///     is not propogated until SaveZones() is called.
        /// </summary>
        /// <param name="child_1_zone">Child zone one if split, null if not split.</param>
        /// <param name="child_2_zone">Child zone two if split, null if not split.</param>
        /// <param name="split">Split type of zone.</param>
        /// <returns>Returns the zone that was added.</returns>
        private Zone AddZone(Zone parentZone, Zone child_1_zone, Zone child_2_zone, ZoneSplitOrientation split)
        {
            Zone zone = new Zone(0, 0, 0, 0, split);
            zone.ChildZone1 = child_1_zone;
            zone.ChildZone2 = child_2_zone;
            zone.Parent     = parentZone;

            if (zone.ChildZone1 != null)
            {
                zone.ChildZone1ID = zone.ChildZone1.ID;
            }
            if (zone.ChildZone2 != null)
            {
                zone.ChildZone2ID = zone.ChildZone2.ID;
            }
            if (zone.Parent != null)
            {
                zone.ParentID = zone.Parent.ID;
            }

            // We need to generate a unique ID for this zone.
            // This will get overriden when we commit to the database, but it allows us
            // to differentiate until then.
            int id = int.MinValue;
            while (true)
            {
                if (m_zoneGrid.GetZoneByID(id) == null)
                {
                    break;
                }
                id++;
            }
            zone.ID = id;

            m_zoneGrid.AddZone(zone);

            return zone;
        }

        /// <summary>
        ///     Removes a zone from the grid. Changes are applied immediately.
        /// </summary>
        /// <param name="zone">Zone to remove.</param>
        private void DeleteZone(Zone zone)
        {
            // Remove from zone grid.
            m_zoneGrid.RemoveZone(zone);

            // Loose control of all super peers.
            foreach (ZoneSuperPeer peer in zone.SuperPeers.ToList())
            {
                SuperPeerLossControl(zone, peer.ClientID);
            }
        }

        /// <summary>
        ///     Saves replicated settings from database.
        /// </summary>
        private void SaveReplicatedSettings()
        {
            Type type = m_settings.GetType();
            foreach (PropertyInfo property in type.GetProperties())
            {
                if (property.GetCustomAttribute<MasterToSlaveReplicatedAttribute>() != null)
                {
                    string name = property.Name;
                    object val = property.GetValue(m_settings);

                    DBResults results = m_databaseConnection.Query(@"SELECT id FROM {0} WHERE `name`='{1}'",
                                                                   Settings.DB_TABLE_REPLICATED_SETTINGS,
                                                                   name);

                    // Update old setting.
                    if (results.RowsAffected > 0)
                    {
                        m_databaseConnection.Query(@"UPDATE {0} SET `value`='{1}' WHERE id={2}",
                                                    Settings.DB_TABLE_REPLICATED_SETTINGS,
                                                    val,
                                                    results[0]["id"].ToString());
                    }

                    // Insert new setting.
                    else
                    {
                        m_databaseConnection.Query(@"INSERT INTO {0}(`name`, `value`) VALUES('{1}', '{2}')",
                                                    Settings.DB_TABLE_REPLICATED_SETTINGS,
                                                    name,
                                                    val);
                    }
                }
            }
        }

        /// <summary>
        ///     Loads replicated settings from database.
        /// </summary>
        private void LoadReplicatedSettings()
        {
            // Load all arbitrator settings from database.
            DBResults results = null;

            // We need to keep trying until we get the settings.
            // If we don't get them on the first try then the master arbitrator is most
            // likely about to insert them at the moment.
            int timeout = Environment.TickCount + Settings.ARBITRATOR_WAIT_FOR_SETTINGS_TIMEOUT_INTERVAL;
            while (true)
            {
                results = m_databaseConnection.Query(@"SELECT `name`, `value` FROM {0}",
                                                      Settings.DB_TABLE_REPLICATED_SETTINGS);

                if (results.RowsAffected > 0)
                {
                    break;
                }

                // We ensure there is a timeout here as its possible that the master arbitrator crashes before it 
                // inserts the the values and we have taken over control, in which case we would just keep looping
                // forever when our current settings are fine.
                if (Environment.TickCount > timeout)
                {
                    Logger.Error("Failed to load settings from database, has master arbitrator crashed?", LoggerVerboseLevel.Normal);
                    break;
                }
                if (CheckIfIsMaster() == true)
                {
                    Logger.Error("We have taken master control of arbitrators, ignoring replicated settings!", LoggerVerboseLevel.Normal);
                    break;

                }

                // Don't want to flood the DB do we?
                Thread.Sleep(1000);
            }

            // Load settings in.
            Type type = m_settings.GetType();
            for (int i = 0; i < results.RowsAffected; i++)
            {
                string name = results[i]["name"].ToString();
                string value = results[i]["value"].ToString();

                PropertyInfo info = type.GetProperty(name);
                if (info != null)
                {
                    Type propertyType = info.PropertyType;
                    object obj = StringHelper.CastStringToType(value, propertyType);

                    if (obj == null)
                    {
                        Logger.Error("Could not load setting '{0}', type '{1}' not supported.", LoggerVerboseLevel.Normal, name, propertyType.Name);
                    }
                    else
                    {
                        info.SetValue(m_settings, obj);
                    }
                }
            }
        }

        /// <summary>
        ///     Reloads zoning information from the database if it has changed.
        /// </summary>
        private void RefreshZoningInformation()
        {
            // Check if things have changed yet.
            DBResults results = m_databaseConnection.Query(@"SELECT `value` FROM {0} WHERE `name`='{1}'",
                                                            Settings.DB_TABLE_REPLICATED_SETTINGS,
                                                            "ZoneInformationLastModified");
            if (results.RowsAffected <= 0 ||
                int.Parse(results[0]["value"].ToString()) == m_settings.ZoneInformationLastModified)
            {
                return;
            }
       
            // Store last modified time.
            m_settings.ZoneInformationLastModified = int.Parse(results[0]["value"].ToString());

            // Load zoning information.
            LoadZones();

            // Send zones to peers.
            BroadcastZoneInformation();
        }

        /// <summary>
        ///     Broadcasts changed zoning information to all peers.
        /// </summary>
        internal void BroadcastZoneInformation()
        {
            // Broadcast changed world grid information to peers.
            foreach (ArbitratorPeer peer in Peers)
            {
                peer.SendUpdatedWorldGrid();
            }

            // Loooooooogs
            Logger.Info("Broadcasting updated world grid to peers because zone information has been modified. Zones last changed at " + m_settings.ZoneInformationLastModified, LoggerVerboseLevel.High);
        }

        /// <summary>
        ///     Invoked when zoning information has changed.
        /// </summary>
        internal void ZoningInformationChanged()
        {
            if (IsMaster == true)
            {
                m_settings.ZoneInformationLastModified = Environment.TickCount;
                SaveReplicatedSettings();

                UpdateZoning();
                BroadcastZoneInformation();
            }
        }

        /// <summary>
        ///     Invoked when we loose master arbitrator control, or when we initial start up
        ///     and find we are not the master arbitrator.
        /// </summary>
        private void LostMasterControl()
        {
            // Load replicated settings.
            LoadReplicatedSettings();

            // Load zones.
            LoadZones();

            Logger.Info("Now a slave arbitrator.", LoggerVerboseLevel.High);
        }

        /// <summary>
        ///     Invoked when we gain arbitrator control, or when we initial start up
        ///     and find we are the master arbitrator.
        /// </summary>
        private void GainedMasterControl()
        {
            // Save all arbitrator settings into database.
            SaveReplicatedSettings();

            // Save zones.
            SaveZones();

            // Mark zoning information as changed.
            ZoningInformationChanged();

            Logger.Info("Now the master arbitrator.", LoggerVerboseLevel.High);
        }

        /// <summary>
        ///     Invoked to given a client super-peer level control over a zone.
        /// </summary>
        /// <param name="zone">Zone to give control over.</param>
        /// <param name="client_id">ID of client to be given control.</param>
        internal void SuperPeerGainControl(Zone zone, int client_id)
        {
            if (m_isMaster == false)
            {
                return;
            }

            // Check zone is valid.
            if (zone == null)
            {
                return;
            }

            // See if we already have control.
            foreach (ZoneSuperPeer peer in zone.SuperPeers)
            {
                if (peer.ClientID == client_id)
                {
                    return;
                }
            }

            // Give client control.
            ZoneSuperPeer new_peer  = new ZoneSuperPeer();
            new_peer.ClientID       = client_id;
            new_peer.ZoneID         = zone.ID;

            // Get connection address for client.
            DBResults results = m_databaseConnection.Query(@"SELECT 
                                                            `ip_address`,
                                                            `listen_port`                    
                                                           FROM 
                                                            {0} 
                                                           WHERE
                                                            `id`={1}",
                                                          Settings.DB_TABLE_ACTIVE_CLIENTS,
                                                          client_id);

            if (results.RowsAffected > 0)
            {
                new_peer.ClientIPAddress  = results[0]["ip_address"].ToString();
                new_peer.ClientListenPort = (int)results[0]["listen_port"];
            }
            else
            {
                Logger.Error("Attempt to give control of zone (id={1}) to client that dosen't exist in database (id={0}).", LoggerVerboseLevel.Normal, client_id, zone.ID);
            }

            zone.SuperPeers.Add(new_peer);

            Logger.Info("Client #{0} has taken control of Zone #{1}. {2} SuperPeers now controlling zone.", LoggerVerboseLevel.High, client_id, zone.ID, zone.SuperPeers.Count);
        }

        /// <summary>
        ///     Invoked to take a clients super-peer level control over a zone.
        /// </summary>
        /// <param name="zone">Zone to have control.</param>
        /// <param name="client_id">ID of client to have control taken.</param>
        internal void SuperPeerLossControl(Zone zone, int client_id)
        {
            if (m_isMaster == false)
            {
                return;
            }

            // Check zone is valid.
            if (zone == null)
            {
                return;
            }

            // See if we already have control.
            bool found = false;
            foreach (ZoneSuperPeer peer in zone.SuperPeers.ToList())
            {
                if (peer.ClientID == client_id)
                {
                    zone.SuperPeers.Remove(peer);

                    found = true;
                }
            }

            if (found == true)
            {
                Logger.Warning("Client #{0} has lost control of Zone #{1}. {2} SuperPeers now controlling zone.", LoggerVerboseLevel.High, client_id, zone.ID, zone.SuperPeers.Count);
            }
        }

        /// <summary>
        ///     Updates zoning information. Splitting zones and reintegrating them as peers move around.
        /// </summary>
        internal void UpdateZoning()
        {
            if (m_isMaster == false)
            {
                return;
            }

            List<Zone> overpopulatedZones = new List<Zone>();
            List<Zone> underpopulatedZones = new List<Zone>();

            // Get client/zone information.
            DBResults active_clients = m_databaseConnection.Query(@"SELECT `id`, `zone_id` FROM {0} WHERE `account_id`!=0",
                                                                  Settings.DB_TABLE_ACTIVE_CLIENTS);
            DBResults superpeers     = m_databaseConnection.Query(@"SELECT `id`, `zone_id`, `client_id` FROM {0}",
                                                                  Settings.DB_TABLE_ZONE_SUPERPEERS);

            // Remove super peers from clients that are no longer active.
            bool superPeersChanged = false;

            for (int i = 0; i < superpeers.RowsAffected; i++)
            {
                DBRow superPeer = superpeers[i];
                bool foundClient = false;
                
                for (int k = 0; k < active_clients.RowsAffected; k++)
                {
                    DBRow client = active_clients[k];
                    if ((int)client["id"] == (int)superPeer["client_id"])
                    {
                        foundClient = true;
                        break;
                    }
                }

                // Client no longer active? ABORT ABORT!
                if (foundClient == false)
                {
                    Zone zone = m_zoneGrid.GetZoneByID((int)superPeer["zone_id"]);
                    SuperPeerLossControl(zone, (int)superPeer["client_id"]);
                    superPeersChanged = true;
                }
            }

            // If we have changed superpeers, restart zone updating from the beginning.
            if (superPeersChanged == true)
            {
                SaveZones();
                ZoningInformationChanged();
                UpdateZoning();

                return;
            }

            // Look through zones that need super peer changes.
            foreach (Zone zone in m_zoneGrid.Zones)
            {
                if (zone.IsLeafZone == false)
                {
                    continue;
                }

                // Calculate zone boundry.
                int zone_x = 0, zone_y = 0;
                int zone_w = 0, zone_h = 0;
                m_zoneGrid.CalculateZoneBounds(zone, 0, 0, m_settings.WorldWidth, m_settings.WorldHeight, out zone_x, out zone_y, out zone_w, out zone_h);

                // Find peers inside zone.
                int peersInZone = 0;
                for (int i = 0; i < active_clients.RowsAffected; i++)
                {
                    if ((int)active_clients[i]["zone_id"] == zone.ID)
                    {
                        peersInZone++;
                    }
                }

                // Find superpeers for zone.
                int superPeersInZone = 0;
                for (int i = 0; i < superpeers.RowsAffected; i++)
                {
                    if ((int)superpeers[i]["zone_id"] == zone.ID)
                    {
                        superPeersInZone++;
                    }
                }

                // Is this zone overpopulated?
                if (peersInZone >= m_settings.ZoneOverpopulationThreshold)
                {
                    overpopulatedZones.Add(zone);
                }

                // Is this zone underpopulated?
                if (peersInZone <= m_settings.ZoneUnderpopulationThreshold && zone != m_zoneGrid.RootZone)
                {
                    int child_1_peers = 0;
                    int child_2_peers = 0;

                    if (zone.Parent != null &&
                        zone.Parent.ChildZone1 != null &&
                        zone.Parent.ChildZone2 != null &&
                        zone.Parent.ChildZone1.ChildZone1 == null &&
                        zone.Parent.ChildZone1.ChildZone2 == null &&
                        zone.Parent.ChildZone2.ChildZone1 == null &&
                        zone.Parent.ChildZone2.ChildZone2 == null)
                    {
                        // Make sure that merging these two zones together dosen't go over the overpopulation threshold.
                        // Otherwise we will end up going round in circles.
                        for (int i = 0; i < active_clients.RowsAffected; i++)
                        {
                            if ((int)active_clients[i]["zone_id"] == zone.Parent.ChildZone1ID)
                            {
                                child_1_peers++;
                            }
                            if ((int)active_clients[i]["zone_id"] == zone.Parent.ChildZone2ID)
                            {
                                child_2_peers++;
                            }
                        }

                        if (child_1_peers + child_2_peers < m_settings.ZoneOverpopulationThreshold)
                        {
                            underpopulatedZones.Add(zone);
                        }
                    }
                }
            }

            // Work out if any peers are in non-leaf zones, if they are
            // then we need to wait until they migrate to their new zones.
            int migratingPeerCount = 0;
            for (int i = 0; i < active_clients.RowsAffected; i++)
            {
                int zone_id = (int)active_clients[i]["zone_id"];
                Zone zone = m_zoneGrid.GetZoneByID(zone_id);

                if ((zone == null && zone_id != 0) ||
                    (zone != null && zone.IsLeafZone == false))
                {
                    migratingPeerCount += 1;
                }
            }

            // Split zones that require it.
            if (overpopulatedZones.Count > 0 && migratingPeerCount <= 0)
            {
                SplitZones(overpopulatedZones);
            }

            // Merge zones that require it.
            if (underpopulatedZones.Count > 0 && migratingPeerCount <= 0)
            {
                MergeZones(underpopulatedZones);
            }

            // Allocate super peers for zones that require it.
            AllocateSuperPeersToZones();
        }

        /// <summary>
        ///     Gets the ID for the least burdended client (the one that has been assigned the least number of zones to control).
        /// </summary>
        /// <param name="zone">Zone capable of being overtaken by client.</param>
        /// <returns>Client ID of least burdended client, or below or equal to 0 if none found.</returns>
        private int GetLeastBurdenedClientID(Zone zone)
        {
            // Find lowest burdened client.
            DBResults results = m_databaseConnection.Query(@"SELECT 
                                                                a.id,
                                                                (
                                                                    SELECT 
                                                                        COUNT(*)
                                                                    FROM
                                                                        {0} AS c
                                                                    WHERE
                                                                        c.client_id = a.id
                                                                ) AS controlled_zone_count,
                                                                (
                                                                    SELECT 
                                                                        COUNT(*)
                                                                    FROM
                                                                        {0} AS c
                                                                    WHERE
                                                                        c.client_id = a.id &&
                                                                        c.zone_id   = {2}
                                                                ) AS this_zone_count,
                                                                listening,
                                                                account_id
                                                            FROM 
                                                                {1} AS a
                                                            ORDER BY
                                                                controlled_zone_count ASC,
                                                                last_active_timestamp DESC
                                                                ",
                                                Settings.DB_TABLE_ZONE_SUPERPEERS,
                                                Settings.DB_TABLE_ACTIVE_CLIENTS,
                                                zone.ID);

            if (results.RowsAffected > 0)
            {
                for (int i = 0; i < results.RowsAffected; i++)
                {
                    if ((long)results[i]["this_zone_count"] <= 0 &&
                        (results[i]["listening"] != null && (int)results[i]["listening"] > 0) &&
                        (results[i]["account_id"] != null && (int)results[i]["account_id"] > 0))
                    {
                        return (int)results[i]["id"];
                    }
                }
            }
            
            return 0;
        }

        /// <summary>
        ///     Performed by the master arbitrator - assigns different peers as SuperPeers for individual zones.
        /// </summary>
        private void AllocateSuperPeersToZones()
        {
            if (m_isMaster == false)
            {
                return;
            }

            bool changesMade = false;

            // Get client/zone information.
            DBResults active_clients           = m_databaseConnection.Query(@"SELECT `id`, `zone_id` FROM {0}",
                                                                  Settings.DB_TABLE_ACTIVE_CLIENTS);
            DBResults superpeers               = m_databaseConnection.Query(@"SELECT `id`, `zone_id`, `client_id` FROM {0}",
                                                                  Settings.DB_TABLE_ZONE_SUPERPEERS);
            DBResults pending_storage_requests = m_databaseConnection.Query(@"SELECT `zone_id` FROM {0}",
                                                                  Settings.DB_TABLE_PENDING_STORE_REQUESTS);

            // Look through zones that need super peer changes.
            foreach (Zone zone in m_zoneGrid.Zones)
            {
                if (zone.IsLeafZone == false)
                {
                    continue;
                }

                // We cannot allocate superpeers to this zone until storage requests for the zone are completed.
                // We do this so that new superpeers recieve the most updated state of a player.
                int pendingStorageRequestCount = 0;
                for (int i = 0; i < pending_storage_requests.RowsAffected; i++)
                {
                    if ((int)pending_storage_requests[i]["zone_id"] == zone.ID)
                    {
                        pendingStorageRequestCount++;
                    }
                }

                // Calculate zone boundry.
                int zone_x = 0, zone_y = 0;
                int zone_w = 0, zone_h = 0;
                m_zoneGrid.CalculateZoneBounds(zone, 0, 0, m_settings.WorldWidth, m_settings.WorldHeight, out zone_x, out zone_y, out zone_w, out zone_h);

                // Find peers inside zone.
                int peersInZone = 0;
                for (int i = 0; i < active_clients.RowsAffected; i++)
                {
                    if ((int)active_clients[i]["zone_id"] == zone.ID)
                    {
                        peersInZone++;
                    }
                }

                // Find superpeers for zone.
                int superPeersInZone = 0;
                for (int i = 0; i < superpeers.RowsAffected; i++)
                {
                    if ((int)superpeers[i]["zone_id"] == zone.ID)
                    {
                        superPeersInZone++;
                    }
                }

                // Zone requires more superpeers?
                //peersInZone >= 1 && 
                if (zone.SuperPeers.Count < m_settings.ZoneSuperPeerCount)
                {
                    if (pendingStorageRequestCount > 0)
                    {
                        //System.Console.WriteLine("Cannot append superpeers to zone " + zone.ID + ", pending storage requests - peers=" + zone.SuperPeers.Count + " requests=" + pendingStorageRequestCount + ".");
                        continue;
                    }

                    int client_id = GetLeastBurdenedClientID(zone);

                    if (client_id != 0)
                    {
                        SuperPeerGainControl(zone, client_id);

                        changesMade = true;
                    }
                }
            }

            // Save changes.
            if (changesMade == true)
            {
                SaveZones();
                ZoningInformationChanged();
            }
        }

        /// <summary>
        ///     Performed by the master arbitrator - attempts to split zones that are overpopulated.
        /// </summary>
        /// <param name="zones">Zones to split.</param>
        private void SplitZones(List<Zone> zones)
        {
            if (m_isMaster == false)
            {
                return;
            }

            bool zonesChanged = false;

            // Split zone into parts.
            foreach (Zone zone in zones)
            {
                // Dafuq? This zone is already split!
                if (zone.ChildZone1 != null ||
                    zone.ChildZone2 != null)
                {
                    continue;
                }

                Zone child_1 = AddZone(zone, null, null, ZoneSplitOrientation.Horizontal);
                Zone child_2 = AddZone(zone, null, null, ZoneSplitOrientation.Horizontal);
                zone.ChildZone1 = child_1;
                zone.ChildZone2 = child_2;

                // Work out split direction based on depth.
                int  depth  = 0;
                Zone parent = zone.Parent;

                while (parent != null)
                {
                    depth++;
                    parent = parent.Parent;    
                }

                // Split direction alternates between depths.
                if (depth % 2 == 0)
                {
                    zone.SplitOrientation = ZoneSplitOrientation.Vertical;
                }
                else
                {
                    zone.SplitOrientation = ZoneSplitOrientation.Horizontal;
                }

                // Super peers from original zone get control of left-zone, right 
                // zone is to-be-populated.
                foreach (ZoneSuperPeer peer in zone.SuperPeers.ToList())
                {
                    SuperPeerLossControl(zone, peer.ClientID);
                   // SuperPeerGainControl(child_1, peer.ClientID);
                }

                Logger.Info("Zone #{0} is overpopulated and was split.", LoggerVerboseLevel.High, zone.ID);
                zonesChanged = true;
            }

            if (zonesChanged == true)
            {
                AllocateSuperPeersToZones();
                SaveZones();
                ZoningInformationChanged();
            }
        }

        /// <summary>
        ///     Performed by the master arbitrator - attempts to merge zones that are overpopulated.
        /// </summary>
        /// <param name="zones">Zones to merge.</param>
        private void MergeZones(List<Zone> zones)
        {
            if (m_isMaster == false)
            {
                return;
            }

            bool zonesChanged = false;

            foreach (Zone zone in zones)
            {
                if (zone.Parent == null ||
                    zone.Parent.ChildZone1 == null ||
                    zone.Parent.ChildZone2 == null)
                {
                    continue;
                }

                // Check both zones of the parent are ready to be merged.
                if (zones.Contains(zone.Parent.ChildZone1) &&
                    zones.Contains(zone.Parent.ChildZone2))
                {
                    Logger.Info("Zone #{0} and Zone #{1} are underpopulated and were merged.", LoggerVerboseLevel.High, zone.Parent.ChildZone1.ID, zone.Parent.ChildZone2.ID);

                    // Clear super peers.
                    List<ZoneSuperPeer> transferedSuperPeers = new List<ZoneSuperPeer>(zone.Parent.ChildZone1.SuperPeers);

                    // Loose control of all super peers.
                    foreach (ZoneSuperPeer peer in zone.Parent.SuperPeers.ToList())
                    {
                        SuperPeerLossControl(zone.Parent, peer.ClientID);
                    }
                    foreach (ZoneSuperPeer peer in zone.Parent.ChildZone1.SuperPeers.ToList())
                    {
                        SuperPeerLossControl(zone.Parent.ChildZone1, peer.ClientID);
                    }
                    foreach (ZoneSuperPeer peer in zone.Parent.ChildZone2.SuperPeers.ToList())
                    {
                        SuperPeerLossControl(zone.Parent.ChildZone2, peer.ClientID);
                    }

                    // Delete child zones.
                    DeleteZone(zone.Parent.ChildZone1);
                    DeleteZone(zone.Parent.ChildZone2);
                    zone.Parent.SuperPeers.Clear();

                    // Super peers from left-child-zone get to control the new zone. The right-child-zone
                    // super peers get to gtfo.
                   /* foreach (ZoneSuperPeer peer in transferedSuperPeers)
                    {
                        peer.ZoneID = zone.Parent.ID;

                        SuperPeerGainControl(zone.Parent, peer.ClientID);
                    }
                    */

                    zonesChanged = true;
                }
            }

            if (zonesChanged == true)
            {
                AllocateSuperPeersToZones();
                SaveZones();
                ZoningInformationChanged();
            }
        }

        /// <summary>
        ///     Sets up the listen connection to accept incoming connections from peers.
        /// </summary>
        /// <returns>Returns true if successful, else false.</returns>
        private async Task<bool> SetupConnection()
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
        ///     Sets up the database connection.
        /// </summary>
        /// <returns>Returns true if successful, else false.</returns>
        private async Task<bool> SetupDBConnection()
        {
            bool result = false;

            if (m_databaseConnection == null)
            {
                Logger.Info("Attempting to connect to database on " + m_settings.DatabaseHostname + ":" + m_settings.DatabasePort + " ...", LoggerVerboseLevel.High);
                m_databaseConnection = new DBConnection();

                result = await m_databaseConnection.ConnectAsync(m_settings.DatabaseHostname, m_settings.DatabasePort, m_settings.DatabaseName, m_settings.DatabaseUsername, m_settings.DatabasePassword);
            }
            else
            {
                Logger.Error("Lost database connection, attempting to setup connection again ...", LoggerVerboseLevel.Normal);
                result = await m_databaseConnection.ReconnectAsync();
            }

            if (result == false)
            {
                Logger.Error("Failed to connect to database on " + m_settings.DatabaseHostname + ":" + m_settings.DatabasePort, LoggerVerboseLevel.Normal);
            }
            else
            {
                Logger.Info("Successfully connected to database.", LoggerVerboseLevel.High);
            }

            return result;
        }

        /// <summary>
        ///     Invoked when a new peer connects.
        /// </summary>
        /// <param name="peer">Peers connection.</param>
        private void ProcessConnectedPeer(Connection peer)
        {
            ArbitratorPeer peerMetaData = new ArbitratorPeer(this, peer);
            peerMetaData.Connected();

            peer.MetaData = peerMetaData;
        }

        /// <summary>
        ///     Invoked when a new peer disconnects.
        /// </summary>
        /// <param name="peer">Peers connection.</param>
        private void ProcessDisconnectedPeer(Connection peer)
        {
            ArbitratorPeer peerMetaData = peer.MetaData as ArbitratorPeer;
            if (peerMetaData == null)
            {
                return;
            }

            peerMetaData.Disconnected();
        }

        /// <summary>
        ///     Invoked when a peer recieves a request to store account information persistently.
        ///     The request is queued until coloborating information is recieved from enough other
        ///     super-peers.
        /// </summary>
        /// <param name="request">Request that was recieved.</param>
        internal void StoreAccountRequestRecieved(StoreAccountRequest request)
        {
            m_databaseConnection.QueryParameterized(@"INSERT INTO {0}(superpeer_id, client_id, zone_id, account_id, state,          success, failed, recieved_arbitrator_id, superpeer_client_id, recieved_time, reason) 
                                                              VALUES ({1},          {2},       {3},     {4},        @parameter_1,   0,       0,       {5},                   {6},                 UNIX_TIMESTAMP(), '{7}')",
                                      new object[] { request.Account.PeristentState.Serialize() },
                                      Settings.DB_TABLE_PENDING_STORE_REQUESTS,
                                      request.SuperPeerID,
                                      request.ClientID,
                                      request.ZoneID,
                                      request.Account.ID,
                                      m_arbitratorDatabaseID,
                                      request.RecievedFrom.DatabaseID,
                                      StringHelper.Escape(request.Reason));

            //System.Console.WriteLine("["+Environment.TickCount+"] Recieved store account for " + request.SuperPeerID+"/"+request.ClientID+" - "+request.Reason);
        }

        /// <summary>
        ///     Processes all storage requests.
        /// </summary>
        private void ProcessStorageRequests()
        {
            DBResults   results;
            bool        storageUpdated = false;

            // Delete all storage requests that have finished and who's arbitrator has disconnected.
            m_databaseConnection.Query(@"DELETE FROM 
	                                        {0} 
                                        WHERE 
	                                        (success=1 OR failed=1) AND
	                                        (Select
		                                        COUNT(*)
	                                            FROM 
		                                        {1} AS b
                                                WHERE 
		                                        id=recieved_arbitrator_id) = 0 ",
                                        Settings.DB_TABLE_PENDING_STORE_REQUESTS,
                                        Settings.DB_TABLE_ACTIVE_ARBITRATORS);

            // Lock tables.
            m_databaseConnection.Query("LOCK TABLES {0} WRITE, {1} WRITE",
                                       Settings.DB_TABLE_PENDING_STORE_REQUESTS,
                                       Settings.DB_TABLE_ACCOUNTS);

            // Lets have a look at all requests and see if we find any we can commit.
            results = m_databaseConnection.Query(@"SELECT *, UNIX_TIMESTAMP()-recieved_time FROM {0} WHERE 
                                                        success=0 AND 
                                                        failed=0",
                                                   Settings.DB_TABLE_PENDING_STORE_REQUESTS);

            // Group together rows by zone id / client id.
            List<int> updated_ids = new List<int>();

            for (int i = 0; i < results.RowsAffected; i++)
            {
                DBRow row = results[i];

                // Ignore this request if we have already processed it.
                if (updated_ids.Contains((int)row["id"]))
                {
                    continue;
                }

                // Make a list of requests to update same client/zone.
                List<DBRow> sameRows = new List<DBRow>();
                sameRows.Add(row);
                updated_ids.Add((int)row["id"]);

                // Look for other rows affecting the same zone and client.
                int oldest_row_elapsed = 0;
                for (int k = 0; k < results.RowsAffected; k++)
                {
                    DBRow subrow = results[k];

                    // Ignore this request if we have already processed it.
                    if (updated_ids.Contains((int)subrow["id"]))
                    {
                        continue;
                    }

                    if ((int)row["client_id"] == (int)subrow["client_id"] &&
                        (int)row["zone_id"] == (int)subrow["zone_id"])
                    {
                        // Store oldest elapsed time for rows.
                        int subrow_elapsed = (int)(long)row["UNIX_TIMESTAMP()-recieved_time"];
                        if (subrow_elapsed > oldest_row_elapsed)
                        {
                            oldest_row_elapsed = subrow_elapsed;
                        }
                        
                        sameRows.Add(subrow);
                        updated_ids.Add((int)subrow["id"]);
                    }
                }

                // Do we have enough rows to commit.
                if (sameRows.Count >= Settings.ZoneSuperPeerCount || 
                    oldest_row_elapsed > Settings.STORE_REQUEST_TIMEOUT_INTERVAL / 1000)
                {
                    Logger.Info("Found {0} matching storage requests for client #{1} in zone #{2}.",
                                LoggerVerboseLevel.High,
                                sameRows.Count,
                                row["client_id"],
                                row["zone_id"]);

                    // Find the request with the most number of similar requests. This
                    // is the row we will consider the correct row.
                    int mostSimilarIndex = -1;
                    int mostSimilarCount = 0;
                    int mostSimilarSuperPeerID= 0;
                    UserAccountPersistentState mostSimilarState = null;

                    for (int j = 0; j < sameRows.Count; j++)
                    {
                        UserAccountPersistentState state1 = new UserAccountPersistentState();
                        state1.Deserialize((byte[])sameRows[j]["state"]);

                        // Count number of similar row.
                        int similarCount = 1;
                        for (int l = 0; l < sameRows.Count; l++)
                        {
                            if (j == l) continue;

                            UserAccountPersistentState state2 = new UserAccountPersistentState();
                            state2.Deserialize((byte[])sameRows[l]["state"]);

                            if (state1.IsSimilarTo(state2))
                            {
                                similarCount++;
                            }
                        }

                        // Is this the most similar state so far?
                        if (similarCount > mostSimilarCount || (similarCount == mostSimilarCount && (int)sameRows[j]["superpeer_id"] < mostSimilarSuperPeerID))
                        {
                            mostSimilarState = state1;
                            mostSimilarIndex = j;
                            mostSimilarCount = similarCount;
                            mostSimilarSuperPeerID = (int)sameRows[j]["superpeer_id"];
                        }
                    }

                    // If we only have a maximum of 1 match, everyone fails, we can't have
                    // one user being authorative.
                    if (mostSimilarCount <= 1)
                    {
                        Logger.Info("Of {0} matching storage requests for client #{1} in zone #{2}, only {3} were similar. Store request rejected.",
                                    LoggerVerboseLevel.Normal,
                                    sameRows.Count,
                                    row["client_id"],
                                    row["zone_id"],
                                    mostSimilarCount);

                        m_databaseConnection.Query("UPDATE {0} SET failed=1 WHERE client_id={1} AND zone_id={2}",
                                                   Settings.DB_TABLE_PENDING_STORE_REQUESTS,
                                                   row["client_id"],
                                                   row["zone_id"]);

                        storageUpdated = true;
                    }
                    else
                    {
                        Logger.Info("Of {0} matching storage requests for client #{1} in zone #{2}, {3} were similar. Store request was accepted.",
                                    LoggerVerboseLevel.High,
                                    sameRows.Count,
                                    row["client_id"],
                                    row["zone_id"],
                                    mostSimilarCount);

                        // Update state of storage requests.
                        for (int j = 0; j < sameRows.Count; j++)
                        {
                            UserAccountPersistentState state2 = new UserAccountPersistentState();
                            state2.Deserialize((byte[])sameRows[j]["state"]);

                            // If its similar to the correct state, success!
                            if (state2.IsSimilarTo(mostSimilarState))
                            {
                                m_databaseConnection.Query("UPDATE {0} SET success=1 WHERE id={1}",
                                                           Settings.DB_TABLE_PENDING_STORE_REQUESTS,
                                                           sameRows[j]["id"]);

                                Logger.Info("Storage request by SuperPeer #{0} for Client #{1} in Zone #{2} was similar to correct states and was accepted.",
                                    LoggerVerboseLevel.High,
                                    sameRows[j]["superpeer_id"],
                                    sameRows[j]["client_id"],
                                    sameRows[j]["zone_id"]);
                            }

                            // Otherwise failure :(.
                            else
                            {
                                m_databaseConnection.Query("UPDATE {0} SET failed=1 WHERE id={1}",
                                                           Settings.DB_TABLE_PENDING_STORE_REQUESTS,
                                                           sameRows[j]["id"]);

                                Logger.Info("Storage request by SuperPeer #{0} for Client #{1} in Zone #{2} was not similar to correct states, and was ignored.",
                                            LoggerVerboseLevel.Normal,
                                            sameRows[j]["superpeer_id"],
                                            sameRows[j]["client_id"],
                                            sameRows[j]["zone_id"]);
                            }
                        }

                        // Now actually store the data.
                        m_databaseConnection.QueryParameterized("UPDATE {0} SET persistent_state=@parameter_1 WHERE id={1}",
                                                                 new object[] { mostSimilarState.Serialize() },
                                                                 Settings.DB_TABLE_ACCOUNTS,
                                                                 row["account_id"]);
                        storageUpdated = true;
                    }
                }
            }

            // Dispose of timed out requests.
            
            results = m_databaseConnection.Query(@"UPDATE {0} SET failed=1 WHERE 
                                                   UNIX_TIMESTAMP() - recieved_time > {1}",
                                                  Settings.DB_TABLE_PENDING_STORE_REQUESTS,
                                                  Settings.STORE_REQUEST_TIMEOUT_INTERVAL / 1000);
            if (results.RowsAffected > 0)
            {
                storageUpdated = true;
            }

            // Unlock tables.
            m_databaseConnection.Query("UNLOCK TABLES");
        }

        /// <summary>
        ///     Sends replies to superpeers regarding completed storage requests.
        /// </summary>
        private void SendStorageRequestReplies()
        {
            DBResults results;

            // Lock tables.
            m_databaseConnection.Query("LOCK TABLES {0} WRITE",
                                       Settings.DB_TABLE_PENDING_STORE_REQUESTS);

            // Send replies to superpeers regarding completed storage requests on this arbitrator.
            results = m_databaseConnection.Query(@"SELECT * FROM {0} WHERE 
                                                        (success!=0 OR failed!=0) AND
                                                        recieved_arbitrator_id={1}",
                                       Settings.DB_TABLE_PENDING_STORE_REQUESTS,
                                       m_arbitratorDatabaseID);

            for (int i = 0; i < results.RowsAffected; i++)
            {
                DBRow row = results[i];

                bool found = false;

                // Send reply!
                foreach (Connection connection in m_listenConnection.Peers)
                {
                    if (connection.MetaData != null)
                    {
                        ArbitratorPeer peer = ((ArbitratorPeer)connection.MetaData);
                        if (peer.DatabaseID == (int)row["superpeer_client_id"])
                        {
                            SuperPeerStoreAccountReplyPacket reply = new SuperPeerStoreAccountReplyPacket();
                            reply.ClientID = (int)row["client_id"];
                            reply.SuperPeerID = (int)row["superpeer_id"];
                            reply.Failed = (int)row["failed"] != 0;
                            reply.Success = (int)row["success"] != 0;
                            connection.SendPacket(reply);

                            found = true;

                            break;
                        }
                    }
                }

                if (found == false)
                {
                    Logger.Warning("Could not find superpeer #{0} to send storage reply to, has superpeer disconnected.", LoggerVerboseLevel.High, row["superpeer_client_id"]);
                }

                m_databaseConnection.Query(@"DELETE FROM {0} WHERE id={1}",
                                            Settings.DB_TABLE_PENDING_STORE_REQUESTS,
                                            row["id"]);
            }

            // Unlock tables.
            m_databaseConnection.Query("UNLOCK TABLES");

        }

        /// <summary>
        ///     This checks the database table of storage requests and if another
        ///     super peers corroborate the request then we commit it.
        /// </summary>
        private void UpdateAccountStorageRequests()
        {
            // If we are the master then process storage requests.
            if (m_isMaster == true)
            {
                ProcessStorageRequests();
            }

            // Send replies to superpeers.
            SendStorageRequestReplies();
        }

        #endregion
        #region Public Methods

        /// <summary>
        ///     Constructs an arbitrator with the given settings.
        /// </summary>
        /// <param name="settings">Settings used to construct arbitrator class.</param>
        public ArbitratorService(Settings settings)
        {
            m_settings = settings;
        }

        /// <summary>
        ///     Initializes this arbitrator, making initial connections to database and getting ready
        ///     to begin accepting connections.
        /// </summary>
        /// <returns>True if successful, otherwise false. If false is returned then the Run/Deinit methods are never called.</returns>
        public bool Initialize()
        {
            Logger.Info("Begining setup of arbitrator server ...", LoggerVerboseLevel.High);

            m_listenConnection      = null;
            m_databaseConnection    = null;

            // Setup database connection.
            SetupDBConnection().Wait();
            if (m_databaseConnection == null || m_databaseConnection.Connected == false)
            {
                Logger.Error("Failed to setup arbitrator, could not initialize database connection.", LoggerVerboseLevel.Normal);
                return false;
            }

            // Setup listening connection.
            SetupConnection().Wait();
            if (m_listenConnection == null || m_listenConnection.Listening == false)
            {
                Logger.Error("Failed to setup arbitrator, could not initialize listen connection.", LoggerVerboseLevel.Normal);
                return false;
            }

            // Register arbitrator in database.
            RegisterArbitrator();

            Logger.Info("Setup arbitrator server successfully.", LoggerVerboseLevel.High);

            return true;
        }

        /// <summary>
        ///     Polls the arbitrator, causing it to check for connection state changes and process incoming packets until it closes.
        /// </summary>
        public void Poll()
        {
            // Lost database connection? Reconnect it.
            if (m_databaseConnection == null || m_databaseConnection.Connected == false)
            {
                SetupDBConnection().Wait();
                if (m_databaseConnection == null || m_databaseConnection.Connected == false)
                {
                    return;
                }
            }

            // Lost connection? Reconnect it.
            if (m_listenConnection == null || m_listenConnection.Listening == false)
            {
                SetupConnection().Wait();
                if (m_listenConnection == null || m_listenConnection.Listening == false)
                {
                    return;
                }
            }

            int timer = Environment.TickCount;

            // Process any incoming packets.
            if (m_listenConnection != null)
            {
                foreach (Connection connection in m_listenConnection.GetConnectedPeers())
                {
                    ProcessConnectedPeer(connection);
                }

                foreach (Connection connection in m_listenConnection.GetDisconnectedPeers())
                {
                    ProcessDisconnectedPeer(connection);
                }

                foreach (Connection connection in m_listenConnection.Peers)
                {
                    if (connection.MetaData != null)
                    {
                        ((ArbitratorPeer)connection.MetaData).Poll();
                    }
                }
            }

            if (Environment.TickCount - timer > 1000)
            {
                System.Console.WriteLine("Peers Took:{0}ms", Environment.TickCount - timer);
            }
            timer = Environment.TickCount;

            // Serialize client states.
            /*
            if (Environment.TickCount - m_lastClientSerializeTime > Settings.ArbitratorClientSerializationInterval)
            {
                SerializeClientPersistentStates();
                m_lastClientSerializeTime = Environment.TickCount;
            }
            */

            // Update arbitrator registration.
            if (Environment.TickCount - m_lastRegisterTime > Settings.ARBITRATOR_REGISTER_INTERVAL)
            {
                RegisterArbitrator();
                m_lastRegisterTime = Environment.TickCount;
            }

            if (Environment.TickCount - timer > 1000)
            {
                System.Console.WriteLine("Register Took:{0}ms", Environment.TickCount - timer);
            }
            timer = Environment.TickCount;

            // Check if we are the master or not?
            if (Environment.TickCount - m_lastMasterCheckTime > Settings.ARBITRATOR_MASTER_RECHECK_INTERVAL)
            {
                bool wasMaster = m_isMaster;
                m_isMaster = CheckIfIsMaster();

                if (m_isMaster != wasMaster || m_lastMasterCheckTime == 0)
                {
                    if (m_isMaster == false)
                    {
                        LostMasterControl();
                    }
                    else
                    {
                        GainedMasterControl();
                    }
                }

                m_lastMasterCheckTime = Environment.TickCount;
            }

            if (Environment.TickCount - timer > 1000)
            {
                System.Console.WriteLine("Lost/Gain Took:{0}ms", Environment.TickCount - timer);
            }
            timer = Environment.TickCount;

            // Update non-master information.
            if (m_isMaster == false)
            {
                // Update our current list of active-clients / super-peers if we are not
                // the master arbitrator in control of zones.
                if (Environment.TickCount - m_lastZoneCheckTime > Settings.ARBITRATOR_ZONE_RECHECK_INTERVAL)
                {
                    RefreshZoningInformation();
                }
            }

            // Update master-sppecific information.
            else
            {
                // Choose superpeers, split/merge zones.
                if (Environment.TickCount - m_lastZoneCheckTime > Settings.ARBITRATOR_ZONE_RECHECK_INTERVAL)
                {
                    UpdateZoning();
                }
            }

            if (Environment.TickCount - timer > 1000)
            {
                System.Console.WriteLine("UpdateZoningInfo Took:{0}ms", Environment.TickCount - timer);
            }
            timer = Environment.TickCount;

            // Update requests to store account information.
            if (Environment.TickCount - m_lastStoreRequestCheckTime > Settings.STORE_REQUEST_CHECK_INTERVAL)
            {
                UpdateAccountStorageRequests();

                m_lastStoreRequestCheckTime = Environment.TickCount;
            }
            if (Environment.TickCount - timer > 1000)
            {
                System.Console.WriteLine("StorageRequests Took:{0}ms", Environment.TickCount - timer);
            }
            timer = Environment.TickCount;

        }

        /// <summary>
        ///     Deinitializes the master server, releasing resources and disconnecting from everything.
        /// </summary>
        public void Deinitialize()
        {
            Logger.Info("Begining cleanup of arbitrator server ...", LoggerVerboseLevel.High);

            // Remove arbitrator from database list.
            DeregisterArbitrator();

            // Close down listening connection.
            if (m_listenConnection != null)
            {
                m_listenConnection.DisconnectAsync(false).Wait();
            }

            // Close down database connection.
            if (m_databaseConnection != null)
            {
                m_databaseConnection.DisconnectAsync().Wait();
            }

            // Dispose of settings class.
            m_settings = null;

            Logger.Info("Cleaned up arbitrator server successfully.", LoggerVerboseLevel.High);
        }

        #endregion
    }

}
