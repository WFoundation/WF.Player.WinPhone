using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Geowigo.ViewModels;
using System.Windows.Data;
using System.Windows.Input;

namespace Geowigo.Views
{
    public partial class HelpPage : BasePage
    {
        #region Properties

        #region ViewModel

        public new HelpViewModel ViewModel
        {
            get
            {
                return (HelpViewModel)base.ViewModel;
            }
        }

        #endregion

        #endregion
        
        public HelpPage()
        {
            InitializeComponent();
            
            // Initial visual state.
            SetDisplayMode(ViewModel.DisplayMode);

            // Listens for view model events.
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.ReportFormFlushRequested += ViewModel_ReportFormFlushRequested;
        }

        private void ViewModel_ReportFormFlushRequested(object sender, EventArgs e)
        {
            // The report TextBox doesn't update its source binding if it's focused.
            // If it is currently focused, request an immediate update of the binding.
            if (DetailsTextBox != null && FocusManager.GetFocusedElement() == DetailsTextBox)
            {
                BindingExpression expr = DetailsTextBox.GetBindingExpression(TextBox.TextProperty);
                if (expr != null)
                {
                    expr.UpdateSource();
                }
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DisplayMode")
            {
                SetDisplayMode(ViewModel.DisplayMode);
            }
        }

        private void SetDisplayMode(HelpViewModel.Mode mode)
        {
            // Gets the target visual state.
            string visualState = null;
            switch (mode)
            {
                case HelpViewModel.Mode.Menu:
                    visualState = "ContentPanelMenuState";
                    break;

                case HelpViewModel.Mode.BugReport:
                    visualState = "ContentPanelReportState";
                    break;

                default:
                    break;
            }
            if (visualState == null)
            {
                return;
            }

            // Transitions to the right state.
            VisualStateManager.GoToState(this, visualState, true);
        }

    }
}