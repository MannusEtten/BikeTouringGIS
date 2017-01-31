﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Symbology;
using GPX;
using Esri.ArcGISRuntime.Layers;
using BikeTouringGISLibrary;
using Esri.ArcGISRuntime.Geometry;
using BikeTouringGISLibrary.Enumerations;

namespace BikeTouringGIS
{
    class RouteSplitter
    {
        private List<wptType> _wayPoints;
        private List<double> _distances;
        private List<List<wptType>> _routes = new List<List<wptType>>();
        private List<wptType> _splitPoints = new List<wptType>();
        private List<int> _splitIndices = new List<int>();
        private int _splitDistance;

        public RouteSplitter(int splitDistance)
        {
            _splitDistance = splitDistance;
        }

        private void CreateSplittedRoutes()
        {
            var allIndices = _splitIndices.ToList();
            allIndices.Insert(0, 0);
            allIndices.Add(_wayPoints.Count - 1);
            for (int i = 0; i < allIndices.Count - 1;i++)
            {
                _routes.Add(_wayPoints.GetRange(allIndices[i], (allIndices[i+1] - allIndices[i]) + 1));
            }
        }

        private void CalculateSplitPoints()
        {
            _splitIndices.Clear();
            var length = 0.0;
            var splitCounter = 1;
            for (int i = 0; i < _wayPoints.Count - 1; i++)
            {
                length += _distances[i];
                var temporaryLength = _splitDistance * 1000 * splitCounter;
                if (length > temporaryLength)
                {
                    var lengthAfter = length - temporaryLength;
                    var lengthBefore = temporaryLength - (length - _distances[i]);
                    if (lengthAfter > lengthBefore)
                    {
                        _splitIndices.Add(i);
                    }
                    else
                    {
                        _splitIndices.Add(i - 1);
                    }
                    splitCounter++;
                }
            }
        }

        private void CalculateOriginalDistances()
        {
            var dataAnalyzer = new DataAnalyzer(_wayPoints);
            _distances = dataAnalyzer.Distances;
        }

        internal void SplitRoute(List<wptType> points)
        {
            _wayPoints = points;
            CalculateOriginalDistances();
            CalculateSplitPoints();
            _splitIndices.ForEach(x => _splitPoints.Add(_wayPoints[x]));
            CreateSplittedRoutes();
        }

        internal IEnumerable<BikeTouringGISGraphic> GetSplitPoints()
        {
            var result = new List<BikeTouringGISGraphic>();
            var sr = new SpatialReference(4326);
            var cumulativeDistance = 0;
            for(int i =0; i < _splitPoints.Count; i++)
            {
                var point = _splitPoints[i];
                var mapPoint = new MapPoint((double)point.lon, (double)point.lat, sr);
                var graphic = new BikeTouringGISGraphic(mapPoint, GraphicType.SplitPoint);
                cumulativeDistance += (int)_distances[i];
                graphic.Attributes["distance"] = cumulativeDistance;
                result.Add(graphic);
            }
            return result;
        }

        internal IEnumerable<BikeTouringGISGraphic> GetSplittedRoutes()
        {
            return new List<BikeTouringGISGraphic>();
        }
    }
}