using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OverlayApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Button clicked!");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Позволяет перетаскивать окно за любую область
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
        private void TitleBar_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            Point pos = e.GetPosition(this);

            // Преобразуем координаты WPF -> экранные
            Point screenPoint = PointToScreen(pos);

            IntPtr hMenu = GetSystemMenu(hwnd, false);
            int cmd = TrackPopupMenuEx(
                hMenu,
                TPM_LEFTALIGN | TPM_RETURNCMD,
                (int)screenPoint.X,
                (int)screenPoint.Y,
                hwnd,
                IntPtr.Zero);

            if (cmd != 0)
                PostMessage(hwnd, WM_SYSCOMMAND, new IntPtr(cmd), IntPtr.Zero);
        }

        // === WinAPI ===
        private const int WM_SYSCOMMAND = 0x112;
        private const int TPM_LEFTALIGN = 0x0000;
        private const int TPM_RETURNCMD = 0x0100;

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern int TrackPopupMenuEx(IntPtr hMenu, int uFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    }
}