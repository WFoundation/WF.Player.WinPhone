using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Geowigo.ViewModels;
using Geowigo.Utils;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;

namespace Geowigo.Views
{
	public partial class GameMapPage : BasePage
	{
		#region Properties

		#region ViewModel

		public new GameMapViewModel ViewModel
		{
			get
			{
				return (GameMapViewModel)base.ViewModel;
			}
		}

		#endregion

		#endregion

        #region Fields

        private Color _polygonFillColor;
        private Color _polygonStrokeColor;
        private Color _playerAccuracyFillColor;
        private Color _playerAccuracyStrokeColor;
        private bool _isMapReady;
        private GameMapViewModel.MapViewRequestedEventArgs _delayedMapViewRequest;

        #endregion
		
		public GameMapPage()
		{
			InitializeComponent();

            // Inits the brushes.
            _polygonFillColor = GetColorClone(Colors.Cyan, 64);
            _polygonStrokeColor = Colors.White;
            _playerAccuracyFillColor = GetColorClone(Colors.White, 64);
            _playerAccuracyStrokeColor = Colors.Black;

            // Adds event listeners for collection changes in the View Model.
            ViewModel.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(OnViewModelPropertyChanged);
            ViewModel.Zones.CollectionChanged += OnViewModelZonesCollectionChanged;
            ViewModel.MapViewRequested += new EventHandler<GameMapViewModel.MapViewRequestedEventArgs>(OnViewModelMapViewRequested);
            ViewModel.SelectCartographicModeRequested += ViewModel_SelectCartographicModeRequested;

            // Adds map events.
            MapControl.ZoomLevelChanged += OnMapZoomLevelChanged;
		}

        private Color GetColorClone(Color color, byte alpha)
        {
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        #region Map Init
        private MapLayer MakeLayer()
        {
            // Creates a layer.
            MapLayer layer = new MapLayer();

            // Adds it to the map.
            MapControl.Layers.Add(layer);

            return layer;
        }

        private void MapControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Injects the application's ID and Token.
            ViewModel.SetMapsCredentials(Microsoft.Phone.Maps.MapsSettings.ApplicationContext);

            // The map can only be used for some things a while after this event has fired.
            Dispatcher.BeginInvoke(OnMapReady);
        }

        private void OnMapReady()
        {
            // Waits a bit for the map to be ready.
            System.Threading.Tasks.Task.Delay(250).Wait();

            // The map is now ready.
            _isMapReady = true;

            // Initializes the map item controls.
            BindMapItemsControlItemsSource("ThingsPushpins", ViewModel.ThingGroups);
            BindMapItemsControlItemsSource("ZoneLabelsPushpins", ViewModel.Zones);

            // Applies a map view request that was issued before the map was ready.
            if (_delayedMapViewRequest != null)
            {
                SetMapView(_delayedMapViewRequest);

                _delayedMapViewRequest = null;
            }
        }

        private void BindMapItemsControlItemsSource(string name, System.Collections.IEnumerable source)
        {
            // Finds the right control.
            MapItemsControl mapItemsControl = MapExtensions.GetChildren(MapControl)
                .OfType<MapItemsControl>()
                .First(mic => mic.Name == name);
            
            // Binds the property.
            mapItemsControl.ItemsSource = source;
        }

        #endregion

        #region Bounds
        private void OnViewModelMapViewRequested(object sender, GameMapViewModel.MapViewRequestedEventArgs e)
        {
            if (_isMapReady)
            {
                SetMapView(e);
            }
            else
            {
                // Delays this request for after the map is ready.
                _delayedMapViewRequest = e;
            }
        }

        private void SetMapView(GameMapViewModel.MapViewRequestedEventArgs e)
        {
            // Updates the view according to the event.
            if (e.TargetBounds != null)
            {
                // Sets the map to show the target bounds.
                MapControl.SetView(e.TargetBounds, e.Animation);
            }
            else
            {
                // Sets the view using the target center and zoom level.
                MapControl.SetView(e.TargetCenter, e.TargetZoomLevel, e.Animation);
            }
        }

        private void OnMapZoomLevelChanged(object sender, MapZoomLevelChangedEventArgs e)
        {
            // Alerts the view model.
            ViewModel.OnMapZoomLevelChanged(MapControl.ZoomLevel, MapControl.Center);
        }

        #endregion

        private void OnViewModelZonesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RefreshMapElements();
        }

        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "PlayerAccuracyArea")
            {
                RefreshMapElements();
            }
        }

        private void RefreshMapElements()
        {
            // Clears map elements.
            System.Collections.ObjectModel.Collection<MapElement> elements = MapControl.MapElements;
            elements.Clear();

            // Refresh player accuracy polygon.
            GeoCoordinateCollection playerAccuracyArea = ViewModel.PlayerAccuracyArea;
            if (playerAccuracyArea != null)
            {
                elements.Add(new MapPolygon()
                {
                    Path = playerAccuracyArea,
                    StrokeThickness = 2,
                    FillColor = _playerAccuracyFillColor,
                    StrokeColor = _playerAccuracyStrokeColor
                });
            }
            
            // Refreshes zones.
            IEnumerable<GameMapViewModel.ZoneData> zones = ViewModel.Zones;
            if (zones != null)
            {
                foreach (GameMapViewModel.ZoneData zone in zones)
                {
                    elements.Add(new MapPolygon()
                    {
                        Path = zone.Points,
                        FillColor = _polygonFillColor,
                        StrokeColor = _polygonStrokeColor,
                        StrokeThickness = 2
                    });
                }
            }
        }

        #region Cartographic Mode Picker

        private void ViewModel_SelectCartographicModeRequested(object sender, EventArgs e)
        {
            // Opens the list picker to pick a mode.
            CartographicModeListPicker.Open();
        } 
        #endregion
	}
}