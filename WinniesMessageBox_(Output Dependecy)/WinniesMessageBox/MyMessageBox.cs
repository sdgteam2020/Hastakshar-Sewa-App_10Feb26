using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Effects;

namespace WinniesMessageBox
{
    public class MyMessageBox
    {
        public enum Buttons { Yes_No, OK, OK_OpenFile,OK_PathOpen }

        public static string Show(string Text)
        {
            return Show(Text, Buttons.OK);
        }

        public static string Show(string Text, Buttons buttons)
        {
            W_MessageBox messageBox = new W_MessageBox(Text, buttons);
            messageBox.Show();
            return messageBox.ReturnString;
        }

        public static string ShowDialog(string Text)
        {
            return ShowDialog(Text, Buttons.OK);
        }

        public static string ShowDialog(string Text, Buttons buttons)
        {
            W_MessageBox messageBox = new W_MessageBox(Text, buttons);
            messageBox.ShowDialog();
            return messageBox.ReturnString;
        }
        public static string ShowDialogForWeb(string Filename)
        {
            return ShowDialogForWeb(Filename, Buttons.OK);
        }
        public static string ShowDialogForWeb(string Filename, Buttons buttons)
        {
            WebView messageBox = new WebView(Filename, buttons);
            messageBox.ShowDialog();
            return messageBox.ReturnString;
        }

        static BlurEffect MyBlur = new BlurEffect();
        public static void ShowBlurEffectAllWindow()
        {
            MyBlur.Radius = 20;
            foreach (Window window in Application.Current.Windows)
                window.Effect = MyBlur;
        }

        public static void StopBlurEffectAllWindow()
        {
            MyBlur.Radius = 0;
        }
    }
}
