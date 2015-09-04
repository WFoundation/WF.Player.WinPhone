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
        }

        protected override void OnReady()
        {
            base.OnReady();

            ViewModel.OnPageReady();
        }
    }
}