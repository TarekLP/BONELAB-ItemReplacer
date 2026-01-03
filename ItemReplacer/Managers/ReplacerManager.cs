using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

using Newtonsoft.Json;

using ItemReplacer.Helpers;
using ItemReplacer.Utilities;

using System.ComponentModel;
using JsonException = System.Text.Json.JsonException;

namespace ItemReplacer.Managers
{
    // This shit is just copied from KeepInventory
    // I am NOT doing this from scratch, just doing this in KeepInventory was shitty enough and took eternity to work
    // - HAHOOS
    public static class ReplacerManager
    {
        private static readonly List<ReplacerConfig> _configs = [];

        public static IReadOnlyList<ReplacerConfig> Configs => _configs.AsReadOnly();

        internal static List<string> IgnoredFilePaths { get; } = [];

        public static string ConfigsDir => Path.Combine(PreferencesManager.ConfigDir, "Configs");

        private static SynchronousFileSystemWatcher FileSystemWatcher { get; set; }

        public static void Setup()
        {
            PreferencesManager.EnsureFolder();

            if (!Directory.Exists(ConfigsDir))
            {
                Core.Logger.Msg("Creating missing folder in Config directory");
                Directory.CreateDirectory(ConfigsDir);
            }

            Core.Logger.Msg("Loading configs from directory...");
            var files = Directory.GetFiles(ConfigsDir);
            if (files?.Length > 0)
            {
                foreach (var item in files)
                {
                    try
                    {
                        if (Check(item)) Register(item);
                        else Core.Logger.Error($"Attempted to load {Path.GetFileName(item)}, but it failed the check");
                    }
                    catch (Exception ex)
                    {
                        Core.Logger.Error($"An unexpected error has occurred while loading '{Path.GetFileName(item)}'", ex);
                    }
                }
            }
        }

        public static void Register(ReplacerConfig config, bool saveToFile = true)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Config cannot be null.");
            }

            if (config.Categories.Count == 0)
            {
                throw new ArgumentNullException(nameof(config), "Config must have a category");
            }
            Core.Logger.Msg($"Registering replacer, ID: '{config.ID}'");
            if (_configs.Exists(c => c.ID == config.ID))
            {
                Core.Logger.Error("A replacer with the same ID is already registered.");
                throw new ArgumentException($"A replacer with the ID '{config.ID}' is already registered.", nameof(config));
            }

            _configs.Add(config);

            if (saveToFile && string.IsNullOrWhiteSpace(config.FilePath))
            {
                Core.Logger.Msg("Saving to file");
                string path = Path.Combine(ConfigsDir, $"{config.ID}.json");
                IgnoredFilePaths.Add(path);
                var file = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                config.FilePath = path;
                var serialized = JsonConvert.SerializeObject(config, Formatting.Indented);
                file.Write(Encoding.UTF8.GetBytes(serialized));
                file.Flush();
                file.Position = 0;
                file.Dispose();

            }

