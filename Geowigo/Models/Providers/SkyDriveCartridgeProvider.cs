using System;
using Microsoft.Live;
using System.Collections.Generic;
using System.Linq;
using System.IO.IsolatedStorage;
using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Threading;

namespace Geowigo.Models.Providers
{
	/// <summary>
	/// A provider that can download cartridges from a user's SkyDrive cloud storage.
	/// </summary>
	public class SkyDriveCartridgeProvider : ICartridgeProvider
	{
		#region Constants

		private static readonly string LiveConnectClientID = "000000004C10D95D";

		private static readonly string[] _Scopes = new string[] { "wl.basic", "wl.skydrive", "wl.offline_access" };

		private static readonly TimeSpan GetRequestTimeoutTimeSpan = TimeSpan.FromSeconds(20d);

		#endregion
		
		#region Nested Classes

		private class SkyDriveFile
		{
			public SkyDriveFile(string id, string name)
			{
				Id = id;
				Name = name;
			}
			public string Id { get; set; }

			public string Name { get; set; }
		}

		#endregion
		
		#region Members

		private bool _isLinked = false;
		private bool _autoLoginOnInitFail = false;
		private bool _isSyncing = false;

		private CartridgeProviderSyncEventArgs _syncEventArgs;

		private List<SkyDriveFile> _dlFiles = new List<SkyDriveFile>();

		private object _syncRoot = new object();

		private LiveAuthClient _authClient;
		private LiveConnectClient _liveClient;

		private Timer _requestTimeout;
		
		#endregion

		#region Properties
		public string ServiceName
		{
			get { return "SkyDrive"; }
		}

		public bool IsLinked
		{
			get
			{
				lock (_syncRoot)
				{
					return _isLinked;
				}
			}

			private set
			{
				lock (_syncRoot)
				{
					if (_isLinked != value)
					{
						_isLinked = value;
						RaisePropertyChanged("IsLinked");
					}
				}
			}
		}

		public string IsoStoreCartridgesPath
		{
			get;
			set;
		}

		public bool IsSyncing
		{
			get
			{
				lock (_syncRoot)
				{
					return _isSyncing;
				}
			}

			private set
			{
				lock (_syncRoot)
				{
					if (_isSyncing != value)
					{
						_isSyncing = value;
						RaisePropertyChanged("IsSyncing");
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets if this provider is allowed to perform
		/// background download (if they are supported).
		/// </summary>
		public bool IsBackgroundDownloadAllowed
		{
			get;
			set;
		}
		#endregion

		#region Events

		public event EventHandler<CartridgeProviderSyncEventArgs> SyncCompleted;

		public event EventHandler<CartridgeProviderSyncEventArgs> SyncProgress;

		public event EventHandler<CartridgeProviderSyncAbortEventArgs> SyncAborted;

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		#endregion

		public SkyDriveCartridgeProvider()
		{
			// Tries to link the provider but does not start the login
			// process if no active session has been found.
			BeginLink(false);
		}

		private void RaisePropertyChanged(string propName)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				if (PropertyChanged != null)
				{
					PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propName));
				}
			});
		}

		#region Timeout
		private void StartTimeoutTimer(TimeSpan timeSpan)
		{
			Timer timer;
			lock (_syncRoot)
			{
				// Creates the timer if it doesn't exist.
				if (_requestTimeout == null)
				{
					// Creates and starts the timer.
					_requestTimeout = new Timer(new TimerCallback(OnTimeoutTimerTick));
				}

				timer = _requestTimeout;
			}

			// The timer should fire once only.
			timer.Change((int)timeSpan.TotalMilliseconds, Timeout.Infinite);
		}

		private void CancelTimeoutTimer()
		{
			lock (_syncRoot)
			{
				// Bye bye timer.
				if (_requestTimeout != null)
				{
					_requestTimeout.Dispose();
					_requestTimeout = null;
				}
			}
		}

		private void OnTimeoutTimerTick(object target)
		{
			// Cancels the timer.
			CancelTimeoutTimer();

			// We are not syncing anymore.
			IsSyncing = false;

			// Raise the event.
			RaiseSyncAbort(true);
		} 
		#endregion

		#region LiveConnect Auth/Login

		public void BeginLink()
		{
			BeginLink(true);
		}

