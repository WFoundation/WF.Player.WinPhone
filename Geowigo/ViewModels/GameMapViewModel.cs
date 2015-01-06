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
using System.Linq;
using Geowigo.Controls;

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

		#region ZoneLabels

		private IEnumerable<Pushpin> _ZoneLabels;
		public IEnumerable<Pushpin> ZoneLabels
		{
			get
			{
				if (_ZoneLabels == null)
				{
					_ZoneLabels = new List<Pushpin>();
				}

				return _ZoneLabels;
			}

			private set
			{
				if (value != _ZoneLabels)
				{
					_ZoneLabels = value;

					RaisePropertyChanged("ZoneLabels");
				}
			}
		} 

		#endregion

		#region PlayerPushpin

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

		#region PlayerAccuracyPolygon

		private MapPolygon _PlayerAccuracyPolygon;
		public MapPolygon PlayerAccuracyPolygon
		{
			get
			{
				return _PlayerAccuracyPolygon;
			}

			private set
			{
				if (value != _PlayerAccuracyPolygon)
				{
					_PlayerAccuracyPolygon = value;

					RaisePropertyChanged("PlayerAccuracyPolygon");
				}
			}
		}

		#endregion

		#region ThingsPushpins
		private IEnumerable<Pushpin> _ThingPushpins;
		public IEnumerable<Pushpin> ThingPushpins
		{
			get
			{
				if (_ThingPushpins == null)
				{
					_ThingPushpins = new List<Pushpin>();
				}

				return _ThingPushpins;
			}

			private set
			{
				if (value != _ThingPushpins)
				{
					_ThingPushpins = value;

					RaisePropertyChanged("ThingPushpins");
				}
			}
		} 
		#endregion

		#endregion

		#region Commands

		#region ShowThingDetailsCommand

		private ICommand _ShowThingDetailsCommand;

		public ICommand ShowThingDetailsCommand
		{
			get 
			{ 
				return _ShowThingDetailsCommand ?? (_ShowThingDetailsCommand = new RelayCommand<Thing>(ShowThingDetails)); 
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

		private const int ACCURACY_CIRCLE_SAMPLES = 50;

		#endregion

		#region Fields

		private Brush _polygonFillBrush;
		private Brush _polygonStrokeBrush;
		private Brush _playerPolygonFillBrush;
		private Brush _playerPolygonStrokeBrush;
		private DataTemplate _thingPushpinContentTemplate;
		private DataTemplate _landmarkPushpinContentTemplate;
		private ControlTemplate _noBorderButtonTemplate;
		private WF.Player.Core.Utils.GeoMathHelper _geoMathHelper;

		#endregion

		public GameMapViewModel()
		{
			// Inits the brushes.
			_polygonFillBrush = new SolidColorBrush(Colors.Cyan) { Opacity = 0.25 };
			_polygonStrokeBrush = new SolidColorBrush(Colors.White);
			_playerPolygonFillBrush = new SolidColorBrush(Colors.White) { Opacity = 0.25 };
			_playerPolygonStrokeBrush = new SolidColorBrush(Colors.Black);

			// Inits the templates.
			_thingPushpinContentTemplate = (DataTemplate)App.Current.Resources["WherigoThingPushpinContentTemplate"];
			_landmarkPushpinContentTemplate = (DataTemplate)App.Current.Resources["LandmarkPushpinContentTemplate"];
			_noBorderButtonTemplate = (ControlTemplate)App.Current.Resources["NoBorderButtonTemplate"];

			// Inits resources.
			_geoMathHelper = new WF.Player.Core.Utils.GeoMathHelper();
		}

		protected override void InitFromNavigation(BaseViewModel.NavigationInfo nav)
		{
			base.InitFromNavigation(nav);

			// Refreshes the map content.
			RefreshZones();
			RefreshThings();
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
			else if (propName == "ActiveVisibleThings")
			{
				RefreshThings();
			}
			else if (propName == "DeviceLocation")
			{
				RefreshPlayer();
			}
		}

        internal CredentialsProvider GetMapsCredentialsProvider()
        {
            try
            {
                // Gets the api key from the app's resources.
                string apiKey = (string)App.Current.Resources["BingMapsKey"];

                // Return the provider.
                return new ApplicationIdCredentialsProvider(apiKey);
            }
            catch (Exception)
            {
                // We couldn't retrieve the API key, so there's no credential provider to return.
                return null;
            }
        }

		private void RefreshPlayer()
		{
			// Gets the current position of the device.
			GeoCoordinate playerPos = Model.Core.DeviceLocation;

			// Makes a pushpin if there is a position of the device,
			// removes it otherwise.
			if (playerPos == null || playerPos.IsUnknown)
			{
				PlayerPushpin = null;
				PlayerAccuracyPolygon = null;
			}
			else
			{
				// Creates or refreshes the pushpin.
				if (PlayerPushpin == null)
				{
					Button btn = new Button()
					{
						Template = _noBorderButtonTemplate,
						Content = Model.Core.Player,
						ContentTemplate = _thingPushpinContentTemplate,
						Command = ShowThingDetailsCommand,
						CommandParameter = Model.Core.Player
					};
					
					PlayerPushpin = new Pushpin()
					{
						Content = btn,
						Location = playerPos
					};
				}
				else
				{
					PlayerPushpin.Location = playerPos;
				}

				// Creates or refreshes the accuracy polygon.
				double acc = Math.Round(playerPos.HorizontalAccuracy);
				if (PlayerAccuracyPolygon == null)
				{					
					// Creates the accuracy circle.
					PlayerAccuracyPolygon = new MapPolygon()
					{
						Fill = _playerPolygonFillBrush,
						Stroke = _playerPolygonStrokeBrush,
						StrokeThickness = 2,
						Locations = _geoMathHelper
							.GetCircle(playerPos.ToZonePoint(), acc, ACCURACY_CIRCLE_SAMPLES)
							.ToLocationCollection()
					};
				}
				else
				{
					// Changes the points of the accuracy circle.
					PlayerAccuracyPolygon.Locations = _geoMathHelper
						.GetCircle(playerPos.ToZonePoint(), acc, ACCURACY_CIRCLE_SAMPLES)
						.ToLocationCollection();
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
				e = new MapViewRequestedEventArgs(bounds.ToLocationRect());
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
					// Makes the event.
					e = new MapViewRequestedEventArgs(Model.Core.Cartridge.StartingLocation.ToGeoCoordinate(), DEFAULT_ZOOM_LEVEL);
				}
			}

			// If an event was prepared, send it.
			if (MapViewRequested != null)
			{
				MapViewRequested(this, e);
			}
		}

		private void RefreshThings()
		{
			List<Pushpin> things = new List<Pushpin>();
			
			// Groups all active things by their location.
			if (Model.Core.ActiveVisibleThings != null)
			{
				// Creates a pushpin for each group of non-Zone things that share 
				// a valid location.
				IEnumerable<IGrouping<GeoCoordinate, Thing>> thingsByLocation = Model.Core.ActiveVisibleThings
						.Where(t => t.ObjectLocation != null && !(t is Zone))
						.GroupBy(t => t.ObjectLocation.ToGeoCoordinate());

				foreach (IGrouping<GeoCoordinate, Thing> group in thingsByLocation)
				{
					// Creates a multiline content control for this group of things.
					Geowigo.Controls.NavigationListBox control = new Controls.NavigationListBox()
					{
						ItemTemplate = _thingPushpinContentTemplate,
						ItemsSource = group, // Zones are ignored
						NavigationCommand = ShowThingDetailsCommand
					};
					control.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

					// Creates and adds a pushpin whose content is the control.
					things.Add(new Pushpin()
					{
						Content = control,
						Location = group.Key
					});
				}
			}

			// This is the new list of pushpins.
			ThingPushpins = things;
		}

		private void RefreshZones()
		{
			List<MapPolygon> polygons = new List<MapPolygon>();
			List<Pushpin> labels = new List<Pushpin>();

			// Creates a map polygon for each zone.
			if (Model.Core.ActiveVisibleZones != null)
			{
				foreach (Zone zone in Model.Core.ActiveVisibleZones)
				{
					// Creates and adds the polygon.
					polygons.Add(new MapPolygon()
					{
						Locations = zone.Points.ToLocationCollection(),
						Fill = _polygonFillBrush,
						Stroke = _polygonStrokeBrush,
						StrokeThickness = 2
					});

					// Creates a landmark pushpin.
					labels.Add(new Pushpin()
					{
						Content = zone.Name,
						Location = zone.Bounds.Center.ToGeoCoordinate(),
						ContentTemplate = _landmarkPushpinContentTemplate,
						Background = null
					});
				} 
			}

			// Refreshes the collection of polygons.
			ZonePolygons = polygons;
			ZoneLabels = labels;
		}

		private void RaisePropertyChanged(string propName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
			}
		}

		#region Commands Impl

		private void ShowThingDetails(Thing thing)
		{
			// Navigates to the details of this thing.
			App.Current.ViewModel.NavigationManager.NavigateToView(thing);
		}

		#endregion
	}
}
