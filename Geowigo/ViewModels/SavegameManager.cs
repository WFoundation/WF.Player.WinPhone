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
using Microsoft.Phone.Controls;
using System.Linq;

namespace Geowigo.ViewModels
{
    /// <summary>
    /// A manager for creating, renaming savegames.
    /// </summary>
    public class SavegameManager
    {

        #region Members

        private AppViewModel _appViewModel;

        #endregion

        #region Constructors

        public SavegameManager(AppViewModel appViewModel)
        {
            _appViewModel = appViewModel;
        }

        #endregion

        /// <summary>
        /// Makes a savegame of the currently playing cartridge, eventually
        /// prompting the user to customize the metadata of the cartridge.
        /// </summary>
        /// <param name="isAutoSave">If true, no prompting is done, and
        /// the savegame is marked as autosave.</param>
        public void SaveGame(bool isAutoSave)
        {
            // Gets a new random CartridgeSavegame.
            CartridgeTag tag = GetCurrentTag();
            CartridgeSavegame cs = new CartridgeSavegame(tag)
            {
                IsAutosave = isAutoSave
            };

            // Shows progress to the user.
            _appViewModel.SetSystemTrayProgressIndicator("Saving game...");

            // Performs the savegame.
            _appViewModel.Model.Core.Save(cs);

            // Shows progress to the user.
            _appViewModel.SetSystemTrayProgressIndicator(isVisible: false);

            // If this is a manual save, shows a message box.
            if (!isAutoSave)
            {
                // What happens next depends on the result of this message box.
                ShowNewSavegameMessageBox(cs);
            }
            else
            {
                // Adds the savegame to the tag.
                tag.AddSavegame(cs);
            }
        }

        #region New Savegame Prompt

        private void ShowNewSavegameMessageBox(CartridgeSavegame cs)
        {
            if (cs == null)
            {
                throw new ArgumentNullException("cs");
            }

            // Creates a custom message box.
            CustomMessageBox cmb = new CustomMessageBox()
            {
                Caption = "Savegame",
                Content = new Controls.SavegameMessageBoxContentControl() { Savegame = cs },
                LeftButtonContent = "OK",
                RightButtonContent = "Cancel"
            };

            // Adds event handlers.
            cmb.Dismissed += new EventHandler<DismissedEventArgs>(OnSavegameCustomMessageBoxDismissed);

            // Shows the message box.
            cmb.Show();
        }

        private void OnSavegameCustomMessageBoxDismissed(object sender, DismissedEventArgs e)
        {
            CustomMessageBox cmb = (CustomMessageBox)sender;

            // Unregisters events.
            cmb.Dismissed -= new EventHandler<DismissedEventArgs>(OnSavegameCustomMessageBoxDismissed);

            // Only moves on if OK has been pushed.
            if (e.Result != CustomMessageBoxResult.LeftButton)
            {
                return;
            }

            // Gets the associated savegame.
            Controls.SavegameMessageBoxContentControl content = cmb.Content as Controls.SavegameMessageBoxContentControl;
            if (content == null)
            {
                throw new InvalidOperationException("Message box has no SavegameMessageBoxContentControl.");
            }
            CartridgeSavegame cs = content.Savegame;
            if (cs == null)
            {
                throw new InvalidOperationException("SavegameMessageBoxContentControl has no CartridgeSavegame.");
            }
            
            // If the name already exists, asks if the old savegame should be replaced.
            CartridgeSavegame oldCSWithSameName = GetSavegameByName(content.Name);
            if (oldCSWithSameName != null)
            {
                // Asks for replacing the savegame.
                if (MessageBox.Show(
                        String.Format("A savegame named {0} already exists for this cartridge. Do you want to override it?", content.Name),
                        "Replace savegame?", 
                        MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    // Go: deletes the old savegame and continues.
                    GetCurrentTag().RemoveSavegame(oldCSWithSameName);
                }
                else
                {
                    // No-go: prompt for another name.
                    ShowNewSavegameMessageBox(cs);

                    // Don't go further
                    return;
                }
            }

            // Edits the savegame.
            cs.Name = content.Name;
            //cs.HashBrush = content.HashBrush;

            // Commit.
            cs.ExportToIsoStore();

            // Adds an history entry for this savegame.
            CartridgeTag tag = _appViewModel.Model.CartridgeStore.GetCartridgeTag(_appViewModel.Model.Core.Cartridge);
            _appViewModel.Model.History.AddSavedGame(tag, cs);

            // Adds the savegame to the tag.
            tag.AddSavegame(cs);
        }

        #endregion

        #region Savegame Management

        private CartridgeTag GetCurrentTag()
        {
            // Returns the tag of the currently playing cartridge.
            return _appViewModel.Model.CartridgeStore.GetCartridgeTag(_appViewModel.Model.Core.Cartridge);
        }

        private CartridgeSavegame GetSavegameByName(string name)
        {
            // Returns the savegame by name, if it exists.
            return GetCurrentTag().GetSavegameByNameOrDefault(name);
        }

        #endregion
    }
}
