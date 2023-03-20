using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashy
{
    internal class FileDetails
    {
        public string FilePath { get; set; }
        public string Hash { get; set; }
        public DateTime LastModified { get; set; }

        public static implicit operator FileDetails(string item)
        {
            List<string> values = new List<string>(item.Split(','));
            return new FileDetails() { FilePath = values[0], Hash = values[1], LastModified = DateTime.Parse(values[2]) };
        }

        // TODO: Implement an explicit operator for FileDetails
        //public static explicit operator string(FileDetails item)
        //{
        //    return item.ToFileDetails;
        //}


        //public static FileDetails ToFileDetails(string item)
        //{
        //    List<string> values = new List<string>(item.Split(','));
        //    //new FileDetails { FilePath = file, Hash = value, LastModified = GetFileLastModified(file
        //    return new FileDetails { FilePath = values[0], Hash = values[1], LastModified = DateTime.Parse(values[2]) };
        //}
    }
}
