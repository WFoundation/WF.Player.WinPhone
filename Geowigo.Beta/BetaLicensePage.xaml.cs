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

namespace Geowigo.Beta
{
    public partial class BetaLicensePage : PhoneApplicationPage
    {
        public BetaLicensePage()
        {
            InitializeComponent();
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            // Clears back entry.
            int entriesToRemove = NavigationService.BackStack.Count();
            for (int i = 0; i < entriesToRemove; i++)
            {
                NavigationService.RemoveBackEntry();
            }

            // Let the back key being performed: the app should quit.
        }

        private void AcceptBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
        	// Go on!
            NavigationService.GoBack();
        }
    }
}