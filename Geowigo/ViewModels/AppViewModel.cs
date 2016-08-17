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
using System.Text;
using Geowigo.Utils;
using System.Reflection;
using Windows.ApplicationModel.Activation;

namespace Geowigo.ViewModels
{	
	/// <summary>
	/// The application view model, which is responsible for application-wide flow and 
    /// control of the app and game UI.
	/// </summary>
	public class AppViewModel
	{
		#region Nested Classes

		/// <summary>
		/// A kind of message to display to the user when a game crash has occured.
		/// </summary>
		public enum CrashMessageType
		{
			NewGame,
			Restore,
			Runtime
		}

		#endregion

		#region Fields

		private MessageBoxManager _MBManagerInstance;

		private SoundManager _SoundManagerInstance;

        private SavegameManager _SavegameManagerInstance;

		private SystemTrayManager _SystemTrayManagerInstance;

		private InputManager _InputManagerInstance;

		private NavigationManager _NavigationManagerInstance;

        private LicensingManager _LicensingManagerInstance;

        private ActionPump _actionPump;

        private IContinuationActivatedEventArgs _LastContractContinuationActivatedEventArgs;

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

        #region AppVersion

        /// <summary>
        /// Gets the current version of the app.
        /// </summary>
        public string AppVersion
        {
            get
            {
                return Version.Parse(Assembly.GetExecutingAssembly()
                        .GetCustomAttributes(false)
                        .OfType<AssemblyFileVersionAttribute>()
                        .First()
                        .Version).ToString();
            }
        }

        #endregion

		#region InputManager

		/// <summary>
		/// Gets the input manager for this view model.
		/// </summary>
		public InputManager InputManager
		{
			get
			{
				return _InputManagerInstance ?? (_InputManagerInstance = new InputManager());
			}
		}

		#endregion

		#region IsScreenLockEnabled
		/// <summary>
		/// Gets or sets if the screen should lock automatically if the user does
		/// not interact with the app.
		/// </summary>
		public bool IsScreenLockEnabled
		{
			get
			{
				return PhoneApplicationService.Current.UserIdleDetectionMode == IdleDetectionMode.Enabled;
			}

			set
			{
				PhoneApplicationService.Current.UserIdleDetectionMode = value ? IdleDetectionMode.Enabled : IdleDetectionMode.Disabled;
			}
		}
		#endregion

        #region LicensingManager

