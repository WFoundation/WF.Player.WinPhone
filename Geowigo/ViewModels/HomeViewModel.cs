using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Geowigo.Models;
using Geowigo.Controls;
using WF.Player.Core;

namespace Geowigo.ViewModels
{
	public class HomeViewModel : DependencyObject
	{
        #region Dependency Properties

        #region AreCartridgesVisible


        public bool AreCartridgesVisible
        {
            get { return (bool)GetValue(AreCartridgesVisibleProperty); }
            set { SetValue(AreCartridgesVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AreCartridgesVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AreCartridgesVisibleProperty =
            DependencyProperty.Register("AreCartridgesVisible", typeof(bool), typeof(HomeViewModel), new PropertyMetadata(false));

        
        #endregion

        #endregion
        
        #region Properties

		#region AppTitle

		public string AppTitle
		{
			get
			{
				return App.Current.ViewModel.AppTitle;
			}
		}

		#endregion

		#region Model

		public WherigoModel Model { get; private set; }

		#endregion

        #region IsoStoreSpyHomepageUri

        public Uri IsoStoreSpyHomepageUri
        {
            get
            {
                return new Uri("/http://isostorespy.codeplex.com/", UriKind.Absolute);
            }
        }
        #endregion

		#endregion

		#region Commands

		#region StartCartridgeCommand

		private ICommand _StartCartridgeCommand;

		public ICommand StartCartridgeCommand
		{
			get
			{
				if (_StartCartridgeCommand == null)
				{
					_StartCartridgeCommand = new RelayCommand<CartridgeTag>(StartCartridge);
				} 

				return _StartCartridgeCommand;
			}
		}

		#endregion

		#region ShowCartridgeInfoCommand

		private ICommand _ShowCartridgeInfoCommand;

		public ICommand ShowCartridgeInfoCommand
		{
			get
			{
				if (_ShowCartridgeInfoCommand == null)
				{
					_ShowCartridgeInfoCommand = new RelayCommand<CartridgeTag>(ShowCartridgeInfo);
				}

				return _ShowCartridgeInfoCommand;
			}
		}

		#endregion

        #region VisitIsoStoreHomePageCommand
        private ICommand _VisitIsoStoreHomePageCommand;

        public ICommand VisitIsoStoreHomePageCommand
        {
            get
            {
                if (_VisitIsoStoreHomePageCommand == null)
                {
                    _VisitIsoStoreHomePageCommand = new RelayCommand(VisitIsoStoreHomePage);
                }

                return _VisitIsoStoreHomePageCommand;
            }
        }
        #endregion

		#endregion

		public HomeViewModel()
		{
			Model = App.Current.Model;
		}

		public void InitFromNavigation(System.Windows.Navigation.NavigationEventArgs e)
		{
			// Removes the back entries, if any.
			App.Current.ViewModel.ClearBackStack();
			
			// Synchronizes the cartridge store.
            RefreshVisibilities();
            Model.CartridgeStore.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnCartridgeStoreCollectionChanged);
			Model.CartridgeStore.SyncFromIsoStore();
		}

		private void StartCartridge(CartridgeTag cartTag)
		{
			// Starts the cartridge!
			App.Current.ViewModel.NavigateToGameHome(cartTag.Cartridge.Filename);
		}

		private void ShowCartridgeInfo(CartridgeTag cartTag)
		{
			// Show the cartridge info!
			App.Current.ViewModel.NavigateToCartridgeInfo(cartTag);
		}

        private void VisitIsoStoreHomePage()
		{
			// Browses to the page.
            Microsoft.Phone.Tasks.WebBrowserTask task = new Microsoft.Phone.Tasks.WebBrowserTask();
            task.Uri = new Uri("http://isostorespy.codeplex.com/", UriKind.Absolute);
            task.Show();
		}

        private void OnCartridgeStoreCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RefreshVisibilities();
        }

        private void RefreshVisibilities()
        {
            // Refreshes if cartridges are visible.
            AreCartridgesVisible = Model.CartridgeStore.Count > 0;
        }
	}
}
