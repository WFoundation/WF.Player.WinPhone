﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WF.Player.Core;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Geowigo.Controls;
using Microsoft.Phone.Controls;
using System.Windows.Data;
using Microsoft.Phone.Shell;
using Geowigo.Utils;
using Geowigo.Models;
using System.IO.IsolatedStorage;
using System.ComponentModel;
using System.Text;

namespace Geowigo.ViewModels
{
	public class GameHomeViewModel : BaseViewModel
	{
        #region Constants
        public static readonly string CartridgeFilenameKey = "cartFilename";

        public static readonly string SectionKey = "section";
        public static readonly string SectionValue_Overview = "overview";
        public static readonly string SectionValue_World = "world";
        public static readonly string SectionValue_Inventory = "inventory";
        public static readonly string SectionValue_Tasks = "tasks";

        public static readonly string SavegameFilenameKey = "gwsFilename";
        #endregion

        #region Members

        private Action _cartridgeStartAction;
        private bool _isReady;
		private bool _isStartProcessLaunched;

		private object _syncRoot = new object();

        #endregion

		#region Dependency Properties

        #region CartridgeTag


        public CartridgeTag CartridgeTag
        {
            get { return (CartridgeTag)GetValue(CartridgeTagProperty); }
            set { SetValue(CartridgeTagProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CartridgeTag.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CartridgeTagProperty =
            DependencyProperty.Register("CartridgeTag", typeof(CartridgeTag), typeof(GameHomeViewModel), new PropertyMetadata(null));


        #endregion

		#region PivotSelectedIndex



		public int PivotSelectedIndex
		{
			get { return (int)GetValue(PivotSelectedIndexProperty); }
			set { SetValue(PivotSelectedIndexProperty, value); }
		}

		// Using a DependencyProperty as the backing store for PivotSelectedIndex.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty PivotSelectedIndexProperty =
			DependencyProperty.Register("PivotSelectedIndex", typeof(int), typeof(GameHomeViewModel), new PropertyMetadata(0));



		#endregion

		#region AreZonesVisible


		public bool AreZonesVisible
		{
			get { return (bool)GetValue(AreZonesVisibleProperty); }
			set { SetValue(AreZonesVisibleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for AreZonesVisible.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AreZonesVisibleProperty =
			DependencyProperty.Register("AreZonesVisible", typeof(bool), typeof(GameHomeViewModel), new PropertyMetadata(false));


		#endregion

		#region AreObjectsVisible


		public bool AreObjectsVisible
		{
			get { return (bool)GetValue(AreObjectsVisibleProperty); }
			set { SetValue(AreObjectsVisibleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for AreObjectsVisible.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AreObjectsVisibleProperty =
			DependencyProperty.Register("AreObjectsVisible", typeof(bool), typeof(GameHomeViewModel), new PropertyMetadata(false));


		#endregion

		#region AreCurrentTasksVisible


		public bool AreCurrentTasksVisible
		{
			get { return (bool)GetValue(AreCurrentTasksVisibleProperty); }
			set { SetValue(AreCurrentTasksVisibleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for AreCurrentTasksVisible.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AreCurrentTasksVisibleProperty =
			DependencyProperty.Register("AreCurrentTasksVisible", typeof(bool), typeof(GameHomeViewModel), new PropertyMetadata(false));


		#endregion

		#region AreHistoryTasksVisible


		public bool AreHistoryTasksVisible
		{
			get { return (bool)GetValue(AreHistoryTasksVisibleProperty); }
			set { SetValue(AreHistoryTasksVisibleProperty, value); }
		}

		// Using a DependencyProperty as the backing store for AreHistoryTasksVisible.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AreHistoryTasksVisibleProperty =
			DependencyProperty.Register("AreHistoryTasksVisible", typeof(bool), typeof(GameHomeViewModel), new PropertyMetadata(false));


		#endregion

		#region IsWorldEmpty


		public bool IsWorldEmpty
		{
			get { return (bool)GetValue(IsWorldEmptyProperty); }
			set { SetValue(IsWorldEmptyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsWorldEmpty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsWorldEmptyProperty =
			DependencyProperty.Register("IsWorldEmpty", typeof(bool), typeof(GameHomeViewModel), new PropertyMetadata(true));


		#endregion

		#region IsInventoryEmpty


		public bool IsInventoryEmpty
		{
			get { return (bool)GetValue(IsInventoryEmptyProperty); }
			set { SetValue(IsInventoryEmptyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsInventoryEmpty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsInventoryEmptyProperty =
			DependencyProperty.Register("IsInventoryEmpty", typeof(bool), typeof(GameHomeViewModel), new PropertyMetadata(true));


		#endregion

		#region IsTasksEmpty

		public bool IsTasksEmpty
		{
			get { return (bool)GetValue(IsTasksEmptyProperty); }
			set { SetValue(IsTasksEmptyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsTasksEmpty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsTasksEmptyProperty =
			DependencyProperty.Register("IsTasksEmpty", typeof(bool), typeof(GameHomeViewModel), new PropertyMetadata(true));


		#endregion

		#endregion

		#region Commands

		#region ShowDetailsCommand

		private ICommand _ShowDetailsCommand;

		/// <summary>
		/// Gets a command to show the details of a thing.
		/// </summary>
		public ICommand ShowDetailsCommand
		{
			get
			{
				return _ShowDetailsCommand ?? (_ShowDetailsCommand = new RelayCommand<UIObject>(ShowDetails));
			}
		}

		#endregion

		#region ShowSectionCommand

		private ICommand _ShowSectionCommand;

		/// <summary>
		/// Gets a command to show the details of a thing.
		/// </summary>
		public ICommand ShowSectionCommand
		{
			get
			{
				return _ShowSectionCommand ?? (_ShowSectionCommand = new RelayCommand<FrameworkElement>(ShowSectionFromTag));
			}
		}

		#endregion

        #region SaveGameCommand

        private ICommand _SaveGameCommand;

        /// <summary>
        /// Gets a command to save the game.
        /// </summary>
        public ICommand SaveGameCommand
        {
            get
            {
                return _SaveGameCommand ?? (_SaveGameCommand = new RelayCommand(StartSaveGame));
            }
        }

        #endregion

        #region QuickSaveGameCommand

        private ICommand _QuickSaveGameCommand;

        /// <summary>
        /// Gets a command to quick save the game.
        /// </summary>
        public ICommand QuickSaveGameCommand
        {
            get
            {
                return _QuickSaveGameCommand ?? (_QuickSaveGameCommand = new RelayCommand(QuickSaveGame));
            }
        }

        #endregion

		#region ShowMapCommand

		private ICommand _ShowMapCommand;

		/// <summary>
		/// Gets a command to show the details of a thing.
		/// </summary>
		public ICommand ShowMapCommand
		{
			get
			{
				return _ShowMapCommand ?? (_ShowMapCommand = new RelayCommand(ShowMap));
			}
		}

		#endregion

		#region ShowDeviceInfoCommand

		private ICommand _ShowDeviceInfoCommand;

		public ICommand ShowDeviceInfoCommand
		{
			get
			{
				if (_ShowDeviceInfoCommand == null)
				{
					_ShowDeviceInfoCommand = new RelayCommand(ShowDeviceInfo);
				}

				return _ShowDeviceInfoCommand;
			}
		}

		#endregion

		#endregion

		#region Tasks Collection View Sources
		/// <summary>
		/// Initializes filters for task collection view sources.
		/// </summary>
		/// <param name="_CurrentTasksSource"></param>
		/// <param name="_HistoryTasksSource"></param>
		public void InitCollectionViewSourcesForTasks(CollectionViewSource currrentTs, CollectionViewSource historyTs)
		{
			// Makes sure that there is one and only one event handler per source.

			currrentTs.Filter -= new FilterEventHandler(OnCurrentTasksSourceFilter);
			currrentTs.Filter += new FilterEventHandler(OnCurrentTasksSourceFilter);

			historyTs.Filter -= new FilterEventHandler(OnHistoryTasksSourceFilter);
			historyTs.Filter += new FilterEventHandler(OnHistoryTasksSourceFilter);
		}

		private void OnHistoryTasksSourceFilter(object sender, FilterEventArgs e)
		{
			// Only take completed tasks.
			e.Accepted = e.Item is Task && ((Task)e.Item).Complete;
		}

		private void OnCurrentTasksSourceFilter(object sender, FilterEventArgs e)
		{
			// Only take uncompleted tasks.
			e.Accepted = e.Item is Task && !((Task)e.Item).Complete;
		}

		#endregion

		protected override void OnPageBackKeyPressOverride(System.ComponentModel.CancelEventArgs e)
		{
			// Cancels the back key event.
			e.Cancel = true;

			// Do nothing if the page is not ready.
			lock (_syncRoot)
			{
				if (!_isStartProcessLaunched)
				{
					return;
				}
			}

			// Ask if we really want to leave the game.
			System.Windows.MessageBoxResult result = System.Windows.MessageBoxResult.None;
			try
			{
				result = System.Windows.MessageBox.Show(
                    "The game will stop and your unsaved progress will be lost.", 
                    "Exit to main menu?", 
                    MessageBoxButton.OKCancel);
			}
			catch (Exception ex)
			{
				// An exception with message "0x8000ffff" is thrown if this message box
				// is requested to be shown a second time while the first request has 
				// still not been processed. In this case, do nothing and hope that
				// the user has better luck next time. In other cases, throw the exception
				// back.
				if (ex.Message == null || !ex.Message.Contains("0x8000ffff"))
				{
					throw;
				}
			}
			if (result == System.Windows.MessageBoxResult.OK)
			{
				// Leaves to main menu and stops the game.
				App.Current.ViewModel.NavigationManager.NavigateToAppHome(true);
			}
		}

        public void OnPageReady()
        {
            // Remembers that the page is ready.
            _isReady = true;
            
            // If a start action is defined, now is time to run it in the
            // UI thread dispatcher.
            if (_cartridgeStartAction == null)
            {
				lock (_syncRoot)
				{
					_isStartProcessLaunched = true; 
				}
				return;
            }
            Dispatcher.BeginInvoke(() =>
            {
                // Runs the action.
                _cartridgeStartAction();
				lock (_syncRoot)
				{
					_isStartProcessLaunched = true; 
				}
            });
        }

		protected override void OnCorePropertyChanged(string propName)
		{
			// Refreshes all visibilities if the model is now ready. Otherwise tries our best
			// with the prop name we get.
			RefreshVisibilities("IsReady".Equals(propName) && Model.Core.IsReady ? null : propName);
		}

		protected override void OnCoreGameStateChanged(WF.Player.Core.Engines.EngineGameState oldState, WF.Player.Core.Engines.EngineGameState newState)
		{			
			// If the engine is stopping or pausing, enables screen lock again.
			if (newState == WF.Player.Core.Engines.EngineGameState.Stopping || newState == WF.Player.Core.Engines.EngineGameState.Pausing)
			{
				App.Current.ViewModel.IsScreenLockEnabled = false;
			}

			// Refreshes the blocking progress bar.
			RefreshProgressBar(newState);
		}

		protected override void OnModelChanged(Models.WherigoModel newModel)
		{
			if (newModel != null && newModel.Core.IsReady)
			{
				// Refreshes all visibilities.
				RefreshVisibilities();
			}
		}

		protected override void InitFromNavigation(NavigationInfo nav)
		{
			base.InitFromNavigation(nav);

			// We probably have a lot of things to do.
			// Let's block the UI and show some progress bar.
			RefreshProgressBar(Model.Core.GameState);

			System.Windows.Navigation.NavigationContext navCtx = nav.NavigationContext;

			// Tries to get a particular section to display.
			string section;
			if (navCtx.QueryString.TryGetValue(SectionKey, out section))
			{
				ShowSection(section);
			}

            // Refreshes the application bar.
            RefreshAppBar();

			// Nothing more to do if a cartridge exists already.
			if (Cartridge != null)
			{
				return;
			}

			// Makes sure the screen lock is disabled.
			App.Current.ViewModel.IsScreenLockEnabled = false;

			// Resets the custom status text.
			App.Current.ViewModel.SystemTrayManager.StatusText = null;

			// Tries to get the filename to query for.
			string filename;
			if (navCtx.QueryString.TryGetValue(CartridgeFilenameKey, out filename))
			{
				// Gets the cartridge tag for this cartridge.
				CartridgeTag = Model.CartridgeStore.GetCartridgeTag(filename);
				
				string gwsFilename;

                // Restores the cartridge or starts a new game?
                if (navCtx.QueryString.TryGetValue(SavegameFilenameKey, out gwsFilename))
                {
					// Starts restoring the game.
                    RunOrDeferIfNotReady(
                        new Action(() =>
                        {
                            // Starts logging.
                            if (Model.Settings.CanGenerateCartridgeLog)
                            {
                                Model.Core.StartLogging(CartridgeTag.CreateLogFile()); 
                            }
							
							// Restores the game.
							Model.Core.InitAndRestoreCartridgeAsync(filename, gwsFilename)
								.ContinueWith(t =>
								{
									// Keeps the cartridge.
									try
									{
										Cartridge = t.Result;
									}
									catch (AggregateException ex)
									{
                                        FailInit(ex, CartridgeTag, true);
									}

									// Registers a history entry.
									CartridgeSavegame savegame = CartridgeTag.Savegames.FirstOrDefault(cs => cs.SavegameFile == gwsFilename);
                                    Model.History.AddRestoredGame(CartridgeTag, savegame);

                                    // Lets the view model know we started the game.
                                    App.Current.ViewModel.HandleGameStarted(CartridgeTag, savegame);

								}, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
                        }));
                }
                else
                {
					// Starts a new game.
                    RunOrDeferIfNotReady(
                        new Action(() =>
                        {
                            // Starts logging.
                            if (Model.Settings.CanGenerateCartridgeLog)
                            {
                                Model.Core.StartLogging(CartridgeTag.CreateLogFile()); 
                            }
							
							// Starts the game.
							Model.Core.InitAndStartCartridgeAsync(filename)
								.ContinueWith(t =>
								{
									// Stores the result of the cartridge.
									try
									{
										Cartridge = t.Result;
									}
									catch (AggregateException ex)
									{
                                        FailInit(ex, CartridgeTag);
									}

									// Registers a history entry.
                                    Model.History.AddStartedGame(CartridgeTag);

                                    // Lets the view model know we started the game.
                                    App.Current.ViewModel.HandleGameStarted(CartridgeTag);

								}, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
                        }));
                }
			}

			// TODO: Cancel nav if no cartridge in parameter?

		}

		/// <summary>
		/// Displays a message when the async initialization failed,
		/// and returns back to the main menu.
		/// </summary>
		/// <param name="ex"></param>
		/// <param name="tag"></param>
		/// <param name="isRestore"></param>
		private void FailInit(AggregateException ex, CartridgeTag tag, bool isRestore = false)
		{
			// Let the app view model handle this crash.
			App.Current.ViewModel.HandleGameCrash(
				isRestore ? AppViewModel.CrashMessageType.Restore : AppViewModel.CrashMessageType.NewGame,
				ex,
				tag.Cartridge
				);
		}

        /// <summary>
        /// Runs an action now or defers it if the page is not ready yet.
        /// </summary>
        /// <param name="action"></param>
        private void RunOrDeferIfNotReady(Action action)
        {
            // Runs the action later if the page is not ready yet.
            if (_isReady)
            {
                // Runs the action.
				action();
            }
            else
            {
                _cartridgeStartAction = action;
            }
        }

        #region Menu Commands
		/// <summary>
		/// Makes the app show the device info page.
		/// </summary>
		private void ShowDeviceInfo()
		{
			// Shows the info!
			App.Current.ViewModel.NavigationManager.NavigateToPlayerInfo();
		}

        /// <summary>
        /// Starts the process of saving a game.
        /// </summary>
        private void StartSaveGame()
        {
			// Saves the game!
            App.Current.ViewModel.SaveGame();
        }

        /// <summary>
        /// Makes a quick save game.
        /// </summary>
        private void QuickSaveGame()
        {
            App.Current.ViewModel.SaveGameQuick();
        }

        /// <summary>
        /// Makes the app show the details of a thing.
        /// </summary>
        /// <param name="t"></param>
        private void ShowDetails(UIObject t)
        {
            if (t.HasOnClick)
            {
                // Runs the on-click command.
                t.CallOnClick();
            }
            else
            {
                // Navigates to the appropriate view.
                App.Current.ViewModel.NavigationManager.NavigateToView(t); 
            }
        }

        /// <summary>
        /// Shows a particular section of the view.
        /// </summary>
        /// <param name="section"></param>
        private void ShowSection(string section)
        {
            if (SectionValue_World.Equals(section))
            {
                PivotSelectedIndex = 1;
            }
            else if (SectionValue_Inventory.Equals(section))
            {
                PivotSelectedIndex = 2;
            }
            else if (SectionValue_Tasks.Equals(section))
            {
                PivotSelectedIndex = 3;
            }
        }

        /// <summary>
        /// Shows a particular section of the view from the tag of a FrameworkElement.
        /// </summary>
        /// <param name="element"></param>
        private void ShowSectionFromTag(FrameworkElement element)
        {
            if (element == null)
            {
                return;
            }

            ShowSection(element.Tag as string);
        }

		/// <summary>
		/// Makes the app show a map of the game zone.
		/// </summary>
		private void ShowMap()
		{
			App.Current.ViewModel.NavigationManager.NavigateToGameMap();
		}
        #endregion

		#region UI Refresh
		/// <summary>
		/// Refreshes the visibility of one or all wherigo object panels.
		/// </summary>
		/// <param name="propName"></param>
		private void RefreshVisibilities(string propName = null)
		{
			bool refreshAll = propName == null;
			bool refreshWorldEmpty = false;

			if (refreshAll || "ActiveVisibleZones".Equals(propName))
			{
				AreZonesVisible = Model.Core.ActiveVisibleZones.Count > 0;
				refreshWorldEmpty = true;
			}

			if (refreshAll || "VisibleObjects".Equals(propName))
			{
				AreObjectsVisible = Model.Core.VisibleObjects.Count > 0;
				refreshWorldEmpty = true;
			}

			if (refreshAll || "ActiveVisibleTasks".Equals(propName))
			{
				int incompleteTasksCount = Model.Core.ActiveVisibleTasks.Where(t => !t.Complete).Count();

				AreCurrentTasksVisible = incompleteTasksCount > 0;
				AreHistoryTasksVisible = Model.Core.ActiveVisibleTasks.Count - incompleteTasksCount > 0;

				IsTasksEmpty = !AreCurrentTasksVisible && !AreHistoryTasksVisible;
			}

			if (refreshAll || "VisibleInventory".Equals(propName))
			{
				IsInventoryEmpty = Model.Core.VisibleInventory.Count <= 0;
			}

			if (refreshAll || refreshWorldEmpty)
			{
				IsWorldEmpty = !AreZonesVisible && !AreObjectsVisible;
			}
		}

		/// <summary>
		/// Refreshes the progress bar depending on the game state.
		/// </summary>
		/// <param name="engineGameState"></param>
		private void RefreshProgressBar(WF.Player.Core.Engines.EngineGameState gameState)
		{
			// Computes the message to display.
			string message = null;
			switch (gameState)
			{
				case WF.Player.Core.Engines.EngineGameState.Stopping:
					message = "Stopping cartridge...";
					break;

				case WF.Player.Core.Engines.EngineGameState.Starting:
					message = "Starting game...";
					break;

				case WF.Player.Core.Engines.EngineGameState.Restoring:
					message = "Restoring game...";
					break;

				case WF.Player.Core.Engines.EngineGameState.Saving:
					message = "Saving cartridge...";
					break;

				case WF.Player.Core.Engines.EngineGameState.Playing:
					// message = null;
					break;

				case WF.Player.Core.Engines.EngineGameState.Initializing:
					message = "Loading cartridge...";
					break;

				default:
					message = ProgressBarStatusText ?? "";
					break;
			}

			// If we need to display a progress message, do it.
			ProgressBarStatusText = message;
			IsProgressBarVisible = message != null;
		}

		/// <summary>
		/// Refreshes the application bar.
		/// </summary>
		private void RefreshAppBar()
		{
			if (ApplicationBar != null)
			{
				return;
			}

			// Creates the app bar.
			ApplicationBar = new ApplicationBar();

			// Adds the savegame menu item.
			ApplicationBar.CreateAndAddMenuItem(SaveGameCommand, "save game");

			// Adds the device info menu item.
			ApplicationBar.CreateAndAddMenuItem(ShowDeviceInfoCommand, "player+device info");

			// Adds the maps menu item.
			ApplicationBar.CreateAndAddButton("appbar.map.treasure.png", ShowMapCommand, "map");

            // Adds the quick save button.
            ApplicationBar.CreateAndAddButton("appbar.save.png", QuickSaveGameCommand, "quick save");
		} 
		#endregion
	}
}