        /// <summary>
        /// Gets the licensing manager for this view model.
        /// </summary>
        public LicensingManager LicensingManager
        {
            get
            {
                return _LicensingManagerInstance ?? (_LicensingManagerInstance = new LicensingManager());
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

		#region NavigationManager
		/// <summary>
		/// Gets the navigation manager for this view model.
		/// </summary>
		public NavigationManager NavigationManager
		{
			get
			{
				return _NavigationManagerInstance ?? (_NavigationManagerInstance = new NavigationManager(this));
			}
		}
		#endregion

		#region SavegameManager
		/// <summary>
		/// Gets the manager of savegames.
		/// </summary>
		public SavegameManager SavegameManager
		{
			get
			{
				return _SavegameManagerInstance ?? (_SavegameManagerInstance = new SavegameManager(this));
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

		#region SystemTrayManager
		/// <summary>
		/// Gets the manager of the system tray.
		/// </summary>
		public SystemTrayManager SystemTrayManager
		{
			get
			{
				return _SystemTrayManagerInstance ?? (_SystemTrayManagerInstance = new SystemTrayManager());
			}
		}
		#endregion

		#region HasRecoveredFromTombstone
		/// <summary>
		/// Gets if the app has recovered from tombstone this session.
		/// </summary>
		public bool HasRecoveredFromTombstone { get; private set; } 
		#endregion

        #region ContinuationActivatedEventArgs

        /// <summary>
        /// Gets the event arguments for an app contract activation, if there is one.
        /// </summary>
        public IContinuationActivatedEventArgs ContractContinuationActivatedEventArgs
        {
            get
            {
                IContinuationActivatedEventArgs e = _LastContractContinuationActivatedEventArgs;

                // Deletes the event so that it won't be used in forthcoming navigation events.
                _LastContractContinuationActivatedEventArgs = null;

                return e;
            }

            private set
            {
                _LastContractContinuationActivatedEventArgs = value;
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
		/// Displays a message box. If a message box is currently on-screen, it will be cancelled.
		/// </summary>
		/// <param name="mbox"></param>
		public void ShowMessageBox(WF.Player.Core.MessageBox mbox)
		{
			// Delegates this to the message box manager.
			MessageBoxManager.Show(mbox);
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
            // Marks the session as being recovered from tombstone.
            HasRecoveredFromTombstone = isRecoveringFromTombstone;
            
            // If the app was tombstoned, goes back to app's home and show
			// a message.
			if (isRecoveringFromTombstone)
			{
                // When an app recovers from tombstone, an uncancellable 
                // navigation occurs to display the last page on the stack.
                // Let us therefore alert the navigation manager about this
                // incoming navigation event.
                NavigationManager.PauseUntilNextNavigation();
                
                // Runs the following in a dispatcher frame because message boxes cannot 
                // be displayed before the first navigation event in the app.
                App.Current.RootFrame.Dispatcher.BeginInvoke(() =>
                {
                    // Displays a message.
                    System.Windows.MessageBox.Show("Geowigo could not resume the game because the app was tombstoned by your phone.\n\n" +
                        "This is likely to happen when the app remains in the background for too long, or if your phone's battery is low.",
                        "Cannot resume game",
                        MessageBoxButton.OK);

                    // Schedules the app to navigate towards the app home
                    // once the tombstone recovery is over.
                    NavigationManager.NavigateToAppHome(true);
                });
				
				return;
			}
			
			// Resume the engine if it was paused.
			if (Model.Core.GameState == WF.Player.Core.Engines.EngineGameState.Paused)
			{
				Model.Core.Resume();
			}
		}

        /// <summary>
        /// Called when the app is activated from a UX contract.
        /// </summary>
        /// <param name="e">Activation event</param>
        public void HandleAppContractActivated(IActivatedEventArgs e)
        {
            if (e is IContinuationActivatedEventArgs)
            {
                // Stores the event args, to be used for BaseViewModel navigation logic.
                ContractContinuationActivatedEventArgs = (IContinuationActivatedEventArgs)e;
            }
        }

		/// <summary>
		/// Called when a game crashed. Displays a message and goes back home.
		/// </summary>
		/// <param name="crashMessageType"></param>
		/// <param name="exception"></param>
		/// <param name="cartridge"></param>
		public void HandleGameCrash(CrashMessageType crashMessageType, Exception exception, Cartridge cartridge)
		{
			// Prepares the message.
			StringBuilder sb = new StringBuilder();
			sb.Append("A problem occurred while ");
			switch (crashMessageType)
			{
				case CrashMessageType.NewGame:
					sb.Append("starting a new game");
					break;

				case CrashMessageType.Restore:
					sb.Append("restoring the saved game");
					break;

				case CrashMessageType.Runtime:
					sb.Append("running the game");
					break;

				default:
					sb.Append("running the game");
					break;
			}
			sb.Append(", therefore Geowigo cannot go on. This most likely happens because of a faulty cartridge");
			if (crashMessageType != CrashMessageType.NewGame)
			{
				sb.Append(" or savegame");
			}
			sb.Append(".\n\nIf the problem persists, you should contact Geowigo's developers (\"get support\" from the main menu) or the Cartridge owner, ");
            string cartFullAuthor = cartridge.GetFullAuthor();
			sb.Append(cartFullAuthor);
			AggregateException agex = exception as AggregateException;
			if (agex == null || 
				(agex != null && agex.InnerExceptions != null && agex.InnerExceptions.Count > 0))
			{
				sb.Append(", quoting the following error messages that were raised during the crash:");
				List<string> messages = new List<string>();
				if (agex == null)
				{
					// In-depth dump of inner exception messages.
					Exception curex = exception;
					while (curex != null)
					{
						messages.Add(curex.Message);
						curex = curex.InnerException;
					}
					messages.Reverse();
				}
				else
				{
					foreach (Exception e in agex.InnerExceptions)
					{
						messages.Add(e.Message);
					}
				}
				int i = messages.Count;
				foreach (string message in messages)
				{
					sb.Append("\n" + i-- + "> " + message);
				}
			}
			else
			{
				sb.Append(".");
			}

			// Prepares the title.
			string caption = "";
			switch (crashMessageType)
			{
				case CrashMessageType.NewGame:
				case CrashMessageType.Restore:
					caption = "Cannot start game";
					break;

				case CrashMessageType.Runtime:
					caption = "Cartridge crashed";
					break;

				default:
					caption = "Game crash";
					break;
			}

            // Dump a log.
            DebugUtils.DumpException(exception, String.Format("Exception during play. Cartridge: {0} by {1}", cartridge.Name, cartFullAuthor), false);

			// Shows a message box.
			System.Windows.MessageBox.Show(
				sb.ToString(),
				caption,
				MessageBoxButton.OK
			);

			// Goes back!
			NavigationManager.NavigateToAppHome(true);
		}

        /// <summary>
        /// Called when a game started.
        /// </summary>
        /// <param name="tag">Cartridge that started.</param>
        /// <param name="savegame">Optional savegame restored when the game started.</param>
        public void HandleGameStarted(CartridgeTag tag, CartridgeSavegame savegame = null)
        {
            // Resets the session quick save.
            SavegameManager.InitSessionSavegames(tag, savegame);
        }

		/// <summary>
		/// Kills the current Wherigo Core instance and replaces it with a new one.
		/// </summary>
		public void HardResetCore()
		{
			// Unregisters the current core.
			UnregisterModel(Model);
			
			// Performs a hard reset.
			Model.HardResetCore();

			// Registers the model again.
			RegisterModel(Model);
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
			CartridgeTag tag = Model.CartridgeStore.GetCartridgeTag(Model.Core.Cartridge);
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
        /// Makes a custom savegame and prompts the user for a name.
        /// </summary>
        public void SaveGame()
        {
            SavegameManager.SaveAndPrompt();
        }

        /// <summary>
        /// Makes a quick save of the current game.
        /// </summary>
        public void SaveGameQuick()
        {
            SavegameManager.SaveQuick();
        }

		/// <summary>
		/// Vibrates the device for a moment in order to alert the user.
		/// </summary>
		public void Vibrate()
		{
			Microsoft.Devices.VibrateController.Default.Start(TimeSpan.FromSeconds(0.6));
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
        internal void BeginRunOnIdle(Action action)
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
			model.Core.CompassCalibrationRequested += new EventHandler(Core_CompassCalibrationRequested);
			model.Core.ShowStatusTextRequested += new EventHandler<StatusTextEventArgs>(Core_ShowStatusTextRequested);
			model.Core.PlayAlertRequested += new EventHandler<WherigoEventArgs>(Core_PlayAlertRequested);
			model.Core.CartridgeCrashed += new EventHandler<CrashEventArgs>(Core_CartridgeCrashed);
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
			model.Core.CompassCalibrationRequested -= new EventHandler(Core_CompassCalibrationRequested);
			model.Core.ShowStatusTextRequested -= new EventHandler<StatusTextEventArgs>(Core_ShowStatusTextRequested);
			model.Core.PlayAlertRequested -= new EventHandler<WherigoEventArgs>(Core_PlayAlertRequested);
			model.Core.CartridgeCrashed -= new EventHandler<CrashEventArgs>(Core_CartridgeCrashed);
		}

		private void Core_AttributeChanged(object sender, AttributeChangedEventArgs e)
		{
			// Show some debug info with attribute changes.
			string name = e.Object is Thing ? ((Thing)e.Object).Name : e.Object.ToString();
			System.Diagnostics.Debug.WriteLine("AttributeChanged: " + name + "." + e.PropertyName);
		}

        private void Core_CartridgeCompleted(object sender, WherigoEventArgs e)
        {
            // Logs a history entry for cartridge completion.
            Model.History.AddCompletedGame(Model.CartridgeStore.GetCartridgeTag(e.Cartridge));
        }

		private void Core_CartridgeCrashed(object sender, CrashEventArgs e)
		{
			// Displays a message, resets the core and goes back to app home.
			HandleGameCrash(CrashMessageType.Runtime, e.ExceptionObject, e.Cartridge);
		}

		private void Core_CompassCalibrationRequested(object sender, EventArgs e)
		{
			// Navigates to the compass calibration view.
			BeginRunOnIdle(NavigationManager.NavigateToCompassCalibration);
		}

		private void Core_InputRequested(object sender, ObjectEventArgs<Input> e)
		{
			// Alerts the input manager of this request.
			InputManager.HandleInputRequested(e.Object);
			
			// Navigates to the input view.
			NavigationManager.NavigateToView(e.Object);
		}

		private void Core_MessageBoxRequested(object sender, MessageBoxEventArgs e)
		{
			// Displays the message box.
			ShowMessageBox(e.Descriptor);
		}

		private void Core_PlayAlertRequested(object sender, WherigoEventArgs e)
		{
			Vibrate();
		}

		private void Core_PlaySoundRequested(object sender, ObjectEventArgs<Media> e)
		{
			PlayMediaSound(e.Object);
		}

        private void Core_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsBusy")
            {
                // Updates the tray progress indicator if the engine is busy.
                bool isBusy = Model.Core.IsBusy;
				if (isBusy)
				{
					SystemTrayManager.BeginShowLoading();
				}
				else
				{
					SystemTrayManager.HideLoading();
				}

                // Relays the information.
                OnCoreIsBusyChanged(isBusy);
            }
        }

        private void Core_SaveRequested(object sender, SavingEventArgs e)
        {
            // Saves game (autosave).
            SavegameManager.SaveAuto();

            // If e.CloseAfterSave, close the game.
            if (e.CloseAfterSave)
            {
                // Wait for the engine to be done doing what it's doing.
                BeginRunOnIdle(() =>
                {
                    // Shows a message box for that.
                    System.Windows.MessageBox.Show("The cartridge has requested to be terminated. The game has been automatically saved, and you will now be taken back to the main menu of the app.", "The game is ending.", MessageBoxButton.OK);

                    // Back to app home.
					NavigationManager.NavigateToAppHome(true);
                });
            }
        }

		private void Core_ScreenRequested(object sender, ScreenEventArgs e)
		{
			// Shows the right screen depending on the event.
			switch (e.Screen)
			{
				case ScreenType.Main:
					NavigationManager.NavigateToGameHome(Model.Core.Cartridge.Filename, GameHomeViewModel.SectionValue_Overview);
					break;

				case ScreenType.Locations:
				case ScreenType.Items:
					NavigationManager.NavigateToGameHome(Model.Core.Cartridge.Filename, GameHomeViewModel.SectionValue_World);
					break;

				case ScreenType.Inventory:
					NavigationManager.NavigateToGameHome(Model.Core.Cartridge.Filename, GameHomeViewModel.SectionValue_Inventory);
					break;

				case ScreenType.Tasks:
					NavigationManager.NavigateToGameHome(Model.Core.Cartridge.Filename, GameHomeViewModel.SectionValue_Tasks);
					break;

				case ScreenType.Details:
					NavigationManager.NavigateToView(e.Object);
					break;

				default:
					throw new InvalidOperationException(String.Format("Unknown WherigoScreenKind cannot be processed: {0}", e.Screen.ToString()));
			}
		}

		private void Core_ShowStatusTextRequested(object sender, StatusTextEventArgs e)
		{
			// Displays or hides the status text depending on its value.
			SystemTrayManager.StatusText = e.Text;
		}

		private void Core_StopSoundsRequested(object sender, WherigoEventArgs e)
		{
			StopAllSounds();
		}

		#endregion

		#endregion

	}
}
