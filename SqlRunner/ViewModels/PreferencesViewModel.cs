using SqlRunner.Extensions;
using SqlRunner.Handlers;
using SqlRunner.Models;
using SqlRunner.Utils;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace SqlRunner.ViewModels
{
    internal class PreferencesViewModel : INotifyPropertyChanged
    {
        private string _connectionString;
        private ICommand _saveCommand;

        public event PropertyChangedEventHandler PropertyChanged;

        public string ConnectionString
        {
            get => _connectionString;
            set => PropertyChanged.ChangeAndNotify(ref _connectionString, value, () => ConnectionString);
        }

        public ICommand SaveCommand => _saveCommand ??= new CommandHandler<Window>((w) => SavePreferences(w));

        public PreferencesViewModel()
        {
            Preferences preferences = Serializer.Deserialize<Preferences>(Settings.PreferencesPath);
            ConnectionString = preferences.ConnectionString;
        }

        private void SavePreferences(Window window)
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                MessageBox.Show("Connection String cannot be empty");
                return;
            }

            Preferences preferences = new()
            {
                ConnectionString = ConnectionString
            };

            Serializer.Serialize(Settings.PreferencesPath, preferences);
            window.Close();
        }
    }
}
