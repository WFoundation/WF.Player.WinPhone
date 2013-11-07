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

namespace Geowigo.Utils
{
	public static class VisualTreeExtensions
	{
		/// <summary>
		/// Finds a child in the visual tree of this DependencyObject.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="parent"></param>
		/// <param name="childName"></param>
		/// <returns></returns>
		public static T FindChild<T>(this DependencyObject parent, string childName) where T : DependencyObject
		{
			// Adapted from http://stackoverflow.com/questions/7034522/how-to-find-element-in-visual-tree-wp7

			T foundChild = null;

			int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < childrenCount; i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(parent, i);
				// If the child is not of the request child type child
				var childType = child as T;
				if (childType == null)
				{
					// recursively drill down the tree
					foundChild = FindChild<T>(child, childName);

					// If the child is found, break so we do not overwrite the found child. 
					if (foundChild != null)
					{
						break;
					}
				}
				else if (!string.IsNullOrEmpty(childName))
				{
					var frameworkElement = child as FrameworkElement;
					// If the child's name is set for search
					if (frameworkElement != null && frameworkElement.Name == childName)
					{
						// if the child's name is of the request name
						foundChild = (T)child;
						break;
					}

					// Need this in case the element we want is nested
					// in another element of the same type
					foundChild = FindChild<T>(child, childName);
				}
				else
				{
					// child element found.
					foundChild = (T)child;
					break;
				}
			}

			return foundChild;
		}
	}
}
