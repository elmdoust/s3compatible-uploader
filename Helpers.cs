﻿using Ionic.Zip;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBackupTool
{
    class Helpers
    {
        public static void writeLogs(string LogFileName, string LogText)
        {
            try
            {
                if (!File.Exists(System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location) + "/" + LogFileName))
                {
                    File.Create(System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location) + "/" + LogFileName);
                }

                StreamWriter streamWriter = new StreamWriter(System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location) + "/" + LogFileName, append: true);
                streamWriter.WriteLine(DateTime.Now.ToString("yyyy/MM/dd - HH:mm") + " : " + LogText);
                streamWriter.Close();
            }
            catch { }
        }
        public static TypeToDeserialize DeserializeString<TypeToDeserialize>(string resultText)
        {
            
                TypeToDeserialize result = JsonConvert.DeserializeObject<TypeToDeserialize>(resultText, new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat, DateTimeZoneHandling = DateTimeZoneHandling.Local });
                return result;            
            
        }

        public static void ZipFiles(string path, string outputFileName)
        {
            using (ZipFile zip = new ZipFile())
            {
                zip.UseZip64WhenSaving = Zip64Option.AsNecessary;                
                zip.AddDirectory(path);
                zip.Save(outputFileName);
            }
        }
    }
}
