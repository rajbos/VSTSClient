using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Configuration;
using VSTSClient.Shared;
using System.Net.Http;

namespace VSTSClient.ProcessTemplate
{
    class Program
    {
        static string basePath;
        static string startPath;
        static string extractPath;
        static string rezipPath;
        static string changedFilesPath;

        static void Main(string[] args)
        {
            // load folder info
            basePath = ConfigurationManager.AppSettings["BasePath"]; // todo: check if this has a value, check for existance

            startPath = Path.Combine(basePath, "Downloaded files"); // todo: check for existance
            extractPath = Path.Combine(basePath, "Unzipped files"); // todo: check for existance
            rezipPath = Path.Combine(basePath, "Rezipped files"); // todo: check for existance
            changedFilesPath = Path.Combine(basePath, "Changed files"); // todo: check for existance

            Helper.LoadSecrets();
            //ExecutionOptions();

            //ExportProcessTemplateZip(new string[] {"Information_Management", "IT Operations"}, startPath);
            ExportProcessTemplateZip(new string[] { }, startPath);

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Hit 'return'");
                Console.ReadLine();
            }
        }

        private static void ExportProcessTemplateZip(string[] processTemplateNames, string startPath)
        {
            var processes = Helper.GetAllProcessTemplates();

            Byte[] bytes = null;
            using (var client = Helper.GetRestClient())
            {
                // check all processes from the server
                foreach (var process in processes)
                {                    
                    // if the search list is empty, download all processes
                    // if processTeplateName is in the searh list, download it
                    if (!processTemplateNames.Any() || processTemplateNames.FirstOrDefault(item => item == process.Name) != null)
                    {
                        Console.WriteLine($"Downloading process template export for '{process.Name}'");

                        HttpResponseMessage response = client.GetAsync("_apis/work/processAdmin/processes/export/" + process.Id + "?api-version=2.2-preview").Result;

                        if (response.IsSuccessStatusCode)
                        {
                            bytes = response.Content.ReadAsByteArrayAsync().Result;
                        }

                        response.Dispose();

                        if (bytes != null)
                        {
                            File.WriteAllBytes(Path.Combine(startPath, process.Name + ".zip"), bytes);
                        }
                        else
                        {
                            // todo: log error
                        }
                    }
                }
            }
        }

        private static void ExecutionOptions()
        {
            var zipFiles = Directory.EnumerateFiles(startPath, "*.zip");
            Console.WriteLine($"Found {zipFiles.Count()} zip files");

            Extract(zipFiles, extractPath);

            // CheckAndCopyEpic(zipFiles, extractPath, "Epic", "Feature");

            // var firstDirectoryName = "Name of the directory to compare files with"
            // CheckFileContentsProcessConfiguration(zipFiles, extractPath, Path.Combine(extractPath, firstDirectoryName, "WorkItem Tracking", "Process", "ProcessConfiguration.xml"));
            // CheckFileContentsCategories(zipFiles, extractPath, Path.Combine(extractPath, firstDirectoryName, "WorkItem Tracking", "Categories.xml"));
            // CheckFileContentsWorkItems(zipFiles, extractPath, Path.Combine(extractPath, firstDirectoryName, "WorkItem Tracking", "WorkItems.xml"));

            CopyFiles(zipFiles, extractPath);

            ZipDirectoriesBackToZip(zipFiles, extractPath, rezipPath);
        }

