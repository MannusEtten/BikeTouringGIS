﻿using BikeTouringGIS.Controls;
using BikeTouringGISLibrary.Enumerations;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Symbology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinUX.Common;
using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Geometry;
using BikeTouringGIS.Extensions;
using WinUX;
using GalaSoft.MvvmLight.Command;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System.Windows;
using BikeTouringGISLibrary;
using System.Collections.ObjectModel;
using BikeTouringGIS.Messenges;
using BikeTouringGISLibrary.Model;

namespace BikeTouringGIS.ViewModels
{
    public class BikeTouringGISMapViewModel : BikeTouringGISBaseViewModel
    {
        public RelayCommand SetupMapCommand { get; private set; }
        public RelayCommand<BikeTouringGISLayer> LayerLoadedCommand { get; private set; }
        private BikeTouringGISLayer _pointsOfInterestLayer;
        private ObservableCollection<BikeTouringGISLayer> _bikeTouringGISLayers;
        private Dictionary<GraphicType, object> _mapSymbols;
        private bool _showKnooppunten, _showOpenCycleMap, _showOpenStreetMap, _mapSetupIsDone;
        private int _totalLengthOfRoutes;
        public BikeTouringGISMapViewModel()
        {
            SetupMapCommand = new RelayCommand(SetupMap);
            LayerLoadedCommand = new RelayCommand<BikeTouringGISLayer>(LayerLoaded);
            _mapSymbols = new Dictionary<GraphicType, object>();
            MessengerInstance.Register<ExtentChangedMessage>(this, SetNewExtent);
            MessengerInstance.Register<LayerRemovedMessage>(this, LayerRemoved);
        }

        private void SetNewExtent(ExtentChangedMessage obj)
        {
            if(obj.Extent == null)
            {
                SetExtent();
            }
            else
            {
                MapView.SetView(obj.Extent.Expand(1.1));
            }
        }

        private void LayerRemoved(LayerRemovedMessage message)
        {
            var layer = message.Layer;
            var splitLayer = layer.SplitLayer;
            _map.Layers.Remove(layer);
            _map.Layers.Remove(splitLayer);
            BikeTouringGISLayers.Remove(layer);
            SetExtent();
            CalculateTotalLength();
            PlacePointsOfInterestLayerOnTop();
        }

        private void LayerLoaded(BikeTouringGISLayer layer)
        {
            if (layer != null)
            {
                BikeTouringGISLayers = new ObservableCollection<BikeTouringGISLayer>(_map.GetBikeTouringGISLayers());
            }
        }

        public MapView MapView { get; internal set; }
        public int TotalLengthOfRoutes
        {
            get { return _totalLengthOfRoutes; }
            set { Set(ref _totalLengthOfRoutes, value); }
        }

        public ObservableCollection<BikeTouringGISLayer> BikeTouringGISLayers
        {
            get { return _bikeTouringGISLayers; }
            set { Set(ref _bikeTouringGISLayers, value); }
        }

        private void SetupMap()
        {
            if (!_mapSetupIsDone)
            {
                ShowOpenCycleMap = false;
                ShowOpenStreetMap = true;
                ShowKnooppunten = false;
                _pointsOfInterestLayer = new BikeTouringGISLayer("Points of Interest");
                _map.Layers.Add(_pointsOfInterestLayer);
                _map.Layers.ForEach(x => x.ShowLegend = false);
                _mapSetupIsDone = true;
            }
        }

        //TODO MME 30012017 checken of in Quartz de binding wel goed werkt!
        public bool ShowOpenCycleMap
        {
            get { return _showOpenCycleMap; }
            set
            {
                Set(ref _showOpenCycleMap, value);
                var osm = _map.Layers?["opencyclemap"] as OpenStreetMapLayer;
                osm.IsVisible = value;
            }
        }

        internal void AddRoutes(BikeTouringGISLayer layer)
        {
            layer.SetSymbolsAndSplitLayerDefaultProperties(_mapSymbols);
            _map.Layers.Add(layer);
            _map.Layers.Add(layer.SplitLayer);
            SetExtent();
            CalculateTotalLength();
            PlacePointsOfInterestLayerOnTop();
        }

        private void PlacePointsOfInterestLayerOnTop()
        {
            var poiLayer = _map.GetBikeTouringGISLayers().FirstOrDefault(x => x.Type == LayerType.PointsOfInterest);
            _map.Layers.Remove(poiLayer);
            _map.Layers.Add(poiLayer);
        }

        //TODO MME 30012017 checken of in Quartz de binding wel goed werkt!
        public bool ShowOpenStreetMap
        {
            get { return _showOpenStreetMap; }
            set
            {
                Set(ref _showOpenStreetMap, value);
                var osm = _map.Layers?["openstreetmap"] as OpenStreetMapLayer;
                osm.IsVisible = value;
            }
        }
        //TODO MME 30012017 checken of in Quartz de binding wel goed werkt!
        public bool ShowKnooppunten
        {
            get { return _showKnooppunten; }
            set
            {
                Set(ref _showKnooppunten, value);
                var wms = _map.Layers?["fietsknooppunten"] as WmsLayer;
                wms.IsVisible = value;
            }
        }

        internal void AddSymbol(GraphicType typeOfSymbol, object symbol)
        {
            _mapSymbols.Add(typeOfSymbol, symbol);
        }

        private void CalculateTotalLength()
        {
            var length = 0;
            _map.GetBikeTouringGISLayers().ForEach(x => length += x.TotalLength);
            TotalLengthOfRoutes = length;
        }

        private void SetExtent()
        {
            Envelope initialExtent = null;
            foreach (var layer in _map.GetBikeTouringGISLayers())
            {
                    initialExtent = initialExtent == null ? initialExtent = layer.Extent : initialExtent = initialExtent.Union(layer.Extent);
            }
            if(initialExtent != null)
            {
                MapView.SetView(initialExtent.Expand(1.2));
            }
        }

        internal void AddPoIs(List<WayPoint> wayPoints)
        {
            wayPoints.ForEach(x => _pointsOfInterestLayer.Graphics.Add(x.ToGraphic()));
            _pointsOfInterestLayer.SetSymbolsAndSplitLayerDefaultProperties(_mapSymbols);
            
        }
    }
}