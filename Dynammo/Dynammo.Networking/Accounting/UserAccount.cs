/// -----------------------------------------------------------------
///  Dynamic MMO (Dynammo)
///  Dissertation project at University of Derby
/// -----------------------------------------------------------------
///  Written by Timothy Leonard
/// -----------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Dynammo.Common;
using Dynammo.Networking;

namespace Dynammo.Networking
{

    /// <summary>
    ///     Stores and retrieves information on a users account from the database.
    /// </summary>
    public sealed class UserAccount
    {
        #region Private Members

        private UserAccountPersistentState   m_persistent_state      = new UserAccountPersistentState();
        private int                          m_last_login_timestamp  = 0;
        private string                       m_username              = "";
        private string                       m_password              = "";
        private string                       m_email                 = "";
        private int                          m_id                    = 0;

        #endregion
        #region Properties

        /// <summary>
        ///     Gets the internal database ID for this user account.
        /// </summary>
        public int ID
        {
            get { return m_id; }
        }

        /// <summary>
        ///     Gets the username associated with this account.
        /// </summary>
        public string Username
        {
            get { return m_username; }
            set { m_username = value; }
        }

        /// <summary>
        ///     Gets the password associated with this account.
        /// </summary>
        public string Password
        {
            get { return m_password; }
            set { m_password = value; }
        }

        /// <summary>
        ///     Gets the email associated with this account.
        /// </summary>
        public string Email
        {
            get { return m_email; }
            set { m_email = value; }
        }

        /// <summary>
        ///     Gets the users last login time as a unix timestamp.
        /// </summary>
        public int LastLoginTimestamp
        {
            get { return m_last_login_timestamp; }
        }

        /// <summary>
        ///     Gets the persistent state of the users account.
        /// </summary>
        public UserAccountPersistentState PeristentState
        {
            get { return m_persistent_state; }
        }

        #endregion
        #region Private Methods

        /// <summary>
        ///     Loads account default settings for this user account.
        /// </summary>
        /// <param name="settings">Settings used to get account settings from.</param>
        private void LoadDefaults(Settings settings)
        {
            if (settings.ClientStartX == 0)
            {
                m_persistent_state.X = RandomHelper.RandomInstance.Next(0, settings.WorldWidth - 1);
            }
            else
            {
                m_persistent_state.X = settings.ClientStartX;
            }

            if (settings.ClientStartY == 0)
            {
                m_persistent_state.Y = RandomHelper.RandomInstance.Next(0, settings.WorldHeight - 1);
            }
            else
            {
                m_persistent_state.Y = settings.ClientStartY;
            }
        }

        #endregion
        #region Public methods
        
        /// <summary>
        ///     Serializes this user account into the database.
        /// </summary>
        /// <param name="database">Database to serialize into.</param>
        public void Serialize(DBConnection database)
        {
            DBResults results = null;

            results = database.Query(@"SELECT id FROM {0} WHERE username='{1}'",
                                                  Settings.DB_TABLE_ACCOUNTS,
                                                  StringHelper.Escape(m_username));

            byte[] persistentState = m_persistent_state.Serialize();

            // Already exists?
            if (results.RowsAffected > 0)
            {
                results = database.QueryParameterized(@"UPDATE {0} SET 
                                                            username='{1}',
                                                            password='{2}',
                                                            email='{3}',
                                                            last_login_timestamp=UNIX_TIMESTAMP(),
                                                            persistent_state=@parameter_1
                                                       WHERE
                                                            username='{4}'",
                                        new object[] { persistentState },
                                        Settings.DB_TABLE_ACCOUNTS,
                                        StringHelper.Escape(m_username.ToLower()),
                                        StringHelper.Escape(m_password.ToLower()),
                                        StringHelper.Escape(m_email.ToLower()),
                                        StringHelper.Escape(m_username.ToLower()));
            }

            // New account?
            else
            {
                results = database.QueryParameterized(@"INSERT INTO {0}
                                                            (username, password, email, last_login_timestamp, persistent_state) 
                                                        VALUES
                                                            ('{1}', '{2}', '{3}', UNIX_TIMESTAMP(), @parameter_1)",
                                            new object [] { persistentState },
                                            Settings.DB_TABLE_ACCOUNTS,
                                            StringHelper.Escape(m_username.ToLower()),
                                            StringHelper.Escape(m_password.ToLower()),
                                            StringHelper.Escape(m_email.ToLower()));
            }
        }

