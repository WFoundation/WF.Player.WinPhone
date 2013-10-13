using System;
using System.Collections.Generic;
using WF.Player.Core;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Linq;

namespace Geowigo.Models
{
	/// <summary>
	/// A store for Cartridges and their related data.
	/// </summary>
	public class CartridgeStore : ReadOnlyObservableCollection<CartridgeTag>
	{
		
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

		#endregion

		#region Constructors

		public CartridgeStore() 
			: base(new ObservableCollection<CartridgeTag>())
		{
			
		}

		#endregion
		
		#region Public Methods

		public void SyncFromIsoStore()
		{
			// TODO: Make Async.
			
			// Opens the isolated storage.
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				// Checks if the Cartridge folder exists.
				string[] dirs = isf.GetDirectoryNames(IsoStoreCartridgesPath);
				if (dirs.Count() > 1)
				{
					System.Diagnostics.Debug.WriteLine("WARNING !!! CartridgeStore.SyncFromIsoStoreAsync: More than one cartridge directory: " + IsoStoreCartridgesPath);
				}
				foreach (string dir in dirs)
				{
					// Imports all GWC files from the directory.
					foreach (string filename in isf.GetFileNames(IsoStoreCartridgesPath + "/*.gwc"))
					{
						// Accept the GWC.
						AcceptCartridge(IsoStoreCartridgesPath + "/" + filename);
					}
				}
			}
		}

		#endregion

		#region CartridgeContext Management

		/// <summary>
		/// Ensures that a cartridge is present in the store.
		/// </summary>
		/// <param name="filename">Filename of the cartridge to consider.</param>
		/// <returns>The CartridgeContext for this cartridge from the store, or a new CartridgeContext
		/// if there was none in store for this cartridge.</returns>
		private CartridgeTag AcceptCartridge(string filename)
		{
			// Creates a cartridge object.
			Cartridge cart = new Cartridge(filename);

			// Loads the cartridge.
			CartridgeTag existingCC;
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream isfs = isf.OpenFile(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read))
				{
					// Loads the metadata.
					WF.Player.Core.Formats.FileFormats.Load(isfs, cart);
				}
			}
			
			// Returns the existing cartridge if it was found.
			existingCC = this.Items.SingleOrDefault(cc => cc.Guid == cart.Guid);
			if (existingCC != null)
			{
				return existingCC;
			}
			
			// The cartridge does not exist in the store yet. Creates an entry for it.
			CartridgeTag newCC = new CartridgeTag(cart);

			// Adds the context to the store.
			this.Items.Add(newCC);

			// Returns the new cartridge context.
			return newCC;
		}

		#endregion
	}
}
