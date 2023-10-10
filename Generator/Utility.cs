using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppGenerator
{
    internal class Utility
    {
        public static void createFile(string path, string text)
        {
            DirectoryInfo di = Directory.CreateDirectory(@"../AppGenerateFiles/");
            DirectoryInfo di1 = Directory.CreateDirectory(@"../AppGenerateFiles/FrontModel");
            DirectoryInfo di8 = Directory.CreateDirectory(@"../AppGenerateFiles/Security");
            DirectoryInfo di2 = Directory.CreateDirectory(@"../AppGenerateFiles/Model");
            DirectoryInfo di3 = Directory.CreateDirectory(@"../AppGenerateFiles/Controllers");
            DirectoryInfo di4 = Directory.CreateDirectory(@"../AppGenerateFiles/Views");
            DirectoryInfo di7 = Directory.CreateDirectory(@"../AppGenerateFiles/Pages");
            DirectoryInfo di5 = Directory.CreateDirectory(@"../AppGenerateFiles/PagesViews");
            DirectoryInfo di6 = Directory.CreateDirectory(@"../AppGenerateFiles/PagesCatalogos");
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
