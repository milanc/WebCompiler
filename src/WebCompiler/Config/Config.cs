﻿using System.IO;
using Newtonsoft.Json;

namespace WebCompiler
{
    /// <summary>
    /// Represents a configuration object used by the compilers.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// The file path to the configuration file.
        /// </summary>
        [JsonIgnore]
        public string FileName { get; set; }

        /// <summary>
        /// The relative file path to the output file.
        /// </summary>
        [JsonProperty("outputFile")]
        public string OutputFile { get; set; }

        /// <summary>
        /// The relative file path to the input file.
        /// </summary>
        [JsonProperty("inputFile")]
        public string InputFile { get; set; }

        /// <summary>
        /// If true it instructs the compiler to create a minified file on disk.
        /// </summary>
        [JsonProperty("minify")]
        public bool Minify { get; set; } = true;

        /// <summary>
        /// If true it makes Visual Studio include the output file in the project.
        /// </summary>
        [JsonProperty("includeInProject")]
        public bool IncludeInProject { get; set; }

        /// <summary>
        /// If true a source map file is generated for the file types that support it.
        /// </summary>
        [JsonProperty("sourceMap")]
        public bool SourceMap { get; set; }

        internal string Output { get; set; }

        /// <summary>
        /// Converts the relative output file to an absolute file path.
        /// </summary>
        public string GetAbsoluteOutputFile()
        {
            string folder = Path.GetDirectoryName(FileName);
            return Path.Combine(folder, OutputFile.Replace("/", "\\"));
        }
    }
}