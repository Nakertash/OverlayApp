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
            Loaded += OnLoaded;
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            // Устанавливаем стили окна
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_TOPMOST;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

            // Настраиваем прозрачность (255 = непрозрачно, 0 = полностью прозрачно)
            //SetLayeredWindowAttributes(hwnd, 0, 255, LWA_ALPHA);
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

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
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TOPMOST = 0x00000008;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOACTIVATE = 0x0010;
        private const int WM_SYSCOMMAND = 0x112;
        private const int TPM_LEFTALIGN = 0x0000;
        private const int TPM_RETURNCMD = 0x0100;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int LWA_ALPHA = 0x2;
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter,
            int x, int y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern int TrackPopupMenuEx(IntPtr hMenu, int uFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    }
}