using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Mono.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using VSTSClient.Shared;

namespace VSTSClient
{
    class Program
    {
        private const int TotalWidth = 40;
               
        static void Main(string[] args)
        {
            var help = false;
            var list = false;
            var workitemcategory = false;
            var listWorkItems = false;
            var updateWorkItems = false;
            var listprojects = false;
            var processName = "";
            var startWorkItemType = "";
            var endWorkItemType = "";
            var countWIT = "";
            var projectName = "";
            var listQueries = false;

            var option_set = new OptionSet()
                .Add("?|help|h", "Prints out the options.", option => help = option != null)
                .Add("l|list", "List available info", option => list = option != null)
                .Add("lq|listqueries", "List queries", option => listQueries = option != null)
                .Add("lp|listprojects", "List available project info", option => listprojects = option != null)
                .Add("c|categories", "List workitem categories", option => workitemcategory = option != null)
                .Add("w|workitems", "List workitems", option => listWorkItems = option != null)
                .Add("u|updateWorkItemTypes", "Update all workitems for starttype to endtype for a certain process type", option => updateWorkItems = option != null)
                .Add("p|processName=", "ProcessName to update for", option => processName = option)
                .Add("proj|projectNameName=", "ProcessName to update for", option => projectName = option)
                .Add("swt|startWorkItemType=", "Workitem update from", option => startWorkItemType = option)
                .Add("ewt|endWorkItemType=", "Workitem update to", option => endWorkItemType = option)
                .Add("wit|witname=", "WorkItemType name to count", option => countWIT = option)
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

            if (!Helper.LoadSecrets())
            {
                Environment.Exit(-1);
            }

            // central connection object
            VssConnection connection = new VssConnection(new Uri(Helper.CollectionUri), new VssBasicCredential(string.Empty, Helper.PersonalAccessToken));

            // execute actions
            if (list) { ListAllInfo(connection); }

            if (listprojects) { ListAllProjects(connection, countWIT); }

            if (workitemcategory) { ListWorkItemCategories(connection, projectName); }

            if (listWorkItems) { GetWorkitems(connection); }

            if (listQueries)
            {
                ProjectHttpClient projectClient;
                IEnumerable<TeamProjectReference> projects;
                GetProjectList(connection, out projectClient, out projects);

                ListQueries(projects, "epic");
            }

            if (updateWorkItems) { UpdateProjectsWorkItems(connection, processName, startWorkItemType, endWorkItemType); }

            // prevent closure of command window from visual studio
            Console.WriteLine("");
            if (Debugger.IsAttached)
            {
                Console.WriteLine($"");
                Console.WriteLine($"Hit enter to close the application");
                Console.ReadLine();
            }
        }

        private static void UpdateProjectsWorkItems(VssConnection connection, string processTypeName, string startWorkItemType, string endWorkItemType)
        {
            var projectsToUpdate = GetProjectsByProcessType(connection, processTypeName);
            Console.WriteLine("");

            foreach (var project in projectsToUpdate)
            {
                List<WorkItemReference> workitems = GetWorkItemsFromProject(connection, project.Id, project.Name, startWorkItemType);
                UpdateWorkItemList(connection, workitems, startWorkItemType, endWorkItemType);
            }
            Console.WriteLine();
        }

        private static void UpdateWorkItemList(VssConnection connection, List<WorkItemReference> workitems, string startWorkItemType, string endWorkItemType)
        {
            WorkItemTrackingHttpClient workItemTrackingClient = connection.GetClient<WorkItemTrackingHttpClient>();

            // create a patchDocument
            JsonPatchDocument patchDocument = new JsonPatchDocument();

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                    Path = "/fields/System.WorkItemType",
                    Value = endWorkItemType,
                }
            );

