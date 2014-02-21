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
using Geowigo.Controls;
using Geowigo.Models;

namespace Geowigo.ViewModels
{
	public class CompassCalibrationViewModel : BaseViewModel
	{		
		#region Events

		/// <summary>
		/// Raised when this view model requests a navigation back.
		/// </summary>
		public event EventHandler NavigateBackRequested;

		#endregion

		#region Dependency Properties

		#region HeadingAccuracy



		public double HeadingAccuracy
		{
			get { return (double)GetValue(HeadingAccuracyProperty); }
			set { SetValue(HeadingAccuracyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for HeadingAccuracy.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty HeadingAccuracyProperty =
			DependencyProperty.Register("HeadingAccuracy", typeof(double), typeof(CompassCalibrationViewModel), new PropertyMetadata(Double.MaxValue));



		#endregion

		#region IsCompassCalibrated


		public bool IsCompassCalibrated
		{
			get { return (bool)GetValue(IsCompassCalibratedProperty); }
			set { SetValue(IsCompassCalibratedProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsCompassCalibrated.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsCompassCalibratedProperty =
			DependencyProperty.Register("IsCompassCalibrated", typeof(bool), typeof(CompassCalibrationViewModel), new PropertyMetadata(false));

				
		#endregion

		#region IsHeadingAccuracyAvailable


		public bool IsHeadingAccuracyAvailable
		{
			get { return (bool)GetValue(IsHeadingAccuracyAvailableProperty); }
			set { SetValue(IsHeadingAccuracyAvailableProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsHeadingAccuracyAvailable.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsHeadingAccuracyAvailableProperty =
			DependencyProperty.Register("IsHeadingAccuracyAvailable", typeof(bool), typeof(CompassCalibrationViewModel), new PropertyMetadata(false));


		#endregion

		#endregion
		
		#region Commands

		#region DoneCommand

		private ICommand _DoneCommand;

		/// <summary>
		/// Gets a command to execute when the user decides the calibratio
		/// is finished.
		/// </summary>
		public ICommand DoneCommand
		{
			get
			{
				if (_DoneCommand == null)
				{
					_DoneCommand = new RelayCommand(EndCalibration);
				}

				return _DoneCommand;
			}
		}

		#endregion

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the maximal heading accuracy that a calibrated
		/// compass should have.
		/// </summary>
		public double MaxHeadingAccuracy { get; set; }

		#endregion

		public CompassCalibrationViewModel()
		{
			MaxHeadingAccuracy = 15;
		}

		protected override void OnModelChanging(Models.WherigoModel oldValue, Models.WherigoModel newValue)
		{
			// Unregisters event handlers.
			if (oldValue != null)
			{
				newValue.Core.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(Core_PropertyChanged);
			}

			// Registers event handlers.
			if (newValue != null)
			{
				// Registers this event manually to bypass BaseViewModel's relay restrictions.
				// (OnCorePropertyChanged is not relayed until the Engine is ready.)
				newValue.Core.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Core_PropertyChanged);

				// Makes sure the compass is running.
				newValue.Core.IsCompassEnabled = true;

				// Refreshes the heading accuracy.
				RefreshCalibrationFromCore(newValue.Core);
			}
		}

		protected override void InitFromNavigation(BaseViewModel.NavigationInfo nav)
		{
			base.InitFromNavigation(nav);

			if (Model != null)
			{
				RefreshCalibrationFromCore(Model.Core);
			}
		}

		private void Core_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "DeviceHeadingAccuracy")
			{
				// Refreshes the value of heading accuracy.
				RefreshCalibrationFromCore((WFCoreAdapter)sender);
			}
		}

		private void RefreshCalibrationFromCore(WFCoreAdapter engine)
		{
			if (engine == null)
			{
				return;
			}			

			double? hAcc = engine.DeviceHeadingAccuracy;
			IsHeadingAccuracyAvailable = hAcc.HasValue;
			if (hAcc.HasValue)
			{
				RefreshCalibration(hAcc.Value);
			}
		}

		private void RefreshCalibration(double accuracy)
		{
			// Reports the change in accuracy.
			HeadingAccuracy = accuracy;

			// Checks if the calibration is complete.
			bool oldIsCalibrated = IsCompassCalibrated;
			bool newIsCalibrated = accuracy <= MaxHeadingAccuracy;
			IsCompassCalibrated = newIsCalibrated;

			// If the calibration just completed, alert the user.
			if (newIsCalibrated && !oldIsCalibrated)
			{
				App.Current.ViewModel.Vibrate();
			}
		}

		private void EndCalibration()
		{
			// Back to previous page.
			if (NavigateBackRequested != null)
			{
				NavigateBackRequested(this, EventArgs.Empty);
			}
		}
	}
}
