using System;
using System.Collections.Generic;
using WF.Player.Core;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.Collections.Specialized;

namespace Geowigo.Models
{
	/// <summary>
	/// A store for Cartridges and their related data.
	/// </summary>
	public class CartridgeStore : ReadOnlyObservableCollection<CartridgeTag>
	{
		#region Members

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

		#endregion

		#region Constructors

		public CartridgeStore() 
			: base(new ObservableCollection<CartridgeTag>())
		{
			
		}

		#endregion
		
		#region Public Methods

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

		/// <summary>
		/// Synchronizes the store from the Isolated Storage.
		/// </summary>
		public void SyncFromIsoStore()
		{
            BackgroundWorker bw = new BackgroundWorker();
            
            bw.DoWork += (o, e) => { 
                SyncFromIsoStoreCore(false); 
            };
            
            bw.RunWorkerAsync();
		}

        private void SyncFromIsoStoreCore(bool asyncEachCartridge)
        {
            // Opens the isolated storage.
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                // Checks if the Cartridge folder exists.
                string[] dirs = isf.GetDirectoryNames(IsoStoreCartridgesPath);
                if (dirs.Count() > 1)
                {
                    System.Diagnostics.Debug.WriteLine("WARNING !!! CartridgeStore.SyncFromIsoStore: More than one cartridge directory: " + IsoStoreCartridgesPath);
                }

                // Business changes.
                IsBusy = true;

                foreach (string dir in dirs)
                {
                    // Imports all GWC files from the directory.
                    foreach (string filename in isf.GetFileNames(IsoStoreCartridgesPath + "/*.gwc"))
                    {
                        // Accept the GWC.
                        if (asyncEachCartridge)
                        {
                            AcceptCartridgeAsync(IsoStoreCartridgesPath + "/" + filename);
                        }
                        else
                        {
                            AcceptCartridge(IsoStoreCartridgesPath + "/" + filename);
                        }
                        
                    }
                }

                // Business changes.
                IsBusy = false;
            }
        }

		#endregion

		#region Cartridge Tags Management

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
