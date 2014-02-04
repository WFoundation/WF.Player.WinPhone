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

namespace Geowigo.Views
{
	public partial class CompassCalibrationPage : BasePage
	{
		#region Properties

		public new CompassCalibrationViewModel ViewModel
		{
			get
			{
				return base.ViewModel as CompassCalibrationViewModel;
			}
		}

		#endregion
		
		public CompassCalibrationPage()
		{
			InitializeComponent();

			ViewModel.NavigateBackRequested += new EventHandler(ViewModel_NavigateBackRequested);
		}

		private void ViewModel_NavigateBackRequested(object sender, EventArgs e)
		{
			// Go back if possible.
			if (NavigationService.CanGoBack)
			{
				NavigationService.GoBack();
			}
		}
	}
}