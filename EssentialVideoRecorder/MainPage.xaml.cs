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

        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");  //I have no idea what this is, but you need it :-)






        public MainPage()
        {
            this.InitializeComponent();
            InitCamera();



        }

        private async void InitCamera()

        {
            stopRecording.Visibility = Visibility.Collapsed;
            var videosLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
            captureFolder = videosLibrary.SaveFolder ?? ApplicationData.Current.LocalFolder;
            _mediaCapture = new MediaCapture();
            await _mediaCapture.InitializeAsync();
            _mediaCapture.Failed += _mediaCapture_Failed;

            _mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;

   
            GetTheVideo.Source = _mediaCapture;

            await _mediaCapture.StartPreviewAsync();

        }

        private async void _mediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            await _mediaRecording.StopAsync();
            System.Diagnostics.Debug.WriteLine("Media Capture Failed");
        }

        private async void startRecording_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                startRecording.Visibility = Visibility.Collapsed;
                _encodingProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);
                // Create storage file for the capture


                if (videoFile == null)
                {
                    videoFile = await captureFolder.CreateFileAsync("EssentialVideo.mp4", CreationCollisionOption.GenerateUniqueName);
                }

                // Calculate rotation angle, taking mirroring into account if necessary
                //var rotationAngle = 360 - ConvertDeviceOrientationToDegrees(GetCameraOrientation());
                var rotationAngle = 0;
                // Add it to the encoding profile, or edit the value if the GUID was already a part of the properties
                 _encodingProfile.Video.Properties[RotationKey] = PropertyValue.CreateInt32(rotationAngle);


                if (videoFile.IsAvailable)  // this is just saying, hey, we got a file
                {

                    //These next three lines are just to create a 00:00:00 timespan, which is the duration of a new file
                    DateTime date1 = new DateTime(2010, 1, 1, 8, 0, 15);
                    DateTime date2 = new DateTime(2010, 1, 1, 8, 0, 15);
                    TimeSpan interval = date2 - date1;
                    
                    //this here extracts the properties of the file....
                    Windows.Storage.FileProperties.VideoProperties j = await videoFile.Properties.GetVideoPropertiesAsync();
                    if (j.Duration ==  interval )
                    {
                        //do nothing
                    } else
                    {
                        int g = 7;
                    }
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
                Debug.WriteLine(ex.Message);

            }
        }

        private async void stopRecording_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                stopRecording.Visibility = Visibility.Collapsed;
                await _mediaCapture.StopRecordAsync();
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
    }
}
