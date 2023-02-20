using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace llcom
{
    
    public partial class InputDialogWindow : Window
    {
        private const int WM_NCLBUTTONDBLCLK = 0xA3;

        public string Value { get; set; }

        public InputDialogWindow(string prompt, string defaultInput = "", string title = null)
        {
            InitializeComponent();
            this.DataContext = this;
            if (defaultInput == null)
                InputText.Visibility = Visibility.Collapsed;
            else
            {
                this.Value = defaultInput;
                InputText.Visibility = Visibility.Visible;
            }
            this.Title = title ?? TryFindResource("InputDialogTitle") as string ?? "?!";
            SingleButton.Visibility = title == null ? Visibility.Visible : Visibility.Collapsed;
            TwoButtons.Visibility = title != null ? Visibility.Visible : Visibility.Collapsed;
            PromptLabel.Text = prompt;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            Tools.Win32.HideControlBox(handle);
            HwndSource.FromHwnd(handle).AddHook(new HwndSourceHook(WndProc));
            this.InputText.SelectAll();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCLBUTTONDBLCLK)
                handled = true;
            return IntPtr.Zero;
        }
    }
}
