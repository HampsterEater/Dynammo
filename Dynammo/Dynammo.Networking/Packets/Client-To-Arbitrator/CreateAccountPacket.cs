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
    ///     Used to specify the result of a create account action.
    /// </summary>
    public enum CreateAccountResult
    {
        Success,
        UsernameAlreadyExists,
        EmailAlreadyExists,
        Failed,
    }

    /// <summary>
    ///     Sent by a client to initialize a account creation request with an arbitrator.
    /// </summary>
    public sealed class CreateAccountPacket : Packet
    {
        public string Username;
        public string Password;
        public string Email;
    }

    /// <summary>
    ///     Sent in reply to a CreateAccountPacket, telling a game-client the result of the account creation.
    /// </summary>
    public sealed class CreateAccountResultPacket : Packet
    {
        public CreateAccountResult Result;
    }

}
