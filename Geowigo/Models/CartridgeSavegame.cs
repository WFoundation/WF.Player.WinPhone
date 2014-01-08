using System;
using WF.Player.Core;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;

namespace Geowigo.Models
{
    /// <summary>
    /// Provides a static metadata description of a Cartridge savegame.
    /// </summary>
    [DataContract]
    public class CartridgeSavegame
    {
        #region Constants

        private const int GeneratedModelVersion = 2;

        #endregion
        
        #region Properties

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        [DataMember]
        public bool IsAutosave { get; set; }

        public string SavegameFile { get; private set; }

        public string MetadataFile { get; private set; }

        [DataMember]
        public int ModelVersion { get; set; }

        #endregion

        #region Constructors

        internal CartridgeSavegame()
        {

        }

        /// <summary>
        /// Constructs a new savegame metadata container for a Cartridge.
        /// </summary>
        /// <param name="cartridge">Cartridge to save.</param>
        public CartridgeSavegame(CartridgeTag tag)
        {
            ModelVersion = GeneratedModelVersion;
            Timestamp = DateTime.Now;
            Name = GetDefaultName(tag);
            SetFileProperties(tag);
        }

        #endregion
        

        /// <summary>
        /// Imports a savegame from metadata associated to a savegame
        /// file.
        /// </summary>
        /// <param name="gwsFilePath">Path to the GWS savegame file.</param>
        /// <param name="isf">Isostore file to use to load.</param>
        /// <returns>The cartridge savegame.</returns>
        /// 
        public static CartridgeSavegame FromIsoStore(string gwsFilePath, IsolatedStorageFile isf)
        {
            // Checks that the metadata file exists.
            string mdFile = gwsFilePath + ".mf";
            if (!(isf.FileExists(mdFile)))
            {
                throw new System.IO.FileNotFoundException(mdFile + " does not exist.");
            }
            
            // Creates a serializer.
            DataContractSerializer serializer = new DataContractSerializer(typeof(CartridgeSavegame));

            // Reads the object.
            CartridgeSavegame cs;
            using (IsolatedStorageFileStream fs = isf.OpenFile(mdFile, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                // Reads the object.
                object o = serializer.ReadObject(fs);

                // Casts it.
                cs = (CartridgeSavegame)o;
            }

            // Adds non-serialized content.
            cs.SavegameFile = gwsFilePath;
            cs.MetadataFile = mdFile;

            // Returns it.
            return cs;
        }

        /// <summary>
        /// Creates or replaces the underlying savegame file and opens a writing
        /// stream for it.
        /// </summary>
        /// <param name="isf">Isolated storage to use.</param>
        /// <returns>The stream to write the savegame on.</returns>
        public System.IO.Stream CreateOrReplace(IsolatedStorageFile isf)
        {
            // Makes sure the containing directory exists.
            isf.CreateDirectory(System.IO.Path.GetDirectoryName(SavegameFile));
            
            // Returns the stream to SavegameFile.
            return isf.OpenFile(SavegameFile, System.IO.FileMode.Create, System.IO.FileAccess.Write);
        }

        /// <summary>
        /// Exports the savegame to the isolated storage.
        /// </summary>
        public void ExportToIsoStore()
        {
            // Creates a serializer.
            DataContractSerializer serializer = new DataContractSerializer(typeof(CartridgeSavegame));
            
            // Writes the object.
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream fs = isf.OpenFile(MetadataFile, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite))
                {
                    serializer.WriteObject(fs, this);
                }
            }
        }

        /// <summary>
        /// Removes this savegame's files from the isolated storage.
        /// </summary>
        public void RemoveFromIsoStore()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                // Removes the gws if it exists.
                if (isf.FileExists(SavegameFile))
                {
                    isf.DeleteFile(SavegameFile);
                }

                // Removes the metadata file if it exists.
                if (isf.FileExists(MetadataFile))
                {
                    isf.DeleteFile(MetadataFile);
                }
            }
        }

        private string GetDefaultName(CartridgeTag tag)
        {
            return String.Format("MySavegame{0}{1}{2}{3}",
                Timestamp.DayOfYear,
                Timestamp.Hour,
                Timestamp.Minute,
                Timestamp.Second);
        }

        private void SetFileProperties(CartridgeTag tag)
        {
            string fname = System.IO.Path.GetFileNameWithoutExtension(tag.Cartridge.Filename);
            SavegameFile = String.Format("/{0}/{1}_{2}.gws",
                tag.PathToSavegames,
                Name,
                fname
            );
            MetadataFile = SavegameFile + ".mf";
        }
    }
}
