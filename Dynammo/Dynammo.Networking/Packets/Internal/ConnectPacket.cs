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
    ///     Packet is sent when the user wants to connect to another remote host.
    ///     Connection will not be processed until this is recieved, it contains the
    ///     properties required to get everything up and running (encryption, hardware id, etc).
    /// </summary>
    public sealed class ConnectPacket : Packet
    {
        public byte[]   HardwareFingerprint;
        public byte[]   ConnectionGUID;

        public string   ComputerName;
        public string   ComputerUserName;
    }

}
