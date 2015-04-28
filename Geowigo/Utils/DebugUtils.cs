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
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;

namespace Geowigo.Utils
{
	public class DebugUtils
	{
        #region Fields

        private static Dictionary<string, string> _LogSessions = new Dictionary<string, string>();

        #endregion
        
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

        #region Logs

        /// <summary>
        /// Creates a new log session for a particular name.
        /// </summary>
        /// <param name="logName"></param>
        public static void NewLogSession(string logName) 
        {
            // Prepares the name of the log file.
            string logFileName = "/Debug/" + DateTime.UtcNow.ToString("yyyyMMddHHmmssffff") + "_log_" + logName + ".txt";

            // Creates the file.
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                // Makes sure the directory exists.
                isf.CreateDirectory("Debug");

                // Creates the file.
                isf.OpenFile(logFileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite).Dispose();
            }

            // Saves the session filename for later.
            _LogSessions[logName] = logFileName;
        }

        /// <summary>
        /// Logs a message in a previously created log session.
        /// </summary>
        /// <param name="logName"></param>
        /// <param name="message"></param>
        public static void Log(string logName, string message)
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                // Opens the log file.
                string filename = _LogSessions[logName];
                using (IsolatedStorageFileStream isfs = isf.OpenFile(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                    // Appends the message.
                    using (StreamWriter sw = new StreamWriter(isfs))
                    {
                        sw.WriteLine(DateTime.UtcNow.ToString("HH:mm:ss") + " " + message);
                    }
                }
            }
        }

        #endregion

        #region Bug Reporting

        /// <summary>
        /// Compiles all generated debug file into one string.
        /// </summary>
        /// <returns></returns>
        public static string MakeDebugReport(bool includeRawData = false)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Geowigo v" + GetVersion());
            sb.AppendLine("Report generated on " + DateTime.UtcNow);

            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                // Enumerates files in /Debug.
                foreach (string filePath in isf.GetFileNames("/Debug/").Select(s => System.IO.Path.Combine("\\Debug", s)))
                {
                    // Got a file.
                    sb.AppendLine();
                    sb.AppendLine("===========");
                    sb.AppendLine(filePath);
                    if (!includeRawData && filePath.Contains("rawdata"))
                    {
                        sb.AppendLine("Ignored in this report.");
                        continue;
                    }

                    // Let's output the file.
                    sb.AppendLine();
                    try
                    {
                        using (IsolatedStorageFileStream isfs = isf.OpenFile(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                            using (StreamReader sr = new StreamReader(isfs))
                            {
                                sb.AppendLine(sr.ReadToEnd());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine("!!! Exception while reading log file: " + ex.Message);
                    }
                }
            }

            sb.AppendLine("===========");
            sb.AppendLine("End of report.");

            return sb.ToString();
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

			string guid = cart.Guid;
			if (guid != null)
			{
				extraDataList.Add(new BugSense.Core.Model.CrashExtraData("cartGuid", guid)); 
			}

			string name = cart.Name;
			if (name != null)
			{
				extraDataList.Add(new BugSense.Core.Model.CrashExtraData("cartName", name.Trim()));
			}

            string engineGameState = "<unknown>";
            Models.WherigoModel model = App.Current.Model;
            if (model != null)
            {
                Models.WFCoreAdapter core = model.Core;
                if (core != null)
                {
                    engineGameState = core.GameState.ToString();
                }
            }
            extraDataList.Add(new BugSense.Core.Model.CrashExtraData("engineGameState", engineGameState));
		}

		/// <summary>
		/// Removes all extra data currently associated to the BugSense instance.
		/// </summary>
		public static void ClearBugSenseCrashExtraData()
		{
			BugSenseHandler.Instance.ClearCrashExtraData();
		} 
		#endregion

        /// <summary>
        /// Removes all files from the log and dump cache.
        /// </summary>
        public static void ClearCache()
        {
            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    isf.DeleteDirectoryRecursive("/Debug");
                }
                catch (Exception)
                {

                }
            }
        }

        /// <summary>
        /// Returns the declared version of this app.
        /// </summary>
        /// <returns></returns>
        public static string GetVersion()
        {
            return Version.Parse(Assembly.GetExecutingAssembly()
                        .GetCustomAttributes(false)
                        .OfType<AssemblyFileVersionAttribute>()
                        .First()
                        .Version).ToString();
        }
    }
}