        /// <summary>
        ///     Loads a user account that has the given username from the database.
        /// </summary>
        /// <param name="database">Database to load username from.</param>
        /// <param name="username">Username of account to load.</param>
        /// <returns>Account loaded, or null if one dosen't exist.</returns>
        public static UserAccount LoadByUsername(DBConnection database, string username)
        {
            DBResults results = database.Query(@"SELECT id, username, password, email, last_login_timestamp, persistent_state FROM {0} WHERE LOWER(`username`)='{1}'",
                                                Settings.DB_TABLE_ACCOUNTS,
                                                StringHelper.Escape(username.ToLower()));

            if (results.RowsAffected > 0)
            {
                DBRow row = results[0];

                UserAccount account = new UserAccount();
                account.m_id                    = (int)row["id"];
                account.m_username              = row["username"].ToString();
                account.m_password              = row["password"].ToString();
                account.m_email                 = row["email"].ToString();
                account.m_last_login_timestamp  = row["last_login_timestamp"] == null ? 0 : (int)row["last_login_timestamp"];
                account.m_persistent_state      = new UserAccountPersistentState((byte[])row["persistent_state"]);

                return account;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        ///     Loads a user account that has the given email from the database.
        /// </summary>
        /// <param name="database">Database to load username from.</param>
        /// <param name="email">email of account to load.</param>
        /// <returns>Account loaded, or null if one dosen't exist.</returns>
        public static UserAccount LoadByEmail(DBConnection database, string email)
        {
            DBResults results = database.Query(@"SELECT id, username, password, email, last_login_timestamp, persistent_state FROM {0} WHERE LOWER(`email`)='{1}'",
                                                Settings.DB_TABLE_ACCOUNTS,
                                                StringHelper.Escape(email.ToLower()));

            if (results.RowsAffected > 0)
            {
                DBRow row = results[0];

                UserAccount account = new UserAccount();
                account.m_id                        = (int)row["id"];
                account.m_username                  = row["username"].ToString();
                account.m_password                  = row["password"].ToString();
                account.m_email                     = row["email"].ToString();
                account.m_last_login_timestamp      = row["last_login_timestamp"] == null ? 0 : (int)row["last_login_timestamp"];
                account.m_persistent_state          = new UserAccountPersistentState((byte[])row["persistent_state"]);

                return account;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        ///     Loads a user account that has the given id from the database.
        /// </summary>
        /// <param name="database">Database to load username from.</param>
        /// <param name="id">ID of account to load.</param>
        /// <returns>Account loaded, or null if one dosen't exist.</returns>
        public static UserAccount LoadByID(DBConnection database, int id)
        {
            DBResults results = database.Query(@"SELECT id, username, password, email, last_login_timestamp, persistent_state FROM {0} WHERE id={1}",
                                                Settings.DB_TABLE_ACCOUNTS,
                                                id);

            if (results.RowsAffected > 0)
            {
                DBRow row = results[0];

                UserAccount account = new UserAccount();
                account.m_id                    = (int)row["id"];
                account.m_username              = row["username"].ToString();
                account.m_password              = row["password"].ToString();
                account.m_email                 = row["email"].ToString();
                account.m_last_login_timestamp  = row["last_login_timestamp"] == null ? 0 : (int)row["last_login_timestamp"];
                account.m_persistent_state      = new UserAccountPersistentState((byte[])row["persistent_state"]);

                return account;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        ///     Create a user account with the given information.
        /// </summary>
        /// <param name="settings">Settings used to initialize this account.</param>
        /// <param name="database">Database to load username from.</param>
        /// <param name="username">Username of account to load.</param>
        /// <returns>Account loaded, or null if one dosen't exist.</returns>
        public static UserAccount CreateAccount(Settings settings, DBConnection database, string username, string password, string email)
        {
            DBResults results = database.Query(@"SELECT id FROM {0} WHERE LOWER(`username`)='{1}'",
                                                Settings.DB_TABLE_ACCOUNTS,
                                                StringHelper.Escape(username.ToLower()));

            if (results.RowsAffected <= 0)
            {
                UserAccount account = new UserAccount();
                account.m_id                    = (int)results.LastInsertID;
                account.m_username              = username;
                account.m_password              = password;
                account.m_email                 = email;
                account.m_last_login_timestamp  = 0;
                account.m_persistent_state      = new UserAccountPersistentState();

                account.LoadDefaults(settings);

                account.Serialize(database);

                return account;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        ///     Creates a shallow clone of this object.
        /// </summary>
        /// <returns>Clone of this object.</returns>
        public UserAccount Clone()
        {
            return this.MemberwiseClone() as UserAccount;
        }

        #endregion
    }

}
