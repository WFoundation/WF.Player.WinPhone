using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Geowigo.ViewModels;
using Geowigo.Converters;

namespace Geowigo.Views
{
    public partial class SettingsPage : BasePage
    {
        public new SettingsViewModel ViewModel
        {
            get
            {
                return (SettingsViewModel)base.ViewModel;
            }
        }
        
        public SettingsPage()
        {
            InitializeComponent();

            this.AddBlockingContentPresenter();

			ItemSourceToStringConverter converter = (ItemSourceToStringConverter)this.Resources["LengthUnitItemSourceToStringConverter"];
			converter.Strings = ViewModel.LengthUnitDescriptions;
        }

        protected override void OnReady()
        {
            base.OnReady();

            ViewModel.OnPageReady();
        }
    }
}