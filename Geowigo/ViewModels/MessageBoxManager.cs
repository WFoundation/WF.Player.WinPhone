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

namespace Geowigo.ViewModels
{
	/// <summary>
	/// A manager for handling the flow of message boxes of the app.
	/// </summary>
	public class MessageBoxManager
	{
        #region Events

        public event EventHandler HasMessageBoxChanged;

        #endregion

		#region Members

		/// <summary>
		/// Matches a custom message box to its wherigo equivalent.
		/// </summary>
		private Dictionary<CustomMessageBox, WF.Player.Core.MessageBox> _WherigoMessageBoxes = new Dictionary<CustomMessageBox, WF.Player.Core.MessageBox>();

        private object _syncRoot = new object();

		#endregion

        #region Properties

        #region Count
        /// <summary>
        /// Gets if this manager has an active message box.
        /// </summary>
        public bool HasMessageBox
        {
            get
            {
                lock (_syncRoot)
                {
                    return _WherigoMessageBoxes.Count > 0;
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
		/// Makes sure a message box is managed by this manager.
		/// </summary>
		/// <param name="wmb"></param>
		private CustomMessageBox Accept(WF.Player.Core.MessageBox wmb)
		{
			CustomMessageBox cmb = null;
			
			// Checks if this instance already manages this message box.
			// If not, starts to manage the box.
			KeyValuePair<CustomMessageBox, WF.Player.Core.MessageBox> pair = _WherigoMessageBoxes.SingleOrDefault(kv => kv.Value == wmb);
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
                lock (_syncRoot)
                {
                    _WherigoMessageBoxes.Add(cmb, wmb); 
                }

                // Sends an event.
                if (HasMessageBoxChanged != null)
                {
                    HasMessageBoxChanged(this, EventArgs.Empty);
                }
			}

			return cmb;
		}

		#region Event handling
		private void RegisterEventHandlersForWig(CustomMessageBox cmb)
		{
            cmb.Dismissed += new EventHandler<DismissedEventArgs>(OnWigCustomMessageBoxDismissed);
		}

		private void UnregisterEventHandlers(CustomMessageBox cmb)
		{
            cmb.Dismissed -= new EventHandler<DismissedEventArgs>(OnWigCustomMessageBoxDismissed);
		}

		private void OnWigCustomMessageBoxDismissed(object sender, DismissedEventArgs e)
		{
			CustomMessageBox cmb = (CustomMessageBox)sender;

			// Unregisters events.
			UnregisterEventHandlers(cmb);

			// Looks the corresponding wherigo message box up in the dictionary, and 
			// gives it a result depending on the custom message box result.
			WF.Player.Core.MessageBox wmb;
			if (_WherigoMessageBoxes.TryGetValue(cmb, out wmb))
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
                lock (_syncRoot)
                {
                    _WherigoMessageBoxes.Remove(cmb); 
                }
				
                // If no more message box is managed, send an event.
                if (_WherigoMessageBoxes.Count == 0 && HasMessageBoxChanged != null)
                {
                    HasMessageBoxChanged(this, EventArgs.Empty);
                }
			}

		}
		#endregion

    }
}
