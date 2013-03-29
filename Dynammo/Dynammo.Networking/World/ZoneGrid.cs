/// -----------------------------------------------------------------
///  Dynamic MMO (Dynammo)
///  Dissertation project at University of Derby
/// -----------------------------------------------------------------
///  Written by Timothy Leonard
/// -----------------------------------------------------------------
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynammo.Common;

namespace Dynammo.Networking
{

    /// <summary>
    ///     Determines in which direction a zone is split.
    /// </summary>
    public enum ZoneSplitOrientation
    {
        Horizontal  = 0,
        Vertical    = 1
    }

    /// <summary>
    ///     Represents an individual zone within a zone grid.
    /// </summary>
    public class Zone
    {
        #region Private Members

        private int                     m_id;
        
        private int                     m_child_zone_1_id;
        private Zone                    m_child_zone_1;

        private int                     m_child_zone_2_id;
        private Zone                    m_child_zone_2;

        private ZoneSplitOrientation    m_split_orientation;

        private List<ZoneSuperPeer>     m_super_peers = new List<ZoneSuperPeer>();

        private int                     m_parent_id = 0;
        private Zone                    m_parent = null;

        #endregion
        #region Properties

        /// <summary>
        ///     Gets or sets the ID of this zone.
        /// </summary>
        public int ID
        {
            get { return m_id; }
            set { m_id = value; }
        }

        /// <summary>
        ///     Gets or sets the parent-ID of this zone.
        /// </summary>
        public int ParentID
        {
            get { return m_parent_id; }
            set { m_parent_id = value; }
        }

        /// <summary>
        ///     Gets or sets the parent of this zone.
        /// </summary>
        public Zone Parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        /// <summary>
        ///     Gets or sets the first child zone's ID.
        /// </summary>
        public int ChildZone1ID
        {
            get { return m_child_zone_1_id; }
            set { m_child_zone_1_id = value; }
        }

        /// <summary>
        ///     Gets or sets the first child zone.
        /// </summary>
        public Zone ChildZone1
        {
            get { return m_child_zone_1; }
            set { m_child_zone_1 = value; }
        }

        /// <summary>
        ///     Gets or sets the second child zone's ID.
        /// </summary>
        public int ChildZone2ID
        {
            get { return m_child_zone_2_id; }
            set { m_child_zone_2_id = value; }
        }

        /// <summary>
        ///     Gets or sets the second child zone.
        /// </summary>
        public Zone ChildZone2
        {
            get { return m_child_zone_2; }
            set { m_child_zone_2 = value; }
        }

        /// <summary>
        ///     Gets or sets the split orientation of this zone.
        /// </summary>
        public ZoneSplitOrientation SplitOrientation
        {
            get { return m_split_orientation; }
            set { m_split_orientation = value; }
        }

        /// <summary>
        ///     Returns true if this is a leaf zone (one that is not further split).
        /// </summary>
        public bool IsLeafZone
        {
            get { return (m_child_zone_2 == null && m_child_zone_1 == null); }
        }

        /// <summary>
        ///     Gets a list of super peers responsible for updating this zone.
        /// </summary>
        public List<ZoneSuperPeer> SuperPeers
        {
            get { return m_super_peers; }
        }

        #endregion
        #region Public Methods

        /// <summary>
        ///     Constructs a new zone instances with the given properties.
        /// </summary>
        /// <param name="id">Numeric ID of zone.</param>
        /// <param name="parent_id">Numeric ID of parent zone.</param>
        /// <param name="child_zone_1_id">Numeric ID of child zone 1.</param>
        /// <param name="child_zone_2_id">Numeric ID of child zone 2.</param>
        /// <param name="split_orientation">Orientation of split for this zone.</param>
        public Zone(int id, int parent_id, int child_zone_1_id, int child_zone_2_id, ZoneSplitOrientation split_orientation)
        {
            m_id                    = id;
            m_parent_id             = parent_id;
            m_child_zone_1_id       = child_zone_1_id;
            m_child_zone_2_id       = child_zone_2_id;
            m_split_orientation     = split_orientation;
        }

