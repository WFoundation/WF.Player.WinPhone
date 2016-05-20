using System;
using Geowigo.Models;
using System.Windows;
using System.Collections;
using Microsoft.Phone.Shell;
using System.Collections.Generic;
using System.Windows.Input;
using Geowigo.Controls;
using Geowigo.Utils;
using System.Device.Location;
using Microsoft.Phone.Tasks;
using System.Collections.ObjectModel;
using Microsoft.Phone.Controls;
using System.Linq;

namespace Geowigo.ViewModels
{
	public class CartridgeInfoViewModel : BaseViewModel
	{
        #region Nested Classes

        /// <summary>
        /// A list of savegames grouped by date.
        /// </summary>
        public class SavegameKeyGroup : List<CartridgeSavegame>
        {
            /// <summary>
            /// Gets the key for this group.
            /// </summary>
            public DateTime Key { get; private set; }

            public SavegameKeyGroup(DateTime key)
            {
                Key = key;
            }
        }

        #endregion
        
        #region Constants
        public static readonly string CartridgeFilenameKey = "filename";
        public static readonly string CartridgeIdKey = "cid";
        public static readonly string FileTokenKey = "fileToken";
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
			DependencyProperty.Register("CartridgeTag", typeof(CartridgeTag), typeof(CartridgeInfoViewModel), new PropertyMetadata(null));

		
		#endregion

		#region SelectedIndex


