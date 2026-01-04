using UnityEngine;
using System.Collections.Generic;

namespace GoldRush.Simulation
{
    public struct ForceZone
    {
        public int MinX, MaxX, MinY, MaxY;  // Bounds (inclusive)
        public Vector2 Force;               // Force applied per frame
        public object Owner;                // Reference to owning infrastructure (for unregistration)
    }

    public class ForceZoneManager
    {
        private static ForceZoneManager _instance;
        public static ForceZoneManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ForceZoneManager();
                return _instance;
            }
        }

        private List<ForceZone> zones = new List<ForceZone>();

        public void RegisterZone(ForceZone zone)
        {
            zones.Add(zone);
        }

        public void UnregisterZone(object owner)
        {
            zones.RemoveAll(z => z.Owner == owner);
        }

        public void UnregisterZone(ForceZone zone)
        {
            zones.Remove(zone);
        }

        public Vector2 GetNetForce(int x, int y)
        {
            Vector2 totalForce = Vector2.zero;

            for (int i = 0; i < zones.Count; i++)
            {
                ForceZone zone = zones[i];
                if (x >= zone.MinX && x <= zone.MaxX &&
                    y >= zone.MinY && y <= zone.MaxY)
                {
                    totalForce += zone.Force;
                }
            }

            return totalForce;
        }

        public bool HasForceAt(int x, int y)
        {
            for (int i = 0; i < zones.Count; i++)
            {
                ForceZone zone = zones[i];
                if (x >= zone.MinX && x <= zone.MaxX &&
                    y >= zone.MinY && y <= zone.MaxY)
                {
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            zones.Clear();
        }

        public int ZoneCount => zones.Count;
    }
}
