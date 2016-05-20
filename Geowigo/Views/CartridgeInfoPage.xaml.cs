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
using Microsoft.Phone.Maps.Controls;
using Geowigo.Utils;

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

            // Adds a blocking content presenter for cartridge loading.
            AddBlockingContentPresenter();
		}

        private void StaticMap_Loaded(object sender, RoutedEventArgs e)
        {
            // Injects the application's ID and Token.
            ((Map)sender).ApplyCredentials();
        }
	}
}