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
    ///     Send to the remote side of a connection after receipt of a PingPacket, used to measure
    ///     the amount of latency between sides of the connection.
    /// </summary>
    public sealed class PongPacket : Packet
    {
    }

}
