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
using WF.Player.Core;

namespace Geowigo.Controls
{
	public class InputTemplateSelector : DataTemplateSelector
	{
		public DataTemplate TextTemplate { get; set; }

		public DataTemplate MultipleChoiceTemplate { get; set; }
		
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			InputType iType = InputType.Unknown;

			if (item is Input)
			{
				iType = ((Input)item).InputType;
			}

			// Returns the proper template according to the type of the input.
			switch (iType)
			{
				case InputType.MultipleChoice:
					return MultipleChoiceTemplate;

				case InputType.Text:
					return TextTemplate;
	
				default:
					return base.SelectTemplate(item, container);
			}
		}
	}
}
