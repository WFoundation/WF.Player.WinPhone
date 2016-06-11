using System;
using Microsoft.Live;
using System.Collections.Generic;
using System.Linq;
using System.IO.IsolatedStorage;
using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Threading;
using Geowigo.Utils;
using System.Threading.Tasks;

namespace Geowigo.Models.Providers
{
	/// <summary>
	/// A provider that can download cartridges from a user's OneDrive cloud storage.
	/// </summary>
	public class OneDriveCartridgeProvider : ICartridgeProvider
	{
		#region Constants

        private static readonly string[] _Scopes = new string[] { "wl.skydrive", "wl.skydrive_update", "onedrive.readwrite", "wl.offline_access", "wl.signin" };

		private static readonly TimeSpan GetRequestTimeoutTimeSpan = TimeSpan.FromSeconds(20d);

		private static readonly string UploadFolderName = "Uploads";
        private static readonly string RootFolderName = "Geowigo";

		#endregion
		
		#region Nested Classes

		private class OneDriveFile
		{
			public OneDriveFile(string id, string name, string dlDirectory)
			{
				Id = id;
				Name = name;
				DownloadDirectory = dlDirectory;
			}
			public string Id { get; set; }

			public string Name { get; set; }

			public string DownloadDirectory { get; set; }

            public override string ToString()
            {
                return String.Format("[{0}, {1}, {2}]", Id, Name, DownloadDirectory);
            }
		}

		#endregion
		
		#region Members

		private bool _isLinked = false;
		private bool _autoLoginOnInitFail = false;
		private bool _isSyncing = false;

		private CartridgeProviderSyncEventArgs _syncEventArgs;

		private List<OneDriveFile> _dlFiles = new List<OneDriveFile>();

		private List<string> _ulFiles;

		private object _syncRoot = new object();

		private LiveAuthClient _authClient;
		private LiveConnectClient _liveClient;

		private IsolatedStorageFileStream _currentUlFileStream;

        private TaskScheduler _uiThreadTaskScheduler;

        private string _rootFolderId;
        private string _uploadsFolderId;

        private List<CancellationTokenSource> _activeCancellationTokenSources = new List<CancellationTokenSource>();

		#endregion

