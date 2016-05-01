using System;
using System.Runtime.Serialization;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Geowigo.Models
{
    /// <summary>
    /// An entry in the history of user operations.
    /// </summary>
    [DataContract]
    public class HistoryEntry
    {
        #region Constants

        public const int ThumbnailWidth = 62;

        #endregion
        
        #region Members

        private CartridgeTag _tag;
        private CartridgeSavegame _savegame;
        private BitmapSource _thumbnail;
        
        #endregion
        
        #region Enums
        
        /// <summary>
        /// A type of history entry.
        /// </summary>
        public enum Type
        {
            Started,
            Restored,
            Saved,
            Completed
        } 

        #endregion

        #region Properties

        [DataMember]
        public HistoryEntry.Type EntryType { get; set; }

        [DataMember]
        public string RelatedSavegameName { get; set; }

        [DataMember]
        public Color RelatedSavegameHashColor { get; set; }

        [DataMember]
        public string RelatedCartridgeGuid { get; set; }

        [DataMember]
        public string RelatedCartridgeFilename { get; set; }

        [DataMember]
        public string RelatedCartridgeName { get; set; }

        [DataMember]
        public string RelatedCartridgeThumbnailBase64 { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        public CartridgeTag CartridgeTag
        {
            get
            {
                if (_tag == null)
                {
                    // Binds the cartridge tag.
                    _tag = App.Current.Model.CartridgeStore.GetCartridgeTag(RelatedCartridgeFilename, RelatedCartridgeGuid);
                }

                return _tag;
            }
        }

        public CartridgeSavegame Savegame
        {
            get
            {
                if (_savegame == null)
                {
                    // Binds the savegame.
                    CartridgeTag tag = CartridgeTag;
                    if (tag != null)
                    {
                        _savegame = tag.GetSavegameByNameOrDefault(RelatedSavegameName);
                    }
                }

                return _savegame;
            }
        }

        public ImageSource Thumbnail
        {
            get
            {
                if (_thumbnail == null && RelatedCartridgeThumbnailBase64 != null)
                {
                    // Gets the thumbnail from the base64 data.
                    _thumbnail = Utils.ImageUtils.GetBitmapSource(Convert.FromBase64String(RelatedCartridgeThumbnailBase64));
                }

                return _thumbnail;
            }
        }

        #endregion

        #region Constructors

        internal HistoryEntry()
        {
        }

        /// <summary>
        /// Constructs a history entry of a particular type, populating
        /// its fields with the contents of a cartridge tag, and using
        /// <code>DateTime.Now</code> as timestamp.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="cartTag"></param>
        public HistoryEntry(HistoryEntry.Type type, CartridgeTag cartTag)
        {
            EntryType = type;
            RelatedCartridgeFilename = cartTag.Cartridge.Filename;
            RelatedCartridgeGuid = cartTag.Guid;
            RelatedCartridgeName = cartTag.Title;

            BitmapSource thumb = (BitmapSource)cartTag.Icon;
            RelatedCartridgeThumbnailBase64 = thumb == null ? null : Utils.ImageUtils.ToBase64String(thumb, ThumbnailWidth, ThumbnailWidth);

            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Constructs a history entry of a particular type, populating
        /// its fields with the contents of a cartridge tag and a cartridge
        /// savegame, and using <code>DateTime.Now</code> as timestamp.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="cartTag"></param>
        /// <param name="savegame"></param>
        public HistoryEntry(HistoryEntry.Type type, CartridgeTag cartTag, CartridgeSavegame savegame)
            : this(type, cartTag)
        {
            RelatedSavegameHashColor = savegame.HashColor;
            RelatedSavegameName = savegame.Name;
        }

        #endregion
    }
}
