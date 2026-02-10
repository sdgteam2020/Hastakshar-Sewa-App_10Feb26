using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WinniesMessageBox
{
    public class CloseMessage
    {
        public static void AllShortNotifications()
        {
            foreach (W_ShortNotification window in Application.Current.Windows.OfType<W_ShortNotification>())
                window.FastClose();
            while (Application.Current.Windows.OfType<W_ShortNotification>().Count() > 0) { }
        }

        public static void AllMessageBoxes()
        {
            foreach (W_MessageBox window in Application.Current.Windows.OfType<W_MessageBox>())
                window.Close();
            while (Application.Current.Windows.OfType<W_MessageBox>().Count() > 0) { }
        }

        public static void AllMessages()
        {
            AllShortNotifications();
            AllMessageBoxes();
        }
    }
}
