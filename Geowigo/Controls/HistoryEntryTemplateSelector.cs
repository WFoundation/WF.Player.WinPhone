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
using Geowigo.Models;

namespace Geowigo.Controls
{
    public class HistoryEntryTemplateSelector : DataTemplateSelector
    {
        public DataTemplate StartedTemplate { get; set; }

        public DataTemplate SavedTemplate { get; set; }

        public DataTemplate RestoredTemplate { get; set; }

        public DataTemplate CompletedTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            HistoryEntry entry = item as HistoryEntry;
            if (entry == null)
            {
                return base.SelectTemplate(item, container);
            }

            switch (entry.EntryType)
            {
                case HistoryEntry.Type.Started:
                    return StartedTemplate;
                    
                case HistoryEntry.Type.Restored:
                    return RestoredTemplate;
                    
                case HistoryEntry.Type.Saved:
                    return SavedTemplate;
                    
                case HistoryEntry.Type.Completed:
                    return CompletedTemplate;
                    
                default:
                    return base.SelectTemplate(item, container);
                    
            }
        }
    }
}
