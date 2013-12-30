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
		/// Gets the Wherigo core responsible for gameplay and feedback.
		/// </summary>
		public WFCoreAdapter Core { get; private set; }

		/// <summary>
		/// Gets the cartridge store that registers cartridges.
		/// </summary>
		public CartridgeStore CartridgeStore { get; private set; }

        /// <summary>
        /// Gets the history of user operations.
        /// </summary>
        public History History { get; private set; }

		#endregion

		#region Constructors

		public WherigoModel()
		{
			Core = new WFCoreAdapter();

			CartridgeStore = new CartridgeStore();

            History = Models.History.FromCacheOrCreate();
		}

		#endregion

		#region Public Methods


		#endregion
	}
}
