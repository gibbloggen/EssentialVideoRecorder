/*
            Program: Essential Vidoe Recorder
            Author:  John Leone
            Email:   gibbloggen@gmail.com
            Date:    9-10-2016
            License: MIT License
            Purpose: A Universal Windows app for both win 10 desktop and mobile



The MIT License (MIT) 
Copyright (c) <2016> <John Leone, gibbloggen@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


References for this program,,,

1)   https://msdn.microsoft.com/en-us/windows/uwp/audio-video-camera/camera  Took bits and pieces of this very helpful
2)   https://github.com/Microsoft/Windows-universal-samples   This also has mmany of the ingredients; however, it is a little tricky to pry it to stand alone.


*/



using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Media.Capture;
using Windows.Media.Editing;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Media.MediaProperties;
using System.Diagnostics;
using Windows.Storage.Pickers;
using System.Linq;
using Windows.Devices.Enumeration;

using System.Resources;
using System.Reflection;
using Windows.ApplicationModel.Resources;
using Windows.Services.Store;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace EssentialVideoRecorder
{
    /// <summary>
    /// It is only this page, this is a very simple app, but it provides a bit of a need.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        
       
        MediaCapture _mediaCapture;  //This is the basic capture that you pass to the xaml
        StorageFile videoFile = null;  // This is the file that it will record to.  Changeable by user.
        private MediaEncodingProfile _encodingProfile;  //These are setting attributes, going to do more with these in the future.
        LowLagMediaRecording _mediaRecording;  //This is from one of the posts, it stems off diagnostics, something that will need to be beefed up.
        StorageFolder captureFolder;  //This defaults to the videos folder, that it has perms for.  OpenPicker, allows the user to save anywhere.
        bool isRecording = false;  // recording flag, not fully utilized yet.
        int incognitoer = 0;
        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");  //I have no idea what this is, but you need it :-)

        private bool filejustcreated = false;

        public ResourceLoader languageLoader;
        private StoreContext context = null;
        private string thanks;
        private string f;


        public MainPage()
        {
            this.InitializeComponent();

           // Versatile.Height = 480;
            //Versatile.Width = 640;
            InitCamera();
          
            languageLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
            thanks = languageLoader.GetString("ManyThanks");
            Window.Current.SizeChanged += Current_SizeChanged;

        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {

           // GetTheVideo.Width = e.Size.Width;
            //GetTheVideo.Height = e.Size.Height;

            Versatile.Width = e.Size.Width;
           Versatile.Height = e.Size.Height;


        }

        //This came from a Tutorial page, with all my camera's, Logitech, they are all identical.  But may need it for other users in the future.
        private void CheckIfStreamsAreIdentical()
        {
            if (_mediaCapture.MediaCaptureSettings.VideoDeviceCharacteristic == VideoDeviceCharacteristic.AllStreamsIdentical ||
                _mediaCapture.MediaCaptureSettings.VideoDeviceCharacteristic == VideoDeviceCharacteristic.PreviewRecordStreamsIdentical)
            {
                System.Diagnostics.Debug.WriteLine("Preview and video streams for this device are identical. Changing one will affect the other");
            }
        }


        private void PopulateStreamPropertiesUI(MediaStreamType streamType, ComboBox comboBox, bool showFrameRate = true)
        {
            // Query all properties of the specified stream type 
            IEnumerable<StreamPropertiesHelper> allStreamProperties =
                _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(streamType).Select(x => new StreamPropertiesHelper(x));

            // Order them by resolution then frame rate
            allStreamProperties = allStreamProperties.Where(x => x.FrameRate > 9).OrderByDescending(x => x.Height * x.Width).ThenByDescending(x => x.FrameRate);

            // Populate the combo box with the entries
            foreach (var property in allStreamProperties)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem();
                comboBoxItem.Content = property.GetFriendlyName(showFrameRate);
                comboBoxItem.Tag = property;
                comboBox.Items.Add(comboBoxItem);
            }
        }

        public async void GetVideoSettings()
        {

            string deviceId = string.Empty;
            // Window.Devices.Enumeration.

            // Finds all video capture devices
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);







            foreach (var device in devices)
            {
                // Check if the device on the requested panel supports Video Profile
                if (MediaCapture.IsVideoProfileSupported(device.Id)) // && device.EnclosureLocation.Panel == panel)
                {
                    // We've located a device that supports Video Profiles on expected panel
                    deviceId = device.Id;
                    break;
                }
            }

            // return deviceId;



            var mediaInitSettings = new MediaCaptureInitializationSettings { VideoDeviceId = deviceId };

            IReadOnlyList<MediaCaptureVideoProfile> profiles = MediaCapture.FindAllVideoProfiles(deviceId);

            var match = (from profile in profiles
                         from desc in profile.SupportedRecordMediaDescription
                         where desc.Width == 640 && desc.Height == 480 && Math.Round(desc.FrameRate) == 30
                         select new { profile, desc }).FirstOrDefault();

            if (match != null)
            {
                mediaInitSettings.VideoProfile = match.profile;
                mediaInitSettings.RecordMediaDescription = match.desc;
            }
            else
            {
                // Could not locate a WVGA 30FPS profile, use default video recording profile
                mediaInitSettings.VideoProfile = profiles[0];
            }
        }



        public async void PurchaseAddOn(string storeId)
        {


            try
            {


                if (context == null)
                {
                    context = StoreContext.GetDefault();

                }

                workingProgressRing.IsActive = true;
                StorePurchaseResult result = await context.RequestPurchaseAsync(storeId);
                workingProgressRing.IsActive = false;

                /*if (result.ExtendedError != null)
                {
                    // The user may be offline or there might be some other server failure.
                    storeResult.Text = $"ExtendedError: {result.ExtendedError.Message}";
                    storeResult.Visibility = Visibility.Visible;
                    return;
                }*/

                switch (result.Status)
                {
                    case StorePurchaseStatus.AlreadyPurchased:
                        storeResult.Text = "The user has already purchased the product.";
                        storeResult.Visibility = Visibility.Visible;
                        break;

                    case StorePurchaseStatus.Succeeded:
                        //storeResult.Text = "The purchase was successful.";
                        ManyThanks.Visibility = Visibility.Visible;
                        break;

                    case StorePurchaseStatus.NotPurchased:
                        storeResult.Text = "The user cancelled the purchase.";
                        storeResult.Visibility = Visibility.Visible;
                        break;

                    case StorePurchaseStatus.NetworkError:
                        storeResult.Text = "The purchase was unsuccessful due to a network error.";
                        storeResult.Visibility = Visibility.Visible;
                        break;

                    case StorePurchaseStatus.ServerError:
                        storeResult.Visibility = Visibility.Visible;
                        storeResult.Text = "The purchase was unsuccessful due to a server error.";
                        break;

                    default:
                        storeResult.Text = "The purchase was unsuccessful due to an unknown error.";
                        storeResult.Visibility = Visibility.Visible;
                        break;
                }
            } catch (Exception ex)
            {
                storeResult.Text = "The purchase was unsuccessful due to an unknown error.";
                storeResult.Visibility = Visibility.Visible;
            }
        }


        private async void InitCamera()

        {
            stopRecording.Visibility = Visibility.Collapsed;
            var videosLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
            captureFolder = videosLibrary.SaveFolder ?? ApplicationData.Current.LocalFolder;


            DeviceInformationCollection j = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);




            var z = j.Count();

           var  Oz =  j.OrderBy(x => x.Name);

            if (j.Count == 0) //messagebox in all languages, no device.
            {

                NoCamera.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                string[] dups = new string[j.Count];
                int WhichDevice = 0;
                string nameThatCamera = "";
                int howManyCameras = 0;
                foreach (DeviceInformation q in Oz)
                {
                    nameThatCamera = q.Name;
                    if (WhichDevice > 0)
                    {
                        for (int v = 0; v < WhichDevice; v++)
                        {
                            if (nameThatCamera == dups[v]) { howManyCameras++; }
                        }
                        if (howManyCameras == 0) { }
                        else
                        {
                            howManyCameras++;
                            nameThatCamera = nameThatCamera + '-' + howManyCameras.ToString();
                        }
                        howManyCameras = 0;
                    }
                    dups[WhichDevice] = q.Name;
                    WhichDevice++;
                    ComboBoxItem comboBoxItem = new ComboBoxItem();
                    comboBoxItem.Content = nameThatCamera;

                    comboBoxItem.Tag = q;
                    CameraSource.Items.Add(comboBoxItem);

                }
            }
            catch (Exception e)
            {

                BadDevice.Visibility = Visibility.Visible;

            }

            try
            {

                

                DeviceInformation gotCamera = (DeviceInformation)Oz.First();
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                settings.VideoDeviceId = gotCamera.Id;
                _mediaCapture = new MediaCapture();

                await _mediaCapture.InitializeAsync();
                _mediaCapture.Failed += _mediaCapture_Failed;

                _mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;


                GetTheVideo.Source = _mediaCapture;

                CheckIfStreamsAreIdentical();

                MediaStreamType streamType;

                streamType = MediaStreamType.VideoRecord;
                PopulateStreamPropertiesUI(streamType, CameraSettings2);

                //added to camera and settings initial setting
                CameraSource.SelectedIndex = 0;
                CameraSettings2.SelectedIndex = 0;
                //await _mediaCapture.StartPreviewAsync();

                // PopulateSettingsComboBox();

            }
            catch (Exception e)
            {

                BadSetting.Visibility = Visibility.Visible;
            }
        }

        private async void ComboBoxSettings_Changed(object sender, RoutedEventArgs e)
        {

            string errIsCommon = "noError";
            BadSetting.Visibility = Visibility.Collapsed;

            try {
                if ((!isRecording) && (CameraSettings2.SelectedIndex > -1))
                {

             /*       if (!skipper)
                    {
                        skipper = !skipper;
                        throw new Exception("Test Exception");
                    }
                    skipper = !skipper;
                    */
                    errIsCommon = "Reading Combo Settings";
                    ComboBoxItem selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;
                    var encodingProperties = (selectedItem.Tag as StreamPropertiesHelper).EncodingProperties;

                    /*

                    string grabResolution = selectedItem.Content.ToString();
                    Single width = Convert.ToSingle(grabResolution.Substring(0, grabResolution.IndexOf('x')));





                    Single height = Convert.ToSingle(grabResolution.Substring(grabResolution.IndexOf('x') + 1, grabResolution.IndexOf('[') - (grabResolution.IndexOf('x') + 2)));


                    Single multiplyer = height / width;
                    //Versatile.Width = Window.Current.Bounds.Width - 100;
                    //Versatile.Height = (Window.Current.Bounds.Width - 100) * multiplyer;
                    //Versatile.Width = width / 2;
                    // Versatile.Height = height / 2;


                */
                    errIsCommon = "MediaCapture Failure";
                    await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, encodingProperties);

                    errIsCommon = "Window Sizing Error";
                    Versatile.Width = Window.Current.Bounds.Width;
                    Versatile.Height = Window.Current.Bounds.Height;
                }
            } catch(Exception ex)
            {
                BadSetting.Visibility = Visibility.Visible;

            }
                /*var selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;

                Resolutions encoderize = selectedItem.Tag as Resolutions;


                Versatile.Height = encoderize.Height;
                Versatile.Width = encoderize.Width;*/



                // await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, encodingProperties);
            
        }

        //This is the original InitCamera for Alpha 5's, to be deleted if this Alpha 6 works.
        /*  private async void InitCamera()

          {
              stopRecording.Visibility = Visibility.Collapsed;
              var videosLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
              captureFolder = videosLibrary.SaveFolder ?? ApplicationData.Current.LocalFolder;


              DeviceInformationCollection j = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);




              var z = j.Count();

              if (j.Count == 0) //messagebox in all languages, no device.
              {

                  NoCamera.Visibility = Visibility.Visible;
                  return;
              }

              try
              {
                  string[] dups = new string[j.Count];
                  int WhichDevice = 0;
                  string nameThatCamera = "";
                  int howManyCameras = 0;
                  foreach (DeviceInformation q in j)
                  {
                      nameThatCamera = q.Name;
                      if(WhichDevice > 0)
                      {
                          for(int v=0; v<WhichDevice; v++)
                          {
                              if( nameThatCamera == dups[v]) { howManyCameras++; }
                          }
                          if (howManyCameras == 0) { } else
                          {
                              howManyCameras++;
                              nameThatCamera = nameThatCamera + '-' + howManyCameras.ToString();
                          }
                          howManyCameras = 0;
                      }
                      dups[WhichDevice] = q.Name;
                      WhichDevice++;
                      ComboBoxItem comboBoxItem = new ComboBoxItem();
                      comboBoxItem.Content = nameThatCamera;

                      comboBoxItem.Tag = q;
                      CameraSource.Items.Add(comboBoxItem);

                  }
              }
              catch (Exception e) {

                  BadDevice.Visibility = Visibility.Visible;

              }

              try
              {
                  DeviceInformation gotCamera = (DeviceInformation)j.First();
                  MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                  settings.VideoDeviceId = gotCamera.Id;
                  _mediaCapture = new MediaCapture();

                  await _mediaCapture.InitializeAsync();
                  _mediaCapture.Failed += _mediaCapture_Failed;

                  _mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;


                  GetTheVideo.Source = _mediaCapture;

                  await _mediaCapture.StartPreviewAsync();

                  PopulateSettingsComboBox();

              }
              catch (Exception e) {

                  BadSetting.Visibility = Visibility.Visible;
              }
          }*/

        private void Window_Deactivated(object sender, EventArgs e)
        {
            
           
        }
        private void Window_Activated(object sender, EventArgs e)
        {
          
        }
        //.from Alpha 5, to be deleted.
       /* private void ComboBoxSettings_Changed(object sender, RoutedEventArgs e)
        {
            if ((!isRecording) && (CameraSettings.SelectedIndex > -1))
            {
                var selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;

                Resolutions encoderize = selectedItem.Tag as Resolutions;


                Versatile.Height = encoderize.Height;
                Versatile.Width = encoderize.Width;

               

              // await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, encodingProperties);
            }
        }*/
        // This also from Alpha 5
        /*
        private void PopulateSettingsComboBox()
        {
            CameraSettings.Items.Clear();
            Resolutions[] myResolutions = new Resolutions[4];
             for(int i = 1; i< 5; i++)
            {
                int x = i * 160;
                int y = i * 120;
              Resolutions j = new Resolutions();
                j.ToSee = x + " x " + y;
                j.Height = y;
                j.Width = x;
                myResolutions[i-1] = j;
            }


            // Populate the combo box with the entries
            foreach (Resolutions resolution in myResolutions)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem();
                comboBoxItem.Content = resolution.ToSee;
                comboBoxItem.Tag = resolution;
                CameraSettings.Items.Add(comboBoxItem);
            }
        }*/

        private async void _mediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            try
            {
                await _mediaRecording.StopAsync();
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Emergency stop sync failed.");
            }
            isRecording = false;
            System.Diagnostics.Debug.WriteLine("Media Capture Failed");
        }
        
        private async void startRecording_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                isRecording = true;
                startRecording.Visibility = Visibility.Collapsed;




                VideoName.IsEnabled = false;
                GetFileName.IsEnabled = false;
                CameraSettings2.IsEnabled = false;
                CameraSource.IsEnabled = false;








                _encodingProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);

                // Create storage file for the capture

                string vidname = "EssentialVideo.mp4";



                if (videoFile == null)
                {
                   
                        videoFile = await captureFolder.CreateFileAsync(vidname, CreationCollisionOption.GenerateUniqueName);
                   
                }
                else if(videoFile.Name.Contains("EssentialVideo"))
                {
                    videoFile = await captureFolder.CreateFileAsync(vidname, CreationCollisionOption.GenerateUniqueName);

                } else if (videoFile.IsAvailable) { }
                else
                {
                    Debug.WriteLine("What should I do with this?" + videoFile.Path);
                }
                /*   {
                       if (videoFile.IsAvailable) { } else await vidoeFile.

                       vidname = videoFile.Name;

                   }
                   if (!filejustcreated)
                   {
                       videoFile = await captureFolder.CreateFileAsync(vidname, CreationCollisionOption.GenerateUniqueName);
                   }
                   else filejustcreated = false;
                   */


                Debug.WriteLine("Starting recording to " + videoFile.Path);

                await _mediaCapture.StartRecordToStorageFileAsync(_encodingProfile, videoFile);
                isRecording = true;

                Debug.WriteLine("Started recording to: " + videoFile.Path);
                stopRecording.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                // File I/O errors are reported as exceptions
                isRecording = false;
                Debug.WriteLine(ex.Message);

            }
        }

        private async void stopRecording_Tapped(object sender, TappedRoutedEventArgs e)
        {
            isRecording = false;
            try
            {
                stopRecording.Visibility = Visibility.Collapsed;
                await _mediaCapture.StopRecordAsync();
                VideoName.IsEnabled = true;
                GetFileName.IsEnabled = true;
                CameraSettings2.IsEnabled = true;
                CameraSource.IsEnabled = true;
                startRecording.Visibility = Visibility.Visible;
                VideoName.Text = "Pick New File Name";
                videoFile = null;

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void MediaCapture_RecordLimitationExceeded(MediaCapture sender)
        {
            try
            {
                await _mediaRecording.StopAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }


            RecordLimit.Visibility = Visibility.Visible;
            System.Diagnostics.Debug.WriteLine("Record limitation exceeded.");
            isRecording = false;
        }

        private async  void GetFileName_Tapped(object sender, TappedRoutedEventArgs e)
        {
           
            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.FileTypeChoices.Add("MP4 video", new List<string>() { ".MP4" });
            fileSavePicker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            StorageFile file = await fileSavePicker.PickSaveFileAsync();
            if (file != null)
            {
                videoFile = file;
                VideoName.Text = videoFile.Name;
                filejustcreated = true;

            }
        }

        private void Reset_Tapped(object sender, TappedRoutedEventArgs e)
        {

           // Versatile.Height = 480;
            //Versatile.Width = 640;
            BadDevice.Visibility = Visibility.Collapsed;
            BadSetting.Visibility = Visibility.Collapsed;
            NoCamera.Visibility = Visibility.Collapsed;
            RecordLimit.Visibility = Visibility.Collapsed;
            ManyThanks.Visibility = Visibility.Collapsed;
            CameraSource.Items.Clear();
            InitCamera();
            //CameraSettings2.SelectedIndex = -1;

        }

        private void Incognito_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Reset.Visibility = Visibility.Collapsed;
            Incognito.Visibility = Visibility.Collapsed;
            VideoName.Visibility = Visibility.Collapsed;
            GetFileName.Visibility = Visibility.Collapsed;
            startRecording.Visibility = Visibility.Collapsed;
            stopRecording.Visibility = Visibility.Collapsed;
            CameraSettings2.Visibility = Visibility.Collapsed;
            BadBorder.Visibility = Visibility.Collapsed;
            CameraSource.Visibility = Visibility.Collapsed;
            Donator.Visibility = Visibility.Collapsed;
            makeDonation.Visibility = Visibility.Collapsed;
            storeResult.Visibility = Visibility.Collapsed;
            BadDevice.Visibility = Visibility.Collapsed;
            BadSetting.Visibility = Visibility.Collapsed;
            NoCamera.Visibility = Visibility.Collapsed;
            RecordLimit.Visibility = Visibility.Collapsed;
            HelpButton.Visibility = Visibility.Collapsed;
            ManyThanks.Visibility = Visibility.Collapsed;

            VideoName.Width = 10;
            startRecording.Width = 10;
            stopRecording.Width = 10;
            CameraSettings2.Width = 10;
            incognitoer = 1;



            Windows.UI.Xaml.Controls.Frame j = (Windows.UI.Xaml.Controls.Frame) VersatilePage.Parent;

            Windows.UI.Xaml.Controls.Border z = (Windows.UI.Xaml.Controls.Border) j.Parent;

           Windows.UI.Xaml.Controls.ScrollViewer r = (Windows.UI.Xaml.Controls.ScrollViewer ) z.Parent;

            
        }

        private void Versatile_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if(incognitoer == 2)
            {
                incognitoer = 0;
                Reset.Visibility = Visibility.Visible;
                Incognito.Visibility = Visibility.Visible;
                VideoName.Visibility = Visibility.Visible;
                GetFileName.Visibility = Visibility.Visible;
                CameraSource.Visibility = Visibility.Visible;
				HelpButton.Visibility = Visibility.Visible;
                //Info.Visibility = Visibility.Visible;
                if (!isRecording)
                {
                    startRecording.Visibility = Visibility.Visible;
                }
                else
                {
                    stopRecording.Visibility = Visibility.Visible;
                }
                
                CameraSettings2.Visibility = Visibility.Visible;
                BadBorder.Visibility = Visibility.Visible;
                makeDonation.Visibility = Visibility.Visible;

                VideoName.Width = 200;
                startRecording.Width = 200;
                stopRecording.Width = 200;
                CameraSettings2.Width = 250;
            }
            if (incognitoer == 1) incognitoer = 2;
        }

        private async void Devicechanged()
        {
            try
            {

                await _mediaCapture.StopPreviewAsync();

                _mediaCapture.Dispose();

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);

            }

            try
            {
                //  int q = 1000;
                //  do { q = 1000; do { q--; } while (q > 0); } while (CameraSource.Items.Count == 0); 

                ComboBoxItem selectedItem = (ComboBoxItem)CameraSource.SelectedItem;

                DeviceInformation gotCamera = selectedItem.Tag as DeviceInformation;

                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                settings.VideoDeviceId = gotCamera.Id;
                System.Diagnostics.Debug.WriteLine("Cam ID" + gotCamera.Id.ToString());
                _mediaCapture = new MediaCapture();

                await _mediaCapture.InitializeAsync(settings);
                _mediaCapture.Failed += _mediaCapture_Failed;

                _mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;


                GetTheVideo.Source = _mediaCapture;


                MediaStreamType streamType;

                CameraSettings2.Items.Clear();
                streamType = MediaStreamType.VideoRecord;
                PopulateStreamPropertiesUI(streamType, CameraSettings2);

                //added to camera and settings initial setting
                //CameraSource.SelectedIndex = 0;
                CameraSettings2.SelectedIndex = 0;

                await _mediaCapture.StartPreviewAsync();




            }
            catch (Exception x)
            {

            }

        }



        //alpha5 legacy code, to delete.
        /*
        private async void Devicechanged()
        {
            try
            {
                
             await _mediaCapture.StopPreviewAsync();

            _mediaCapture.Dispose();

            } catch ( Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);

            }

            try
            {
              //  int q = 1000;
              //  do { q = 1000; do { q--; } while (q > 0); } while (CameraSource.Items.Count == 0); 

                ComboBoxItem selectedItem = (ComboBoxItem ) CameraSource.SelectedItem;

                DeviceInformation gotCamera = selectedItem.Tag as DeviceInformation;

                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                settings.VideoDeviceId = gotCamera.Id;
                System.Diagnostics.Debug.WriteLine("Cam ID" + gotCamera.Id.ToString());
                _mediaCapture = new MediaCapture();

                await _mediaCapture.InitializeAsync(settings);
                _mediaCapture.Failed += _mediaCapture_Failed;

                _mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;


                GetTheVideo.Source = _mediaCapture;

                await _mediaCapture.StartPreviewAsync();
            } catch ( Exception x)
            {

            }    
            
        }*/

        private void makeDonation_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Donator.Visibility == Visibility.Visible)
                Donator.Visibility = Visibility.Collapsed;
            else Donator.Visibility = Visibility.Visible;

            storeResult.Visibility = Visibility.Collapsed;


        }

        private void Donation1_Tapped(object sender, TappedRoutedEventArgs e)
        {
            PurchaseAddOn("9nblggh43gh3");

            Donator.Visibility = Visibility.Collapsed;
        }

        private void Donation2_Tapped(object sender, TappedRoutedEventArgs e)
        {
            PurchaseAddOn("9nblggh43gh0");
            Donator.Visibility = Visibility.Collapsed;
        }

        private void Donation3_Tapped(object sender, TappedRoutedEventArgs e)
        {
            PurchaseAddOn("9nblggh43gx7");
            Donator.Visibility = Visibility.Collapsed;
        }

        private void Devicechanged_Changed2(object sender, SelectionChangedEventArgs e)
        {
            if (CameraSource.Items.Count == 0) return;
            else Devicechanged();
        }

		private void HelpButton_Tapped(object sender, TappedRoutedEventArgs e)
		{
			Frame.Navigate(typeof(Help));
		}
	}

	class Resolutions
    {
        public string ToSee;
        public int Width;
        public int Height;
        
       

    }
}
