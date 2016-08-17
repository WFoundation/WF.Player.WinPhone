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
using System.Collections.Generic;
using Geowigo.Utils;

namespace Geowigo.ViewModels
{
    /// <summary>
    /// A manager for creating, renaming savegames.
    /// </summary>
    public class SavegameManager
    {

        #region Members

        private AppViewModel _appViewModel;
        private Dictionary<CartridgeTag, CartridgeSavegame> _quickSaves = new Dictionary<CartridgeTag, CartridgeSavegame>();
        private Dictionary<CartridgeTag, CartridgeSavegame> _autoSaves = new Dictionary<CartridgeTag, CartridgeSavegame>();

        #endregion

        #region Constructors

        public SavegameManager(AppViewModel appViewModel)
        {
            _appViewModel = appViewModel;
        }

        #endregion

        /// <summary>
        /// Makes a savegame of the currently playing cartridge and prompts the user for a name.
        /// </summary>
        public void SaveAndPrompt()
        {
            // Creates a savegame container
            CartridgeTag tag = GetCurrentTag();
            CartridgeSavegame cs = new CartridgeSavegame(tag);

            // Saves the game, displays a prompt, and a message in case of error.
            SaveCore(tag, cs, (t,c) => ShowNewSavegameMessageBox(c), true);
        }

        /// <summary>
        /// Makes an auto savegame of the currently playing cartridge, eventually replacing an existing
        /// auto savegame for the current game session.
        /// </summary>
        public void SaveAuto()
        {
            SaveCore(GetCurrentTag(), _autoSaves, CreateAutoSavegame, false);
        }

        /// <summary>
        /// Makes a quick savegame of the currently playing cartridge, eventually replacing an existing
        /// quick savegame for the current game session.
        /// </summary>
        public void SaveQuick()
        {
            SaveCore(GetCurrentTag(), _quickSaves, CreateQuickSavegame, true);
        }

        /// <summary>
        /// Initializes quick and auto savegames for a new game session.
        /// </summary>
        /// <param name="tag">Cartridge being played</param>
        /// <param name="savegameCandidate">Savegame that started the game session, or null if it is a new game.</param>
        public void InitSessionSavegames(CartridgeTag tag, CartridgeSavegame savegameCandidate)
        {
            // Inits the session's quick save from the restored savegame if it is a quicksave, or makes a new one if not.
            if (savegameCandidate != null && savegameCandidate.IsQuicksave)
            {
                _quickSaves[tag] = savegameCandidate;
            }
            else
            {
                CreateQuickSavegame(tag);
            }

            // Inits the session's auto save from the restored savegame if it is an autosave, or makes a new one if not.
            if (savegameCandidate != null && savegameCandidate.IsAutosave)
            {
                _autoSaves[tag] = savegameCandidate;
            }
            else
            {
                CreateAutoSavegame(tag);
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
			_appViewModel.MessageBoxManager.AcceptAndShow(cmb);
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
            CartridgeTag tag = GetCurrentTag();
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
                    tag.RemoveSavegame(oldCSWithSameName);
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
            cs.HashColor = content.HashColor;

            // Commit.
            cs.ExportToIsoStore();

            // Adds an history entry for this savegame.
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

        private CartridgeSavegame CreateQuickSavegame(CartridgeTag tag)
        {
            return CreateSavegame(tag, "Quick Save", " {0}", true, false, _quickSaves);
        }

        private CartridgeSavegame CreateAutoSavegame(CartridgeTag tag)
        {
            return CreateSavegame(tag, "Auto Save", " {0}", false, true, _autoSaves);
        }

        private CartridgeSavegame CreateSavegame(CartridgeTag tag, string nameRoot, string suffixFormat, bool isQuickSave, bool isAutoSave, Dictionary<CartridgeTag, CartridgeSavegame> dict)
        {
            if (!isQuickSave && !isAutoSave)
            {
                throw new InvalidOperationException("Savegame must be either quick save or auto save");
            }
            
            // Makes a savegame.
            string intPattern = " {0}";
            int saveId = tag.GetLastSavegameNameInteger(nameRoot, intPattern) + 1;
            CartridgeSavegame cs = new CartridgeSavegame(tag, nameRoot + String.Format(intPattern, saveId))
            {
                IsQuicksave = isQuickSave,
                IsAutosave = isAutoSave
            };

            // Sets it as the current save for the tag.
            dict[tag] = cs;

            // Returns
            return cs;
        }

        #endregion

        #region Savegame Execution

        private void SaveCore(
            CartridgeTag tag,
            Dictionary<CartridgeTag, CartridgeSavegame> savegameDict,
            Func<CartridgeTag, CartridgeSavegame> generator,
            bool displayError)
        {
            // Gets the current session' preferred save for the cartridge.
            CartridgeSavegame savegame;
            if (!savegameDict.TryGetValue(tag, out savegame))
            {
                // If there's none, makes one.
                savegame = generator(tag);
            }

            // Saves the game, updates the savegame metadata and cleans the history.
            SaveCore(tag, savegame, (t, s) =>
            {
                // Updates the savegame's attributes.
                s.Timestamp = DateTime.Now;

                // Saves the savegame to the store and adds it to the tag if needed.
                t.RefreshOrAddSavegame(s);

                // Ensures there's a unique history entry for this savegame.
                History history = _appViewModel.Model.History;
                history.RemoveAllOf(t.Guid, s.Name, HistoryEntry.Type.Saved);
                history.AddSavedGame(t, s);
            }, displayError);
        }

        private void SaveCore(
            CartridgeTag tag,
            CartridgeSavegame cs,
            Action<CartridgeTag, CartridgeSavegame> continuation,
            bool displayError)
        {
            try
            {
                // Performs the savegame.
                _appViewModel.Model.Core.SaveAsync(cs)
                    .ContinueWith(t =>
                    {
                        // Continues.
                        continuation(tag, cs);

                    }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                // Keeps track of the error.
                DebugUtils.DumpException(ex);

                // Displays an error if needed.
                if (displayError)
                {
                    MessageBox.Show("An error occured while preparing the savegame. Please try again.", "Error", MessageBoxButton.OK);
                }
            }
        }

        #endregion
    }
}
