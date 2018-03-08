﻿using Mono.Options;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using VSTSClient.Shared;
using VSTSClient.ProcessTemplate.JsonResponseModels;
using Newtonsoft.Json;
using System.Threading;

namespace VSTSClient.ProcessTemplate
{
    static class Program
    {
        static string basePath;
        static string startPath;
        static string extractPath;
        static string rezipPath;
        static string changedFilesPath;

        const string ProcessTemplatesListFileName = "ProcessTemplates.txt";

        static void Main(string[] args)
        {
            // check if all folders are available:
            CheckFolders();

            var listTemplates = false;
            var help = false;
            var saveToDisk = false;
            var export = false;
            var hideDefaults = false;
            var import = false;
            var unzip = false;
            var zip = false;

            var option_set = new OptionSet()
                .Add("?|help|h", "Prints out the options.", option => help = option != null)
                .Add("l|list", "List available process templates, adding '{s}' will save this list to disk", option => listTemplates = option != null)
                .Add("s|save", "Save list to disk", option => saveToDisk = option != null)
                .Add("hd|hide", "Hide default templates", option => hideDefaults = option != null)
                .Add("e|export", "Export all process templates (located in the saved list file) from VSTS to disk", option => export = option != null)

                .Add("u|unzip", "Unzip all process templates (located in the saved list file)", option => unzip = option != null)
                .Add("z|zip", "Rezip all process templates (located in the saved list file) from folders to zipfiles", option => zip = option != null)

                .Add("i|import", "Import all process templates (located in the saved list file) from VSTS to disk", option => import = option != null)
            ;

            try
            {
                option_set.Parse(args);
            }
            catch (OptionException)
            {
                ShowHelp("Error - usage is:", option_set);
            }

            if (help) { ShowHelp("Help - usage is:", option_set); }
                        

            Helper.LoadSecrets();
            Console.WriteLine("");

            if (listTemplates) { ListTemplates(saveToDisk, hideDefaults); };
            if (export) { ExportProcessTemplates(); };

            if (unzip) { Extract(); }
            if (zip) { ZipDirectoriesBackToZip(); }

            if (import) { ImportProcessTemplates(); };
                        

            if (Debugger.IsAttached)
            {
                Console.WriteLine("");
                Console.WriteLine("Hit 'return'");
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Load and check all neccesary folders for availablilty
        /// </summary>
        private static void CheckFolders()
        {
            // load setting from appSettings
            basePath = ConfigurationManager.AppSettings["BasePath"];
            
            if (String.IsNullOrWhiteSpace(basePath))
            {
                LogError(new string[] { $"BasePath setting is empty. Please check the config file for this value", "Execution stopped." });
                Environment.Exit(-1);
            }

            // init default directories
            startPath = Path.Combine(basePath, "Downloaded files");
            extractPath = Path.Combine(basePath, "Unzipped files");
            rezipPath = Path.Combine(basePath, "Rezipped files");
            changedFilesPath = Path.Combine(basePath, "Changed files");
            
            // check for or create if those folders do exist
            if (!CheckOrCreateFolders(new string[] { startPath, extractPath, rezipPath, changedFilesPath }))
            {
                Environment.Exit(-1);
            }
        }

        /// <summary>
        /// Check the list of folders for their existance. If not present, try to create them
        /// </summary>
        /// <param name="directories">Fully expanded directory names to check</param>
        /// <returns>Succes if all folders are present or created succesfully</returns>
        private static bool CheckOrCreateFolders(string[] directories)
        {
            var errors = 0;
            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    try
                    {
                        Directory.CreateDirectory(directory);
                    }
                    catch (Exception e)
                    {
                        LogError(new string[] { $"Error creating the folder '{directory}'. Exception message: {e.Message}", "Execution stopped." });
                        errors++;
                    }
                }
            }

            return errors == 0;
        }

