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
    ///     Send from a super peer to an arbitrator to store account information.
    /// </summary>
    public class SuperPeerStoreAccountPacket : SuperPeerClientPacket
    {
        public UserAccount Account;
        public int ZoneID;
        public string Reason;
    }

    /// <summary>
    ///     Send from arbitrator to super peer after storing account information.
    /// </summary>
    public class SuperPeerStoreAccountReplyPacket : SuperPeerClientPacket
    {
        public bool Success;
        public bool Failed;
    }

}
