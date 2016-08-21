using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Geowigo.Models.Providers
{
	/// <summary>
	/// Describes a provider that can download files from remote
	/// storage into isolated storage.
	/// </summary>
	public interface ICartridgeProvider : INotifyPropertyChanged
	{
		/// <summary>
		/// Gets the user-displayable name of the service.
		/// </summary>
		string ServiceName { get; }

		/// <summary>
		/// Gets if this provider is linked to valid credentials
		/// and has valid session information.
		/// </summary>
		bool IsLinked { get; }

		/// <summary>
		/// Gets if this provider is performing a syncing operation.
		/// </summary>
		bool IsSyncing { get; }

		/// <summary>
		/// Gets or sets the path to isolated storage where cartridges
		/// downloaded by this provider are being stored.
		/// </summary>
		string IsoStoreCartridgesPath { get; set; }

		/// <summary>
		/// Gets or sets the path to isolated storage where extra cartridge
		/// content download by this provider is being stored.
		/// </summary>
		string IsoStoreCartridgeContentPath { get; set; }

		/// <summary>
		/// Raised when a link has aborted.
		/// </summary>
		event EventHandler<CartridgeProviderFailEventArgs> LinkAborted;

		/// <summary>
		/// Raised when a synchronization has completed.
		/// </summary>
		/// <remarks>The event arguments recapitulate all the
		/// files that have been added or marked for deletion,
		/// even those that were mentioned in <code>SyncProgress</code>
		/// events for the current sync.</remarks>
		event EventHandler<CartridgeProviderSyncEventArgs> SyncCompleted;

		/// <summary>
		/// Raised when progress information about a current sync
		/// operations is available.
		/// </summary>
		event EventHandler<CartridgeProviderSyncEventArgs> SyncProgress;

		/// <summary>
		/// Raised when the synchronization has aborted.
		/// </summary>
		event EventHandler<CartridgeProviderFailEventArgs> SyncAborted;

		/// <summary>
		/// Starts to sync this provider's isolated storage folder with
		/// the contents of the remote storage.
		/// </summary>
		/// <remarks>
		/// This method only downloads new files and removes old files.
		/// No change is performed on the remote storage.
		/// </remarks>
		void BeginSync();

		/// <summary>
		/// Starts to link this provider with an account if it is not
		/// already linked.
		/// </summary>
		/// <remarks>
		/// This method can trigger navigational changes in the app,
		/// eventually displaying a custom sign-in experience.
		/// </remarks>
		void BeginLink();
	}
}
