using System;
using System.Windows.Forms;

namespace LoLBR_OfflineChat;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}