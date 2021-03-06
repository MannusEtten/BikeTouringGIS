﻿using BikeTouringGISLibrary.GPX;

namespace BikeTouringGISLibrary.Model
{
    public class WayPoint : GeometryData
    {
        public decimal Lat { get; private set; }
        public decimal Lon { get; private set; }
        public string Source { get; internal set; }

        public static implicit operator wptType(WayPoint waypoint)
        {
            var wptType = new wptType
            {
                name = waypoint.Name,
                lon = waypoint.Lon,
                lat = waypoint.Lat
            };
            return wptType;
        }
    }
}