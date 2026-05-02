using Inventor;

namespace Inventor82
{
    public static class PartContextMenuHook
    {
        private static ButtonDefinition _showInExplorerButton;
        private static UserInputEvents _userInputEvents;

        public static void SetupContextMenuHook()
        {
            _showInExplorerButton =
                Standalone.m_inventorApplication.CommandManager.ControlDefinitions.AddButtonDefinition(
                Properties.Resources.ShowInExplorer_Name,
                "Inventor82.ShowInExplorer",
                CommandTypesEnum.kFileOperationsCmdType,
                "4b25f873-a1fb-423b-92f0-96a4df264b55",
                Properties.Resources.ShowInExplorer_Description,
                Properties.Resources.ShowInExplorer_Description,
                null,
                null
        );

            _showInExplorerButton.OnExecute += ShowInExplorerButton_OnExecute;

            _userInputEvents = Standalone.m_inventorApplication.CommandManager.UserInputEvents;
            _userInputEvents.OnContextMenu += UserInputEvents_OnContextMenu;
        }
        private static void UserInputEvents_OnContextMenu(SelectionDeviceEnum SelectionDevice, NameValueMap AdditionalInfo, CommandBar CommandBar)
        {
            System.Diagnostics.Debug.WriteLine("Context menu: " + CommandBar.InternalName);
            CommandBar.Controls.AddButton(_showInExplorerButton, 1);
        }
        private static void ShowInExplorerButton_OnExecute(NameValueMap Context)
        {
            Document doc = Standalone.m_inventorApplication.ActiveDocument;
            if (doc == null || string.IsNullOrWhiteSpace(doc.FullFileName))
                return;
            string file = doc.FullFileName;
            System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + file + "\"");
        }
        public static void Cleanup()
        {
            if (_showInExplorerButton != null)
            {
                _showInExplorerButton.OnExecute -= ShowInExplorerButton_OnExecute;
                _showInExplorerButton.Delete();
                _showInExplorerButton = null;
            }
            if (_userInputEvents != null)
            {
                _userInputEvents.OnContextMenu -= UserInputEvents_OnContextMenu;
                _userInputEvents = null;
            }
        }

    }//
}
    
