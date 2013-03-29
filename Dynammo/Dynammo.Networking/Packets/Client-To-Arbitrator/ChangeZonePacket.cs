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
    ///     Sent by a client to an arbitrator to inform the arbitrator that we have changed to another zone.
    /// </summary>
    public sealed class ChangeZonePacket : Packet
    {
        public int ZoneID;
    }

}