            Console.WriteLine($"\tUpdating {workitems.Count()} workitems from type {startWorkItemType} to {endWorkItemType}");
            var done = 0; var started = DateTime.Now;
            var success = 0; var error = 0;
            foreach (var workItem in workitems)
            {
                var workItemId = workItem.Id;
                WorkItem result = null;
                // call for the update
                try
                {
                    result = workItemTrackingClient.UpdateWorkItemAsync(patchDocument, workItemId).Result;
                }
                catch (Exception e)
                {
                    Console.WriteLine("");
                    Console.WriteLine($"\t\tError changing workitem for id '{workItemId}': {e.Message} {e.InnerException?.Message}");
                }
                // check result
                if (result != null && result.Fields["System.WorkItemType"].ToString() == endWorkItemType)
                {
                    success++;
                }
                else
                {
                    error++;
                }
                done++;

                if (workitems.Count() % 10 == 0)
                {
                    // display progress
                    Console.Write($"\t{workitems.Count() % 10}%");
                }
            }
            // close previous line
            Console.WriteLine();
            // log end status
            Console.Write($"\tOperation complete. Total items: {workitems.Count()}, success: {success}, errors: {error}, done: {done}. Duration: {(DateTime.Now - started).TotalSeconds.ToString("N2")} seconds");
        }

        /// <summary>
        /// Get a list of all workitems for a given type inside of a project
        /// </summary>
        /// <param name="connection">Connection object to use</param>
        /// <param name="projectId">ProjectId to load woritems from</param>
        /// <param name="projectName">Name of the project for display purpose</param>
        /// <param name="startWorkItemType">WorkItemType we need to filter on</param>
        /// <param name="logging">Log the result count</param>
        /// <returns>The workitems that were found in the project, for the give workitemtype</returns>
        private static List<WorkItemReference> GetWorkItemsFromProject(VssConnection connection, Guid projectId, string projectName, string startWorkItemType, bool logging = true)
        {
            if (logging) Console.WriteLine($"\tLoading workitems for project {projectName}");

            WorkItemTrackingHttpClient workItemTrackingClient = connection.GetClient<WorkItemTrackingHttpClient>();

            Wiql wiql = new Wiql
            {
                Query = $"Select [System.Id], [System.Title], [System.State], [System.WorkItemType] From WorkItems Where [System.WorkItemType] = '{startWorkItemType}' and [Area Path] Under '{projectName}'"
            };

            WorkItemQueryResult workItems;
            try
            {
                workItems = workItemTrackingClient.QueryByWiqlAsync(wiql).Result;
                if (logging) Console.WriteLine($"\t\tFound {workItems.WorkItems.Count()} work items for type '{startWorkItemType}'");
                return workItems.WorkItems.ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine($"\t\t Error loading workitems to count for project '{projectName}' and work item type='{startWorkItemType}': {e.Message}");
            }

            return null;
        }

        private static void GetWorkitems(VssConnection connection, bool update = false)
        {
            Console.WriteLine("Loading workitem info");
            WorkItemTrackingHttpClient workItemTrackingClient = connection.GetClient<WorkItemTrackingHttpClient>();

            var workitemIds = new int[] { 9, 10 };
            string[] fieldNames = new string[] {
                "System.Id",
                "System.Title",
                "System.WorkItemType"
                // , "Microsoft.VSTS.Scheduling.RemainingWork"
            };

            List<WorkItem> workitems = workItemTrackingClient.GetWorkItemsAsync(workitemIds, fieldNames).Result;

            foreach (var workitem in workitems)
            {
                Console.WriteLine($"Workitem found: {workitem.Id}");
                foreach (var fieldName in fieldNames)
                {
                    Console.Write("  {0}: {1}", fieldName, workitem.Fields[fieldName]);
                }
                Console.WriteLine();
            }

            if (update)
            {
                // create a patchDocument
                JsonPatchDocument patchDocument = new JsonPatchDocument();

                patchDocument.Add(
                    new JsonPatchOperation()
                    {
                        Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                        Path = "/fields/System.WorkItemType",
                        Value = "Epic",
                        //From = changedBy
                    }
                );

                var workItemId = workitemIds[1];
                Console.WriteLine($"Updating workitem with id {workItemId}");
                // call for the update
                //var result = workItemTrackingClient.UpdateWorkItemAsync(patchDocument, workItemId).Result;
                // show result
                //Console.WriteLine($"Workitem change result: {result.Id}, WorkItemType: {result.Fields["System.WorkItemType"]}");
            }
        }

        /// <summary>
        /// Get a project reference from the server
        /// </summary>
        /// <param name="connection">Connection to use</param>
        /// <param name="projectName">Project to search for</param>
        /// <returns></returns>
        private static TeamProjectReference GetProject(VssConnection connection, string projectName)
        {
            Console.WriteLine($"Searching for project '{projectName}'");
            // search for project
            ProjectHttpClient projectClient = connection.GetClient<ProjectHttpClient>();

            var project = projectClient.GetProjects().Result.FirstOrDefault(item => item.Name == projectName);

            if (project == null)
            {
                Console.WriteLine($"Project with name '{projectName}' not found");
                return null;
            }
            else
            {
                Console.WriteLine($"Project was found");
            }

            return project;
        }

        /// <summary>
        /// Get all projects based on the processtemplate they have
        /// </summary>
        /// <param name="connection">Connection to use</param>
        /// <param name="processTypeName">Name of the process type to filter on</param>
        /// <returns>List of found processses</returns>
        private static List<TeamProjectReference> GetProjectsByProcessType(VssConnection connection, string processTypeName)
        {
            Console.WriteLine($"Searching for projects with processtype '{processTypeName}'");
            // search for project
            ProjectHttpClient projectClient = connection.GetClient<ProjectHttpClient>();

            var projects = projectClient.GetProjects().Result;
            var filteredProjectList = new List<TeamProjectReference>();
            Console.WriteLine($"Found {projects.Count()} projects in total, filtering on processTypeName = {processTypeName}");

            foreach (var project in projects)
            {
                var fullProject = projectClient.GetProject(project.Id.ToString(), includeCapabilities: true).Result;

                // find processtemplate capability:
                var processTemplate = fullProject.Capabilities.FirstOrDefault(item => item.Key == "processTemplate");
                if (processTemplate.Value != null)
                {
                    // find the template info:
                    var processTemplateName = processTemplate.Value.FirstOrDefault(item => item.Key == "templateName");
                    var processTemplateId = processTemplate.Value.FirstOrDefault(item => item.Key == "templateTypeId");

                    if (processTemplateName.Value == processTypeName)
                    {
                        Console.WriteLine($"\tFound project: {fullProject.Name.PadRight(TotalWidth)} Id: {fullProject.Id}");
                        filteredProjectList.Add(fullProject);
                    }
                }
            }

            return filteredProjectList;
        }

        /// <summary>
        /// List the available workitem categories for a project
        /// </summary>
        /// <param name="connection">Connection to use</param>
        /// <param name="projectName">Project to search for</param>
        private static void ListWorkItemCategories(VssConnection connection, string projectName)
        {
            var project = GetProject(connection, projectName);
            if (project == null) return;

            Guid projectId = project.Id;

            WorkItemTrackingHttpClient workItemTrackingClient = connection.GetClient<WorkItemTrackingHttpClient>();

            List<WorkItemTypeCategory> results = workItemTrackingClient.GetWorkItemTypeCategoriesAsync(projectId).Result;

            Console.WriteLine("Work Item Type Categories:");

            foreach (WorkItemTypeCategory category in results)
            {
                Console.WriteLine($"\t{category.Name.PadRight(TotalWidth)} <{category.ReferenceName}>");
            }
        }

        private static void ShowHelp(string message, OptionSet option_set)
        {
            Console.Error.WriteLine(message);
            option_set.WriteOptionDescriptions(Console.Error);
            Environment.Exit(-1);
        }

        private static void ListQueries(IEnumerable<TeamProjectReference> projects, string textToFind, bool spoolToDisk = false)
        {
            var exportFolder = @"C:\VSTSClientExport\"; // todo extract to central location
            if (spoolToDisk)
            {
                Console.WriteLine($"Listing all queries to the exportfolder: '{exportFolder}'");
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($":{Helper.PersonalAccessToken}")));

                    foreach (var project in projects)
                    {
                        // get the list of queries
                        using (HttpResponseMessage response = client.GetAsync($"{Helper.CollectionUri}/{project.Name}/_apis/wit/queries?$expand=all&$depth=1&api-version=4.1-preview").Result)
                        {
                            response.EnsureSuccessStatusCode();
                            string responseBody = response.Content.ReadAsStringAsync().Result;

                            CustomClasses.QueriesList QueriesList = null;
                            // convert json to a class
                            try
                            {
                                QueriesList = JsonConvert.DeserializeObject<CustomClasses.QueriesList>(responseBody);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Error converting json to class: {e.Message}");
                                continue;
                            }

                            if (QueriesList.value == null)
                            {
                                Console.WriteLine("");
                                continue;
                            }

                            foreach (var folder in QueriesList.value)
                            {
                                if (folder.children == null)
                                {
                                    // skip this item
                                    continue;
                                }

                                var projectLogged = false;

                                foreach (var query in folder.children)
                                {
                                    if (query != null && string.IsNullOrEmpty(query.wiql))
                                    {
                                        // skip this query
                                        continue;
                                    }

                                    if (query.wiql.ToLowerInvariant().Contains(textToFind))
                                    {   
                                        // Found the search term in a query  
                                        
                                        var projectName = project.Name;
                                        var linkUrl = query._links.html.href;
                                        var queryName = query.name;

                                        if (!projectLogged)
                                        {
                                            Console.WriteLine($"{projectName}");
                                            projectLogged = true;
                                        }

                                        Console.WriteLine($"\t{queryName} - {linkUrl}");
                                    }
                                }
                            }

                            if (responseBody.ToLowerInvariant().Contains("epic") && spoolToDisk)
                            {
                                var filePath = Path.Combine(exportFolder, $"{project.Name}.json");
                                File.WriteAllText(filePath, responseBody);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// List all available hierarchy information from the server
        /// </summary>
        /// <param name="connection">Connection to use</param>
        private static void ListAllInfo(VssConnection connection)
        {
            Console.WriteLine($"Connection to vsts on '{Helper.CollectionUri}'");

            ListCollections(connection);

            ListAllProcessTemplates(connection);

            ListAllProjects(connection, "");

            Console.WriteLine();
        }

        /// <summary>
        /// List all projects on the server, and count the number of WorkItems for a given type
        /// </summary>
        /// <param name="connection">Connection to use</param>
        /// <param name="countWITname">Name of the WorkItemType to count the number of</param>
        private static void ListAllProjects(VssConnection connection, string countWITname)
        {
            // get all projects
            ProjectHttpClient projectClient;
            IEnumerable<TeamProjectReference> projects;
            GetProjectList(connection, out projectClient, out projects);

            Console.WriteLine($"Found {projects.Count()} projects");
            foreach (var project in projects.OrderBy(item => item.Name))
            {
                Console.Write($"\t{project.Name.PadRight(TotalWidth)}");
                var fullProject = projectClient.GetProject(project.Id.ToString(), includeCapabilities: true).Result;

                // find processtemplate capability:
                var processTemplate = fullProject.Capabilities.FirstOrDefault(item => item.Key == "processTemplate");
                if (processTemplate.Value != null)
                {
                    // find the template info:
                    var processTemplateName = processTemplate.Value.FirstOrDefault(item => item.Key == "templateName");
                    //var processTemplateId = processTemplate.Value.FirstOrDefault(item => item.Key == "templateTypeId");

                    Console.Write($", ProcessTemplate: {processTemplateName.Value.PadRight(TotalWidth)}");
                }

                if (!String.IsNullOrEmpty(countWITname))
                {
                    var workitems = GetWorkItemsFromProject(connection, project.Id, project.Name, countWITname, false);
                    if (workitems != null)
                    {
                        Console.Write($", WorkItems with type '{countWITname}': {workitems.Count}");
                    }
                }

                Console.WriteLine();
            }
        }

        private static void GetProjectList(VssConnection connection, out ProjectHttpClient projectClient, out IEnumerable<TeamProjectReference> projects)
        {
            projectClient = connection.GetClient<ProjectHttpClient>();
            projects = projectClient.GetProjects().Result.OrderBy(item => item.Name);
        }

        /// <summary>
        ///  List all available collections on the server
        /// </summary>
        /// <param name="connection">Connection to use</param>
        private static void ListCollections(VssConnection connection)
        {
            ProjectCollectionHttpClient projectCollectionClient = connection.GetClient<ProjectCollectionHttpClient>();
            IEnumerable<TeamProjectCollectionReference> projectCollections = projectCollectionClient.GetProjectCollections().Result;

            // get all collections
            Console.WriteLine($"Found {projectCollections.Count()} collections");
            foreach (var collection in projectCollections)
            {
                Console.WriteLine($"\t{collection.Name}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// List all process templates on the server
        /// </summary>
        /// <param name="connection">Connection to use</param>
        private static void ListAllProcessTemplates(VssConnection connection)
        {
            // list all processes
            ProcessHttpClient processClient = connection.GetClient<ProcessHttpClient>();

            var processes = processClient.GetProcessesAsync().Result;
            Console.WriteLine($"Found {processes.Count} processes");

            foreach (var process in processes.OrderBy(item => item.Name))
            {
                // var fullProcess = processClient.GetProcessByIdAsync(process.Id).Result;
                
                Console.WriteLine($"\t{(process.IsDefault ? "*" : " ")} Name: {process.Name.PadRight(TotalWidth)} Id: {process.Id}, Type: {process.Type}");
            }
            Console.WriteLine();
        }
    }
}
