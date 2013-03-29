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
using System.Reflection;
using System.ComponentModel;

namespace Dynammo.Common
{

    /// <summary>
    ///     This attribute allows settings to be flagged as being replicated from the master arbitrator to 
    ///     slave arbitrators. This is mainly used for zoning information so that arbitrators don't end up 
    ///     with differing settings. Replication is mainly done through the database for this.
    /// </summary>
    public class MasterToSlaveReplicatedAttribute : Attribute
    {
        // Dosen't actually need to contain anything, simply a flag.
    }

    /// <summary>
    ///     Stores all modifiable game settings. All properties should be defined
    ///     as per the guidelines for property-grids, so instances of this class
    ///     can be properly bound. Also contains properties used by the simulator.
    /// </summary>
    public class Settings
    {
        #region Private Members

        // Common settings.
        private string m_dbHostname             = "localhost";
        private string m_dbUsername             = "root";
        private string m_dbPassword             = "passw0rd";
        private string m_dbName                 = "dynammo";
        private ushort m_dbPort                 = 3306;

        // Simulator settings.
        private int m_simulatorReloadClientInterval = 500;

        // Arbitrator settings.
        private int m_arbitratorCount       = 3;
        private int m_arbitratorLifetimeMin = 60 * 60 * 1000;//60000 * 4;//0 * 999;
        private int m_arbitratorLifetimeMax = 60 * 60 * 1000;//60000 * 10;//0 * 999;
        //private int m_arbitratorClientSerializationInterval = 60 * 1000;

        // Peer settings.
        private int m_clientCount                 = 25;//200;
        private int m_clientConnectIntervalMin    = 500;//500;
        private int m_clientConnectIntervalMax    = 1000;//2000;
        private int m_clientLifetimeMin           = 60 * 60 * 1000;//10000;
        private int m_clientLifetimeMax           = 60 * 60 * 1000;//120000;
        private float m_clientMovementSpeedMin    = 3.0f;//5.0f;
        private float m_clientMovementSpeedMax    = 8.0f;//20.0f;
        private float m_clientDirectionChangeTimeMin = 3500.0f;
        private float m_clientDirectionChangeTimeMax = 5000.0f;
        private float m_clientStartX = 0f;
        private float m_clientStartY              = 0f;
        private int m_superPeerWorldStateBroadcastInterval = 100;

        //private float m_superPeerFudgeDataIntervalMin = 10000.0f;
       // private float m_superPeerFudgeDataIntervalMax = 60000.0f;

        // Zoneing settings.
        private int m_zoneInformationLastModified  = 0;
        private int m_worldWidth                   = 400;
        private int m_worldHeight                  = 400;
        private int m_zoneMinWidth                 = 64;
        private int m_zoneMinHeight                = 64;
        private int m_zoneOverpopulationThreshold  = 5;//9;
        private int m_zoneUnderpopulationThreshold = 2;
        private int m_zoneSuperPeerCount           = 3;

        #endregion

        // -----------------------------------------------------------------------------------------
        //  Constant Compile-Time Settings
        // -----------------------------------------------------------------------------------------
        #region Constant Properties

        // Reconnection timeout settings.
        public const int CONNECTION_RECONNECT_DURATION              = (30 * (60 * 1000));
        public const int CONNECTION_RECONNECT_BACKOFF_MAX           = 2 * (60 * 1000);
        public const int CONNECTION_RECONNECT_BACKOFF_START         = 50;
        public const int CONNECTION_RECONNECT_BACKOFF_MULTIPLIER    = 2;

        // Connection timeouts.
        public const int CONNECTION_SEND_TIMEOUT                    = 01000;
        public const int CONNECTION_RECIEVE_TIMEOUT                 = 10000;
        public const int CONNECTION_DISCONNECT_TIMEOUT              = 10000;
        public const int CONNECTION_LISTEN_BACKLOG                  = Int32.MaxValue;
        public const int CONNECTION_POLL_INTERVAL                   = 1000;
        public const int CONNECTION_PING_INTERVAL                   = 10000;
        public const int CONNECTION_PING_TIMEOUT_INTERVAL           = 600000;
        public const int CONNECTION_ESTABLISH_TIMEOUT_INTERVAL      = 10000;

