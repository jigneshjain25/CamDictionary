using System;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
//For MediaCapture  
//For Encoding Image in JPEG format  
using Windows.Storage;         //For storing Capture Image in App storage or in Picture Library  
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;  //For BitmapImage. for showing image on screen we need BitmapImage format.  
using WindowsPreview.Media.Ocr;

namespace CamDictionary
{
    class OcrManager
    {
        WriteableBitmap bitmap;
        private OcrEngine ocrEngine;
        private TextBox ocrText;

        public OcrManager(TextBox textBox)
        {
            ocrEngine = new OcrEngine(OcrLanguage.English);
            ocrText = textBox;
        }

        /// <summary>
        /// Loads image from file to bitmap and displays it in UI.
        /// </summary>
        public async Task LoadImage(StorageFile file)
        {
            ImageProperties imgProp = await file.Properties.GetImagePropertiesAsync();
                        
            bool sourceSet = false; 
            using (var imgStream = await file.OpenAsync(FileAccessMode.Read))
            {                
                bitmap = new WriteableBitmap((int)imgProp.Width, (int)imgProp.Height);
                bitmap.SetSource(imgStream);
                if (imgProp.Width > 2600 || imgProp.Height > 2600)
                {
                    double scale = 1.0;
                    if (imgProp.Height > 2600)
                        scale = 2600.0 / imgProp.Height;
                    else if (imgProp.Width > 2600)
                        scale = Math.Min(scale, 2600.0 / imgProp.Width);
                    bitmap = bitmap.Resize((int)(imgProp.Width * scale), (int)(imgProp.Height * scale),  WriteableBitmapExtensions.Interpolation.Bilinear);
                }                                
            }
            ExtractText();
        }

        private async void ExtractText()
        {
            Debug.WriteLine("Inside extract function");
            // Check whether is loaded image supported for processing.
            // Supported image dimensions are between 40 and 2600 pixels.
            if (bitmap.PixelHeight < 40 ||
                bitmap.PixelHeight > 2600 ||
                bitmap.PixelWidth < 40 ||
                bitmap.PixelWidth > 2600)
            {
                ocrText.Text = "Image size is not supported." +
                                    Environment.NewLine +
                                    "Loaded image size is " + bitmap.PixelWidth + "x" + bitmap.PixelHeight + "." +
                                    Environment.NewLine +
                                    "Supported image dimensions are between 40 and 2600 pixels.";
                                
                return;
            }

            var ocrResult = await ocrEngine.RecognizeAsync((uint)bitmap.PixelHeight, (uint)bitmap.PixelWidth, bitmap.PixelBuffer.ToArray());

            // OCR result does not contain any lines, no text was recognized. 
            if (ocrResult.Lines != null)
            {
                string extractedText = "";

                // Iterate over recognized lines of text.
                foreach (var line in ocrResult.Lines)
                {
                    // Iterate over words in line.
                    foreach (var word in line.Words)
                    {
                        extractedText += word.Text + " ";
                    }
                    extractedText += Environment.NewLine;
                }

                ocrText.Text = extractedText;
            }
            else
            {
                ocrText.Text = "No text.";
            }
        }
    }
}