        /// <summary>
        /// Load the processtemplate list from disk and check if the processes do exists on VSTS
        /// </summary>
        /// <returns>The processtemplates that do exists</returns>
        private static List<Microsoft.TeamFoundation.Core.WebApi.Process> GetCleanedProcessTemplateList()
        {
            var processesTodo = new List<Microsoft.TeamFoundation.Core.WebApi.Process>();
            var processes = Helper.GetAllProcessTemplates(false);

            // get the list of processes we need to process
            var processTemplatesListFileName = Path.Combine(basePath, ProcessTemplatesListFileName);

            if (!File.Exists(processTemplatesListFileName))
            {
                LogError(new string[]
                {
                    $"Cannot find process list file in location '{processTemplatesListFileName}'",
                    $"Please use the list function in combination with the save option first"
                });

                Console.WriteLine($"");
                return processesTodo;
            }

            var processTemplates = File.ReadAllLines(processTemplatesListFileName);            

            foreach (var line in processTemplates)
            {
                var locatedProcess = processes.FirstOrDefault(item => item.Name == line);
                if (locatedProcess == null)
                {
                    LogError(new string[]
                    {
                        $"Cannot find process template with name '{line}' in VSTS. Please check the template name"
                    });

                    // go to the nex line
                    continue;
                }

                processesTodo.Add(locatedProcess);
            }

            return processesTodo;
        }

        /// import all process templates 
        private static void ImportProcessTemplates()
        {
            var processesTodo = GetCleanedProcessTemplateList();

            if (processesTodo != null)
            {
                ImportProcessTemplateZip(processesTodo, rezipPath);
            }
        }

        /// <summary>
        /// Export all process templates (loaded from exported name file) from VSTS to disk
        /// </summary>
        private static void ExportProcessTemplates()
        {
            var processesTodo = GetCleanedProcessTemplateList();

            if (processesTodo != null)
            {
                ExportProcessTemplateZip(processesTodo, startPath);
            }
        }

