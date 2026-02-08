using System.Windows.Forms;

namespace Hotkii;

static class Program
{
    [STAThread]
    static void Main()
    {
        Log.Write($"Hotkii {BuildVersion.Git}");

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayForm());

        Log.Write("Hotkii exiting.");
    }
}
