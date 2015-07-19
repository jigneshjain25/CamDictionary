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


using Windows.Media.Capture;      //For MediaCapture  
using Windows.Media.MediaProperties;  //For Encoding Image in JPEG format  
using Windows.Storage;         //For storing Capture Image in App storage or in Picture Library  
using Windows.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Activation;
using Windows.Media.Devices;  //For BitmapImage. for showing image on screen we need BitmapImage format.  
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace CamDictionary
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>

    public sealed partial class MainPage : Page
    {
        CoreApplicationView view;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;            

            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;            
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.

            Start_Capture_Preview_Click();
        }

        //Declare MediaCapture object globally  
        Windows.Media.Capture.MediaCapture captureManager;
        bool _isInitialized = false;       

        async private void Start_Capture_Preview_Click()
        {
            //var cameraDevice = await FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Front);
            var Videodevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            var cameraDevice = Videodevices.FirstOrDefault(item => item.EnclosureLocation != null && item.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back);
            if (cameraDevice == null)
            {
                Debug.WriteLine("No camera device found!");
                return;
            }

            // Create MediaCapture and its settings
            captureManager = new MediaCapture();

            var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };

            // Initialize MediaCapture
            try
            {
                await captureManager.InitializeAsync(settings);
                // just after initialization
                var maxResolution = captureManager.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo).Aggregate(
                                    (i1, i2) => (i1 as VideoEncodingProperties).Width > (i2 as VideoEncodingProperties).Width ? i1 : i2);
                await captureManager.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, maxResolution);
                captureManager.SetPreviewRotation(VideoRotation.Clockwise90Degrees);
                _isInitialized = true;                
            }
            catch (UnauthorizedAccessException)
            {
                Debug.WriteLine("The app was denied access to the camera");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception when initializing MediaCapture with {0}: {1}", cameraDevice.Id, ex.ToString());
            }
            
            capturePreview.Source = captureManager;   //Start preiving on CaptureElement  
            await captureManager.StartPreviewAsync();  //Start camera capturing   
        }        

        async private void Capture_Image(object sender, RoutedEventArgs e)
        {
            //Create JPEG image Encoding format for storing image in JPEG type  
            ImageEncodingProperties format = ImageEncodingProperties.CreateJpeg();

            StorageFile capturefile;

            //rotate and save the image
            using (var imageStream = new InMemoryRandomAccessStream())
            {
                try
                {
                    await captureManager.VideoDeviceController.FocusControl.UnlockAsync();
                    var focusSettings = new FocusSettings();
                    focusSettings.AutoFocusRange = AutoFocusRange.Normal;
                    focusSettings.Mode = FocusMode.Auto;
                    focusSettings.WaitForFocus = true;
                    focusSettings.DisableDriverFallback = false;
                    captureManager.VideoDeviceController.FocusControl.Configure(focusSettings);
                    await captureManager.VideoDeviceController.FocusControl.FocusAsync();
                }
                catch { }

                //generate stream from MediaCapture
                await captureManager.CapturePhotoToStreamAsync(format, imageStream);

                //create decoder and encoder
                BitmapDecoder dec = await BitmapDecoder.CreateAsync(imageStream);
                BitmapEncoder enc = await BitmapEncoder.CreateForTranscodingAsync(imageStream, dec);

                //roate the image
                enc.BitmapTransform.Rotation = BitmapRotation.Clockwise90Degrees;

                //write changes to the image stream
                await enc.FlushAsync();

                //save the image
                StorageFolder folder = KnownFolders.SavedPictures;
                capturefile = await folder.CreateFileAsync("photo_" + DateTime.Now.Ticks.ToString() + ".jpg", CreationCollisionOption.ReplaceExisting);
                string captureFileName = capturefile.Name;

                //store stream in file
                using (var fileStream = await capturefile.OpenStreamForWriteAsync())
                {
                    try
                    {
                        //because of using statement stream will be closed automatically after copying finished
                        await RandomAccessStream.CopyAsync(imageStream, fileStream.AsOutputStream());
                    }
                    catch
                    {

                    }
                }
            }

            this.Frame.Navigate(typeof(OcrText), capturefile);
        }  

        async private void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            if (captureManager != null)
            {
                await captureManager.StopPreviewAsync();  //stop camera capturing 
                captureManager.Dispose();
                captureManager = null;
            }
            if (!e.Handled && Frame.CurrentSourcePageType.FullName == "CamDictionary.MainPage")
                Application.Current.Exit();
        }

        async private void Select_Image(object sender, RoutedEventArgs e)
        {
            if (captureManager != null)
            {
                await captureManager.StopPreviewAsync();  //stop camera capturing 
                captureManager.Dispose();
                captureManager = null;
            }
            
            view = CoreApplication.GetCurrentView();

            string ImagePath = string.Empty;
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            filePicker.ViewMode = PickerViewMode.Thumbnail;

            // Filter to include a sample subset of file types
            filePicker.FileTypeFilter.Clear();            
            filePicker.FileTypeFilter.Add(".png");
            filePicker.FileTypeFilter.Add(".jpeg");
            filePicker.FileTypeFilter.Add(".jpg");

            filePicker.PickSingleFileAndContinue();            
            view.Activated += viewActivated; 
        }
        
        private async void viewActivated(CoreApplicationView sender, IActivatedEventArgs args1)
        {           
            FileOpenPickerContinuationEventArgs args = args1 as FileOpenPickerContinuationEventArgs;

            if (args != null)
            {
                if (args.Files.Count == 0) return;

                view.Activated -= viewActivated;
                StorageFile storageFile = args.Files[0];
                Debug.WriteLine(storageFile.Path);
                Debug.WriteLine(storageFile.DisplayName);
                this.Frame.Navigate(typeof(OcrText), storageFile);
            }
        }
    }
}