		private void BeginLink(bool autoLogin)
		{			
			// Returns if already linked.
			if (IsLinked)
			{
				return;
			}
			
			// Stores auto-login setting.
			lock (_syncRoot)
			{
				_autoLoginOnInitFail = autoLogin;
			}
			
			// Makes sure the auth client exists.
			if (_authClient == null)
			{
				_authClient = new LiveAuthClient(LiveConnectClientID);
				_authClient.InitializeCompleted += new EventHandler<LoginCompletedEventArgs>(OnAuthClientInitializeCompleted);
				_authClient.LoginCompleted += new EventHandler<LoginCompletedEventArgs>(OnAuthClientLoginCompleted);
			}

			// Starts initializing.
			try
			{
				_authClient.InitializeAsync(_Scopes);
			}
			catch (LiveAuthException ex)
			{
				// Ignores but dumps the exception.
				Geowigo.Utils.DebugUtils.DumpException(ex, dumpOnBugSenseToo: true);
			}
		}

		private void OnAuthClientInitializeCompleted(object sender, LoginCompletedEventArgs e)
		{
			if (e.Status == LiveConnectSessionStatus.Connected)
			{
				// We're online, get the client.
				MakeClientFromSession(e.Session);
			}
			else
			{
				// Checks if we need to auto login.
				bool shouldAutoLogin = false;
				lock (_syncRoot)
				{
					shouldAutoLogin = _autoLoginOnInitFail;
				}

				// If we need to auto-login, do it.
				if (shouldAutoLogin)
				{
					_authClient.LoginAsync(_Scopes);
				}
			}
		}

		private void MakeClientFromSession(LiveConnectSession session)
		{
			// Creates the client.
			_liveClient = new LiveConnectClient(session);

			// Adds event handlers.
			_liveClient.DownloadCompleted += new EventHandler<LiveDownloadCompletedEventArgs>(OnLiveClientDownloadCompleted);
			_liveClient.BackgroundDownloadCompleted += new EventHandler<LiveOperationCompletedEventArgs>(OnLiveClientBackgroundDownloadCompleted);
			_liveClient.GetCompleted += new EventHandler<LiveOperationCompletedEventArgs>(OnLiveClientGetCompleted);

			// Makes the client download even when on battery, or cellular data scheme.
			_liveClient.BackgroundTransferPreferences = BackgroundTransferPreferences.AllowCellularAndBattery;

			// Attaches downloads that have been running in the background
			// while the app was not active.
			_liveClient.AttachPendingTransfers();

			// Notify we're linked.
			IsLinked = true;
		}

		private void OnAuthClientLoginCompleted(object sender, LoginCompletedEventArgs e)
		{
			if (e.Status == LiveConnectSessionStatus.Connected)
			{
				// We're online, get the client.
				MakeClientFromSession(e.Session);

				// Notify we're linked.
				IsLinked = true;
			}
		}

		#endregion

		#region LiveConnect Sync

		public void BeginSync()
		{
			// Sanity checks.
			if (!IsLinked)
			{
				throw new InvalidOperationException("The SkyDrive provider is not linked.");
			}

			// Makes sure a pending sync is not in progress.
			if (IsSyncing)
			{
				return;
			}
			IsSyncing = true;

			// Creates the folder if it isn't created.
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				isf.CreateDirectory(IsoStoreCartridgesPath);
			}

			// Sync Step 1: Ask for the list of files from root folder.
			_liveClient.GetAsync("me/skydrive/files?filter=folders", "root");

