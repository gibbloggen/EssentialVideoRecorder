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



        public ResourceLoader languageLoader;


        public MainPage()
        {
            this.InitializeComponent();

            InitCamera();

           // languageLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
           // string  str = languageLoader.GetString("Test");



        }

        private async void InitCamera()

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

           
            foreach(DeviceInformation q in j)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem();
                comboBoxItem.Content = q.Name;
                comboBoxItem.Tag = q;
                CameraSource.Items.Add(comboBoxItem);
                
            }
           DeviceInformation gotCamera =(DeviceInformation) j.First();
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

        private void Window_Deactivated(object sender, EventArgs e)
        {
            
           
        }
        private void Window_Activated(object sender, EventArgs e)
        {
          
        }

        private void ComboBoxSettings_Changed(object sender, RoutedEventArgs e)
        {
            if ((!isRecording) && (CameraSettings.SelectedIndex > -1))
            {
                var selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;

                Resolutions encoderize = selectedItem.Tag as Resolutions;


                Versatile.Height = encoderize.Height;
                Versatile.Width = encoderize.Width;

               

              // await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, encodingProperties);
            }
        }

        private void PopulateSettingsComboBox()
        {
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
        }
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
                CameraSettings.IsEnabled = false;
                CameraSource.IsEnabled = false;








                _encodingProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);
                
                // Create storage file for the capture

               

                if (videoFile == null)
                {
                    videoFile = await captureFolder.CreateFileAsync("EssentialVideo.mp4", CreationCollisionOption.GenerateUniqueName);
                }

      
                
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
                CameraSettings.IsEnabled = true;
                CameraSource.IsEnabled = true;
                startRecording.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void MediaCapture_RecordLimitationExceeded(MediaCapture sender)
        {
            await _mediaRecording.StopAsync(); 
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
            }
        }

        private void Reset_Tapped(object sender, TappedRoutedEventArgs e)
        {

            Versatile.Height = 480;
            Versatile.Width = 640;
            CameraSettings.SelectedIndex = -1;

        }

        private void Incognito_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Reset.Visibility = Visibility.Collapsed;
            Incognito.Visibility = Visibility.Collapsed;
            VideoName.Visibility = Visibility.Collapsed;
            GetFileName.Visibility = Visibility.Collapsed;
            startRecording.Visibility = Visibility.Collapsed;
            stopRecording.Visibility = Visibility.Collapsed;
            CameraSettings.Visibility = Visibility.Collapsed;
            BadBorder.Visibility = Visibility.Collapsed;
            CameraSource.Visibility = Visibility.Collapsed;

            VideoName.Width = 10;
            startRecording.Width = 10;
            stopRecording.Width = 10;
            CameraSettings.Width = 10;
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
                if (!isRecording)
                {
                    startRecording.Visibility = Visibility.Visible;
                }
                else
                {
                    stopRecording.Visibility = Visibility.Visible;
                }
                
                CameraSettings.Visibility = Visibility.Visible;
                BadBorder.Visibility = Visibility.Visible;
                VideoName.Width = 200;
                startRecording.Width = 200;
                stopRecording.Width = 200;
                CameraSettings.Width = 250;
            }
            if (incognitoer == 1) incognitoer = 2;
        }

        private async void Devicechanged_Changed(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                
             await _mediaCapture.StopPreviewAsync();

            _mediaCapture.Dispose();

            } catch ( Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);

            }
            var selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;
          
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

    
            
        }
     
    }

    class Resolutions
    {
        public string ToSee;
        public int Width;
        public int Height;
        
       

    }
}
