using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RealwearImageAnalysis
{
    public class Constants
    {
        public const string CLASSIFY_URL = ""; //replace with your own prediction url from custom vision
        public const string PREDICTION_KEY = ""; //replace with your own prediction key from your custom vision account
        public const int NUM_PARTITIONS_W = 5; //replace with the number of partitions you want to divide by width wise
        public const int NUM_PARTITIONS_H = 5; //replace with the number of partitions you want to divide by height wise
        public const string STORAGE_ACCOUNT_NAME = ""; //replace with the name of your storage account
        public const string STORAGE_ACCOUNT_ACCESS_KEY = ""; //replace with your storage account access key
        public const string CONTAINER_NAME = ""; //replace with the name of the container in your storage account
    }
}