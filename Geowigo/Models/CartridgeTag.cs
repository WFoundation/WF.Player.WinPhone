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
using WF.Player.Core.Formats;

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
        public const string GlobalSavegamePath = "/Savegames_and_Logs";

        public const int SmallThumbnailMinWidth = 32;
		public const int BigThumbnailMinWidth = 173;
		public const int PosterMinWidth = 432;
        public const int PanoramaMinWidth = 1024;
        public const int PanoramaCropHeight = 800;

		private const string ThumbCacheFilename = "thumb.jpg";
		private const string PosterCacheFilename = "poster.jpg";
        private const string IconCacheFilename = "icon.jpg";
        private const string PanoramaCacheFilename = "panorama.jpg";

		#endregion

		#region Fields

		private ImageSource _thumbnail;
		private ImageSource _poster;
        private ImageSource _icon;
        private ImageSource _panorama;
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
		/// Gets the path to the logs folder for the Cartridge.
		/// </summary>
		public string PathToLogs { get; private set; }

		/// <summary>
		/// Gets the unique identifier for the Cartridge.
		/// </summary>
		public string Guid { get; private set; }

		/// <summary>
		/// Gets the title of the Cartridge.
		/// </summary>
		public string Title { get; private set; }

		/// <summary>
		/// Gets the description of the Cartridge.
		/// </summary>
		public string Description { get; private set; }

		/// <summary>
		/// Gets the start location description of the Cartridge.
		/// </summary>
		public string StartingDescription { get; private set; }

		/// <summary>
		/// Gets the Cartridge object.
		/// </summary>
		public Cartridge Cartridge { get; private set; }

        /// <summary>
        /// Gets the cached small icon for the Cartridge.
        /// </summary>
        public ImageSource Icon
        {
            get
            {
                return _icon;
            }

            private set
            {
                if (_icon != value)
                {
                    _icon = value;

                    RaisePropertyChanged("Icon");
                }
            }
        } 

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
        /// Gets the cached panorama image for the Cartridge.
        /// </summary>
        public ImageSource Panorama
        {
            get
            {
                return _panorama;
            }

            private set
            {
                if (_panorama != value)
                {
                    _panorama = value;

                    RaisePropertyChanged("Panorama");
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
				return _soundFiles ?? (_soundFiles = new Dictionary<int,string>());
			}
		}

        /// <summary>
        /// Gets the available savegames for the cartridge.
        /// </summary>
        public IEnumerable<CartridgeSavegame> Savegames
        {
            get
            {
				return _savegames ?? (_savegames = new List<CartridgeSavegame>());
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
			Description = cart.LongDescription;
			StartingDescription = cart.StartingDescription;
			PathToCache = GlobalCachePath + "/" + Guid;
            PathToSavegames = String.Format("{0}/{1}_{2}",
                GlobalSavegamePath,
                Guid.Substring(0, 4),
                System.IO.Path.GetFileNameWithoutExtension(Cartridge.Filename)
            );
			PathToLogs = PathToSavegames;

			// Event handlers.
			//Cartridge.PropertyChanged += new PropertyChangedEventHandler(OnCartridgePropertyChanged);
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
					Thumbnail = ImageUtils.SaveThumbnail(new ImageUtils.ThumbnailOptions(isf, thumbCachePath, Cartridge.Icon, Cartridge.Poster, BigThumbnailMinWidth));
				}

                // Icon
                string iconCachePath = GetCachePathCore(IconCacheFilename);
                if (isf.FileExists(iconCachePath))
                {
                    Icon = ImageUtils.GetBitmapSource(iconCachePath, isf);
                }
                else
                {
                    Icon = ImageUtils.SaveThumbnail(new ImageUtils.ThumbnailOptions(isf, iconCachePath, Cartridge.Icon, Cartridge.Poster, SmallThumbnailMinWidth));
                }

                // Poster
                string posterCachePath = GetCachePathCore(PosterCacheFilename);
                if (isf.FileExists(posterCachePath))
                {
                    Poster = ImageUtils.GetBitmapSource(posterCachePath, isf);
                }
                else
                {
                    Poster = ImageUtils.SaveThumbnail(new ImageUtils.ThumbnailOptions(isf, posterCachePath, Cartridge.Poster, null, PosterMinWidth, true));
                }

                // Panorama
                string panoramaCachePath = GetCachePathCore(PanoramaCacheFilename);
                if (isf.FileExists(panoramaCachePath))
                {
                    Panorama = ImageUtils.GetBitmapSource(panoramaCachePath, isf);
                }
                else
                {
                    Panorama = ImageUtils.SaveThumbnail(new ImageUtils.ThumbnailOptions(isf, panoramaCachePath, Cartridge.Poster, null, PanoramaMinWidth, true, PanoramaCropHeight));
                }

				// Sounds
				ImportOrMakeSoundsCache(isf);

                // Savegames
                ImportSavegamesCache(isf);
			}
		}

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
					using (IsolatedStorageFileStream fs = isf.OpenFile(filename, FileMode.Create, FileAccess.Write))
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
            // Sanity check: a savegame with similar name should
            // not exist.
			CartridgeSavegame sameNameCS;
            if ((sameNameCS = Savegames.SingleOrDefault(c => c.Name == cs.Name)) != null)
            {
				System.Diagnostics.Debug.WriteLine("CartridgeTag: Removing savegame to make room for new one: " + sameNameCS.Name);
				
				// Removes the previous savegame that bears the same name.
				RemoveSavegame(sameNameCS);
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

		#region Cache (Core)

		private string GetCachePathCore(string filename)
		{
			return PathToCache + "/" + filename;
		}

		private string GetCachePathCore(Media media)
		{
			// FDL files are converted to WAV files.
			string extension = media.Type == MediaType.FDL ? "WAV" : media.Type.ToString().ToUpper();

			return GetCachePathCore(String.Format("{0}.{1}", media.MediaId, extension));
		}

		private void ImportOrMakeSoundsCache(IsolatedStorageFile isf)
		{
			_soundFiles = new Dictionary<int, string>();

			foreach (var sound in Cartridge.Resources.Where(m => ViewModels.SoundManager.IsPlayableSound(m)))
			{
				// Copies the sound file to the cache if it doesn't exist already.
				string cacheFilename = GetCachePathCore(sound);
				if (!isf.FileExists(cacheFilename))
				{
					using (IsolatedStorageFileStream fs = isf.CreateFile(cacheFilename))
					{
						if (sound.Type == MediaType.FDL)
						{
							// Converts the FDL to WAV and writes it.
							ConvertAndWriteFDL(fs, sound);
						}
						else
						{
							// Dumps the bytes out!
							fs.Write(sound.Data, 0, sound.Data.Length);
						}
					}
				}

				// Adds the sound filename to the dictionary.
				_soundFiles.Add(sound.MediaId, cacheFilename);
			}

			RaisePropertyChanged("Sounds");
		}

		private void ConvertAndWriteFDL(IsolatedStorageFileStream fs, Media sound)
		{
			using (MemoryStream inputFdlStream = new MemoryStream(sound.Data))
			{
				using (Stream outputWavStream = new WF.Player.Core.Formats.FDL().ConvertToWav(inputFdlStream))
				{
					outputWavStream.CopyTo(fs);
				}
			}
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

		#endregion

		#region Logs

		/// <summary>
		/// Creates a new log file for this cartridge tag.
		/// </summary>
		/// <returns></returns>
		public GWL CreateLogFile()
		{
			// Creates a file in the logs folder.
			string filename = String.Format("/{0}/{1:yyyyMMddhhmmss}_{2}.gwl",
				PathToLogs,
				DateTime.Now.ToLocalTime(),
				System.IO.Path.GetFileNameWithoutExtension(Cartridge.Filename));
			IsolatedStorageFileStream fs = IsolatedStorageFile
				.GetUserStoreForApplication()
				.OpenFile(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);

			// Creates a logger for this file.
			return new GWL(fs);
		}

		#endregion

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
    }
}
