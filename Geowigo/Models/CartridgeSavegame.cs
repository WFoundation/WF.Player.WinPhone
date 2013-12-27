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

        private const int GeneratedModelVersion = 1;

        #endregion
        
        #region Properties

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

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
            Name = Timestamp.Ticks.ToString();
            SetFileProperties(tag.Cartridge.Filename, tag.Guid);
        }

        private void SetFileProperties(string cartFilename, string cartGuid)
        {
            string fname = System.IO.Path.GetFileNameWithoutExtension(cartFilename);
            SavegameFile = String.Format("/Savegames/{0}_{1}/{2}_{3}.gws",
                cartGuid.Substring(0, 4),
                fname,
                Name,
                fname
            );
            MetadataFile = SavegameFile + ".mf";
        }

        #endregion

        /// <summary>
        /// Imports a CartridgeSavegame from metadata associated to a savegame
        /// file.
        /// </summary>
        /// <param name="gwsFilePath">Path to the GWS savegame file.</param>
        /// <param name="isf">Isostore file to use to load.</param>
        /// <returns>The cartridge savegame.</returns>
        /// 
        public static CartridgeSavegame FromCache(string gwsFilePath, IsolatedStorageFile isf)
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
        /// Exports the cartridge metadata to the cache.
        /// </summary>
        public void ExportToCache()
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
    }
}
