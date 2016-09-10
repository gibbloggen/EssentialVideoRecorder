using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Capture;
using Windows.Media.Editing;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Media.MediaProperties;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using System.Diagnostics;
using Windows.UI.Core;
using Windows.Storage.Pickers;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace EssentialVideoRecorder
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MediaComposition mediaComposition;
        MediaStreamSource mediaStreamSource;
        MediaCapture _mediaCapture;
        MediaClip mediaClip;
        StorageFolder videoFolder = null;
        StorageFile videoFile = null;
        private MediaEncodingProfile _encodingProfile;
        LowLagMediaRecording _mediaRecording;
        bool _isPreviewing;
        MediaElement mediaElement;
        StorageFolder captureFolder;
        bool isRecording = false;

        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");






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

          //  mediaComposition = new MediaComposition();
          //  CameraCaptureUI captureUI = new CameraCaptureUI();
            //captureUI.VideoSettings.Format = CameraCaptureUIVideoFormat.Mp4;
//
            _mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;

         //   StorageFile videoFile = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Video);

      //      if (videoFile == null)
      //      {
                // User cancelled photo capture
     //           return;
    //        }

      //      MediaClip mediaClip = await MediaClip.CreateFromFileAsync(videoFile);

      //      mediaComposition.Clips.Add(mediaClip);
      // //     mediaStreamSource = mediaComposition.GeneratePreviewMediaStreamSource(
     //           (int)mediaElement.ActualWidth,
     //           (int)mediaElement.ActualHeight);

     ///       mediaElement.SetMediaStreamSource(mediaStreamSource);

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
