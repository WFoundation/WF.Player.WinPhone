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
using System.Reflection;

namespace Geowigo.Views
{
    public partial class BetaLicensePage : PhoneApplicationPage
    {
		public string VersionText { get; set; }
		
		public BetaLicensePage()
        {
			VersionText = "Current Version: " + Version.Parse(Assembly.GetExecutingAssembly()
						.GetCustomAttributes(false)
						.OfType<AssemblyFileVersionAttribute>()
						.First()
						.Version);
			
			InitializeComponent();

			DataContext = this;
        }

        private void AcceptBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
        	// Go on!
			App.Current.ViewModel.NavigateToAppHome();
        }
    }
}