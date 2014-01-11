using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.IO.IsolatedStorage;
using Geowigo.Utils;
using WF.Player.Core;
using System.Collections.Specialized;
using System.Linq;

namespace Geowigo.Models
{
    [CollectionDataContract]
    public class History : ObservableCollection<HistoryEntry>
    {
        #region Properties

        /// <summary>
        /// Gets the path to this history in the isolated storage.
        /// </summary>
        public string IsoStoreHistoryPath
        {
            get
            {
                return CommonHistoryPath;
            }
        }

        /// <summary>
        /// Gets or sets if this history is synchronized with the cache.
        /// </summary>
        /// <remarks>
        /// If true, this history exports itself to the cache every time
        /// it is modified.
        /// </remarks>
        protected bool IsSyncedWithCache
        {
            get
            {
                lock (_syncRoot)
                {
                    return _isSynced;
                }
            }

            set
            {
                lock (_syncRoot)
                {
                    _isSynced = value;
                }
            }
        }

        #endregion

        #region Members

        private static readonly string CommonHistoryPath = "/History/userhistory.txt";

        private bool _isSynced = false;

        private object _syncRoot = new object();

        #endregion
        
        #region Constructors

        public History()
            : base()
        {
            
        }

        #endregion
        
        #region Cache

        /// <summary>
        /// Imports the history from the isolated storage, or creates
        /// a new empty one if none is found.
        /// </summary>
        /// <returns>The most up-to-date history.</returns>
        public static History FromCacheOrCreate()
        {
            // Checks if the iso store contains the history.
            try
            {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (isf.FileExists(CommonHistoryPath))
                    {
                        using (IsolatedStorageFileStream fs = isf.OpenFile(CommonHistoryPath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                            // Tries to deserialize the history.
                            DataContractSerializer serializer = new DataContractSerializer(typeof(History));
                            History history = (History)serializer.ReadObject(fs);
                            history.IsSyncedWithCache = true;
                            return history;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                // An exception should not happen, so log it.
                DebugUtils.DumpException(ex);
            }

            // At this point, we haven't been able to import the history
            // from the isostore. So create a new empty one and save it
            // to the isostore.
            History h = new History();
            h.ExportToCache();
            return h;
        }

        /// <summary>
        /// Exports the contents of the current history to the isolated
        /// storage.
        /// </summary>
        public void ExportToCache()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                // Makes sure the directory exists.
                isf.CreateDirectory(System.IO.Path.GetDirectoryName(this.IsoStoreHistoryPath));

                using (IsolatedStorageFileStream fs = isf.OpenFile(IsoStoreHistoryPath, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite))
                {
                    // Serializes.
                    DataContractSerializer serializer = new DataContractSerializer(typeof(History));
                    serializer.WriteObject(fs, this);
                }
            }
        }

        #endregion

        #region Add to History

        /// <summary>
        /// Adds a history entry for a game that has been restored.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="savegame"></param>
        public void AddRestoredGame(CartridgeTag tag, CartridgeSavegame savegame)
        {
            Add(new HistoryEntry(HistoryEntry.Type.Restored, tag, savegame));
        }

        /// <summary>
        /// Adds a history entry for a game that has been started.
        /// </summary>
        /// <param name="tag"></param>
        public void AddStartedGame(CartridgeTag tag)
        {
            Add(new HistoryEntry(HistoryEntry.Type.Started, tag));
        }

        /// <summary>
        /// Adds a history entry for a game that has been saved.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="savegame"></param>
        public void AddSavedGame(CartridgeTag tag, CartridgeSavegame savegame)
        {
            Add(new HistoryEntry(HistoryEntry.Type.Saved, tag, savegame));
        }

        /// <summary>
        /// Adds a history entry for a game that has been completed.
        /// </summary>
        /// <param name="tag"></param>
        public void AddCompletedGame(CartridgeTag tag)
        {
            Add(new HistoryEntry(HistoryEntry.Type.Completed, tag));
        }

        #endregion

        #region Remove from History

        /// <summary>
        /// Removes all entries related to a specific cartridge GUID
        /// from the history.
        /// </summary>
        /// <param name="cartrigeGuid"></param>
        public void RemoveAllOf(string cartrigeGuid)
        {
            // Disables sync.
            IsSyncedWithCache = false;

            // Removes all related entries.
            List<HistoryEntry> removedItems = this.Where(he => he.RelatedCartridgeGuid == cartrigeGuid).ToList();
            foreach (var entry in removedItems)
            {
                this.Remove(entry);
            }

            // Exports to cache.
            ExportToCache();

            // Reenables sync.
            IsSyncedWithCache = true;
        }

        #endregion

        #region Collection Overrides

        protected override void ClearItems()
        {
            base.ClearItems();

            if (IsSyncedWithCache)
            {
                ExportToCache();
            }
        }

        protected override void InsertItem(int index, HistoryEntry item)
        {
            base.InsertItem(index, item);

            if (IsSyncedWithCache)
            {
                ExportToCache();
            }
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);

            if (IsSyncedWithCache)
            {
                ExportToCache();
            }
        }

        protected override void SetItem(int index, HistoryEntry item)
        {
            base.SetItem(index, item);

            if (IsSyncedWithCache)
            {
                ExportToCache();
            }
        }

        #endregion
    }
}
