using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Crappy
{   
    public class Configuration
    {
        public readonly Dictionary<SettingType, Setting> Settings = new Dictionary<SettingType, Setting>();
        private readonly string fileName;

        public Configuration(string fileName)
        {
            this.fileName = fileName;
            Load();
        }

        ~Configuration() => Save();

        //Falta hacer un GetSetting porque estoy entrando al diccionario derecho

        private void Load()
        {
            LoadDefaultSettings();
            LoadSavedSettings();
        }

		private void AddSetting(Setting setting)
        {
            Settings.Add(setting.Type, setting);
        }

        private void LoadDefaultSettings()
        {
            Settings.Clear();

            foreach (Setting setting in GetDefaultSettings())
            {
                AddSetting(setting);
            }
        }

        private Setting[] GetDefaultSettings()
        {
            return new[]
            {
                new Setting 
                { 
                    Type = SettingType.Randomness,
                    Name = "randomness",
                    Value = 1,
                    Minimum = 0,
                    Maximum = 1000,
                    Default = 1
                }
            };
        }

        /// Los settings del archivo tienen que estar dentro de los defaults
        private void LoadSavedSettings()
        {
            if (File.Exists(fileName))
            {
                string fileContent = File.ReadAllText(fileName);
                
                try
                {               
                    List<Setting> settings = JsonConvert.DeserializeObject<List<Setting>>(fileContent);

                    foreach(Setting setting in settings)
                    {
                        if (Settings.ContainsKey(setting.Type))
                        {
                            Settings[setting.Type] = setting;
                        }
                    }
                }
                catch(Exception ex)
                {                   
                    throw new FileLoadException($"Error loading settings file {fileName}", ex);
                }
            }
        }

        public void Save()
        {
            File.WriteAllText(fileName, JsonConvert.SerializeObject(Settings.Values));
        }
    }
}