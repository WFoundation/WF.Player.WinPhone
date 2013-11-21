using System;
using System.Net;
using System.Windows.Media.Imaging;
using System.IO;
using WF.Player.Core;
using System.Windows.Media;
using System.IO.IsolatedStorage;
using ImageTools;
using ImageTools.IO;
using ImageTools.IO.Gif;
using System.Threading;
using Geowigo.Utils;
using System.Windows;

namespace Geowigo.Utils
{
	public class ImageUtils
	{
		static ImageUtils()
		{
			// Adds support for GIF.
			Decoders.AddDecoder<GifDecoder>();
		}

		public static BitmapSource GetBitmapSource(ExtendedImage image)
		{
			// Waits for the bitmap to be loaded if it needs so.
			if (!image.IsFilled)
			{
				if (!image.IsLoading)
				{
					return null;
				}

				ManualResetEvent resetEvent = new ManualResetEvent(false);

				EventHandler onLoaded = new EventHandler((o, e) =>
				{
					resetEvent.Set();
				});

				image.LoadingCompleted += onLoaded;

				resetEvent.WaitOne(1000);

				image.LoadingCompleted -= onLoaded;
			}


			// Converts the bitmap.
			// Source code from ImageTools.Utils.ImageExtensions.ToBitmap();
			WriteableBitmap bitmap = new WriteableBitmap(image.PixelWidth, image.PixelHeight);

			ImageBase temp = image;

			byte[] pixels = temp.Pixels;

			if (pixels != null)
			{
				int[] raster = bitmap.Pixels;

				if (raster != null)
				{
					Buffer.BlockCopy(pixels, 0, raster, 0, pixels.Length);

					for (int i = 0; i < raster.Length; i++)
					{
						int abgr = raster[i];
						int a = (abgr >> 24) & 0xff;

						float m = a / 255f;

						int argb = a << 24 |
							(int)(((abgr >> 0) & 0xff) * m) << 16 |
							(int)(((abgr >> 8) & 0xff) * m) << 8 |
							(int)(((abgr >> 16) & 0xff) * m);
						raster[i] = argb;
					}
				}
			}

			bitmap.Invalidate();

			return bitmap;  
		}

		public static BitmapSource GetBitmapSource(byte[] data)
		{
			using (MemoryStream ms = new MemoryStream(data))
			{
				try
				{
					// Tries to load the image using Windows Phone's internals.
					BitmapImage image = null;
					image = new BitmapImage();
					image.SetSource(ms);

					return image;
				}
				catch (Exception)
				{
					try
					{
						// It didn't work. Now try with ImageTools.
						ExtendedImage eim = new ExtendedImage();
						eim.SetSource(ms);

						return GetBitmapSource(eim);
					}
					catch (Exception e)
					{
						// Dump data!
						DebugUtils.DumpData(data, e);
						return null;
					}
				}
			}
		}

		public static BitmapSource GetBitmapSource(Media media)
		{
			if (media == null || media.Data == null)
			{
				return null;
			}

			return GetBitmapSource(media.Data);
		}

		public static BitmapSource GetBitmapSource(string filename, IsolatedStorageFile isoStore)
		{
			// Make sure this method runs in the UI thread.
			if (!Deployment.Current.Dispatcher.CheckAccess())
			{
				return Deployment.Current.Dispatcher.Invoke<BitmapSource>(() =>
				{
					return GetBitmapSource(filename, isoStore);
				});
			}
			
			BitmapImage image = null;

			using (IsolatedStorageFileStream stream = isoStore.OpenFile(filename, FileMode.Open, FileAccess.Read))
			{
				image = new BitmapImage();
				image.SetSource(stream);
			}

			return image;
		}

		public static ImageSource SaveThumbnail(IsolatedStorageFile isoStore, string filename, Media prefered, Media fallback = null, int minWidth = -1)
		{
			// Make sure this method runs in the UI thread.
			if (!Deployment.Current.Dispatcher.CheckAccess())
			{
				return Deployment.Current.Dispatcher.Invoke<ImageSource>(() => 
				{
					return SaveThumbnail(isoStore, filename, prefered, fallback, minWidth);
				});
			}
			
			// Gets the images.
			BitmapSource preferedImage = GetBitmapSource(prefered);
			BitmapSource fallbackImage = GetBitmapSource(fallback);

			// Determines which image needs to be saved.
			BitmapSource targetImage = preferedImage;
			if (preferedImage == null)
			{
				// Use the fallback image if the prefered image does not exist.
				targetImage = fallbackImage;
			}
			else if (fallbackImage != null && minWidth > -1 && preferedImage.PixelWidth < minWidth && preferedImage.PixelWidth < fallbackImage.PixelWidth)
			{
				// Use the fallback image if its width is bigger than the prefered image's width, the latter being
				// smaller than the min width.
				targetImage = fallbackImage;
			}

			// No image? Return.
			if (targetImage == null)
			{
				return null; 
			}

			// Gets the dimensions of the target image.
			int targetWidth = (int) Math.Max(minWidth, targetImage.PixelWidth);
			double sourcePixelRatio = targetImage.PixelWidth / (double) targetImage.PixelHeight;
			int targetHeight = (int) Math.Floor(targetWidth / sourcePixelRatio);
			
			// Saves the image.
			using (IsolatedStorageFileStream stream = isoStore.OpenFile(filename, FileMode.Create))
			{
				new WriteableBitmap(targetImage).SaveJpeg(stream, targetWidth, targetHeight, 0, 100);
			}

			// Returns the image.
			return targetImage;
		}


	}
}
