using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Drawing;
using System.IO;


namespace RealwearImageAnalysis
{
    public class PredictionRequest
    {

        /*
         * Sends passed in byte array of subimage to your Custom Vision account. Pass in your own prediction key and custom vision
         * prediction url endpoint. 
         * 
         * Parameter byte[] byte_data: byte array representation of subimage
         * Parameter int r: row number of current subimage
         * Parameter int c: column number of current subimage
         * 
         * Returns the probability of every tag for that image sorted into descending order.
         */ 
        static async Task<List<Pred>> Classify_Subimage(byte[] byte_data, int r, int c)
        {
            var client = new HttpClient();

            //Swap Constants.PREDICTION_KEY for the prediction key associated with your account. Can be found by clicking the settings icon
            client.DefaultRequestHeaders.Add("Prediction-Key", Constants.PREDICTION_KEY);
            HttpResponseMessage response;
            using (var content = new ByteArrayContent(byte_data))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                //PostAsync to the prediction url for your Custom Vision project. To find the prediction url, click on the performance tab at the top of your project,
                //then click on "Prediction URL". Use the url provided under "If you have an image file."
                response = await client.PostAsync(Constants.CLASSIFY_URL, content);
                Console.WriteLine(await response.Content.ReadAsStringAsync());
            }
            //list of all tags and their probabilities returned as a string
            string pred = await response.Content.ReadAsStringAsync();
            //Use regex to parse out every tag and probability pair
            string regex = "\"(?:Tag\":\")([^\"]*)\",\"(?:Probability)\":([^}]*)";
            MatchCollection mc = Regex.Matches(pred, regex);
            List<Pred> pred_order = new List<Pred>();
            foreach(Match m in mc)
            {
                //create Pred object out of each pair, add to list of all the tags and predictions for the subimage
                String tag = m.Groups[1].Value;
                double p = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture.NumberFormat);
                pred_order.Add(new Pred(tag, p, r, c));
            }
            //order by decreasing probability of tag
            List<Pred> sorted_list = pred_order.OrderByDescending(o => o.Pred_Double).ToList();
            return sorted_list;
        }

        /*
         * Based on the tags and probabilities for each subimage, returns whether there is a can or not for that subimage.
         * 90% probability threshold to be called a can. If not met, will return "not can", even if "can" is the more
         * probable tag. 
         */ 

        static bool Is_Can(List<Pred> pred_list)
        {
            for (var i = 0; i < pred_list.Count; i++)
            {
                if (pred_list[i].Pred_String.Equals("can") && pred_list[i].Pred_Double >= 0.9) return true;
                if (pred_list[i].Pred_String.Equals("not can")) return false;
            }
            return false;
        }

        /*
         * Based on the tags and probabilities for each subimage, returns whether the can is crushed or not for that subimage.
         * 90% probability threshold to be called crushed. If not met, will return "uncrushed", even if "crushed" is the more
         * probable tag. 
         */

        static string Crushed(List<Pred> pred_list)
        {
            for (var i = 0; i < pred_list.Count; i++)
            {
                if (pred_list[i].Pred_String.Equals("crushed") && pred_list[i].Pred_Double >= 0.9) return "crushed";
                if (pred_list[i].Pred_String.Equals("uncrushed")) return "uncrushed";
            }
            return "uncrushed";
        }

        /*
         * Finds all subsections of crushed cans within an image. First splices up image into subimages, I do 25 subimages (5x5), but can be changed
         * to other dimensions by substituting NUM_PARTITIONS_H and NUM_PARTITIONS_W for desired dimensions. Then uses API calls to Custom Vision to 
         * classify each subimage.
         * 
         * Returns a list of the row and column number for each subimage that contains a crushed can. 
         */

        public async Task<List<Tuple<int, int>>> Classify_Image(Bitmap bitmap)
        {
         
            int height = bitmap.Height;
            int width = bitmap.Width;
            
            //List of all tasks, one for each subsection
            List<Task<List<Pred>>> tasks = new List<Task<List<Pred>>>();
            Bitmap section;

            //Swap out NUM_PARTITIONS_H and NUM_PARTITIONS_W with the number of images you want to divide the entire image into height wise and width-wise, respectively.

            for (int r = 0; r < Constants.NUM_PARTITIONS_H; r++)
            {
                for (int c = 0; c < Constants.NUM_PARTITIONS_W; c++)
                {
                    //Clone a subsection of the image, then convert into byte array
                    Rectangle rect = new Rectangle(c * width / Constants.NUM_PARTITIONS_W, r * height / Constants.NUM_PARTITIONS_H, width / Constants.NUM_PARTITIONS_W, height / Constants.NUM_PARTITIONS_H);
                    section = bitmap.Clone(rect, bitmap.PixelFormat);
                    byte[] section_barr;
                    using (var stream = new MemoryStream())
                    {
                        section.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        section_barr = stream.ToArray();
                    }

                    section.Dispose();
                    section = null;
                    int row = r;
                    int col = c;

                    //Pass byte array of subsection into Classify_Subimage. This is done with tasks
                    var retval = new Task<List<Pred>>(() => Classify_Subimage(section_barr, row, col).Result);
                    tasks.Add(retval);
                    retval.Start();
                }
            }

            //awaiting all subimage classification tasks to complete
            await Task.WhenAll(tasks.ToArray());

            //keeps track of the row and col for each subimage that contains crushed cans
            List<Tuple<int, int>> crushed_coordinates = new List<Tuple<int, int>>();

            foreach(var task in tasks)
            {
                List<Pred> sorted_pred = task.Result;
                Console.WriteLine(sorted_pred.ToString());
                int r = sorted_pred[0].Coordinates.Item1;
                int c = sorted_pred[0].Coordinates.Item2;
                if (Is_Can(sorted_pred))
                {
                    if (Crushed(sorted_pred).Equals("crushed"))
                    {
                        crushed_coordinates.Add(new Tuple<int, int>(r, c));
                    }
                }
                
                
            }
            return crushed_coordinates;
        }
        
    }
}