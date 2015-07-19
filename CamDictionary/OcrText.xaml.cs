using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace CamDictionary
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>    

    public sealed partial class OcrText : Page
    {
        StorageFile image;

        Popup popup;
        Point lastCoordinates;
        PopUpUserControl control;
        InternalDictionary internalDictionaryObject;
        
        public OcrText()
        {
            this.InitializeComponent();

            //->Biswadip Maity Debugging Start
            internalDictionaryObject = new InternalDictionary();     //TODO: Optimize for time
            
            //internalDictionaryObject.searchForWord("House");
            //->Biswadip Maity Debugging End

            //->Ronak code starts
            articleText.AddHandler(TappedEvent, new TappedEventHandler(TextTapped), true);
            MakePopUp(internalDictionaryObject);
            //->Ronak code ends

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            image = e.Parameter as StorageFile;
            OcrManager ocrManager = new OcrManager(articleText);
            await ocrManager.LoadImage(image);
        }        

        void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if(rootFrame != null && rootFrame.CanGoBack)
            {
                rootFrame.GoBack();
                e.Handled = true;
            }

        }

        private void MakePopUp(InternalDictionary internalDictionaryObject)
        {
            popup = new Popup();
            popup.Height = 200;
            popup.Width = 200;
            control = new PopUpUserControl(internalDictionaryObject);
            popup.Child = control;
            popup.IsOpen = true;
            popup.Visibility = Visibility.Collapsed;
            popup.IsLightDismissEnabled = true;            
        }

        private void ShowPopUp(String meaning, double YOffset)
        {
            if(YOffset < 25)
            {
                YOffset += 100;
            }
            popup.VerticalOffset = YOffset; //touch point + textbox vertical offset
            popup.Visibility = Visibility.Visible;
            control.SetMeaning(meaning);
        }

        private void HidePopUp()
        {
            popup.Visibility = Visibility.Collapsed;
        }
        //handler called when screen is tapped
        public void TextTapped(object sender, TappedRoutedEventArgs e)
        {            
            lastCoordinates = e.GetPosition(articleText);
            //textBox2.Text = "tap detected x: "+point.X+ " y: "+point.Y;

        }

        //handler called when selection is changed
        private void TextBox1_SelectionChanged(object sender, RoutedEventArgs e)
        {
            //MakePopUp();
            //textBox2.Text = textBox1.SelectedText;
            //textBox2.Visibility = Visibility.Visible;
            //selectionText = textBox1.SelectedText;
            //label1.Text = "Selection length is " + articleText.SelectionLength.ToString();
            //label2.Text = "Selection starts at " + articleText.SelectionStart.ToString();
            string popUpText = articleText.SelectedText;
            popUpText.Trim();
            if (popUpText.Equals("") || popUpText.Contains(" "))
                HidePopUp();
            else
                ShowPopUp(popUpText, lastCoordinates.Y);
        }

    }
}
