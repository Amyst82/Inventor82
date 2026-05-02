using System;

namespace Inventor82
{
    public static class Standalone
    {
        public static Inventor.Application m_inventorApplication;
        public static void Cleanup()
        {
            m_inventorApplication = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
