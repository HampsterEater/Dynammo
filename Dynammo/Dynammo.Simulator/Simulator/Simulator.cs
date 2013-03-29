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
using Dynammo.Arbitrator;
using Dynammo.Client;
using Dynammo.Networking;
using System.Windows.Forms;

namespace Dynammo.Simulator
{

    /// <summary>
    ///     This class contains the basic code that allows the simulation of multiple parts of the developed
    ///     architecture on multiple different computers at once.
    /// </summary>
    public class Simulator
    {
        #region Private Members

        private DBConnection                    m_databaseConnection;

        private Settings                        m_settings;

        private List<SimulatorThread>           m_threads = new List<SimulatorThread>();

        private int                             m_clientSpawnTimer = Environment.TickCount;

        private int                             m_loadNetworkStateTimer = Environment.TickCount;

        private bool                            m_running;
        private bool                            m_paused;
        private bool                            m_aborting;

        private List<SimulatorClientState>      m_clientStates      = new List<SimulatorClientState>();
        private List<SimulatorArbitratorState>  m_arbitratorStates  = new List<SimulatorArbitratorState>();

        private ZoneGrid                        m_zoneGrid          = new ZoneGrid();

        #endregion
        #region Public Properties

        /// <summary>
        ///     Returns a copy of the thread list.
        /// </summary>
        public List<SimulatorThread> Threads
        {
            get { return new List<SimulatorThread>(m_threads); }
        }

        /// <summary>
        ///     Returns a copy of the client states.
        /// </summary>
        public List<SimulatorClientState> ClientStates
        {
            get { return new List<SimulatorClientState>(m_clientStates); }
        }

        /// <summary>
        ///     Returns a copy of the arbitrator states.
        /// </summary>
        public List<SimulatorArbitratorState> ArbitratorStates
        {
            get { return new List<SimulatorArbitratorState>(m_arbitratorStates); }
        }

        /// <summary>
        ///     Returns the zone grid that represents the world.
        /// </summary>
        public ZoneGrid ZoneGrid
        {
            get { return m_zoneGrid; }
        }

        /// <summary>
        ///     Returns true if this simulator is running.
        /// </summary>
        public bool IsRunning
        {
            get { return m_running; }
        }

        /// <summary>
        ///     Returns true if this simulator is paused.
        /// </summary>
        public bool IsPaused
        {
            get { return m_paused; }
        }

        /// <summary>
        ///     Returns true if this simulator is aborting.
        /// </summary>
        public bool IsAborting
        {
            get { return m_aborting; }
        }

        /// <summary>
        ///     Returns the current database connection.
        /// </summary>
        public DBConnection DatabaseConnection
        {
            get { return m_databaseConnection; }
        }

        /// <summary>
        ///     Returns the current settings object used by the simulator. Contains
        ///     the current replicated state.
        /// </summary>
        public Settings Settings
        {
            get { return m_settings; }
        }

        #endregion
        #region Private Methods

        /// <summary>
        ///     Spawns a new arbitrator simulation thread.
        /// </summary>
        private bool SpawnArbitrator()
        {
            ArbitratorSimulatorThread thread = new ArbitratorSimulatorThread();
            thread.Run(m_settings.Clone());

            m_threads.Add(thread);

            return true;
        }

