using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Crappy
{   
    public class Configuration
    {
        private static Configuration _instance;
        public static Configuration Get()
        {
            if (_instance is null)
            {
                _instance = new Configuration();
            }

            return _instance;
        }

        public readonly Dictionary<SettingType, Setting> Settings = new Dictionary<SettingType, Setting>();
        private string _fileName;
        public string FileName 
        {
            get => _fileName;
            set 
            {
                _fileName = value;
                Load();
            }
        }

        private Configuration(){}

        ~Configuration() => Save();
       
        public T GetValue<T>(SettingType type)
        {
            if (Settings.TryGetValue(type, out Setting setting))
            {
                try
                {
                    return (T)Convert.ChangeType(setting.Value, typeof(T));
                }
                catch(Exception ex)
                {
                    throw new ArgumentException($"Error coverting setting {setting.SettingType} to {typeof(T)}", ex);
                }
            }
            else
            {
                throw new ArgumentException($"Setting not found: {type}");
            }
        }

        private void Load()
        {
            Settings.Clear();
            LoadDefaultSettings();
            LoadSavedSettings();
        }

        private void LoadDefaultSettings()
        {
            foreach (Setting setting in GetDefaultSettings())
            {
                Settings.Add(setting.SettingType, setting);
            }
        }

        private Setting[] GetDefaultSettings()
        {
            return new[]
            {
                new Setting 
                { 
                    SettingType = SettingType.Randomness,
                    OptionType = OptionType.Spin,
                    Value = "1",
                    Minimum = 0,
                    Maximum = 1000,
                    Default = "1"
                },
                new Setting
                {
                    SettingType = SettingType.LogToFile,
                    OptionType = OptionType.Check,
                    Value = "false",
                    Default = "false"
                }
            };
        }

        /// Los settings del archivo tienen que estar dentro de los defaults
        private void LoadSavedSettings()
        {
            if (File.Exists(_fileName))
            {
                string fileContent = File.ReadAllText(_fileName);
                
                try
                {               
                    List<Setting> settings = JsonConvert.DeserializeObject<List<Setting>>(fileContent);

                    foreach(Setting setting in settings)
                    {
                        if (Settings.ContainsKey(setting.SettingType))
                        {
                            Settings[setting.SettingType] = setting;
                        }
                    }
                }
                catch(Exception ex)
                {                   
                    throw new FileLoadException($"Error loading settings file {_fileName}", ex);
                }
            }
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(_fileName, JsonConvert.SerializeObject(Settings.Values));
            }
            catch(Exception ex)
            {
                throw new Exception($"Error saving configuration file '{_fileName}'", ex);
            }
        }
    }
}