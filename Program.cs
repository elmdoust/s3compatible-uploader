
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Amazon.Runtime;

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
                        switch (item.Policy)
                        {
                            case "LAST_CREATED":
                                var directory = new DirectoryInfo(item.SourceDirectoryPath);
                                var myFile = (from f in directory.GetFiles()
                                              orderby f.LastWriteTime descending
                                              select f).First();
                                var path = $"{item.SourceDirectoryPath}/{myFile.Name}";
                                Console.WriteLine("\n\n Please Wait, Object is uploading....... \n\n");
                                if (myFile.Length / 1024 / 1024 < 400)
                                    await UploadObjectFromFileAsync(_s3Client, item.S3BucketName, myFile.Name, path);
                                else
                                    await UploadObjectAsync(_s3Client, item.S3BucketName, myFile.Name, path);
                                break;
                            case "ALL_FILES":
                                string ZipFileName = $"{item.SourceDirectoryPath.Replace("/", "-").Replace(":", "")}-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm")}.zip";
                                Console.WriteLine("\n\n Zip object is creating....... \n\n");
                                Helpers.ZipFiles(item.SourceDirectoryPath, ZipFileName);
                                var myZipFileInfo = new FileInfo(ZipFileName);
                                Console.WriteLine("\n\n Please Wait, Object is uploading....... \n\n");
                                if (myZipFileInfo.Length / 1024 / 1024 < 400)
                                    await UploadObjectFromFileAsync(_s3Client, item.S3BucketName, ZipFileName, ZipFileName);
                                else
                                    await UploadObjectAsync(_s3Client, item.S3BucketName, ZipFileName, ZipFileName);

                                File.Delete(ZipFileName);
                                break;
                        }
                    }
                    else
                    {
                        Helpers.writeLogs(DomainConfigs.LogFileName, $"The directory {item.SourceDirectoryPath} didn't exist");
                    }
                }
                if (!DomainConfigs.AutoMode)
                {
                    Console.WriteLine("\nThe job finished. see the result in log file. press any key to exit...");
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

        public static async Task UploadObjectAsync(
            IAmazonS3 client,
            string bucketName,
            string keyName,
            string filePath)
        {
            // Create list to store upload part responses.
            List<UploadPartResponse> uploadResponses = new List<UploadPartResponse>();

            // Setup information required to initiate the multipart upload.
            InitiateMultipartUploadRequest initiateRequest = new InitiateMultipartUploadRequest()
            {
                BucketName = bucketName,
                Key = keyName,
            };

            // Initiate the upload.
            InitiateMultipartUploadResponse initResponse =
                await client.InitiateMultipartUploadAsync(initiateRequest);

            // Upload parts.
            long contentLength = new FileInfo(filePath).Length;
            long partSize = 200 * (long)Math.Pow(2, 20); // 400 MB

            try
            {
                Console.WriteLine("Uploading parts");

                long filePosition = 0;
                for (int i = 1; filePosition < contentLength; i++)
                {
                    UploadPartRequest uploadRequest = new UploadPartRequest()
                    {
                        BucketName = bucketName,
                        Key = keyName,
                        UploadId = initResponse.UploadId,
                        PartNumber = i,
                        PartSize = partSize,
                        FilePosition = filePosition,
                        FilePath = filePath,
                    };

                    // Track upload progress.
                    uploadRequest.StreamTransferProgress +=
                        new EventHandler<StreamTransferProgressArgs>(UploadPartProgressEventCallback);

                    // Upload a part and add the response to our list.
                    uploadResponses.Add(await client.UploadPartAsync(uploadRequest));

                    filePosition += partSize;
                }

                // Setup to complete the upload.
                CompleteMultipartUploadRequest completeRequest = new CompleteMultipartUploadRequest()
                {
                    BucketName = bucketName,
                    Key = keyName,
                    UploadId = initResponse.UploadId,
                };
                completeRequest.AddPartETags(uploadResponses);

                // Complete the upload.
                CompleteMultipartUploadResponse completeUploadResponse =
                    await client.CompleteMultipartUploadAsync(completeRequest);
                string FinalStatus = $"Object {keyName} added to {bucketName} bucket";
                Console.WriteLine(FinalStatus);
                Helpers.writeLogs(DomainConfigs.LogFileName, FinalStatus);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"An AmazonS3Exception was thrown: {exception.Message}");
                string ErrorMessage = $"Error: {exception.Message}";
                Console.WriteLine(ErrorMessage);
                Helpers.writeLogs(DomainConfigs.LogFileName, ErrorMessage);

                // Abort the upload.
                AbortMultipartUploadRequest abortMPURequest = new AbortMultipartUploadRequest()
                {
                    BucketName = bucketName,
                    Key = keyName,
                    UploadId = initResponse.UploadId,
                };
                await client.AbortMultipartUploadAsync(abortMPURequest);
            }
        }

        public static void UploadPartProgressEventCallback(object sender, StreamTransferProgressArgs e)
        {
            Console.WriteLine($"{e.TransferredBytes}/{e.TotalBytes}");
        }

    }

}
