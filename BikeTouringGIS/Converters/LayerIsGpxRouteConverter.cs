﻿using BikeTouringGIS.Controls;
using BikeTouringGISLibrary.Enumerations;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BikeTouringGIS.Converters
{
    public class LayerIsGpxRouteConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var layer = (BikeTouringGISLayer)value;
            return layer.Type == LayerType.GPXRoute ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}