using Inventor;
using System;
using System.Drawing;
#pragma warning disable CA1416 // Validate platform compatibility
namespace Inventor82
{
    public static class UIHelper
    {
        private static ButtonDefinition _exportStlButton;
        public static void CreatExportStlButton()
        {
            var cmdMgr = Standalone.m_inventorApplication.CommandManager;
            var uiMgr = Standalone.m_inventorApplication.UserInterfaceManager;

            Image img = Properties.Resources.exportSTLicon;
            var smallIcon = PictureDispConverter.ToIPictureDisp(img);
            var largeIcon = smallIcon;
            _exportStlButton = cmdMgr.ControlDefinitions.AddButtonDefinition(
                Properties.Resources.ExportStl_Name,
                "Inventor82.ExportSTL",
                CommandTypesEnum.kFileOperationsCmdType,
                "4b25f873-a1fb-423b-92f0-96a4df264b55",
                Properties.Resources.ExportStl_Description,
                Properties.Resources.ExportStl_Description,
                smallIcon,
                largeIcon
            );

            _exportStlButton.OnExecute += ExportStlButton_OnExecute;

            try
            {
                //Add the button to Part mode into Part tab
                Ribbon partRibbon = uiMgr.Ribbons["Part"];
                RibbonTab partToolsTab = partRibbon.RibbonTabs["id_TabModel"];
                RibbonPanel partPanel;
                //Add the same panel to Assembly mode into Assembly tab
                Ribbon assemblyRibbon = uiMgr.Ribbons["Assembly"];
                RibbonTab assemblyToolsTab = assemblyRibbon.RibbonTabs["id_TabAssemble"];
                RibbonPanel assemblyPanel;
                try
                {
                    partPanel = partToolsTab.RibbonPanels["Inventor82Panel"];
                    assemblyPanel = assemblyToolsTab.RibbonPanels["Inventor82Panel"];
                }
                catch
                {
                    partPanel = partToolsTab.RibbonPanels.Add("Amyst", "Inventor82Panel", "4b25f873-a1fb-423b-92f0-96a4df264b55");
                    assemblyPanel = assemblyToolsTab.RibbonPanels.Add("Amyst", "Inventor82Panel", "4b25f873-a1fb-423b-92f0-96a4df264b55");
                }
                //Add the button to both panels
                partPanel.CommandControls.AddButton(_exportStlButton, true, true, "", false);
                assemblyPanel.CommandControls.AddButton(_exportStlButton, true, true, "", false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to create ribbon UI: " + ex.Message);
            }
        }

        private static void ExportStlButton_OnExecute(NameValueMap Context)
        {
            try
            {
               Executables.ExportActiveDocumentAsStl();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "STL Export",System.Windows.Forms.MessageBoxButtons.OK,System.Windows.Forms.MessageBoxIcon.Error);
            }
        }
    }
}
