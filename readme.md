# How to use
Simple example:
```csharp
var mySasUrl = "https://myazuresasurl.com?helloiamasasurl";
var pathToFile = "C:\\myfile.txt";

var uploader = new AzureSasUploader(mySasUrl);

await uploader.UploadFileAsync(pathToFile);
```

The uploader has an event hook called ProgressChanged which reports upload progress. This event is fired every time the uploader finishes uploading a chunk.

```csharp
uploader.ProgressChanged += (caller, progressPercent) => 
{
    //Log progress
    Console.WriteLine($"Upload Progress: {progressPercent}%");
}

await uploader.UploadFileAsync(pathToFile);
```

You can modify the number of parallel threads that the uploader will use through its options. You can also modify the size of each chunk - which defaults to 6MB.

```csharp
var options = new AzureSasUploaderOptions 
{
    ParallelUploadThreads = 4, //Default: 4
    ChunkSizeInBytes = 1024 * 1024 * 6 //Default: 6MB
};

var uploader = new AzureSasUploader(mySasUrl, options);
```