		#region Properties
		public string ServiceName
		{
			get { return "OneDrive"; }
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
                        Log("IsLinked = " + value);
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

		public string IsoStoreCartridgeContentPath
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
                        Log("IsSyncing = " + value);
						RaisePropertyChanged("IsSyncing");
					}
				}
			}
		}

        /// <summary>
        /// Gets if this provider is allowed to perform background downloads (if they are supported).
        /// </summary>
        public bool IsBackgroundDownloadAllowed
        {
            get
            {
                return false; // Microsoft.Devices.Environment.DeviceType != Microsoft.Devices.DeviceType.Emulator
            }
        }

        /// <summary>
        /// Gets how many cartridges this provider provides.
        /// </summary>
        public int CartridgeCount
        {
            get
            {
                try
                {
                    using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!isf.DirectoryExists(IsoStoreCartridgesPath))
                        {
                            return 0;
                        }
                        
                        string[] fileNames = isf.GetFileNames(System.IO.Path.Combine(IsoStoreCartridgesPath, "*.gwc"));

                        return fileNames.Length;
                    }
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets the name of the owner of the cartridge source.
        /// </summary>
        public string OwnerName { get; private set; }

		#endregion

		#region Events

		public event EventHandler<CartridgeProviderSyncEventArgs> SyncCompleted;

		public event EventHandler<CartridgeProviderSyncEventArgs> SyncProgress;

		public event EventHandler<CartridgeProviderSyncAbortEventArgs> SyncAborted;

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		#endregion

		public OneDriveCartridgeProvider()
		{
            _uiThreadTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            
            // Opens a new log session.
            DebugUtils.NewLogSession("OneDrive");
            
            // Tries to link the provider but does not start the login
			// process if no active session has been found.
			BeginLink(false);
		}

        private void Log(string message)
        {
            // Appends the name of the calling method and logs the line.
            System.Diagnostics.StackFrame frame = new System.Diagnostics.StackFrame(1);
            var method = frame.GetMethod();
            DebugUtils.Log("OneDrive", method.Name + "(): " + message);
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

        #region Task Operations

        private bool CheckTaskCompleted(System.Threading.Tasks.Task task, string errorLogMessagePrefix = "")
        {
            // The task completed.
            if (task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
            {
                return true;
            }

            // The task failed. Log a message.
            string log = errorLogMessagePrefix;
            if (task.Exception != null)
            {
                // Adds an aggregation of inner exception messages to the log.
                log += " " + task.Exception.Message + "(";
                foreach (Exception e in task.Exception.Flatten().InnerExceptions)
                {
                    log += e.Message + " ; ";
                }
                log += ")";

                // Dumps the aggregate exception.
                DebugUtils.DumpException(task.Exception, errorLogMessagePrefix, false);
            }
            Log(log);

            return false;
        }

        private CancellationToken CreateTimeoutCancellationToken(TimeSpan delay)
        {
            CancellationTokenSource cts = new CancellationTokenSource(delay);

            _activeCancellationTokenSources.Add(cts);

            return cts.Token;
        }

        private void StartTaskAndContinueWith<T>(Func<System.Threading.Tasks.Task<T>> mainTaskFunc,  Action<System.Threading.Tasks.Task<T>> continuationAction) 
        {
            Task.Factory.StartNew(() =>
            {
                mainTaskFunc().ContinueWith(continuationAction);
            },
            CancellationToken.None, TaskCreationOptions.None, _uiThreadTaskScheduler);
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
                Log("Already linked, ignored.");
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
                string clientId = null;
                try
                {
                    clientId = (string)App.Current.Resources["LiveConnectClientID"];
                }
                catch (Exception ex)
                {
                    Log("Unable to fetch LiveConnectClientID from App resources.");
                    throw new InvalidOperationException("Unable to fetch LiveConnectClientID from App resources.", ex);
                }

                _authClient = new LiveAuthClient(clientId);
			}
            
			// Starts initializing.
			try
			{
                Log("Starts InitializeAsync");
                StartTaskAndContinueWith(() => _authClient.InitializeAsync(_Scopes), OnAuthClientInitializeCompleted);
			}
			catch (LiveAuthException ex)
			{
				// Ignores but dumps the exception.
                Log("LiveAuthException: " + ex.Message);
				Geowigo.Utils.DebugUtils.DumpException(ex, dumpOnBugSenseToo: false);
			}
		}

		private void OnAuthClientInitializeCompleted(System.Threading.Tasks.Task<LiveLoginResult> task)
		{
            if (!CheckTaskCompleted(task, "Not connected. Failed to initialize client."))
            {
                return;
            }

            LiveLoginResult result = task.Result;

            if (result.Status == LiveConnectSessionStatus.Connected)
            {
                Log("Connected. Moving on with client.");

                // We're online, get the client.
                MakeClientFromSession(result.Session);
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
                    Log("Not connected. Starts LoginAsync (auto-login)");
                    StartTaskAndContinueWith(() => _authClient.LoginAsync(_Scopes), OnAuthClientLoginCompleted);
                }
                else
                {
                    Log("Not connected. Auto-login not requested.");
                }
            }
		}

		private void MakeClientFromSession(LiveConnectSession session)
		{
			// Creates the client.
			_liveClient = new LiveConnectClient(session);

			// Disables background transfer.
			_liveClient.BackgroundTransferPreferences = BackgroundTransferPreferences.None;

            Log("Client init'd.");

			// Notify we're linked.
			IsLinked = true;
		}

		private void OnAuthClientLoginCompleted(System.Threading.Tasks.Task<LiveLoginResult> task)
		{
            if (!CheckTaskCompleted(task, "Not connected. Failed to login client."))
            {
                return;
            }

            LiveLoginResult result = task.Result;

            Log("Login done. Status = " + result.Status);

            if (result.Status == LiveConnectSessionStatus.Connected)
            {
                // We're online, get the client.
                MakeClientFromSession(result.Session);

                // Notify we're linked.
                IsLinked = true;
            }
		}

		#endregion

        #region LiveConnect Unlink/Logout

        public void Unlink()
        {
            if (!IsLinked)
            {
                Log("The OneDrive provider is not linked.");
                throw new InvalidOperationException("The OneDrive provider is not linked.");
            }

            _authClient.Logout();

            IsSyncing = false;
            IsLinked = false;

            _authClient = null;

            if (_liveClient != null)
            {
                _liveClient = null;
            }
        }

        #endregion

		#region LiveConnect Sync

		public void BeginSync()
		{
			// Sanity checks.
			if (!IsLinked)
			{
                Log("The OneDrive provider is not linked.");
                throw new InvalidOperationException("The OneDrive provider is not linked.");
			}

			// Makes sure a pending sync is not in progress.
			if (IsSyncing)
			{
                Log("IsSyncing, ignored.");
                return;
			}
			IsSyncing = true;

			// Creates the folder if it isn't created.
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				isf.CreateDirectory(IsoStoreCartridgesPath);
				isf.CreateDirectory(IsoStoreCartridgeContentPath);
			}

			// Sync Step 1: Ask for the list of files from app folder.

            Log("Starts listing files in Geowigo");
            StartTaskAndContinueWith(
                () => GetRootFolderChildrenAsync(RootFolderName),
                OnLiveClientGetRootChildrenCompleted);
		}

        private async Task<LiveOperationResult> GetRootFolderChildrenAsync(string folderName)
        {
            // Creates or gets the folder.
            if (_rootFolderId == null)
            {
                _rootFolderId = await CreateDirectoryAsync(folderName, "me/skydrive");
            }
            if (_rootFolderId == null)
            {
                string message = "Cannot create or retrieve folder " + folderName + " on OneDrive.";
                Log(message);
                throw new InvalidOperationException(message);
            }

            // Gets the files in the folder.
            return await _liveClient.GetAsync(_rootFolderId + "/files", CreateTimeoutCancellationToken(GetRequestTimeoutTimeSpan));
        }

        public async Task<string> CreateDirectoryAsync(string folderName, string parentFolder)
        {
            string folderId = null;

            // Retrieves all the directories.
            var queryFolder = parentFolder + "/files?filter=folders,albums";
            var opResult = await _liveClient.GetAsync(queryFolder, CreateTimeoutCancellationToken(GetRequestTimeoutTimeSpan));
            dynamic result = opResult.Result;

            foreach (dynamic folder in result.data)
            {
                // Checks if current folder has the passed name.
                if (folder.name.ToLowerInvariant() == folderName.ToLowerInvariant())
                {
                    folderId = folder.id;
                    break;
                }
            }

            if (folderId == null)
            {
                // Directory hasn't been found, so creates it using the PostAsync method.
                var folderData = new Dictionary<string, object>();
                folderData.Add("name", folderName);
                opResult = await _liveClient.PostAsync(parentFolder, folderData);
                result = opResult.Result;

                // Retrieves the id of the created folder.
                folderId = result.id;
            }

            return folderId;
        }

		private void BeginDownloadFile(OneDriveFile file)
		{
            // Sync Step 3: We're scheduling some files to be downloaded.
            
            // Adds the file id to the list of currently downloading files.
			lock (_syncRoot)
			{
				_dlFiles.Add(file);
			}

			// Starts downloading the cartridge to the isostore.
            bool shouldDirectDownload = !IsBackgroundDownloadAllowed;
            Log(file.ToString() + ", directDownload = " + shouldDirectDownload);
			string fileAttribute = file.Id + "/content";
            if (shouldDirectDownload)
			{
				// Direct download.
                Log("Starts DownloadAsync");
                StartTaskAndContinueWith(
                    () => _liveClient.DownloadAsync(fileAttribute), 
                    t => OnLiveClientDownloadCompleted(t, file));
			}
			else
			{
				try
				{
					// Tries to perform a background download.
                    Log("Starts BackgroundDownloadAsync");
                    StartTaskAndContinueWith(
                        () => _liveClient.BackgroundDownloadAsync(
                            fileAttribute,
                            new Uri(GetTempIsoStorePath(file.Name), UriKind.RelativeOrAbsolute)
                            ),
                            t => OnLiveClientBackgroundDownloadCompleted(t, file));
				}
				catch (Exception)
				{
					// Tries the direct download method.
                    Log("Starts DownloadAsync (fallback)");
                    StartTaskAndContinueWith(
                        () => _liveClient.DownloadAsync(fileAttribute),
                        t => OnLiveClientDownloadCompleted(t, file));
				}
			}
			
		}

		private void EndSyncDownloads()
		{
			// Sync Step 5. 
			// The downloading phase of the sync is over, let's continue with uploads.

            Log("Downloads done. Moving on.");

			// Cancels everything if we shouldn't upload.
			if (!App.Current.Model.Settings.CanProviderUpload)
			{
				Log("Upload disabled by user setting.");
                EndSync();
				return;
			}

            // Starts uploads asynchronously.
            Task.Factory.StartNew(BeginUploads);
		}

        private async void BeginUploads()
        {
            // Makes a list of files to upload.
            Dictionary<string, DateTime> uploadCandidates;
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                uploadCandidates = isf.GetAllFiles("/*.gws")
                    .Union(isf.GetAllFiles("/*.gwl")
                    .Where(s => ("/" + s).StartsWith(IsoStoreCartridgeContentPath)))
                    .ToDictionary(s => s, s => isf.GetLastWriteTime(s).DateTime.ToUniversalTime());
            }

            // Returns if there is nothing to upload.
            if (uploadCandidates.Count < 1)
            {
                EndSync();
                return;
            }

            // Creates or fetches the upload folder.
            if (_uploadsFolderId == null)
            {
                try
                {
                    _uploadsFolderId = await CreateDirectoryAsync(UploadFolderName, _rootFolderId);
                }
                catch (Exception)
                {
                    Log("Error while ensuring Uploads folder.");
                    EndSync();
                    return;
                }
            }

            // Gets the list of files currently in the Uploads folder and removes from the upload candidates
            // the files that are identical to their remote counterpart.
            LiveOperationResult r = await _liveClient.GetAsync(_uploadsFolderId + "/files", CreateTimeoutCancellationToken(GetRequestTimeoutTimeSpan));
            dynamic result = r.Result;
            foreach (dynamic file in result.data)
            {
                // We only consider files in the upload directory.
                if (file.type != "file")
                {
                    continue;
                }

                // If the file name has no match in the upload candidates, go on.
                KeyValuePair<string, DateTime> candidate = uploadCandidates
                    .FirstOrDefault(kvp => Path.GetFileName(kvp.Key) == file.name);
                if (candidate.Key == null)
                {
                    continue;
                }

                // We have a match. If the remote file is more recent than the local file,
                // removes the candidate from the list of candidates.
                try
                {
                    DateTime remoteLastModified = DateTime.Parse(file.updated_time).ToUniversalTime();
                    if (remoteLastModified >= candidate.Value)
                    {
                        uploadCandidates.Remove(candidate.Key);
                        Log("Won't upload " + candidate.Key + " because it is same as, or older than remote file.");
                    }
                }
                catch (Exception)
                {
                    // We ignore exceptions and let this file be uploaded.
                }
            }

            // Returns if there is nothing to upload.
            if (uploadCandidates.Count < 1)
            {
                EndSync();
                return;
            }

            // Stores the list of files to upload.
            lock (_syncRoot)
            {
                _ulFiles = uploadCandidates.Keys.ToList();
            }

            foreach (string item in uploadCandidates.Keys)
            {
                Log("Scheduled for upload " + item);
            }

            // Starts uploading the first file. The next ones will be triggered once it finished uploading.
            string firstFile = _ulFiles[0];
            BeginUpload(firstFile);
        }

		private void EndSync()
		{
            Log("Sync ends.");

            // Clears tokens.
            _activeCancellationTokenSources.ForEach(cts => cts.Dispose());
            _activeCancellationTokenSources.Clear();
            
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

		private string GetIsoStorePath(string filename, string directory)
		{
			return String.Format("{0}/{1}", directory, filename);
		}

		private string GetTempIsoStorePath(string filename)
		{
			return String.Format("/shared/transfers/_{0}", filename);
		}

		private void OnLiveClientBackgroundDownloadCompleted(System.Threading.Tasks.Task<LiveOperationResult> task, OneDriveFile file)
		{
			// Sync Step 4. The file has already been downloaded to isostore.
			// Just move it to its right location.
			// (This only runs on the device.)

            if (!CheckTaskCompleted(task, "Background download failed (" + file.Name + ")"))
            {
                PostProcessDownload(file, null, false);
                return;
            }

            LiveOperationResult result = task.Result;

			string filepath = GetIsoStorePath(file.Name, file.DownloadDirectory);
            bool success;

			try
			{
				// Moves the file.
				using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
				{
					// Makes sure the directory exists.
					isf.CreateDirectory(file.DownloadDirectory);

					// Removes the destination file if it exists.
					if (isf.FileExists(filepath))
					{
						isf.DeleteFile(filepath);
					}

					// Moves the downloaded file to the right place.
					isf.MoveFile(GetTempIsoStorePath(file.Name), filepath);
                    Log("Moved file to " + filepath);

                    success = true;
				}
            }
            catch (Exception e)
            {
                Log("Error while moving " + filepath + ": " + e.Message);
                Geowigo.Utils.DebugUtils.DumpException(e, file.ToString(), true);

                success = false;
            }

			PostProcessDownload(file, filepath, success);
		}

		private void OnLiveClientDownloadCompleted(System.Threading.Tasks.Task<LiveDownloadOperationResult> task, OneDriveFile file)
		{            
            // Sync Step 4. The file has been downloaded as a memory stream.
			// Write it to its direct location.
			// (This runs on the emulator and on devices where the background download method failed.)

            if (!CheckTaskCompleted(task, "Download failed (" + file.Name + ")"))
            {
                PostProcessDownload(file, null, false);
                return;
            }

            LiveDownloadOperationResult result = task.Result;

            string filepath = GetIsoStorePath(file.Name, file.DownloadDirectory);
            bool success = false;

            try
            {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    // Makes sure the directory exists.
                    isf.CreateDirectory(file.DownloadDirectory);

                    // Creates a file at the right place.
                    using (IsolatedStorageFileStream fs = isf.OpenFile(filepath, FileMode.Create))
                    {
                        result.Stream.CopyTo(fs);
                    }
                }

                Log("Created " + filepath);

                success = true;
            }
            catch (Exception e)
            {
                Log("Error while writing " + filepath + ": " + e.Message);
                Geowigo.Utils.DebugUtils.DumpException(e, file.ToString(), true);

                success = false;
            }

			PostProcessDownload(file, filepath, success);
		}

        private void OnLiveClientGetRootChildrenCompleted(System.Threading.Tasks.Task<LiveOperationResult> task)
        {
            if (!CheckTaskCompleted(task, "Get root children failed."))
            {
                EndSyncDownloads();
                return;
            }

            LiveOperationResult res = task.Result;

            // No result? Nothing to do.
            if (res.Result == null)
            {
                Log("Get root children returned no results.");
                EndSyncDownloads();
                return;
            }
            
            // Sync Step 2: We are getting results for the app root folder.
            // We need to enumerate through all file entries to download GWC and GWS files.

            List<object> data = (List<object>)res.Result["data"];

            // Enumerates through all the file entries.
            List<OneDriveFile> cartFiles = new List<OneDriveFile>();
            List<OneDriveFile> extraFiles = new List<OneDriveFile>();
            foreach (IDictionary<string, object> content in data)
            {
                // Is it a cartridge file?
                string name = (string)content["name"];
                string lname = name.ToLower();
                object type = content["type"];
                try
                {
                    Log("Found " + type + " called " + lname);
                }
                catch (Exception)
                {
                }
                if ("file".Equals(type))
                {
                    if (lname.EndsWith(".gwc"))
                    {
                        // Adds the file to the list of cartridges.
                        Log("Marked as cartridge to download: " + name);
                        cartFiles.Add(new OneDriveFile((string)content["id"], name, IsoStoreCartridgesPath));
                    }
                    else if (lname.EndsWith(".gws"))
                    {
                        // Adds the file to the list of extra files.
                        Log("Marked as extra file to download: " + name);
                        extraFiles.Add(new OneDriveFile((string)content["id"], name, IsoStoreCartridgeContentPath));
                    }
                }
            }

            // Creates the list of cartridges in the isostore that do not exist on OneDrive anymore.
            // These will be removed. Extra files are not removed even if they are not on OneDrive anymore.
            List<string> isoStoreFiles;
            List<string> toRemoveFiles;
            List<string> isoStoreExtraFiles;
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                isoStoreExtraFiles = isf.GetAllFiles(IsoStoreCartridgeContentPath + "/*.gws");

                isoStoreFiles =
                    isf
                    .GetFileNames(GetIsoStorePath("*.gwc", IsoStoreCartridgesPath))
                    .Select(s => IsoStoreCartridgesPath + "/" + s)
                    .ToList();

                toRemoveFiles =
                    isoStoreFiles
                    .Where(s => !cartFiles.Any(sd => sd.Name == System.IO.Path.GetFileName(s)))
                    .ToList();
            }

            // Clears from the list of extra files to download those which
            // are already somewhere in the isolated storage.
            foreach (OneDriveFile ef in extraFiles.ToList())
            {
                if (isoStoreExtraFiles.Any(s => s.EndsWith("/" + ef.Name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    // The file needn't be downloaded.
                    Log("Unmarked download because it exists locally: " + ef);
                    extraFiles.Remove(ef);
                }
            }

            // Creates the list of cartridges and extra files that are on OneDrive but
            // not in the isolated storage.
            List<OneDriveFile> toDlFiles = cartFiles
                .Where(sd => !isoStoreFiles.Contains(GetIsoStorePath(sd.Name, sd.DownloadDirectory)))
                .Union(extraFiles)
                .ToList();

            foreach (OneDriveFile file in toDlFiles)
            {
                Log("Scheduled to download: " + file);
            }
            Log("Total count of scheduled downloads: " + toDlFiles.Count);

            foreach (string fileName in toRemoveFiles)
            {
                Log("Scheduled to remove locally: " + fileName);
            }
            Log("Total count of scheduled local removals: " + toRemoveFiles.Count);

            // Bakes an event for when the sync will be over.
            lock (_syncRoot)
            {
                _syncEventArgs = new CartridgeProviderSyncEventArgs()
                {
                    AddedFiles = toDlFiles
                        .Select(sd => GetIsoStorePath(sd.Name, sd.DownloadDirectory))
                        .ToList(),
                    ToRemoveFiles = toRemoveFiles
                };
            }

            // Sends a progress event for removing the files marked to be removed.
            if (toRemoveFiles.Count > 0)
            {
                RaiseSyncProgress(toRemoveFiles: toRemoveFiles);
            }

            // Starts downloading all new files, or terminate the sync if no new file is to be downloaded.
            if (toDlFiles.Count > 0)
            {
                toDlFiles.ForEach(sd => BeginDownloadFile(sd));
            }
            else
            {
                EndSyncDownloads();
            }
        }

		private void OnLiveClientUploadCompleted(System.Threading.Tasks.Task<LiveOperationResult> task, string localFilePath)
		{
			// Sync Step 6. An upload has finished or didn't work.
			// Try to upload the next pending file or finish the whole process.

            if (CheckTaskCompleted(task, "Upload failed (" + localFilePath + ")"))
            {
                Log("Upload completed: " + localFilePath);
            }
			
            PostProcessUpload(localFilePath);
		}

        private void BeginUpload(string localFilePath)
        {
            try
            {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    _currentUlFileStream = isf.OpenFile(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

                    Log("Starts UploadAsync");

                    StartTaskAndContinueWith(
                        () => _liveClient.UploadAsync(_uploadsFolderId, Path.GetFileName(localFilePath), _currentUlFileStream, OverwriteOption.Overwrite),
                        t => OnLiveClientUploadCompleted(t, localFilePath));
                }
            }
            catch (Exception e)
            {
                Geowigo.Utils.DebugUtils.DumpException(e, "BeginUpload failed for " + localFilePath, true);

                // Forces a post-process of the upload to discard this file and try another one.
                PostProcessUpload(localFilePath);
            }
        }

        private void PostProcessUpload(string localFilePath)
        {
            // Removes the file from the current list of pending uploads.
            string nextFile = null;
            lock (_syncRoot)
            {
                // Removes the completed upload.
                _ulFiles.Remove(localFilePath);

                // Gets the next one.
                nextFile = _ulFiles.FirstOrDefault();
            }

            // Disposes the current file stream if any.
            if (_currentUlFileStream != null)
            {
                _currentUlFileStream.Dispose();
                _currentUlFileStream = null;
            }

            // Uploads the next file or ends the sync.
            if (nextFile == null)
            {
                // If there is nothing more to do, the sync is over.
                EndSync();
            }
            else
            {
                // Uploads the next file.
                BeginUpload(nextFile);
            }
        }

		private void PostProcessDownload(OneDriveFile file, string filepath, bool wasSuccess)
		{
            // Removes the file from the queue of files to download.
            int filesLeft;
            lock (_syncRoot)
            {
                // Removes the file if it is registered.
                _dlFiles.RemoveAll(f => f.Id == file.Id);

                // Gets how many files are pending completion.
                filesLeft = _dlFiles.Count;
            }

            if (wasSuccess)
            {
                // Raise information about this progress.
                RaiseSyncProgress(addedFiles: new string[] { filepath }); 
            }

			// If no more files are pending download, the sync is over!
			if (filesLeft == 0)
			{
				EndSyncDownloads();
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
