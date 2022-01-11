using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SnehaSanthosh_Lab4_Serverless
{

    public class ImagesDB
    {
        public int ImageID { get; set; }
        public string ImageKey { get; set; }
        public Dictionary<string, AttributeValue> Labels { get; set; }
    
    }

}

