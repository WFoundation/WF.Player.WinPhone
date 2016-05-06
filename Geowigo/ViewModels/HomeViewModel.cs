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
using System.Collections.Generic;
using System.Linq;
using Microsoft.Phone.Controls;

namespace Geowigo.ViewModels
{
	public class HomeViewModel : BaseViewModel
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

        #region IsRecentVisible


        public bool IsRecentVisible
        {
            get { return (bool)GetValue(IsRecentVisibleProperty); }
            set { SetValue(IsRecentVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsRecentVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsRecentVisibleProperty =
            DependencyProperty.Register("IsRecentVisible", typeof(bool), typeof(HomeViewModel), new PropertyMetadata(false));


        #endregion

        #region RecentCartridges


        public IEnumerable<CartridgeTag> RecentCartridges
        {
            get { return (IEnumerable<CartridgeTag>)GetValue(RecentCartridgesProperty); }
            set { SetValue(RecentCartridgesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RecentCartridges.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RecentCartridgesProperty =
            DependencyProperty.Register("RecentCartridges", typeof(IEnumerable<CartridgeTag>), typeof(HomeViewModel), new PropertyMetadata(null));


        #endregion

        #region AlphaGroupedCartridges


        public IEnumerable<AlphaKeyGroup<CartridgeTag>> AlphaGroupedCartridges
        {
            get { return (List<AlphaKeyGroup<CartridgeTag>>)GetValue(AlphaGroupedCartridgesProperty); }
            set { SetValue(AlphaGroupedCartridgesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AlphaGroupedCartridges.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AlphaGroupedCartridgesProperty =
            DependencyProperty.Register("AlphaGroupedCartridges", typeof(IEnumerable<AlphaKeyGroup<CartridgeTag>>), typeof(HomeViewModel), new PropertyMetadata(null));


        #endregion

        #region BackgroundImageBrush



        public ImageBrush BackgroundImageBrush
        {
            get { return (ImageBrush)GetValue(BackgroundImageBrushProperty); }
            set { SetValue(BackgroundImageBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BackgroundImageBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackgroundImageBrushProperty =
            DependencyProperty.Register("BackgroundImageBrush", typeof(ImageBrush), typeof(HomeViewModel), new PropertyMetadata(null));



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
				return _SyncProvidersCommand ?? (_SyncProvidersCommand = new RelayCommand(SyncAll));
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

        #region ShowSettingsCommand
        private ICommand _ShowSettingsCommand;

        public ICommand ShowSettingsCommand
        {
            get
            {
                if (_ShowSettingsCommand == null)
                {
                    _ShowSettingsCommand = new RelayCommand(ShowSettings);
                }

                return _ShowSettingsCommand;
            }
        }
        #endregion

		#endregion

		protected override void InitFromNavigation(NavigationInfo nav)
		{
			// Synchronizes the cartridge store.
            Model.CartridgeStore.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnCartridgeStoreCollectionChanged);
            Model.CartridgeStore.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(OnCartridgeStorePropertyChanged);
            if (Model.Settings.SyncOnStartUp)
            {
                Model.CartridgeStore.SyncAll();
            }
            else
            {
                Model.CartridgeStore.SyncFromIsoStore(); 
            }

            // Monitors the history.
            Model.History.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnHistoryCollectionChanged);

            // Initial refresh.
            RefreshRecentCartridges();
            RefreshAllCartridges();
            RefreshVisibilities();

			// Inits the app bar.
			RefreshAppBar();

            // Shows a warning message box about the version being obsolete.
            if (!Model.Settings.IgnoreObsoleteVersionWarning)
            {
                // Makes a message box.
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("This is the last version of Geowigo which supports Windows Phone 7.5 and 8.0.\n");
                sb.AppendLine("Despite being fully able to run Wherigo 1.0 cartridges, this version does not support some of the features one would expect from a modern Wherigo app, such as offline maps...\n");
                sb.AppendLine("If you can see this message, consider upgrading to Windows Phone 8.1 or later, or if you are already using a modern phone, make sure that Geowigo is up-to-date.\n");
                sb.AppendLine("Thank you, and have fun.\nMangatome");
                CheckBox cb = new CheckBox()
                {
                    Content = "Never show this message again",
                    IsChecked = false
                };
                CustomMessageBox cmb = new CustomMessageBox()
                {
                    Caption = "Compatibility Warning",
                    IsFullScreen = true,
                    Message = sb.ToString(),
                    Content = cb,
                    LeftButtonContent = "OK"
                };

                // Prepares an event handler for when the message box will be dismissed.
                cmb.Dismissing += (o, e) =>
                {
                    this.ApplicationBar.IsVisible = true;

                    Model.Settings.IgnoreObsoleteVersionWarning = cb.IsChecked.GetValueOrDefault();
                };
                
                // Shows the message box.
                this.ApplicationBar.IsVisible = false;
                App.Current.ViewModel.MessageBoxManager.AcceptAndShow(cmb);
            }
		}
        
        #region Menu Commands

        private void ShowSettings()
        {
            // Navigates.
            App.Current.ViewModel.NavigationManager.NavigateToSettings();
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
                App.Current.ViewModel.NavigationManager.NavigateToProviderLinkWizard(provider);
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
                    if (System.Windows.MessageBox.Show(String.Format("The savegame {0} could not be found, perhaps because it is not installed anymore.\n\nDo you want to remove history entries for this cartridge?", entry.RelatedSavegameName), "Savegame not found", MessageBoxButton.OKCancel) == System.Windows.MessageBoxResult.OK)
                    {
                        // Clears the history of the entries related to this cartridge.
                        Model.History.RemoveAllOf(entry.RelatedCartridgeGuid);
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

		private void SyncAll()
		{
			// Syncs providers that are not synced.
            Model.CartridgeStore.SyncAll();
		}

		private void GoToForumThread()
		{
			// Navigates to the forum thread.
			WebBrowserTask task = new WebBrowserTask();
			task.Uri = new Uri("http://forums.groundspeak.com/GC/index.php?showtopic=315741", UriKind.Absolute);
			task.Show();
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
            RefreshAllCartridges();
            
            RefreshVisibilities();

            RefreshRecentCartridges();
        }

        private void OnCartridgeStorePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Only refresh the background image if there's none right now and the store is not busy.
            if (e.PropertyName == "IsBusy" && !((CartridgeStore)sender).IsBusy && BackgroundImageBrush == null)
            {
                RefreshBackground();
            }
        }

        private void RefreshBackground()
        {
            // Shows a random cartridge poster as background, or nothing if no cartridge with 
            // a poster is installed.
            lock (Model.CartridgeStore.SyncRoot)
            {
                IEnumerable<CartridgeTag> posteredCartridges = Model.CartridgeStore.Where(ct => ct.Panorama != null);
                int cnt = posteredCartridges.Count();
                if (cnt < 1)
                {
                    BackgroundImageBrush = null;
                }
                else
                {                    
                    BackgroundImageBrush = new ImageBrush() 
                    {
                        ImageSource = posteredCartridges.ElementAt(new Random().Next(cnt)).Panorama, 
                        Stretch = Stretch.UniformToFill,
                        Opacity = 0.4d
                    };
                } 
            }
        }

        private void RefreshAllCartridges()
        {
            // Creates a list of groups of cartridges.
            AlphaGroupedCartridges = AlphaKeyGroup<CartridgeTag>.CreateGroups(Model.CartridgeStore, System.Globalization.CultureInfo.CurrentUICulture, ct => AlphaKeyGroup<CartridgeTag>.GetFirstAlphaNumChar(ct.Title), true);
        }

        private void OnHistoryCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RefreshRecentCartridges();
            
            RefreshVisibilities();
        }

        private void RefreshRecentCartridges()
        {
            IEnumerable<CartridgeTag> recentCarts = Model.History
                .OrderByDescending(he => he.Timestamp)
                .GroupBy(he => he.RelatedCartridgeFilename)
                .Take(8)
                .Select(ig => Model.CartridgeStore.GetCartridgeTagOrDefault(ig.Key))
                .Where(ct => ct != null);

            RecentCartridges = recentCarts.Count() > 0 ? recentCarts : null;
        }

        private void RefreshVisibilities()
        {
            AreCartridgesVisible = Model.CartridgeStore.Count > 0;
            IsHistoryVisible = Model.History.Count > 0;
            IsRecentVisible = RecentCartridges != null;
        }

		private void RefreshAppBar()
		{
            ApplicationBar = new ApplicationBar() { Mode = ApplicationBarMode.Minimized };
			ApplicationBar.CreateAndAddMenuItem(SyncProvidersCommand, "sync cartridges");
			ApplicationBar.CreateAndAddMenuItem(GetHelpCommand, "talk & get support");
            ApplicationBar.CreateAndAddMenuItem(ShowSettingsCommand, "settings");
		}
    }
}