        // Arbitrator registration timers.  
        public const int ARBITRATOR_REGISTER_INTERVAL                   = 5  * 1000;
        public const int ARBITRATOR_REGISTER_TIMEOUT_INTERVAL           = 40 * 1000;
        public const int ARBITRATOR_MASTER_RECHECK_INTERVAL             = 1 * 1000;
        public const int ARBITRATOR_ZONE_RECHECK_INTERVAL               = 1 * 1000;
        public const int ARBITRATOR_WAIT_FOR_SETTINGS_TIMEOUT_INTERVAL  = 60 * 1000;
        public const int ARBITRATOR_WAIT_FOR_ZONES_TIMEOUT_INTERVAL     = 60 * 1000;

        public const int CLIENT_REGISTER_INTERVAL                   = 5 * 1000;
        public const int CLIENT_REGISTER_TIMEOUT_INTERVAL           = 40 * 1000;

        public const int STORE_REQUEST_CHECK_INTERVAL               = 50;//1 * 1000;
        public const int STORE_REQUEST_TIMEOUT_INTERVAL             = 3 * 1000;

        // Database values.
        public const string DB_TABLE_ACTIVE_ARBITRATORS             = "active_arbitrators";
        public const string DB_TABLE_ACTIVE_CLIENTS                 = "active_clients";
        public const string DB_TABLE_REPLICATED_SETTINGS            = "replicated_settings";
        public const string DB_TABLE_ZONES                          = "zones";
        public const string DB_TABLE_ZONE_SUPERPEERS                = "zone_superpeers";
        public const string DB_TABLE_ACCOUNTS                       = "accounts";
        public const string DB_TABLE_PENDING_STORE_REQUESTS         = "pending_store_requests";

        #endregion

        // -----------------------------------------------------------------------------------------
        //  Common Settings
        // -----------------------------------------------------------------------------------------
        #region Common Properties

        [CategoryAttribute("Database Settings"), DisplayName("Database Hostname"), DefaultValue("localhost"), DescriptionAttribute("Hostname of the database storing the game state.")]
        public string DatabaseHostname
        {
            get { return m_dbHostname; }
            set { m_dbHostname = value; }
        }

        [CategoryAttribute("Database Settings"), DisplayName("Database Username"), DefaultValue("root"), DescriptionAttribute("Username used to login to the main database.")]
        public string DatabaseUsername
        {
            get { return m_dbUsername; }
            set { m_dbUsername = value; }
        }

        [CategoryAttribute("Database Settings"), DisplayName("Database Password"), DefaultValue("passw0rd"), DescriptionAttribute("Password used to login to the main database.")]
        public string DatabasePassword
        {
            get { return m_dbPassword; }
            set { m_dbPassword = value; }
        }

        [CategoryAttribute("Database Settings"), DisplayName("Database Schema Name"), DefaultValue("dynammo"), DescriptionAttribute("Name of schema within the main database to use.")]
        public string DatabaseName
        {
            get { return m_dbName; }
            set { m_dbName = value; }
        }

        [CategoryAttribute("Database Settings"), DisplayName("Database Port"), DefaultValue((ushort)3306), DescriptionAttribute("Port on which to connect to the main database.")]
        public ushort DatabasePort
        {
            get { return m_dbPort; }
            set { m_dbPort = value; }
        }

        #endregion

        // -----------------------------------------------------------------------------------------
        //  Arbitrator Settings
        // -----------------------------------------------------------------------------------------
        #region Arbitrator Properties

        [CategoryAttribute("Arbitrator Settings"), DisplayName("Number of Arbitrators"), DefaultValue(3), DescriptionAttribute("Number of arbitrator servers that will be responsible for controlling the game world.")]
        public int ArbitratorCount
        {
            get { return m_arbitratorCount; }
            set { m_arbitratorCount = value; }
        }

        [CategoryAttribute("Arbitrator Settings"), DisplayName("Minimum Arbitrator Lifetime"), DefaultValue(120000), DescriptionAttribute("Minimum lifetime of arbitrators in milliseconds.")]
        public int ArbitratorLifetimeMin
        {
            get { return m_arbitratorLifetimeMin; }
            set { m_arbitratorLifetimeMin = value; }
        }