        private static void CopyFiles(IEnumerable<string> zipFiles, string extractPath)
        {
            var failed = 0;
            var success = 0;
            foreach (var fileName in zipFiles)
            {
                var directoryName = Path.GetFileNameWithoutExtension(fileName);
                var directoryToEditName = Path.Combine(extractPath, directoryName);

                var categoriesPath = Path.Combine(directoryToEditName, "WorkItem Tracking", "Categories.xml");
                var processConfigurationPath = Path.Combine(directoryToEditName, "WorkItem Tracking", "Process", "ProcessConfiguration.xml");
                var workItemsPath = Path.Combine(directoryToEditName, "WorkItem Tracking", "WorkItems.xml");
                var featurePath = Path.Combine(directoryToEditName, "WorkItem Tracking", "TypeDefinitions", "Feature.xml");
                var epicPath = Path.Combine(directoryToEditName, "WorkItem Tracking", "TypeDefinitions", "Epic.xml");

                try
                {
                    // overwrite the files
                    CopyFile(categoriesPath, Path.Combine(extractPath, changedFilesPath, "Categories.xml"));
                    CopyFile(processConfigurationPath, Path.Combine(extractPath, changedFilesPath, "ProcessConfiguration.xml"));
                    CopyFile(workItemsPath, Path.Combine(extractPath, changedFilesPath, "WorkItems.xml"));
                    CopyFile(featurePath, Path.Combine(extractPath, changedFilesPath, "Feature.xml"));
                    CopyFile(epicPath, Path.Combine(extractPath, changedFilesPath, "Epic.xml"));

                    //Console.WriteLine($"{directoryName} succeeded");
                    success++;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception in folder '{directoryName}': {e.Message}");
                    failed++;
                }
            }
            Console.WriteLine($"Copy files is done. Success:'{success}', failed: {failed}");
        }
        private static void CopyFile(string pathToOverwrite, string newFileName)
        {
            if (!File.Exists(newFileName))
            {
                Console.WriteLine($"Cannot find file to copy '{newFileName}'");
            }

            if (File.Exists(pathToOverwrite))
            {
                File.Delete(pathToOverwrite);
            }

            File.Copy(newFileName, pathToOverwrite);
        }
        static void Extract(IEnumerable<string> zipFiles, string extractPath)
        {
            var i = 0;
            foreach (var fileName in zipFiles)
            {
                var directoryName = Path.GetFileNameWithoutExtension(fileName);
                var newPath = Path.Combine(extractPath, directoryName);

                if (Directory.Exists(newPath))
                {
                    Directory.Delete(newPath, true);
                }

                // unzip the file to its own dir in extract path
                ZipFile.ExtractToDirectory(fileName, newPath);
                i++;
            }
            Console.WriteLine($"Extracted {i} zip files to directories.");
        }
        static void ZipDirectoriesBackToZip(IEnumerable<string> zipFiles, string extractedPath, string rezipPath)
        {
            var i = 0;
            foreach (var fileName in zipFiles)
            {
                var directoryName = Path.GetFileNameWithoutExtension(fileName);

                var newFileName = Path.Combine(rezipPath, directoryName) + ".zip";
                if (File.Exists(newFileName))
                {
                    File.Delete(newFileName);
                }

                // rezip the file to its new zip file based on the dir in extract path
                ZipFile.CreateFromDirectory(Path.Combine(extractedPath, directoryName), newFileName);
                i++;
            }
            Console.WriteLine($"Zipped {i} directories back up");
        }
        static void CheckAndCopyEpic(IEnumerable<string> zipFiles, string extractPath, string typeFrom, string typeTo)
        {
            // Wathc out! this doesn't change the contents of the file! use CopyFiles to copy the file from a base dir. 
            foreach (var fileName in zipFiles)
            {
                var directoryName = Path.GetFileNameWithoutExtension(fileName);
                var directoryToEditName = Path.Combine(extractPath, directoryName, "WorkItem Tracking", "TypeDefinitions");
                // find typefrom file
                var typeFromFileName = $"{directoryToEditName}\\{typeFrom}.xml";
                if (!File.Exists(typeFromFileName))
                {
                    Console.WriteLine($"Cannot find typeFrom '{typeFrom}' inside of '{directoryName}'");
                    continue;
                }
                var typeToFileName = $"{directoryToEditName}\\{typeTo}.xml";
                // check if typeto exists
                if (File.Exists(typeToFileName))
                {
                    Console.WriteLine($"New typeTo '{typeTo}' inside of '{directoryName}'allready exists. Skipping this action");
                    continue;
                }
                // copy typefrom to typeto
                File.Copy(typeFromFileName, typeToFileName);
            }
        }
        static void CheckFileContentsProcessConfiguration(IEnumerable<string> zipFiles, string extractPath, string originFile)
        {
            Console.WriteLine($"Checking for {Path.GetFileNameWithoutExtension(originFile)}");
            var same = 0;
            var diff = 0;
            foreach (var fileName in zipFiles)
            {
                var directoryName = Path.GetFileNameWithoutExtension(fileName);
                var directoryToEditName = Path.Combine(extractPath, directoryName, "WorkItem Tracking", "Process", "ProcessConfiguration.xml");
                // find typefrom file
                var fileToCheck = $"{directoryToEditName}";


                if (!File.Exists(fileToCheck))
                {
                    Console.WriteLine($"Cannot find file to Check '{fileToCheck}' inside of '{directoryName}'");
                    continue;
                }
                // read org file
                var originalFileContent = File.ReadAllText(originFile);
                // read file to check
                var fileToheckContent = File.ReadAllText(fileToCheck);
                // check contents

                if (string.Compare(originalFileContent, fileToheckContent) > 0)
                {
                    Console.WriteLine($"File in '{directoryName}' differs from original");
                    diff++;
                }
                else
                {
                    //Console.WriteLine($"\tFile in '{directoryName}' is the same as original");
                    same++;
                }
            }

            Console.WriteLine($"\tStatus for '{Path.GetFileNameWithoutExtension(originFile)}': {same} files are the same, {diff} files differ in content");
        }
        static void CheckFileContentsCategories(IEnumerable<string> zipFiles, string extractPath, string originFile)
        {
            Console.WriteLine($"Checking for {Path.GetFileNameWithoutExtension(originFile)}");
            var same = 0;
            var diff = 0;
            foreach (var fileName in zipFiles)
            {
                var directoryName = Path.GetFileNameWithoutExtension(fileName);
                var directoryToEditName = Path.Combine(extractPath, directoryName, "WorkItem Tracking", "Categories.xml");
                // find typefrom file
                var fileToCheck = $"{directoryToEditName}";


                if (!File.Exists(fileToCheck))
                {
                    Console.WriteLine($"Cannot find file to Check '{fileToCheck}' inside of '{directoryName}'");
                    continue;
                }
                // read org file
                var originalFileContent = File.ReadAllText(originFile);
                // read file to check
                var fileToheckContent = File.ReadAllText(fileToCheck);
                // check contents

                if (string.Compare(originalFileContent, fileToheckContent) > 0)
                {
                    Console.WriteLine($"File in '{directoryName}' differs from original");
                    diff++;
                }
                else
                {
                    //Console.WriteLine($"\tFile in '{directoryName}' is the same as original");
                    same++;
                }
            }

            Console.WriteLine($"\tStatus for '{Path.GetFileNameWithoutExtension(originFile)}': {same} files are the same, {diff} files differ in content");
        }
        static void CheckFileContentsWorkItems(IEnumerable<string> zipFiles, string extractPath, string originFile)
        {
            Console.WriteLine($"Checking for {Path.GetFileNameWithoutExtension(originFile)}");
            var same = 0;
            var diff = 0;
            foreach (var fileName in zipFiles)
            {
                var directoryName = Path.GetFileNameWithoutExtension(fileName);
                var directoryToEditName = Path.Combine(extractPath, directoryName, "WorkItem Tracking", "WorkItems.xml");
                // find typefrom file
                var fileToCheck = $"{directoryToEditName}";


                if (!File.Exists(fileToCheck))
                {
                    Console.WriteLine($"Cannot find file to Check '{fileToCheck}' inside of '{directoryName}'");
                    continue;
                }
                // read org file
                var originalFileContent = File.ReadAllText(originFile);
                // read file to check
                var fileToheckContent = File.ReadAllText(fileToCheck);
                // check contents

                var diffIndex = string.Compare(originalFileContent, fileToheckContent);
                if (diffIndex > 0)
                {
                    Console.WriteLine($"File in '{directoryName}' differs from original. DiffIndex = {diffIndex}");
                    diff++;
                }
                else
                {
                    //Console.WriteLine($"\tFile in '{directoryName}' is the same as original");
                    same++;
                }
            }

            Console.WriteLine($"\tStatus for '{Path.GetFileNameWithoutExtension(originFile)}': {same} files are the same, {diff} files differ in content");
        }
    }
}
