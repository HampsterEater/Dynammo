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
    ///     Represents an individual request by a superpeer to store changes to a users account 
    ///     in persistent storage. These requests are stacked up by the arbitrator and are
    ///     only acted upon if enough request from multiple superpeers are the same.
    /// </summary>
    public class StoreAccountRequest
    {
        #region Members

        private int m_superpeer_id = 0;
        private int m_client_id = 0;
        private int m_zone_id = 0;
        private ArbitratorPeer m_recieved_from = null;
        private UserAccount m_account = null;
        private int m_recieve_time = 0;
        public string m_reason = "";

        #endregion
        #region Properties

        /// <summary>
        ///     Gets or sets the ID of the superpeer that this request was recieved from.
        /// </summary>
        public int SuperPeerID
        {
            get { return m_superpeer_id; }
            set { m_superpeer_id = value; }
        }

        /// <summary>
        ///     Gets or sets the ID of the client that this request is storing an account for.
        /// </summary>
        public int ClientID
        {
            get { return m_client_id; }
            set { m_client_id = value; }
        }

        /// <summary>
        ///     Gets or sets the ID of the zone this storage request was initiated by.
        /// </summary>
        public int ZoneID
        {
            get { return m_zone_id; }
            set { m_zone_id = value; }
        }

        /// <summary>
        ///     Gets or sets the arbitrator this request was recieved from.
        /// </summary>
        public ArbitratorPeer RecievedFrom
        {
            get { return m_recieved_from; }
            set { m_recieved_from = value; }
        }
        /// <summary>
        ///     Gets or sets the account that is being requested to be stored.
        /// </summary>
        public UserAccount Account
        {
            get { return m_account; }
            set { m_account = value; }
        }

        /// <summary>
        ///     Gets or sets the time this request was recieved.
        /// </summary>
        public int RecieveTime
        {
            get { return m_recieve_time; }
            set { m_recieve_time = value; }
        }

        /// <summary>
        ///     Gets or sets meta data describing the reason this account store was requested.
        /// </summary>
        public string Reason
        {
            get { return m_reason; }
            set { m_reason = value; }
        }

        #endregion
        #region Methods

        #endregion
    }

}
