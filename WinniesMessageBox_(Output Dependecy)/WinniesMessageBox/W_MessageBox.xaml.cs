using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WinniesMessageBox
{
    /// <summary>
    /// Interakční logika pro W_MessageBox.xaml
    /// </summary>
    public partial class W_MessageBox : Window
    {
        public string ReturnString { get; set; }

        public W_MessageBox(string Text, MyMessageBox.Buttons buttons)
        {
            InitializeComponent();
           
            txbText.Text = Text;

            switch (buttons)
            {
                case MyMessageBox.Buttons.OK:
                    btnOK.Visibility = Visibility.Visible;
                    break;
                case MyMessageBox.Buttons.Yes_No:
                    btnYes.Visibility = Visibility.Visible;
                    btnNo.Visibility = Visibility.Visible;
                    break;
                case MyMessageBox.Buttons.OK_OpenFile:
                    btnExit.Visibility = Visibility.Visible;
                    btnOpenFile.Visibility = Visibility.Visible;
                    btnOpenPath.Visibility = Visibility.Visible;
                    break;
                case MyMessageBox.Buttons.OK_PathOpen:
                    btnExit.Visibility = Visibility.Visible;
                    btnOpenPath.Visibility = Visibility.Visible;
                    btnExit.Margin = new Thickness(0, 10, 200, 5);
                    btnOpenPath.Margin = new Thickness(0, 9, -125, 5);
                    break;
            }
        }

        private void gBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch { }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            ReturnString = "-1";
            Close();
        }

        DoubleAnimation anim;
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Closing -= Window_Closing;
            e.Cancel = true;
            anim = new DoubleAnimation(0, (Duration)TimeSpan.FromSeconds(0.3));
            anim.Completed += (s, _) => this.Close();
            this.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Height = (txbText.LineCount * 27) + gBar.Height + 60;
        }

        private void btnReturnValue_Click(object sender, RoutedEventArgs e)
        {
            ReturnString = ((Button)sender).Uid.ToString();
            Close();
        }
    }
}
