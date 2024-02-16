using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppGenerator
{
    internal class Utility
    {
        public static void createFile(string path, string directory,  string text)
        {
            DirectoryInfo di = Directory.CreateDirectory($"../AppGenerateFiles/{directory}/");
            DirectoryInfo di1 = Directory.CreateDirectory($"../AppGenerateFiles/{directory}/FrontModel");
            DirectoryInfo dia = Directory.CreateDirectory($"../AppGenerateFiles/{directory}/FrontModel/ModelComponent");
            DirectoryInfo di8 = Directory.CreateDirectory($"../AppGenerateFiles/{directory}/Security");
            DirectoryInfo di2 = Directory.CreateDirectory($"../AppGenerateFiles/{directory}/Model");
            DirectoryInfo di3 = Directory.CreateDirectory($"../AppGenerateFiles/{directory}/Controllers");
            DirectoryInfo di4 = Directory.CreateDirectory($"../AppGenerateFiles/{directory}/Views");
            DirectoryInfo di7 = Directory.CreateDirectory($"../AppGenerateFiles/{directory}/Pages");
            DirectoryInfo di5 = Directory.CreateDirectory($"../AppGenerateFiles/{directory}/PagesViews");
            DirectoryInfo di6 = Directory.CreateDirectory($"../AppGenerateFiles/{directory}/PagesCatalogos");
            // Create the file, or overwrite if the file exists.
            using (FileStream fs = File.Create(path))
            {
                byte[] info = new UTF8Encoding(true).GetBytes(text);
                // Add some information to the file.
                fs.Write(info, 0, info.Length);
            }

            // Open the stream and read it back.
            using (StreamReader sr = File.OpenText(path))
            {
                string s = "";
                while ((s = sr.ReadLine()) != null)
                {
                    Console.WriteLine(s);
                }
            }
        }
        public static string capitalize(string str)
        {
            return char.ToUpper(str[0]) + str.Substring(1);
        }

    }
    
}
