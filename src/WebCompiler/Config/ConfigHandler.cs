using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

namespace WebCompiler
{
    /// <summary>
    /// Handles reading and writing config files to disk.
    /// </summary>
    public class ConfigHandler
    {
        private static ConcurrentDictionary<string, ConcurrentDictionary<string, Config>> ExtensionBasedConfigs { get; } = new ConcurrentDictionary<string, ConcurrentDictionary<string, Config>>();

        /// <summary>
        /// Adds a config file if no one exist or adds the specified config to an existing config file.
        /// </summary>
        /// <param name="fileName">The file path of the configuration file.</param>
        /// <param name="config">The compiler config object to add to the configration file.</param>
        public void AddConfig(string fileName, Config config)
        {
            IEnumerable<Config> existing = GetConfigs(fileName);
            List<Config> configs = new List<Config>();
            configs.AddRange(existing);
            configs.Add(config);
            config.FileName = fileName;

            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                DefaultValueHandling = DefaultValueHandling.Ignore,
            };

            string content = JsonConvert.SerializeObject(configs, settings);
            File.WriteAllText(fileName, content, new UTF8Encoding(true));
        }

        /// <summary>
        /// Removes the specified config from the file.
        /// </summary>
        public void RemoveConfig(Config configToRemove)
        {
            IEnumerable<Config> configs = GetConfigs(configToRemove.FileName);
            List<Config> newConfigs = new List<Config>();

            if (configs.Contains(configToRemove))
            {
                newConfigs.AddRange(configs.Where(b => !b.Equals(configToRemove)));
                string content = JsonConvert.SerializeObject(newConfigs, Formatting.Indented);
                File.WriteAllText(configToRemove.FileName, content);
            }
        }

        /// <summary>
        /// Creates a file containing the default compiler options if one doesn't exist.
        /// </summary>
        public void CreateDefaultsFile(string fileName)
        {
            if (File.Exists(fileName))
                return;

            var defaults = new
            {
                compilers = new
                {
                    less = new LessOptions(),
                    sass = new SassOptions(),
                    nodesass = new NodeSassOptions(),
                    stylus = new StylusOptions(),
                    babel = new BabelOptions(),
                    coffeescript = new IcedCoffeeScriptOptions(),
                    handlebars = new HandlebarsOptions(),
                },
                minifiers = new
                {
                    css = new
                    {
                        enabled = true,
                        termSemicolons = true,
                        gzip = false
                    },
                    javascript = new
                    {
                        enabled = true,
                        termSemicolons = true,
                        gzip = false
                    },
                }
            };

            string json = JsonConvert.SerializeObject(defaults, Formatting.Indented);
            File.WriteAllText(fileName, json);
        }

        /// <summary>
        /// Get all the config objects in the specified file.
        /// </summary>
        /// <param name="fileName">A relative or absolute file path to the configuration file.</param>
        /// <param name="changedFile">File name of the changed file</param>
        /// <returns>A list of Config objects.</returns>
        public static IEnumerable<Config> GetConfigs(string fileName, string changedFile = null)
        {
            FileInfo file = new FileInfo(fileName);

            if (!file.Exists)
                return Enumerable.Empty<Config>();

            var content = File.ReadAllText(fileName);
            var configs = JsonConvert.DeserializeObject<IEnumerable<Config>>(content);
            var folder = Path.GetDirectoryName(file.FullName);
            var folderLength = folder.Length + 1;
            var extensionConfigs = new List<Config>();
            foreach (Config config in configs)
            {
                if (!string.IsNullOrEmpty(config.InputExtension))
                {
                    var cacheKey = $"{Path.GetFullPath(fileName)}-{config.InputExtension}";

                    if (!ExtensionBasedConfigs.ContainsKey(cacheKey))
                    {
                        var files = Directory.GetFiles(folder, $"*{config.InputExtension}", SearchOption.AllDirectories);
                        var fileConfigs = files.ToDictionary(f => f, f =>
                        {
                            var inputFile = f.Substring(folderLength);
                            return new Config()
                            {
                                FileName = fileName,
                                InputFile = inputFile,
                                OutputFile = inputFile.Replace(config.InputExtension, config.OutputExtension),
                                InputExtension = null,
                                OutputExtension = null
                            };
                        });

                        ExtensionBasedConfigs.TryAdd(cacheKey, new ConcurrentDictionary<string, Config>(fileConfigs));
                    }
                    else if (changedFile != null && !ExtensionBasedConfigs[cacheKey].ContainsKey(changedFile))
                    {
                        ExtensionBasedConfigs[cacheKey].TryAdd(changedFile, new Config()
                        {
                            FileName = fileName,
                            InputFile = changedFile,
                            OutputFile = changedFile.Replace(config.InputExtension, config.OutputExtension),
                            InputExtension = null,
                            OutputExtension = null
                        });
                    }

                    extensionConfigs.AddRange(ExtensionBasedConfigs[cacheKey].Values);

                    continue;
                }
                config.FileName = fileName;
            }

            return configs.Where(c => string.IsNullOrEmpty(c.InputExtension)).Concat(extensionConfigs);
        }

        /// <summary>
        /// Resets the configs based on input extensions.
        /// </summary>
        public static void ResetExtensionBasedConfigs()
        {
            ExtensionBasedConfigs.Clear();
        }
    }
}
