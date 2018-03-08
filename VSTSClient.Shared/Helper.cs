using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace VSTSClient.Shared
{
    public static class Helper
    {
        public static string CollectionUri { get; set; }
        public static string PersonalAccessToken { get; set; }
        public static VssConnection connection { get; set; }
        /// <summary>
        /// Load secrets from config file
        /// </summary>
        public static bool LoadSecrets()
        {
            CollectionUri = ConfigurationManager.AppSettings["Url"];
            PersonalAccessToken = ConfigurationManager.AppSettings["PAT"];

            if (String.IsNullOrEmpty(CollectionUri)) { Console.WriteLine("Cannot find collection URL in appSettings. Add a key with name 'Url'"); }
            if (String.IsNullOrEmpty(PersonalAccessToken)) { Console.WriteLine("Cannot find personal access token in appSettings. Add a key with name 'PAT'"); }

            if (String.IsNullOrEmpty(CollectionUri) || String.IsNullOrEmpty(PersonalAccessToken))
            {
                return false;
            }

            // central connection object
            connection = new VssConnection(new Uri(Helper.CollectionUri), new VssBasicCredential(string.Empty, Helper.PersonalAccessToken));

            return true;
        }
        

        public static HttpClient GetRestClient(string projectName = "")
        {
            HttpClient client = new HttpClient();

            client.BaseAddress = new Uri($"{Helper.CollectionUri}/{projectName}");

            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($":{Helper.PersonalAccessToken}")));

            return client;
        }

        public static object GetProcessIdFromProcessTemplateName(string processTemplateName)
        {
            return GetProcessTemplate(connection, processTemplateName).Id;
        }

        /// <summary>
        /// List all process templates on the server
        /// </summary>
        /// <param name="connection">Connection to use</param>
        public static void ListAllProcessTemplates(VssConnection connection, int totalWidth)
        {
            // list all processes
            ProcessHttpClient processClient = connection.GetClient<ProcessHttpClient>();

            var processes = processClient.GetProcessesAsync().Result;
            Console.WriteLine($"Found {processes.Count} processes");

            foreach (var process in processes.OrderBy(item => item.Name))
            {
                // var fullProcess = processClient.GetProcessByIdAsync(process.Id).Result;

                Console.WriteLine($"\t{(process.IsDefault ? "*" : " ")} Name: {process.Name.PadRight(totalWidth)} Id: {process.Id}, Type: {process.Type}");
            }
            Console.WriteLine();
        }

        public static Process GetProcessTemplate(VssConnection connection, string processTemplateName)
        {
            // list all processes
            ProcessHttpClient processClient = connection.GetClient<ProcessHttpClient>();

            var allProcesses = processClient.GetProcessesAsync().Result;

            var process = allProcesses.FirstOrDefault(item => item.Name == processTemplateName);

            return process;
        }
    }
}
