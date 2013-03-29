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
using System.Data;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using Dynammo.Common;

namespace Dynammo.Networking
{

    /// <summary>
    ///     Used to store an individual row retrieved from a database query.
    /// </summary>
    public class DBRow
    {
        #region Members

        private Dictionary<string, object> m_values = new Dictionary<string, object>();

        #endregion
        #region Properties

        /// <summary>
        ///     Allows the user to get or set the value of any fields in this row.
        /// </summary>
        /// <param name="key">Name of field to retrieve.</param>
        /// <returns>Value of field, or null if none available.</returns>
        public object this[string key]
        {
            get
            {
                if (m_values.ContainsKey(key))
                {
                    return m_values[key];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                m_values[key] = value;
            }
        }

        /// <summary>
        ///     Returns a list of column names stored in this row.
        /// </summary>
        public List<string> ColumnNames
        {
            get
            {
                List<string> list = new List<string>();
                foreach (string key in m_values.Keys)
                {
                    list.Add(key);
                }
                return list;
            }
        }

        #endregion
        #region Private Methods

        #endregion
        #region Public Methods

        #endregion
    }

    /// <summary>
    ///     Used to store results for a given database query.
    /// </summary>
    public class DBResults
    {
        #region Members

        private int     m_rowsAffected = 0;
        private long    m_lastInsertID = 0;
        private DBRow[] m_rows         = new DBRow[16];

        #endregion
        #region Properties

        /// <summary>
        ///     Allows the user to get at row by zero-based index.
        /// </summary>
        /// <param name="key">Index of row to recieve.</param>
        /// <returns>Row at given index.</returns>
        public DBRow this[int key]
        {
            get
            {
                return m_rows[key];
            }
        }


        /// <summary>
        ///     Gets the number of rows the last command affected.
        /// </summary>
        public int RowsAffected
        {
            get
            {
                return m_rowsAffected;
            }
        }

        /// <summary>
        ///     Gets the primary ID of the last row inserted.
        /// </summary>
        public long LastInsertID
        {
            get
            {
                return m_lastInsertID;
            }
        }

        #endregion
        #region Private Methods

        #endregion
        #region Public Methods

        /// <summary>
        ///     Constructs an instance of this class.
        /// </summary>
        /// <param name="command">Command these results are for.</param>
        /// <param name="reader">Data reader to extract results with.</param>
        public DBResults(MySqlCommand command, MySqlDataReader reader)
        {
            m_rowsAffected = reader.RecordsAffected;
            m_lastInsertID = command.LastInsertedId;

            if (reader.HasRows == true)
            {
                m_rowsAffected = 0;

                // Read in each row.
                while (reader.Read())
                {
                    // Make a new row instance to hold information about this row.
                    DBRow row = new DBRow();

                    // Get all values.
                    object[] values = new object[reader.FieldCount];
                    reader.GetValues(values);

                    // Enter values into array based on column names.
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string name = reader.GetName(i);
                        row[name] = values[i] is System.DBNull ? null : values[i];
                    }

                    // Insert into rows array.
                    if (m_rowsAffected >= m_rows.Length)
                    {
                        DBRow[] newArray = new DBRow[m_rows.Length * 2];
                        Array.Copy(m_rows, newArray, m_rows.Length);
                        m_rows = newArray;
                    }
                    m_rows[m_rowsAffected++] = row;
                }
            }
        }

        #endregion
    }

    /// <summary>
    ///     Responsible for connecting, disconnecting and send/recieving data
    ///     from a remote mySQL database.
    /// </summary>
    public class DBConnection
    {
        #region Members

        // Connection state.
        private MySqlConnection     m_connection;
        private bool                m_connecting;
        private bool                m_connected;
        private bool                m_reconnecting;

        private string              m_serverIP;
        private ushort              m_serverPort;
        private string              m_dbName;
        private string              m_username;
        private string              m_password;

        #endregion
        #region Properties

        /// <summary>
        ///     Gets if this connection is connected or not.
        /// </summary>
        public bool Connected
        {
            get { return m_connected; }
        }

        /// <summary>
        ///     Gets if this connection is connecting or not.
        /// </summary>
        public bool Connecting
        {
            get { return m_connecting; }
        }

        /// <summary>
        ///     Gets if this connection is reconnecting or not.
        /// </summary>
        public bool Reconnecting
        {
            get { return m_reconnecting; }
        }

        #endregion
        #region Private Methods

        /// <summary>
        ///     Disposes of the socket if it exists.
        /// </summary>
        private void DisposeSocket()
        {
            if (m_connection == null)
            {
                return;
            }

            m_connection.StateChange -= Connection_StateChange;

            try
            {
                if (m_connection.State != ConnectionState.Closed)
                {
                    m_connection.Close();
                }
            }
            catch (MySql.Data.MySqlClient.MySqlException)
            {
                // Ignore the error, its not important, we're closing down. But not it anyway.
                Logger.Error("Recieved SQL exception whilst disposing of old connection.", LoggerVerboseLevel.Normal);
            }

            m_connection     = null;
            m_connected      = false;
            m_connecting     = false;
            m_reconnecting   = false;
        }

        /// <summary>
        ///     Invoked when the database connection changes.
        /// </summary>
        /// <param name="sender">Sender that invoked this.</param>
        /// <param name="e">Arguments of invokation.</param>
        private void Connection_StateChange(object sender, StateChangeEventArgs e)
        {
            m_connected  = (e.CurrentState == ConnectionState.Open      ||
                            e.CurrentState == ConnectionState.Executing ||
                            e.CurrentState == ConnectionState.Fetching);
            m_connecting = (e.CurrentState == ConnectionState.Connecting);
        }