        [CategoryAttribute("Arbitrator Settings"), DisplayName("Maximum Arbitrator Lifetime"), DefaultValue(480000), DescriptionAttribute("Maximum lifetime of arbitrators in milliseconds.")]
        public int ArbitratorLifetimeMax
        {
            get { return m_arbitratorLifetimeMax; }
            set { m_arbitratorLifetimeMax = value; }
        }

        #endregion

        // -----------------------------------------------------------------------------------------
        //  Peer Settings
        // -----------------------------------------------------------------------------------------
        #region Peer Properties

        [CategoryAttribute("Client Settings"), DisplayName("Maximum Number of Clients"), DefaultValue(200), DescriptionAttribute("Maximum number of clients that can be in the game world at any given time.")]
        public int ClientCount
        {
            get { return m_clientCount; }
            set { m_clientCount = value; }
        }

        [CategoryAttribute("Client Settings"), DisplayName("Minimum Client Lifetime"), DefaultValue(20000), DescriptionAttribute("Minimum lifetime of clients in milliseconds.")]
        public int ClientLifetimeMin
        {
            get { return m_clientLifetimeMin; }
            set { m_clientLifetimeMin = value; }
        }

        [CategoryAttribute("Client Settings"), DisplayName("Maximum Client Lifetime"), DefaultValue(240000), DescriptionAttribute("Maximum lifetime of clients in milliseconds.")]
        public int ClientLifetimeMax
        {
            get { return m_clientLifetimeMax; }
            set { m_clientLifetimeMax = value; }
        }

        [CategoryAttribute("Client Settings"), DisplayName("Minimum Client Connect Interval"), DefaultValue(500), DescriptionAttribute("Minimum interval in milliseconds between client connections.")]
        public int ClientConnectIntervalMin
        {
            get { return m_clientConnectIntervalMin; }
            set { m_clientConnectIntervalMin = value; }
        }

        [CategoryAttribute("Client Settings"), DisplayName("Maximum Client Connect Interval"), DefaultValue(2000), DescriptionAttribute("Maximum interval in milliseconds between client connectionss.")]
        public int ClientConnectIntervalMax
        {
            get { return m_clientConnectIntervalMax; }
            set { m_clientConnectIntervalMax = value; }
        }

        [CategoryAttribute("Client Settings"), DisplayName("Minimum Client Movement Speed"), DefaultValue(5.0f), DescriptionAttribute("Minimum movement speed of clients in pixels per second.")]
        public float ClientMovementSpeedMin
        {
            get { return m_clientMovementSpeedMin; }
            set { m_clientMovementSpeedMin = value; }
        }

        [CategoryAttribute("Client Settings"), DisplayName("Maximum Client Movement Speed"), DefaultValue(30.0f), DescriptionAttribute("Maximum movement speed of clients in pixels per second.")]
        public float ClientMovementSpeedMax
        {
            get { return m_clientMovementSpeedMax; }
            set { m_clientMovementSpeedMax = value; }
        }

        [CategoryAttribute("Client Settings"), DisplayName("Minimum Client Direction Change Interval"), DefaultValue(1000.0f), DescriptionAttribute("Minimum interval in milliseconds between client direction changes.")]
        public float ClientDirectionChangeTimeMin
        {
            get { return m_clientDirectionChangeTimeMin; }
            set { m_clientDirectionChangeTimeMin = value; }
        }

        [CategoryAttribute("Client Settings"), DisplayName("Maximum Client Direction Change Interval"), DefaultValue(8000.0f), DescriptionAttribute("Maximum interval in milliseconds between client direction changes.")]
        public float ClientDirectionChangeTimeMax
        {
            get { return m_clientDirectionChangeTimeMax; }
            set { m_clientDirectionChangeTimeMax = value; }
        }

        [CategoryAttribute("Client Settings"), DisplayName("Client Start X Position"), DefaultValue(0f), DescriptionAttribute("Starting position on the X axis of the client. Setting this to 0 will produce a random position.")]
        public float ClientStartX
        {
            get { return m_clientStartX; }
            set { m_clientStartX = value; }
        }

        [CategoryAttribute("Client Settings"), DisplayName("Client Start Y Position"), DefaultValue(0f), DescriptionAttribute("Starting position on the Y axis of the client. Setting this to 0 will produce a random position.")]
        public float ClientStartY
        {
            get { return m_clientStartY; }
            set { m_clientStartY = value; }
        }

