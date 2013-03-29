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

namespace Dynammo.Networking
{

    /// <summary>
    ///     Send from a super peer to an arbitrator to retrieve account information.
    /// </summary>
    public class SuperPeerRetrieveAccountPacket : SuperPeerClientPacket
    {
    }

    /// <summary>
    ///     Send from arbitrator to super peer with account information.
    /// </summary>
    public class SuperPeerRetrieveAccountReplyPacket : SuperPeerClientPacket
    {
        public UserAccount Account;
    }
}
