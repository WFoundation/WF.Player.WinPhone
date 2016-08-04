using System;
using WF.Player.Core;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Linq;
using Geowigo.Utils;

namespace Geowigo.Models
{
    /// <summary>
    /// Provides a static metadata description of a Cartridge savegame.
    /// </summary>
    [DataContract]
    public class CartridgeSavegame
    {
        
        #region Properties

        /// <summary>
        /// Gets or sets the unique name of this savegame.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets when this savegame has been made.
        /// </summary>
        [DataMember]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets if this savegame has been made automatically.
        /// </summary>
        [DataMember]
        public bool IsAutosave { get; set; }

        /// <summary>
        /// Gets or sets if this savegame is a quick save.
        /// </summary>
        [DataMember]
        public bool IsQuicksave { get; set; }

        /// <summary>
        /// Gets or sets the hash color representing this savegame.
        /// </summary>
        [DataMember]
        public Color HashColor { get; set; }

        /// <summary>
        /// Gets the file path of the savegame.
        /// </summary>
        public string SavegameFile { get; private set; }

        /// <summary>
        /// Gets the file path of the metadata file.
        /// </summary>
        public string MetadataFile { get; private set; }

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
            Timestamp = DateTime.Now;
            Name = GetDefaultName(tag);
            HashColor = GetHashColor(Name);
            SetFileProperties(tag);
        }

        /// <summary>
        /// Constructs a new savegame metadata container for a Cartridge and a name.
        /// </summary>
        /// <param name="cartridge">Cartridge to save.</param>
        /// <param name="name"></param>
        public CartridgeSavegame(CartridgeTag tag, string name)
        {
            Timestamp = DateTime.Now;
            Name = name;
            HashColor = GetHashColor(Name);
            SetFileProperties(tag);
        }

		/// <summary>
		/// Constructs a new savegame metadata container for a Cartridge, using metadata
		/// from a GWS metadata container.
		/// </summary>
		/// <param name="tag">Cartridge to save.</param>
		/// <param name="gwsMetadata">Metadata of a GWS file.</param>
		public CartridgeSavegame(CartridgeTag tag, WF.Player.Core.Formats.GWS.Metadata gwsMetadata, string gwsFilename)
		{
			Timestamp = gwsMetadata.SaveCreateDate;
			Name = gwsMetadata.SaveName;
			HashColor = GetHashColor(Name);
			SetFileProperties(tag, gwsFilename);
		}

        #endregion

        /// <summary>
        /// Computes the color brush hash that corresponds to a particular
        /// name for a savegame.
        /// </summary>
        /// <param name="name">Name of a savegame.</param>
        /// <returns>The hash color brush corresponding to the name.</returns>
        public static Color GetHashColor(string name)
        {
            // Gets bytes from the hash of the name.
            byte[] bytes = BitConverter.GetBytes(name.GetHashCode());

            // Computes a color from each byte.
            return Color.FromArgb(255, bytes[1], bytes[2], bytes[3]);
        }

        /// <summary>
        /// Imports a savegame from metadata associated to a savegame
        /// file.
        /// </summary>
        /// <param name="gwsFilePath">Path to the GWS savegame file.</param>
        /// <param name="isf">Isostore file to use to load.</param>
        /// <returns>The cartridge savegame.</returns>
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
        /// Changes the name of this savegame.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="name"></param>
        public void Rename(CartridgeTag tag, string name, IsolatedStorageFile isf)
        {
            string oldGwsFile = SavegameFile;
            string oldMdFile = MetadataFile;
            
            // Changes the properties.
            Name = name;
            SetFileProperties(tag);

            // Renames the files.
            if (isf.FileExists(oldGwsFile))
            {
                isf.MoveFile(oldGwsFile, SavegameFile);
            }
            if (isf.FileExists(oldMdFile))
            {
                isf.MoveFile(oldMdFile, MetadataFile);
            }
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
            // Gets a context-aware default name:
            // Proximity to the closest thing.
            Thing closestThing = null;
            try
            {
               closestThing = App.Current.Model.Core.VisibleThings
             .OrderBy(t => { if (t.VectorFromPlayer == null) { return -1; } else { return t.VectorFromPlayer.Distance.Value; } })
             .FirstOrDefault();
            }
            catch (Exception)
            {

            }

            if (closestThing != null)
            {
                string proximity = "By";

                if (closestThing is Zone)
                {
                    PlayerZoneState state = ((Zone)closestThing).State;
                    if (state == PlayerZoneState.Inside)
                    {
                        proximity = "Inside";
                    }
                    else if (state == PlayerZoneState.Proximity)
                    {
                        proximity = "Close to";
                    }
                    else if (state == PlayerZoneState.Distant)
                    {
                        proximity = "Away from";
                    }
                    else
                    {
                        proximity = null;
                    }
                }
                else if (closestThing is Character)
                {
                    proximity = "Close to";
                }

                if (proximity != null)
                {
                    return String.Format("{0} {1}", proximity, closestThing.Name); 
                }
            }

            // We've had no luck finding a closest thing.
            return String.Format("MySavegame{0}{1}{2}{3}",
                Timestamp.DayOfYear,
                Timestamp.Hour,
                Timestamp.Minute,
                Timestamp.Second);
        }

        private void SetFileProperties(CartridgeTag tag, string saveFilename = null)
        {
			if (saveFilename == null)
			{
				string fname = System.IO.Path.GetFileNameWithoutExtension(tag.Cartridge.Filename);
				SavegameFile = String.Format("{0}/{1}_{2}.gws",
					tag.PathToSavegames,
					Name.ReplaceInvalidFileNameChars(),
					fname.ReplaceInvalidFileNameChars()
				);
			}
			else
			{
				SavegameFile = String.Format("{0}/{1}", tag.PathToSavegames, saveFilename);
				if (!SavegameFile.EndsWith(".gws", StringComparison.InvariantCultureIgnoreCase))
				{
					SavegameFile += ".gws";
				}
			}
            MetadataFile = SavegameFile + ".mf";
        }
    }
}
