using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace VSTSProcessZipper
{
    class Program
    {
        static void Main(string[] args)
        {
            var startPath = @"C:\Users\RobBos\Downloads\Raet";
            var extractPath = @"C:\Users\RobBos\Downloads\Raet unzip\";
            var rezipPath = @"C:\Users\RobBos\Downloads\Raet rezip\";
            var zipFiles = Directory.EnumerateFiles(startPath, "*.zip");
            Console.WriteLine($"Found {zipFiles.Count()} zip files");

            Extract(zipFiles, extractPath);

            // CheckAndCopyEpic(zipFiles, extractPath, "Epic", "Feature");

            // CheckFileContentsProcessConfiguration(zipFiles, extractPath, Path.Combine(extractPath, "Aangifte_BE", "WorkItem Tracking", "Process", "ProcessConfiguration.xml"));
            // CheckFileContentsCategories(zipFiles, extractPath, Path.Combine(extractPath, "Aangifte_BE", "WorkItem Tracking", "Categories.xml"));
            // CheckFileContentsWorkItems(zipFiles, extractPath, Path.Combine(extractPath, "Aangifte_BE", "WorkItem Tracking", "WorkItems.xml"));

            CopyFiles(zipFiles, extractPath);

            ZipDirectoriesBackToZip(zipFiles, extractPath, rezipPath);

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Hit 'return'");
                Console.ReadLine();
            }
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
                    // CopyFile(categoriesPath, @"C:\Users\RobBos\Downloads\Raet Changed files\Categories.xml");
                    CopyFile(processConfigurationPath, @"C:\Users\RobBos\Downloads\Raet Changed files\ProcessConfiguration.xml");
                    // CopyFile(workItemsPath, @"C:\Users\RobBos\Downloads\Raet Changed files\WorkItems.xml");
                    // CopyFile(featurePath, @"C:\Users\RobBos\Downloads\Raet Changed files\Feature.xml");

                    CopyFile(epicPath, @"C:\Users\RobBos\Downloads\Raet Changed files\Epic.xml");

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
