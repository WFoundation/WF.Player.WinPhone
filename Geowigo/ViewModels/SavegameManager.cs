﻿using System;
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

            // Performs the savegame.
			_appViewModel.Model.Core.SaveAsync(cs)
				.ContinueWith(t =>
				{
					// If the savegame failed, display a message.
                    if (!t.Result)
                    {
                        MessageBox.Show("An error occured while preparing the savegame. Please try again.", "Error", MessageBoxButton.OK);
                        return;
                    }
                    
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
				}, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Makes a quick savegame of the currently playing cartridge, eventually replacing an existing
        /// quick savegame for the current game session.
        /// </summary>
        public void SaveGameQuick()
        {
            // Gets the quick save for the current cartridge.
            CartridgeTag tag = GetCurrentTag();
            CartridgeSavegame savegame;
            if (!_quickSaves.TryGetValue(tag, out savegame))
            {
                // If there's none, makes one.
                savegame = MakeQuickSave(tag);
            }

            // Saves the game.
            _appViewModel.Model.Core.SaveAsync(savegame)
                .ContinueWith(t =>
                {
                    // If the savegame failed, display a message.
                    if (!t.Result)
                    {
                        MessageBox.Show("An error occured while preparing the savegame. Please try again.", "Error", MessageBoxButton.OK);
                        return;
                    }

                    // Updates the savegame's attributes.
                    savegame.Timestamp = DateTime.Now;

                    // Saves the savegame to the store and adds it to the tag if needed.
                    tag.RefreshOrAddSavegame(savegame);

                    // Ensures there's a unique history entry for this savegame.
                    History history = _appViewModel.Model.History;
                    history.RemoveAllOf(tag.Guid, savegame.Name, HistoryEntry.Type.Saved);
                    history.AddSavedGame(tag, savegame);

                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Initializes a quick save for a game session.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="savegameCandidate"></param>
        public void InitQuickSave(CartridgeTag tag, CartridgeSavegame savegameCandidate)
        {
            // Uses the candidate savegame if it is a quicksave. Otherwise makes one new.
            if (savegameCandidate != null && savegameCandidate.IsQuicksave)
            {
                _quickSaves[tag] = savegameCandidate;
            }
            else
            {
                MakeQuickSave(tag);
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

        private CartridgeSavegame MakeQuickSave(CartridgeTag tag)
        {
            // Makes a savegame.
            string nameRoot = "Quick Save";
            string intPattern = " {0}";
            int quickSaveId = tag.GetLastSavegameNameInteger(nameRoot, intPattern) + 1;
            CartridgeSavegame cs = new CartridgeSavegame(tag, nameRoot + String.Format(intPattern, quickSaveId))
            {
                IsQuicksave = true
            };

            // Sets it as the current quick save for the tag.
            _quickSaves[tag] = cs;

            // Returns
            return cs;
        }

        #endregion
    }
}
