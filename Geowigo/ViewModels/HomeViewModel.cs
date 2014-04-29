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
using System.Windows.Data;
using Geowigo.Models.Providers;
using Microsoft.Phone.Shell;
using Geowigo.Utils;
using Microsoft.Phone.Tasks;

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

        #region IsHistoryVisible


        public bool IsHistoryVisible
        {
            get { return (bool)GetValue(IsHistoryVisibleProperty); }
            set { SetValue(IsHistoryVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsHistoryVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsHistoryVisibleProperty =
            DependencyProperty.Register("IsHistoryVisible", typeof(bool), typeof(HomeViewModel), new PropertyMetadata(false));


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

		#region ApplicationBar


		public IApplicationBar ApplicationBar
		{
			get { return (IApplicationBar)GetValue(ApplicationBarProperty); }
			set { SetValue(ApplicationBarProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ApplicationBar.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ApplicationBarProperty =
			DependencyProperty.Register("ApplicationBar", typeof(IApplicationBar), typeof(HomeViewModel), new PropertyMetadata(null));


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

        #region RunHistoryEntryActionCommand

        private ICommand _RunHistoryEntryActionCommand;

        /// <summary>
        /// Gets a command to run the action associated to an history entry.
        /// </summary>
        public ICommand RunHistoryEntryActionCommand
        {
            get
            {
                return _RunHistoryEntryActionCommand ?? (_RunHistoryEntryActionCommand = new RelayCommand<HistoryEntry>(RunHistoryEntryAction));
            }
        }
        #endregion

		#region RunProviderActionCommand

		private ICommand _RunProviderActionCommand;

		/// <summary>
		/// Gets a command to run the action associated with a cartridge provider.
		/// </summary>
		public ICommand RunProviderActionCommand
		{
			get
			{
				return _RunProviderActionCommand ?? (_RunProviderActionCommand = new RelayCommand<ICartridgeProvider>(RunProviderAction));
			}
		}
		#endregion

		#region SyncProvidersCommand

		private ICommand _SyncProvidersCommand;

		/// <summary>
		/// Gets a command to sync all providers.
		/// </summary>
		public ICommand SyncProvidersCommand
		{
			get
			{
				return _SyncProvidersCommand ?? (_SyncProvidersCommand = new RelayCommand(SyncProviders));
			}
		}
		#endregion

		#region ClearHistoryCommand

		private ICommand _ClearHistoryCommand;

		/// <summary>
		/// Gets a command to clear the history.
		/// </summary>
		public ICommand ClearHistoryCommand
		{
			get
			{
				return _ClearHistoryCommand ?? (_ClearHistoryCommand = new RelayCommand(ClearHistory));
			}
		}
		#endregion

		#region GetHelpCommand

		private ICommand _GetHelpCommand;

		/// <summary>
		/// Gets a command to get some help.
		/// </summary>
		public ICommand GetHelpCommand
		{
			get
			{
				return _GetHelpCommand ?? (_GetHelpCommand = new RelayCommand(GoToForumThread));
			}
		}
		#endregion

		#region CalibrateCompassCommand

		private ICommand _CalibrateCompassCommand;

		/// <summary>
		/// Gets a command to start calibrating the compass.
		/// </summary>
		public ICommand CalibrateCompassCommand
		{
			get
			{
				return _CalibrateCompassCommand ?? (_CalibrateCompassCommand = new RelayCommand(CalibrateCompass, CanCalibrateCompassExecute));
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
			App.Current.ViewModel.NavigationManager.ClearBackStack();
			
			// Synchronizes the cartridge store.
            RefreshVisibilities();
            Model.CartridgeStore.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnCartridgeStoreCollectionChanged);
			Model.CartridgeStore.SyncFromIsoStore();

            // Monitors the history.
            Model.History.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnHistoryCollectionChanged);

			// Inits the app bar.
			RefreshAppBar();
		}
        
        #region Menu Commands

        private void StartCartridge(CartridgeTag cartTag)
        {
            // Starts the cartridge!
            App.Current.ViewModel.NavigationManager.NavigateToGameHome(cartTag.Cartridge.Filename);
        }

        private void ShowCartridgeInfo(CartridgeTag cartTag)
        {
            // Show the cartridge info!
            App.Current.ViewModel.NavigationManager.NavigateToCartridgeInfo(cartTag);
        }

		private void RunProviderAction(ICartridgeProvider provider)
		{
			if (provider.IsLinked)
			{
				if (provider.IsSyncing)
				{
					// The provider is syncing. Show it.
					System.Windows.MessageBox.Show(String.Format("Your {0} account is linked, and the app is currently looking for or downloading cartridges.", provider.ServiceName), provider.ServiceName, MessageBoxButton.OK);
				}
				else
				{
					// The provider is linked but no cartridge has been downloaded yet.
					// Show it.
					if (System.Windows.MessageBox.Show(String.Format("Your {0} account is linked, but no cartridge has been downloaded yet.\nDo you want to sync again?", provider.ServiceName), provider.ServiceName, MessageBoxButton.OKCancel) == System.Windows.MessageBoxResult.OK)
					{
						provider.BeginSync();
					}
				}
			}
			else
			{
				// The provider is not linked: try to do it.
				provider.BeginLink();
			}
		}

        private void RunHistoryEntryAction(HistoryEntry entry)
        {
            // Is the entry is related to saving the game?
            bool isRelatedToSavegame = entry.EntryType == HistoryEntry.Type.Saved ||
                entry.EntryType == HistoryEntry.Type.Restored;

            // Gets the cartridge tag if it still exists.
            CartridgeTag tag = entry.CartridgeTag;

            // Does the cartridge still exist?
            // NO -> Asks for clearing the history.
            if (tag == null)
            {
                // Asks for clearing the history.
                if (System.Windows.MessageBox.Show(String.Format("The cartridge {0} could not be found, perhaps because it is not installed anymore.\n\nDo you want to remove the history entries for this cartridge?", entry.RelatedCartridgeName), "Cartridge not found", MessageBoxButton.OKCancel) == System.Windows.MessageBoxResult.OK)
                {
                    // Clears the history of the entries related to this cartridge.
                    Model.History.RemoveAllOf(entry.RelatedCartridgeGuid);
                }

                // No more to do.
                return;
            }

            // Is the entry is related to saving the game?
            // YES -> Asks if the user wants to restore it.
            // NO -> Go to the cartridge info page.
            if (isRelatedToSavegame)
            {
                // Gets the savegame if it still exists.
                CartridgeSavegame savegame = entry.Savegame;

                // Does the savegame still exist?
                // NO -> Asks for removing the entry.
                // YES -> Restore it.
                if (savegame == null)
                {
                    // Asks for removing the entry.
                    if (System.Windows.MessageBox.Show(String.Format("The savegame {0} could not be found, perhaps because it is not installed anymore.\n\nDo you want to remove this history entry?", entry.RelatedSavegameName), "Savegame not found", MessageBoxButton.OKCancel) == System.Windows.MessageBoxResult.OK)
                    {
                        // Clears the history of the entries related to this cartridge.
                        Model.History.Remove(entry);
                    } 
                }
                else
                {
                    // Restores the cartridge.
                    App.Current.ViewModel.NavigationManager.NavigateToGameHome(tag.Cartridge.Filename, savegame);
                }
            }
            else
            {
                // Navigates to the cartridge info.
                // (We know the cartridge tag exists at this point.)
                ShowCartridgeInfo(tag);
            }

        }

		private void SyncProviders()
		{
			// Syncs providers that are not synced.
			Model.CartridgeStore.SyncFromProviders();
		}

		private void ClearHistory()
		{
			// Asks for clearing the history.
			if (System.Windows.MessageBox.Show("Do you want to delete all entries of the history?", "Clear history", MessageBoxButton.OKCancel) == System.Windows.MessageBoxResult.OK)
			{
				App.Current.ViewModel.ClearHistory();
			}
		}

		private void GoToForumThread()
		{
			// Navigates to the forum thread.
			WebBrowserTask task = new WebBrowserTask();
			task.Uri = new Uri("http://forums.groundspeak.com/GC/index.php?showtopic=315741", UriKind.Absolute);
			task.Show();
		}

		private void CalibrateCompass()
		{
			// Navigates to the right page.
			App.Current.ViewModel.NavigationManager.NavigateToCompassCalibration();			
		}

		private bool CanCalibrateCompassExecute()
		{
			return Model.Core.IsCompassSupported;
		}

        #endregion

        #region Collection View Sources

        public void InitCollectionViewSources(CollectionViewSource historySource)
        {
            // The history is sorted by Timestamp descending.
            historySource.SortDescriptions.Add(new System.ComponentModel.SortDescription("Timestamp", System.ComponentModel.ListSortDirection.Descending));
        }

        #endregion

        private void OnCartridgeStoreCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RefreshVisibilities();
        }

        private void OnHistoryCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RefreshVisibilities();
        }

        private void RefreshVisibilities()
        {
            AreCartridgesVisible = Model.CartridgeStore.Count > 0;
            IsHistoryVisible = Model.History.Count > 0;
        }

		private void RefreshAppBar()
		{
			ApplicationBar = new ApplicationBar();
			ApplicationBar.CreateAndAddMenuItem(ClearHistoryCommand, "clear history");
			ApplicationBar.CreateAndAddMenuItem(SyncProvidersCommand, "sync cartridges");
			ApplicationBar.CreateAndAddMenuItem(CalibrateCompassCommand, "calibrate compass");
			ApplicationBar.CreateAndAddMenuItem(GetHelpCommand, "talk & get support");
		}
    }
}
