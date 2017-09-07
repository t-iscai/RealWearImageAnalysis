Project Description

  This is the product I've been working on as an intern at Microsoft. It's written in C# on Visual Studio. The solution has two projects. The first project is AnalyzeImage and it's a Xamarin application that targets the Android platform and serves as the client side for the second project ImageUploadAPI, which is a .NET API. You can select an image on the Android side, POST that image to the web API, which then uploads the image to Azure, while at the same time using Azure's Custom Vision Service to detect anomalies in the image. Anomalies are detected by splitting the image up into subimages, then classifying each subimage. I originally used it to detect instances of crushed cans, but you can change this to detect whatever you want. The anomalies are highlighted on the final displayed image.

Setup Instructions for ImageUploadAPI

  To get ImageUplaodAPI you must first have a Custom Vision Service project and a storage account on Azure. To create the Custom Vision project, you click new project once you create an account on customvision.ai. After this, you can upload images, tag them, then click the green button at the top of the web page that says Train. If you already have an existing project, you can skip the above step. Below is how to create a storage account

  Steps to create a storage account:
  1. Login to your Azure portal.
  2. Go to "Storage accounts", click Add then you'll be at "Create storage account." Name your storage account.
  3. Under Deployment model Azure describes the two options as, "use Resource manager for new applications and the latest Azure website and use classic if you have existing applications deployed in a Classic virtual network." I used Resource manager because they fit my needs better.
  4. Click "Blob storage" under Account kind
  5. Under Replication select the RA-GRS option
  6. Click Hot under Access tier
  7. Select your Subscription and Resource group. For Resource group, choose whether you want to create a new one or use an existing one

  Once you have your Custom Vision project and Azure storage account set up, you MUST fill out the empty constants in the Constants.cs class under ImageUploadAPI. If you don't, the program will crash because there are asserts throughout the code that check to see whether these fields are filled. Here is how to fil them

  1. CLASSIFY_URL: This is the prediction url from your Custom Vision project. To find it, click on the performance tab at the top of your Custom Vision project. Then click on "Prediction URL". Use the url provided under "If you have an image file."
PREDICTION_KEY: Replace this with the prediction key from your custom vision account. To find this, click on the gear on the top bar, then copy the key under "Prediction."
  2. NUM_PARTITIONS_W: This is the number of partitions you want to divide your image into width wise. Default is 5
  3. NUM_PARTITIONS_H: This is the number of partitions you want to divide your image into height wise. Default is 5
  4. STORAGE_ACCOUNT_NAME: Replace this with the name of your storage account
  5. STORAGE_ACCOUNT_ACCESS_KEY: Replace this with your storage account's access key. Find by going to your storage account, then selecting "Access keys" under settings. Select an option in the "KEY" column
  6. CONTAINER_NAME: Replace this with the name you want to give your container inside your storage account. You can manually generate your container name on your azure portal, but can also generate it through the code.
  
After finishing your Web API, you should publish your .NET API on Azure. Then, you will use the generated URL in Analyze Image



Setup instructions for AnalyzeImage

  On the Android side, there is much less setup. You should be able to go directly to the Constants.cs class under AnalyzeImage and fill out the desired constants 
  1. POST_API_URL: replace this with the generate URL you get after publishing your .NET API on Azure. Be sure the URL contains "/api/classify", as this is the route specification used for the classification POST request. Alternatively, you can change the routing by going to the classify POST function in the Values Controller of ImageUploadAPI to change the route specification. 
  2. NUM_PARTITIONS_W: replace this is with the number of partitions you want your image to have width wise. Default is 5. Make sure it is the same as the NUM_PARTITIONS_W in the Constants.cs class under ImageUploadAPI 
  3. NUM_PARTITIONS_H: replace this is with the number of partitions you want your image to have height wise. Default is 5. Make sure it is the same as the NUM_PARTITIONS_H in the Constants.cs class under ImageUploadAPI
