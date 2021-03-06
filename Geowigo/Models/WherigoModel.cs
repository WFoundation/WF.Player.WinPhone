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
using Geowigo.Utils;
using System.ComponentModel;

namespace Geowigo.Models
{
	/// <summary>
	/// Provides facility methods to query the Wherigo engine model.
	/// </summary>
	public class WherigoModel
	{
		#region Properties

		/// <summary>
		/// Gets the Wherigo core responsible for gameplay and feedback.
		/// </summary>
		public WFCoreAdapter Core { get; private set; }

		/// <summary>
		/// Gets the cartridge store that registers cartridges.
		/// </summary>
		public CartridgeStore CartridgeStore { get; private set; }

        /// <summary>
        /// Gets the history of user operations.
        /// </summary>
        public History History { get; private set; }

        /// <summary>
        /// Gets the user settings.
        /// </summary>
        public Settings Settings { get; private set; }

		#endregion

		#region Constructors

		public WherigoModel()
		{
            Settings = new Models.Settings();
            Settings.PropertyChanged += OnSettingsPropertyChanged;
            
            Core = new WFCoreAdapter();

            CartridgeStore = new CartridgeStore()
            {
                AutoSyncProvidersOnLink = Settings.SyncOnStartUp
            };

            History = Models.History.FromCacheOrCreate();
		}

		#endregion

		/// <summary>
		/// When all hope died, as a last resort, this method kills the
		/// current Wherigo Core instance and replaces it with a new one.
		/// </summary>
		/// <remarks>
		/// As with all last resorts, potential side-effects are numerous and
		/// unpredictable.
		/// </remarks>
		internal void HardResetCore()
		{
			// Immediately disposes the current instance, if any.
			if (Core != null)
			{
				try
				{
					Core.Dispose();
				}
				catch (Exception ex)
				{
					// Not good: the engine wouldn't let itself be disposed.
					// Signal this.
					DebugUtils.DumpException(ex, "hard reset, failed Engine disposal", true);
				}
			}

			// A new Core for X-mas.
			Core = new WFCoreAdapter();
		}

        /// <summary>
        /// Removes a cartridge from the store, and wipes all its related content, including
        /// cache, logs and savegames.
        /// </summary>
        /// <param name="tag"></param>
        public void DeleteCartridgeAndContent(CartridgeTag tag)
        {
            // Clears the history for this tag.
            History.RemoveAllOf(tag.Guid);
            
            // Removes it from the store.
            CartridgeStore.RemoveCartridgeTag(tag, true);
        }

        private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SyncOnStartUp")
            {
                CartridgeStore.AutoSyncProvidersOnLink = Settings.SyncOnStartUp;
            }
        }
	}
}
