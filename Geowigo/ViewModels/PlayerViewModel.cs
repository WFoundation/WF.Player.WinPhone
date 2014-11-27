using System;
using WF.Player.Core;
using System.Windows;
using System.Device.Location;
using Geowigo.Utils;
using Microsoft.Phone.Shell;
using System.Windows.Input;
using Geowigo.Controls;

namespace Geowigo.ViewModels
{
	public class PlayerViewModel : BaseViewModel
	{
		#region Constants

		public static readonly double MaxGoodLocationAccuracy = 50d;

		#endregion
		
		#region Properties

		#region WherigoObject

		public new Thing WherigoObject
		{
			get
			{
				return (Thing)base.WherigoObject;
			}
		}

		#endregion

		#endregion

		#region Dependency Properties

		#region LocationStatus


		public string LocationStatus
		{
			get { return (string)GetValue(LocationStatusProperty); }
			set { SetValue(LocationStatusProperty, value); }
		}

		// Using a DependencyProperty as the backing store for LocationStatus.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty LocationStatusProperty =
			DependencyProperty.Register("LocationStatus", typeof(string), typeof(PlayerViewModel), new PropertyMetadata(null));


		#endregion

		#region LocationAccuracyStatus


		public string LocationAccuracyStatus
		{
			get { return (string)GetValue(LocationAccuracyStatusProperty); }
			set { SetValue(LocationAccuracyStatusProperty, value); }
		}

		// Using a DependencyProperty as the backing store for LocationAccuracyStatus.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty LocationAccuracyStatusProperty =
			DependencyProperty.Register("LocationAccuracyStatus", typeof(string), typeof(PlayerViewModel), new PropertyMetadata(null));


		#endregion

		#region LocationWarning


		public string LocationWarning
		{
			get { return (string)GetValue(LocationWarningProperty); }
			set { SetValue(LocationWarningProperty, value); }
		}

		// Using a DependencyProperty as the backing store for LocationWarning.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty LocationWarningProperty =
			DependencyProperty.Register("LocationWarning", typeof(string), typeof(PlayerViewModel), new PropertyMetadata(null));


		#endregion

		#region CompassStatus


