using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSTSClient.Shared
{
    public static class LocalStorageHelper
    {
        private const string StorageFileName = "VSTSClientStore.txt";
        /// <summary>
        /// Store the connection data locally and safe
        /// </summary>
        /// <param name="basePath">Path to the process template storage to store</param>
        /// <param name="collectionUri">CollectionUr to store</param>
        /// <param name="personalAccessToken">Personal Access Token to store</param>
        public static void StoreConnectionData(string basePath, string collectionUri, string personalAccessToken)
        {
            var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

            if (isoStore.FileExists(StorageFileName))
            {
                isoStore.DeleteFile(StorageFileName);
            }

            // save a new file
            using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(StorageFileName, FileMode.CreateNew, isoStore))
            {
                using (StreamWriter writer = new StreamWriter(isoStream))
                {
                    writer.WriteLine($"url={collectionUri}");
                    writer.WriteLine($"pat={personalAccessToken}");
                    writer.WriteLine($"basePath={basePath}");
                }
            }
        }

        /// <summary>
        /// Retrieve the stored information
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="collectionUri"></param>
        /// <param name="personalAccessToken"></param>
        public static void RetrieveConnectionData(out string basePath, out string collectionUri, out string personalAccessToken)
        {
            var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

            if (!isoStore.FileExists(StorageFileName))
            {
                collectionUri = "";
                personalAccessToken = "";
                basePath = "";

                return;
            }

            // save a new file
            using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(StorageFileName, FileMode.Open, isoStore))
            {
                using (var reader = new StreamReader(isoStream))
                {
                    collectionUri = reader.ReadLine().Split('=')[1];
                    personalAccessToken = reader.ReadLine().Split('=')[1];
                    basePath = reader.ReadLine().Split('=')[1];
                }
            }
        }
    }
}
