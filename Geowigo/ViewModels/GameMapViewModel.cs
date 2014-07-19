using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls.Maps;
using System.Collections.Generic;
using WF.Player.Core;
using System.Collections.ObjectModel;
using Geowigo.Utils;
using System.Diagnostics;
using System.ComponentModel;
using System.Device.Location;

namespace Geowigo.ViewModels
{
	public class GameMapViewModel : BaseViewModel, INotifyPropertyChanged
	{
		#region Nested Classes

		public class MapViewRequestedEventArgs : EventArgs
		{
			public LocationRect TargetBounds { get; private set; }

			public GeoCoordinate TargetCenter { get; private set; }

			public double TargetZoomLevel { get; private set; }

			public MapViewRequestedEventArgs(LocationRect locRect)
			{
				TargetBounds = locRect;
			}

			public MapViewRequestedEventArgs(GeoCoordinate center, double zoomLevel)
			{
				TargetCenter = center;
				TargetZoomLevel = zoomLevel;
			}
		}

		#endregion

		#region Properties

		#region ZonePolygons
		private IEnumerable<MapPolygon> _ZonePolygons;
		public IEnumerable<MapPolygon> ZonePolygons
		{
			get
			{
				if (_ZonePolygons == null)
				{
					_ZonePolygons = new List<MapPolygon>();
				}

				return _ZonePolygons;
			}

			private set
			{
				if (value != _ZonePolygons)
				{
					_ZonePolygons = value;

					RaisePropertyChanged("ZonePolygons");
				}
			}
		} 
		#endregion

		#region PlayerLocation

		private Pushpin _PlayerPushpin;
		public Pushpin PlayerPushpin
		{
			get
			{
				return _PlayerPushpin;
			}

			private set
			{
				if (value != _PlayerPushpin)
				{
					_PlayerPushpin = value;

					RaisePropertyChanged("PlayerPushpin");
				}
			}
		}

		#endregion

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		public event EventHandler<MapViewRequestedEventArgs> MapViewRequested;

		#endregion

		#region Constants

		public const double DEFAULT_ZOOM_LEVEL = 16d;

		public const double MAX_AUTO_ZOOM_LEVEL = 18d;

		#endregion

		#region Fields

		private Brush _polygonFillBrush;
		private Brush _polygonBorderBrush;

		#endregion

		public GameMapViewModel()
		{
			// Inits the brushes.
			_polygonFillBrush = new SolidColorBrush(Colors.Cyan) { Opacity = 0.25 };
			_polygonBorderBrush = new SolidColorBrush(Colors.White);
		}

		protected override void InitFromNavigation(BaseViewModel.NavigationInfo nav)
		{
			base.InitFromNavigation(nav);

			// Refreshes the map content.
			RefreshZones();

			// Refreshes the player.
			RefreshPlayer();

			// Refreshes the bounds of the map.
			RefreshBounds();
		}

		protected override void OnCorePropertyChanged(string propName)
		{
			if (propName == "ActiveVisibleZones")
			{
				RefreshZones();
			}
			else if (propName == "DeviceLocation")
			{
				RefreshPlayer();
			}
		}

		private void RefreshPlayer()
		{
			// Gets the current position of the device.
			GeoCoordinate playerPos = Model.Core.DeviceLocation;

			// Makes a pushpin if there is a position of the device,
			// removes it otherwise.
			if (playerPos == null)
			{
				PlayerPushpin = null;
			}
			else
			{
				// Creates or refreshes the pushpin.
				if (PlayerPushpin == null)
				{
					PlayerPushpin = new Pushpin()
					{
						Content = Model.Core.Player.Name,
						Location = playerPos
					};
				}
				else
				{
					PlayerPushpin.Location = playerPos;
				}
			}
		}

		private void RefreshBounds()
		{
			// Gets the current bounds of the engine.
			CoordBounds bounds = Model.Core.Bounds;

			// Are the bounds valid?
			// YES -> Computes a view around them.
			// NO -> Computes a default view around the starting location
			// of the cartridge or the current location of the player.
			MapViewRequestedEventArgs e = null;
			if (bounds != null && bounds.IsValid)
			{
				e = new MapViewRequestedEventArgs(
					new LocationRect(bounds.Top, bounds.Left, bounds.Bottom, bounds.Right)
					);
			}
			else
			{
				// Is the cartridge play anywhere?
				// YES -> Try to get the player's location as center.
				// NO -> Sets the starting location of the cartridge as center.
				if (Model.Core.Cartridge.IsPlayAnywhere)
				{
					// If there is a current location of the player, use it as center.
					// If not, do nothing more.
					GeoCoordinate deviceLoc = Model.Core.DeviceLocation;
					if (deviceLoc != null && !deviceLoc.IsUnknown)
					{
						e = new MapViewRequestedEventArgs(deviceLoc, DEFAULT_ZOOM_LEVEL);
					}
				}
				else
				{
					// Gets the starting location.
					Cartridge cart = Model.Core.Cartridge;
					System.Device.Location.GeoCoordinate startLoc =
						new System.Device.Location.GeoCoordinate(
							cart.StartingLocationLatitude,
							cart.StartingLocationLongitude);

					// Makes the event.
					e = new MapViewRequestedEventArgs(startLoc, DEFAULT_ZOOM_LEVEL);
				}
			}

			// If an event was prepared, send it.
			if (MapViewRequested != null)
			{
				MapViewRequested(this, e);
			}
		}

		private void RefreshZones()
		{
			List<MapPolygon> polygons = new List<MapPolygon>();

			// Creates a map polygon for each zone.
			foreach (Zone zone in Model.Core.ActiveVisibleZones)
			{
				// Converts the zone points to a location collection.
				LocationCollection locations = new LocationCollection();
				foreach (ZonePoint point in zone.Points)
				{
					locations.Add(new System.Device.Location.GeoCoordinate(point.Latitude, point.Longitude, point.Altitude));
				}

				// Creates and adds the polygon.
				polygons.Add(new MapPolygon() { 
					Locations = locations, 
					Fill = _polygonFillBrush,
					Stroke = _polygonBorderBrush,
					StrokeThickness = 2
				});
			}

			// Refreshes the collection of polygons.
			ZonePolygons = polygons;
		}

		private void RaisePropertyChanged(string propName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
			}
		}
	}
}