            MenuManager.SetupReplacers();
        }

        public static void Register(string filePath)
        {
            Core.Logger.Msg($"Registering replacer from file: '{filePath}'");
            if (File.Exists(filePath))
            {
                string text = ReadAllTextUsedFile(filePath);
                if (string.IsNullOrWhiteSpace(text) || !IsJSON(text))
                {
                    throw new ArgumentException("The contents of the file are not JSON");
                }
                else
                {
                    var config = JsonConvert.DeserializeObject<ReplacerConfig>(text);
                    if (config != null)
                    {
                        config.FilePath = filePath;
                        if (Configs.Any(x => x.FilePath == filePath))
                            return;

                        Register(config, false);
                    }
                }
            }
            else
            {
                throw new FileNotFoundException($"Could not find file {filePath}");
            }
        }

        private static bool Check(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path)) throw new FileNotFoundException($"Config file at '{path}' could not be found");
            string text;
            try
            {
                text = ReadAllTextUsedFile(path);
            }
            catch (Exception ex)
            {
                Core.Logger.Error("Unable to check the integrity of the file, because an unexpected error has occurred", ex);
                return false;
            }
            if (string.IsNullOrWhiteSpace(text) || !IsJSON(text) || text == "{}")
            {
                Core.Logger.Error("The content of the file is not correct");
                return false;
            }
            else
            {
                try
                {
                    return DeserializeAndCheck(path, text);
                }
                catch (Exception ex)
                {
                    Core.Logger.Error("An unexpected error has occurred while deserializing config", ex);
                    return false;
                }
            }
        }

        private static bool DeserializeAndCheck(string path, string text)
        {
            var config = JsonConvert.DeserializeObject<ReplacerConfig>(text);
            if (config != null)
            {
                if (!string.IsNullOrWhiteSpace(config.ID))
                {
                    if (Configs.Any(x => x.ID == config.ID && x.FilePath != path))
                    {
                        Core.Logger.Error("The ID is already used in another replacer");
                        return false;
                    }
                }
                else
                {
                    Core.Logger.Error($"The ID '{config.ID}' is null or empty");
                    return false;
                }
            }
            else
            {
                Core.Logger.Error("Deserialized config is null");
                return false;
            }
            return true;
        }

        private static readonly Dictionary<string, DateTime> LastWrite = [];

        internal static bool PreventDoubleTrigger(string file)
        {
            if (!File.Exists(file))
            {
                LastWrite.Remove(file);
                return false;
            }

            var write = File.GetLastWriteTime(file);
            if (!LastWrite.ContainsKey(file))
            {
                LastWrite.Add(file, write);
                return IsIgnored(file);
            }
            else
            {
                bool equal = LastWrite[file] == write;
                LastWrite[file] = write;
                return equal || IsIgnored(file);
            }
        }

        internal static bool IsIgnored(string path)
        {
            string fullPath = Path.GetFullPath(path);

            if (!IgnoredFilePaths.Any())
                return false;

            return IgnoredFilePaths.Any(x =>
            {
                var _path = Path.GetFullPath(x);
                bool equal = _path == fullPath;
                if (equal)
                    IgnoredFilePaths.Remove(x);
                return equal;
            }
            );
        }

        internal static string ReadAllTextUsedFile(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"Could not read text from '{path}', because it doesn't exist");
            using var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (file != null)
            {
                file.Position = 0;
                using StreamReader reader = new(file);
                return reader.ReadToEnd();
            }
            return null;
        }

        // TODO: Improve file handling, as sometimes it doesn't register the changes correctly
        internal static void CreateFileWatcher()
        {
            LastWrite.Clear();
            FileSystemWatcher?.Dispose();
            FileSystemWatcher = new SynchronousFileSystemWatcher(ConfigsDir)
            {
                EnableRaisingEvents = true,
                Filter = "*.json"
            };
            FileSystemWatcher.Error += (x, y) => Core.Logger.Error("An unexpected error was thrown by the file watcher for the configs", y.GetException());
            FileSystemWatcher.Deleted += (x, y) =>
            {
                if (IsIgnored(y.FullPath)) return;
                LastWrite.Remove(y.FullPath);
                Core.Logger.Msg($"{y.Name} has been deleted, unregistering replacer");
                Configs.Where(x => x.FilePath == y.FullPath).ForEach(x => Unregister(x.ID, false));
            };
            FileSystemWatcher.Created += (x, y) =>
            {
                if (IsIgnored(y.FullPath)) return;
                if (Check(y.FullPath))
                {
                    Core.Logger.Msg($"{y.Name} has been created, registering replacer");
                    Register(y.FullPath);
                }
            };
            FileSystemWatcher.Changed += (x, y) =>
            {
                if (PreventDoubleTrigger(y.FullPath)) return;
                if (Check(y.FullPath))
                {
                    Core.Logger.Msg($"{y.Name} has been modified, updating");
                    if (!Update(y.FullPath, x => x.Update(y.FullPath), true))
                    {
                        Core.Logger.Msg($"{y.Name} has been modified, but wasn't registered. Registering replacer");
                        Register(y.FullPath);
                    }
                    var configs = Configs.Where(x => x.AutoUpdate(y.FullPath));
                    configs.ForEach(x => x.Update(y.FullPath));
                }
                else
                {
                    Core.Logger.Error($"{y.Name} was updated, but is not suitable to be a replacer");
                    Update(y.FullPath, x => Unregister(x.ID), true);
                }
                MenuManager.SetupReplacers();

            };
            FileSystemWatcher.Renamed += (x, y) =>
            {
                if (IsIgnored(y.FullPath)) return;
                if (LastWrite.ContainsKey(y.OldFullPath))
                {
                    var old = LastWrite[y.OldFullPath];
                    LastWrite.Remove(y.OldFullPath);
                    LastWrite.Add(y.FullPath, old);
                }
                Core.Logger.Msg($"{y.OldName} has been renamed to {y.Name}, updating information");
                if (!Update(y.OldFullPath, x => x.FilePath = y.FullPath) && Check(y.FullPath))
                {
                    Core.Logger.Msg($"{y.Name} has been renamed to {y.Name}, but wasn't registered. Registering replacer");
                    Register(y.FullPath);

                }

            };
        }

        internal static bool Update(string filePath, Action<ReplacerConfig> action, bool requireFileWatcherOption = false)
        {
            var configs = Configs.Where(x => x.FilePath == filePath);
            if (requireFileWatcherOption && configs.ToList().TrueForAll(x => !x.IsFileWatcherEnabled))
                return true;

            if (configs.Any())
            {
                foreach (var config in configs)
                    action(config);

                return true;
            }
            return false;
        }

        internal static bool IsJSON(string text)
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(text);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public static void Unregister(string ID, bool removeFile = true)
        {
            Core.Logger.Msg($"Unregistering config with ID '{ID}'");
            var config = _configs.FirstOrDefault(x => x.ID == ID);
            if (config != null)
            {
                _configs.Remove(config);
                if (removeFile && !string.IsNullOrWhiteSpace(config.FilePath))
                {
                    Core.Logger.Msg($"Removing file at '{config.FilePath}'");
                    if (File.Exists(config.FilePath))
                        File.Delete(config.FilePath);
                }
                Core.Logger.Msg($"Unregistered config with ID '{ID}'");
            }
            else
            {
                Core.Logger.Error($"A config with ID '{ID}' does not exist!");
                throw new KeyNotFoundException($"Config with ID '{ID}' could not be found!");
            }
        }
    }

    public class ReplacerConfig
    {
        public ReplacerConfig(string name, string color, string id, List<ReplacerCategory> categories, bool enabled = true)
        {
            ID = id;
            Name = name;
            Color = color;
            Categories = categories;
            Enabled = enabled;
        }

        [JsonConstructor]
        public ReplacerConfig()
        {

        }

        [JsonProperty("name")]
        public string Name { get; set; }

        // HEX Color Code
        [JsonProperty("color")]
        public string Color { get; set; }


        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonIgnore]
        public string FilePath { get; set; }

        [JsonIgnore]
        public bool IsFileWatcherEnabled { get; set; } = true;

        [JsonProperty("categories")]
        public List<ReplacerCategory> Categories { get; set; }

        internal bool AutoUpdate(string path) => FilePath == path && IsFileWatcherEnabled;


        public void SaveToFile(bool printMessage = true)
        {
            if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
            {
                ReplacerManager.IgnoredFilePaths.Add(FilePath);
                if (printMessage) Core.Logger.Msg($"Saving '{ID}' to file...");
                try
                {
                    var serialized = JsonConvert.SerializeObject(this, Formatting.Indented);
                    var file = File.Create(FilePath);
                    using var writer = new StreamWriter(file) { AutoFlush = true };
                    file.Position = 0;
                    writer.Write(serialized);
                    writer.DisposeAsync().AsTask().ContinueWith((task) =>
                    {
                        if (task.IsCompletedSuccessfully)
                        {
                            if (printMessage) Core.Logger.Msg($"Saved '{ID}' to file successfully!");
                        }
                        else if (printMessage)
                        {
                            Core.Logger.Error($"Failed to save '{ID}' to file", task.Exception);
                        }
                    });
                }
                catch (Exception ex)
                {
                    if (printMessage) Core.Logger.Error($"Failed to save '{ID}' to file", ex);
                    throw;
                }
            }
            else
            {
                if (printMessage) Core.Logger.Error($"Replacer '{ID}' does not have a file set or it doesn't exist!");
                throw new FileNotFoundException("Replacer does not have a file!");
            }
        }

        public bool TrySaveToFile(bool printMessage = true)
        {
            try
            {
                SaveToFile(printMessage);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal void Update(ReplacerConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);
            if (!string.IsNullOrWhiteSpace(config.ID))
            {
                if (ReplacerManager.Configs.Any(x => x.ID == config.ID && x != this))
                {
                    Core.Logger.Error("The new ID is already used in another replacer, will not overwrite");
                    throw new ArgumentException("The new ID is already used in another replacer, will not overwrite");
                }
                else
                {
                    ID = config.ID;
                }
            }
            else
            {
                Core.Logger.Error("The new ID is null or empty, will not overwrite");
                throw new ArgumentException("The new ID is null or empty, will not overwrite");
            }
            if (Name != config.Name) Name = config.Name;
            if (Color != config.Color) Color = config.Color;
            if (Enabled != config.Enabled) Enabled = config.Enabled;
            if (Categories != config.Categories) Categories = config.Categories;
        }

        internal void Update(string path)
        {
            ReplacerManager.IgnoredFilePaths.Add(FilePath);
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path)) throw new FileNotFoundException($"Save file at '{path}' could be found");
            var text = ReplacerManager.ReadAllTextUsedFile(path);
            if (string.IsNullOrWhiteSpace(text) || !ReplacerManager.IsJSON(text))
            {
                Core.Logger.Error($"A config file at '{path}' was changed and the content are no longer suitable for loading as a replacer. This means that the replacer at runtime will not be overwritten by new content");
                throw new InvalidDataException($"A config file at '{path}' was changed and the content are no longer suitable for loading as a replacer. This means that the replacer at runtime will not be overwritten by new content");
            }
            else
            {
                var save = JsonConvert.DeserializeObject<ReplacerConfig>(text);
                if (save != null)
                {
                    Update(save);
                }
                else
                {
                    Core.Logger.Error($"A save file at '{path}' was changed and the content are no longer suitable for loading as a save. This means that the save at runtime will not be overwritten by new content");
                    throw new InvalidDataException($"A save file at '{path}' was changed and the content are no longer suitable for loading as a save. This means that the save at runtime will not be overwritten by new content");
                }
            }
        }


    }

    public class ReplacerCategory
    {
        [JsonConstructor]
        public ReplacerCategory(string name, List<ReplacerEntry> entries, bool enabled = true)
        {
            Name = name;
            Entries = entries;
            Enabled = enabled;
        }

        public ReplacerCategory(string name)
        {
            Name = name;
            Entries = [];
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("entries")]
        public List<ReplacerEntry> Entries { get; set; }
    }

    [method: JsonConstructor]
    public class ReplacerEntry(string original, string replaceWith, bool isRegEx = false)
    {
        [JsonProperty("original")]
        public string Original { get; set; } = original;

        [JsonProperty("replaceWith")]
        public string ReplaceWith { get; set; } = replaceWith;

        [DefaultValue(false)]
        [JsonProperty("isRegEx", DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool IsRegEx { get; set; } = isRegEx;
    }
}
