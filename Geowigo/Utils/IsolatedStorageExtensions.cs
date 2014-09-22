using System;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.IO;

namespace Geowigo.Utils
{
	public static class IsolatedStorageExtensions
	{
		public static List<String> GetAllDirectories(this IsolatedStorageFile storeFile, string pattern)
		{
			// Get the root of the search string.
			string root = Path.GetDirectoryName(pattern);

			if (root != "")
			{
				root += "/";
			}

			// Retrieve directories.
			List<String> directoryList = new List<String>(storeFile.GetDirectoryNames(pattern));

			// Retrieve subdirectories of matches.
			for (int i = 0, max = directoryList.Count; i < max; i++)
			{
				string directory = directoryList[i] + "/";
				List<String> more = storeFile.GetAllDirectories(root + directory + "*");

				// For each subdirectory found, add in the base path.
				for (int j = 0; j < more.Count; j++)
				{
					more[j] = directory + more[j];
				}

				// Insert the subdirectories into the list and
				// update the counter and upper bound.
				directoryList.InsertRange(i + 1, more);
				i += more.Count;
				max += more.Count;
			}

			return directoryList;
		}
		
		public static List<String> GetAllFiles(this IsolatedStorageFile storeFile, string pattern)
		{
			// Get the root and file portions of the search string.
			string fileString = Path.GetFileName(pattern);

			List<String> fileList = new List<String>(storeFile.GetFileNames(pattern));

			// Loop through the subdirectories, collect matches,
			// and make separators consistent.
			foreach (string directory in storeFile.GetAllDirectories("*"))
			{
				foreach (string file in storeFile.GetFileNames(directory + "/" + fileString))
				{
					fileList.Add((directory + "/" + file));
				}
			}

			return fileList;
		}
	}
}
