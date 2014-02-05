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

namespace Geowigo.Controls
{
	public partial class BlockingContentPresenter : UserControl
	{
		#region Dependency Properties

		#region IsProgressBarVisible


		public bool IsProgressBarVisible
		{
			get { return (bool)GetValue(IsProgressBarVisibleProperty); }
			set { SetValue(IsProgressBarVisibleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsProgressBarVisible.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsProgressBarVisibleProperty =
			DependencyProperty.Register("IsProgressBarVisible", typeof(bool), typeof(BlockingContentPresenter), new PropertyMetadata(false));


		#endregion

		#region ProgressBarStatusText


		public string ProgressBarStatusText
		{
			get { return (string)GetValue(ProgressBarStatusTextProperty); }
			set { SetValue(ProgressBarStatusTextProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ProgressBarStatusText.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ProgressBarStatusTextProperty =
			DependencyProperty.Register("ProgressBarStatusText", typeof(string), typeof(BlockingContentPresenter), new PropertyMetadata(null));


		#endregion

		#region InnerContent


		public object InnerContent
		{
			get { return (object)GetValue(InnerContentProperty); }
			set { SetValue(InnerContentProperty, value); }
		}

		// Using a DependencyProperty as the backing store for InnerContent.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty InnerContentProperty =
			DependencyProperty.Register("InnerContent", typeof(object), typeof(BlockingContentPresenter), new PropertyMetadata(null));


		#endregion

		#endregion
		
		public BlockingContentPresenter()
		{
			InitializeComponent();

			this.DataContext = this;
		}
	}
}
