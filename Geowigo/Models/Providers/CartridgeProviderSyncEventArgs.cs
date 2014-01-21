using System;
using System.Collections.Generic;

namespace Geowigo.Models.Providers
{
	/// <summary>
	/// Arguments for a provider's synchronization of cartridges.
	/// </summary>
	public class CartridgeProviderSyncEventArgs : EventArgs
	{
		/// <summary>
		/// Gets or sets the file paths that were added to the
		/// isolated storage during the sync.
		/// </summary>
		public IEnumerable<string> AddedFiles { get; set; }

		/// <summary>
		/// Gets or sets the file paths that need to be removed from 
		/// the isolated storage.
		/// </summary>
		/// <remarks>
		/// They have not been removed yet to give time to callers
		/// to prevent the user from running a deleted cartridge.
		/// </remarks>
		public IEnumerable<string> ToRemoveFiles { get; set; }
	}
}
