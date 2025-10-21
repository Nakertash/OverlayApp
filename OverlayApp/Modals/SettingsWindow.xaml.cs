using Newtonsoft.Json;
using OverlayApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OverlayApp.Modals
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsModel? settingsModel = new SettingsModel();
        public ObservableCollection<EditableItem> Items { get; set; }
        public SettingsWindow()
        {
            if (!File.Exists("./app_settings.json"))
            {
                File.WriteAllText("./app_settings.json", JsonConvert.SerializeObject(new SettingsModel()));
            }
            settingsModel = JsonConvert.DeserializeObject<SettingsModel>(File.ReadAllText("./app_settings.json"));
            InitializeComponent();
            Items = [.. settingsModel.Pets.Select(x=> new EditableItem() { Text = x })];

            DataContext = this;
        }

        private void AddPet(object sender, RoutedEventArgs e)
        {
            Items.Add(new EditableItem() { Text = "new pet" });

        }

        private void saveSettings(object sender, RoutedEventArgs e)
        {
            settingsModel.Pets = Items.Select(x => x.Text).ToList();
            File.WriteAllText("./app_settings.json", JsonConvert.SerializeObject(settingsModel));
            DialogResult = true;
        }
        public class EditableItem : INotifyPropertyChanged
        {
            private string _text;
            public string Text
            {
                get => _text;
                set
                {
                    if (_text != value)
                    {
                        _text = value;
                        OnPropertyChanged(nameof(Text));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
