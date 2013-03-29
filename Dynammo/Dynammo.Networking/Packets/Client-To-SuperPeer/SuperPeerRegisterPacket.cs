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
    ///     Send to a super peer to register for events from it.
    /// </summary>
    public class SuperPeerRegisterPacket : SuperPeerPacket
    {
        public int ClientID;
    }

    /// <summary>
    ///     Send from a super peer in reply to SuperPeerRegisterPacket.
    /// </summary>
    public class SuperPeerRegisterReplyPacket : SuperPeerPacket
    {
    }

}
