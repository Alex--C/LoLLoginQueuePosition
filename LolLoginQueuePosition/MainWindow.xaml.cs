using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Text.RegularExpressions;

namespace LolLoginQueuePosition
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private StreamReader logFileReader;
        private DispatcherTimer dispatcherTimer;
        private int lastPosition;
        private int lastPositionReadAt;

        public MainWindow()
        {
            InitializeComponent();
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0);
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Debug.WriteLine("Tick");
            dispatcherTimer.Stop();
            string line;
            int newInterval = 10;
            int newIntervalReadAt = 0;
            int newPosition = 0;
            int newPositionReadAt = 0;
            string currentPattern = @"^(\d+).*Login queue position is (\d+)$";
            string nextPattern = @"^(\d+).*Next login queue ticker update in (\d+) seconds\.$";
            string timestampPattern = @"^(\d+)\.";
            Regex rCurrent = new Regex(currentPattern, RegexOptions.IgnoreCase);
            Regex rNext = new Regex(nextPattern, RegexOptions.IgnoreCase);
            Regex rTimestamp = new Regex(timestampPattern, RegexOptions.IgnoreCase);

            string lastLine = "";
            while ((line = logFileReader.ReadLine()) != null)
            {
                lastLine = line;
                Match currentMatch = rCurrent.Match(line);
                if (currentMatch.Success)
                {
                    Debug.WriteLine("{0} @ {1}", currentMatch.Groups[2].Value, currentMatch.Groups[1].Value);
                    currentPosLabel.Content = currentMatch.Groups[2].Value;
                    if (newPosition != 0)
                    {
                        lastPosition = newPosition;
                    }
                    newPosition = Int32.Parse(currentMatch.Groups[2].Value);
                    if (newPositionReadAt != 0)
                    {
                        lastPositionReadAt = newPositionReadAt;
                    }
                    newPositionReadAt = Int32.Parse(currentMatch.Groups[1].Value);
                }

                Match nextMatch = rNext.Match(line);
                if (nextMatch.Success)
                {
                    newInterval = Int32.Parse(nextMatch.Groups[2].Value);
                    newIntervalReadAt = Int32.Parse(nextMatch.Groups[1].Value);
                }
            }

            Match lastTimestampMatch = rTimestamp.Match(lastLine);
            if (lastTimestampMatch.Success)
            {
                int lastTimestamp = Int32.Parse(lastTimestampMatch.Groups[1].Value);
                dispatcherTimer.Interval = new TimeSpan(0, 0, newInterval + 10 - (lastTimestamp - newIntervalReadAt));
                Debug.WriteLine("Next tick in {0}", newInterval + 10 - (lastTimestamp - newIntervalReadAt));

                int positionsGained = lastPosition - newPosition;
                int timePassed = newPositionReadAt - lastPositionReadAt;
                float factor = newPosition / positionsGained;
                Debug.WriteLine("Gained {0} positions in {1} seconds.", positionsGained, timePassed);
                float totalTime = factor * timePassed;
                estimationLabel.Content = string.Format("{0} seconds ({1} minutes)", totalTime, totalTime / 60);
                lastPositionReadAt = newPositionReadAt;
                lastPosition = newPosition;
            }

            dispatcherTimer.Start();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            string[] drives = { "C", "G", "A", "B", "D", "E", "F", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
            string path = ":\\Riot Games\\PBE";
            string filePath = "\\Logs\\LeagueClient Logs\\";
            foreach (string drive in drives) {
                if (Directory.Exists(drive + path))
                {
                    folderBrowserDialog.SelectedPath = drive + path;
                    break;
                }
            }

            System.Windows.Forms.DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string selectedPath = folderBrowserDialog.SelectedPath;
                Debug.WriteLine(selectedPath + filePath);
                if (Directory.Exists(selectedPath + filePath))
                {
                    string mostRecentFileName = "";
                    foreach (string fileName in Directory.EnumerateFiles(selectedPath + filePath, "*_LeagueClient.log"))
                    {
                        Debug.WriteLine(fileName);
                        if (string.Compare(fileName, mostRecentFileName) == 1)
                        {
                            mostRecentFileName = fileName;
                        }
                    }
                    Debug.WriteLine(mostRecentFileName);
                    if (mostRecentFileName != "")
                    {
                        FileStream fileStream = new FileStream(mostRecentFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        logFileReader = new StreamReader(fileStream);
                        folderChooser.IsEnabled = false;
                        dispatcherTimer.Start();
                        folderChooser.Content = "Reading Log...";
                    }
                }
            }
        }
    }
}