        #endregion
        #region Public Methods

        /// <summary>
        ///     Deconstructs all connection resources.
        /// </summary>
        ~DBConnection()
        {
            DisposeSocket();
        }

        /// <summary>
        ///     Reconnects to the server if we have been booted for some reason.
        /// </summary>
        public async Task<bool> ReconnectAsync()
        {
            // If we are already reconnecting then ignore.
            if (m_reconnecting == true)
            {
                return false;
            }
            m_reconnecting = true;

            Logger.Error("Attempting to perform database reconnection ...", LoggerVerboseLevel.Normal);

            // Keep attempting to reconnect.
            long reconnect_timeout = Environment.TickCount + Settings.CONNECTION_RECONNECT_DURATION;
            int exponential_backoff = Settings.CONNECTION_RECONNECT_BACKOFF_START;

            while (Environment.TickCount < reconnect_timeout)
            {
                bool success = await ConnectAsync(m_serverIP, m_serverPort, m_dbName, m_username, m_password);
                if (m_connected == true && success == true)
                {
                    break;
                }

                // Exponentially backoff so we don't flood the server with connections.
                await Task.Delay(exponential_backoff);
                exponential_backoff = Math.Min(Settings.CONNECTION_RECONNECT_BACKOFF_MAX, exponential_backoff * Settings.CONNECTION_RECONNECT_BACKOFF_MULTIPLIER);

                Logger.Error("Database reconnection failed, attempting again in {0}ms.", LoggerVerboseLevel.Normal, exponential_backoff);
            }

            if (m_connected == false)
            {
                Logger.Error("Failed to reconnect to database after disconnect.", LoggerVerboseLevel.Normal);
            }

            m_reconnecting = false;

            return m_connected;
        }

        /// <summary>
        ///     Connects to a service running on another host.
        /// </summary>
        /// <param name="ip">IP address of the server to connect to.</param_
        /// <param name="port">Port number that service is running on.</param>
        /// <param name="name">Database name to connect to.</param>
        /// <param name="username">Username to use when connecting.</param>
        /// <param name="password">Password to use when connecting.</param>
        public async Task<bool> ConnectAsync(string ip, ushort port, string name, string username, string password)
        {
            if (m_connection != null)
            {
                DisposeSocket();
            }

            m_connecting = true;
            m_connection = new MySqlConnection("server=" + ip + ";database=" + name + ";uid=" + username + ";pwd=" + password + ";");
            m_connection.StateChange += Connection_StateChange;

            try
            {
                m_connection.Open();
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                Logger.Error(ex.Message, LoggerVerboseLevel.Normal);

                DisposeSocket();
                return false;
            }

            // Wait until we are connected.
            while (m_connection.State == ConnectionState.Connecting)
            {
                await Task.Yield();
            }

            // Store connection settings incase with have to reconnect.
            m_serverIP      = ip;
            m_serverPort    = port;
            m_dbName        = name;
            m_username      = username;
            m_password      = password;

            m_connecting = false; 
            m_connected  = (m_connection.State == ConnectionState.Open      ||
                            m_connection.State == ConnectionState.Executing ||
                            m_connection.State == ConnectionState.Fetching);

            return m_connected;
        }

        /// <summary>
        ///     Disconnects from the host.
        /// </summary>
        /// <returns>True if the host was force disconnected.</returns>
        public async Task<bool> DisconnectAsync()
        {
            DisposeSocket();
            return true;
        }

        /// <summary>
        ///     Performs a query on this database connection.
        /// </summary>
        /// <returns>A DBResults instances that contains the results of this query, or null if the query failed.</returns>
        public DBResults Query(string query, params object[] formatParam)
        {
            query = String.Format(query, formatParam);

            try
            {
                MySqlCommand    currentCommand           = new MySqlCommand(query, m_connection);
                MySqlDataReader currentCommandDataReader = currentCommand.ExecuteReader() as MySqlDataReader;

                DBResults results = new DBResults(currentCommand, currentCommandDataReader);

                currentCommandDataReader.Close();

                return results;
            }
            catch (System.Data.Common.DbException ex)
            {
                Logger.Error("Database query failed with error: {0}", LoggerVerboseLevel.Normal, ex.Message);
                return null;
            }
        }

        /// <summary>
        ///     Performs a query on this database connection with mysql parameterisation.
        /// </summary>
        /// <returns>A DBResults instances that contains the results of this query, or null if the query failed.</returns>
        public DBResults QueryParameterized(string query, object[] parameters, params object[] formatParam)
        {
            query = String.Format(query, formatParam);

            try
            {
                MySqlCommand currentCommand = new MySqlCommand(query, m_connection);

                int index = 1;
                foreach (object param in parameters)
                {
                    currentCommand.Parameters.Add("@parameter_" + index, param);
                    index++;
                }

                MySqlDataReader currentCommandDataReader = currentCommand.ExecuteReader() as MySqlDataReader;
                DBResults results = new DBResults(currentCommand, currentCommandDataReader);
                currentCommandDataReader.Close();

                return results;
            }
            catch (System.Data.Common.DbException ex)
            {
                Logger.Error("Database query failed with error: {0}", LoggerVerboseLevel.Normal, ex.Message);
                return null;
            }
        }


        #endregion
    }

}
