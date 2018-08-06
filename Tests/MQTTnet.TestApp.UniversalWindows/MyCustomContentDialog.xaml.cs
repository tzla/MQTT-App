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

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ContentDialogDemo01
{
    // Define your own ContentDialogResult enum
    public enum MyResult
    {
        Yes,
        No,
        Cancle,
        Nothing
    }

    public sealed partial class MyCustomContentDialog : ContentDialog
    {
        public MyResult Result { get; set; }

        public MyCustomContentDialog()
        {
            this.InitializeComponent();
            this.Result = MyResult.Nothing;
        }

        // Handle the button clicks from dialog
        private void btn1_Click(object sender, RoutedEventArgs e)
        {
            this.Result = MyResult.Yes;
            // Close the dialog
            dialog.Hide();
        }

        private void btn2_Click(object sender, RoutedEventArgs e)
        {
            this.Result = MyResult.No;
            // Close the dialog
            dialog.Hide();
        }

        private void btn3_Click(object sender, RoutedEventArgs e)
        {
            this.Result = MyResult.Cancle;
            // Close the dialog
            dialog.Hide();
        }

        private void TextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }
    }
}