        #endregion
    }

    /// <summary>
    ///     This class is responsible for storing and manipulating the current zoning information for the world.
    /// </summary>
    public class ZoneGrid
    {
        #region Members

        private Dictionary<int, Zone>   m_zones             = new Dictionary<int, Zone>();
        private Dictionary<int, Zone>   m_allZones          = new Dictionary<int, Zone>(); // All zones we have ever encountered.
        private Zone                    m_rootZone          = null;

        private List<ZoneSuperPeer>     m_gained_superpeers = new List<ZoneSuperPeer>();
        private List<ZoneSuperPeer>     m_lost_superpeers   = new List<ZoneSuperPeer>();

        #endregion
        #region Properties

        /// <summary>
        ///     Gets the root zone of this zone grid.
        /// </summary>
        public Zone RootZone
        {
            get { return m_rootZone; }
        }

        /// <summary>
        ///     Gets a list of all zones in this grid.
        /// </summary>
        public List<Zone> Zones
        {
            get
            {
                List<Zone> zones = new List<Zone>();
                foreach (Zone zone in m_zones.Values)
                {
                    zones.Add(zone);
                }
                return zones;
            }
        }

        /// <summary>
        ///     Gets a list of super-peers who have gained control during the last
        ///     zone-grid update.
        /// </summary>
        public List<ZoneSuperPeer> GainedSuperPeers
        {
            get { return m_gained_superpeers; }
        }

        /// <summary>
        ///     Gets a list of super-peers who have lost control during the last
        ///     zone-grid update.
        /// </summary>
        public List<ZoneSuperPeer> LostSuperPeers
        {
            get { return m_lost_superpeers; }
        }


        #endregion
        #region Private Methods

        /// <summary>
        ///     Calculates the bounding rectangle of the given zone based on a given world size and position.
        /// </summary>
        /// <param name="zone">Zone to calculate bounds for.</param>
        /// <param name="target_zone">Target zone to calculate bounds for.</param>
        /// <param name="world_x">X position of world.</param>
        /// <param name="world_y">Y position of world.</param>
        /// <param name="world_w">Width of world.</param>
        /// <param name="world_h">Height of world.</param>
        /// <param name="zone_x">Resulting zone boundry X position.</param>
        /// <param name="zone_y">Resulting zone boundry Y position.</param>
        /// <param name="zone_w">Resulting zone boundry width.</param>
        /// <param name="zone_h">Resulting zone boundry height.</param>
        private bool CalculateZoneBoundsInternal(Zone zone, Zone target_zone, int world_x, int world_y, int world_w, int world_h, out int zone_x, out int zone_y, out int zone_w, out int zone_h)
        {
            if (zone == target_zone)
            {
                zone_x = world_x;
                zone_y = world_y;
                zone_w = world_w;
                zone_h = world_h;
                return true;
            }

            // Is this split?
            if (zone.ChildZone1 != null || zone.ChildZone2 != null)
            {
                if (zone.SplitOrientation == ZoneSplitOrientation.Horizontal)
                {
                    if (CalculateZoneBoundsInternal(zone.ChildZone1, target_zone, world_x, world_y, world_w / 2, world_h, out zone_x, out zone_y, out zone_w, out zone_h) == true)
                    {
                        return true;
                    }
                    if (CalculateZoneBoundsInternal(zone.ChildZone2, target_zone, world_x + (world_w / 2), world_y, world_w / 2, world_h, out zone_x, out zone_y, out zone_w, out zone_h) == true)
                    {
                        return true;
                    }
                }
                else
                {
                    if (CalculateZoneBoundsInternal(zone.ChildZone1, target_zone, world_x, world_y, world_w, world_h / 2, out zone_x, out zone_y, out zone_w, out zone_h) == true)
                    {
                        return true;
                    }
                    if (CalculateZoneBoundsInternal(zone.ChildZone2, target_zone, world_x, world_y + (world_h / 2), world_w, world_h / 2, out zone_x, out zone_y, out zone_w, out zone_h) == true)
                    {
                        return true;
                    }
                }
            }

            zone_x = 0;
            zone_y = 0;
            zone_w = 0;
            zone_h = 0;
            return false;
        }

