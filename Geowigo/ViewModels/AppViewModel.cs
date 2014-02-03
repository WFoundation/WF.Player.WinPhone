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
using WF.Player.Core;
using System.IO.IsolatedStorage;
using Geowigo.Models;
using System.Collections.Generic;
using Microsoft.Phone.Controls;
using System.IO;
using System.Windows.Navigation;
using System.Linq;
using Microsoft.Phone.Shell;
using WF.Player.Core.Threading;

namespace Geowigo.ViewModels
{	
	/// <summary>
	/// The application view model, which is responsible for application-wide flow and 
    /// control of the app and game UI.
	/// </summary>
	public class AppViewModel
	{

		#region Members

		private MessageBoxManager _MBManagerInstance;

		private SoundManager _SoundManagerInstance;

        private SavegameManager _SavegameManagerInstance;

        private ActionPump _actionPump;

		#endregion

		#region Properties

		#region Model
		/// <summary>
		/// Gets or sets the wherigo model used by this ViewModel.
		/// </summary>
		public WherigoModel Model 
		{
			get
			{
				return _Model;
			}
			set
			{
				if (_Model != value)
				{
					// Unregisters the current model.
					if (_Model != null)
					{
						UnregisterModel(_Model);
					}

					// Changes the model.
					_Model = value;

					// Registers the new model.
					if (_Model != null)
					{
						RegisterModel(_Model);
					}
				}
			}
		}

		private WherigoModel _Model;
		#endregion

		#region AppTitle

		/// <summary>
		/// Gets the title of the application.
		/// </summary>
		public string AppTitle
		{
			get
			{
				return "Geowigo Wherigo Player";
			}
		}

		#endregion

		#region MessageBoxManager

		/// <summary>
		/// Gets the message box manager for this view model.
		/// </summary>
		public MessageBoxManager MessageBoxManager
		{
			get
			{
				return _MBManagerInstance ?? (_MBManagerInstance = new MessageBoxManager());
			}
		}

		#endregion

		#region SoundManager

		/// <summary>
		/// Gets the sound manager for this view model.
		/// </summary>
		public SoundManager SoundManager
		{
			get
			{
				return _SoundManagerInstance ?? (_SoundManagerInstance = new SoundManager());
			}
		}

		#endregion

        #region SavegameManager
        public SavegameManager SavegameManager
        {
            get
            {
                return _SavegameManagerInstance ?? (_SavegameManagerInstance = new SavegameManager(this));
            }
        }
        #endregion

		#endregion

        #region Constructors

        public AppViewModel()
        {
            _actionPump = new ActionPump();

            MessageBoxManager.HasMessageBoxChanged += new EventHandler(MessageBoxManager_HasMessageBoxChanged);
        }

        #endregion

		#region Public Methods

        /// <summary>
        /// Removes all entries from the user history.
        /// </summary>
        public void ClearHistory()
        {
            // Bye bye history.
            Model.History.Clear();
        }

		/// <summary>
		/// Navigates the app to the main page of the app.
		/// </summary>
		public void NavigateToAppHome(bool stopCurrentGame = false)
		{
			// Stops the current game if needed.
			if (stopCurrentGame && Model.Core.Cartridge != null)
			{
				Model.Core.Stop();
				Model.Core.Reset();
			}

			// Removes all back entries until the app home is found.
			string prefix = "/Views/";
			foreach (JournalEntry entry in App.Current.RootFrame.BackStack.ToList())
			{
				if (entry.Source.ToString().StartsWith(prefix + "HomePage.xaml"))
				{
					break;
				}

				// Removes the current entry.
				App.Current.RootFrame.RemoveBackEntry();
			}

			// If there is a back entry, goes back: it is the game home.
			// Otherwise, navigates to the game home.
			if (App.Current.RootFrame.BackStack.Count() > 0)
			{
				App.Current.RootFrame.GoBack();
			}
			else
			{
				App.Current.RootFrame.Navigate(new Uri(prefix + "HomePage.xaml", UriKind.Relative));
			}
		}

