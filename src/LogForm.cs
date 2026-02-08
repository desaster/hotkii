using System.Drawing;
using System.Windows.Forms;

namespace Hotkii;

class LogForm : Form
{
    private TextBox textBox;

    public LogForm()
    {
        Text = "Hotkii - Debug Log";
        Size = new Size(700, 500);
        StartPosition = FormStartPosition.CenterScreen;

        textBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9),
            BackColor = Color.White,
            WordWrap = false
        };

        Controls.Add(textBox);
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);

        if (Visible) {
            textBox.Text = Log.GetAll();
            ScrollToEnd();
            Log.Subscribe(OnLogMessage);
        } else {
            Log.Unsubscribe(OnLogMessage);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing) {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnFormClosing(e);
    }

    private void OnLogMessage(string line)
    {
        if (InvokeRequired) {
            Invoke(() => OnLogMessage(line));
            return;
        }

        textBox.AppendText(line + Environment.NewLine);
        ScrollToEnd();
    }

    private void ScrollToEnd()
    {
        textBox.SelectionStart = textBox.TextLength;
        textBox.ScrollToCaret();
    }
}
