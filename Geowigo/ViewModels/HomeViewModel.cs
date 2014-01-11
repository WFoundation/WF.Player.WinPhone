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
                    _VisitIsoStoreHomePageCommand = new RelayCommand(VisitIsoStoreSpyHomePage);
                }

                return _VisitIsoStoreHomePageCommand;
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

            // Monitors the history.
            Model.History.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(OnHistoryCollectionChanged);
		}
        
        #region Menu Commands

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

        /// <summary>
        /// Makes the app run the action most appropriate for
        /// an history entry.
        /// </summary>
        /// <param name="entry"></param>
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
                    App.Current.ViewModel.NavigateToGameHome(tag.Cartridge.Filename, savegame);
                }
            }
            else
            {
                // Navigates to the cartridge info.
                // (We know the cartridge tag exists at this point.)
                ShowCartridgeInfo(tag);
            }

        } 
        #endregion

        #region Collection View Sources

        public void InitCollectionViewSources(CollectionViewSource historySource)
        {
            // The history is sorted by Timestamp descending.
            historySource.SortDescriptions.Add(new System.ComponentModel.SortDescription("Timestamp", System.ComponentModel.ListSortDirection.Descending));
        }

        #endregion

        private void VisitIsoStoreSpyHomePage()
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

        private void OnHistoryCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RefreshVisibilities();
        }

        private void RefreshVisibilities()
        {
            AreCartridgesVisible = Model.CartridgeStore.Count > 0;
            IsHistoryVisible = Model.History.Count > 0;
        }
    }
}