		public string CompassStatus
		{
			get { return (string)GetValue(CompassStatusProperty); }
			set { SetValue(CompassStatusProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CompassStatus.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CompassStatusProperty =
			DependencyProperty.Register("CompassStatus", typeof(string), typeof(PlayerViewModel), new PropertyMetadata(null));


		#endregion

		#region CompassAccuracyStatus


		public string CompassAccuracyStatus
		{
			get { return (string)GetValue(CompassAccuracyStatusProperty); }
			set { SetValue(CompassAccuracyStatusProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CompassAccuracyStatus.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CompassAccuracyStatusProperty =
			DependencyProperty.Register("CompassAccuracyStatus", typeof(string), typeof(PlayerViewModel), new PropertyMetadata(null));


		#endregion

		#region CompassWarning


		public string CompassWarning
		{
			get { return (string)GetValue(CompassWarningProperty); }
			set { SetValue(CompassWarningProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CompassWarning.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CompassWarningProperty =
			DependencyProperty.Register("CompassWarning", typeof(string), typeof(PlayerViewModel), new PropertyMetadata(null));


		#endregion

		#endregion

		#region Commands

		#region CalibrateCompassCommand

		/// <summary>
		/// Gets a command to calibrate the compass.
		/// </summary>
		public ICommand CalibrateCompassCommand
		{
			get
			{
				return _calibrateCompassCommand ?? (_calibrateCompassCommand = new RelayCommand(CalibrateCompass, CanCalibrateCompassCommandExecute));
			}
		}

		#endregion

		#endregion

		#region Fields

		private RelayCommand _calibrateCompassCommand;

		#endregion

		private void RefreshLocationStatuses()
		{
			// Handles first cases where no location is found.

			GeoPositionStatus locStatus = Model.Core.DeviceLocationStatus;

			// Location service is disabled or unavailable.
			if (locStatus == GeoPositionStatus.Disabled)
			{
				LocationWarning = "This device's location services are disabled or not working.";
			}

			// Location service is OK but no data was found.
			else if (locStatus == GeoPositionStatus.NoData)
			{
				LocationWarning = "Location services are enabled but gave no data so far.";
			}

			else if (locStatus == GeoPositionStatus.Initializing)
			{
				LocationWarning = null;
			}

			if (locStatus != GeoPositionStatus.Ready)
			{
				LocationStatus = "Status: " + locStatus.ToString();
				LocationAccuracyStatus = null;
				return;
			}

			// Location service is OK and data should be here.
			GeoCoordinate loc = Model.Core.DeviceLocation;
			
			// Data is not valid.
			if (loc == null || loc.IsUnknown)
			{
				LocationWarning = "Location services are enabled but only gave invalid data so far.";
				LocationStatus = "Status: " + locStatus.ToString();
				return;
			}

			// Data is valid.
			bool isPoorAccuracy = loc.HorizontalAccuracy >= MaxGoodLocationAccuracy;
			LocationStatus = loc.ToZonePoint().ToString(GeoCoordinateUnit.DegreesMinutes);
			LocationAccuracyStatus = String.Format("Accuracy: {0:0.00}m ({1})",
				loc.HorizontalAccuracy,
				isPoorAccuracy ? "POOR" : "OK");

			// Shows a warning for low accuracy.
			if (isPoorAccuracy)
			{
				LocationWarning = "Very low accuracy. Try looking for clear sky.";
			}
			else
			{
				LocationWarning = null;
			}
		}

		private void RefreshCompassStatuses()
		{
			// If the compass is not supported on this device, show a warning
			// and nothing more.
			if (!Model.Core.IsCompassSupported)
			{
				CompassStatus = null;
				CompassAccuracyStatus = null;
				CompassWarning = "The compass is not supported on this device.";
				return;
			}

			// The compass is supported.
			
			// Show heading if possible.
			double? heading = Model.Core.DeviceHeading;
			CompassStatus = heading.HasValue ?
				String.Format("Heading: {0}°", heading.Value) :
				"Heading: Unknown";

			// Show accuracy if possible.
			double? accuracy = Model.Core.DeviceHeadingAccuracy;
			bool hasPoorAccuracy = accuracy.HasValue && accuracy.Value >= CompassCalibrationViewModel.MaxGoodHeadingAccuracy;
			if (accuracy.HasValue)
			{
				string txt = String.Format("Accuracy: {0}° ", accuracy.Value);

				txt += hasPoorAccuracy ? "(POOR)" : "(OK)";

				CompassAccuracyStatus = txt;
			}
			else
			{
				CompassAccuracyStatus = "Accuracy: Unknown";
			}

			// Shows warnings.
			if (!heading.HasValue || !accuracy.HasValue)
			{
				CompassWarning = "The compass is enabled but only gave empty or partial data so far.";
			}
			else if (hasPoorAccuracy)
			{
				CompassWarning = "Poor accuracy. You should calibrate the compass.";
			}
			else
			{
				CompassWarning = null;
			}
		}

		protected override void OnModelChanging(Models.WherigoModel oldValue, Models.WherigoModel newValue)
		{
			// Removes handlers on the old model.
			if (oldValue != null)
			{
				// Disables the compass.
				oldValue.Core.IsCompassEnabled = false;

				// Removes handlers.
				oldValue.Core.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(Core_PropertyChanged);
			}

			// Adds handlers to the new model.
			if (newValue != null)
			{
				// Enables the compass for diagnostics.
				newValue.Core.IsCompassEnabled = true;

				// Adds handlers.
				newValue.Core.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Core_PropertyChanged);
			}
		}

		protected override void OnModelChanged(Models.WherigoModel newModel)
		{
			if (newModel != null)
			{
				// Refreshes the statuses.
				RefreshCompassStatuses();
				RefreshLocationStatuses();
			}
		}

		private void Core_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
            // Alternative event handler because override OnCorePropertyChanged does not fire when the engine is not ready.
            
            if (e.PropertyName == "DeviceLocationStatus")
			{
				RefreshLocationStatuses();
			}
            else if (e.PropertyName == "DeviceLocation")
            {
                // If the engine is not ready, display the actual device location.
                RefreshLocationStatuses();
            }
            else if (e.PropertyName == "IsCompassEnabled")
            {
                RefreshCompassStatuses();
            }
		}

		protected override void InitFromNavigation(NavigationInfo nav)
		{
			// Inits the application bar.
			InitAppBar();
            
            // Refresh everything.
            RefreshLocationStatuses();
            RefreshCompassStatuses();
		}

		private void InitAppBar()
		{
			ApplicationBar = new ApplicationBar() { Mode = ApplicationBarMode.Minimized };

			// Adds a button for compass calibration.
			ApplicationBar.CreateAndAddMenuItem(CalibrateCompassCommand, "calibrate compass");
		}

		private void CalibrateCompass()
		{
			// Navigates to compass calibration.
			App.Current.ViewModel.NavigationManager.NavigateToCompassCalibration();
		}

		private bool CanCalibrateCompassCommandExecute()
		{
			return Model.Core.IsCompassSupported;
		}
	}
}