        /// <summary>
        ///     Spawns a new client simulation thread.
        /// </summary>
        private bool SpawnClient()
        {
            string arbitratorHost = "localhost";
            ushort arbitratorPort = 0;

            // Select all arbitrators in the database, ordered by which have hte most clients
            // registered to them.
            DBResults results = m_databaseConnection.Query(@"SELECT 
                                                                a.id,
                                                                a.ip_address,
                                                                a.port,
                                                                (
                                                                    SELECT 
                                                                        COUNT(*)
                                                                    FROM
                                                                        {0} AS c
                                                                    WHERE
                                                                        c.arbitrator_id = a.id
                                                                ) AS client_count,
                                                                a.last_active_timestamp
                                                            FROM 
                                                                {1} AS a
                                                            ORDER BY
                                                                client_count ASC,
                                                                a.last_active_timestamp DESC
                                                                ",
                                                            Settings.DB_TABLE_ACTIVE_CLIENTS,
                                                            Settings.DB_TABLE_ACTIVE_ARBITRATORS);

            // Did we get any arbitrators we can use?
            if (results.RowsAffected <= 0)
            {
                return false;
            }

            arbitratorHost = (string)results[0]["ip_address"];
            arbitratorPort = (ushort)((int)results[0]["port"]);

            if (arbitratorHost == HardwareHelper.GetLocalIPAddress())
            {
                arbitratorHost = "localhost";
            }

            // Create peer thread.
            ClientSimulatorThread new_thread = new ClientSimulatorThread(arbitratorHost, arbitratorPort);
            new_thread.Run(m_settings.Clone());
            m_threads.Add(new_thread);

            return true;
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
        ///     Does some general cleanup on the database before we begin a simulation.
        /// </summary>
        private void CleanupDatabase()
        {
            DBResults results;

            // Delete old arbitrator entries.
            results = m_databaseConnection.Query(@"DELETE FROM {0} WHERE UNIX_TIMESTAMP()-last_active_timestamp > {1}",
                                                  Settings.DB_TABLE_ACTIVE_ARBITRATORS,
                                                  Settings.ARBITRATOR_REGISTER_TIMEOUT_INTERVAL / 1000);

            // Delete old client entries.
            results = m_databaseConnection.Query(@"DELETE FROM {0} WHERE UNIX_TIMESTAMP()-last_active_timestamp > {1}",
                                                  Settings.DB_TABLE_ACTIVE_CLIENTS,
                                                  Settings.CLIENT_REGISTER_TIMEOUT_INTERVAL / 1000);         
        }

        /// <summary>
        ///     Removes all remenants of previous runs.
        /// </summary>
        private void DisposeDatabase()
        {
            DBResults results;

            // Delete old arbitrator entries.
            results = m_databaseConnection.Query(@"DELETE FROM {0}",
                                                  Settings.DB_TABLE_ACTIVE_ARBITRATORS);

            // Delete old client entries.
            results = m_databaseConnection.Query(@"DELETE FROM {0}",
                                                  Settings.DB_TABLE_ACTIVE_CLIENTS);

            // Delete old replicated settings.
            results = m_databaseConnection.Query(@"DELETE FROM {0}",
                                                  Settings.DB_TABLE_REPLICATED_SETTINGS);

            // Delete old zones.
            results = m_databaseConnection.Query(@"DELETE FROM {0}",
                                                  Settings.DB_TABLE_ZONES);

            // Delete old zone superpeers.
            results = m_databaseConnection.Query(@"DELETE FROM {0}",
                                                  Settings.DB_TABLE_ZONE_SUPERPEERS);

            // Delete old accounts.
            results = m_databaseConnection.Query(@"DELETE FROM {0}",
                                                  Settings.DB_TABLE_ACCOUNTS);

            // Delete old store requests.
            results = m_databaseConnection.Query(@"DELETE FROM {0}",
                                                  Settings.DB_TABLE_PENDING_STORE_REQUESTS);  
        }

        /// <summary>
        ///     Loads information about the current network state form the database.
        /// </summary>
        private void LoadNetworkState()
        {
            LoadArbitrators();
            LoadClients();
            LoadReplicatedSettings();
            LoadZones();
            LoadZoneSuperPeers();
        }

        /// <summary>
        ///     Loads the zone map from the database.
        /// </summary>
        private void LoadZones()
        {
            DBConnection db = m_databaseConnection;
            DBResults results = null;

            results = db.Query("SELECT id, child_zone_1_id, child_zone_2_id, parent_id, split_orientation FROM {0}",
                                Settings.DB_TABLE_ZONES);

            // Clear out the zone grid.
            m_zoneGrid.Clear();

            // Add New Grids.
            for (int i = 0; i < results.RowsAffected; i++)
            {
                DBRow row = results[i];
                
                Zone zone = new Zone((int)row["id"],
                                     (int)row["parent_id"], 
                                     (int)row["child_zone_1_id"], 
                                     (int)row["child_zone_2_id"], 
                                     (ZoneSplitOrientation)((int)row["split_orientation"]));

                m_zoneGrid.AddZone(zone);
            }
        }

        /// <summary>
        ///     Loads zoen super-peers from the database.
        /// </summary>
        private void LoadZoneSuperPeers()
        {
            DBConnection db = m_databaseConnection;
            DBResults results = null;

            results = db.Query("SELECT id, zone_id, client_id FROM {0}",
                                Settings.DB_TABLE_ZONE_SUPERPEERS);

            // Clear out the zone grid.
            foreach (Zone zone in m_zoneGrid.Zones)
            {
                zone.SuperPeers.Clear();
            }

            // Add new super peers.
            for (int i = 0; i < results.RowsAffected; i++)
            {
                DBRow row = results[i];

                ZoneSuperPeer superPeer = new ZoneSuperPeer();
                superPeer.ID            = (int)row["id"];
                superPeer.ZoneID        = (int)row["zone_id"];
                superPeer.ClientID      = (int)row["client_id"];

                Zone zone = m_zoneGrid.GetZoneByID(superPeer.ZoneID);
                if (zone != null)
                {
                    zone.SuperPeers.Add(superPeer);
                }
            }
        }

        /// <summary>
        ///     Loads arbitrator information from the database.
        /// </summary>
        private void LoadArbitrators()
        {
            DBConnection    db      = m_databaseConnection;
            DBResults       results = null;

            results = db.Query("SELECT id, ip_address, port, last_active_timestamp FROM {0}",
                                Settings.DB_TABLE_ACTIVE_ARBITRATORS);

            // Delete Old Rows.
            foreach (SimulatorArbitratorState subState in m_arbitratorStates.ToList())
            {
                bool found = false;

                for (int i = 0; i < results.RowsAffected; i++)
                {
                    if ((int)results[i]["id"] == (int)subState.DatabaseSettings["id"])
                    {
                        found = true;
                        break;
                    }
                }

                if (found == false)
                {
                    m_arbitratorStates.Remove(subState);
                }
            }

            // Add New Rows / Update Old Rows.
            for (int i = 0; i < results.RowsAffected; i++)
            {
                DBRow row = results[i];
                SimulatorArbitratorState state = null;

                foreach (SimulatorArbitratorState subState in m_arbitratorStates)
                {
                    if ((string)subState.DatabaseSettings["ip_address"] == (string)row["ip_address"] &&
                        (int)subState.DatabaseSettings["port"] == (int)row["port"])
                    {
                        state = subState;
                        break;
                    }
                }

                if (state == null)
                {
                    state = new SimulatorArbitratorState();
                    m_arbitratorStates.Add(state);
                }

                foreach (string key in row.ColumnNames)
                {
                    state.DatabaseSettings[key] = row[key];
                }
            }

            // Work out which arbitrator is the master.
            SimulatorArbitratorState masterArbitrator = null;
            foreach (SimulatorArbitratorState subState in m_arbitratorStates.ToList())
            {
                subState.IsMaster = false;

                if (masterArbitrator == null ||
                    (int)subState.DatabaseSettings["id"] < (int)masterArbitrator.DatabaseSettings["id"])
                {
                    masterArbitrator = subState;
                }
            }

            if (masterArbitrator != null)
            {
                masterArbitrator.IsMaster = true;
            }
        }

        /// <summary>
        ///     Loads clients from the database.
        /// </summary>
        private void LoadClients()
        {
            DBConnection db = m_databaseConnection;
            DBResults results = null;

            results = db.Query("SELECT id, arbitrator_id, ip_address, port, last_active_timestamp, account_id, listening, listen_port, zone_id FROM {0}",
                                Settings.DB_TABLE_ACTIVE_CLIENTS);

            // Delete Old Rows.
            foreach (SimulatorClientState subState in m_clientStates.ToList())
            {
                bool found = false;

                for (int i = 0; i < results.RowsAffected; i++)
                {
                    if ((int)results[i]["id"] == (int)subState.DatabaseSettings["id"])
                    {
                        found = true;
                        break;
                    }
                }

                if (found == false)
                {
                    m_clientStates.Remove(subState);
                }
            }

            // Add New Rows / Update Old Rows.
            for (int i = 0; i < results.RowsAffected; i++)
            {
                DBRow row = results[i];
                SimulatorClientState state = null;

                foreach (SimulatorClientState subState in m_clientStates)
                {
                    if ((string)subState.DatabaseSettings["ip_address"] == (string)row["ip_address"] &&
                        (int)subState.DatabaseSettings["port"]          == (int)row["port"])
                    {
                        state = subState;
                        break;
                    }
                }

                if (state == null)
                {
                    state = new SimulatorClientState();
                    m_clientStates.Add(state);
                }

                // Copy over all settings.
                foreach (string key in row.ColumnNames)
                {
                    state.DatabaseSettings[key] = row[key];
                }

                // Update client account?
                if (row["account_id"] != null)
                {
                    if (state.Account != null)
                    {
                        if ((int)row["account_id"] != state.Account.ID)
                        {
                            state.Account = UserAccount.LoadByID(m_databaseConnection, (int)row["account_id"]);
                        }
                    }
                    else
                    {
                        state.Account = UserAccount.LoadByID(m_databaseConnection, (int)row["account_id"]);
                    }
                }
                else
                {
                    state.Account = null;
                }
            }
        }

        /// <summary>
        ///     Loads replicated settings from the database.
        /// </summary>
        private void LoadReplicatedSettings()
        {
            DBConnection db = m_databaseConnection;
            DBResults results = null;

            results = m_databaseConnection.Query(@"SELECT `name`, `value` FROM {0}",
                                                  Settings.DB_TABLE_REPLICATED_SETTINGS);

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

        #endregion
        #region Public Methods

        /// <summary>
        ///     Polls the simulator, this is where new threads are spun up to simulate different parts of the architecture.
        /// </summary>
        public void Poll()
        {
            // While polling threads, we count them as well
            // so we can decide if we want to spawn any more later.
            int arbitratorThreadCount           = 0;
            int listeningArbitratorThreadCount  = 0;
            int clientThreadCount               = 0;

            // Lost database connection? Reconnect it.
            if (m_running == true && m_aborting == false)
            {
                if (m_databaseConnection == null || m_databaseConnection.Connected == false)
                {
                    SetupDBConnection().Wait();
                    if (m_databaseConnection == null || m_databaseConnection.Connected == false)
                    {
                        return;
                    }
                }
            }

            // Poll simulation threads.
            foreach (SimulatorThread thread in m_threads.ToArray())
            {
                if (thread.IsRunning == false)
                {
                    Logger.Info("Simulator thread #{0} has now finished.", LoggerVerboseLevel.High, thread.ThreadID);
                    m_threads.Remove(thread);
                }
                else if (thread.IsAborting == false)
                {
                    if (thread is ArbitratorSimulatorThread)
                    {
                        ArbitratorSimulatorThread arbitrator = thread as ArbitratorSimulatorThread;
                        if (arbitrator.Service != null && arbitrator.Service.IsListening == true)
                        {
                            listeningArbitratorThreadCount++;
                        }
                        arbitratorThreadCount++;
                    }
                    else if (thread is ClientSimulatorThread)
                    {
                        clientThreadCount++;
                    }
                }
            }

            // Have we stopped running or are we trying to abort?
            if (m_aborting == true || m_running == false)
            {
                if (m_threads.Count <= 0)
                {
                    if (m_aborting == true)
                    {
                        // Dispose of database connection.
                        if (m_databaseConnection != null)
                        {
                            m_databaseConnection.DisconnectAsync().Wait();
                            m_databaseConnection = null;
                        }

                        Logger.Info("All simulator threads have now terminated.", LoggerVerboseLevel.Normal);
                    }

                    m_running  = false;
                    m_aborting = false;
                    return;
                }

                return;
            }

            // Paused?
            if (m_paused == true)
            {
                return;
            }

            // Spawn off new arbitrators.
            if (arbitratorThreadCount < m_settings.ArbitratorCount)
            {
                if (SpawnArbitrator() == true)
                {
                    Logger.Info("Spawned new arbitrator thread ({0}/{1} arbitrators running).", LoggerVerboseLevel.High, arbitratorThreadCount + 1, m_settings.ArbitratorCount);
                }
            }

            // Spawn off new game clients.
            if (clientThreadCount < m_settings.ClientCount && listeningArbitratorThreadCount > 0)
            {
                if (Environment.TickCount > m_clientSpawnTimer)
                {
                    if (SpawnClient() == true)
                    {
                        Logger.Info("Spawned new client thread ({0}/{1} clients running).", LoggerVerboseLevel.High, clientThreadCount + 1, m_settings.ClientCount);
                    }

                    m_clientSpawnTimer = Environment.TickCount + RandomHelper.RandomInstance.Next(m_settings.ClientConnectIntervalMin, m_settings.ClientConnectIntervalMax);
                }
            }

            // Load current network state.
            if (Environment.TickCount > m_loadNetworkStateTimer)
            {
                LoadNetworkState();
                m_loadNetworkStateTimer = Environment.TickCount + m_settings.SimulatorReloadClientInterval;
            }
        }

        /// <summary>
        ///     Starts the simulator using the given settings.
        /// </summary>
        public void Start(Settings settings)
        {
            if (m_running == true)
            {
                return;
            }

            m_threads.Clear();
            m_settings = settings;

            // Try and setup database connection.
            Task<bool> task = SetupDBConnection();
            task.Wait();

            if (task.Result == false)
            {
                return;
            }

            // Any remenants of old networks?
            DBResults results1 = m_databaseConnection.Query("SELECT * FROM {0}",
                                                           Settings.DB_TABLE_ACTIVE_ARBITRATORS);
            DBResults results2 = m_databaseConnection.Query("SELECT * FROM {0}",
                                                           Settings.DB_TABLE_ACTIVE_CLIENTS);
            DBResults results3 = m_databaseConnection.Query("SELECT * FROM {0}",
                                                           Settings.DB_TABLE_REPLICATED_SETTINGS);
            DBResults results4 = m_databaseConnection.Query("SELECT * FROM {0}",
                                                           Settings.DB_TABLE_ZONES);
            DBResults results5 = m_databaseConnection.Query("SELECT * FROM {0}",
                                                           Settings.DB_TABLE_ZONE_SUPERPEERS);
            DBResults results6 = m_databaseConnection.Query("SELECT * FROM {0}",
                                                           Settings.DB_TABLE_ACCOUNTS);
            DBResults results7 = m_databaseConnection.Query("SELECT * FROM {0}",
                                                           Settings.DB_TABLE_PENDING_STORE_REQUESTS);

            if (results1.RowsAffected > 0 || results2.RowsAffected > 0 || 
                results3.RowsAffected > 0 || results4.RowsAffected > 0 || 
                results5.RowsAffected > 0 || results6.RowsAffected > 0 ||
                results7.RowsAffected > 0)
            {
                if (MessageBox.Show("There are currently several entries in the database for active arbitrators and active clients.\n\nEither these are remnants of an old, improperly terminated simulation, or you are running multiple simulators.\n\nIf these are old remnants then click Yes to purge them, No to ignore.", "Database Remnants", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    DisposeDatabase();
                }
            }

            // Cleanup the database.
            CleanupDatabase();

            m_running   = true;
            m_paused    = false;
            m_aborting  = false;
        }

        /// <summary>
        ///     Pauses the simulation.
        /// </summary>
        public void Pause()
        {
            if (m_running == false || m_paused == true)
            {
                return;
            }

            foreach (SimulatorThread thread in m_threads)
            {
                if (thread.IsPaused == false)
                {
                    thread.Pause();
                }
            }

            m_paused = true;
        }

        /// <summary>
        ///     Resumes the simulation after being paused.
        /// </summary>
        public void Resume()
        {
            if (m_running == false || m_paused == false)
            {
                return;
            }

            foreach (SimulatorThread thread in m_threads)
            {
                if (thread.IsPaused == true)
                {
                    thread.Resume();
                }
            }

            m_paused = false;
        }

        /// <summary>
        ///     Stops this simulation and aborts all parts of the architecture being simulated.
        /// </summary>
        public void Stop()
        {
            if (m_running == false)
            {
                return;
            }

            // Tell all threads to abort.
            foreach (SimulatorThread thread in m_threads)
            {
                thread.Abort();
            }

            Logger.Info("Waiting for threads to terminate ...", LoggerVerboseLevel.Normal);

            m_aborting = true;
            m_paused   = false;
        }

        #endregion
    }

}
