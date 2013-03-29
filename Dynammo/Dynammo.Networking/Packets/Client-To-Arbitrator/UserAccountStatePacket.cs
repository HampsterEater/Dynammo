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
    ///     Sent to a client to update their local copy of their account persistent state.
    /// </summary>
    public sealed class UserAccountStatePacket : Packet
    {
        public UserAccount Account;
        public int ClientID;
    }

}
