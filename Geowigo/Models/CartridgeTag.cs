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

namespace Geowigo.Models
{
	/// <summary>
	/// Provides a static metadata description of a Cartridge.
	/// </summary>
	public class CartridgeTag : INotifyPropertyChanged
	{
		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
		
		#region Constants

		public const string GlobalCachePath = "/Cache";

		public const int SmallThumbnailMinWidth = 173;
		public const int BigThumbnailMinWidth = 432;

		private const string ThumbCacheFilename = "thumb.jpg";
		private const string PosterCacheFilename = "poster.jpg";

		#endregion
		
		#region Properties

		public string PathToCache { get; private set; }

		public string Guid { get; private set; }

		public string Title { get; private set; }

		public Cartridge Cartridge { get; private set; }

		#region Thumbnail
		private ImageSource _thumbnail;
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
		#endregion

		#region Poster
		private ImageSource _poster;
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
		#endregion

		#endregion
		
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
		}

		/// <summary>
		/// Imports or create the cache for this CartridgeTag.
		/// </summary>
		public void ImportOrMakeCache()
		{
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				// Ensures the cache folder exists.
				isf.CreateDirectory(PathToCache);

				// Thumbnail
				string thumbCachePath = GetCachePath(ThumbCacheFilename);
				if (isf.FileExists(thumbCachePath))
				{
					Thumbnail = ImageUtils.GetBitmapSource(thumbCachePath, isf);
				}
				else
				{
					Thumbnail = ImageUtils.SaveThumbnail(isf, thumbCachePath, Cartridge.Icon, Cartridge.Poster, SmallThumbnailMinWidth);
				}

				// Poster
				string posterCachePath = GetCachePath(PosterCacheFilename);
				if (isf.FileExists(posterCachePath))
				{
					Poster = ImageUtils.GetBitmapSource(posterCachePath, isf);
				}
				else
				{
					Poster = ImageUtils.SaveThumbnail(isf, GetCachePath("poster.jpg"), Cartridge.Poster, null, BigThumbnailMinWidth);
				}
			}
		}

		private string GetCachePath(string filename)
		{
			return PathToCache + "/" + filename;
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
		
	}
}
