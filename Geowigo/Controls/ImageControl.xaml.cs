using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Geowigo.Controls
{
	public partial class ImageControl : UserControl
	{
		#region Dependency Properties

		#region Source


		public ImageSource Source
		{
			get { return (ImageSource)GetValue(SourceProperty); }
			set { SetValue(SourceProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Source.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SourceProperty =
			DependencyProperty.Register("Source", typeof(ImageSource), typeof(ImageControl), new PropertyMetadata(null, OnSourcePropertyChanged));

		private static void OnSourcePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			((ImageControl)o).OnSourceChanged(e.OldValue as ImageSource, e.NewValue as ImageSource);
		}

		#endregion

		#endregion

		#region Constants
		private const double MaxScale = 1.0;
		private const double BorderThreshold = 0.8;
		#endregion

		#region Fields
		private BitmapSource _bitmap;

		private double _scale = 1.0;
		private double _minScale;
		private double _coercedScale;
		#endregion

		public ImageControl()
		{
			InitializeComponent();
		}

		private void OnSourceChanged(ImageSource oldValue, ImageSource newValue)
		{
			Dispatcher.BeginInvoke(InitImage);
		}

		private void OnImageControlLoaded(object sender, RoutedEventArgs e)
		{
			Dispatcher.BeginInvoke(InitImage);
		}

		private void InitImage()
		{
			_bitmap = Source as BitmapSource;

			_scale = 0;
			CoerceScale(true);
			_scale = _coercedScale;

			ResizeImage(true);
		}

		private void ResizeImage(bool center)
		{
			if (_coercedScale != 0 && _bitmap != null)
			{
				// Resizes the image control.
				double newWidth = Math.Round(_bitmap.PixelWidth * _coercedScale);
				double newHeight = Math.Round(_bitmap.PixelHeight * _coercedScale);
				InnerImage.Width = newWidth;
				InnerImage.Height = newHeight;
			}
		}

		private void CoerceScale(bool recompute)
		{
			if (recompute && _bitmap != null && LayoutRoot != null)
			{
				// Calculate the minimum scale:
				// - if the image is bigger than the viewport, scale it down so that it fits it.
				// - if the image is smaller than the viewport, let it be scaled 100%.
				double minScaleX = Math.Min(LayoutRoot.ActualWidth * BorderThreshold / _bitmap.PixelWidth, 1.0);
				double minScaleY = Math.Min(LayoutRoot.ActualHeight * BorderThreshold / _bitmap.PixelHeight, 1.0);
				_minScale = Math.Min(minScaleX, minScaleY);
			}

			_coercedScale = Math.Min(MaxScale, Math.Max(_scale, _minScale));
		}
	}
}
