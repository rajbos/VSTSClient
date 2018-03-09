using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSTSClient.Shared
{
    public static class LogHelper
    {
        /// <summary>
        /// Central error logging, including text colorization
        /// </summary>
        /// <param name="messages">Messages to log</param>
        public static void LogError(string[] messages)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var message in messages)
            {
                Console.WriteLine(message);
            }
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
