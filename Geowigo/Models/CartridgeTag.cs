using System;
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
using System.IO;
using System.Windows.Media.Imaging;
using Geowigo.Utils;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace Geowigo.Models
{
	/// <summary>
	/// Provides a static metadata description and cache of a Cartridge.
	/// </summary>
	public class CartridgeTag : INotifyPropertyChanged
	{
		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
		
		#region Constants

		public const string GlobalCachePath = "/Cache";
        public const string GlobalSavegamePath = "/Savegames";

		public const int SmallThumbnailMinWidth = 173;
		public const int BigThumbnailMinWidth = 432;

		private const string ThumbCacheFilename = "thumb.jpg";
		private const string PosterCacheFilename = "poster.jpg";

		#endregion

		#region Fields

		private ImageSource _thumbnail;
		private ImageSource _poster;
		private Dictionary<int, string> _soundFiles;
        private List<CartridgeSavegame> _savegames;

		#endregion
		
		#region Properties

		/// <summary>
		/// Gets the path to the cache folder for the Cartridge.
		/// </summary>
		public string PathToCache { get; private set; }

        /// <summary>
        /// Gets the path to the savegames folder for the Catridge.
        /// </summary>
        public string PathToSavegames { get; private set; }

		/// <summary>
		/// Gets the unique identifier for the Cartridge.
		/// </summary>
		public string Guid { get; private set; }

		/// <summary>
		/// Gets the title of the Cartridge.
		/// </summary>
		public string Title { get; private set; }

		/// <summary>
		/// Gets the Cartridge object.
		/// </summary>
		public Cartridge Cartridge { get; private set; }

		/// <summary>
		/// Gets the cached thumbnail icon for the Cartridge.
		/// </summary>
		public ImageSource Thumbnail
		{
			get
			{
				return _thumbnail;
			}

			private set
			{
				if (_thumbnail != value)
				{
					_thumbnail = value;

					RaisePropertyChanged("Thumbnail");
				}
			}
		} 

		/// <summary>
		/// Gets the cached poster image for the Cartridge.
		/// </summary>
		public ImageSource Poster
		{
			get
			{
				return _poster;
			}

			private set
			{
				if (_poster != value)
				{
					_poster = value;

					RaisePropertyChanged("Poster");
				}
			}
		}

		/// <summary>
		/// Gets the cached filenames of sounds of this Cartridge.
		/// </summary>
		public IDictionary<int, string> Sounds
		{
			get
			{
				return _soundFiles;
			}
		}

        /// <summary>
        /// Gets the available savegames for the cartridge.
        /// </summary>
        public IEnumerable<CartridgeSavegame> Savegames
        {
            get
            {
                return _savegames;
            }
        }

		#endregion

		#region Constructors
		/// <summary>
		/// Constructs an uncached CartridgeTag from the basic metadata of a Cartridge.
		/// </summary>
		/// <param name="cart"></param>
		public CartridgeTag(Cartridge cart)
		{
			if (cart == null)
			{
				throw new ArgumentNullException("cart");
			}

			// Basic metadata.
			Cartridge = cart;
			Guid = cart.Guid;
			Title = cart.Name;
			PathToCache = GlobalCachePath + "/" + Guid;
            PathToSavegames = String.Format("{0}/{1}_{2}",
                GlobalSavegamePath,
                Guid.Substring(0, 4),
                System.IO.Path.GetFileNameWithoutExtension(Cartridge.Filename)
            );
		}
		#endregion

		#region Cache
		/// <summary>
		/// Imports or create the cache for this CartridgeTag.
		/// </summary>
		public void ImportOrMakeCache()
		{            
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{				
				// Ensures the cache folder exists.
				isf.CreateDirectory(PathToCache);
                isf.CreateDirectory(PathToSavegames);

				// Thumbnail
				string thumbCachePath = GetCachePathCore(ThumbCacheFilename);
				if (isf.FileExists(thumbCachePath))
				{
					Thumbnail = ImageUtils.GetBitmapSource(thumbCachePath, isf);
				}
				else
				{
					Thumbnail = ImageUtils.SaveThumbnail(isf, thumbCachePath, Cartridge.Icon, Cartridge.Poster, SmallThumbnailMinWidth);
				}

				// Poster
				string posterCachePath = GetCachePathCore(PosterCacheFilename);
				if (isf.FileExists(posterCachePath))
				{
					Poster = ImageUtils.GetBitmapSource(posterCachePath, isf);
				}
				else
				{
					Poster = ImageUtils.SaveThumbnail(isf, posterCachePath, Cartridge.Poster, null, BigThumbnailMinWidth);
				}

				// Sounds
				ImportOrMakeSoundsCache(isf);

                // Savegames
                ImportSavegamesCache(isf);

				//// TEMP
				//BackgroundWorker bw = new BackgroundWorker();
				//bw.DoWork += new DoWorkEventHandler(bw_DoWork);
				//bw.RunWorkerAsync();
			}
		}

		//void bw_DoWork(object sender, DoWorkEventArgs e)
		//{
		//    using (WF.Player.Core.Engines.Engine engine = WherigoHelper.CreateEngine())
		//    {
		//        //engine.Init(Cartridge);
		//    }
		//}

		/// <summary>
		/// Gets the path to the cached version of a media.
		/// </summary>
		/// <param name="media"></param>
		/// <param name="recacheIfFileNotFound">If true, the cache for this media
		/// is recreated if its theoretical file path was not found. If false,
		/// null is returned if </param>
		/// <returns>The isostore path of the media if it is cached, null otherwise.</returns>
		public string GetMediaCachePath(Media media, bool recacheIfFileNotFound)
		{
			// Looks the file up in the registered sounds.
			// If not found, gets the theoretical value instead.
			string filename;
			if (!_soundFiles.TryGetValue(media.MediaId, out filename))
			{
				filename = GetCachePathCore(media);
			}

			// Recreates the file if it doesn't exist.
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				if (!isf.FileExists(filename))
				{
					// Makes sure the directory exists.
					isf.CreateDirectory(System.IO.Path.GetDirectoryName(filename));

					// Writes the contents of the media.
					using (IsolatedStorageFileStream fs = isf.CreateFile(filename))
					{
						fs.Write(media.Data, 0, media.Data.Length);
					}
				}
			}

			// Updates the path in the sounds dictionary.
			_soundFiles[media.MediaId] = filename;

			return filename;
		}

        #endregion

        #region Savegames

        /// <summary>
        /// Exports a savegame to the isolated storage and adds it to this tag.
        /// </summary>
        /// <param name="cs">The savegame to add.</param>
        public void AddSavegame(CartridgeSavegame cs)
        {
            // Sanity check: a cartridge with similar name should
            // not exist.
            if (Savegames.Any(c => c.Name == cs.Name))
            {
                throw new InvalidOperationException("A savegame with the same name already exists for this savegame.");
            }
            
            // Makes sure the savegame is exported to the cache.
            cs.ExportToIsoStore();
            
            // Adds the savegame.
            _savegames.Add(cs);

            // Notifies of a change.
            RaisePropertyChanged("Savegames");
        }

        /// <summary>
        /// Removes a savegame's contents from the isolated storage and removes
        /// it from this tag.
        /// </summary>
        public void RemoveSavegame(CartridgeSavegame cs)
        {
            // Removes the savegame.
            _savegames.Remove(cs);

            // Makes sure the savegame is cleared from cache.
            cs.RemoveFromIsoStore();

            // Notifies of a change.
            RaisePropertyChanged("Savegames");
        }

        /// <summary>
        /// Gets a savegame of this cartridge by name, or null if none
        /// is found.
        /// </summary>
        /// <param name="name">Name of the savegame to find.</param>
        /// <returns>The savegame, or null if it wasn't found.</returns>
        public CartridgeSavegame GetSavegameByNameOrDefault(string name)
        {
            return Savegames.SingleOrDefault(cs => cs.Name == name);
        }

		#endregion

		#region Private Methods

		private string GetCachePathCore(string filename)
		{
			return PathToCache + "/" + filename;
		}

		private string GetCachePathCore(Media media)
		{
			return GetCachePathCore(String.Format("{0}.{1}", media.MediaId, media.Type.ToString()));
		}

		private void ImportOrMakeSoundsCache(IsolatedStorageFile isf)
		{
			_soundFiles = new Dictionary<int, string>();

			foreach (var sound in Cartridge.Resources.Where(m => m.Type == MediaType.MP3 || m.Type == MediaType.WAV))
			{
				// Copies the sound file to the cache if it doesn't exist already.
				string cacheFilename = GetCachePathCore(sound);
				if (!isf.FileExists(cacheFilename))
				{
					using (IsolatedStorageFileStream fs = isf.CreateFile(cacheFilename))
					{
						fs.Write(sound.Data, 0, sound.Data.Length);
					}
				}

				// Adds the sound filename to the dictionary.
				_soundFiles.Add(sound.MediaId, cacheFilename);
			}

			RaisePropertyChanged("Sounds");
		}

        private void ImportSavegamesCache(IsolatedStorageFile isf)
        {
            List<CartridgeSavegame> cSavegames = new List<CartridgeSavegame>();
            
            string[] gwsFiles = isf.GetFileNames(PathToSavegames + "/*.gws");
            if (gwsFiles != null)
            {
                // For each file, imports its metadata.
                foreach (string file in gwsFiles)
                {
                    try
                    {
                        cSavegames.Add(CartridgeSavegame.FromIsoStore(PathToSavegames + "/" + file, isf));
                    }
                    catch (Exception ex)
                    {
                        // Outputs the exception.
                        System.Diagnostics.Debug.WriteLine("CartridgeTag: WARNING: Exception during savegame import.");
                        DebugUtils.DumpException(ex);
                    }
                }
            }

            // Sets the savegame list.
            _savegames = cSavegames;
            RaisePropertyChanged("Savegames");
        }

		private void RaisePropertyChanged(string propName)
		{
			Deployment.Current.Dispatcher.BeginInvoke(() =>
			{
				if (PropertyChanged != null)
				{
					PropertyChanged(this, new PropertyChangedEventArgs(propName));
				}
			});
		} 

		#endregion


    }
}
