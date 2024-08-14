using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace CFA
{
    public class FileHandler
    {
        public static string s_basePath;
        public static string s_configFilePath { get; set; }
        public static string s_backupFilePath { get; set; }
        public static object s_config { get; set; }
        public static YamlMappingNode s_root { get; set; }

        public static void SetPath(string basePath, string configFilePath, object config)
        {
            string directoryPath = Path.GetDirectoryName(basePath);
            s_configFilePath = configFilePath;
            s_backupFilePath = Path.Combine(directoryPath, "configbackup");
            s_config = config;
            s_root = LoadYamlFile();
        }
        
        public static YamlMappingNode LoadYamlFile()
        {
            var yamlStream = new YamlStream();
            using (var reader = new StreamReader(s_configFilePath))
            {
                yamlStream.Load(reader);
            }

            if (yamlStream.Documents.Count != 0)
            {
                var rootNode = yamlStream.Documents[0].RootNode as YamlMappingNode;
                if (rootNode != null)
                {
                    return rootNode;
                }
            }

            return new YamlMappingNode();
        }
        public static void SetConfigFilePath(string filePath, TextBox filePathTextBox)
        {
            s_configFilePath = filePath;
            filePathTextBox.Text = filePath;
            s_root = LoadYamlFile();
        }
        public static void SetBackupPath(string backupFolder, TextBox backupPathTextBox)
        {
            s_backupFilePath = backupFolder;
            backupPathTextBox.Text = backupFolder;
        }
        public static void MakeBackup()
        {
            string backupFolder = s_backupFilePath;
            Directory.CreateDirectory(backupFolder);
            string backupFilePath = Path.Combine(backupFolder, $"config_{DateTime.Now:yyyyMMddHHmmss}.yml");
            File.Copy(s_configFilePath, backupFilePath, true);
        }
        public static void Save(YamlMappingNode root, string filepath)
        {
            YamlStream yaml = new YamlStream();
            yaml.Documents.Add(new YamlDocument(root));
            filepath = filepath == null ? s_configFilePath : filepath;
            using (var writer = new StreamWriter(filepath))
            {
                yaml.Save(writer, false);
            }
        }
    }
}
