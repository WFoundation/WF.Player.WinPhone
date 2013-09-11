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

		#region Properties

		#endregion

		#region Fields

		private GeoCoordinateWatcher _GeoWatcher;
		private GeoPosition<GeoCoordinate> _LastKnownPosition;
		private bool _IsPlaying = false;
		private bool _IsBooting = false;
		private object _SyncRoot = new Object();

		#endregion

		#region Constructors

		public WFCoreAdapter() : base()
		{
			// TODO: Base properties (device and such).
			
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
			lock (_SyncRoot)
			{
				_IsPlaying = false;
				_IsBooting = true;
			}

			Cartridge cart = new Cartridge(filename);

			using (IsolatedStorageFileStream fs = IsolatedStorageFile.GetUserStoreForApplication().OpenFile(cart.Filename, System.IO.FileMode.Open, System.IO.FileAccess.Read))
			{
				Init(fs, cart);
			}

			ProcessPosition(_LastKnownPosition);


			// Run Time: the game starts.
			lock (_SyncRoot)
			{
				_IsBooting = false;
				_IsPlaying = true;
			}
						
			Start();

			return cart;
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
			
			// Checks if the game is running.
			bool isRunning = false;
			bool isBooting = false;
			lock (_SyncRoot)
			{
				isRunning = _IsPlaying;
				isBooting = _IsBooting;
			}

			// No refresh if no position or if no game is running.
			// Force refresh if the game is booting.
			if ((!isBooting && !isRunning) || position == null || position.Location == null)
			{
				return;
			}

			GeoCoordinate gc = position.Location;

			// Updates the position if it is known.
			if (!gc.IsUnknown)
			{
				this.RefreshLocation(gc.Latitude, gc.Longitude, gc.Altitude, gc.HorizontalAccuracy);
				this.RefreshHeading(gc.Course);

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
		}

		void WFCoreAdapter_LogMessageRequested(object sender, LogMessageEventArgs e)
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
