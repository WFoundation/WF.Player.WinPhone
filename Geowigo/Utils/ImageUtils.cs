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
        public class ThumbnailOptions
        {
            public ThumbnailOptions(IsolatedStorageFile isf, string filename, Media preferred, Media fallback, int minWidth)
            {
                IsoStoreFile = isf;
                Filename = filename;
                PreferedMedia = preferred;
                FallbackMedia = fallback;
                MinWidth = minWidth;
            }

            public ThumbnailOptions(IsolatedStorageFile isf, string filename, Media preferred, Media fallback, int minWidth, bool blur)
            {
                IsoStoreFile = isf;
                Filename = filename;
                PreferedMedia = preferred;
                FallbackMedia = fallback;
                MinWidth = minWidth;
                Blur = blur;
            }

            public ThumbnailOptions(IsolatedStorageFile isf, string filename, Media preferred, Media fallback, int minWidth, bool blur, int heightToCrop)
            {
                IsoStoreFile = isf;
                Filename = filename;
                PreferedMedia = preferred;
                FallbackMedia = fallback;
                MinWidth = minWidth;
                Blur = blur;
                CropRectangle = new Rectangle(0, 0, minWidth, heightToCrop);
            }

            public Media PreferedMedia { get; set; }

            public Media FallbackMedia { get; set; }

            public int MinWidth { get; set; }

            public bool Blur { get; set; }

            public string Filename { get; set; }

            public IsolatedStorageFile IsoStoreFile { get; set; }

            public Rectangle? CropRectangle { get; set; }
        }
        
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

        //public static ImageSource SaveThumbnail(IsolatedStorageFile isoStore, string filename, Media prefered, Media fallback = null, int minWidth = -1, bool blur = false)
        public static ImageSource SaveThumbnail(ThumbnailOptions options)
		{
			// Make sure this method runs in the UI thread.
			if (!Deployment.Current.Dispatcher.CheckAccess())
			{
				return Deployment.Current.Dispatcher.Invoke<ImageSource>(() => 
				{
                    //return SaveThumbnail(isoStore, filename, prefered, fallback, minWidth, blur);
                    return SaveThumbnail(options);
				});
			}
			
			// Gets the images.
			WriteableBitmap preferedImage = GetBitmapSource(options.PreferedMedia);
            WriteableBitmap fallbackImage = GetBitmapSource(options.FallbackMedia);

			// Determines which image needs to be saved.
            WriteableBitmap targetImage = preferedImage;
			if (preferedImage == null)
			{
				// Use the fallback image if the prefered image does not exist.
				targetImage = fallbackImage;
			}
            else if (fallbackImage != null && options.MinWidth > -1 && preferedImage.PixelWidth < options.MinWidth && preferedImage.PixelWidth < fallbackImage.PixelWidth)
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
			int targetWidth = (int) Math.Max(options.MinWidth, targetImage.PixelWidth);
			double sourcePixelRatio = targetImage.PixelWidth / (double) targetImage.PixelHeight;
			int targetHeight = (int) Math.Floor(targetWidth / sourcePixelRatio);
            
            // Blurs the image if needed.
            ExtendedImage targetExtendedImage = null;
            if (options.Blur)
            {
                // Converts the image to an ImageTools image.
                targetExtendedImage = targetImage.ToImage();

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
            }

            // Crops the image if needed.
            if (options.CropRectangle.HasValue)
            {
                if (targetExtendedImage == null)
                {
                    // Converts the image to an ImageTools image.
                    targetExtendedImage = targetImage.ToImage();
                }

                // Computes the downscaled crop rectangle.
                // We're downscaling the crop rectangle instead of upscaling the image and then cropping it,
                // in order to save some time.
                // WP will upscale the cropped image later on.
                int conformedCropWidth = Math.Min(options.CropRectangle.Value.Width, targetWidth);
                int conformedCropHeight = Math.Min(options.CropRectangle.Value.Height, targetHeight);
                double scaleFactor = (double)targetExtendedImage.PixelWidth / (double)targetWidth;
                Rectangle crop = options.CropRectangle.Value;
                crop.Width = (int)(conformedCropWidth * scaleFactor);
                crop.Height = (int)(conformedCropHeight * scaleFactor);
                crop.X = (int)((double)crop.X * scaleFactor);
                crop.Y = (int)((double)crop.Y * scaleFactor);

                // Crops the image.
                targetExtendedImage = ExtendedImage.Crop(targetExtendedImage, crop);

                // Stores the final dimensions of the image for later scaling.
                targetWidth = conformedCropWidth;
                targetHeight = conformedCropHeight;
            }

            if (targetExtendedImage != null)
            {
                targetImage = targetExtendedImage.ToBitmap();
            }

			// Saves the image.
			try
			{
				using (IsolatedStorageFileStream stream = options.IsoStoreFile.OpenFile(options.Filename, FileMode.Create, FileAccess.ReadWrite))
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