        #endregion
        #region Public Methods

        /// <summary>
        ///     Converts this grid into a format that can be sent as a packet.
        /// </summary>
        /// <returns>Packet that can be sent to recreate this zone grid on another peer.</returns>
        public ZoneGridPacket ToPacket()
        {
            ZoneGridPacket packet = new ZoneGridPacket();

            packet.Zones = new ZoneGridPacketZoneInfo[m_zones.Count];
            for (int i = 0; i < m_zones.Count; i++)
            {
                Zone zone = m_zones[m_zones.Keys.ToArray()[i]];

                packet.Zones[i] = new ZoneGridPacketZoneInfo();
                packet.Zones[i].ID = zone.ID;
                packet.Zones[i].ParentID = zone.ParentID;
                packet.Zones[i].ChildZone1ID = zone.ChildZone1ID;
                packet.Zones[i].ChildZone2ID = zone.ChildZone2ID;
                packet.Zones[i].SplitOrientation = zone.SplitOrientation;

                packet.Zones[i].SuperPeers = new ZoneGridPacketSuperPeerInfo[zone.SuperPeers.Count];
                for (int k = 0; k < zone.SuperPeers.Count; k++)
                {
                    ZoneSuperPeer peer = zone.SuperPeers[k];

                    packet.Zones[i].SuperPeers[k].ID                = peer.ID;
                    packet.Zones[i].SuperPeers[k].ZoneID            = peer.ZoneID;
                    packet.Zones[i].SuperPeers[k].ClientID          = peer.ClientID;
                    packet.Zones[i].SuperPeers[k].ClientIPAddress   = peer.ClientIPAddress;
                    packet.Zones[i].SuperPeers[k].ClientListenPort  = peer.ClientListenPort;
                }
            }

            return packet;
        }

        /// <summary>
        ///     Converts a packet representation of this zone grid into the zone grid.
        /// </summary>
        /// <param name="packet">Packet to convert.</param>
        public void FromPacket(ZoneGridPacket packet)
        {
            List<ZoneSuperPeer> old_superpeers = new List<ZoneSuperPeer>();
            List<ZoneSuperPeer> new_superpeers = new List<ZoneSuperPeer>();
            List<Zone> old_zones = new List<Zone>(m_zones.Values);

            // Make a full list of all super peers that currently exist.
            foreach (Zone zone in m_zones.Values)
            {
                old_superpeers.AddRange(zone.SuperPeers);
            }

            // Dispose of general stuff.
            m_zones.Clear();
            m_gained_superpeers.Clear();
            m_lost_superpeers.Clear();

            // Rebuild the zone grid.
            foreach (ZoneGridPacketZoneInfo zoneInfo in packet.Zones)
            {
                Zone zone = null;

                foreach (Zone z in old_zones)
                {
                    if (z.ID == zoneInfo.ID)
                    {
                        zone = z;
                        zone.ParentID = zoneInfo.ParentID;
                        zone.ChildZone1ID = zoneInfo.ChildZone1ID;
                        zone.ChildZone2ID = zoneInfo.ChildZone2ID;
                        zone.Parent = null;
                        zone.ChildZone1 = null;
                        zone.ChildZone2 = null;
                        zone.SplitOrientation = zoneInfo.SplitOrientation;
                        break;
                    }
                }

                if (zone == null)
                {
                    zone = new Zone(zoneInfo.ID, zoneInfo.ParentID, zoneInfo.ChildZone1ID, zoneInfo.ChildZone2ID, zoneInfo.SplitOrientation);
                }

                List<ZoneSuperPeer> oldZoneSuperPeers = new List<ZoneSuperPeer>(zone.SuperPeers);
                zone.SuperPeers.Clear();

                foreach (ZoneGridPacketSuperPeerInfo peerInfo in zoneInfo.SuperPeers)
                {
                    ZoneSuperPeer peer = null;

                    foreach (ZoneSuperPeer p in oldZoneSuperPeers)
                    {
                        if (p.ID == peerInfo.ID)
                        {
                            peer = p;
                            break;
                        }
                    }

                    if (peer == null)
                    {
                        peer = new ZoneSuperPeer();
                    }

                    peer.ID                 = peerInfo.ID;
                    peer.ZoneID             = peerInfo.ZoneID;
                    peer.ClientID           = peerInfo.ClientID;
                    peer.ClientIPAddress    = peerInfo.ClientIPAddress;
                    peer.ClientListenPort   = peerInfo.ClientListenPort;

                    zone.SuperPeers.Add(peer);
                }

                new_superpeers.AddRange(zone.SuperPeers);

                AddZone(zone);
            }

            // Calculate which super-peers are new.
            foreach (ZoneSuperPeer peer in old_superpeers)
            {
                bool found = false;
                foreach (ZoneSuperPeer peer2 in new_superpeers)
                {
                    if (peer.ZoneID == peer2.ZoneID &&
                        peer.ClientID == peer2.ClientID)
                    {
                        found = true;
                        break;
                    }
                }

                if (found == false)
                {
                    m_lost_superpeers.Add(peer);
                }
            }

            // Calculate which super-peers have been list.
            foreach (ZoneSuperPeer peer in new_superpeers)
            {
                bool found = false;
                foreach (ZoneSuperPeer peer2 in old_superpeers)
                {
                    if (peer.ZoneID == peer2.ZoneID &&
                        peer.ClientID == peer2.ClientID)
                    {
                        found = true;
                        break;
                    }
                }

                if (found == false)
                {
                    m_gained_superpeers.Add(peer);
                }
            }
        }

