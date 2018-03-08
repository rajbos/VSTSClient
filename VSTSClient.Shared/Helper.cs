using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSTSClient.Shared
{
    public static class Helper
    {
        public static string CollectionUri { get; set; }
        public static string PersonalAccessToken { get; set; }
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

            return true;
        }
    }
}
