﻿using BicycleTripsPreparationApp;
using BikeTouringGIS.Controls;
using BikeTouringGIS.Messenges;
using BikeTouringGIS.Models;
using BikeTouringGISLibrary;
using Esri.ArcGISRuntime.Layers;
using GalaSoft.MvvmLight.Command;
using GPX;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BikeTouringGIS.ViewModels
{
    public class BikeTouringGISViewModel : BikeTouringGISBaseViewModel
    {
        private List<string> _loadedFiles = new List<string>();
        private int _splitLength;
        private bool _convertTracksToRoutesAutomatically;

        public RelayCommand<BikeTouringGISMapViewModel> OpenGPXFileCommand { get; private set; }
        public RelayCommand<SplitLayerProperties> ChangeSplitRouteCommand { get; private set; }
        public RelayCommand<BikeTouringGISLayer> FlipDirectionCommand { get; private set; }
        public RelayCommand<SplitLayerProperties> SplitRouteCommand { get; private set; }
        public RelayCommand<BikeTouringGISLayer> RemoveSplitRouteCommand { get; private set; }
        public RelayCommand<BikeTouringGISLayer> SaveSplitRouteCommand { get; private set; }
        public RelayCommand<BikeTouringGISLayer> SaveLayerCommand { get; private set; }
        public RelayCommand<BikeTouringGISLayer> CenterToLayerCommand { get; private set; }
        public RelayCommand<BikeTouringGISLayer> RemoveLayerCommand { get; private set; }
        public RelayCommand CenterCommand { get; private set; }

        public BikeTouringGISViewModel()
        {
            OpenGPXFileCommand = new RelayCommand<BikeTouringGISMapViewModel>(OpenGPXFile);
            FlipDirectionCommand = new RelayCommand<BikeTouringGISLayer>(FlipDirection);
            SplitRouteCommand = new RelayCommand<SplitLayerProperties>(SplitRoute);
            ChangeSplitRouteCommand = new RelayCommand<SplitLayerProperties>(SplitRoute, CanReSplitRoute);
            RemoveSplitRouteCommand = new RelayCommand<BikeTouringGISLayer>(RemoveSplitRoute);
            RemoveLayerCommand = new RelayCommand<BikeTouringGISLayer>(RemoveLayer);
            SaveSplitRouteCommand = new RelayCommand<BikeTouringGISLayer>(SaveSplitRoute);
            SaveLayerCommand = new RelayCommand<BikeTouringGISLayer>(SaveLayer);
            CenterToLayerCommand = new RelayCommand<BikeTouringGISLayer>(CenterMap);
            CenterCommand = new RelayCommand(() => CenterMap(null));
        }

        private void CenterMap(BikeTouringGISLayer obj)
        {
            MessengerInstance.Send(new ExtentChangedMessage() { Extent = obj?.Extent });
        }

        private void SaveLayer(BikeTouringGISLayer obj)
        {
            if(obj != null)
            {
                var gpxFile = new GPXFile();
                var gpx = new gpxType();
                var rte = new rteType();
                rte.name = obj.Title;
                rte.rtept = obj.ToRoute().Points.ToArray();
                gpx.rte = new List<rteType>() { rte }.ToArray();
                gpxFile.Save(obj.FileName, gpx);
                obj.IsInEditMode = false;
            }
        }

        private void SaveSplitRoute(BikeTouringGISLayer obj)
        {
            var baseDirectory = Path.GetDirectoryName(obj.FileName);
            int i = 1;
            foreach (var splitRoute in obj.SplitRoutes)
            {
                var filename = string.Format(@"{0}\{1}_{2}.gpx", baseDirectory, obj.SplitPrefix, i);
                var gpxFile = new GPXFile();
                var gpx = new gpxType();
                var rte = new rteType();
                rte.name = $"{i}_{obj.SplitPrefix}";
                rte.rtept = splitRoute.Points.ToArray();
                gpx.rte = new List<rteType>() { rte }.ToArray();
                gpxFile.Save(filename, gpx);
                i++;
            }
        }

        private void RemoveLayer(BikeTouringGISLayer layer)
        {
            _loadedFiles.Remove(layer.FileName);
            MessengerInstance.Send(new LayerRemovedMessage() { Layer = layer });
        }

        private void RemoveSplitRoute(BikeTouringGISLayer obj)
        {
            obj.RemoveSplitRoutes();
        }

        private bool CanReSplitRoute(SplitLayerProperties parameters)
        {
            return parameters.CanReSplit;
        }

        private void SplitRoute(SplitLayerProperties obj)
        {
            if (obj.CanSplit)
            {
                obj.Layer.SplitRoute(obj.Distance);
            }
        }

        public bool ConvertTracksToRoutesAutomatically
        {
            get { return _convertTracksToRoutesAutomatically; }
            set { Set(ref _convertTracksToRoutesAutomatically, value); }
        }

        public int SplitLength
        {
            get { return _splitLength; }
            set { Set(ref _splitLength, value); }
        }

        private void FlipDirection(BikeTouringGISLayer obj)
        {
            obj.FlipDirection();
        }

        private async void OpenGPXFile(BikeTouringGISMapViewModel mapViewModel)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "GPX files (*.gpx)|*.gpx";
            openFileDialog.Multiselect = true;
            openFileDialog.InitialDirectory = DropBoxHelper.GetDropBoxFolder();
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var file in openFileDialog.FileNames)
                {
                    if(_loadedFiles.Contains(file))
                    {
                        continue;
                    }
                    var gpxFileInformation = new GpxFileReader().LoadFile(file);
                    foreach (var track in gpxFileInformation.Tracks)
                    {
                        bool convertTrack = ConvertTracksToRoutesAutomatically;
                        if (!convertTrack)
                        {
                            StringBuilder textBuilder = new StringBuilder();
                            textBuilder.AppendLine($"Track {track.Name} is defined as track and not as route");
                            textBuilder.AppendLine();
                            textBuilder.AppendLine("routes are used by navigation-devices");
                            textBuilder.AppendLine("tracks are to register where you have been");
                            textBuilder.AppendLine();
                            textBuilder.AppendLine("Do you want to convert it to a route?");
                            convertTrack = await ConvertTrackToRoute(textBuilder.ToString());
                        }
                        if (convertTrack)
                        {
                            track.ConvertTrackToRoute();
                        }
                    }
                    _loadedFiles.Add(file);
                    gpxFileInformation.CreateGeometries();
                    foreach(IRoute route in gpxFileInformation.AllRoutes)
                    {
                        var layer = new BikeTouringGISLayer(file, route, route.Name);
                        mapViewModel.AddRoutes(layer);
                    }
                    mapViewModel.AddPoIs(gpxFileInformation.WayPoints);
                }
            }
        }

        private async Task<bool> ConvertTrackToRoute(string text)
        {
            var window = Application.Current.MainWindow as MetroWindow;
            var result = await window.ShowMessageAsync("Convert track to route", text, MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Affirmative)
            {
                return true;
            }
            return false;
        }

        public string VersionInformation
        {
            get
            {
                var versionApp = typeof(App).Assembly.GetName().Version;
                return string.Format("version {0}.{1}.{2}", versionApp.Major, versionApp.Minor, versionApp.Build);
            }
        }
    }
}