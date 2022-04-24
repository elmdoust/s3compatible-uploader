using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBackupTool
{
    class Config
    {
        public List<SourceAndDestination> Directories { get; set; }
        public string SecretKey { get; set; }
        public string AccessKey { get; set; }
        public string EndpointUrl { get; set; }
        public string LogFileName { get; set; }
    }

    class SourceAndDestination
    {
        public string SourceDirectoryPath { get; set; }
        public string S3BucketName { get; set; }
    }
}
