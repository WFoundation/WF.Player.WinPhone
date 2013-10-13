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

namespace Geowigo.Models
{
	/// <summary>
	/// Provides a static metadata description of a Cartridge.
	/// </summary>
	public class CartridgeTag
	{
		#region Constants

		public const string GlobalCachePath = "/Cache";

		public const int SmallThumbnailMinWidth = 173;
		public const int BigThumbnailMinWidth = 432;

		#endregion
		
		#region Properties

		public string PathToCache { get; private set; }

		public string Guid { get; private set; }

		public string Title { get; private set; }

		public Cartridge Cartridge { get; private set; }

		public ImageSource Thumbnail { get; private set; }

		public ImageSource Poster { get; private set; }

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

			// Cache
			MakeCache();
		}

		private void MakeCache()
		{
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				// Ensures the cache folder exists.
				isf.CreateDirectory(PathToCache);

				// Thumbnail
				Thumbnail = ImageUtils.SaveThumbnail(isf, GetCachePath("thumb.jpg"), Cartridge.Icon, Cartridge.Poster, SmallThumbnailMinWidth);

				// Poster
				Poster = ImageUtils.SaveThumbnail(isf, GetCachePath("poster.jpg"), Cartridge.Poster, null, BigThumbnailMinWidth);
			}
		}

		private string GetCachePath(string filename)
		{
			return PathToCache + "/" + filename;
		}
	}
}
