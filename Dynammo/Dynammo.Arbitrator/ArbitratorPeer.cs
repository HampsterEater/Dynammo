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

namespace Dynammo.Arbitrator
{

    /// <summary>
    ///     Stores general meta data on a peers connected to the arbitrator.
    /// </summary>
    public class ArbitratorPeer
    {
        #region Private Members

        // General settings.
        private ArbitratorService m_arbitrator;
        private Connection m_connection;

        // Registration settings.
        private int m_lastRegisterTime = Environment.TickCount;

        // Database settings.
        private long m_clientDatabaseID = 0;
        private UserAccount m_account = null;

        // Listening settings.
        private bool m_listening = false;
        private int m_listenPort = 0;

        // Zoning information.
        private int m_zoneID = 0; // Which zone we are currently in.

        #endregion
        #region Public Properties

        /// <summary>
        ///     Returns the database ID of this peer.
        /// </summary>
        public long DatabaseID
        {
            get { return m_clientDatabaseID; }
        }

        /// <summary>
        ///     Gets account information for peer if logged in.
        /// </summary>
        public UserAccount Account
        {
            get { return m_account; }
        }

        #endregion
        #region Private Methods

        /// <summary>
        ///     Registers client with database.
        /// </summary>
        private void RegisterClient()
        {
            DBConnection db = m_arbitrator.DatabaseConnection;
            string local_ip = m_connection.IPAddress;
            int local_port = m_connection.Port;
            int listen_port = m_listenPort;

            // Delete old client entries.
            DBResults results = db.Query(@"DELETE FROM {0} WHERE UNIX_TIMESTAMP()-last_active_timestamp > {1}",
                                          Settings.DB_TABLE_ACTIVE_CLIENTS,
                                          Settings.CLIENT_REGISTER_TIMEOUT_INTERVAL / 1000);

            // Does row already exist.
            results = db.Query(@"SELECT id FROM {0} WHERE ip_address='{1}' AND port={2}",
                                                   Settings.DB_TABLE_ACTIVE_CLIENTS,
                                                   local_ip,
                                                   local_port);

            // Update current record.
            if (results != null && results.RowsAffected > 0)
            {
                m_clientDatabaseID = (int)results[0]["id"];

                db.Query(@"UPDATE {0} SET 
                                        last_active_timestamp=UNIX_TIMESTAMP(), 
                                        arbitrator_id={1},
                                        listening={2},
                                        listen_port={3},
                                        zone_id={4}
                           WHERE id={5}",
                          Settings.DB_TABLE_ACTIVE_CLIENTS,
                          m_arbitrator.DatabaseID,
                          m_listening,
                          m_listenPort,
                          m_zoneID,
                          m_clientDatabaseID);
            }

            // Insert new record.
            else
            {
                results = db.Query(@"INSERT INTO {0}(
                                                        arbitrator_id, 
                                                        ip_address, 
                                                        port, 
                                                        last_active_timestamp,
                                                        listening,
                                                        listen_port,
                                                        zone_id
                                                    ) 
                                     VALUES         (
                                                        {1}, 
                                                        '{2}', 
                                                        {3}, 
                                                        UNIX_TIMESTAMP(),
                                                        {4},
                                                        {5},
                                                        {6}
                                                    )",
                                    Settings.DB_TABLE_ACTIVE_CLIENTS,
                                    m_arbitrator.DatabaseID,
                                    local_ip,
                                    local_port,
                                    m_listening,
                                    m_listenPort,
                                    m_zoneID);
                if (results != null)
                {
                    m_clientDatabaseID = results.LastInsertID;
                }
            }
        }

        /// <summary>
        ///     Unregisters client from database.
        /// </summary>
        private void UnRegisterClient()
        {
            DBConnection db = m_arbitrator.DatabaseConnection;
            string local_ip = m_connection.IPAddress;
            int local_port = m_connection.Port;

            db.Query(@"DELETE FROM {0} WHERE ip_address='{1}' AND port={2}",
                      Settings.DB_TABLE_ACTIVE_CLIENTS,
                      local_ip,
                      local_port); 
        }

