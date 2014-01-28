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

namespace Geowigo.Models
{
	/// <summary>
	/// A store for Cartridges and their related data.
	/// </summary>
	public class CartridgeStore : ReadOnlyObservableCollection<CartridgeTag>
	{
		#region Members

		private List<ICartridgeProvider> _providers = new List<ICartridgeProvider>();
		private bool _isBusy = false;
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
				return _isBusy;
			}

			private set
			{
				if (value != _isBusy)
				{
					_isBusy = value;

					//OnIsBusyChanged(EventArgs.Empty);
					OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("IsBusy"));
				}
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
		/// <returns>Null if the tag was not found.</returns>
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

				// Business changes.
				IsBusy = true;

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

				// Business changes.
				IsBusy = false;
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
			bool shouldBeBusy = false; 

			foreach (ICartridgeProvider provider in _providers)
			{
				if (provider.IsLinked && !provider.IsSyncing)
				{
					provider.BeginSync();
					shouldBeBusy = true;
				}
			}

			if (shouldBeBusy)
			{
				IsBusy = true;
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
                    return null;
                }

                // Loads the metadata.
                using (IsolatedStorageFileStream isfs = isf.OpenFile(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
					WF.Player.Core.Formats.FileFormats.Load(isfs, cart);
                }
			}
			
			// Returns the existing cartridge if it was found.
			lock (_syncRoot)
			{
				existingCC = this.Items.SingleOrDefault(cc => cc.Guid == cart.Guid); 
			}
			if (existingCC != null)
			{
				return existingCC;
			}
			
			// The cartridge does not exist in the store yet. Creates an entry for it.
			CartridgeTag newCC = new CartridgeTag(cart);

			// Adds the context to the store.
			lock (_syncRoot)
			{
				this.Items.Add(newCC);
			}

			// Makes the cache.
			newCC.ImportOrMakeCache();

			// Returns the new cartridge context.
			return newCC;
		}

		#endregion

		#region Provider Management

		private void AddDefaultProviders()
		{
			AddProvider(new SkyDriveCartridgeProvider());
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

			// Adds the provider to the list.
			_providers.Add(provider);

			// Notifies the list has changed.
			OnPropertyChanged(new PropertyChangedEventArgs("Providers"));
		}

		private void OnProviderSyncAborted(object sender, CartridgeProviderSyncAbortEventArgs e)
		{
			IsBusy = false;
		}

		private void OnProviderSyncCompleted(object sender, CartridgeProviderSyncEventArgs e)
		{
			ProcessSyncEvent(e, true);
		}

		private void OnProviderSyncProgress(object sender, CartridgeProviderSyncEventArgs e)
		{
			IsBusy = true;
			ProcessSyncEvent(e, false);
		}

		private void ProcessSyncEvent(CartridgeProviderSyncEventArgs e, bool busyStateChange)
		{
			// The sync is complete.
			if (busyStateChange)
			{
				IsBusy = true; 
			}

			// Rejects the files marked to be removed.
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				foreach (string filename in e.ToRemoveFiles)
				{
					// Removes the file from the list.
					RejectCartridge(filename);

					// Deletes the file in the store. 
					if (isf.FileExists(filename))
					{
						isf.DeleteFile(filename);
					}
				}
			}

			// Accepts the files that have been added.
			foreach (string filename in e.AddedFiles)
			{
				AcceptCartridge(filename);
			}

			// Finished.
			if (busyStateChange)
			{
				IsBusy = false; 
			}
		}

		private void OnProviderPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			ICartridgeProvider provider = (ICartridgeProvider)sender;

			if (e.PropertyName == "IsLinked" && provider.IsLinked)
			{
				// The provider is now linked. Start syncing.
				IsBusy = true;
				provider.BeginSync();
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
	}
}
