using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Net;
using Android.Provider;
using Android.Speech;




namespace AnalyzeImage
{
    [Activity(Label = "AnalyzeImage", MainLauncher = true, Icon = "@drawable/icon")]
    public class Activity1 : Activity
    {
        public static readonly int gallery_id = 1000; //ID given if 
        public static readonly int camera_id = 2000;
        
        private Button button_gallery; //button to select image from gallery
        private Button button_camera; //button to take picture
        private Button button_analyze; //button to analyze image and upload to azure
        private ImageView _imageView;
        
        private string image_path = ""; //path of current selected image
        private Stream image_stream; //stream of current selected image
        private byte[] image_bytes; //byte array of current selected image
        private Bitmap temp_bitmap; //temp_bitmap used to overlay shaded sections. Declared public to stop bitmaps from taking up extra space

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);
            _imageView = FindViewById<ImageView>(Resource.Id.imageView1);
            button_gallery = FindViewById<Button>(Resource.Id.MyButton); //button to click if you want to choose image from gallery
            button_camera = FindViewById<Button>(Resource.Id.button1); //button to click if you want to take picture
            button_analyze = FindViewById<Button>(Resource.Id.btnAnalyze); //button to click if you want to analyze selected image

            button_gallery.Click += Button_Gallery_Click;
            button_camera.Click += Button_Camera_Click;
            button_analyze.Click += Button_Analyze_Click;
        }


        /*
         * Function called if camera button is clicked. Passes intent to the OnActivityResult function with the camera_id ID number.
         */
        private void Button_Camera_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(Android.Provider.MediaStore.ActionImageCapture);
            StartActivityForResult(intent, camera_id);
        }

        /*
         * Function called if gallery button is clicked. Passes intent to the OnActivityResult function with the gallery_id ID number.
         */

        private void Button_Gallery_Click(object sender, EventArgs eventArgs)
        {
            Intent = new Intent();
            Intent.SetType("image/*");
            Intent.SetAction(Intent.ActionGetContent);
            StartActivityForResult(Intent.CreateChooser(Intent, "Select Picture"), gallery_id);
        }


        /*
         * Function called if analyze button is clicked. If there's no image selected, displays error as a toast.
         * If there is an image selected, calls Upload Image asynchronously. 
         */

        private async void Button_Analyze_Click(object sender, EventArgs e)
        {
            if (image_path.Equals("")) //checking to see if there is a selected image to analyze. If not, prompts user to select one
            {
                Toast.MakeText(Application.Context, "Please select an image for analysis", ToastLength.Short).Show();
            }
            else
            {
                await Upload_Image();
            }
        }

        /*
         * Calls Gallery_Activity_Result or Camera_Activity_Result depending on the request_code, and whether
         * the result code is OK and the Intent is not null
         */ 

        protected override void OnActivityResult(int request_code, Result result_code, Intent data)
        {
            AppDomain current_domain = AppDomain.CurrentDomain;
            current_domain.UnhandledException += new UnhandledExceptionEventHandler(My_Handler);

            //choosing from gallery activity result. Checks to see if the result code is OK and the intent is not null
            if (request_code == gallery_id && result_code == Result.Ok && data != null)
                Gallery_Activity_Result(data);

            //taking photo activity result. Checks to see if the result code is OK and the intent is not null
            else if (request_code == camera_id && result_code == Result.Ok)
                Camera_Activity_Result(data);

        }

        
        /*
         * Result function for clicking on the gallery button. Sets the private variable detailing the current image
         * to the image selected from the gallery.
         */ 

        private void Gallery_Activity_Result(Intent data)
        {

            Android.Net.Uri image_uri = data.Data;
            Console.WriteLine(image_uri);
            _imageView.SetImageURI(image_uri);
            string path_name = null;
            try
            {
                path_name = Get_Path_To_Image(image_uri);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in getting path to image " + e.Message);
            }

            //sets private variable image_bytes to the byte array of the current selected image
            image_bytes = File.ReadAllBytes(path_name);

            //sets private variable image_path to the path of the current selected image
            image_path = path_name;

            //sets private variable image_stream to the stream of the current selected image
            image_stream = new MemoryStream(image_bytes);
        }

        /*
         * Result function for clicking on the camera button. Sets the private variable detailing the current image
         * to the image just taken with the camera.
         */

        private void Camera_Activity_Result(Intent data)
        {

            Bitmap bitmap = (Bitmap)data.Extras.Get("data");
            _imageView.SetImageBitmap(bitmap);
            var sd_card_path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
            //sets private variable image_path to the path of the current selected image
            image_path = System.IO.Path.Combine(sd_card_path, DateTime.Now.ToString("h:mm:ss tt") + ".png");
            var image_compression_stream = new FileStream(image_path, FileMode.Create);
            bitmap.Compress(Bitmap.CompressFormat.Png, 100, image_compression_stream);


            image_compression_stream.Close();
            //sets private variable image_bytes to the byte array of the current selected image
            image_bytes = File.ReadAllBytes(image_path);
            //sets private variable image_stream to the stream of the current selected image
            image_stream = new MemoryStream(image_bytes);
        }



        /*
         * Given an image's uri, gets the local path to the image
         * 
         * Parameter uri: an image's uri
         * 
         * Returns the local path to that image.
         */

        private string Get_Path_To_Image(Android.Net.Uri uri)
        {
            string doc_id = "";
            using (var c1 = ContentResolver.Query(uri, null, null, null, null))
            {
                c1.MoveToFirst();
                String document_id = c1.GetString(0);
                doc_id = document_id.Substring(document_id.LastIndexOf(":") + 1);
            }

            string path = null;

            // The projection contains the columns we want to return in our query.
            string selection = Android.Provider.MediaStore.Images.Media.InterfaceConsts.Id + " =? ";
            using (var cursor = ManagedQuery(Android.Provider.MediaStore.Images.Media.ExternalContentUri, null, selection, new string[] { doc_id }, null))
            {
                if (cursor == null) return path;
                var column_index = cursor.GetColumnIndexOrThrow(Android.Provider.MediaStore.Images.Media.InterfaceConsts.Data);
                cursor.MoveToFirst();
                path = cursor.GetString(column_index);
            }
            return path;
        }

        //Handler for unhandled exceptions. 

        static void My_Handler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("My_Handler caught : " + e.Message);
            Console.WriteLine("Runtime terminating: {0}", args.IsTerminating);
        }

        /*
         * Function that posts selected image to the created API. Then draws shaded rectangles over the areas
         * that have been identified as containing crushed cans. 
         */ 


        private async Task<String> Upload_Image()
        {
            
            var speech_result = Plugin.TextToSpeech.CrossTextToSpeech.Current.Speak("Uploading for analysis."); //gets device to say "Uploading for analysis"
            HttpContent file_stream_content = new StreamContent(image_stream);
            var client = new HttpClient();
            var form_data = new MultipartFormDataContent();
            String file_name = image_path.Replace("/", string.Empty);
            form_data.Add(file_stream_content, file_name, file_name);

            System.Diagnostics.Debug.Assert(!Constants.POST_API_URL.Equals(""), "Go to AnalyzeImage Constants.cs to add your POST api url");
            var response = client.PostAsync(Constants.POST_API_URL, form_data).Result;

            //gets the response from the API as a string. The response is a Tuple of a throwaway int and a List that contains the tuples of the row and column
            //coordinates of all of the subsections that are identified as having crushed cans. Used a tuple with a throwaway int to make it into an object.
            String response_content_string = await response.Content.ReadAsStringAsync();
            //Uses JSON parser to parse out the List of Tuples. 
            JObject response_json;
            List<JToken> json_list;
            try
            {
                response_json = JObject.Parse(response_content_string);
                json_list = response_json["m_Item2"].ToList<JToken>();
            } catch(Exception e)
            {
                Toast.MakeText(Application.Context, "Classification encountered an error", ToastLength.Short).Show();
                return "didn't work";
            }
       
            //create bitmap out of the current selected images path name
            Bitmap bitmap = BitmapFactory.DecodeFile(image_path);
            int height = bitmap.Height;
            int width = bitmap.Width;

            //creates copy of bitmap to add to canvas so we can draw semi-opaque white rectangles over the portions with crushed cans.
            temp_bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Rgb565);
            Canvas temp_canvas = new Canvas(temp_bitmap);
            temp_canvas.DrawBitmap(bitmap, 0, 0, null);

            //recyles bitmap and sets to null to prevent from bitmaps eating up memory
            bitmap.Recycle();
            bitmap = null;

            //loops through all sections that have been identified as containing crushed cans. 
            foreach(var token in json_list){
                Tuple<int, int> coordinate = new Tuple<int, int>(token["m_Item1"].ToObject<int>(), token["m_Item2"].ToObject<int>());
                Paint rect_paint = new Paint();
                rect_paint.SetARGB(255, 255, 255, 255);
                rect_paint.SetStyle(Paint.Style.Fill);
                rect_paint.StrokeWidth = 10;
                rect_paint.Alpha = 100;

                //draws rectangles with alpha 100 over coordinates identified as having crushed cans. 

                //replace Constants.NUM_PARTITIONS_W and Constants.NUM_PARTITIONS_H with the number images you want to split by width and height, respectively.

                float top_x = coordinate.Item2 * width / Constants.NUM_PARTITIONS_W;
                float top_y = coordinate.Item1 * height / Constants.NUM_PARTITIONS_H;
                float bottom_x = (coordinate.Item2 + 1) * width / Constants.NUM_PARTITIONS_W;
                float bottom_y = (coordinate.Item1 + 1) * height / Constants.NUM_PARTITIONS_H;
                temp_canvas.DrawRect(new RectF(top_x, top_y, bottom_x, bottom_y), rect_paint);

                _imageView.SetImageDrawable(new BitmapDrawable(Application.Resources, temp_bitmap));
            }
            if (json_list.Count() == 0) Toast.MakeText(Application.Context, "There seem to be no crushed cans", ToastLength.Short).Show();
            else Toast.MakeText(Application.Context, "The highlighted areas are thought to be cans in need of maintenance", ToastLength.Short).Show();
            
            return "finished";

        }

        



    }
}