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
using System.IO.IsolatedStorage;
using System.ComponentModel;
using System.Diagnostics;
using System.Device.Location;
using WF.Player.Core.Engines;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Devices.Sensors;

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
		/// Gets the current heading of the player.
		/// </summary>
		public double? Heading { get; private set; }

		/// <summary>
		/// Gets the player character.
		/// </summary>
		public Character Player { get; private set; }

		internal PlayerLocationChangedEventArgs(Character player, GeoCoordinate gc, double? heading)
		{
			Location = gc;
			Player = player;
			Heading = heading;
		}
	}

	#endregion

	public class WFCoreAdapter : Engine
	{
		#region Events

		/// <summary>
		/// Raised when the player location has changed.
		/// </summary>
		public event EventHandler<PlayerLocationChangedEventArgs> PlayerLocationChanged;

		/// <summary>
		/// Raised when the calibration of the compass is requested.
		/// </summary>
		public event EventHandler CompassCalibrationRequested;

		#endregion

		#region Members

		private GeoCoordinateWatcher _GeoWatcher;
		private GeoCoordinate _LastKnownLocation;
		private bool _HasLastKnownLocationChanged;

		private Compass _Compass;
		private bool _IsCompassEnabled;
		private double? _LastKnownHeading;
		private double? _LastKnownHeadingAccuracy;
		private bool _HasLastKnownHeadingChanged;

		private object _SyncRoot = new Object();

		#endregion

		#region Properties

		public List<Thing> ActiveVisibleThings { get; private set; }

		public GeoCoordinate DeviceLocation 
		{
			get
			{
				lock (_SyncRoot)
				{
					return _LastKnownLocation != null && !_LastKnownLocation.IsUnknown ? _LastKnownLocation : null;
				}
			}

			private set
			{
				bool hasChanged = false;
				lock (_SyncRoot)
				{
					if (_LastKnownLocation != value)
					{
						_LastKnownLocation = value;
						_HasLastKnownLocationChanged = true;
						hasChanged = true;
					}
				}

				if (hasChanged)
				{
					RaisePropertyChanged("DeviceLocation");

					ApplySensorData();
				}
			}
		}

		public double? DeviceHeading
		{
			get
			{
				lock (_SyncRoot)
				{
					return _LastKnownHeading;
				}
			}

			private set
			{
				bool hasChanged = false;
				lock (_SyncRoot)
				{
					if (_LastKnownHeading != value)
					{
						_LastKnownHeading = value;
						hasChanged = true;
						_HasLastKnownHeadingChanged = true;
					}
				}

				if (hasChanged)
				{
					RaisePropertyChanged("DeviceHeading");

					ApplySensorData();
				}
			}
		}

		public double? DeviceHeadingAccuracy
		{
			get
			{
				lock (_SyncRoot)
				{
					return _LastKnownHeadingAccuracy;
				}
			}

			private set
			{
				bool hasChanged = false;
				lock (_SyncRoot)
				{
					if (_LastKnownHeadingAccuracy != value)
					{
						_LastKnownHeadingAccuracy = value;
						hasChanged = true;
					}
				}

				if (hasChanged)
				{
					RaisePropertyChanged("DeviceHeadingAccuracy");
				}
			}
		}

		#endregion

		#region Constructors

		public WFCoreAdapter() : base(new WinPhonePlatformHelper())
		{
			// Creates and starts the location service.
			_GeoWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
			_GeoWatcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(GeoWatcher_StatusChanged);
			_GeoWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(GeoWatcher_PositionChanged);
			_GeoWatcher.Start();

			// Creates and starts the compass service.
			if (Compass.IsSupported)
			{
				_IsCompassEnabled = true;
				_Compass = new Compass();
				_Compass.TimeBetweenUpdates = TimeSpan.FromMilliseconds(250);
				_Compass.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<CompassReading>>(Compass_CurrentValueChanged);
				_Compass.Calibrate += new EventHandler<CalibrationEventArgs>(OnCompassCalibrate);
				//_Compass.Start();
			}

			// Deploys handlers.
			RegisterCoreEventHandlers();
		}

		#endregion

		#region Engine
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

			ApplySensorData();

			// Run Time: the game starts.

			Start();

			// TEMP DEBUG
			ApplySensorData();

			return cart;
		}

		/// <summary>
		/// Resumes playing a Wherigo cartridge saved game.
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

				ApplySensorData();

				// Run Time: the game starts.

				using (IsolatedStorageFileStream fs = isf.OpenFile(gwsFilename, System.IO.FileMode.Open, System.IO.FileAccess.Read))
				{
					Restore(fs);
				}
			}

			return cart;
		}

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
		#endregion

		private void ApplySensorData()
		{
			if (!IsReady)
			{
				return;
			}

			// Applies heading if found.
			bool shouldRefreshHeading = false;
			double? deviceHeading;
			lock (_SyncRoot)
			{
				shouldRefreshHeading = 
					_LastKnownHeading.HasValue 
					&& _HasLastKnownHeadingChanged;

				deviceHeading = _LastKnownHeading;
			}
			if (shouldRefreshHeading)
			{
				this.RefreshHeading(deviceHeading.Value);
			}
			lock (_SyncRoot)
			{
				// Marks heading as refreshed.
				_HasLastKnownHeadingChanged = false;
			}

			// Applies location if found.
			bool shouldRefreshLoc = false;
			GeoCoordinate deviceLoc;
			lock (_SyncRoot)
			{
				shouldRefreshLoc = 
					_LastKnownLocation != null 
					&& _HasLastKnownLocationChanged;
				deviceLoc = _LastKnownLocation;
			}
			if (shouldRefreshLoc)
			{
				this.RefreshLocation(deviceLoc.Latitude, deviceLoc.Longitude, deviceLoc.Altitude, deviceLoc.HorizontalAccuracy);
			}
			lock (_SyncRoot)
			{
				// Marks location as refreshed.
				_HasLastKnownLocationChanged = false;
			}

			// Sends a new event if something changed.
			if (shouldRefreshHeading || shouldRefreshLoc)
			{
				RaisePlayerLocationChanged(DeviceLocation, DeviceHeading);
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
			if (position == null || position.Location == null || position.Location.IsUnknown)
			{
				return;
			}

			// Stores the new valid position.
			DeviceLocation = position.Location;

			// Uses the location's course if the compass is not enabled.
			if (!_IsCompassEnabled)
			{
				DeviceHeading = position.Location.Course;
			}
		}

		#endregion

		#region Compass Service Event Handlers

		private void Compass_CurrentValueChanged(object sender, SensorReadingEventArgs<CompassReading> e)
		{
			if (((Compass)sender).IsDataValid)
			{
				ProcessCompass(e.SensorReading); 
			}
		}

		private void OnCompassCalibrate(object sender, CalibrationEventArgs e)
		{
			// Relay the event.
			Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				if (CompassCalibrationRequested != null)
				{
					CompassCalibrationRequested(this, EventArgs.Empty);
				}
			}));
		}

		private void ProcessCompass(CompassReading compassReading)
		{
			// Ignores the value if it's not a number or is not valid.
			if (Double.IsNaN(compassReading.TrueHeading))
			{
				return;
			}
			
			// Stores the new valid heading.
			DeviceHeading = compassReading.TrueHeading;
			DeviceHeadingAccuracy = compassReading.HeadingAccuracy;
		}

		#endregion

		#region Event Raisers

		private void RaisePlayerLocationChanged(GeoCoordinate gc, double? heading)
		{
			Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				if (PlayerLocationChanged != null)
				{
					PlayerLocationChanged(this, new PlayerLocationChangedEventArgs(Player, gc, heading));
				}
			}));
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
			else if (_Compass != null && e.PropertyName == "GameState")
			{
				if (GameState == EngineGameState.Playing)
				{
					_Compass.Start();
				}
				else
				{
					_Compass.Stop();
				}
			}
		}

		private void WFCoreAdapter_LogMessageRequested(object sender, LogMessageEventArgs e)
		{
			LogDebug(String.Format("[{0}]: {1}", e.Level, e.Message));
		}

		#endregion

		private void LogDebug(string message)
		{
			Debug.WriteLine("WFCoreAdapter: " + message);
		}
    }
}
