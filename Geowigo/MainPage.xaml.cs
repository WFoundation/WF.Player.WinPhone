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
using WF.Player.Core;
using System.IO.IsolatedStorage;

namespace Geowigo
{
	public partial class MainPage : PhoneApplicationPage
    {
        // Constructeur
        public MainPage()
        {
            InitializeComponent();
        }

		protected override void OnTap(System.Windows.Input.GestureEventArgs e)
		{
 			 base.OnTap(e);

			 TestWF2();
		}

		private void TestWF2()
		{
			//App.Current.ViewModel.NavigateToGameHome("/unit_test.gwc");
			//App.Current.ViewModel.NavigateToGameHome("/butor_internal_beta5_all.gwc");
			//App.Current.ViewModel.NavigateToGameHome("/dixieme_promenade_all.gwc");
			App.Current.ViewModel.NavigateToGameHome("/battleship.gwc");
			//App.Current.ViewModel.NavigateToGameHome("/Wherigo Tutorial.gwc");
			//App.Current.ViewModel.NavigateToGameHome("/kelownas_challenge.gwc");
			//App.Current.ViewModel.NavigateToGameHome("/hail_to_the_victor.gwc");
			//App.Current.ViewModel.NavigateToGameHome("/iphonetest4.gwc");
			
		}
    }
}