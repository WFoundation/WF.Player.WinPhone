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
using Geowigo.Utils;
using System.IO.IsolatedStorage;

namespace Geowigo.ViewModels
{
	/// <summary>
	/// A manager for playing sounds.
	/// </summary>
	public class SoundManager
	{
		#region Fields

		private MediaElement _soundPlayer;
		private bool _isPlaying;
		private object _syncRoot = new object();

		#endregion

		#region Public Methods
		/// <summary>
		/// Plays a sounds from the isolated storage.
		/// </summary>
		/// <param name="isoStoreFile">Path to the isolated storage file.</param>
		public void PlaySound(string isoStoreFile)
		{
			if (EnsureSoundPlayerReady())
			{
				// Opens the file and starts playing.
				using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
				{
					using (IsolatedStorageFileStream fs = isf.OpenFile(isoStoreFile, System.IO.FileMode.Open))
					{
						_soundPlayer.SetSource(fs);
						_soundPlayer.Play();
					}
				}
			}
		}

		/// <summary>
		/// Stops currently playing sounds if any.
		/// </summary>
		public void StopSounds()
		{
			if (_soundPlayer != null)
			{
				StopSoundsInternal();
			}
		}

		#endregion

		#region Internal Player Management
		private bool EnsureSoundPlayerReady()
		{
			if (_soundPlayer != null)
			{
				// Stops all sounds.
				StopSoundsInternal();

				return true;
			}

			//MediaElement currentPlayer = App.Current.RootFrame.FindName("CommonMediaElement") as MediaElement;
			MediaElement currentPlayer = App.Current.RootFrame.FindChild<MediaElement>("CommonMediaElement");

			if (currentPlayer != _soundPlayer)
			{
				// Detaches handlers from the old sound player.
				if (_soundPlayer != null)
				{
					_soundPlayer.MediaOpened -= new RoutedEventHandler(OnMediaOpened);
					_soundPlayer.MediaFailed -= new EventHandler<ExceptionRoutedEventArgs>(OnMediaFailed);
					_soundPlayer.MediaEnded -= new RoutedEventHandler(OnMediaEnded);
				}

				// Attaches handlers on the new sound player.
				_soundPlayer = currentPlayer;
				if (_soundPlayer != null)
				{
					_soundPlayer.MediaOpened += new RoutedEventHandler(OnMediaOpened);
					_soundPlayer.MediaFailed += new EventHandler<ExceptionRoutedEventArgs>(OnMediaFailed);
					_soundPlayer.MediaEnded += new RoutedEventHandler(OnMediaEnded);
				}
			}

			return _soundPlayer != null;
		}

		private void StopSoundsInternal()
		{
			bool isPlaying;

			lock (_syncRoot)
			{
				isPlaying = _isPlaying;
			}

			if (_isPlaying)
			{
				_soundPlayer.Stop();
			}
		} 
		#endregion

		#region Media Element Event Handlers
		private void OnMediaEnded(object sender, RoutedEventArgs e)
		{
			lock (_syncRoot)
			{
				_isPlaying = false;
			}
		}

		private void OnMediaFailed(object sender, ExceptionRoutedEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine("SoundManager: Media failed. {0}", e.ErrorException.Message);

			lock (_syncRoot)
			{
				_isPlaying = false;
			}
		}

		private void OnMediaOpened(object sender, RoutedEventArgs e)
		{
			lock (_syncRoot)
			{
				_isPlaying = true;
			}
		} 
		#endregion
	}
}