        /// <summary>
        ///     Clears the grid of zones.
        /// </summary>
        public void Clear()
        {
            m_zones.Clear();
        }

        /// <summary>
        ///     Adds the given zone to this zone grid.
        /// </summary>
        /// <param name="zone">Zone to add.</param>
        public void AddZone(Zone zone)
        {
            // Add to zones dictionary.
            if (m_zones.ContainsKey(zone.ID))
            {
                m_zones.Remove(zone.ID);
            }
            m_zones.Add(zone.ID, zone);

            // Add to all zones dictionary.
            if (m_allZones.ContainsKey(zone.ID))
            {
                m_allZones.Remove(zone.ID);
            }
            m_allZones.Add(zone.ID, zone);

            // Link up zones.
            int lowest_id = -1;
            foreach (int key in m_zones.Keys)
            {
                Zone z = m_zones[key];

                if (lowest_id == -1 || z.ID < lowest_id)
                {
                    lowest_id = z.ID;
                }

                if (z.ChildZone1ID != 0)
                {
                    if (m_zones.ContainsKey(z.ChildZone1ID))
                    {
                        z.ChildZone1 = m_zones[z.ChildZone1ID];
                    }
                    else
                    {
                        z.ChildZone1 = z;
                    }
                } 
                if (z.ChildZone2ID != 0)
                {
                    if (m_zones.ContainsKey(z.ChildZone2ID))
                    {
                        z.ChildZone2 = m_zones[z.ChildZone2ID];
                    }
                    else
                    {
                        z.ChildZone2 = z;
                    }
                }
                if (z.ParentID != 0)
                {
                    if (m_zones.ContainsKey(z.ParentID))
                    {
                        z.Parent = m_zones[z.ParentID];
                    }
                    else
                    {
                        z.Parent = z;
                    }
                }
            }

            // Store root zone.
            if (lowest_id != -1)
            {
                if (m_zones.ContainsKey(lowest_id))
                {
                    m_rootZone = m_zones[lowest_id];
                }
                else
                {
                    m_rootZone = null;
                }
            }
            else
            {
                m_rootZone = null;
            }
        }

