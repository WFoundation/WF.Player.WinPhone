using System;
using System.Collections.Generic;
using WF.Player.Core;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Collections.Specialized;
using Geowigo.Models.Providers;
using Geowigo.Utils;

namespace Geowigo.Models
{
	/// <summary>
	/// A store for Cartridges and their related data.
	/// </summary>
	public class CartridgeStore : ReadOnlyObservableCollection<CartridgeTag>
	{
		#region Members

		private ProgressAggregator _isBusyAggregator = new ProgressAggregator();
		private List<ICartridgeProvider> _providers = new List<ICartridgeProvider>();
		private object _syncRoot = new object();

		#endregion

		#region Events

		public new event PropertyChangedEventHandler PropertyChanged
		{
			add
			{
				base.PropertyChanged += value;
			}

			remove
			{
				base.PropertyChanged -= value;
			}
		}

        public new event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                base.CollectionChanged += value;
            }

            remove
            {
                base.CollectionChanged -= value;
            }
        }


		#endregion
		
		#region Properties

		/// <summary>
		/// Gets the path to the folder containing cartridge data in the isolated storage.
		/// </summary>
		public string IsoStoreCartridgesPath
		{
			get
			{
				return "/Cartridges";
			}
		}
		
		/// <summary>
		/// Gets if this instance is busy loading cartridges.
		/// </summary>
		public bool IsBusy
		{
			get
			{
				return _isBusyAggregator.HasWorkingSource;
			}
		}

		/// <summary>
		/// Gets an enumeration of the cartridge providers that can download
		/// cartridges for this instance.
		/// </summary>
		public IEnumerable<ICartridgeProvider> Providers
		{
			get
			{
				return _providers;
			}
		}

		#endregion

		#region Constructors

		public CartridgeStore() 
			: base(new ObservableCollection<CartridgeTag>())
		{
			// Registers event handlers.
			_isBusyAggregator.PropertyChanged += new PropertyChangedEventHandler(OnIsBusyAggregatorPropertyChanged);

			// Adds some cartridge providers.
			AddDefaultProviders();
		}

		#endregion
		
		#region Tag Retrieval

		/// <summary>
		/// Gets the single tag for a Cartridge.
		/// </summary>
		/// <param name="cartridge">The Cartridge to get the tag for.</param>
		/// <returns></returns>
		public CartridgeTag GetCartridgeTagOrDefault(Cartridge cartridge)
		{
			lock (_syncRoot)
			{
				return this.SingleOrDefault(ct => ct.Cartridge.Guid == cartridge.Guid); 
			}
		}

		/// <summary>
		/// Gets the single tag for a cartridge filename and a GUID.
		/// </summary>
		/// <remarks>This method looks for a tag with similar GUID. If not found,
		/// it tries to load the cartridge at the specified filename and returns
		/// its tag if the GUIDs match.</remarks>
		/// <param name="filename">Filename of the cartridge.</param>
		/// <param name="guid">Guid of the Cartridge to get.</param>
		/// <returns>Null if the tag was not found or the GUIDs didn't match.</returns>
		public CartridgeTag GetCartridgeTagOrDefault(string filename, string guid)
		{
			CartridgeTag tag = null;
			lock (_syncRoot)
			{
				// Tries to get the tag if it is registered already.
				tag = this.SingleOrDefault(ct => ct.Cartridge.Guid == guid);
			}

			// If the tag is found, returns it.
			if (tag != null)
			{
				return tag;
			}

			// Tries to accept the tag from filename.
			tag = AcceptCartridge(filename);

			// Only returns the tag if both GUIDs match.
			return tag == null || tag.Guid != guid ? null : tag;
		}

		/// <summary>
		/// Gets the single tag for a cartridge filename.
		/// </summary>
		/// <param name="filename">Filename of the cartridge.</param>
		/// <returns>The single cartridge tag to match this filename, or null if 
		/// the cartridge was not found.</returns>
		public CartridgeTag GetCartridgeTagOrDefault(string filename)
		{
			return AcceptCartridge(filename);
		}

		#endregion

		#region Sync From IsoStore
		/// <summary>
		/// Synchronizes the store from the Isolated Storage.
		/// </summary>
		public void SyncFromIsoStore()
		{
			BackgroundWorker bw = new BackgroundWorker();

			bw.DoWork += (o, e) =>
			{
				SyncFromIsoStoreCore(false);
			};

			bw.RunWorkerAsync();
		}

		private void SyncFromIsoStoreCore(bool asyncEachCartridge)
		{
			// Opens the isolated storage.
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				// Makes sure the folder exists.
				isf.CreateDirectory(IsoStoreCartridgesPath);

				// Goes through all the sub-directories and accepts the cartridge files.
				IEnumerable<string> dirs = GetAllDirectoryNames(isf, IsoStoreCartridgesPath);
				foreach (string dir in dirs)
				{
					// Imports all GWC files from the directory.
					foreach (string filename in isf.GetFileNames(dir + "/*.gwc"))
					{
						string filepath = dir + "/" + filename;
						
						// Accept the GWC.
						if (asyncEachCartridge)
						{
							AcceptCartridgeAsync(filepath);
						}
						else
						{
							AcceptCartridge(filepath);
						}

					}
				}
			}
		}

		private IEnumerable<string> GetAllDirectoryNames(IsolatedStorageFile isf, string rootPath)
		{
			List<string> allDirs = new List<string>();

			// Gets the paths of sub-directories of first level in rootPath.
			IEnumerable<string> subPaths;
			try
			{
				subPaths = isf
					.GetDirectoryNames(rootPath + "/*")
					.Select(s => rootPath + "/" + s);
			}
			catch (Exception)
			{
				subPaths = new string[] { };
			}

			// For each of them, check for sub-directories.
			foreach (string subPath in subPaths)
			{
				allDirs.AddRange(GetAllDirectoryNames(isf, subPath));
			}

			// Adds the root path to the list.
			allDirs.Add(rootPath);
			
			return allDirs;
		} 

		#endregion

		#region Sync From Providers

		/// <summary>
		/// Starts syncing all linked providers that are not syncing.
		/// </summary>
		public void SyncFromProviders()
		{
			foreach (ICartridgeProvider provider in _providers)
			{
				if (provider.IsLinked && !provider.IsSyncing)
				{
					provider.BeginSync();
				}
			}
		}

		#endregion

		#region Tag Acceptance

		/// <summary>
		/// Ensures that a cartridge is not present in the store.
		/// </summary>
		/// <param name="filename">Filename of the cartridge to remove.</param>
		/// <remarks>This method does not effectively remove the cartridge
		/// from the isolated storage.</remarks>
		private void RejectCartridge(string filename)
		{
			System.Diagnostics.Debug.WriteLine("CartridgeStore: Trying to reject cartridge " + filename);

			// Updates the progress.
			string businessTag = "reject" + filename;
			_isBusyAggregator[businessTag] = true;

			lock (_syncRoot)
			{
				// Gets the existing cartridge if it was found.
				CartridgeTag existingTag = this.Items.SingleOrDefault(cc => cc.Cartridge.Filename == filename);

				// Removes the tag if it was found.
				if (existingTag != null)
				{
					this.Items.Remove(existingTag); 
				}
			}

			// Updates the progress.
			_isBusyAggregator[businessTag] = false;
		}

		/// <summary>
		/// Ensures asynchronously that a cartridge is present in the store.
		/// </summary>
		/// <param name="filename">Filename of the cartridge to consider.</param>
		private void AcceptCartridgeAsync(string filename)
		{
			BackgroundWorker bw = new BackgroundWorker();
			bw.DoWork += new System.ComponentModel.DoWorkEventHandler((o, e) => AcceptCartridge(filename));
			bw.RunWorkerAsync();
		}

		/// <summary>
		/// Ensures that a cartridge is present in the store.
		/// </summary>
		/// <param name="filename">Filename of the cartridge to consider.</param>
		/// <returns>The CartridgeContext for this cartridge from the store, or a new CartridgeContext
		/// if there was none in store for this cartridge.</returns>
		private CartridgeTag AcceptCartridge(string filename)
		{
            System.Diagnostics.Debug.WriteLine("CartridgeStore: Trying to accept cartridge " + filename);

			bool isAborted = false;

			// Refreshes the progress.
			string businessTag = "accept:" + filename;
			_isBusyAggregator[businessTag] = true;

            // Creates a cartridge object.
			Cartridge cart = new Cartridge(filename);

			// Loads the cartridge.
			CartridgeTag existingCC;
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
                // File exist check.
                if (!isf.FileExists(filename))
                {
                    System.Diagnostics.Debug.WriteLine("CartridgeStore: WARNING: Cartridge file not found: " + filename);

					isAborted = true;
                }

                // Loads the metadata.
				if (!isAborted)
				{
					using (IsolatedStorageFileStream isfs = isf.OpenFile(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read))
					{
						try
						{
							WF.Player.Core.Formats.CartridgeLoaders.Load(isfs, cart);
						}
						catch (Exception ex)
						{
							// This cartridge seems improper to loading.
							// Let's just dump the exception and return.
							DebugUtils.DumpException(ex, dumpOnBugSenseToo: true);
							System.Diagnostics.Debug.WriteLine("CartridgeStore: WARNING: Loading failed, ignored : " + filename);
							isAborted = true;
						}
					} 
				}
			}

			CartridgeTag newCC = null;
			if (!isAborted)
			{
				// Returns the existing cartridge if it was found.
				lock (_syncRoot)
				{
					existingCC = this.Items.SingleOrDefault(cc => cc.Guid == cart.Guid);
				}
				if (existingCC != null)
				{
					// Refreshes the progress.
					_isBusyAggregator[businessTag] = false;

					return existingCC;
				}

				// The cartridge does not exist in the store yet. Creates an entry for it.
				newCC = new CartridgeTag(cart);

				// Adds the context to the store.
				lock (_syncRoot)
				{
					this.Items.Add(newCC);
				}

				// Makes the cache.
				newCC.ImportOrMakeCache(); 
			}

			// Refreshes the progress.
			_isBusyAggregator[businessTag] = false;

			// Returns the new cartridge context or null if the operation
			// was aborted.
			return newCC;
		}

		private void AcceptSavegame(string filename)
		{
			// Copies this savegame to the content folders of each cartridge
			// whose name matches the cartridge name in the savegame metadata.

			System.Diagnostics.Debug.WriteLine("CartridgeStore: Trying to accept savegame " + filename);

			// Refreshes the progress.
			string businessTag = "accept:" + filename;
			_isBusyAggregator[businessTag] = true;

			// Gets the cartridge this savegame is associated with.
			bool isAborted = false;
			WF.Player.Core.Formats.GWS.Metadata saveMetadata = null;
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				if (!isf.FileExists(filename))
				{
					System.Diagnostics.Debug.WriteLine("CartridgeStore: WARNING: Savegame file not found: " + filename);

					isAborted = true;
				}

				if (!isAborted)
				{
					using (IsolatedStorageFileStream fs = isf.OpenFile(filename, System.IO.FileMode.Open))
					{
						saveMetadata = WF.Player.Core.Formats.GWS.LoadMetadata(fs);
					} 
				}
			}

			if (!isAborted)
			{
				// For each matching tag, creates an associated savegame and copies the file to each
				// tag's content folder.
				List<CartridgeTag> matches;
				lock (_syncRoot)
				{
					matches = Items.Where(ct => ct.Title == saveMetadata.CartridgeName).ToList();
				}
				using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
				{
					foreach (CartridgeTag tag in matches)
					{
						// Creates a savegame.
						CartridgeSavegame save = new CartridgeSavegame(tag, saveMetadata, System.IO.Path.GetFileName(filename));

						// Copies the new file to the right isolated storage.
						isf.CopyFile(filename, save.SavegameFile);

						// Adds the savegame to its tag.
						tag.AddSavegame(save);
					}
				} 
			}

			// Refreshes the progress.
			_isBusyAggregator[businessTag] = false;
		}

		#endregion

		#region Provider Management

		private void AddDefaultProviders()
		{
			AddProvider(new SkyDriveCartridgeProvider() { IsBackgroundDownloadAllowed = true });
		}

		private void AddProvider(ICartridgeProvider provider)
		{
			// Sanity check.
			if (_providers.Any(p => p.ServiceName == provider.ServiceName))
			{
				throw new InvalidOperationException("A provider with same ServiceName already exists. " + provider.ServiceName);
			}
			
			// Registers event handlers.
			provider.PropertyChanged += new PropertyChangedEventHandler(OnProviderPropertyChanged);
			provider.SyncCompleted += new EventHandler<CartridgeProviderSyncEventArgs>(OnProviderSyncCompleted);
			provider.SyncProgress += new EventHandler<CartridgeProviderSyncEventArgs>(OnProviderSyncProgress);
			provider.SyncAborted += new EventHandler<CartridgeProviderSyncAbortEventArgs>(OnProviderSyncAborted);
			
			// Sets the provider up.
			provider.IsoStoreCartridgesPath = String.Format("{0}/From {1}", IsoStoreCartridgesPath, provider.ServiceName);
			provider.IsoStoreCartridgeContentPath = CartridgeTag.GlobalSavegamePath;

			// Adds the provider to the list.
			_providers.Add(provider);

			// Notifies the list has changed.
			OnPropertyChanged(new PropertyChangedEventArgs("Providers"));
		}

		private void OnProviderSyncAborted(object sender, CartridgeProviderSyncAbortEventArgs e)
		{
			_isBusyAggregator[sender] = false;
		}

		private void OnProviderSyncCompleted(object sender, CartridgeProviderSyncEventArgs e)
		{
			ProcessSyncEvent(e);

			_isBusyAggregator[sender] = false;
		}

		private void OnProviderSyncProgress(object sender, CartridgeProviderSyncEventArgs e)
		{
			_isBusyAggregator[sender] = true;

			ProcessSyncEvent(e);
		}

		private void ProcessSyncEvent(CartridgeProviderSyncEventArgs e)
		{
			// Accepts the files that have been added.
			List<string> filesToRemove = new List<string>();
			foreach (string filename in e.AddedFiles.Where(s => s.EndsWith(".gwc", StringComparison.InvariantCultureIgnoreCase)))
			{
				AcceptCartridge(filename);
			}
			foreach (string filename in e.AddedFiles.Where(s => s.EndsWith(".gws", StringComparison.InvariantCultureIgnoreCase)))
			{
				// Copies this savegame to the content folders of each cartridge
				// whose name matches the cartridge name in the savegame metadata.
				AcceptSavegame(filename);

				// Marks the file to be deleted.
				filesToRemove.Add(filename);
			}

			// Rejects and removes the files marked to be removed.
			filesToRemove.AddRange(e.ToRemoveFiles);
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				foreach (string filename in filesToRemove)
				{
					// Removes the file from the list.
					if (filename.EndsWith(".gwc", StringComparison.InvariantCultureIgnoreCase))
					{
						RejectCartridge(filename); 
					}

					// Deletes the file in the store. 
					if (isf.FileExists(filename))
					{
						isf.DeleteFile(filename);
					}
				}
			}

		}

		private void OnProviderPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			ICartridgeProvider provider = (ICartridgeProvider)sender;

			if (e.PropertyName == "IsLinked" && provider.IsLinked)
			{
				// The provider is now linked. Start syncing.
				provider.BeginSync();
			}
			else if (e.PropertyName == "IsSyncing")
			{
				_isBusyAggregator[provider] = provider.IsSyncing;
			}
		}

		#endregion

		#region ReadOnlyObservableCollection

		protected override void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() => base.OnCollectionChanged(args));
		}

		protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs args)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() => base.OnPropertyChanged(args));
		}

		#endregion

		private void OnIsBusyAggregatorPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "HasWorkingSource")
			{
				// Relays the event.
				OnPropertyChanged(new PropertyChangedEventArgs("IsBusy"));
			}
		}
	}
}
