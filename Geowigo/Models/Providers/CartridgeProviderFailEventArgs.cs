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
	public class CartridgeProviderFailEventArgs : EventArgs
	{
		/// <summary>
		/// Gets or sets if the failure is because of a timeout.
		/// </summary>
		public bool HasTimedOut { get; set; }

		/// <summary>
		/// Gets or sets the exception which caused the failure.
		/// </summary>
		public Exception Exception { get; set; }
	}
}
