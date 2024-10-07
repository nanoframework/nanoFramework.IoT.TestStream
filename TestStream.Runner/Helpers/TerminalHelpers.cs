// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
