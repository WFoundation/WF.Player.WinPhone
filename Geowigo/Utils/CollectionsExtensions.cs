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
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Geowigo.Utils
{
	public static class CollectionsExtensions
	{
        /// <summary>
        /// Gets the value for a setting, or a default value if the setting doesn't exist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sett"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public static T GetValueOrDefault<T>(this System.IO.IsolatedStorage.IsolatedStorageSettings sett, string key, T val = default(T)) 
        {
            T rval = default(T);
            if (sett.TryGetValue<T>(key, out rval))
            {
                return rval;
            }

            return val;
        }

        /// <summary>
        /// Sets the value for a setting and saves the settings instance.
        /// </summary>
        /// <param name="sett"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetValueAndSave(this System.IO.IsolatedStorage.IsolatedStorageSettings sett, string key, object value)
        {
            sett[key] = value;

            sett.Save();
        }
        
        /// <summary>
		/// Performs additions, update and removals to this ICollection
		/// so that its items match the items of an enumerable.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="coll"></param>
		/// <param name="target"></param>
		public static void SyncWith<T>(this ICollection<T> coll, ICollection<T> target)
		{
			if (target == null || coll == null)
			{
				throw new ArgumentNullException();
			}

			foreach (T t in target)
			{
				bool collContained = coll.Contains(t);
				
				coll.Remove(t);

				if (!collContained)
				{
					coll.Add(t);
				}
			}

			List<T> toRemove = new List<T>();
			foreach (T t in coll)
			{
				if (!target.Contains(t))
				{
					toRemove.Add(t);
				}
			}
			foreach (T t in toRemove)
			{
				coll.Remove(t);
			}
		}
	}
}
