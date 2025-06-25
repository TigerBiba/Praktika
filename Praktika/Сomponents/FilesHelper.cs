using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Praktika.Сomponents
{
    internal class FilesHelper
    {
        readonly static string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Protocols");
        public static void CreateFolderPath()
        {
            if (!Directory.Exists(FilesHelper.folderPath))
                Directory.CreateDirectory(FilesHelper.folderPath);
        }

        public static string ReturnFolderPath() => FilesHelper.folderPath;
    }
}
