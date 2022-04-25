
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Linq;

namespace AutoBackupTool
{
    class UploadObject
    {
        private static IAmazonS3 _s3Client;
        private static Config DomainConfigs;
        

        static async Task Main()
        {
            
            StreamReader streamReader = new StreamReader(System.IO.Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location) + "/config.json");
            string JsonConfig = streamReader.ReadToEnd();

            DomainConfigs = Helpers.DeserializeString<Config>(JsonConfig);
            if (DomainConfigs == null)
            {                
                Console.WriteLine("Cannot read schema format of config.json \n correct the format and try again. ");
                Console.ReadKey();                
            }
            else
            {
                if (!DomainConfigs.AutoMode)
                {
                    Console.WriteLine("Config fetched. Press any key to try get backup...");
                    Console.ReadKey();
                }
                var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(DomainConfigs.AccessKey, DomainConfigs.SecretKey);
                var config = new AmazonS3Config { ServiceURL = DomainConfigs.EndpointUrl };
                _s3Client = new AmazonS3Client(awsCredentials, config);

                foreach (SourceAndDestination item in DomainConfigs.Directories)
                {
                    if (Directory.Exists(item.SourceDirectoryPath))
                    {
                        var directory = new DirectoryInfo(item.SourceDirectoryPath);
                        var myFile = (from f in directory.GetFiles()
                                      orderby f.LastWriteTime descending
                                      select f).First();

                        var path = $"{item.SourceDirectoryPath}/{myFile.Name}";
                        Console.WriteLine("\n\n Please Wait, Object is uploading....... \n\n");
                        await UploadObjectFromFileAsync(_s3Client, item.S3BucketName, myFile.Name, path);

                    }
                    else
                    {
                        Helpers.writeLogs(DomainConfigs.LogFileName, $"The directory {item.SourceDirectoryPath} didn't exist");
                    }
                }
                if (!DomainConfigs.AutoMode)
                {
                    Console.WriteLine("The job finished. see the result in log file. press any key to exit...");
                    Console.ReadKey();
                }
            }
            //Console.ReadKey();
        }

  
        private static async Task UploadObjectFromFileAsync(
            IAmazonS3 client,
            string bucketName,
            string objectName,
            string filePath)
        {
            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectName,
                    FilePath = filePath,
                    ContentType = "text/plain"
                };

                putRequest.Metadata.Add("x-amz-meta-title", "AutoBackupTool");

                PutObjectResponse response = await client.PutObjectAsync(putRequest);

                foreach (PropertyInfo prop in response.GetType().GetProperties())
                {
                    Console.WriteLine($"{prop.Name}: {prop.GetValue(response, null)}");
                }
                string FinalStatus = $"Object {objectName} added to {bucketName} bucket";
                Console.WriteLine(FinalStatus);
                Helpers.writeLogs(DomainConfigs.LogFileName, FinalStatus);
            }
            catch (AmazonS3Exception e)
            {
                string ErrorMessage = $"Error: {e.Message}";
                Console.WriteLine(ErrorMessage);
                Helpers.writeLogs(DomainConfigs.LogFileName, ErrorMessage);
            }
        }
        
    }

}
