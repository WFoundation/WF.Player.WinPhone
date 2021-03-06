﻿using System;
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
using WF.Player.Core;

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
					try
					{
						using (IsolatedStorageFileStream fs = isf.OpenFile(isoStoreFile, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
						{
							_soundPlayer.SetSource(fs);
							_soundPlayer.Play();
						}
					}
					catch (Exception ex)
					{
						// Logs the exception.
						DebugUtils.DumpException(ex, "play sound " + System.IO.Path.GetFileName(isoStoreFile), true);
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

		/// <summary>
		/// Determines if a media represents a sound that can be played 
		/// by this SoundManager.
		/// </summary>
		/// <param name="media"></param>
		/// <returns>True if the sound type is supported by this sound 
		/// manager.</returns>
		public static bool IsPlayableSound(Media media)
		{
			if (media == null)
			{
				return false;
			}

			MediaType mt = media.Type;

			return mt == MediaType.MP3 || mt == MediaType.WAV || mt == MediaType.FDL;
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
