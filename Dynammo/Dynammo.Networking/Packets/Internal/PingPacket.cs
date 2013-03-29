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
    ///     Send to the remote side of a connection to request a PongPacket, used to measure
    ///     the amount of latency between sides of the connection.
    /// </summary>
    public sealed class PingPacket : Packet
    {
    }

}
