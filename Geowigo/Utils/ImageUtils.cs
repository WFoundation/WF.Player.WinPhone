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
        private static ImageTools.Filtering.GaussianBlur _gaussianBlurFilter;
        
        static ImageUtils()
		{
			// Adds support for GIF.
			Decoders.AddDecoder<GifDecoder>();
		}

		public static WriteableBitmap GetBitmapSource(ExtendedImage image)
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
            return image.ToBitmap();
		}

		public static WriteableBitmap GetBitmapSource(byte[] data)
		{
			using (MemoryStream ms = new MemoryStream(data))
			{
				try
				{
					// Tries to load the image using Windows Phone's internals.
					BitmapImage image = null;
					image = new BitmapImage();
					image.SetSource(ms);

					return new WriteableBitmap(image);
				}
				catch (Exception)
				{
					try
					{
						// Resets the memory stream's position.
                        ms.Position = 0;

                        // It didn't work. Now try with ImageTools.
						ExtendedImage eim = new ExtendedImage();
						eim.SetSource(ms);

                        // Gets the image source for the image.
						WriteableBitmap bs = GetBitmapSource(eim);
                        if (bs == null)
                        {
                            // Something went wrong.
                            DebugUtils.DumpData(data, "ImageTools failed to generate an image source.");
                        }
                        return bs;
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

		public static WriteableBitmap GetBitmapSource(Media media)
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

			try
			{
				using (IsolatedStorageFileStream stream = isoStore.OpenFile(filename, FileMode.Open))
				{
					image = new BitmapImage();
					image.SetSource(stream);
				}
			}
			catch (Exception)
			{
				// The exception is ignored, and null is returned.
				image = null;
			}

			return image;
		}

		public static ImageSource SaveThumbnail(IsolatedStorageFile isoStore, string filename, Media prefered, Media fallback = null, int minWidth = -1, bool blur = false)
		{
			// Make sure this method runs in the UI thread.
			if (!Deployment.Current.Dispatcher.CheckAccess())
			{
				return Deployment.Current.Dispatcher.Invoke<ImageSource>(() => 
				{
					return SaveThumbnail(isoStore, filename, prefered, fallback, minWidth, blur);
				});
			}
			
			// Gets the images.
			WriteableBitmap preferedImage = GetBitmapSource(prefered);
            WriteableBitmap fallbackImage = GetBitmapSource(fallback);

			// Determines which image needs to be saved.
            WriteableBitmap targetImage = preferedImage;
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
            
            // Blurs the image if needed.
            if (blur)
            {
                // Converts the image to an ImageTools image.
                ExtendedImage targetExtendedImage = targetImage.ToImage();

                // Resizes the image if it can help decreasing the time needed to blur it.
                // The image is only resized if the target size is less than the original size.
                // Otherwise it means that the original image is smaller than the target size, in which case we blur it
                // as it is and let WP scale up the blurred image later.
                if (targetExtendedImage.PixelHeight * targetExtendedImage.PixelWidth > targetWidth * targetHeight)
                {
                    targetExtendedImage = ExtendedImage.Resize(targetExtendedImage, targetWidth, targetHeight, new ImageTools.Filtering.NearestNeighborResizer());  
                }
                
                // Inits the blur filter if needed and runs it.
                if (_gaussianBlurFilter == null)
                {
                    _gaussianBlurFilter = new ImageTools.Filtering.GaussianBlur() { Variance = 2d };
                }
                targetExtendedImage = ExtendedImage.Apply(targetExtendedImage, _gaussianBlurFilter);

                // Converts the image back to a WP-displayable image.
                targetImage = targetExtendedImage.ToBitmap();
            }

			// Saves the image.
			try
			{
				using (IsolatedStorageFileStream stream = isoStore.OpenFile(filename, FileMode.Create, FileAccess.ReadWrite))
				{
                    targetImage.SaveJpeg(stream, targetWidth, targetHeight, 0, 100);
				}
			}
			catch (ArgumentException ex)
			{
				// Nothing to do, let's just dump this for further analysis.
				DebugUtils.DumpException(ex, string.Format("SaveJpeg(w:{0},h:{1})", targetWidth, targetHeight), true);
			}

			// Returns the image.
			return targetImage;
		}

        public static string ToBase64String(BitmapSource bitmapSource, int width, int height)
        {
            // Encodes the image to JPEG and writes it to a byte array.
            byte[] imageData;
            using (MemoryStream ms = new MemoryStream())
            {
                WriteableBitmap wb = new WriteableBitmap(bitmapSource);
                wb.SaveJpeg(ms, width, height, 0, 100);
                imageData = ms.ToArray();
            }

            // Converts the data to a base64 string.
            return Convert.ToBase64String(imageData);
        }
    }
}
