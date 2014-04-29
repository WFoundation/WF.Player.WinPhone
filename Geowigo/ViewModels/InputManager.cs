using System;
using WF.Player.Core;

namespace Geowigo.ViewModels
{
	/// <summary>
	/// A manager for monitoring cartridge Input screens.
	/// </summary>
	public class InputManager
	{
		#region Constants

		private const int MAX_IGNORED_CONSECUTIVE_REQUESTS = 2;

		#endregion
		
		#region Fields

		private int _lastInputObjIndex = -1;
		private int _consecutiveRequests = 0;

		#endregion

		/// <summary>
		/// Marks internally an input as requested, allowing the properties 
		/// of this InputManager to be updated.
		/// </summary>
		/// <param name="input">Input that has been requested to be shown.</param>
		public void HandleInputRequested(Input input)
		{
			// Is the input already tracked?
			// YES -> Increment the consecutive requests field.
			// NO -> Forget the last input and tracks this one.
			int objIndex = input.ObjIndex;
			if (objIndex == _lastInputObjIndex)
			{
				_consecutiveRequests++;
			}
			else
			{
				_lastInputObjIndex = objIndex;
				_consecutiveRequests = 1;
			}
		}

		/// <summary>
		/// Determines if an input is known to be looping.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public bool IsLooping(Input input)
		{
			return input != null && input.ObjIndex == _lastInputObjIndex && _consecutiveRequests > MAX_IGNORED_CONSECUTIVE_REQUESTS;
		}

		/// <summary>
		/// Resets this manager so that no Input is marked as looping.
		/// </summary>
		public void Reset()
		{
			_lastInputObjIndex = -1;
			_consecutiveRequests = 0;
		}
	}
}
