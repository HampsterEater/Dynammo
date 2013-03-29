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
    ///     Represents a packet send to a super peer, that specifically pertains to the given
    ///     client ID.
    /// </summary>
    public class SuperPeerClientPacket : SuperPeerPacket
    {
        public int ClientID;
    }

}
