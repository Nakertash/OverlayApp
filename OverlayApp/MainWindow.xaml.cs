using OverlayApp.Models;
using OverlayApp.Modals;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Newtonsoft.Json;

namespace OverlayApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly TimeSpan _tick = TimeSpan.FromMilliseconds(100);
        private readonly PeriodicTimer _timer;
        private string _logFile = string.Empty;
        private long _lastBytes = 0;
        private FileSystemWatcher _watcher;
        private FileStream _fs;
        private List<DamageWithTimeStamp> _damage = new List<DamageWithTimeStamp>();
        private List<string> _pets = new List<string>();
        private DamageWithTimeStamp? _firstBlood;
        private bool _isTracking = true;

        public MainWindow()
        {
            _timer = new PeriodicTimer(_tick);
            InitializeComponent();
            Loaded += OnLoaded;
            ReloadFile();
            _ = RunLoop();
            Closed += (sender, args) =>
            {
                _timer.Dispose();
                if (_watcher != null)
                    _watcher.Dispose();

                if (_fs != null)
                    _fs.Dispose();
            };
            if (!File.Exists("./app_settings.json"))
            {
                File.WriteAllText("./app_settings.json", JsonConvert.SerializeObject(new SettingsModel()));
            }
            var settings = JsonConvert.DeserializeObject<SettingsModel>(File.ReadAllText("./app_settings.json"));
            if (settings.Pets == null)
            {
                settings.Pets = new List<string>();
            }
            _pets = settings.Pets;
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this; // чтобы окно знало "родителя" (опционально)
            if (settingsWindow.ShowDialog() == true)
            {
                var result = settingsWindow.settingsModel;
                _pets = result.Pets;

            }
        }
        private void ReloadFile()
        {
            if (!File.Exists("./path.txt"))
            {
                File.Create("./path.txt");
                MessageBox.Show($"Впишите в файл path.txt путь к папке с логами");
                Close();
                return;
            }
            var configPath = File.ReadAllText("./path.txt");
            if (_fs != null)
            {
                _fs.Close();
            }
            if (_watcher != null)
            {
                _watcher.Dispose();
            }
            _logFile = FindLogFile(configPath);
            if (string.IsNullOrEmpty(_logFile))
            {
                Close();
            }
            _watcher = new FileSystemWatcher(Path.GetDirectoryName(_logFile))
            {
                Filter = Path.GetFileName(_logFile),
                NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite
            };
            _fs = new FileStream(_logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _watcher.Changed += (_, __) => CheckLog();
            _watcher.EnableRaisingEvents = true;
            _lastBytes = new FileInfo(_logFile).Length;
            MessageBox.Show($"Файл {_logFile} выбран");
        }
        private string FindLogFile(string folderPath)
        {
            var result = string.Empty;
            var files = Directory.GetFiles(folderPath);
            var lastDate = DateTime.MinValue;
            var lastId = 0;
            var logs = new List<LOTROLogFile>();
            foreach (var file in files)
            {
                var name = System.IO.Path.GetFileName(file);

                if (name.Contains("_") && name.Contains(".txt"))
                {
                    var parts = name.Split('_');
                    var id = int.Parse(parts[2].Replace(".txt", ""));
                    var date = DateTime.Parse(parts[1].Substring(0, 4) + "-" + parts[1].Substring(4, 2) + "-" + parts[1].Substring(6, 2));
                    logs.Add(new LOTROLogFile()
                    {
                        Date = date,
                        ID = id,
                        Path = file
                    });
                }
            }
            var lastLog = logs.OrderByDescending(x => x.Date).ThenByDescending(x => x.ID).FirstOrDefault();
            result = lastLog?.Path ?? string.Empty;
            return result;
        }
        private void CheckLog()
        {
            if (_fs == null) return;

            long fileSize = _fs.Length;

            if (fileSize < _lastBytes)
            {
                // файл был очищен или перезаписан
                _fs.Seek(0, SeekOrigin.Begin);
                _lastBytes = 0;
                fileSize = _fs.Length;
            }

            if (fileSize > _lastBytes)
            {
                _fs.Seek(_lastBytes, SeekOrigin.Begin);
                byte[] buffer = new byte[fileSize - _lastBytes];
                int bytesRead = _fs.Read(buffer, 0, buffer.Length);
                var text = System.Text.Encoding.Default.GetString(buffer, 0, bytesRead);
                var lines = text.Split('\n');
                foreach (var line in lines)
                {
                    if (line.ToLower().Contains("терпит урон")
                        || line.ToLower().Contains("you hit")
                        || _pets.Any(x => line.ToLower().Contains(x.ToLower() + " hits"))
                        )
                    {
                        var words = line.Split(' ');
                        var numstr = words.FirstOrDefault(x => int.TryParse(x, out _));
                        if (string.IsNullOrEmpty(numstr)) { return; }
                        var num = int.Parse(numstr);
                        var newBlood = new DamageWithTimeStamp
                        {
                            Damage = num,
                            Time = DateTime.Now.TimeOfDay
                        };
                        _damage.Add(newBlood);
                        if (_firstBlood == null)
                        {
                            _firstBlood = newBlood;
                        }
                    }
                }
                _lastBytes = fileSize;

            }
        }
        private async Task RunLoop()
        {
            try
            {
                while (await _timer.WaitForNextTickAsync())
                {
                    var now = DateTime.Now.TimeOfDay;
                    if (_firstBlood == null || !_isTracking) continue;
                    var delta = (int)(now - _firstBlood.Time).TotalSeconds;
                    dpsLabel.Content = Math.Round((double)_damage.Sum(x => x.Damage) / delta, 2) + "dps";
                    timeLabel.Content = delta + "s";
                }
            }
            catch (OperationCanceledException)
            {
            }
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
            if (_isTracking)
            {
                _isTracking = false;
                timeLabel.Content = timeLabel.Content + " (stopped)";
                StopStartButton.Content = "Start";
                return;
            }
            _isTracking = true;
            _damage.Clear();
            _firstBlood = null;
            dpsLabel.Content = "0dps";
            timeLabel.Content = "0s";
            StopStartButton.Content = "Stop";
        }
        private void RefreshFile_Click(object sender, RoutedEventArgs e)
        {
            ReloadFile();
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