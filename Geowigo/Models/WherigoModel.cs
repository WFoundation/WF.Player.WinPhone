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
using WF.Player.Core;
using System.IO.IsolatedStorage;

namespace Geowigo.Models
{
	/// <summary>
	/// Provides facility methods to query the Wherigo engine model.
	/// </summary>
	public class WherigoModel
	{
		#region Properties

		/// <summary>
		/// Gets or sets the Wherigo core responsible for wherigo work and feedback.
		/// </summary>
		public WFCoreAdapter Core { get; set; }

		/// <summary>
		/// Gets the cartridge store that registers cartridges.
		/// </summary>
		public CartridgeStore CartridgeStore { get; private set; }

		#endregion

		#region Constructors

		public WherigoModel()
		{
			Core = new WFCoreAdapter();
			CartridgeStore = new CartridgeStore();
		} 

		#endregion

		#region Public Methods


		#endregion

	}
}
