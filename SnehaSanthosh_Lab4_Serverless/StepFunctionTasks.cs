using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;

using Amazon.Lambda.S3Events;


using Amazon.Rekognition;
using Amazon.Rekognition.Model;

using Amazon.S3;
using Amazon.S3.Model;
using Tag = Amazon.S3.Model.Tag;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SnehaSanthosh_Lab4_Serverless
{
    public class StepFunctionTasks
    {
        /// <summary>
        /// The default minimum confidence used for detecting labels.
        /// </summary>
        public const float DEFAULT_MIN_CONFIDENCE = 90f;

        /// <summary>
        /// The name of the environment variable to set which will override the default minimum confidence level.
        /// </summary>
        public const string MIN_CONFIDENCE_ENVIRONMENT_VARIABLE_NAME = "MinConfidence";

        private ImagesDB imagesDB = new ImagesDB();
        

        IAmazonS3 S3Client { get; }

        IAmazonRekognition RekognitionClient { get; }

        float MinConfidence { get; set; } = DEFAULT_MIN_CONFIDENCE;

        HashSet<string> SupportedImageTypes { get; } = new HashSet<string> { ".png", ".jpg", ".jpeg" };

        //DynamoDB
        private static AmazonDynamoDBClient dynamoDBClient;
        private static DynamoDBContext dynamoDbContext;
        IAmazonDynamoDB DynamoDBClient { get; }
        State state = new State();
        /// Default constructor that Lambda will invoke.
        /// </summary>
        
        public StepFunctionTasks()
        {

        }
        public StepFunctionTasks(IAmazonS3 s3Client, IAmazonRekognition rekognitionClient, float minConfidence, IAmazonDynamoDB dynamoDBClient)
        {     
            this.S3Client = s3Client;
            this.RekognitionClient = rekognitionClient;
            this.MinConfidence = minConfidence;
            this.DynamoDBClient = dynamoDBClient;
            dynamoDbContext = new DynamoDBContext(dynamoDBClient);
        }

        
        /// <summary>
        /// A function for responding to S3 create events. It will determine if the object is an image
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public State ValidateImage(State state, ILambdaContext context)
        {
                if (!SupportedImageTypes.Contains(Path.GetExtension(state.Key)))
                {                 
                Console.WriteLine($"Object {state.BucketName}:{state.Key} is not a supported image type");
                state.ValidImage = false;
                    return state;
                }

            state.ValidImage = true;
            return state;
        }
        /// <summary>
        /// A function for responding to S3 create events. It will determine if the object is an image and use Amazon Rekognition
        /// to detect labels and add the labels as tags on the S3 object.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public State DetectLabels(State state, ILambdaContext context)
        {                
                Console.WriteLine($"Looking for labels in image {state.BucketName}:{state.Key}");
                using (var rekognitionClient = new AmazonRekognitionClient())
                {
                    var detectResponses =  rekognitionClient.DetectLabelsAsync(new DetectLabelsRequest
                {
                    MinConfidence = MinConfidence,
                    Image = new Image
                    {
                        S3Object = new Amazon.Rekognition.Model.S3Object
                        {
                            Bucket = state.BucketName,
                            Name = state.Key
                        }
                    }
                }).Result;

                var tags = new List<Tag>();
                    imagesDB.Labels = new Dictionary<string, AttributeValue>();
                    foreach (var label in detectResponses.Labels)
                    {
                        if (imagesDB.Labels.Count() < 10)// && imagesDB.ImageKey != record.S3.Object.Key)
                        {
                            imagesDB.Labels.Add(label.Name, new AttributeValue { N = label.Confidence.ToString() });
                            Console.WriteLine($"\tFound Label {label.Name} with confidence {label.Confidence}");

                        }
                    }
                       
                        using (var client = new AmazonDynamoDBClient())
                        {
                            int imageId = new Random().Next(1000000);
                            // string customerId = Guid.NewGuid().ToString();

                             client.PutItemAsync(new PutItemRequest
                            {
                                TableName = "ImagesDB",
                                Item = new Dictionary<string, AttributeValue>
                            {
                                { "ImageID", new AttributeValue { N = imageId.ToString()}},
                                { "ImageKey", new AttributeValue { S = state.Key}},
                                { "Labels", new AttributeValue { M = imagesDB.Labels }}
                            }
                            });
                        }
                }
            return state;
        }

       
        public async Task<string> GenerateThumbnails(State state, ILambdaContext context)
        {
    
            try
            {
                using (var s3Client = new AmazonS3Client())
                {
                    var rs = await s3Client.GetObjectMetadataAsync(
                    state.BucketName,
                    state.Key);

                    if (rs.Headers.ContentType.StartsWith("image/"))
                    {
                        using (GetObjectResponse response = await s3Client.GetObjectAsync(
                            state.BucketName,
                            state.Key))
                        {
                            using (Stream responseStream = response.ResponseStream)
                            {
                                using (StreamReader reader = new StreamReader(responseStream))
                                {
                                    using (var memstream = new MemoryStream())
                                    {
                                        var buffer = new byte[512];
                                        var bytesRead = default(int);
                                        while ((bytesRead = reader.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                                            memstream.Write(buffer, 0, bytesRead);
                                        // Perform image manipulation 
                                        var transformedImage = GCImagingOperations.GetConvertedImage(memstream.ToArray());
                                        PutObjectRequest putRequest = new PutObjectRequest()
                                        {
                                            BucketName = state.BucketName,
                                            Key = $"thumbnails/grayscale-{state.Key}",
                                            ContentType = rs.Headers.ContentType,
                                            ContentBody = transformedImage
                                        };
                                        await s3Client.PutObjectAsync(putRequest);
                                    }
                                }
                            }
                        }
                    }
                    return rs.Headers.ContentType;
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