        /// <summary>
        ///     Processes a packet that has been recieved from a client.
        /// </summary>
        /// <param name="packet">Packet that we recieved.</param>
        private void ProcessIncomingPacket(Packet packet)
        {
            // -----------------------------------------------------------------
            // Client is attempting to login.
            // -----------------------------------------------------------------
            if (packet is LoginPacket)
            {
                LoginPacket specificPacket = packet as LoginPacket;
                LoginResultPacket reply = new LoginResultPacket();

                reply.Result = LoginResult.Success;

                // Already logged into an account?
                if (m_account != null)
                {
                    reply.Result = LoginResult.AlreadyLoggedIn;
                }

                else
                {
                    UserAccount account = UserAccount.LoadByUsername(m_arbitrator.DatabaseConnection, specificPacket.Username);

                    // Account not found?
                    if (account == null)
                    {
                        reply.Result = LoginResult.AccountNotFound;
                    }

                    // Password invalid?
                    else if (account.Password != specificPacket.Password)
                    {
                        reply.Result = LoginResult.PasswordInvalid;
                    }

                    // Account already in us?
                    else
                    {
                        DBResults results = m_arbitrator.DatabaseConnection.Query(@"SELECT id FROM {0} WHERE account_id={1}",
                                                                                   Settings.DB_TABLE_ACTIVE_CLIENTS,
                                                                                   account.ID);

                        // Account already in us?
                        if (results.RowsAffected > 0)
                        {
                            reply.Result = LoginResult.AccountInUse;
                        }

                        // Success! Mark client as logged in.
                        else
                        {
                            m_arbitrator.DatabaseConnection.Query(@"UPDATE {0} SET account_id={1} WHERE id={2}",
                                                                    Settings.DB_TABLE_ACTIVE_CLIENTS,
                                                                    account.ID,
                                                                    m_clientDatabaseID);
                            m_account = account;
                        }
                    }
                }

                m_connection.SendPacket(reply, specificPacket);
            }

            // -----------------------------------------------------------------
            // Client is attempting to create an account.
            // -----------------------------------------------------------------
            else if (packet is CreateAccountPacket)
            {
                CreateAccountPacket specificPacket = packet as CreateAccountPacket;
                CreateAccountResultPacket reply = new CreateAccountResultPacket();

                reply.Result = CreateAccountResult.Success;

                // Email already exists :(
                if (UserAccount.LoadByEmail(m_arbitrator.DatabaseConnection, specificPacket.Email) != null)
                {
                    reply.Result = CreateAccountResult.EmailAlreadyExists;
                }
                else
                {
                    // Username already exists :(.
                    if (UserAccount.LoadByUsername(m_arbitrator.DatabaseConnection, specificPacket.Username) != null)
                    {
                        reply.Result = CreateAccountResult.UsernameAlreadyExists;
                    }

                    // Create account!
                    else
                    {
                        UserAccount.CreateAccount(m_arbitrator.Settings, m_arbitrator.DatabaseConnection,
                                                  specificPacket.Username,
                                                  specificPacket.Password,
                                                  specificPacket.Email);
                    }
                }

                m_connection.SendPacket(reply, specificPacket);
            }

            // -----------------------------------------------------------------
            // Client is attempting to register as listening and ready to be 
            // a superpeer.
            // -----------------------------------------------------------------
            else if (packet is RegisterAsListeningPacket)
            {
                RegisterAsListeningPacket specificPacket = packet as RegisterAsListeningPacket;
                RegisterAsListeningResultPacket reply = new RegisterAsListeningResultPacket();

                if (m_listening == true)
                {
                    reply.Result = RegisterAsListeningResult.AlreadyListening;
                }
                else if (m_account == null)
                {
                    reply.Result = RegisterAsListeningResult.NotLoggedIn;
                }
                else
                {
                    m_listenPort = specificPacket.Port;
                    m_listening = true;

                    reply.Result = RegisterAsListeningResult.Success;

                    // Update the client registration to include listening information.
                    RegisterClient();

                    // Send the peer a world grid.
                    SendUpdatedWorldGrid();

                    // Send account information to them.
                    UserAccountStatePacket p = new UserAccountStatePacket();
                    p.Account                = m_account.Clone();
                    p.Account.Email          = "";
                    p.Account.Password       = "";
                    p.ClientID               = (int)m_clientDatabaseID;
                    m_connection.SendPacket(p);
                }

                m_connection.SendPacket(reply, specificPacket);
            }

            // -----------------------------------------------------------------
            // Client has changed zone.
            // -----------------------------------------------------------------
            else if (packet is ChangeZonePacket)
            {
                ChangeZonePacket specificPacket = packet as ChangeZonePacket;

                Logger.Info("Client moved into zone #{0} from zone #{1}.", LoggerVerboseLevel.High, specificPacket.ZoneID, m_zoneID);

                // Update and save zone information to database.
                m_zoneID = specificPacket.ZoneID;
                RegisterClient();
            }

            // -----------------------------------------------------------------
            // SuperPeer wants account information for a given client.
            // -----------------------------------------------------------------
            else if (packet is SuperPeerRetrieveAccountPacket)
            {
                SuperPeerRetrieveAccountPacket specificPacket = packet as SuperPeerRetrieveAccountPacket;

                // TODO: Check client has authority to retrieve account information for given player.
                //       (eg. its a superpeer in an area the client is). Flag as cheating if not.

                Logger.Info("Client #{0} wants to retrieve account information for player #{1}.", LoggerVerboseLevel.High, m_clientDatabaseID, specificPacket.ClientID);
         
                // Load clients information from the database.
                DBResults client_info = m_arbitrator.DatabaseConnection.Query(@"SELECT `account_id` FROM {0} WHERE id={1}",
                                                                               Settings.DB_TABLE_ACTIVE_CLIENTS,
                                                                               specificPacket.ClientID);
                
                // TODO: Add error checking for when client-id is not valid.

                SuperPeerRetrieveAccountReplyPacket reply = new SuperPeerRetrieveAccountReplyPacket();
                reply.SuperPeerID       = specificPacket.SuperPeerID;
                reply.ClientID          = specificPacket.ClientID;
                if (client_info.RowsAffected > 0)
                {
                    reply.Account = UserAccount.LoadByID(m_arbitrator.DatabaseConnection, (int)client_info[0]["account_id"]);
                }
                else
                {
                    reply.Account = new UserAccount();
                    reply.Account.Username = "Disconnected";
                }
                reply.Account.Email     = "";
                reply.Account.Password  = "";
                m_connection.SendPacket(reply);
            }

            // -----------------------------------------------------------------
            // SuperPeer wants to store account information for a given client.
            // -----------------------------------------------------------------
            else if (packet is SuperPeerStoreAccountPacket)
            {
                SuperPeerStoreAccountPacket specificPacket = packet as SuperPeerStoreAccountPacket;

                // TODO: Check client has authority to store account information for given player.
                //       (eg. its a superpeer in an area the client is). Flag as cheating if not.

                Logger.Info("Client #{0} wants to store account information for player #{1} in zone #{2}.", LoggerVerboseLevel.High, m_clientDatabaseID, specificPacket.ClientID, specificPacket.ZoneID);

                StoreAccountRequest request = new StoreAccountRequest();
                request.SuperPeerID         = specificPacket.SuperPeerID;
                request.ClientID            = specificPacket.ClientID;
                request.ZoneID              = specificPacket.ZoneID;
                request.RecievedFrom        = this;
                request.Account             = specificPacket.Account;
                request.RecieveTime         = Environment.TickCount;
                request.Reason              = specificPacket.Reason;

                m_arbitrator.StoreAccountRequestRecieved(request);
            }

            // -----------------------------------------------------------------
            // Client wants to gracefully disconnect.
            // -----------------------------------------------------------------
            else if (packet is GracefulDisconnectPacket)
            {
                UnRegisterClient();

                GracefulDisconnectReplyPacket reply = new GracefulDisconnectReplyPacket();
                m_connection.SendPacket(reply, packet);
            }
        }

