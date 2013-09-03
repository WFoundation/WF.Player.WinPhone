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

		/// <summary>
		/// Gets a Wherigo object that has a certain id.
		/// </summary>
		/// <typeparam name="T">Type of the object to expect, subclass of Table.</typeparam>
		/// <param name="id">Id of the object to get.</param>
		/// <returns>A wherigo object of the expected type.</returns>
		/// <exception cref="InvalidOperationException">No object with such Id exists, or the object is not of the
		/// required type.</exception>
		public T GetWherigoObject<T>(int id) where T : Table
		{
			Table wobj = Core.GetObject(id);

			if (wobj == null)
			{
				throw new InvalidOperationException("No wherigo object has id " + id);
			}

			if (!(wobj is T))
			{
				throw new InvalidOperationException(String.Format("The wherigo object with id {0} has type {1} but not {2}.", id, wobj.GetType().ToString(), typeof(T).ToString()));
			}

			return (T)wobj;
		}

		/// <summary>
		/// Gets a Wherigo object that has a certain id.
		/// </summary>
		/// <typeparam name="T">Type of the object to expect, subclass of Table.</typeparam>
		/// <param name="id">Id of the object to get.</param>
		/// <param name="wObj">A wherigo object of the expected type, or null if it wasn't found or is not
		/// of the expected type.</param>
		/// <returns>True if the method returned, false otherwise.</returns>
		public bool TryGetWherigoObject<T>(int id, out T wObj) where T : Table
		{
			wObj = Core.GetObject(id) as T;

			return wObj != null;
		}

		#endregion

	}
}