		public int SelectedIndex
		{
			get { return (int)GetValue(SelectedIndexProperty); }
			set { SetValue(SelectedIndexProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelectedIndex.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SelectedIndexProperty =
			DependencyProperty.Register("SelectedIndex", typeof(int), typeof(CartridgeInfoViewModel), new PropertyMetadata(0));


		#endregion

        #region StartCoordinates


        public WF.Player.Core.ZonePoint StartCoordinates
        {
            get { return (WF.Player.Core.ZonePoint)GetValue(StartCoordinatesProperty); }
            set { SetValue(StartCoordinatesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StartingCoordinate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StartCoordinatesProperty =
            DependencyProperty.Register("StartCoordinates", typeof(WF.Player.Core.ZonePoint), typeof(CartridgeInfoViewModel), new PropertyMetadata(null));


        #endregion

        #region IsStartPushpinVisible


        public bool IsStartPushpinVisible
        {
            get { return (bool)GetValue(IsStartPushpinVisibleProperty); }
            set { SetValue(IsStartPushpinVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsStartPushpinVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsStartPushpinVisibleProperty =
            DependencyProperty.Register("IsStartPushpinVisible", typeof(bool), typeof(CartridgeInfoViewModel), new PropertyMetadata(false));


        #endregion

        #region SavegameGroups


        public IEnumerable<SavegameKeyGroup> SavegameGroups
        {
            get { return (IEnumerable<SavegameKeyGroup>)GetValue(SavegameGroupsProperty); }
            set { SetValue(SavegameGroupsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SavegameGroups.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SavegameGroupsProperty =
            DependencyProperty.Register("SavegameGroups", typeof(IEnumerable<SavegameKeyGroup>), typeof(CartridgeInfoViewModel), new PropertyMetadata(null));


        
        #endregion

        #region AreSavegamesVisible


        public bool AreSavegamesVisibles
        {
            get { return (bool)GetValue(AreSavegamesVisiblesProperty); }
            set { SetValue(AreSavegamesVisiblesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AreSavegamesVisibles.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AreSavegamesVisiblesProperty =
            DependencyProperty.Register("AreSavegamesVisibles", typeof(bool), typeof(CartridgeInfoViewModel), new PropertyMetadata(false));


        
        #endregion

		#region Author


		public string Author
		{
			get { return (string)GetValue(AuthorProperty); }
			set { SetValue(AuthorProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Author.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AuthorProperty =
			DependencyProperty.Register("Author", typeof(string), typeof(CartridgeInfoViewModel), new PropertyMetadata(null));


		#endregion

        #region VectorToStartingCoordinate


        public WF.Player.Core.LocationVector VectorToStartingCoordinate
        {
            get { return (WF.Player.Core.LocationVector)GetValue(VectorToStartingCoordinateProperty); }
            set { SetValue(VectorToStartingCoordinateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VectorToStartingCoordinate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VectorToStartingCoordinateProperty =
            DependencyProperty.Register("VectorToStartingCoordinate", typeof(WF.Player.Core.LocationVector), typeof(CartridgeInfoViewModel), new PropertyMetadata(null));

        #endregion
        #endregion

        #region Commands

        #region StartNewGameCommand

        private ICommand _startNewGameCommand;

		/// <summary>
		/// Gets a command to start a new game.
		/// </summary>
		public ICommand StartNewGameCommand
		{
			get
			{
				return _startNewGameCommand ?? (_startNewGameCommand = new RelayCommand(StartNewGame));
			}
		}

		#endregion

        #region ResumeGameCommand
        private ICommand _resumeGameCommand;

        /// <summary>
        /// Gets a command to resume a saved game.
        /// </summary>
        public ICommand ResumeGameCommand
        {
            get
            {
                return _resumeGameCommand ?? (_resumeGameCommand = new RelayCommand<CartridgeSavegame>(ResumeGame));
            }
        }
        #endregion

        #region NavigateToStartCommand

        private ICommand _navigateToStartCommand;

        /// <summary>
        /// Gets a command to navigate to the starting location.
        /// </summary>
        public ICommand NavigateToStartCommand
        {
            get
            {
                return _navigateToStartCommand ?? (_navigateToStartCommand = new RelayCommand(NavigateToStart, CanNavigateToStartExecute));
            }
        }

        #endregion

		#endregion

		#region Members

		private IApplicationBar _appBarInfo;

		#endregion
		
		protected override void InitFromNavigation(NavigationInfo nav)
		{
			System.Windows.Navigation.NavigationContext navCtx = nav.NavigationContext;
            bool shouldCancel = false;

            // Shows a progress bar.
            IsProgressBarVisible = true;
            ProgressBarStatusText = null;

			// Parses the cartridge guid parameter and tries to get its tag.
			string filenameParam;
			string cidParam;
            string fileTokenParam;
			if (navCtx.QueryString.TryGetValue(CartridgeFilenameKey, out filenameParam) && 
				navCtx.QueryString.TryGetValue(CartridgeIdKey, out cidParam))
			{
				// Opens a cartridge from filename.
                CartridgeTag = Model.CartridgeStore.GetCartridgeTag(filenameParam, cidParam);
			}
            else if (navCtx.QueryString.TryGetValue(FileTokenKey, out fileTokenParam))
            {
                // Opens a cartridge from file association token.

                CartridgeTag = Model.CartridgeStore.GetCartridgeTagFromFileAssociation(fileTokenParam);
            }

            // Quits if the tag is null.
            if (CartridgeTag == null)
            {
                shouldCancel = true;
                MessageBox.Show("Sorry, Geowigo cannot open this cartridge.", "Error", MessageBoxButton.OK);
            }

            // Leave now if we need to cancel.
            if (shouldCancel)
            {
                App.Current.ViewModel.NavigationManager.NavigateBack();
                return;
            }

            // The progress bar is not needed anymore.
            IsProgressBarVisible = false;

            // Inits the data context.
            Cartridge = CartridgeTag.Cartridge;
            WherigoObject = CartridgeTag.Cartridge;

            // Refreshes content.
            RefreshAppBar();
            RefreshStaticContent();
            RefreshLocatedContent();
            RefreshSavegames();
		}

        #region Savegames Long List

        private void RefreshSavegames()
        {
            // Creates a new dictionary to hold the different keys.
            Dictionary<DateTime, SavegameKeyGroup> groups = new Dictionary<DateTime, SavegameKeyGroup>();

            // For each savegame in the tag, get the key and add it to the
            // related group.
            foreach (CartridgeSavegame savegame in CartridgeTag.Savegames.OrderByDescending(cs => cs.Timestamp))
            {
                // The key is the day during which the savegame was made.
                DateTime key = savegame.Timestamp.Date;
                
                // Gets or creates the group for this key.
                SavegameKeyGroup group;
                if (!groups.TryGetValue(key, out group))
                {
                    // Creates the group.
                    group = new SavegameKeyGroup(key);

                    // Adds it to the dictionary.
                    groups.Add(key, group);
                }

                // Adds the savegame to the group.
                group.Add(savegame);
            }

            // The savegame collection is ready for the long list!
            SavegameGroups = new List<SavegameKeyGroup>(groups.Values);

            // Refreshes the visibility.
            AreSavegamesVisibles = CartridgeTag.Savegames.Any();
        }

        #endregion

		#region Menu Actions

        private bool CanCartridgeRun(WF.Player.Core.Cartridge cart)
        {
            bool canCartridgeRun = true;

            // Checks if a good location is available.
            double? locationAccuracy = null;
            if (Model.Core.DeviceLocation != null)
            {
                locationAccuracy = Model.Core.DeviceLocation.HorizontalAccuracy;
            }
            if (Model.Core.DeviceLocationStatus != System.Device.Location.GeoPositionStatus.Ready || locationAccuracy == null)
            {
                // No location is available.
                canCartridgeRun = System.Windows.MessageBox.Show(
                    String.Format("This device's location could not be found because location services are not ready. This may lead the game {0} to not behave correctly. Check the device status page from the main menu.\n\nTap on OK to run the game anyway, or on Cancel to return to the main menu.", cart.Name),
                    "Location services are not ready",
                    MessageBoxButton.OKCancel
                    ) == System.Windows.MessageBoxResult.OK;
            }
            else if (locationAccuracy > PlayerViewModel.MaxGoodLocationAccuracy)
            {
                // No good accuracy.
                canCartridgeRun = System.Windows.MessageBox.Show(
                    String.Format("This device's location accuracy is poor right now ({1:0}m). This may lead the game {0} to not behave correctly. Check the device status page from the main menu.\n\nTap on OK to run the game anyway, or on Cancel to return to the main menu.", cart.Name, locationAccuracy),
                    "Poor location accuracy",
                    MessageBoxButton.OKCancel
                    ) == System.Windows.MessageBoxResult.OK;
            }

            return canCartridgeRun;
        }

		private void StartNewGame()
		{
            // Starts the cartridge!
            if (CanCartridgeRun(Cartridge))
            {
                // Starts a new game!
                App.Current.ViewModel.NavigationManager.NavigateToGameHome(Cartridge.Filename);
            }
		}

        private void ResumeGame(CartridgeSavegame savegame)
        {
            // Resumes the game!
            if (CanCartridgeRun(Cartridge))
            {
                App.Current.ViewModel.NavigationManager.NavigateToGameHome(Cartridge.Filename, savegame); 
            }
        }

        private void NavigateToStart()
        {
            // Creates a new Bing Maps task.
            BingMapsTask task = new BingMapsTask();
            task.Center = StartCoordinates.ToGeoCoordinate();
            task.SearchTerm = String.Format("{0}", StartCoordinates.ToString(WF.Player.Core.GeoCoordinateUnit.DecimalDegrees));
            task.ZoomLevel = 2;

            // Starts the task.
            task.Show();
        }

        private bool CanNavigateToStartExecute()
        {
            if (System.ComponentModel.DesignerProperties.IsInDesignTool)
            {
                return false;
            }
            
            return Cartridge != null && !Cartridge.IsPlayAnywhere;
        }

		#endregion

		#region Application Bar
		private void RefreshAppBar()
		{
			switch (SelectedIndex)
			{
				case 0:
					if (_appBarInfo == null)
					{
						MakeAppBarForInfo();
					}
					ApplicationBar = _appBarInfo;
					
					break;

				default:
					break;
			}
		}

		private void MakeAppBarForInfo()
		{
			_appBarInfo = new Microsoft.Phone.Shell.ApplicationBar();
			
			// Creates and adds the buttons.
			_appBarInfo.CreateAndAddButton("appbar.transport.play.rest.png", StartNewGameCommand, "start");
		}
		#endregion

        private void RefreshStaticContent()
        {
			// Author name and company.
			string fullAuthor = Cartridge.GetFullAuthor();
			if (!String.IsNullOrWhiteSpace(fullAuthor))
			{
				Author = "by " + fullAuthor;
			}
        }

        private void RefreshLocatedContent()
        {
            if (Cartridge == null)
            {
                return;
            }

            // Starting point.
            if (Cartridge.IsPlayAnywhere)
            {
                // Start pushpin -> hidden
                // Player marker -> visible
                // Map center -> player position

                // Sets the start location from device location, so that the map center reflects
                // the player position.
                if (Model.Core.DeviceLocation == null)
                {
                    // No location yet: display a default.
                    StartCoordinates = WF.Player.Core.ZonePoint.Zero;
                }
                else
                {
                    // Change the current coordinates if it is far enough from the last one.
                    if (StartCoordinates == null || Model.Core.DeviceLocation.GetDistanceTo(StartCoordinates.ToGeoCoordinate()) > 50)
                    {
                        StartCoordinates = Model.Core.DeviceLocation.ToZonePoint();
                    }
                }

                IsStartPushpinVisible = false;
                VectorToStartingCoordinate = null;
            }
            else
            {
                // Start pushpin -> visible
                // Player marker -> visible
                // Map center -> start location

                StartCoordinates = Cartridge.StartingLocation;
                IsStartPushpinVisible = true;
                VectorToStartingCoordinate = Model.Core.DeviceLocation == null ? null : Model.Core.DeviceLocation.ToZonePoint().GetVectorTo(StartCoordinates);
            }
        }

        protected override void OnModelChanging(WherigoModel oldValue, WherigoModel newValue)
        {
            if (oldValue != null)
            {
                newValue.Core.PropertyChanged -= Core_PropertyChanged;
            }

            if (newValue != null)
            {
                newValue.Core.PropertyChanged += Core_PropertyChanged;
            }
        }

        void Core_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DeviceLocation")
            {
                RefreshLocatedContent();
            }
        }
    }
}
