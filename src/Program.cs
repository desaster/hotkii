using System.Reflection;
using System.Windows.Forms;

namespace Hotkii;

static class Program
{
    [STAThread]
    static void Main()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        Console.WriteLine($"Hotkii v{version?.ToString(3) ?? "?"}");

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayForm());

        Console.WriteLine("Hotkii exiting.");
    }
}
