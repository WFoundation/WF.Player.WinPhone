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

        #region WherigoObject


        public Thing WherigoObject
        {
            get { return (Thing)GetValue(WherigoObjectProperty); }
            set { SetValue(WherigoObjectProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WherigoObject.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WherigoObjectProperty =
            DependencyProperty.Register("WherigoObject", typeof(Thing), typeof(DistanceControl), new PropertyMetadata(null, OnWherigoObjectChanged));

        private static void OnWherigoObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DistanceControl)d).OnWherigoObjectChanged(e.NewValue as Thing);
        }

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

            App.Current.Model.Core.PropertyChanged += OnCorePropertyChanged;
		}

        void OnCorePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DeviceHeading")
            {
                double? heading = App.Current.Model.Core.DeviceHeading;
                if (heading.HasValue)
                {
                    RefreshBearing(deviceHeading: heading.Value);
                }
            }
        }

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
            App.Current.Model.Core.PropertyChanged -= OnCorePropertyChanged;
		}

		private void OnDistanceChanged(DependencyPropertyChangedEventArgs e)
		{
			Distance newDistance = e.NewValue as Distance;

            RefreshFromDistance(newDistance);
		}

        private void RefreshFromDistance(Distance newDistance)
        {
            if (newDistance == null)
            {
                if (WherigoObject == null)
                {
                    // Shows the control in a state displaying that the distance is undefined.
                    DistanceText.Text = "In Range";
                    VisualStateManager.GoToState(this, this.UnknownState.Name, true);
                }
                else
                {
                    // Shows some context: the thing's container, if there is one.
                    if (WherigoObject.Container == null)
                    {
                        // No clue, let's just show "In Range".
                        DistanceText.Text = "In Range";
                        VisualStateManager.GoToState(this, this.UnknownState.Name, true);
                    }
                    else
                    {
                        // We have a name.
                        DistanceText.Text = "By " + WherigoObject.Container.Name;
                        VisualStateManager.GoToState(this, this.UnknownState.Name, true);
                    }
                }
            }
            else
            {
                // Tries to retrieve the new distance value.
                // In some rare cases this may fail.
                double distanceValue = 0;
                try
                {
                    distanceValue = newDistance.Value;
                }
                catch (InvalidOperationException)
                {
                    // This probably means that an error occurred while computing
                    // or retrieving the value. Let's display "Unknown" as a distance
                    // text.
                    DistanceText.Text = "Unknown";
                    return;
                }

                // Are we inside or outside?
                if (distanceValue == 0)
                {
                    DistanceText.Text = "Here";
                    VisualStateManager.GoToState(this, this.InsideState.Name, true);
                }
                else
                {
                    DistanceText.Text = newDistance.BestMeasureAs(App.Current.Model.Settings.LengthUnit);
                    VisualStateManager.GoToState(this, this.OutsideState.Name, true);
                }

            }
        }

		private void OnVectorChanged(DependencyPropertyChangedEventArgs e)
		{
			LocationVector v = (LocationVector)e.NewValue;

            RefreshFromVector(v);
		}

        private void RefreshFromVector(LocationVector v)
        {
            if (v == null)
            {
                // No distance to show.
                bool distanceChanged = Distance != null;
                Distance = null;
                RefreshBearing(bearingFromNorth: 0);
                if (!distanceChanged)
                {
                    RefreshFromDistance(null);
                }
            }
            else
            {
                // A distance to show!
                Distance = v.Distance;
                RefreshBearing(bearingFromNorth: v.Bearing);
            }
        }

        private void OnWherigoObjectChanged(WF.Player.Core.Thing wherigoObject)
        {
            // Updates the vector.
            LocationVector vector = null;
            if (wherigoObject != null)
            {
                vector = wherigoObject.VectorFromPlayer;
            }

            bool vectorChanged = Vector != vector;
            Vector = vector;
            if (!vectorChanged)
            {
                RefreshFromVector(vector);
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
