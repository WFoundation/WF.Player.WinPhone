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

namespace Geowigo.Views
{
	public partial class CartridgeInfoPage : BasePage
	{
		#region Properties

		public new ViewModels.CartridgeInfoViewModel ViewModel
		{
			get
			{
				return base.ViewModel as ViewModels.CartridgeInfoViewModel;
			}
		}

		#endregion
		
		public CartridgeInfoPage()
		{
			InitializeComponent();
		}

		private void StaticMap_StatusChanged(object sender, JeffWilcox.Controls.StaticMapStatusChangedEventArgs e)
		{
            ViewModel.OnStaticMapStatusChanged(e.Status);
		}
	}
}