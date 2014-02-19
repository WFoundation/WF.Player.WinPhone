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
    public partial class BetaLicensePage : PhoneApplicationPage
    {
        public BetaLicensePage()
        {
            InitializeComponent();
        }

        private void AcceptBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
        	// Go on!
			App.Current.ViewModel.NavigateToAppHome();
        }
    }
}