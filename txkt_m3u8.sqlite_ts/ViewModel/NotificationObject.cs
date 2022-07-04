using System.ComponentModel;

namespace txkt_m3u8.sqlite_ts.ViewModel
{
    public abstract class NotificationObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// 发起通知
        /// </summary>
        /// <param name="propertyName">属性名</param>
        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
