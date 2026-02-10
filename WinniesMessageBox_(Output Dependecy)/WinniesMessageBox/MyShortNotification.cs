using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Effects;

namespace WinniesMessageBox
{
    public class MyShortNotification
    {
       public static string Show(string text)
        {            
            W_ShortNotification shortNotification = new W_ShortNotification(text);
            shortNotification.Show();
            return "1";
        }

        public static string ShowDialog(string text)
        {
            ShowBlurEffectAllWindow();
            W_ShortNotification shortNotification = new W_ShortNotification(text);
            shortNotification.ShowDialog();
            StopBlurEffectAllWindow();
            return "1";
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
