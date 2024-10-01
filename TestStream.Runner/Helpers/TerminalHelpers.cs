using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace nanoFramework.IoT.TestRunner.Helpers
{
    internal class TerminalHelpers
    {
        public static void LogInListView(string msg, List<string> status, ListView lstView)
        {
            status.Add(msg);
            lstView.MoveDown();
            try
            {
                Application.Refresh();
            }
            catch
            {
                // nothing on purpose
            }
        }
    }
}
