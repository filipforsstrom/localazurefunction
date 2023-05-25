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
    private readonly int outputMaxHeight = 150;
    private readonly int outputMaxWidth = 150;
    private readonly int inputMaxHeight = 1080;
    private readonly int inputMaxWidth = 1920;

    [FunctionName("BlobFunction")]
    public void Run([BlobTrigger("drawings/{name}", Connection = "")] Stream myBlob, string name, ILogger log)
    {
        string[] fileNameWords = name.Split('.');
        string compressedFileName = $"{fileNameWords[0]}-compressed.{fileNameWords[1]}";

        using (var output = new MemoryStream())
        {
            using (var image = new MagickImage(myBlob))
            {
                if (image.Width > inputMaxWidth || image.Height > inputMaxHeight)
                {
                    image.Resize(outputMaxWidth, outputMaxHeight);
                    image.Write(output);
                }
                else
                {
                    myBlob.Position = 0;
                    myBlob.CopyTo(output);
                }
            }

            var optimizer = new ImageOptimizer
            {
                IgnoreUnsupportedFormats = true,
            };

            output.Position = 0;
            optimizer.Compress(output);

            BlobClient blobClient = new(_connectionString, _containerName, compressedFileName);

            blobClient.Upload(output);
        }
    }

}
