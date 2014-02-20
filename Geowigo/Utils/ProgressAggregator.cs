﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Geowigo.Utils
{
	/// <summary>
	/// A thread-safe aggregator for various sources of progress that deliver an overview
	/// of progress.
	/// </summary>
	public class ProgressAggregator : INotifyPropertyChanged
	{
		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		#region Fields

		private Dictionary<object, bool> _indeterminateProgresses = new Dictionary<object, bool>();
		private object _syncRoot = new object();

		#endregion

		#region Indexers

		/// <summary>
		/// Gets or sets if a source of progress is active or not.
		/// </summary>
		/// <param name="key">The key corresponding to the source of progress.</param>
		/// <returns>True if the source of progress is known and is active,
		/// false if it is not known or is not active.</returns>
		public bool this[object key]
		{
			get
			{
				// Unknown sources of progress return false.
				
				bool value = false;
				bool hasValue = false;
				lock (_syncRoot)
				{
					hasValue = _indeterminateProgresses.TryGetValue(key, out value);
				}

				return hasValue && value;
			}

			set
			{
				// Only keeps a reference to active sources of progress.
				bool hadIndeterminateProgress;
				bool hasIndeterminateProgress;
				lock (_syncRoot)
				{
					hadIndeterminateProgress = _indeterminateProgresses.Any();
					
					if (value)
					{
						_indeterminateProgresses[key] = value;
					}
					else if (_indeterminateProgresses.ContainsKey(key))
					{
						_indeterminateProgresses.Remove(key);
					}

					hasIndeterminateProgress = _indeterminateProgresses.Any();
				}

				// Checks if propeties have changed.
				if (hadIndeterminateProgress != hasIndeterminateProgress)
				{
					RaisePropertyChanged("HasWorkingSource");
				}
			}
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets if this aggregator has any working source of progress.
		/// </summary>
		public bool HasWorkingSource
		{
			get
			{
				lock (_syncRoot)
				{
					return _indeterminateProgresses.Any();
				}
			}
		}

		#endregion

		private void RaisePropertyChanged(string propName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
			}
		}
	}
}
