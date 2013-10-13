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
				using (IsolatedStorageFileStream stream = isf.CreateFile(prefix + "rawdata.txt"))
				{
					stream.Write(data, 0, data.Length);
				}

				// Dumps the stack trace.
				using (IsolatedStorageFileStream stream = isf.CreateFile(prefix + "stacktrace.txt"))
				{
					using (StreamWriter sw = new StreamWriter(stream))
					{
						// Writes a long version of the stacktrace.
						sw.Write(new StackTrace(ex).ToString());
					}
				}
			}
		}
	}
}
