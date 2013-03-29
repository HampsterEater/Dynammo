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
    ///     Used to specify the result of a register-as-listening action.
    /// </summary> 
    public enum RegisterAsListeningResult
    {
        Success,
        NotLoggedIn,
        AlreadyListening,
        Failed
    }

    /// <summary>
    ///     Sent by a client to tell an arbitrator that we are listening and are available
    ///     for being setup as a super peer.
    /// </summary>
    public sealed class RegisterAsListeningPacket : Packet
    {
        public int Port;
    }

    /// <summary>
    ///     Sent in reply to a RegisterAsListeningPacket, telling a game-client the result.
    /// </summary>
    public sealed class RegisterAsListeningResultPacket : Packet
    {
        public RegisterAsListeningResult Result;
    }
}
