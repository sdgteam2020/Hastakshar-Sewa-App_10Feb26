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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WinniesMessageBox
{
    /// <summary>
    /// Interaction logic for PopupWin.xaml
    /// </summary>
    public partial class PopupWin : Window
    {
        public PopupWin()
        {
            InitializeComponent();

            ProcessStackPanel.Children.Clear(); // Clear previous results
                                                //  _ = RunProcessStep(Message, isSuccess); // Run async method without awaiting in constructor
                                                // StartProcessAsync();
        }
        public async Task StartProcessAsync(string Message, bool isSuccess)
        {
            ProcessStackPanel.Children.Clear(); // Clear previous results

            // Process Steps
            // await RunProcessStep("Step 1: Initializing...",true);
            // await RunProcessStep("Step 2: Processing Data...", true);
            //await RunProcessStep("Step 3: Finalizing...",false);
            // await RunProcessStep("Step 3: Finalizing...", false);
        }
        public async Task RunProcessStep(string stepName, bool isSuccess)
        {
            await Task.Delay(1000); // Simulate delay
            // Create a StackPanel for step
            StackPanel stepPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };

            TextBlock tick = new TextBlock { Text = "✔", FontSize = 24, Foreground = Brushes.Green, Margin = new Thickness(5, 0, 10, 0) };
            TextBlock cross = new TextBlock { Text = "✖", FontSize = 24, Foreground = Brushes.Red, Margin = new Thickness(5, 0, 10, 0) };
            // Step Text
            TextBlock stepText = new TextBlock { Text = stepName, FontSize = 14, Margin = new Thickness(5, 0, 10, 0) };

            // Placeholder Image (Will be changed after process)


            // Add to UI
            if (isSuccess)
                stepPanel.Children.Add(tick);
            else
                stepPanel.Children.Add(cross);
            stepPanel.Children.Add(stepText);

            ProcessStackPanel.Children.Add(stepPanel);

            // Simulate Process
            // await Task.Delay(1000); // Simulate delay

            // Set Status (✔ or ✖ randomly for demo)

        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
