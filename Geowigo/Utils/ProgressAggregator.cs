using System;
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
		private List<object> _workingSourcesQueue = new List<object>();
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
				if (value)
				{
					lock (_syncRoot)
					{
						_indeterminateProgresses[key] = value; 
					}
					EnsureWorkingSource(key);
				}
				else if (_indeterminateProgresses.ContainsKey(key))
				{
					lock (_syncRoot)
					{
						_indeterminateProgresses.Remove(key); 
					}
					RemoveWorkingSource(key);
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
					return _workingSourcesQueue.Count > 0;
				}
			}
		}

		/// <summary>
		/// Gets the working source that is currently at the top of the queue.
		/// </summary>
		public object FirstWorkingSource
		{
			get
			{
				return _workingSourcesQueue.FirstOrDefault();
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

		private void RemoveWorkingSource(object key)
		{
			// Removes the source and determines if the top changed.
			object currentTop;
			object newTop;
			lock (_syncRoot)
			{
				currentTop = _workingSourcesQueue.FirstOrDefault();
				_workingSourcesQueue.Remove(key);
				newTop = _workingSourcesQueue.FirstOrDefault(); 
			}

			// Raises an event if the top changed.
			if (currentTop != newTop)
			{
				RaisePropertyChanged("FirstWorkingSource");

				if (currentTop == null || newTop == null)
				{
					RaisePropertyChanged("HasWorkingSource");
				}
			}
		}

		private void EnsureWorkingSource(object key)
		{
			// Makes sure the key is only once in the list, and on top.
			bool hadSource;
			lock (_syncRoot)
			{
				hadSource = _workingSourcesQueue.Count > 0;
				int index = _workingSourcesQueue.IndexOf(key);
				if (index == 0)
				{
					// The working source is already top. Nothing to do.
					return;
				}
				if (index > 0)
				{
					_workingSourcesQueue.Remove(key);
				}
				_workingSourcesQueue.Add(key);
			}

			// Raises events.
			RaisePropertyChanged("FirstWorkingSource");
			if (!hadSource)
			{
				RaisePropertyChanged("HasWorkingSource");
			}
		}
	}
}
