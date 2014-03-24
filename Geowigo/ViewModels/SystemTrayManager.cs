using System;
using Geowigo.Utils;
using System.Threading;
using Microsoft.Phone.Shell;
using System.Windows;

namespace Geowigo.ViewModels
{
	/// <summary>
	/// A manager for displaying progress and status on the system tray.
	/// </summary>
	public class SystemTrayManager
	{
		#region Constants

		/// <summary>
		/// Timespan to wait before showing indeterminate progress of a long task.
		/// </summary>
		private readonly TimeSpan LongProgressDisplayDelay = TimeSpan.FromSeconds(1.25);

		private readonly TimeSpan TemporaryDisplay = TimeSpan.FromSeconds(2);

		#endregion
		
		#region Fields

		private string _statusText;

		private ProgressAggregator _progressAggregator = new ProgressAggregator();

		private Timer _loadingDeferTimer;

		private Timer _tempStatusTextDisplay;

		private string LoadingProgressSourceKey = "Loading...";

		private object _syncRoot = new object();

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets a permanent status text of the system tray.
		/// </summary>
		/// <remarks>Null, empty or whitespace-only values make the
		/// status text disappear.</remarks>
		public string StatusText
		{
			get
			{
				return _statusText;
			}

			set
			{
				string newValue = String.IsNullOrWhiteSpace(value) ? null : value;
				if (_statusText != newValue)
				{
					_statusText = newValue;

					OnStatusTextChanged(newValue);
				}
			}
		}

		#endregion

		public SystemTrayManager()
		{
			_progressAggregator.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(OnProgressAggregatorPropertyChanged);
		}

		#region Generic Progress

		/// <summary>
		/// Shows an indeterminate progress.
		/// </summary>
		/// <param name="status"></param>
		public void ShowProgress(string status)
		{
			_progressAggregator[status] = true;
		}

		/// <summary>
		/// Hides an indeterminate progress that was previously shown.
		/// </summary>
		/// <param name="status"></param>
		public void HideProgress(string status)
		{
			_progressAggregator[status] = false;
		}

		#endregion

		#region Loading Progress

		/// <summary>
		/// Displays a "Loading" progress indicator after a short
		/// delay.
		/// </summary>
		/// <remarks>This method has no effect if the "Loading"
		/// progress indicator is already displayed or has already
		/// been marked for display.</remarks>
		public void BeginShowLoading()
		{
			lock (_syncRoot)
			{
				// Returns if the timer or the display is already on.
				if (_loadingDeferTimer != null || _progressAggregator[LoadingProgressSourceKey])
				{
					return;
				}

				// Creates and sets the timer.
				_loadingDeferTimer = new Timer(OnLoadingDeferTimerTick, null, (int)LongProgressDisplayDelay.TotalMilliseconds, Timeout.Infinite);
			}
		}

		/// <summary>
		/// Hides a "Loading" progress indicator immediately.
		/// </summary>
		/// <remarks><para>If the progress indicator has been marked
		/// for display by <code>BeginShowLoading()</code> but
		/// has not been displayed yet, its scheduled display
		/// is cancelled and it will not appear.</para>
		/// <para>This method has no effect if the "Loading"
		/// progress indicator is not displayed or not marked for
		/// display.</para></remarks>
		public void HideLoading()
		{
			// Cancels the defer timer if it is on.
			lock (_syncRoot)
			{
				if (_loadingDeferTimer != null)
				{
					_loadingDeferTimer.Dispose();
					_loadingDeferTimer = null;
				}
			}

			// Hides the loading progress.
			_progressAggregator[LoadingProgressSourceKey] = false;
		}

		private void OnLoadingDeferTimerTick(object state)
		{
			// Displays the loading progress.
			_progressAggregator[LoadingProgressSourceKey] = true;

			// Cancels the defer timer if it is on.
			lock (_syncRoot)
			{
				if (_loadingDeferTimer != null)
				{
					_loadingDeferTimer.Dispose();
					_loadingDeferTimer = null;
				}
			}
		}

		#endregion

		#region Status Text
		private void OnStatusTextChanged(string newValue)
		{
			// Immediately displays the status text.
			// Keep the progress bar if other operations are in progress.
			SetProgressIndicator(newValue);

			lock (_syncRoot)
			{
				// Disposes the current timer if it exists.
				if (_tempStatusTextDisplay != null)
				{
					_tempStatusTextDisplay.Dispose();
					_tempStatusTextDisplay = null;
				}

				// If there is other progress pending, just show the status text
				// for a while.
				if (_progressAggregator.HasWorkingSource)
				{
					_tempStatusTextDisplay = new Timer(OnTempStatusTextDisplayTick, null, TemporaryDisplay.Seconds, Timeout.Infinite);
				}
			}
		}

		private void OnTempStatusTextDisplayTick(object state)
		{
			// Disposes the current timer if it exists.
			if (_tempStatusTextDisplay != null)
			{
				_tempStatusTextDisplay.Dispose();
				_tempStatusTextDisplay = null;
			}

			// If this ticks, the status text may have to disappear
			// and the system tray should display some other progress instead.
			Deployment.Current.Dispatcher.BeginInvoke(RefreshProgressIndicator);
		}

		#endregion

		#region Progress Indicator Display

		private void RefreshProgressIndicator()
		{
			// Priorities of displaying progress:
			// 1: The first object in the progress aggregator.
			// 2: The status text.

			// Gets the top progress object, if any.
			string topProgress = _progressAggregator.FirstWorkingSource as string;
			if (topProgress != null)
			{
				// Shows this.
				SetProgressIndicator(topProgress, true);
			}
			else
			{
				// Shows the status text and no progress.
				SetProgressIndicator(StatusText, false);
			}

		}

		private void SetProgressIndicator(string status = null, bool? isIndeterminate = null, bool isVisible = true)
		{
			// Gets the current progress indicator.
			ProgressIndicator progress = SystemTray.ProgressIndicator;

			// Creates or updates the current progress indicator.
			if (progress == null)
			{
				// Creates the progress indicator.
				progress = new ProgressIndicator()
				{
					Text = status,
					IsIndeterminate = isIndeterminate ?? _progressAggregator.HasWorkingSource,
					IsVisible = isVisible,
					Value = 0
				};

				// Displays it.
				SystemTray.ProgressIndicator = progress;
			}
			else
			{
				// Updates the progress indicator.
				progress.Text = status;
				progress.IsIndeterminate = isIndeterminate ?? progress.IsIndeterminate;
				progress.IsVisible = isVisible;
			}
		} 

		#endregion

		private void OnProgressAggregatorPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "FirstWorkingSource")
			{
				Deployment.Current.Dispatcher.BeginInvoke(RefreshProgressIndicator);
			}
		}
	}
}
