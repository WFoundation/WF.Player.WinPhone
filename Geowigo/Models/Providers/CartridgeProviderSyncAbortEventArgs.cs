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

namespace Geowigo.Models.Providers
{
	public class CartridgeProviderSyncAbortEventArgs : EventArgs
	{
		/// <summary>
		/// Gets or sets if the sync has aborted because of a timeout.
		/// </summary>
		public bool HasTimedOut { get; set; }
	}
}