        #endregion
        #region Internal Methods

        /// <summary>
        ///     Serializes the current persistent state of this client to the database.
        /// </summary>
        internal void SerializePersistentStates()
        {
            if (m_account == null)
            {
                return;
            }

            m_account.Serialize(m_arbitrator.DatabaseConnection);
        }

        /// <summary>
        ///     Sends an updated version of the world grid to the peer.
        /// </summary>
        internal void SendUpdatedWorldGrid()
        {
            if (m_connection.Connected == true && m_connection.ConnectionEstablished == true)
            {
                ZoneGridPacket packet = m_arbitrator.ZoneGrid.ToPacket();
                m_connection.SendPacket(packet);
            }
        }

        #endregion
        #region Public Methods

        /// <summary>
        ///     Constructs a new instance of this class.
        /// </summary>
        /// <param name="arbitrator">Arbitrator that this peer is connected to.</param>
        /// <param name="peer_connection">Connection through which this peer is connected.</param>
        public ArbitratorPeer(ArbitratorService arbitrator, Connection peer_connection)
        {
            m_arbitrator = arbitrator;
            m_connection = peer_connection;
        }

        /// <summary>
        ///     Invoked when this peer is connected.
        /// </summary>
        public void Connected()
        {
            RegisterClient();
        }

        /// <summary>
        ///     Invoked when this peer is disconnected.
        /// </summary>
        public void Disconnected()
        {
            UnRegisterClient();
        }

        /// <summary>
        ///     Polls this peer connections, processing packets, updating registrations, etc.
        /// </summary>
        public void Poll()
        {
            // Update client registration.
            if (Environment.TickCount - m_lastRegisterTime > Settings.CLIENT_REGISTER_INTERVAL)
            {
                RegisterClient();
                m_lastRegisterTime = Environment.TickCount;
            }

            // Read in packets.
            Packet packet = m_connection.DequeuePacket();
            if (packet != null)
            {
                ProcessIncomingPacket(packet);
            }
        }

        #endregion
    }

}
