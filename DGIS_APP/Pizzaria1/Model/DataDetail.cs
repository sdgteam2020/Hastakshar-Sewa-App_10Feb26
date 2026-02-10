using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGISApp
{
    public class DataDetail : INotifyPropertyChanged
    {
        private string mFileName;
        private string mFileURL;
        private string mFileSize;
        private string mDownloadTime;
        private int mProgress;

        public string FileName
        {
            get { return mFileName; }
            set
            {
                mFileName = value;
                OnPropertyChanged("FileName");
            }
        }

        public string FileURL
        {
            get { return mFileURL; }
            set
            {
                mFileURL = value;
                OnPropertyChanged("FileName");
            }
        }

        public string FileSize
        {
            get { return mFileSize; }
            set
            {
                mFileSize = value;
                OnPropertyChanged("FileSize");
            }
        }

        public string DownloadTime
        {
            get { return mDownloadTime; }
            set
            {
                mDownloadTime = value;
                OnPropertyChanged("DownloadTime");
            }
        }

        public int Progress
        {
            get { return mProgress; }
            set
            {
                mProgress = value;
                OnPropertyChanged("Progress");
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
