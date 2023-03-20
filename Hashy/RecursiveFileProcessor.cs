using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashy
{
    internal class RecursiveFileProcessor
    {
        static List<string> files = new List<string>();

        public static List<string> RecursiveFileSearch(string path)
        {
            CheckDirOrFile(path);

            return files;
        }

        private static void CheckDirOrFile(string path)
        {
            if (File.Exists(path))
            {
                // This path is a file
                ProcessFile(path);
            }
            else if (Directory.Exists(path))
            {
                // This path is a directory
                ProcessDirectory(path);
            }
            else
            {
                Console.WriteLine("{0} is not a valid file or directory.", path);
            }
        }

        // Process all files in the directory passed in, recurse on any directories
        // that are found, and process the files they contain.
        private static void ProcessDirectory(string targetDirectory)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(fileName);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(subdirectory);
        }

        // Insert logic for processing found files here.
        private static void ProcessFile(string path)
        {
            files.Add(path);
        }
    }
}
