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
using System.Runtime.Serialization;

namespace Geowigo.Models
{
    /// <summary>
    /// An entry in the history of user operations.
    /// </summary>
    [DataContract]
    public class HistoryEntry
    {
        #region Enums
        
        /// <summary>
        /// A type of history entry.
        /// </summary>
        public enum Type
        {
            Started,
            Restored,
            Saved,
            Installed
        } 

        #endregion

        #region Properties

        [DataMember]
        public HistoryEntry.Type EntryType { get; set; }

        [DataMember]
        public string RelatedSavegameName { get; set; }

        [DataMember]
        public string RelatedCartridgeGuid { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        #endregion

        #region Constructors

        internal HistoryEntry()
        {

        }

        public HistoryEntry(CartridgeTag cartTag)
        {

        }

        #endregion
    }
}
