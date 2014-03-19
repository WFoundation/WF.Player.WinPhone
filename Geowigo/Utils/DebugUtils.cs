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
using System.IO.IsolatedStorage;
using System.IO;
using System.Diagnostics;
using BugSense;

namespace Geowigo.Utils
{
	public class DebugUtils
	{
		#region Dumps
		/// <summary>
		/// Dumps a message and data to the isolated storage.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="message"></param>
		public static void DumpData(byte[] data, string message)
		{
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				// Ensures the folder exists.
				isf.CreateDirectory("/Debug");

				// Gets the filename prefix.
				string prefix = "/Debug/" + DateTime.UtcNow.ToString("yyyyMMddHHmmssffff") + "_msg_";

				// Dumps the data.
				using (IsolatedStorageFileStream stream = isf.CreateFile(prefix + "rawdata.txt"))
				{
					stream.Write(data, 0, data.Length);
				}

				// Dumps the stack trace.
				using (IsolatedStorageFileStream stream = isf.CreateFile(prefix + "msg_st.txt"))
				{
					using (StreamWriter sw = new StreamWriter(stream))
					{
						sw.Write(message);
						sw.WriteLine("----");
						DumpStack(sw);
					}
				}
			}
		}

		/// <summary>
		/// Dumps an exception and data to the isolated storage.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="ex"></param>
		public static void DumpData(byte[] data, Exception ex)
		{
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				// Ensures the folder exists.
				isf.CreateDirectory("/Debug");

				// Gets the filename prefix.
				string prefix = "/Debug/" + DateTime.UtcNow.ToString("yyyyMMddHHmmssffff") + "_" + ex.GetHashCode() + "_";

				// Dumps the data.
				using (IsolatedStorageFileStream stream = isf.CreateFile(prefix + "ex_rawdata.txt"))
				{
					stream.Write(data, 0, data.Length);
				}

				// Dumps the stack trace.
				using (IsolatedStorageFileStream stream = isf.CreateFile(prefix + "ex_stacktrace.txt"))
				{
					DumpException(ex, stream);
				}
			}
		}

		/// <summary>
		/// Dumps an exception to the isolated storage and on BugSense.
		/// </summary>
		/// <param name="ex"></param>
		/// <param name="customMessage"></param>
		/// <param name="dumpOnBugSenseToo"></param>
		public static void DumpException(Exception ex, string customMessage = null, bool dumpOnBugSenseToo = false)
		{
			// BugSense dump.
			if (dumpOnBugSenseToo && BugSenseHandler.IsInitialized)
			{
				BugSense.Core.Model.LimitedCrashExtraDataList extraData = null;
				if (customMessage != null)
				{
					extraData = new BugSense.Core.Model.LimitedCrashExtraDataList();
					extraData.Add("customMessage", customMessage);
				}

				BugSenseHandler.Instance.SendExceptionAsync(ex, extraData);
			}

			// Local dump.
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				// Ensures the folder exists.
				isf.CreateDirectory("/Debug");

				// Gets the filename prefix.
				string prefix = "/Debug/" + DateTime.UtcNow.ToString("yyyyMMddHHmmssffff") + "_" + ex.GetHashCode() + "_";

				// Dumps the stack trace.
				using (IsolatedStorageFileStream stream = isf.CreateFile(prefix + "stacktrace.txt"))
				{
					DumpException(ex, stream, customMessage);
				}
			}
		}

		private static void DumpException(Exception ex, Stream stream, string header = null)
		{
			using (StreamWriter sw = new StreamWriter(stream))
			{
				if (header != null)
				{
					sw.WriteLine("Custom header message:");
					sw.WriteLine(header);
					sw.WriteLine("---");
				}

				sw.WriteLine("Main Exception: " + ex.GetType().FullName);
				sw.WriteLine("---");
				sw.WriteLine(ex);
				sw.WriteLine("---");

				Exception eo = ex.InnerException;
				while (eo != null)
				{
					sw.WriteLine(eo);
					sw.WriteLine("---");

					eo = eo.InnerException;
				}
			}
		}

		private static void DumpStack(StreamWriter sw)
		{
			sw.WriteLine("Stack trace:");
			sw.WriteLine();

			StackTrace st = new StackTrace();

			sw.WriteLine(st.ToString());
		} 
		#endregion


		#region BugSense Specific

		/// <summary>
		/// Adds an extra data to BugSense describing a cartridge.
		/// </summary>
		/// <param name="cart"></param>
		public static void AddBugSenseCrashExtraData(WF.Player.Core.Cartridge cart)
		{
			var extraDataList = BugSenseHandler.Instance.CrashExtraData;
			extraDataList.Add(new BugSense.Core.Model.CrashExtraData("cartGuid", cart.Guid));
			extraDataList.Add(new BugSense.Core.Model.CrashExtraData("cartName", cart.Name));
		}

		/// <summary>
		/// Removes all extra data currently associated to the BugSense instance.
		/// </summary>
		public static void ClearBugSenseCrashExtraData()
		{
			BugSenseHandler.Instance.ClearCrashExtraData();
		} 
		#endregion
	}
}
