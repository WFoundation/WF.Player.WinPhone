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
using Geowigo.Models;
using Microsoft.Phone.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Geowigo.ViewModels
{
	/// <summary>
	/// A manager for handling the flow of message boxes of the app.
	/// </summary>
	public class MessageBoxManager
	{
		#region Constants

		/// <summary>
		/// How much time to wait before a HasMessageBoxChanged event is
		/// confirmed and raised.
		/// </summary>
		private const int MessageBoxChangeRaisingDelayMilliseconds = 150;

		#endregion
		
		#region Events

        public event EventHandler HasMessageBoxChanged;

        #endregion

		#region Members

		/// <summary>
		/// Matches a custom message box to its wherigo equivalent.
		/// </summary>
		private Dictionary<CustomMessageBox, WF.Player.Core.MessageBox> _wherigoMessageBoxes = new Dictionary<CustomMessageBox, WF.Player.Core.MessageBox>();

		private List<CustomMessageBox> _otherMessageBoxes = new List<CustomMessageBox>();

		private Timer _eventRaisingTimer;

        private object _syncRoot = new object();

		#endregion

        #region Properties

		#region HasMessageBox
		/// <summary>
        /// Gets if this manager has an active message box.
        /// </summary>
        public bool HasMessageBox
        {
            get
            {
                lock (_syncRoot)
                {
                    return _wherigoMessageBoxes.Count + _otherMessageBoxes.Count > 0;
                }
            }
        }
        #endregion

        #endregion

		/// <summary>
		/// Displays a message box from a Wherigo game. 
        /// If a message box is on-screen, it is dismissed.
		/// </summary>
		/// <param name="mbox"></param>
		public void Show(WF.Player.Core.MessageBox mbox)
		{
			if (mbox == null)
			{
				throw new ArgumentNullException("mbox");
			}

            Accept(mbox).Show();
		}

		/// <summary>
		/// Takes control of a custom message box and display it.
		/// </summary>
		/// <remarks>This MessageBoxManager will from now on take care
		/// of the lifetime of this custom message box, namely raising
		/// HasMessageBoxChanged when this message box is shown or
		/// hidden.</remarks>
		/// <param name="cmb">A message box to display.</param>
		public void AcceptAndShow(CustomMessageBox cmb)
		{
			if (cmb == null)
			{
				throw new ArgumentNullException("cmb");
			}

			Accept(cmb).Show();
		}

		private CustomMessageBox Accept(CustomMessageBox cmb)
		{
			// Registers event handlers for it.
			RegisterEventHandlersForCustom(cmb);

			// Remembers this message box.
			bool hadMessageBoxes;
			lock (_syncRoot)
			{
				hadMessageBoxes = HasMessageBox;
				_otherMessageBoxes.Add(cmb);
			}

			// Raises an event if this has changed.
			if (!hadMessageBoxes)
			{
				RaiseHasMessageBoxChanged();
			}

			return cmb;
		}

		/// <summary>
		/// Makes sure a message box is managed by this manager.
		/// </summary>
		/// <param name="wmb"></param>
		private CustomMessageBox Accept(WF.Player.Core.MessageBox wmb)
		{
			CustomMessageBox cmb = null;
			
			// Checks if this instance already manages this message box.
			// If not, starts to manage the box.
			KeyValuePair<CustomMessageBox, WF.Player.Core.MessageBox> pair = _wherigoMessageBoxes.SingleOrDefault(kv => kv.Value == wmb);
			if (pair.Value == wmb)
			{
				// The target message box exists already.
				cmb = pair.Key;
			}
			else
			{
				// Creates a target message box.
				cmb = new CustomMessageBox()
				{
					Caption = App.Current.Model.Core.Cartridge.Name,
					//Message = wmb.Text,
					Content = new Controls.WherigoMessageBoxContentControl() { MessageBox = wmb },
					LeftButtonContent = wmb.FirstButtonLabel ?? "OK",
					RightButtonContent = wmb.SecondButtonLabel
				};

				// Registers events.
				RegisterEventHandlersForWig(cmb);

				// Adds the pair to the dictionary.
				bool hadMessageBoxes;
				lock (_syncRoot)
                {
					hadMessageBoxes = HasMessageBox;
					_wherigoMessageBoxes.Add(cmb, wmb); 
                }

                // Sends an event if it changed.
				if (!hadMessageBoxes)
				{
					RaiseHasMessageBoxChanged(); 
				}
			}

			return cmb;
		}

		/// <summary>
		/// Dismisses all active message boxes registered by this manager.
		/// </summary>
		public void DismissAllMessageBoxes()
		{
			foreach (var mb in _otherMessageBoxes.ToList())
			{
				mb.Dismiss();
			}
			foreach (var mb in _wherigoMessageBoxes.Keys.ToList())
			{
				mb.Dismiss();
			}
		}

		#region Event handling
		private void RegisterEventHandlersForCustom(CustomMessageBox cmb)
		{
			cmb.Dismissed += new EventHandler<DismissedEventArgs>(OnCustomMessageBoxDismissed);
		}

		private void RegisterEventHandlersForWig(CustomMessageBox cmb)
		{
            cmb.Dismissed += new EventHandler<DismissedEventArgs>(OnWigCustomMessageBoxDismissed);
		}

		private void UnregisterEventHandlers(CustomMessageBox cmb)
		{
            cmb.Dismissed -= new EventHandler<DismissedEventArgs>(OnWigCustomMessageBoxDismissed);
			cmb.Dismissed -= new EventHandler<DismissedEventArgs>(OnCustomMessageBoxDismissed);
		}

		private void OnCustomMessageBoxDismissed(object sender, DismissedEventArgs e)
		{
			CustomMessageBox cmb = (CustomMessageBox)sender;

			// Unregisters events.
			UnregisterEventHandlers(cmb);

			// Removes this message box if it is registered.
			bool hasValue = false;
			bool hasMoreBoxes = false;
			lock (_syncRoot)
			{
				if (hasValue = _otherMessageBoxes.Contains(cmb))
				{
					_otherMessageBoxes.Remove(cmb);
				}

				hasMoreBoxes = HasMessageBox;
			}

			// Sends an event if it was registered.
			if (hasValue && !hasMoreBoxes)
			{
				// If no more message box is managed, send an event.
				BeginRaiseHasMessageBoxChanged();
			}
		}

		private void OnWigCustomMessageBoxDismissed(object sender, DismissedEventArgs e)
		{
			CustomMessageBox cmb = (CustomMessageBox)sender;

			// Unregisters events.
			UnregisterEventHandlers(cmb);

			// Looks the corresponding wherigo message box up in the dictionary, and 
			// gives it a result depending on the custom message box result.
			WF.Player.Core.MessageBox wmb;
			bool hasValue = false;
			lock (_syncRoot)
			{
				hasValue = _wherigoMessageBoxes.TryGetValue(cmb, out wmb);
			}
			if (hasValue)
			{
                // Gives result to the Wherigo message box.
				switch (e.Result)
				{
					case CustomMessageBoxResult.LeftButton:
						wmb.GiveResult(WF.Player.Core.MessageBoxResult.FirstButton);
						break;

					case CustomMessageBoxResult.RightButton:
						wmb.GiveResult(WF.Player.Core.MessageBoxResult.SecondButton);
						break;

					case CustomMessageBoxResult.None:
						// TODO: Keep track of lost message boxes.
						System.Diagnostics.Debug.WriteLine("Dismissed message box with no result: " + wmb.Text);

						wmb.GiveResult(WF.Player.Core.MessageBoxResult.Cancel);
						break;

					default:
						throw new InvalidOperationException("Unknown value of CustomMessageBoxResult cannot be processed: " + e.Result.ToString());
				}

                // Bye bye box.
				bool hasMoreBoxes = false;
                lock (_syncRoot)
                {
                    _wherigoMessageBoxes.Remove(cmb);
					hasMoreBoxes = HasMessageBox;
                }
				
                // If no more message box is managed, send an event.
				if (!hasMoreBoxes)
				{
					BeginRaiseHasMessageBoxChanged(); 
				}
			}

		}
		#endregion

		#region Event Raising

		private void BeginRaiseHasMessageBoxChanged()
		{
			// If a timer is already waiting for a message box to arrive,
			// stop it, because the change it carries has been overriden by
			// this very event.
			// If not, starts the timer.
			lock (_syncRoot)
			{
				if (_eventRaisingTimer != null)
				{
					_eventRaisingTimer.Change(Timeout.Infinite, Timeout.Infinite);
					_eventRaisingTimer.Dispose();
					_eventRaisingTimer = null;
				}
				else
				{
					_eventRaisingTimer = new Timer(OnEventRaisingTimerTick, null, MessageBoxChangeRaisingDelayMilliseconds, Timeout.Infinite);
				}
			}
		}

		private void OnEventRaisingTimerTick(object state)
		{
			// Disposes the timer.
			int messageBoxCount;
			lock (_syncRoot)
			{
				if (_eventRaisingTimer != null)
				{
					_eventRaisingTimer.Dispose();
					_eventRaisingTimer = null;
				}

				messageBoxCount = _wherigoMessageBoxes.Count + _otherMessageBoxes.Count;
			}

			// Sends the event.
			Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				if (HasMessageBoxChanged != null)
				{
					HasMessageBoxChanged(this, EventArgs.Empty);
				}
			}));
		}

		private void RaiseHasMessageBoxChanged()
		{
			if (HasMessageBoxChanged != null)
			{
				HasMessageBoxChanged(this, EventArgs.Empty);
			}
		}

		#endregion
	}
}
