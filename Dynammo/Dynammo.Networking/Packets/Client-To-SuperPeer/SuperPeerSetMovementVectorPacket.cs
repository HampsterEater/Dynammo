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
    ///     Sent from a client to a superpeer to tell them of the current movement vector
    ///     we are moving along.
    /// </summary>
    public class SuperPeerSetMovementVectorPacket : SuperPeerPacket
    {
        public float VectorX;
        public float VectorY;
        public float Speed;
    }

}