        /// <summary>
        /// Central error logging, including text colorization
        /// </summary>
        /// <param name="messages">Messages to log</param>
        private static void LogError(string[] messages)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var message in messages)
            {
                Console.WriteLine(message);
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Get a list off ALL process templates from VSTS and display it on the screen
        /// </summary>
        /// <param name="saveToDisk">If true, the list of processes will also be saved to disk</param>
        private static void ListTemplates(bool saveToDisk, bool hideDefaults = false)
        {
            var processes = Helper.GetAllProcessTemplates();
            
            // find the process we need
            var nameList = new List<string>();
            foreach (var process in processes)
            {
                if (hideDefaults && process.IsDefault)
                {
                    // hide the defaults
                    continue;
                }
                nameList.Add(process.Name);
            }

            if (hideDefaults)
            {
                Console.WriteLine($"Found {nameList.Count} processes that are not defaults:");
            }
            
            // only display the process we'll use
            foreach (var name in nameList)
            {
                Console.WriteLine($"\t{name}");
            }

            if (saveToDisk)
            {
                // save a file with the names of the processes to the basePath
                var savedFile = Path.Combine(basePath, ProcessTemplatesListFileName);
                File.WriteAllLines(savedFile, nameList.ToArray());
                
                Console.WriteLine("");
                Console.WriteLine($"Exported all process filenames to file '{savedFile}'");
            }
        }

        /// <summary>
        /// Show the help for this executable
        /// </summary>
        /// <param name="message"></param>
        /// <param name="option_set"></param>
        private static void ShowHelp(string message, OptionSet option_set)
        {
            Console.Error.WriteLine(message);
            option_set.WriteOptionDescriptions(Console.Error);
            Environment.Exit(-1);
        }

        /// <summary>
        /// Import the listed templates from disk to VSTS
        /// </summary>
        /// <param name="processTemplates">List of templates to import</param>
        /// <param name="fromPath">Location of the zipfiles on disk</param>
        private static void ImportProcessTemplateZip(List<Microsoft.TeamFoundation.Core.WebApi.Process> processTemplates, string fromPath)
        {
            Byte[] bytes = null;
            var success = 0;

            Console.WriteLine($"Starting to import {processTemplates.Count} templates from {fromPath} to VSTS");
            Console.WriteLine("");

            using (var client = Helper.GetRestClient())
            {
                // reset headers to zip file
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/zip"));                

                // check all processes from the server
                foreach (var process in processTemplates)
                {
                    try
                    {
                        // read file from disk:
                        var fileName = Path.Combine(fromPath, process.Name + ".zip");
                        if (!File.Exists(fileName))
                        {
                            LogError(new string[] 
                            {
                                $"Cannot find zip file for process {process.Name} in location {fromPath}",
                                $"This template will be skipped."
                            });

                            continue;
                        }

                        // read file contents
                        bytes = File.ReadAllBytes(fileName);
                        ByteArrayContent content = new ByteArrayContent(bytes);
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");

                        Console.Write($"\tUploading process template '{process.Name}'");
                        HttpResponseMessage response = client.PostAsync("_apis/work/processAdmin/processes/import?ignoreWarnings=true&api-version=2.2-preview", content).Result;

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"\t\t upload successful!");
                            var responseBody = response.Content.ReadAsStringAsync().Result;

                            var importRepsonse = JsonConvert.DeserializeObject<ImportRepsonse>(responseBody);

                            if (importRepsonse.validationResults.Any())
                            {
                                // errors during validation check?
                                var messages = new List<string> { $"\tValidation error importing process template for '{process.Name}'"};
                                foreach (var validationResult in importRepsonse.validationResults)
                                {
                                    messages.Add($"\tDescription: {validationResult.description}, Error: {validationResult.error}, File: {validationResult.file}, Issuetype: {validationResult.issueType}, Line:{validationResult.line}");
                                }
                                LogError(messages.ToArray());

                                // go to the next process
                                continue;
                            }

                            Console.WriteLine("\tChecking status...");
                            var started = DateTime.Now;
                            var importIncomplete = true;
                            while (((DateTime.Now - started).TotalMinutes < 3 && importIncomplete)) //todo: create setting for duration
                            {                                
                                importIncomplete = !WaitForImportProcessStatus(importRepsonse.promoteJobId);
                                Thread.Sleep(2500);
                            }
                        }

                        response.Dispose();
                                                
                        success++;
                    }
                    catch (Exception e)
                    {
                        LogError(new string[] { $"\tError importing process template for '{process.Name}', error: {e.Message}" });
                    }
                }

                Console.WriteLine($"Imported {success} zip files");
            }
        }

        /// <summary>
        /// Check the status of the import process step
        /// </summary>
        /// <param name="promoteJobId"></param>
        private static bool WaitForImportProcessStatus(string promoteJobId)
        {
            var completed = false;
            using (var client = Helper.GetRestClient())
            {
                HttpResponseMessage response = client.GetAsync("_apis/work/processAdmin/processes/status/" + promoteJobId).Result;

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = response.Content.ReadAsStringAsync().Result;

                    var promoteRepsonse = JsonConvert.DeserializeObject<PromoteStatus>(responseBody);

                    Console.WriteLine($"\t\t{DateTime.Now.ToLongTimeString()} projects pending: {promoteRepsonse.pending}, projects complete:{promoteRepsonse.complete}, import succesful: {promoteRepsonse.successful}, remaining retries: {promoteRepsonse.remainingRetries}, message: {promoteRepsonse.message}");
                    completed = promoteRepsonse.pending == 0;
                }

                response.Dispose();
            }

            return completed;
        }

        /// <summary>
        /// Export all process templates in the list and save them to disk
        /// </summary>
        /// <param name="processTemplates">List of proces templates to export</param>
        /// <param name="savePath">path to save the files to</param>
        private static void ExportProcessTemplateZip(List<Microsoft.TeamFoundation.Core.WebApi.Process> processTemplates, string savePath)
        {
            Byte[] bytes = null;
            var success = 0;

            Console.WriteLine($"Starting to download {processTemplates.Count} templates from VSTS");
            Console.WriteLine("");

            using (var client = Helper.GetRestClient())
            {                
                // check all processes from the server
                foreach (var process in processTemplates)
                {
                    try
                    {
                        Console.WriteLine($"\tDownloading process template '{process.Name}'");

                        HttpResponseMessage response = client.GetAsync("_apis/work/processAdmin/processes/export/" + process.Id + "?api-version=2.2-preview").Result;

                        if (response.IsSuccessStatusCode)
                        {
                            bytes = response.Content.ReadAsByteArrayAsync().Result;
                        }

                        response.Dispose();

                        if (bytes != null)
                        {
                            File.WriteAllBytes(Path.Combine(savePath, process.Name + ".zip"), bytes);
                        }
                        else
                        {
                            LogError(new string[] { $"\tGot an empty file from VSTS while downloading ProcessTemplate for '{process.Name}'" });
                        }

                        success++;
                    }
                    catch (Exception e)
                    {
                        LogError(new string[] { $"\tError downloading ProcessTemplate for '{process.Name}', error: {e.Message}" });
                    }
                }

                Console.WriteLine($"Exported {success} zip files to location'{savePath}'");
            }            
        }
        /// <summary>
        /// Export all process templates names in the given array to the export location
        /// </summary>
        /// <param name="processTemplateNames">List of template names to export</param>
        /// <param name="savePath">Path to save the files to</param>
        private static void ExportProcessTemplateZip(string[] processTemplateNames, string savePath)
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
                            File.WriteAllBytes(Path.Combine(savePath, process.Name + ".zip"), bytes);
                        }
                        else
                        {
                            // todo: log error
                        }
                    }
                }
            }
        }

        /// <summary>
        /// All old calls together
        /// </summary>
        private static void ExecutionOptions()
        {
            var zipFiles = Directory.EnumerateFiles(startPath, "*.zip");
            Console.WriteLine($"Found {zipFiles.Count()} zip files");

            //Extract(zipFiles, extractPath);

            // CheckAndCopyEpic(zipFiles, extractPath, "Epic", "Feature");

            // var firstDirectoryName = "Name of the directory to compare files with"
            // CheckFileContentsProcessConfiguration(zipFiles, extractPath, Path.Combine(extractPath, firstDirectoryName, "WorkItem Tracking", "Process", "ProcessConfiguration.xml"));
            // CheckFileContentsCategories(zipFiles, extractPath, Path.Combine(extractPath, firstDirectoryName, "WorkItem Tracking", "Categories.xml"));
            // CheckFileContentsWorkItems(zipFiles, extractPath, Path.Combine(extractPath, firstDirectoryName, "WorkItem Tracking", "WorkItems.xml"));

            CopyFiles(zipFiles, extractPath);

            //ZipDirectoriesBackToZip(zipFiles, extractPath, rezipPath);
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
        
        /// Unzip all processtemplates to the export folder      
        private static void Extract()
        {
            var processTemplates = GetCleanedProcessTemplateList();
            Console.WriteLine($"Found {processTemplates.Count} templates to unzip");
            var i = 0;
            foreach (var processTemplate in processTemplates)
            {
                var zipFile = Path.Combine(startPath, processTemplate.Name + ".zip");
                var newPath = Path.Combine(extractPath, processTemplate.Name);

                if (Directory.Exists(newPath))
                {
                    Directory.Delete(newPath, true);
                }

                // unzip the file to its own dir in extract path
                ZipFile.ExtractToDirectory(zipFile, newPath);
                i++;
            }
            Console.WriteLine($"Extracted {i} zip files to directories.");
        }

        /// Compress the directories back to zipfiles
        static void ZipDirectoriesBackToZip()
        {
            var processTemplates = GetCleanedProcessTemplateList();
            Console.WriteLine($"Found {processTemplates.Count} templates to unzip");
            var i = 0;

            foreach (var processTemplate in processTemplates)
            {
                var directoryToZip = Path.Combine(extractPath, processTemplate.Name);

                var newFileName = Path.Combine(rezipPath, processTemplate.Name + ".zip");
                if (File.Exists(newFileName))
                {
                    File.Delete(newFileName);
                }

                // rezip the file to its new zip file based on the dir in extract path
                ZipFile.CreateFromDirectory(directoryToZip, newFileName);
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
