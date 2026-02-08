using System.Windows.Forms;

namespace Hotkii;

static class Program
{
    [STAThread]
    static void Main()
    {
        Console.WriteLine($"Hotkii {BuildVersion.Git}");

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayForm());

        Console.WriteLine("Hotkii exiting.");
    }
}
