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
    ///     Send to a super peer to unregister for events from it.
    /// </summary>
    public class SuperPeerUnregisterPacket : SuperPeerPacket
    {
        public int ChangeZoneSuperPeerCount;
    }

    /// <summary>
    ///     Send from a super peer in reply to SuperPeerUnregisterPacket.
    /// </summary>
    public class SuperPeerUnregisterReplyPacket : SuperPeerPacket
    {
    }

}
