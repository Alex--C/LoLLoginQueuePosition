﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;

namespace LolLoginQueuePosition
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private StreamReader logFileReader;
        private DispatcherTimer dispatcherTimer;
        private int lastPosition;
        private int lastPositionReadAt;
        private SlidingWindow<int> slidingWindow;

        public Color SlidingWindowSaturationIndicator => slidingWindow?.IsSaturated ?? false ? Colors.Green : Colors.Red;

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0);
            slidingWindow = new SlidingWindow<int>(15);
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
                    newPosition = Int32.Parse(currentMatch.Groups[2].Value);
                    Debug.WriteLine("{0} @ {1}", newPosition, currentMatch.Groups[1].Value);
                    currentPosLabel.Content = newPosition;

                    if (newPosition != 0)
                    {
                        if (lastPosition != 0)
                        {
                            var positionDelta = lastPosition - newPosition;
                            slidingWindow.Add(positionDelta);
                            RaisePropertyChanged(nameof(SlidingWindowSaturationIndicator));
                        }
                        lastPosition = newPosition;
                    }
                    
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
                int nextInt = newInterval + 10 - (lastTimestamp - newIntervalReadAt);
                if (nextInt < 1)
                {
                    nextInt = 1;
                }
                dispatcherTimer.Interval = new TimeSpan(0, 0, nextInt);
                Debug.WriteLine("Next tick in {0}", newInterval + 10 - (lastTimestamp - newIntervalReadAt));


                var binomialAverage = Binomial.CalculateBinomialAverage(slidingWindow);

                int timePassed = newPositionReadAt - lastPositionReadAt;
                if (binomialAverage != 0) { 
                    var totalTime = TimeSpan.FromSeconds(newPosition / (binomialAverage / timePassed));
                    estimationLabel.Content = totalTime.ToString(@"hh\h\ mm\m\i\n\ ss\s\e\c");
                }
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

            foreach (string drive in drives)
            {
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

                
                if (selectedPath.EndsWith("LeagueClient Logs"))
                {
                    filePath = "\\";
                }
                else if (selectedPath.EndsWith("Logs"))
                {
                    filePath = "\\LeagueClient Logs\\";
                }

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
                        fileStream.Seek(-10240, SeekOrigin.End);

                        logFileReader = new StreamReader(fileStream);
                        folderChooser.IsEnabled = false;
                        dispatcherTimer.Start();
                        folderChooser.Content = "Reading Log...";
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
