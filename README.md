The is a simple console application for windows (any version) that developed using .net 4.7.2
You can use it for uploading file to S3-Compatible storages (AWS or any other S3-Compatible storage providers)

Dependencies :

- .NetFramework 4.7.2
- AWSSDK.S3 (you can get it from nuget package or use the dll assemblies located in bin/release directory)
- Newtonsoft.json you can get it from nuget package or use the dll assemblies located in bin/release directory)

Setup and use :

- There is a config file (named "config.json") beside the executable file that you should fill it before first use.

* put your s3 endpointUrl , AccessKey and SecretKey

* a txt file name for writing event logs

* and set an array of source directory paths and destination s3 bucket names
	note : the application automatically upload the last created file of every directory path to its related bucket name

* set AutomaticMode to false if you want to see the steps and results on the console. Otherwise it doesn't prompt user at the begining and the ending of the process
	note : You can create a scheduled task (using Windows Task Scheduler or any other tools), to run this app automatically. you can set AutomaticMode to true. 
in this case app will open, upload and close at the scheduled time automatically.





