using System;
using System.IO;
using Azure.Storage.Blobs;
using ImageMagick;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace localazurefunction;

public class BlobFunction
{
    private readonly string _connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
    private readonly string _containerName = Environment.GetEnvironmentVariable("ContainerName");

    [FunctionName("BlobFunction")]
    public void Run([BlobTrigger("samples-workitems/{name}", Connection = "")]Stream myBlob, string name, ILogger log)
    {
        log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
        // compress
        byte[] imageToCompress;
        using (MemoryStream memoryStream = new MemoryStream())
        {
            myBlob.CopyTo(memoryStream);
            var optimizer = new ImageOptimizer();
            optimizer.Compress(memoryStream);
            // optimizer.Compress(memoryStream);
            imageToCompress = memoryStream.ToArray();
            memoryStream.Position = 0;
        }
        
        // rename blob
        // drawing_123123-12312-wefwef-12332.jpg
        string[] fileNameWords = name.Split('.');
        string compressedFileName = $"{fileNameWords[0]}-compressed.{fileNameWords[1]}";
        // create a new blob with name compressedFileName
        // upload to another container

        BlobClient blobClient = new(_connectionString, _containerName, compressedFileName);
        blobClient.Upload(myBlob);
    }
}
