
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

        //private const string BUCKET_NAME = "hamisheh-backup-database";
        //private const string OBJECT_NAME = "abc.txt";
        
        private static Config DomainConfigs;
        private static toolbox.Tools_Communication methods_com = new toolbox.Tools_Communication();
        static async Task Main()
        {

            StreamReader streamReader = new StreamReader(System.IO.Path.GetDirectoryName(
      System.Reflection.Assembly.GetExecutingAssembly().Location) + "/config.json");
            string JsonConfig = streamReader.ReadToEnd();
            DomainConfigs = methods_com.DeserializeString<Config>(JsonConfig);

            var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(DomainConfigs.AccessKey, DomainConfigs.SecretKey);
            var config = new AmazonS3Config { ServiceURL = DomainConfigs.EndpointUrl };
            _s3Client = new AmazonS3Client(awsCredentials, config);

            foreach (SourceAndDestination item in DomainConfigs.Directories)
            {
                if (Directory.Exists(item.Path))
                {
                    var directory = new DirectoryInfo(item.Path);
                    var myFile = (from f in directory.GetFiles()
                                  orderby f.LastWriteTime descending
                                  select f).First();

                    //// The method expects the full path, including the file name.
                    var path = $"{item.Path}/{myFile.Name}";
                    Console.WriteLine("\n\n Please Wait, Object is uploading....... \n\n");
                    await UploadObjectFromFileAsync(_s3Client, item.BucketName, myFile.Name, path);

                }
                else
                {
                    writeLogs($"The directory {item.Path} didn't exist");
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

                putRequest.Metadata.Add("x-amz-meta-title", "someTitle");

                PutObjectResponse response = await client.PutObjectAsync(putRequest);

                foreach (PropertyInfo prop in response.GetType().GetProperties())
                {
                    Console.WriteLine($"{prop.Name}: {prop.GetValue(response, null)}");
                }
                string FinalStatus = $"Object {objectName} added to {bucketName} bucket";
                Console.WriteLine(FinalStatus);
                writeLogs(FinalStatus);
            }
            catch (AmazonS3Exception e)
            {
                string ErrorMessage = $"Error: {e.Message}";
                Console.WriteLine(ErrorMessage);
                writeLogs(ErrorMessage);
            }
        }
        private static void writeLogs(string log)
        {
            try
            {
                if (File.Exists(System.IO.Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location) + "/" + DomainConfigs.LogFileName))
                {
                    StreamWriter streamWriter = new StreamWriter(System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location) + "/" + DomainConfigs.LogFileName,append:true);
                    streamWriter.WriteLine(DateTime.Now.ToString("yyyy/MM/dd - HH:mm") + " : " + log);
                    streamWriter.Close();
                }
            }
            catch { }
        }
    }

}