			// Starts the timeout timer.
			StartTimeoutTimer(GetRequestTimeoutTimeSpan);
		}

		private void BeginDownloadCartridge(SkyDriveFile file)
		{
			// Adds the file id to the list of currently downloading files.
			lock (_syncRoot)
			{
				_dlFiles.Add(file);
			}
			
			// Starts downloading the cartridge to the isostore.
			string fileAttribute = file.Id + "/content";
			if (!IsBackgroundDownloadAllowed || Microsoft.Devices.Environment.DeviceType == Microsoft.Devices.DeviceType.Emulator)
			{
				// The emulator has no support for background download.
				// Peform a direct download instead.
				_liveClient.DownloadAsync(fileAttribute, GetDownloadUserState(file));
			}
			else
			{
				try
				{
					// Tries to perform a background download.
					_liveClient.BackgroundDownloadAsync(
						fileAttribute,
						new Uri(GetTempIsoStorePath(file.Name), UriKind.RelativeOrAbsolute),
						GetDownloadUserState(file)
						);
				}
				catch (Exception ex)
				{
					// Logs the exception.
					Utils.DebugUtils.DumpException(ex, "SkyDriveCartridgeProvider, background download attempt on actual device.", true);

					// Tries the direct download method.
					_liveClient.DownloadAsync(fileAttribute, GetDownloadUserState(file));
				}
			}
			
		}

		private void EndSync()
		{
			CartridgeProviderSyncEventArgs e;

			// Marks the sync as completed.
			lock (_syncRoot)
			{
				e = _syncEventArgs;
				_syncEventArgs = null;
			}
			IsSyncing = false;

			// Notifies that the sync is finished.
			if (e != null)
			{
				if (SyncCompleted != null)
				{
					SyncCompleted(this, e);
				}
			}
		}

		private string GetDownloadUserState(SkyDriveFile file)
		{
			return file.Id + "|" + file.Name;
		}

		private string GetIsoStorePath(string filename)
		{
			return String.Format("{0}/{1}", IsoStoreCartridgesPath, filename);
		}

		private string GetTempIsoStorePath(string filename)
		{
			return String.Format("/shared/transfers/_{0}", filename);
		}

		private void OnLiveClientBackgroundDownloadCompleted(object sender, LiveOperationCompletedEventArgs e)
		{
			// Sync Step 5. The file has already been downloaded to isostore.
			// Just move it to its right location.
			// (This only runs on the device.)

			int filesLeft;
			string dlFilename;
			string originalFilename;
			PreProcessDownload(e, out filesLeft, out dlFilename, out originalFilename);

			string filepath = GetIsoStorePath(originalFilename);
			if (e.Result != null)
			{
				// Moves the file.
				using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
				{
					// Makes sure the directory exists.
					isf.CreateDirectory(IsoStoreCartridgesPath);

					// Moves the downloaded file to the right place.
					try
					{
						isf.MoveFile(GetTempIsoStorePath(dlFilename), filepath);
					}
					catch (Exception ex)
					{
						// In case of exception here, do nothing.
						// An attempt to load the file will be done anyway.
						Geowigo.Utils.DebugUtils.DumpException(ex, dumpOnBugSenseToo: true);
					}
				}
			}

			PostProcessDownload(filepath, filesLeft);
		}

		private void OnLiveClientDownloadCompleted(object sender, LiveDownloadCompletedEventArgs e)
		{
			// Sync Step 5. The file has been downloaded as a memory stream.
			// Write it to its direct location.
			// (This runs on the emulator and on devices where the background
			// download method failed.)

			int filesLeft;
			string dlFilename;
			string originalFilename;
			PreProcessDownload(e, out filesLeft, out dlFilename, out originalFilename);

			string filepath = GetIsoStorePath(dlFilename);
			if (e.Result != null)
			{
				using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
				{
					// Makes sure the directory exists.
					isf.CreateDirectory(IsoStoreCartridgesPath);

					// Creates a file at the right place.
					using (IsolatedStorageFileStream fs = isf.OpenFile(filepath, FileMode.Create))
					{
						e.Result.CopyTo(fs);
					}
				}
			}

			PostProcessDownload(filepath, filesLeft);
		}

		private void OnLiveClientGetCompleted(object sender, LiveOperationCompletedEventArgs e)
		{
			// Cancels the timeout timer.
			CancelTimeoutTimer();
			
			// No result? Nothing to do.
			if (e.Result == null)
			{
				EndSync(); 
				return;
			}

			if ("root".Equals(e.UserState))
			{
				// Sync Step 2: We are getting results for the root folder.
				// We need to enumerate through all file entries and find the first
				// folder whose name is "Geowigo".
				// Then, we will ask for file enumerations of this folder.
				// If no folder is found, the sync is over.

				// Enumerates through all the file entries.
				List<object> data = (List<object>)e.Result["data"];
				foreach (IDictionary<string, object> content in data)
				{
					// Is it a folder?
					if ("folder".Equals(content["type"]))
					{
						// Is its name "Geowigo"?
						if ("Geowigo".Equals((string)content["name"], StringComparison.InvariantCultureIgnoreCase))
						{
							// Sync Step 3. Asks for the list of files in this folder.
							_liveClient.GetAsync((string)content["id"] + "/files", "geowigo");

							// Starts the timeout timer.
							StartTimeoutTimer(GetRequestTimeoutTimeSpan);

							// Nothing more to do.
							return;
						}
					}
				}

				// If we are here, it means that the Geowigo folder was not found.
				// The sync ends.
				EndSync();
				return;
			}
			else if ("geowigo".Equals(e.UserState))
			{
				// Sync Step 4: We are getting results for the Geowigo folder.
				// We need to enumerate through all files and download each GWC
				// file in the background.

				// Enumerates through all the file entries.
				List<SkyDriveFile> cartFiles = new List<SkyDriveFile>();
				List<object> data = (List<object>)e.Result["data"];
				foreach (IDictionary<string, object> content in data)
				{
					// Is it a cartridge file?
					string name = (string)content["name"];
					if ("file".Equals(content["type"]) && name.ToLower().EndsWith(".gwc"))
					{
						// Adds the file to the list.
						string id = (string)content["id"];
						lock (_syncRoot)
						{
							cartFiles.Add(new SkyDriveFile(id, name));
						}
					}
				}

				// Creates the list of files in the isostore that do not exist 
				// on SkyDrive anymore.
				List<string> isoStoreFiles;
				List<string> toRemoveFiles;
				using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
				{
					isoStoreFiles =
						isf
						.GetFileNames(GetIsoStorePath("*.gwc"))
						.Select(s => IsoStoreCartridgesPath + "/" + s)
						.ToList();

					toRemoveFiles =
						isoStoreFiles
						.Where(s => !cartFiles.Any(sd => sd.Name == System.IO.Path.GetFileName(s)))
						.ToList();
				}

				// Creates the list of cartridges that are on SkyDrive but
				// not in the isolated storage.
				List<SkyDriveFile> toDlFiles = cartFiles
					.Where(sd => !isoStoreFiles.Contains(GetIsoStorePath(sd.Name)))
					.ToList();

				// Bakes an event for when the sync will be over.
				lock (_syncRoot)
				{
					_syncEventArgs = new CartridgeProviderSyncEventArgs()
					{
						AddedFiles = toDlFiles
							.Select(sd => GetIsoStorePath(sd.Name))
							.ToList(),
						ToRemoveFiles = toRemoveFiles
					};
				}

				// Sends a progress event for removing the files marked
				// to be removed.
				if (toRemoveFiles.Count > 0)
				{
					RaiseSyncProgress(toRemoveFiles: toRemoveFiles);
				}

				// Starts downloading all new files, or terminate
				// the sync if no new file is to be downloaded.
				if (toDlFiles.Count > 0)
				{
					toDlFiles.ForEach(sd => BeginDownloadCartridge(sd));
				}
				else
				{
					EndSync();
				}
			}
		}

		private void ParseDownloadEventArgs(AsyncCompletedEventArgs e, out string fileId, out string dlFilename, out string originalFilename)
		{
			string[] ustate = (e.UserState ?? "|").ToString().Split(new char[] { '|' });
			fileId = ustate[0];
			originalFilename = ustate[1];
			dlFilename = originalFilename;

			// Gets the downloaded filename from the event args if they support it.
			if (e is LiveOperationCompletedEventArgs)
			{
				object rawDlLoc;
				var result = ((LiveOperationCompletedEventArgs)e).Result;
				if (result != null && result.TryGetValue("downloadLocation", out rawDlLoc))
				{
					// The download location given by these event args is corrupted by
					// inappropriately inserted escape characters.
					// So let's just hack our way through it and figure out the actual
					// filename of the file that was saved in the isostore.
					dlFilename = ((string)rawDlLoc)
						.Split(new string[] { "shared\transfers_" }, StringSplitOptions.RemoveEmptyEntries)
						.LastOrDefault();
				}
			}
		}

		private void PostProcessDownload(string filepath, int filesLeft)
		{
			// Raise information about this progress.
			RaiseSyncProgress(addedFiles: new string[] { filepath });

			// If no more files are pending download, the sync is over!
			if (filesLeft == 0)
			{
				EndSync();
			}
		}

		private void PreProcessDownload(AsyncCompletedEventArgs e, out int filesLeft, out string dlFilename, out string originalFilename)
		{
			// Removes the file from the queue of files to download.
			lock (_syncRoot)
			{
				// Parses the user state.
				string fileId;
				ParseDownloadEventArgs(e, out fileId, out dlFilename, out originalFilename);

				// Removes the file if it is registered.
				SkyDriveFile file = _dlFiles.SingleOrDefault(sd => sd.Id.Equals(fileId));
				if (file != null)
				{
					_dlFiles.Remove(file);
				}

				// Gets how many files are pending completion.
				filesLeft = _dlFiles.Count;
			}
		}

		private void RaiseSyncProgress(IEnumerable<string> addedFiles = null, IEnumerable<string> toRemoveFiles = null)
		{
			// Raises an event for this file path.
			if (SyncProgress != null)
			{
				SyncProgress(this, new CartridgeProviderSyncEventArgs()
				{
					AddedFiles = addedFiles ?? new string[] {},
					ToRemoveFiles = toRemoveFiles ?? new string[] { }
				});
			}
		}

		private void RaiseSyncAbort(bool hasTimeout)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				if (SyncAborted != null)
				{
					SyncAborted(this, new CartridgeProviderSyncAbortEventArgs()
					{
						HasTimedOut = hasTimeout
					});
				}
			});
		}

		#endregion

	}
}