        /// <summary>
        ///     Removes the given zone from this grid.
        /// </summary>
        /// <param name="zone">Zone to remove.</param>
        public void RemoveZone(Zone zone)
        {
            // Remove the zone.
            for (int i = 0; i < m_zones.Count; i++)
            {
                int key       = m_zones.Keys.ElementAt(i);
                Zone sub_zone = m_zones[key];

                if (sub_zone.ID == zone.ID)
                {
                    m_zones.Remove(key);
                }
            }

            // Remove references from all other zones.
            foreach (Zone z in m_zones.Values)
            {
                if (z.ChildZone1 == zone)
                {
                    z.ChildZone1 = null;
                    z.ChildZone1ID = 0;
                }
                if (z.ChildZone2 == zone)
                {
                    z.ChildZone2 = null;
                    z.ChildZone2ID = 0;
                }
                if (z.Parent == zone)
                {
                    z.Parent = null;
                    z.ParentID = 0;
                }
            }

            if (m_rootZone == zone)
            {
                m_rootZone = null;
            }
        }

        /// <summary>
        ///     Gets a zone based on if it contains the given X/Y coordinate.
        /// </summary>
        /// <param name="world_x">World X position.</param>
        /// <param name="world_y">World Y position.</param>
        /// <param name="world_w">World width.</param>
        /// <param name="world_h">World height.</param>
        /// <param name="x">X Coordinate.</param>
        /// <param name="y">Y Coordinate.</param>
        /// <returns>Zone that contains the point, otherwise null.</returns>
        public Zone GetZoneByPosition(int world_x, int world_y, int world_w, int world_h, int x, int y)
        {
            int zone_x = 0, zone_y = 0, zone_w = 0, zone_h = 0;

            for (int i = 0; i < 3; i++)
            {
                foreach (Zone zone in m_zones.Values)
                {
                    if (zone.IsLeafZone == false)
                    {
                        continue;
                    }

                    CalculateZoneBounds(zone, world_x, world_y, world_w, world_h, out zone_x, out zone_y, out zone_w, out zone_h);

                    if (MathHelper.RectContains(zone_x, zone_y, zone_w + i, zone_h + i, x, y))
                    {
                        return zone;
                    }
                }
            }

            throw new InvalidOperationException("Position was outside of the zone grid.");
        }

        /// <summary>
        ///     Gets a zone based on its ID.
        /// </summary>
        /// <param name="id">ID of zone to get.</param>
        /// <returns>Zone if it exists, else null.</returns>
        public Zone GetZoneByID(int id)
        {
            if (m_zones.ContainsKey(id))
            {
                return m_zones[id];
            }
            if (m_allZones.ContainsKey(id))
            {
                return m_allZones[id];
            }
            if (id <= 0)
            {
                return null;
            }
            return null;
        }

        /// <summary>
        ///     Calculates the bounding rectangle of the given zone based on a given world size and position.
        /// </summary>
        /// <param name="zone">Zone to calculate bounds for.</param>
        /// <param name="world_x">X position of world.</param>
        /// <param name="world_y">Y position of world.</param>
        /// <param name="world_w">Width of world.</param>
        /// <param name="world_h">Height of world.</param>
        /// <param name="zone_x">Resulting zone boundry X position.</param>
        /// <param name="zone_y">Resulting zone boundry Y position.</param>
        /// <param name="zone_w">Resulting zone boundry width.</param>
        /// <param name="zone_h">Resulting zone boundry height.</param>
        public void CalculateZoneBounds(Zone zone, int world_x, int world_y, int world_w, int world_h, out int zone_x, out int zone_y, out int zone_w, out int zone_h)
        {
            zone_x = 0;
            zone_y = 0;
            zone_w = 0;
            zone_h = 0;

            CalculateZoneBoundsInternal(m_rootZone, zone, world_x, world_y, world_w, world_h, out zone_x, out zone_y, out zone_w, out zone_h);
        }

        #endregion
    }

}
