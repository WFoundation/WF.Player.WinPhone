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
using System.ComponentModel;
using System.Diagnostics;
using System.Device.Location;
using WF.Player.Core.Engines;
using System.Collections.Generic;
using System.Linq;

namespace Geowigo.Models
{
	#region Event Classes

	/// <summary>
	/// Event arguments with regards to the player object.
	/// </summary>
	public class PlayerLocationChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Gets the current location of the player.
		/// </summary>
		public GeoCoordinate Location { get; private set; }

		/// <summary>
		/// Gets the player character.
		/// </summary>
		public Character Player { get; private set; }

		internal PlayerLocationChangedEventArgs(Character player, GeoCoordinate gc)
		{
			Location = gc;
			Player = player;
		}
	}

	#endregion

	public class WFCoreAdapter : Engine
	{
		#region Events

		/// <summary>
		/// raised when the player location has changed.
		/// </summary>
		public event EventHandler<PlayerLocationChangedEventArgs> PlayerLocationChanged;

		#endregion

		#region Fields

		private GeoCoordinateWatcher _GeoWatcher;
		private GeoPosition<GeoCoordinate> _LastKnownPosition;
		private object _SyncRoot = new Object();

		#endregion

		#region Properties

		#region ActiveVisibleThings

		public List<Thing> ActiveVisibleThings { get; private set; }

        public GeoCoordinate DeviceLocation { get; private set; }

		#endregion

		#endregion

		#region Constructors

		public WFCoreAdapter() : base(new WinPhonePlatformHelper())
		{
			// Creates and starts the location service.
			_GeoWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
			_GeoWatcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(GeoWatcher_StatusChanged);
			_GeoWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(GeoWatcher_PositionChanged);
			_GeoWatcher.Start();

			// Deploys handlers.
			RegisterCoreEventHandlers();
		}

		#endregion

		/// <summary>
		/// Starts to play a Wherigo cartridge game.
		/// </summary>
		/// <param name="filename">Filename of the cartridge in the isolated storage.</param>
		public Cartridge InitAndStartCartridge(string filename)
		{
			// Boot Time: inits the cartridge and process position.
			Cartridge cart = new Cartridge(filename);

			using (IsolatedStorageFileStream fs = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(cart.Filename, System.IO.FileMode.Open, System.IO.FileAccess.Read))
			{
				Init(fs, cart); 
			}

			ProcessPosition(_LastKnownPosition);

			// Run Time: the game starts.

			Start();

			return cart;
		}

        /// <summary>
        /// Starts to play a Wherigo cartridge game.
        /// </summary>
        /// <param name="filename">Filename of the cartridge in the isolated storage.</param>
        /// <param name="gwsFilename">Filename of the savegame to restore.</param>
        public Cartridge InitAndRestoreCartridge(string filename, string gwsFilename)
        {
            // Boot Time: inits the cartridge and process position.
            Cartridge cart = new Cartridge(filename);

            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream fs = isf.OpenFile(cart.Filename, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    Init(fs, cart);
                } 
            
                ProcessPosition(_LastKnownPosition);

                // Run Time: the game starts.

                using (IsolatedStorageFileStream fs = isf.OpenFile(gwsFilename, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    Restore(fs);
                } 
            }

            return cart;
        }

        //public void InitAndStartCartridgeAsync(string filename, Action<Cartridge> callback)
        //{
        //    BackgroundWorker bw = new BackgroundWorker();
        //    bw.DoWork += (o, e) =>
        //    {
        //        callback(InitAndStartCartridge(filename));
        //    };
        //    bw.RunWorkerAsync();
        //}

        /// <summary>
        /// Saves the game to a CartridgeSavegame object.
        /// </summary>
        /// <param name="cs">The CartridgeSavegame representing the savegame.</param>
        public void Save(CartridgeSavegame cs)
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (System.IO.Stream fs = cs.CreateOrReplace(isf))
                {
                    Save(fs);
                }
            }
        }

		#region Location Service Events Handlers
		
		private void GeoWatcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
		{
			ProcessPosition(e.Position);
		}

		private void GeoWatcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
		{			
			// Debug log.
			LogDebug(String.Format("Location service status changed to {0}.", e.Status.ToString()));
			
			switch (e.Status)
			{
				case GeoPositionStatus.Disabled:
					break;

				case GeoPositionStatus.Initializing:
					break;

				case GeoPositionStatus.NoData:
					break;

				case GeoPositionStatus.Ready:
					// Refresh the position in the engine.
					ProcessPosition(_GeoWatcher.Position);
					break;

				default:
					throw new InvalidProgramException("Unexpected status of the Location Service.");
			}
		}

		private void ProcessPosition(GeoPosition<GeoCoordinate> position)
		{
			// Stores the position.
			_LastKnownPosition = position ?? _LastKnownPosition;

			// No refresh if no position or if no game is running.
			// Force refresh if the game is booting.
			if (!IsReady || position == null || position.Location == null)
			{
				return;
			}

			GeoCoordinate gc = position.Location;

			// Updates the position if it is known.
			if (!gc.IsUnknown)
			{
				this.RefreshLocation(gc.Latitude, gc.Longitude, gc.Altitude, gc.HorizontalAccuracy);
				this.RefreshHeading(gc.Course);

                this.DeviceLocation = gc;

                RaisePropertyChanged("DeviceLocation");

				RaisePlayerLocationChanged(new GeoCoordinate(Latitude, Longitude, Altitude, Accuracy, 0, 0, Heading));
			}
		}

		#endregion

		#region Event Raisers

		private void RaisePlayerLocationChanged(GeoCoordinate gc)
		{
			if (PlayerLocationChanged != null)
			{
				PlayerLocationChanged(this, new PlayerLocationChangedEventArgs(Player, gc));
			}
		}

		#endregion

		#region WF Core Event Handlers

		private void RegisterCoreEventHandlers()
		{
			this.LogMessageRequested += new EventHandler<LogMessageEventArgs>(WFCoreAdapter_LogMessageRequested);
			this.PropertyChanged += new PropertyChangedEventHandler(WFCoreAdapter_PropertyChanged);
		}

		private void WFCoreAdapter_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ActiveVisibleZones" || e.PropertyName == "VisibleObjects")
			{
				ActiveVisibleThings = new List<Thing>();
				ActiveVisibleThings.AddRange(ActiveVisibleZones.OfType<Thing>());
				ActiveVisibleThings.AddRange(VisibleObjects);

				RaisePropertyChanged("ActiveVisibleThings");
			}
		}

		private void WFCoreAdapter_LogMessageRequested(object sender, LogMessageEventArgs e)
		{
			LogDebug(String.Format("[{0}]: {1}", e.Level, e.Message));
		}

		#endregion

		private void LogDebug(string message)
		{
			Debug.WriteLine("[DEBUG] " + message);
		}
    }
}
