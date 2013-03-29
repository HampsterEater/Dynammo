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
    ///     Packet sent on the application layer for graceful disconnects, allows the
    ///     other end of the connection to delay the peers disconnection until it wants to 
    ///     send the reply. Typically this is used to make sure we process all the peers packets
    ///     before disconnection occurs.
    /// </summary>
    public sealed class GracefulDisconnectPacket : Packet
    {
    }

    /// <summary>
    ///     Reply to above packet.
    /// </summary>
    public sealed class GracefulDisconnectReplyPacket : Packet
    {
    }

}
