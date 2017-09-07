using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using RealwearImageAnalysis;

namespace ImageUploadAPI.Controllers
{
    public class ValuesController : ApiController
    {
        
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        /*
         * Uploads the passed in HTTPContent file to Azure. 
         * 
         * Returns OK once everything is done. 
         */ 

        public async Task<HttpStatusCode> Upload_File_To_Azure(HttpContent file)
        {
            
            string filename = file.Headers.ContentDisposition.FileName.Trim('\"'); // gets rid of quotes around filename
            var file_byte_array = await file.ReadAsByteArrayAsync();

            //get storage account
            //listed below are the parameters in order for creating a new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials
            //1. Name of your azure storage account
            //2. Your storage account access key. Can be found by in "Access keys" under Settings for your Storage account
            CloudStorageAccount storage_account;

            //make sure that you have added in your storage account name and your storage account key before executing
            Debug.Assert(!Constants.STORAGE_ACCOUNT_NAME.Equals("") && !Constants.STORAGE_ACCOUNT_ACCESS_KEY.Equals(""), "Go to ImageUploadAPI Constants.cs to add in your storage account name and access key");
            try
            {
                storage_account = new CloudStorageAccount(
                new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(Constants.STORAGE_ACCOUNT_NAME, Constants.STORAGE_ACCOUNT_ACCESS_KEY),
                true);
            }
            catch
            {
                return HttpStatusCode.Forbidden;
            }
            //create a blob client for the storage account
            CloudBlobClient blob_client = storage_account.CreateCloudBlobClient();

            //get reference to container the specified container blob storage. If the container does not exist, waits for it to be created.
            Debug.Assert(!Constants.CONTAINER_NAME.Equals(""), "Go to ImageUploadAPI Constants.cs class to add in your container name");
            CloudBlobContainer container = blob_client.GetContainerReference(Constants.CONTAINER_NAME);
            container.CreateIfNotExists();

            //create a file for the uploaded image. First checks to see if the file already exists by looking for the file name in the 
            //storage container. If it does, we don't upload it. If it doesn't, we create a memory stream out of the byte array holding
            //the image, then upload the memory stream to azure
            try
            {
                CloudBlockBlob image_file = container.GetBlockBlobReference(filename);
                Stream file_stream = new MemoryStream(file_byte_array);
                if (!image_file.Exists()) await image_file.UploadFromStreamAsync(file_stream);
            }
            catch
            {
                return HttpStatusCode.BadRequest;
            }
            //returns ok if there have been no errors
            return HttpStatusCode.OK;
        }


        /*
         * HttpPost. Post image file. Function then uploads image file to Azure blob storage and classifies image by detecting subsections of
         * the image that contain crushed cans. 
         * 
         * Returns a list of every subsection that contains a damaged can. A tuple of the row and column number of the upper-left corner of the subsection
         * is used to record a subsection that contains a damaged can. Wraps this in a tuple so it's returned as an object so the JSON decoding
         * on the client side is happy. 
         */

        [HttpPost, Route("api/classify")]
        public async Task<Tuple<int,List<Tuple<int, int>>>> Classify()
        {
            if (!Request.Content.IsMimeMultipartContent("form-data")) {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }
            var provider = new MultipartMemoryStreamProvider();
            //can upload multiple images, however in android file I only upload one at a time
            await Request.Content.ReadAsMultipartAsync(provider);


            //currently only allows one file at a time, so just take only file in contents. Image file. 
            var file = provider.Contents[0];

            var azure_result = Upload_File_To_Azure(file);
            // gets rid of quotes around filename
            string filename = file.Headers.ContentDisposition.FileName.Trim('\"'); 
            var file_byte_array = await file.ReadAsByteArrayAsync();

            //Bitmap representation of image file
            Bitmap file_bitmap = new Bitmap(new MemoryStream(file_byte_array));
            
            PredictionRequest pr = new PredictionRequest();
            List<Tuple<int, int>> classify_result = await pr.Classify_Image(file_bitmap);
            await azure_result;
            return new Tuple<int, List<Tuple<int, int>>>(0, classify_result);
        }
    }
}
