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
    ///     Stores information about a zone grid super-peer in a format that can 
    ///     be sent by a ZoneGridPacket.
    /// </summary>
    public struct ZoneGridPacketSuperPeerInfo
    {
        public int    ID;
        public int    ZoneID;
        public int    ClientID;
        public string ClientIPAddress;
        public int    ClientListenPort;
    }

    /// <summary>
    ///     Stores information about a zone grid in a format that can 
    ///     be sent by a ZoneGridPacket.
    /// </summary>
    public struct ZoneGridPacketZoneInfo
    {
        public int                              ID;
        public int                              ParentID;
        public int                              ChildZone1ID;
        public int                              ChildZone2ID;
        public ZoneSplitOrientation             SplitOrientation;

        public ZoneGridPacketSuperPeerInfo[]    SuperPeers;
    }

    /// <summary>
    ///     Sent by an arbitrator to a client to given them the current state of the zone-grid.
    /// </summary>
    public sealed class ZoneGridPacket : Packet
    {
        public ZoneGridPacketZoneInfo[] Zones;
    }

}
