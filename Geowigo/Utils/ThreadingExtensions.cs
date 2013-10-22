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
using System.Windows.Threading;
using System.Threading;

namespace Geowigo.Utils
{
	public static class ThreadingExtensions
	{
		/// <summary>
		/// Requests an action to be performed on the Dispatcher and blocks the calling thread
		/// until this action has finished.
		/// </summary>
		/// <param name="dispatcher"></param>
		/// <param name="action"></param>
		public static void Invoke(this Dispatcher dispatcher, Action action)
		{
			ManualResetEvent resetEvent = new ManualResetEvent(false);

			dispatcher.BeginInvoke(new Action(() =>
			{
				// Executes the actions.
				action();

				// Sets the event.
				resetEvent.Set();
			}));

			// Waits for the dispatcher to finish.
			resetEvent.WaitOne();
		}

		/// <summary>
		/// Requests a function to be performed on the Dispatcher and blocks the calling thread
		/// until this action has finished.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="dispatcher"></param>
		/// <param name="func"></param>
		/// <returns></returns>
		public static T Invoke<T>(this Dispatcher dispatcher, Func<T> func)
		{
			T t = default(T);

			Invoke(dispatcher, new Action(() =>
			{
				t = func();
			}));

			return t;
		}
	}
}
