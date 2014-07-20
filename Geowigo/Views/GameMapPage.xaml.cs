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
using Microsoft.Phone.Controls.Maps;
using Geowigo.ViewModels;
using Geowigo.Utils;

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
		
		public GameMapPage()
		{
			InitializeComponent();

			MapControl.Mode = new Microsoft.Phone.Controls.Maps.AerialMode(true);

			// Adds event listeners for collection changes in the View Model.
			ViewModel.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(OnViewModelPropertyChanged);
			ViewModel.MapViewRequested += new EventHandler<GameMapViewModel.MapViewRequestedEventArgs>(OnViewModelMapViewRequested);
		}

		private void OnViewModelMapViewRequested(object sender, GameMapViewModel.MapViewRequestedEventArgs e)
		{
			// Updates the view according to the event.
			if (e.TargetBounds != null)
			{
				// Sets the map to show the target bounds.
				MapControl.SetView(e.TargetBounds);

				// If the bounds are too small, makes sure the surroundings are shown
				// to the player too.
				if (MapControl.ZoomLevel > GameMapViewModel.MAX_AUTO_ZOOM_LEVEL)
				{
					MapControl.ZoomLevel = GameMapViewModel.MAX_AUTO_ZOOM_LEVEL;
				}
			}
			else
			{
				// Sets the view using the target center and zoom level.
				MapControl.SetView(e.TargetCenter, e.TargetZoomLevel);
			}
		}

		private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ZonePolygons")
			{
				// Removes all current zones.
				ZonesLayer.Children.Clear();

				// Adds all new zone polygons.
				foreach (MapPolygon poly in ViewModel.ZonePolygons)
				{
					ZonesLayer.Children.Add(poly);
				}
			}
			else if (e.PropertyName == "PlayerPushpin")
			{	
				// Refreshes the pushpin of the player.
				if (ViewModel.PlayerPushpin == null)
				{
					PlayerLayer.Children.Clear();
				}
				else
				{
					// Syncs the current pushpin with the view model pushpin.
					Pushpin playerPin = PlayerLayer.Children.OfType<Pushpin>().SingleOrDefault();
					if (playerPin != ViewModel.PlayerPushpin)
					{
						PlayerLayer.Children.Remove(playerPin);
						PlayerLayer.Children.Add(ViewModel.PlayerPushpin);
					}
				}
			}
			else if (e.PropertyName == "ThingPushpins")
			{
				// Removes all current things.
				ItemsLayer.Children.Clear();

				// Adds all new things.
				foreach (Pushpin pin in ViewModel.ThingPushpins)
				{
					ItemsLayer.Children.Add(pin);
				}
			}
		}
	}
}