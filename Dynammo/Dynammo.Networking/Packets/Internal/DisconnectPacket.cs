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
    ///     Packet is sent when the user wants to disconnect from a service.
    ///     The server responds by disconnecting the user safely.
    /// </summary>
    public sealed class DisconnectPacket : Packet
    {
    }

}
