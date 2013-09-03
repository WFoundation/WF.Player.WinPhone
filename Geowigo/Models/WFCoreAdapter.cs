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

	public class WFCoreAdapter : Engine, INotifyPropertyChanged
	{
		#region Classes

		/// <summary>
		/// A class that processes the events from Wherigo Foundation's Core engine.
		/// </summary>
		/*private class WFCoreUIImpl : IUserInterface
		{
			private WFCoreAdapter _Parent;

			internal WFCoreUIImpl(WFCoreAdapter parent)
			{
				_Parent = parent;
			}

			#region IUserInterface
			public void LogMessage(int level, string message)
			{
				_Parent.LogDebug(String.Format("[LOG {0}]: {1}", level, message));

				// TODO: output logging (GWL)
			}

			public void MessageBox(string text, WF.Player.Core.Media mediaObj, string btn1Label, string btn2Label, WF.Player.Core.CallbackFunction wrapper)
			{
				// Raises the parent event.
				_Parent.RaiseMessageBoxRequested(new WherigoMessageBox(text, mediaObj, btn1Label, btn2Label, wrapper), true);	
			}

			public void GetInput(WF.Player.Core.Input inputObj)
			{
				// Raises the parent event.
				_Parent.RaiseInputRequested(inputObj, true);
			}

			public void MediaEvent(int type, WF.Player.Core.Media mediaObj)
			{
				// Debug log.
				_Parent.LogDebug(String.Format("MediaEvent; {0}, {1} ", type, mediaObj.MediaId));

				// The Groundspeak engine only should give 1 as a type.
				if (type != 1)
				{
					_Parent.LogDebug("Discarded media event had type {0}, != 1.");
					throw new NotImplementedException("Discarded media event had type {0}, != 1.");
				}

				// Raise the parent event.
				_Parent.RaisePlaySoundRequested(mediaObj, true);
			}

			public void ShowStatusText(string text)
			{
				throw new NotImplementedException();
			}

			public void ShowScreen(int screen, int idxObj)
			{				
				// Finds the right kind of event.
				WherigoScreenKind kind = WherigoScreenKind.Unknown;
				UIObject obj = null;
				if (screen == _Parent.MAINSCREEN)
				{
					kind = WherigoScreenKind.Main;
				}
				else if (screen == _Parent.LOCATIONSCREEN)
				{
					kind = WherigoScreenKind.Locations;
				}
				else if (screen == _Parent.ITEMSCREEN)
				{
					kind = WherigoScreenKind.Items;
				}
				else if (screen == _Parent.INVENTORYSCREEN)
				{
					kind = WherigoScreenKind.Inventory;
				}
				else if (screen == _Parent.TASKSCREEN)
				{
					kind = WherigoScreenKind.Tasks;
				}
				else if (screen == _Parent.DETAILSCREEN)
				{
					obj = _Parent.GetObject(idxObj) as UIObject;

					if (obj != null)
					{
						// Accepts the event as a proper Details event.
						kind = WherigoScreenKind.Details;
					}
				}

				// Raises the parent event.
				_Parent.RaiseScreenRequested(kind, obj, true);
			}

			public void NotifyOS(string command)
			{
				throw new NotImplementedException();
			}

			public void AttributeChanged(WF.Player.Core.Table obj, string type)
			{
				// Debug log
				_Parent.LogDebug(String.Format("AttributeChanged; {0}.{1} ", obj.GetNameOrId(), type));

				// Checks if an engine property has changed.
				if (obj is Task && (type == "Active" || type == "Visible"))
				{
					_Parent.RaisePropertyChanged("ActiveVisibleTasks");
				}
				else if (obj is Zone && (type == "Active" || type == "Visible"))
				{
					_Parent.RaisePropertyChanged("ActiveVisibleZones");
				}

				// Relay event.
				if (_Parent.AttributeChanged != null)
				{
					_Parent.AttributeChanged(this, new WherigoAttributeChangedEventArgs(obj, type));
				}
			}

			public void CartridgeChanged(string type)
			{
				// Debug log.
				_Parent.LogDebug(String.Format("CartridgeChanged; {0}.", type));
				
				if ("complete".Equals(type))
				{
					// Raises the parent event.
					_Parent.RaiseCartridgeCompleted(_Parent.Cartridge);
				}
				else if ("sync".Equals(type))
				{
					// TODO: Raise sync event? Cartridge saved?
				}
			}

			public void CommandChanged(WF.Player.Core.Command obj)
			{
				// Debug log.
				_Parent.LogDebug(String.Format("CommandChanged; {0}.", obj.GetNameOrId()));
				
				// Raises the parent event.
				_Parent.RaiseCommandChanged(obj);
			}

			public void InventoryChanged(WF.Player.Core.Thing obj, WF.Player.Core.Thing fromContainer, WF.Player.Core.Thing toContainer)
			{
				// Debug log.
				_Parent.LogDebug(String.Format("InventoryChanged; {0} went from {1} to {2}.", obj.GetNameOrId(), GetTableNameOrId(fromContainer), GetTableNameOrId(toContainer)));

				// Raises the parent event.
				_Parent.RaiseInventoryChanged(obj, fromContainer, toContainer);

				// Check for player inventory changes.
				if (_Parent.Player.Equals(toContainer) || _Parent.Player.Equals(fromContainer))
				{
					// The player visible inventory has changed.
					_Parent.RaisePropertyChanged("VisibleInventory");
				}

				// Check for visible objects changes.
				if (_Parent.IsZone(fromContainer) || _Parent.IsZone(toContainer))
				{
					// Make a guess.
					_Parent.RaisePropertyChanged("VisibleObjects");
				}
			}

			public void ZoneStateChanged(System.Collections.Generic.List<WF.Player.Core.Zone> zones)
			{
				// Debug log.
				_Parent.LogDebug(String.Format("ZoneStateChanged; {0} zones, {1} objects.", zones.Count, _Parent.VisibleObjects.Count));

				// The list of zones and objects has changed.
				_Parent.RaisePropertyChanged("ActiveVisibleZones");
				_Parent.RaisePropertyChanged("VisibleObjects");
			}

			public string GetDevice()
			{
				// Returns device info and manufacturer.
				return String.Format("Windows Phone {0}/{1}", Environment.OSVersion.Version.ToString(2), Microsoft.Phone.Info.DeviceStatus.DeviceManufacturer);
			}

			public string GetDeviceId()
			{
				// Returns device ID.
				object idHash;
				if (!Microsoft.Phone.Info.DeviceExtendedProperties.TryGetValue("DeviceUniqueId", out idHash))
				{
					return "Unknown";
				}

				return Convert.ToBase64String((byte[])idHash);
			}

			public string GetVersion()
			{
				// Returns player version.
				return "2.11-compatible";
			}

			public void Syncronize(WF.Player.Core.SyncronizeTick tick, object source)
			{
				// Debug log.
				_Parent.LogDebug("Sync tick.");
				
				// Runs the delegate in the ui thread.
				Deployment.Current.Dispatcher.BeginInvoke(new Action(() => tick(source)));
			}
			#endregion

			
		}*/

		#endregion

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
			// Creates and starts the location service.
			_GeoWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
			_GeoWatcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(GeoWatcher_StatusChanged);
			_GeoWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(GeoWatcher_PositionChanged);
			_GeoWatcher.Start();
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

		private void LogDebug(string message)
		{
			Debug.WriteLine("[DEBUG] " + message);
		}



		
	}
}
