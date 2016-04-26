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
using Microsoft.Phone.Shell;

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

            public MapAnimationKind Animation { get; private set; }

            public MapViewRequestedEventArgs(LocationRectangle locRect, MapAnimationKind anim = MapAnimationKind.None)
			{
				TargetBounds = locRect;
                Animation = anim;
			}

			public MapViewRequestedEventArgs(GeoCoordinate center, double zoomLevel, MapAnimationKind anim = MapAnimationKind.None)
			{
				TargetCenter = center;
				TargetZoomLevel = zoomLevel;
                Animation = anim;
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

        public interface IThingGroupData
        {
            GeoCoordinate Location { get; }

            IEnumerable<Thing> Things { get; }
        }

        private class ThingGroupDataImpl : IThingGroupData
        {
            private Dictionary<Thing, GeoCoordinate> _things = new Dictionary<Thing, GeoCoordinate>();
            
            public ThingGroupDataImpl(Thing thing)
            {
                Add(thing, thing.ObjectLocation.ToGeoCoordinate());
            }

            public ThingGroupDataImpl(Thing thing, GeoCoordinate location)
            {
                Add(thing, location);
            }

            public GeoCoordinate Location { get; private set; }

            public IEnumerable<Thing> Things
            {
                get { return _things.Keys; }
            }

            internal void Add(Thing thing, GeoCoordinate location)
            {
                // Adds this to the list.
                _things.Add(thing, location);

                // Computes the group's average location.
                double lat = 0;
                double lon = 0;
                foreach (KeyValuePair<Thing, GeoCoordinate> kvp in _things)
                {
                    GeoCoordinate gc = kvp.Value;

                    lat += gc.Latitude;
                    lon += gc.Longitude;
                }
                lat /= _things.Count;
                lon /= _things.Count;
                Location = new GeoCoordinate(lat, lon);
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
        private ObservableCollection<IThingGroupData> _ThingGroups;
        public ObservableCollection<IThingGroupData> ThingGroups
        {
            get
            {
                if (_ThingGroups == null)
                {
                    _ThingGroups = new ObservableCollection<IThingGroupData>();
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

        #region CartographicModes
        public Array CartographicModes
        {
            get
            {
                return Enum.GetValues(typeof(MapCartographicMode));
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

        #region MoveMapViewToPlayerCommand

        private ICommand _MoveMapViewToPlayerCommand;

        public ICommand MoveMapViewToPlayerCommand
		{
			get 
			{
                return _MoveMapViewToPlayerCommand ?? (_MoveMapViewToPlayerCommand = new RelayCommand(MoveMapViewToPlayer)); 
			}
		}


		#endregion

        #region MoveMapViewToOverviewCommand

        private ICommand _MoveMapViewToOverviewCommand;

        public ICommand MoveMapViewToOverviewCommand
		{
			get 
			{
                return _MoveMapViewToOverviewCommand ?? (_MoveMapViewToOverviewCommand = new RelayCommand(MoveMapViewToOverview)); 
			}
		}

		#endregion

        #region SelectMapCartographicModeCommand

        private ICommand _SelectMapCartographicModeCommand;

        public ICommand SelectMapCartographicModeCommand
        {
            get
            {
                return _SelectMapCartographicModeCommand ?? (_SelectMapCartographicModeCommand = new RelayCommand(SelectMapCartographicMode));
            }
        }

        #endregion

		#endregion

		#region Events

        public event PropertyChangedEventHandler PropertyChanged;

		public event EventHandler<MapViewRequestedEventArgs> MapViewRequested;

        public event EventHandler SelectCartographicModeRequested;

		#endregion

		#region Constants

		private const double DEFAULT_ZOOM_LEVEL = 16d;

        private const double PLAYER_ZOOM_LEVEL = 19d;

		private const int ACCURACY_CIRCLE_SAMPLES = 25;

        private const double MAX_THING_GROUP_DISTANCE_PX = 50;

        private const int THING_GROUP_SLEEP_BEFORE_WORK_MS = 100;

		#endregion

		#region Fields

		private WF.Player.Core.Utils.GeoMathHelper _geoMathHelper;
        private double? _currentThingGroupingMaxDistance;
        private BackgroundWorker _thingGroupingBackgroundWorker;

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

        public void OnMapZoomLevelChanged(double zoomLevel, GeoCoordinate center)
        {
            // Computes the current resolution of the map, in meters per pixel.
            // See https://msdn.microsoft.com/en-us/library/aa940990.aspx
            double resolution = 156543.04d * Math.Cos(center.Latitude * Math.PI / 180d) / Math.Pow(2, zoomLevel);

            // Computes the max distance in meters allowed for grouping things.
            _currentThingGroupingMaxDistance = resolution * MAX_THING_GROUP_DISTANCE_PX;

            // Refreshes the things.
            RefreshThings();
        }

		protected override void InitFromNavigation(BaseViewModel.NavigationInfo nav)
		{
			base.InitFromNavigation(nav);

			// Refreshes the map content.
			RefreshZones();
			RefreshThings();
			RefreshPlayer();

			// Refreshes the bounds of the map.
            if (nav.NavigationMode != System.Windows.Navigation.NavigationMode.Back)
            {
                RefreshBounds(MapAnimationKind.None); 
            }

            // Refreshes the app bar.
            RefreshApplicationBar();
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

		private void RefreshBounds(MapAnimationKind animation)
		{
            if (Model == null || Model.Core == null || Model.Core.Cartridge == null)
            {
                return;
            }
            
            // Gets the current location data out of the engine.
			CoordBounds bounds = Model.Core.Bounds;
            GeoCoordinate deviceLoc = Model.Core.DeviceLocation;

			// Are the bounds valid?
			// YES -> Computes a view around them.
			// NO -> Computes a default view around the starting location
			// of the cartridge or the current location of the player.
			MapViewRequestedEventArgs e = null;
			if (bounds != null && bounds.IsValid)
			{
				// Inflates the bounds to include the player.
                if (deviceLoc != null && !deviceLoc.IsUnknown)
                {
                    bounds.Inflate(deviceLoc.ToZonePoint());
                }
                
                e = new MapViewRequestedEventArgs(bounds.ToLocationRectangle(), animation);
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
					if (deviceLoc != null && !deviceLoc.IsUnknown)
					{
						e = new MapViewRequestedEventArgs(deviceLoc, DEFAULT_ZOOM_LEVEL, animation);
					}
                    else
                    {
                        return;
                    }
				}
				else
				{
					// Makes the event.
					e = new MapViewRequestedEventArgs(Model.Core.Cartridge.StartingLocation.ToGeoCoordinate(), DEFAULT_ZOOM_LEVEL, animation);
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
			// Cancels the background worker if it's working.
            if (_thingGroupingBackgroundWorker != null && !_thingGroupingBackgroundWorker.CancellationPending)  
            {
                _thingGroupingBackgroundWorker.CancelAsync();
            }

            // Makes a worker.
            _thingGroupingBackgroundWorker = new BackgroundWorker() { WorkerSupportsCancellation = true };
            _thingGroupingBackgroundWorker.DoWork += (o, e) =>
            {
                // Waits for a while.
                System.Threading.Thread.Sleep(THING_GROUP_SLEEP_BEFORE_WORK_MS);

                // Stops if we're cancelled.
                BackgroundWorker bw = (BackgroundWorker)o;
                if (bw.CancellationPending)
                {
                    return;
                }

                // Groups all active things by their location.
                if (Model.Core.VisibleThings != null)
                {
                    // Gets all things that have a location and are not a zone.
                    IEnumerable<Thing> things = Model.Core.VisibleThings
                        .Where(t => !(t is Zone));

                    // Groups the things depending on distance.
                    IEnumerable<IThingGroupData> groups;
                    if (_currentThingGroupingMaxDistance.HasValue)
                    {
                        // Groups things according to a max distance. 
                        groups = GetThingGroups(things, _currentThingGroupingMaxDistance.Value);
                    }
                    else
                    {
                        // Makes one group per thing.
                        groups = things.Where(t => t.ObjectLocation != null).Select(t => new ThingGroupDataImpl(t));
                    }

                    // Checks if the groups changed. If not, don't refresh,
                    // because recreating pushpins is an expansive operation for the view.
                    IEnumerable<IThingGroupData> currentGroups = ThingGroups;
                    bool noNeedToRefresh = groups.All(g => currentGroups.Any(og => og.Location == g.Location && og.Things.OrderBy(ogt => ogt.ObjIndex).SequenceEqual(g.Things.OrderBy(gt => gt.ObjIndex))));
                    if (noNeedToRefresh)
                    {
                        return;
                    }

                    // Sets the property.
                    if (!bw.CancellationPending)
                    {
                        Dispatcher.BeginInvoke(() => ClearAndAddRange(ThingGroups, groups));
                    }
                    else
                    {
                        return;
                    }
                }
            };

            // Work.
            _thingGroupingBackgroundWorker.RunWorkerAsync();
		}

        private IEnumerable<IThingGroupData> GetThingGroups(IEnumerable<Thing> things, double maxDistance)
        {
            // Makes a list of groups.
            List<ThingGroupDataImpl> groups = new List<ThingGroupDataImpl>();

            // For each thing, if it is close enough to a group, adds it.
            foreach (Thing thing in things)
            {
                // Gets the location of this thing, or its container.
                GeoCoordinate loc = null;
                Thing container = thing;
                while (container != null && loc == null)
                {
                    if (container is Zone)
                    {
                        loc = ((Zone)container).Bounds.Center.ToGeoCoordinate();
                        break;
                    }
                    else if (container.ObjectLocation != null)
                    {
                        loc = container.ObjectLocation.ToGeoCoordinate();
                        break;
                    }
                    else
                    {
                        container = container.Container;
                    }
                }
                if (loc == null)
                {
                    // Ignores this thing, which has no valid location in the world.
                    continue;
                }
                
                // Is it close enough to a group?
                ThingGroupDataImpl targetGroup = null;
                foreach (ThingGroupDataImpl group in groups)
                {
                    // Adds this to the group if it's close enough to the group.
                    if (group.Location.GetDistanceTo(loc) <= maxDistance)
                    {
                        targetGroup = group;
                        break;
                    }
                }

                if (targetGroup != null)
                {
                    // We found a suitable group.
                    targetGroup.Add(thing, loc);
                }
                else
                {
                    // No group worked for this, make one for it.
                    groups.Add(new ThingGroupDataImpl(thing, loc));
                }
            }

            // We have our groups.
            return groups.Cast<IThingGroupData>();
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

        private void RefreshApplicationBar()
        {
            if (ApplicationBar != null)
            {
                return;
            }

            // Creates the app bar.
            IApplicationBar appBar = new ApplicationBar()
            {
                Mode = ApplicationBarMode.Default,
                Opacity = 0.5,
               
            };

            appBar.CreateAndAddMenuItem(SelectMapCartographicModeCommand, "change map background");

            appBar.CreateAndAddButton("appbar.location.circle.png", MoveMapViewToPlayerCommand, "me");

            appBar.CreateAndAddButton("appbar.location.checkin.png", MoveMapViewToOverviewCommand, "overview");

            ApplicationBar = appBar;
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

        private void MoveMapViewToOverview()
        {
            RefreshBounds(MapAnimationKind.Linear);
        }

        private void MoveMapViewToPlayer()
        {
            // Requests to move the map view to the player at a close zoom level.

            MapViewRequestedEventArgs e;
            
            // Gets the player position. Cancels if there's none.
            GeoCoordinate deviceLoc = Model.Core.DeviceLocation;
            if (deviceLoc != null && !deviceLoc.IsUnknown)
            {
                e = new MapViewRequestedEventArgs(deviceLoc, PLAYER_ZOOM_LEVEL, MapAnimationKind.Linear);
            }
            else
            {
                return;
            }

            // Sends the event.
            if (e != null && MapViewRequested != null)
            {
                MapViewRequested(this, e);
            }
        }

        private void SelectMapCartographicMode()
        {
            if (SelectCartographicModeRequested != null)
            {
                SelectCartographicModeRequested(this, EventArgs.Empty);
            }
        }

		#endregion
	}
}