        [CategoryAttribute("SuperPeer Settings"), DisplayName("SuperPeer World State Broadcast Interval"), DefaultValue(250), DescriptionAttribute("Interval in milliseconds between each transmission from a super peer to its clients of the worlds state.")]
        public int SuperPeerWorldStateBroadcastInterval
        {
            get { return m_superPeerWorldStateBroadcastInterval; }
            set { m_superPeerWorldStateBroadcastInterval = value; }
        }        

        #endregion

        // -----------------------------------------------------------------------------------------
        //  Zoneing Settings
        // -----------------------------------------------------------------------------------------
        #region Zoneing Properties

        [MasterToSlaveReplicated(), CategoryAttribute("Zoning Settings"), DisplayName("World Width"), DefaultValue(500), DescriptionAttribute("Width of the game world in pixels.")]
        public int WorldWidth
        {
            get { return m_worldWidth; }
            set { m_worldWidth = value; }
        }

        [MasterToSlaveReplicated(), CategoryAttribute("Zoning Settings"), DisplayName("World Height"), DefaultValue(500), DescriptionAttribute("Height of the game world in pixels.")]
        public int WorldHeight
        {
            get { return m_worldHeight; }
            set { m_worldHeight = value; }
        }

        [MasterToSlaveReplicated(), CategoryAttribute("Zoning Settings"), DisplayName("Minimum Zone Width"), DefaultValue(64), DescriptionAttribute("Minimum width of a subdivided zone in pixels.")]
        public int MinimumZoneWidth
        {
            get { return m_zoneMinWidth; }
            set { m_zoneMinWidth = value; }
        }

        [MasterToSlaveReplicated(), CategoryAttribute("Zoning Settings"), DisplayName("Minimum Zone Height"), DefaultValue(64), DescriptionAttribute("Minimum height of a subdivided zone in pixels.")]
        public int MinimumZoneHeight
        {
            get { return m_zoneMinHeight; }
            set { m_zoneMinHeight = value; }
        }

        [MasterToSlaveReplicated(), CategoryAttribute("Zoning Settings"), DisplayName("Zone Overpopulation Threshold"), DefaultValue(10), DescriptionAttribute("The number of players that need to be in this zone for it to be considered for sub-division.")]
        public int ZoneOverpopulationThreshold
        {
            get { return m_zoneOverpopulationThreshold; }
            set { m_zoneOverpopulationThreshold = value; }
        }

        [MasterToSlaveReplicated(), CategoryAttribute("Zoning Settings"), DisplayName("Zone Underpopulation Threshold"), DefaultValue(4), DescriptionAttribute("The number of players that need to be in this zone for it to be considered for re-integration with nearby zones.")]
        public int ZoneUnderpopulationThreshold
        {
            get { return m_zoneUnderpopulationThreshold; }
            set { m_zoneUnderpopulationThreshold = value; }
        }

        [MasterToSlaveReplicated(), CategoryAttribute("Zoning Settings"), DisplayName("Zone SuperPeer Count"), DefaultValue(3), DescriptionAttribute("The number of SuperPeers required to run an indiviudal zone.")]
        public int ZoneSuperPeerCount
        {
            get { return m_zoneSuperPeerCount; }
            set { m_zoneSuperPeerCount = value; }
        }

        [MasterToSlaveReplicated()]
        public int ZoneInformationLastModified
        {
            get { return m_zoneInformationLastModified; }
            set { m_zoneInformationLastModified = value; }
        }

        #endregion
        
        // -----------------------------------------------------------------------------------------
        //  Simulator Settings
        // -----------------------------------------------------------------------------------------
        #region Simulator Settings
        
        [CategoryAttribute("Simulator Settings"), DisplayName("Simulator State Reload Interval"), DefaultValue(1000), DescriptionAttribute("How often the simulator reloads the state of the game from the database.")]
        public int SimulatorReloadClientInterval
        {
            get { return m_simulatorReloadClientInterval; }
            set { m_simulatorReloadClientInterval = value; }
        }

        #endregion

        // -----------------------------------------------------------------------------------------
        //  Public Methods
        // -----------------------------------------------------------------------------------------
        #region Public Methods

        /// <summary>
        ///     Creates a shallow clone of this object.
        /// </summary>
        /// <returns>Clone of this object.</returns>
        public Settings Clone()
        {
            return this.MemberwiseClone() as Settings;
        }

        #endregion
    }


}
