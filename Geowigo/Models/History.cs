using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.IO.IsolatedStorage;
using Geowigo.Utils;

namespace Geowigo.Models
{
    [CollectionDataContract]
    public class History : List<HistoryEntry>
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

        #endregion

        #region Members

        private static readonly string CommonHistoryPath = "/History/userhistory.txt";

        #endregion
        
        #region Constructors

        private History()
            : base(new List<HistoryEntry>())
        {
            
        }

        #endregion

        #region Methods

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
                            return (History)serializer.ReadObject(fs);
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
        private void ExportToCache()
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
    }
}
