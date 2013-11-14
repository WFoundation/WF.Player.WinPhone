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

namespace Geowigo.Utils
{
	public class DebugUtils
	{		
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
				string prefix = "/Debug/" + DateTime.UtcNow.ToString("yyyyMMddHHmmssffff") +"_" + ex.GetHashCode() + "_";

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
		/// Dumps an exception to the isolated storage.
		/// </summary>
		/// <param name="ex"></param>
		public static void DumpException(Exception ex)
		{
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
			{
				// Ensures the folder exists.
				isf.CreateDirectory("/Debug");

				// Gets the filename prefix.
				string prefix = "/Debug/" + DateTime.UtcNow.ToString("yyyyMMddHHmmssffff") + "_" + ex.GetHashCode() + "_";

				// Dumps the stack trace.
				using (IsolatedStorageFileStream stream = isf.CreateFile(prefix + "stacktrace.txt"))
				{
					DumpException(ex, stream);
				}
			}
		}

		private static void DumpException(Exception ex, Stream stream)
		{
			using (StreamWriter sw = new StreamWriter(stream))
			{
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
	}
}
