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
    ///     Used to specify the result of a login acount action.
    /// </summary> 
    public enum LoginResult
    {
        Success,
        AccountNotFound,
        PasswordInvalid,
        AlreadyLoggedIn,
        AccountInUse,
        Failed
    }

    /// <summary>
    ///     Sent by a client to initialize a login request with an arbitrator.
    /// </summary>
    public sealed class LoginPacket : Packet
    {
        public string Username;
        public string Password;
    }

    /// <summary>
    ///     Sent in reply to a LoginPacket, telling a game-client the result of the login.
    /// </summary>
    public sealed class LoginResultPacket : Packet
    {
        public LoginResult Result;
    }

}
