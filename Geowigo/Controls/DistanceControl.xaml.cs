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
using WF.Player.Core;

namespace Geowigo.Controls
{
	public partial class DistanceControl : UserControl
	{		
		#region Dependency Properties

		#region Distance
		public Distance Distance
		{
			get { return (Distance)GetValue(DistanceProperty); }
			set { SetValue(DistanceProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Distance.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty DistanceProperty =
			DependencyProperty.Register("Distance", typeof(Distance), typeof(DistanceControl), new PropertyMetadata(null, OnDistanceChanged));

		public static void OnDistanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((DistanceControl)d).OnDistanceChanged(e);
		} 
		#endregion

		#region Bearing

		public double Bearing
		{
			get { return (int)GetValue(BearingProperty); }
			set { SetValue(BearingProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ArrowOrientation.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty BearingProperty =
			DependencyProperty.Register("Bearing", typeof(double), typeof(DistanceControl), new PropertyMetadata(0d));//, OnBearingPropertyChanged));

		//private static void OnBearingPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		//{
		//    ((DistanceControl)o).OnBearingChanged(e);
		//}

		#endregion

		#region Vector



		public LocationVector Vector
		{
			get { return (LocationVector)GetValue(VectorProperty); }
			set { SetValue(VectorProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Vector.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty VectorProperty =
			DependencyProperty.Register("Vector", typeof(LocationVector), typeof(DistanceControl), new PropertyMetadata(null, OnVectorChanged));

		private static void OnVectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((DistanceControl)d).OnVectorChanged(e);
		}

		#endregion

		#region TextVisibility



		public Visibility TextVisibility
		{
			get { return (Visibility)GetValue(TextVisibilityProperty); }
			set { SetValue(TextVisibilityProperty, value); }
		}

		// Using a DependencyProperty as the backing store for TextVisibility.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TextVisibilityProperty =
			DependencyProperty.Register("TextVisibility", typeof(Visibility), typeof(DistanceControl), new PropertyMetadata(default(Visibility)));



		#endregion

		#region Orientation



		public Orientation Orientation
		{
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Orientation.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty OrientationProperty =
			DependencyProperty.Register("Orientation", typeof(Orientation), typeof(DistanceControl), new PropertyMetadata(default(Orientation)));

		

		#endregion

		#region TextMargin


		public Thickness TextMargin
		{
			get { return (Thickness)GetValue(TextMarginProperty); }
			set { SetValue(TextMarginProperty, value); }
		}

		// Using a DependencyProperty as the backing store for TextMargin.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TextMarginProperty =
			DependencyProperty.Register("TextMargin", typeof(Thickness), typeof(DistanceControl), new PropertyMetadata(default(Thickness)));


		#endregion

		#endregion

		#region Members

		private double _lastBearingFromNorth;
		private double _lastDeviceHeading;

		#endregion

		public DistanceControl()
		{
			InitializeComponent();

			VisualStateManager.GoToState(this, this.UnknownState.Name, true);

			if (System.ComponentModel.DesignerProperties.IsInDesignTool)
			{
				return;	
			}

			Unloaded += new RoutedEventHandler(OnUnloaded);

			App.Current.Model.Core.PlayerLocationChanged += new EventHandler<Models.PlayerLocationChangedEventArgs>(OnPlayerLocationChanged);
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			App.Current.Model.Core.PlayerLocationChanged -= new EventHandler<Models.PlayerLocationChangedEventArgs>(OnPlayerLocationChanged);
		}

		private void OnPlayerLocationChanged(object sender, Models.PlayerLocationChangedEventArgs e)
		{
			// If the heading of the device has changed, adjusts the bearing that is on-screen.
			if (e.Heading.HasValue)
			{
				RefreshBearing(deviceHeading: e.Heading);
			}
		}

		private void OnDistanceChanged(DependencyPropertyChangedEventArgs e)
		{
			Distance newDistance = e.NewValue as Distance;

			if (newDistance == null)
			{
				// Shows the control in a state displaying that the distance is undefined.
				DistanceText.Text = "Unknown";
				VisualStateManager.GoToState(this, this.UnknownState.Name, true);
			}
			else
			{
				// Are we inside or outside?
				if (newDistance.Value == 0)
				{
					DistanceText.Text = "Here";
					VisualStateManager.GoToState(this, this.InsideState.Name, true);
				}
				else
				{
					DistanceText.Text = newDistance.BestMeasureAs(DistanceUnit.Meters);
					VisualStateManager.GoToState(this, this.OutsideState.Name, true);
				}
				
			}
		}

		private void OnVectorChanged(DependencyPropertyChangedEventArgs e)
		{
			LocationVector v = (LocationVector)e.NewValue;
			
			if (v == null)
			{
				// No distance to show.
				Distance = null;
				RefreshBearing(bearingFromNorth: 0);
			}
			else
			{
				// A distance to show!
				Distance = v.Distance;
				RefreshBearing(bearingFromNorth: v.Bearing);
			}
		}

		private void RefreshBearing(double? bearingFromNorth = null, double? deviceHeading = null)
		{
			// Keeps track of the newest values.
			if (bearingFromNorth.HasValue && !Double.IsNaN(bearingFromNorth.Value))
			{
				_lastBearingFromNorth = bearingFromNorth.Value;
			}
			if (deviceHeading.HasValue && !Double.IsNaN(deviceHeading.Value))
			{
				_lastDeviceHeading = deviceHeading.Value;
			}

			// Computes the bearing from actual device orientation.
			Bearing = (_lastBearingFromNorth - _lastDeviceHeading) % 360;
		}
	}
}