		/// <summary>
		/// Navigates the app to the game main page of a cartridge.
		/// </summary>
		public void NavigateToGameHome(string filename)
		{
			App.Current.RootFrame.Navigate(new Uri(String.Format("/Views/GameHomePage.xaml?{0}={1}", GameHomeViewModel.CartridgeFilenameKey, filename), UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the game main page of a cartridge at a specific section.
		/// </summary>
		public void NavigateToGameHome(string filename, string section)
		{
			App.Current.RootFrame.Navigate(new Uri(String.Format("/Views/GameHomePage.xaml?{0}={1}&{2}={3}", GameHomeViewModel.CartridgeFilenameKey, filename, GameHomeViewModel.SectionKey, section), UriKind.Relative));
		}

        /// <summary>
        /// Navigates the app to the game main page of a cartridge and restores a
        /// savegame.
        /// </summary>
        public void NavigateToGameHome(string filename, CartridgeSavegame savegame)
        {
            App.Current.RootFrame.Navigate(new Uri(String.Format(
                "/Views/GameHomePage.xaml?{0}={1}&{2}={3}", 
                GameHomeViewModel.CartridgeFilenameKey, 
                filename,
                GameHomeViewModel.SavegameFilenameKey,
                savegame.SavegameFile), UriKind.Relative));
        }

		/// <summary>
		/// Navigates the app to the info page of a cartridge.
		/// </summary>
		public void NavigateToCartridgeInfo(CartridgeTag tag)
		{
			App.Current.RootFrame.Navigate(new Uri(String.Format("/Views/CartridgeInfoPage.xaml?{0}={1}&{2}={3}", CartridgeInfoViewModel.CartridgeFilenameKey, tag.Cartridge.Filename, CartridgeInfoViewModel.CartridgeIdKey, tag.Guid), UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the view that best fits an Input object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(Input wherigoObj)
		{
			App.Current.RootFrame.Navigate(new Uri("/Views/InputPage.xaml?wid=" + wherigoObj.ObjIndex, UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the view that best fits a Thing object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(Thing wherigoObj)
		{
			App.Current.RootFrame.Navigate(new Uri("/Views/ThingPage.xaml?wid=" + wherigoObj.ObjIndex, UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the view that best fits a Task object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(Task wherigoObj)
		{
			App.Current.RootFrame.Navigate(new Uri("/Views/TaskPage.xaml?wid=" + wherigoObj.ObjIndex, UriKind.Relative));
		}

		/// <summary>
		/// Navigates the app to the view that best fits a UIObject object.
		/// </summary>
		/// <param name="wherigoObj"></param>
		public void NavigateToView(UIObject wherigoObj)
		{
			if (wherigoObj is Thing)
			{
				NavigateToView((Thing)wherigoObj);
			}
			else if (wherigoObj is Task)
			{
				NavigateToView((Task)wherigoObj);
			}
		}

		/// <summary>
		/// Displays a message box. If a message box is currently on-screen, it will be cancelled.
		/// </summary>
		/// <param name="mbox"></param>
		public void ShowMessageBox(WF.Player.Core.MessageBox mbox)
		{
			// Delegates this to the message box manager.
			MessageBoxManager.Show(mbox);
		}

		/// <summary>
		/// Navigates one step back in the game activity.
		/// </summary>
		/// <remarks>
		/// This method has no effect if the previous view in the stack is not a game view.
		/// </remarks>
		public void NavigateBack()
		{
			// Returns if the previous view is not a game view.
			JournalEntry previousPage = App.Current.RootFrame.BackStack.FirstOrDefault();
			if (previousPage == null)
			{
				System.Diagnostics.Debug.WriteLine("AppViewModel: WARNING: NavigateBack() cancelled because no page in the stack.");
				
				return;
			}
			string previousPageName = previousPage.Source.ToString();
			string prefix = "/Views/";
			if (!(previousPageName.StartsWith(prefix + "GameHomePage.xaml") ||
				previousPageName.StartsWith(prefix + "InputPage.xaml") ||
				previousPageName.StartsWith(prefix + "TaskPage.xaml") ||
				previousPageName.StartsWith(prefix + "ThingPage.xaml")))
			{
				System.Diagnostics.Debug.WriteLine("AppViewModel: WARNING: NavigateBack() cancelled because previous page is no game!");
				
				return;
			}

			// Goes back.
			App.Current.RootFrame.GoBack();
		}

		/// <summary>
		/// Clears the navigation back stack, making the current view the first one.
		/// </summary>
		public void ClearBackStack()
		{
			int entriesToRemove = App.Current.RootFrame.BackStack.Count();
			for (int i = 0; i < entriesToRemove; i++)
			{
				App.Current.RootFrame.RemoveBackEntry();
			}
		}

		/// <summary>
		/// Called when the app is being deactivated.
		/// </summary>
		public void HandleAppDeactivated()
		{
			if (Model.Core.GameState == WF.Player.Core.Engines.EngineGameState.Playing)
			{
				Model.Core.Pause();
			}
		}

		/// <summary>
		/// Called when the app is being activated.
		/// </summary>
		/// <param name="isRecoveringFromTombstone"></param>
		public void HandleAppActivated(bool isRecoveringFromTombstone)
		{
			if (Model.Core.GameState == WF.Player.Core.Engines.EngineGameState.Paused)
			{
				Model.Core.Resume();
			}
		}

		/// <summary>
		/// Sets the system tray progress indicator.
		/// </summary>
		/// <param name="status"></param>
		/// <param name="isIndeterminate"></param>
		/// <param name="isVisible"></param>
		public void SetSystemTrayProgressIndicator(string status = null, bool isIndeterminate = true, bool isVisible = true)
		{
			Microsoft.Phone.Shell.SystemTray.ProgressIndicator = new Microsoft.Phone.Shell.ProgressIndicator()
			{
				IsIndeterminate = isIndeterminate,
				IsVisible = isVisible,
				Text = status
			};
		}

		/// <summary>
		/// Plays a media sound.
		/// </summary>
		/// <param name="media">The sound to play.</param>
		public void PlayMediaSound(Media media)
		{
			// Sanity check.
			if (!SoundManager.IsPlayableSound(media))
			{
				System.Diagnostics.Debug.WriteLine("AppViewModel: Ignored playing sound of unsupported type: " + media.Type.ToString());
				return;
			}
			
			// Gets the media filename in cache.
			CartridgeTag tag = Model.CartridgeStore.GetCartridgeTagOrDefault(Model.Core.Cartridge);
			string filename = tag.GetMediaCachePath(media, true);

			// Plays the file.
			SoundManager.PlaySound(filename);
		}

		/// <summary>
		/// Stops all currently played sounds.
		/// </summary>
		public void StopAllSounds()
		{
			SoundManager.StopSounds();
		}

        /// <summary>
        /// Starts the protocol of saving the current game.
        /// </summary>
        public void SaveGame(bool isAutoSave)
        {
            // Delegates the protocol to the SavegameManager.
            SavegameManager.SaveGame(isAutoSave);
        }

		#endregion

		#region Private Methods

        #region Action Pump Management

        private void OnCoreIsBusyChanged(bool isBusy)
        {
            // Refreshes if the action pump is pumping.
            _actionPump.IsPumping = !isBusy && !MessageBoxManager.HasMessageBox;
        }

        private void MessageBoxManager_HasMessageBoxChanged(object sender, EventArgs e)
        {
            // Refreshes if the action pump is pumping.
            _actionPump.IsPumping = !Model.Core.IsBusy && !MessageBoxManager.HasMessageBox;
        }

        /// <summary>
        /// Runs an action as soon as the engine is not busy and no
        /// game message box is onscreen.
        /// </summary>
        /// <param name="action"></param>
        private void BeginRunOnIdle(Action action)
        {            
            // Wraps the action in a dispatcher action.
            Action wrapper = new Action(() =>
            {
                App.Current.RootFrame.Dispatcher.BeginInvoke(action);
            });
            
            // If the engine is idle, runs the action immediately.
            if (!Model.Core.IsBusy && !MessageBoxManager.HasMessageBox)
            {
                wrapper();
            }
            else
            {
                _actionPump.AcceptAction(wrapper);
            }
        }

        #endregion

        #region Model Event Handlers
        
        private void RegisterModel(WherigoModel model)
		{
			model.Core.InputRequested += new EventHandler<ObjectEventArgs<Input>>(Core_InputRequested);
			model.Core.ShowMessageBoxRequested += new EventHandler<MessageBoxEventArgs>(Core_MessageBoxRequested);
			model.Core.ShowScreenRequested += new EventHandler<ScreenEventArgs>(Core_ScreenRequested);
			model.Core.PlayMediaRequested += new EventHandler<ObjectEventArgs<Media>>(Core_PlaySoundRequested);
			model.Core.StopSoundsRequested += new EventHandler<WherigoEventArgs>(Core_StopSoundsRequested);
            model.Core.SaveRequested += new EventHandler<SavingEventArgs>(Core_SaveRequested);
			model.Core.AttributeChanged += new EventHandler<AttributeChangedEventArgs>(Core_AttributeChanged);
			model.Core.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Core_PropertyChanged);
            model.Core.CartridgeCompleted += new EventHandler<WherigoEventArgs>(Core_CartridgeCompleted);
		}

		private void UnregisterModel(WherigoModel model)
		{
			model.Core.InputRequested -= new EventHandler<ObjectEventArgs<Input>>(Core_InputRequested);
			model.Core.ShowMessageBoxRequested -= new EventHandler<MessageBoxEventArgs>(Core_MessageBoxRequested);
			model.Core.ShowScreenRequested -= new EventHandler<ScreenEventArgs>(Core_ScreenRequested);
			model.Core.PlayMediaRequested -= new EventHandler<ObjectEventArgs<Media>>(Core_PlaySoundRequested);
			model.Core.StopSoundsRequested -= new EventHandler<WherigoEventArgs>(Core_StopSoundsRequested);
            model.Core.SaveRequested -= new EventHandler<SavingEventArgs>(Core_SaveRequested);
            model.Core.AttributeChanged -= new EventHandler<AttributeChangedEventArgs>(Core_AttributeChanged);
            model.Core.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(Core_PropertyChanged);
            model.Core.CartridgeCompleted -= new EventHandler<WherigoEventArgs>(Core_CartridgeCompleted);
		}

        private void Core_CartridgeCompleted(object sender, WherigoEventArgs e)
        {
            // Logs a history entry for cartridge completion.
            Model.History.AddCompletedGame(Model.CartridgeStore.GetCartridgeTagOrDefault(e.Cartridge));
        }

        private void Core_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsBusy")
            {
                // Updates the tray progress indicator if the engine is busy.
                bool isBusy = Model.Core.IsBusy;
                SetSystemTrayProgressIndicator("Loading...", true, isBusy);

                // Relays the information.
                OnCoreIsBusyChanged(isBusy);
            }
        }

        private void Core_AttributeChanged(object sender, AttributeChangedEventArgs e)
        {
            // Show some debug info with attribute changes.
            string name = e.Object is Thing ? ((Thing)e.Object).Name : e.Object.ToString();
            System.Diagnostics.Debug.WriteLine("AttributeChanged: " + name + "." + e.PropertyName);
        }

        private void Core_SaveRequested(object sender, SavingEventArgs e)
        {
            // Saves game.
            SaveGame(true);

            // If e.CloseAfterSave, close the game.
            if (e.CloseAfterSave)
            {
                // Wait for the engine to be done doing what it's doing.
                BeginRunOnIdle(() =>
                {
                    // Shows a message box for that.
                    System.Windows.MessageBox.Show("The cartridge has requested to be terminated. The game has been automatically saved, and you will now be taken back to the main menu of the app.", "The game is ending.", MessageBoxButton.OK);

                    // Back to app home.
                    NavigateToAppHome(true);
                });
            }
        }

		private void Core_InputRequested(object sender, ObjectEventArgs<Input> e)
		{
			// Navigates to the input view.
			NavigateToView(e.Object);
		}

		private void Core_MessageBoxRequested(object sender, MessageBoxEventArgs e)
		{
			// Displays the message box.
			ShowMessageBox(e.Descriptor);
		}

		private void Core_ScreenRequested(object sender, ScreenEventArgs e)
		{
			// Shows the right screen depending on the event.
			switch (e.Screen)
			{
				case ScreenType.Main:
					NavigateToGameHome(Model.Core.Cartridge.Filename, GameHomeViewModel.SectionValue_Overview);
					break;

				case ScreenType.Locations:
				case ScreenType.Items:
					NavigateToGameHome(Model.Core.Cartridge.Filename, GameHomeViewModel.SectionValue_World);
					break;

				case ScreenType.Inventory:
					NavigateToGameHome(Model.Core.Cartridge.Filename, GameHomeViewModel.SectionValue_Inventory);
					break;

				case ScreenType.Tasks:
					NavigateToGameHome(Model.Core.Cartridge.Filename, GameHomeViewModel.SectionValue_Tasks);
					break;

				case ScreenType.Details:
					NavigateToView(e.Object);
					break;

				default:
					throw new InvalidOperationException(String.Format("Unknown WherigoScreenKind cannot be processed: {0}", e.Screen.ToString()));
			}
		}

		private void Core_PlaySoundRequested(object sender, ObjectEventArgs<Media> e)
		{
			PlayMediaSound(e.Object);
		}

		private void Core_StopSoundsRequested(object sender, WherigoEventArgs e)
		{
			StopAllSounds();
		}


		#endregion

		#endregion

        #region Beta-Specific Features

        /// <summary>
        /// Starts the Beta-specific features.
        /// </summary>
        internal void GoBeta()
        {
            // Navigates to the beta license page.
            //App.Current.RootFrame.Navigating += new NavigatingCancelEventHandler(Beta_Navigating);
            App.Current.RootFrame.Navigated += new NavigatedEventHandler(Beta_Navigated);
        }

        void Beta_Navigated(object sender, NavigationEventArgs e)
        {
            // Bye bye event handler.
            App.Current.RootFrame.Navigated -= new NavigatedEventHandler(Beta_Navigated);

            // Navigates to the new page.
            App.Current.RootFrame.Dispatcher.BeginInvoke(new Action(() =>
            {
                App.Current.RootFrame.Navigate(new Uri("/Geowigo.Beta;component/BetaLicensePage.xaml", UriKind.Relative));
            }));

            // Adds a new event handler for starting the rest of beta features.
            App.Current.RootFrame.Navigated += new NavigatedEventHandler(Beta_Navigated2);
        }

        void Beta_Navigated2(object sender, NavigationEventArgs e)
        {
            if (e.Uri.OriginalString.Contains("/HomePage.xaml"))
            {
                // Go beta
                GoBetaRest();
            }
        }

        private IApplicationBar _appBar;
        private void GoBetaRest()
        {
            // Registers events for update checks.
            Beta.UpdateManager updateMan = new Beta.UpdateManager();
            updateMan.UpdateFound += new EventHandler(Beta_UpdateFound);
            updateMan.BeginCheckForUpdate();

            // Injects the beta appbar in the application.
            if (_appBar == null)
            {
                _appBar = Beta.BetaManager.Instance.BetaAppBar;

                ApplicationBarMenuItem iem = new ApplicationBarMenuItem("clear history");
                iem.Click += new EventHandler(Beta_ClearHistoryMenuItemClick);
                _appBar.MenuItems.Add(iem);

				ApplicationBarMenuItem iem2 = new ApplicationBarMenuItem("sync cartridges");
				iem2.Click += new EventHandler(Beta_SyncCartridgesMenuItemClick);
				_appBar.MenuItems.Add(iem2);
            }
            ((PhoneApplicationPage)App.Current.RootFrame.Content).ApplicationBar = _appBar;
        }

        private void Beta_ClearHistoryMenuItemClick(object sender, EventArgs e)
        {
            if (System.Windows.MessageBox.Show("Do you want to delete all entries of the history?", "Clear history", MessageBoxButton.OKCancel) == System.Windows.MessageBoxResult.OK)
            {
                ClearHistory();
            }
        }

		private void Beta_SyncCartridgesMenuItemClick(object sender, EventArgs e)
		{
			Model.CartridgeStore.SyncFromProviders();
		}

        private void Beta_UpdateFound(object sender, EventArgs e)
        {
            Beta.UpdateManager updateMan = (Beta.UpdateManager) sender;

            if (updateMan != null && updateMan.HasNewerVersion)
            {
                // Pauses the game if possible.
                if (Model.Core.GameState == WF.Player.Core.Engines.EngineGameState.Playing)
                {
                    Model.Core.Pause();
                }

                // Shows the box.
                updateMan.ShowMessageBox();

                // Resumes the game if possible.
                if (Model.Core.GameState == WF.Player.Core.Engines.EngineGameState.Paused)
                {
                    Model.Core.Resume();
                }
            }
        }

        #endregion
    }
}
