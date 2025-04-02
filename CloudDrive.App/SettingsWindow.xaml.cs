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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;


namespace CloudDrive.App
{
    public partial class SettingsWindow : Window
    {
        private string _serverUrl = "";
        private static string SettingsFilePath
        {
            get
            {
                var appDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CloudDrive");
                Directory.CreateDirectory(appDataPath);
                return System.IO.Path.Combine(appDataPath, "settings.json");
            }
        }

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        public class ClientSettings
        {
            public required string ServerUrl { get; set; }
            public required string FolderPath { get; set; }
        }

        private void Folder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FolderPathTextBox.Text = folderDialog.SelectedPath;
            }
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = new ClientSettings
            {
                ServerUrl = ServerUrlTextBox.Text,
                FolderPath = FolderPathTextBox.Text
            };

            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(SettingsFilePath, json);

            MessageBox.Show("Ustawienia zapisane!");

            // Po zapisaniu ustawień przejdź do logowania
            new LoginWindow().Show();
            this.Close();
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonConvert.DeserializeObject<ClientSettings>(json);

                if (settings != null)
                {
                    ServerUrlTextBox.Text = settings.ServerUrl ?? string.Empty;
                    FolderPathTextBox.Text = settings.FolderPath ?? string.Empty;

                    // ✅ PRZYPISANIE _serverUrl NA PODSTAWIE ODCZYTANYCH USTAWIEŃ
                    _serverUrl = settings.ServerUrl ?? string.Empty;
                }
            }
        }
    }
}
