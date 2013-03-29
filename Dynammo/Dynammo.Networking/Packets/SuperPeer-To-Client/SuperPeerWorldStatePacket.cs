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
using Dynammo.Common;

namespace Dynammo.Networking
{


    /// <summary>
    ///     Stores information on an individual player state within the world.
    /// </summary>
    public struct SuperPeerWorldStatePlayerInfo
    {
        public int ClientID;
        public UserAccount Account;
    }

    /// <summary>
    ///     Sent from a super peer to the client to update their current view of the world.
    /// </summary>
    public class SuperPeerWorldStatePacket : SuperPeerPacket
    {
        public SuperPeerWorldStatePlayerInfo[] Peers;
    }

}
