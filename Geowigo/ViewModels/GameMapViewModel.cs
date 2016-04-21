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
using System.Collections.Generic;
using WF.Player.Core;
using System.Collections.ObjectModel;
using Geowigo.Utils;
using System.Diagnostics;
using System.ComponentModel;
using System.Device.Location;
using System.Linq;
using Geowigo.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;

namespace Geowigo.ViewModels
{
	public class GameMapViewModel : BaseViewModel //, INotifyPropertyChanged
	{
		#region Nested Classes

		public class MapViewRequestedEventArgs : EventArgs
		{
			public LocationRectangle TargetBounds { get; private set; }

			public GeoCoordinate TargetCenter { get; private set; }

			public double TargetZoomLevel { get; private set; }

            public MapViewRequestedEventArgs(LocationRectangle locRect)
			{
				TargetBounds = locRect;
			}

			public MapViewRequestedEventArgs(GeoCoordinate center, double zoomLevel)
			{
				TargetCenter = center;
				TargetZoomLevel = zoomLevel;
			}
		}

        public class ZoneData
        {
            public GeoCoordinateCollection Points { get; private set; }

            public string Name { get; private set; }

            public GeoCoordinate NameAnchor { get; private set; }

            public ZoneData(GeoCoordinateCollection points, GeoCoordinate anchor, string name)
            {
                Points = points;
                NameAnchor = anchor;
                Name = name;
            }
            
        }

		#endregion

        #region Dependency Properties

        #region PlayerLocation


        public GeoCoordinate PlayerLocation
        {
            get { return (GeoCoordinate)GetValue(PlayerLocationProperty); }
            set { SetValue(PlayerLocationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PlayerLocation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlayerLocationProperty =
            DependencyProperty.Register("PlayerLocation", typeof(GeoCoordinate), typeof(GameMapViewModel), new PropertyMetadata(null));


        #endregion

        #endregion

		#region Properties

        #region Zones
        private ObservableCollection<ZoneData> _Zones;
        public ObservableCollection<ZoneData> Zones
        {
            get
            {
                if (_Zones == null)
                {
                    _Zones = new ObservableCollection<ZoneData>();
                }

                return _Zones;
            }
        }
        #endregion

        #region ThingGroups
        private ObservableCollection<IGrouping<GeoCoordinate, Thing>> _ThingGroups;
        public ObservableCollection<IGrouping<GeoCoordinate, Thing>> ThingGroups
        {
            get
            {
                if (_ThingGroups == null)
                {
                    _ThingGroups = new ObservableCollection<IGrouping<GeoCoordinate, Thing>>();
                }

                return _ThingGroups;
            }
        }
        #endregion

        #region PlayerAccuracyArea
        private GeoCoordinateCollection _PlayerAccuracyArea;
        public GeoCoordinateCollection PlayerAccuracyArea
        {
            get
            {
                return _PlayerAccuracyArea;
            }

            private set
            {
                if (_PlayerAccuracyArea != value)
                {
                    _PlayerAccuracyArea = value;

                    RaisePropertyChanged("PlayerAccuracyArea");
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

		private WF.Player.Core.Utils.GeoMathHelper _geoMathHelper;

		#endregion

		public GameMapViewModel()
		{
			// Inits resources.
			_geoMathHelper = new WF.Player.Core.Utils.GeoMathHelper();
		}

        internal void SetMapsCredentials(Microsoft.Phone.Maps.MapsApplicationContext ctx)
        {
            if (ctx == null)
	        {
                return;
	        }
            
            try
            {
                // Gets the two keys from the app's resources.
                ctx.ApplicationId = (string)App.Current.Resources["MapsApplicationId"];
                ctx.AuthenticationToken = (string)App.Current.Resources["MapsApplicationId"];
            }
            catch (Exception)
            {
                // We couldn't retrieve the keys, so reset both properties.
                ctx.ApplicationId = null;
                ctx.AuthenticationToken = null;
            }
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
			else if (propName == "VisibleThings")
			{
				RefreshThings();
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

            // Default values if the position is invalid.
            if (playerPos == null || playerPos.IsUnknown)
            {
                PlayerLocation = null;
                PlayerAccuracyArea = null;
                return;
            }
            
            // Updates the position.
            PlayerLocation = playerPos;

            // Makes the accuracy polygon points.
            PlayerAccuracyArea = _geoMathHelper
                .GetCircle(playerPos.ToZonePoint(), Math.Round(playerPos.HorizontalAccuracy), ACCURACY_CIRCLE_SAMPLES)
                .ToGeoCoordinateCollection();
		}

		private void RefreshBounds()
		{
            if (Model == null || Model.Core == null || Model.Core.Cartridge == null)
            {
                return;
            }
            
            // Gets the current bounds of the engine.
			CoordBounds bounds = Model.Core.Bounds;

			// Are the bounds valid?
			// YES -> Computes a view around them.
			// NO -> Computes a default view around the starting location
			// of the cartridge or the current location of the player.
			MapViewRequestedEventArgs e = null;
			if (bounds != null && bounds.IsValid)
			{
				e = new MapViewRequestedEventArgs(bounds.ToLocationRectangle());
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
			// Groups all active things by their location.
			if (Model.Core.VisibleThings != null)
			{
                IEnumerable<IGrouping<GeoCoordinate, Thing>> groups = Model.Core.VisibleThings
                        .Where(t => t.ObjectLocation != null && !(t is Zone))
                        .GroupBy(t => t.ObjectLocation.ToGeoCoordinate());

                ClearAndAddRange(ThingGroups, groups);
			}
		}

		private void RefreshZones()
		{
			// Creates a map polygon for each zone.
            IEnumerable<Zone> zones = Model.Core.ActiveVisibleZones;
			if (zones != null)
			{
				IEnumerable<ZoneData> data = zones.
                    Select(z => new ZoneData(z.Points.ToGeoCoordinateCollection(), z.Bounds.Center.ToGeoCoordinate(), z.Name));

                ClearAndAddRange(Zones, data);
			}
		}

        private void ClearAndAddRange<T>(ICollection<T> collec, IEnumerable<T> range)
        {
            // Clears.
            collec.Clear();

            // Adds range.
            foreach (T item in range)
            {
                collec.Add(item);
            }
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
