using System;
using System.Runtime.InteropServices;
using Inventor;

namespace Inventor82
{
    [GuidAttribute("4b25f873-a1fb-423b-92f0-96a4df264b55")]
    public class StandardAddInServer : ApplicationAddInServer
    {

        public StandardAddInServer()
        {
        }
        ApiServer _apiServer;
        public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            Standalone.m_inventorApplication = addInSiteObject.Application;
            UIHelper.CreatExportStlButton();
            PartContextMenuHook.SetupContextMenuHook();
            _apiServer = new ApiServer(13382);
            _apiServer.Start();
        }

        public void Deactivate()
        {
            _apiServer.Dispose();
            PartContextMenuHook.Cleanup();
            Standalone.Cleanup();
        }
        #region ApplicationAddInServer Members
        public void ExecuteCommand(int commandID)
        {
        }
        public object Automation => null;

        #endregion

    }
}
