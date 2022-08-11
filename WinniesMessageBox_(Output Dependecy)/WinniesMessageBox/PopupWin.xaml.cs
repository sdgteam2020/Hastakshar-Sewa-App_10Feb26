using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WinniesMessageBox
{
    
    public partial class PopupWin : Window
    {
        public PopupWin()
        {
            InitializeComponent();

            ProcessStackPanel.Children.Clear();  
        }
        public async Task StartProcessAsync(string Message, bool isSuccess)
        {
            ProcessStackPanel.Children.Clear(); // Clear previous results
 
        }
        public async Task RunProcessStep(string stepName, bool isSuccess)
        {
           
            StackPanel stepPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };

            TextBlock tick = new TextBlock { Text = "✔", FontSize = 24, Foreground = Brushes.Green, Margin = new Thickness(5, 0, 10, 0) };
            TextBlock cross = new TextBlock { Text = "✖", FontSize = 24, Foreground = Brushes.Red, Margin = new Thickness(5, 0, 10, 0) };
           
            TextBlock stepText = new TextBlock { Text = stepName, FontSize = 14, Margin = new Thickness(5, 0, 10, 0) };
             
            if (isSuccess)
                stepPanel.Children.Add(tick);
            else
                stepPanel.Children.Add(cross);
            stepPanel.Children.Add(stepText);

            ProcessStackPanel.Children.Add(stepPanel);
             
             await Task.Delay(1000);  
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
