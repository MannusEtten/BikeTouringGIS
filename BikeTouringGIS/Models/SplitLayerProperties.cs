﻿using BikeTouringGIS.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeTouringGIS.Models
{
    public class SplitLayerProperties
    {
        public BikeTouringGISLayer Layer { get; set; }
        public int Distance { get; set; }

        public bool CanSplit
        {
            get { return Distance < Layer.TotalLength && Distance > 0; }
        }

        public bool CanReSplit
        {
            get { return Layer.IsSplitted && Distance > 0; }
        }
    }
}