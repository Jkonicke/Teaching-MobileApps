﻿using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content;
using System.Collections.Generic;
using Android.Content.PM;
using Android.Provider;
using System;

namespace GoogleApiExample
{
    [Activity(Label = "GoogleApiExample", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        string[] items = { "tree", "car", "door", "flashlight", "cup", "shirt", "shelf", "book", "ruler", "keyboard", "mouse", "cat", "chair", "pen", "television", "bird", "shoe", "pencile", "dog", "pattern" };
        Random rand = new Random();
        int rng;
        string find;
        Android.Graphics.Bitmap bitmap;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            Button begin = FindViewById<Button>(Resource.Id.startButton);

            rng = rand.Next(0, 19);
            find = items[19];

            begin.Click += delegate
            {
                SetContentView(Resource.Layout.preperation);
                TextView nextpic = FindViewById<TextView>(Resource.Id.thingToFind);
                nextpic.Text = string.Format("Try to take a picture of " + find);
                if (IsThereAnAppToTakePictures() == true)
                {
                    FindViewById<Button>(Resource.Id.launchCameraButton).Click += TakePicture;
                };
            };

            
        }

        

        /// <summary>
        /// Apparently, some android devices do not have a camera.  To guard against this,
        /// we need to make sure that we can take pictures before we actually try to take a picture.
        /// </summary>
        /// <returns></returns>
        private bool IsThereAnAppToTakePictures()
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            IList<ResolveInfo> availableActivities =
                PackageManager.QueryIntentActivities
                (intent, PackageInfoFlags.MatchDefaultOnly);
            return availableActivities != null && availableActivities.Count > 0;
        }


        private void TakePicture(object sender, System.EventArgs e)
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            StartActivityForResult(intent, 0);
        }

        // <summary>
        // Called automatically whenever an activity finishes
        // </summary>
        // <param name = "requestCode" ></ param >
        // < param name="resultCode"></param>
        /// <param name="data"></param>
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            SetContentView(Resource.Layout.layout1);
            ImageView submittedPic = FindViewById<ImageView>(Resource.Id.pictureToCompare);
            //AC: workaround for not passing actual files
            bitmap = (Android.Graphics.Bitmap)data.Extras.Get("data");
            submittedPic.SetImageBitmap(bitmap);

            //convert bitmap into stream to be sent to Google API
            string bitmapString = "";
            using (var stream = new System.IO.MemoryStream())
            {
                bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, 0, stream);

                var bytes = stream.ToArray();
                bitmapString = System.Convert.ToBase64String(bytes);
            }

            //credential is stored in "assets" folder
            string credPath = "google_api.json";
            Google.Apis.Auth.OAuth2.GoogleCredential cred;

            //Load credentials into object form
            using (var stream = Assets.Open(credPath))
            {
                cred = Google.Apis.Auth.OAuth2.GoogleCredential.FromStream(stream);
            }
            cred = cred.CreateScoped(Google.Apis.Vision.v1.VisionService.Scope.CloudPlatform);

            // By default, the library client will authenticate 
            // using the service account file (created in the Google Developers 
            // Console) specified by the GOOGLE_APPLICATION_CREDENTIALS 
            // environment variable. We are specifying our own credentials via json file.
            var client = new Google.Apis.Vision.v1.VisionService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                ApplicationName = "pa-3-195221",
                HttpClientInitializer = cred
            });

            //set up request
            var request = new Google.Apis.Vision.v1.Data.AnnotateImageRequest();
            request.Image = new Google.Apis.Vision.v1.Data.Image();
            request.Image.Content = bitmapString;

            //tell google that we want to perform label detection
            request.Features = new List<Google.Apis.Vision.v1.Data.Feature>();
            request.Features.Add(new Google.Apis.Vision.v1.Data.Feature() { Type = "LABEL_DETECTION" });
            var batch = new Google.Apis.Vision.v1.Data.BatchAnnotateImagesRequest();
            batch.Requests = new List<Google.Apis.Vision.v1.Data.AnnotateImageRequest>();
            batch.Requests.Add(request);

            //send request.  Note that I'm calling execute() here, but you might want to use
            //ExecuteAsync instead
            var apiResult = client.Images.Annotate(batch).Execute();

            //this looks at api results confidence is % chance, discription is the actual item itself
            //apiResult.Responses[0].LabelAnnotations[0].Description;

            Button back = FindViewById<Button>(Resource.Id.backButton);
            TextView answer = FindViewById<TextView>(Resource.Id.userInformer);
            bool found = false;
            int count = 0;

            foreach(var item in apiResult.Responses[0].LabelAnnotations)
            {
                count++;
                if (find == item.Description)
                {
                    found = true;
                }
                if(count == 10)
                {
                    break;
                }
            }

            if (found == true)
            {
                answer.Text = string.Format("Correct that is " + find);
            }
            else
            {
                answer.Text = string.Format("Sorry but the Api didn't identify your picture as " + find);
            }

            back.Text = string.Format("Push me to get your next item to find a picture of.");

            back.Click += delegate {
                SetContentView(Resource.Layout.preperation);
                rng = rand.Next(0, 19);
                find = items[rng];
                TextView nextpic = FindViewById<TextView>(Resource.Id.thingToFind);
                ImageView lastPic = FindViewById<ImageView>(Resource.Id.takenPictureImageView);
                lastPic.SetImageBitmap(bitmap);
                nextpic.Text = string.Format("Try to take a picture of " + find);
                if (IsThereAnAppToTakePictures() == true)
                {
                    FindViewById<Button>(Resource.Id.launchCameraButton).Click += TakePicture;
                };
            };
            

            // Dispose of the Java side bitmap.
            System.GC.Collect();
        }

    }
}

