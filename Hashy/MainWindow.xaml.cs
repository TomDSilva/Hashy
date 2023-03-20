using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace Hashy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string hashMode;
        public MainWindow()
        {
            InitializeComponent();

            consoleRichTextBox.Document.Blocks.Clear();

            // Set the default hash mode to 0 (being MD5):
            hashModeComboBox.SelectedIndex = 0;

            // TEMP testing parameters not meant for production:
            dirTextBox.Text = "E:\\deleteme\\source";
            outputTextBox.Text = "E:\\deleteme\\output.csv";
            reportTextBox.Text = "E:\\deleteme\\output.csv";
        }

        
        // Force form size and prevent any adjustment:
        private void Form1_Load(object sender, EventArgs e)
        {
            this.MinHeight = 400;
            this.MinWidth = 600;
            this.MaxHeight = 400;
            this.MaxWidth = 600;
        }

        // TEST debug button not meant for production version:
        private void debugButton_Click(object sender, EventArgs e)
        {
            //var about = new About();
            //about.Show();
            Console.ReadLine();
        }

        private void scanButton_Click(object sender, EventArgs e)
        {
            // Grab the selected hash mode into a string:
            hashMode = hashModeComboBox.SelectedItem.ToString();

            // Create and start the scan thread:
            Thread scanThread = new Thread(InitialScan);
            scanThread.Start();
        }

        private void checkButton_Click(object sender, EventArgs e)
        {
            // Create and start the check thread:
            Thread checkThread = new Thread(Check);
            checkThread.Start();
        }

        private void sourceButton_Click(object sender, EventArgs e)
        {
            //OpenFileDialog openFileDialog = new OpenFileDialog();
            //if (openFileDialog.ShowDialog() == true)
            //{
            //    dirTextBox.Text = File.ReadAllText(openFileDialog.FileName);
            //}

            VistaFolderBrowserDialog folderBrowserDialog = new VistaFolderBrowserDialog();
            folderBrowserDialog.Description = "Select folder to scan...";
            folderBrowserDialog.UseDescriptionForTitle = true;
            folderBrowserDialog.ShowDialog();
            dirTextBox.Text = folderBrowserDialog.SelectedPath;
        }

        private void outputButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = false;
            openFileDialog.DefaultExt = "csv";
            openFileDialog.Title = "Create new report...";
            if (openFileDialog.ShowDialog() == true)
            {
                outputTextBox.Text = openFileDialog.FileName;
            }
        }

        private void existingReportButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = "csv";
            openFileDialog.Title = "Select existing report...";
            if (openFileDialog.ShowDialog() == true)
            {
                outputTextBox.Text = openFileDialog.FileName;
            }
        }

        private void clearTerminalButton_Click(object sender, EventArgs e)
        {
            consoleRichTextBox.Document.Blocks.Clear();
        }

        private void consoleRichTextBox_TextChanged(object sender, EventArgs e)
        {
            //// set the current caret position to the end
            //consoleRichTextBox.SelectionStart = consoleRichTextBox.Text.Length;

            //TextPointer text = consoleRichTextBox.Document.ContentStart;
            //while (text.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.Text)
            //{
            //    text = text.GetNextContextPosition(LogicalDirection.Forward);
            //}
            //TextPointer startPos = text.GetPositionAtOffset(0);
            //TextPointer endPos = text.GetPositionAtOffset(10);
            //consoleRichTextBox.Selection.Select(startPos, endPos);

            //// scroll it automatically
            //consoleRichTextBox.ScrollToCaret();

            consoleRichTextBox.ScrollToEnd();
        }

        private void InitialScan()
        {
            // Disable UI:
            DisableUI();

            string scanRoot = GetTBText(dirTextBox);
            string reportDest = GetTBText(outputTextBox);

            AppendLine(consoleRichTextBox, "------------------------------------------------------------");

            if (File.Exists(reportDest))
            {
                MessageBoxResult result = MessageBox.Show("Output file already exists, do you want to overwrite?", "ALERT", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    AppendLine(consoleRichTextBox, "!!!SCAN ABORTED!!!", Brushes.Red);

                    // Enable UI:
                    EnableUI();

                    return;
                }
            }

            // Variable to hold when we started the scan:
            DateTime started = DateTime.Now;

            AppendLine(consoleRichTextBox, $"Scan started at {started}");
            AppendLine(consoleRichTextBox, "Report will be saved here:");
            AppendLine(consoleRichTextBox, reportDest);

            // Create a list called files that will hold the output from the recursive file processor:
            List<string> files = RecursiveFileProcessor.RecursiveFileSearch(GetTBText(dirTextBox));

            // Set totalProgressBar back to 0 (in case previously run):
            SetProgressBar(totalProgressBar, 0);

            SetPercentageLabel(totalPercentageLabel, "0%");

            // Set totalProgressBar max value to the total of the files we are going to loop through:
            SetProgressBarMax(totalProgressBar, files.Count);

            StreamWriter outputFile = OpenFile(outputTextBox.Text);

            WriteLine(outputFile, @"f7c07c6ab1c9faaccee57b77b826ff51\r\ncf3b9982a86afb4dd9b50556f1817aa9\r\ne1d4aa4e629ee2d4e553c538bfcad177\r\ne724f3d18bf2985fea81a912c642ffba\r\nc37dd1fc6a3ddd4fdd77fe3ceedf8974\r\n96d0ee391f51eb3e2a00064ed3a25eac\r\n11ae1e159900bfd8fe6731825ffe43e9\r\nbaa9d174ce36a1ac00a5fdbde0b55898\r\n0957faec8615cc14a0c3b229c92d38ab\r\nfa388c8c3a97671b1557a4ae53bc468b");

            string outputHeader = $"{started.ToString()},{dirTextBox.Text},{hashMode}";
            WriteLine(outputFile, outputHeader);

            int i = 1;

            // Loop through all the files:
            foreach (var file in files)
            {
                string value;

                AppendLine(consoleRichTextBox, $"Checking {file}");

                if (hashMode == "MD5")
                {
                    // Fill value with the output from the MD5 file calculation:
                    value = CalculateMD5(file);
                }
                else if (hashMode == "SHA256")
                {
                    value = CalculateSHA256(file);
                }
                else
                {
                    value = "!!!ERROR!!!";
                }

                // Add the value output to the fileDetails list along with the current file:
                string line = $"{file},{value},{GetFileLastModified(file)}";
                WriteLine(outputFile, line);

                SetProgressBar(totalProgressBar, i);
                string d = 100 / files.Count * i + "%";
                SetPercentageLabel(totalPercentageLabel, d);
                if (i == files.Count)
                {
                    SetPercentageLabel(totalPercentageLabel, "100%");
                }
                i = i + 1;
            }

            // Clear the fileDetails list:
            CloseFile(outputFile);

            // Clear the files list:
            files.Clear();

            DateTime finished = DateTime.Now;

            AppendLine(consoleRichTextBox, $"Scan finished at {finished}");

            var scanDuration = finished - started;
            // TODO: Rework logic so that if it takes longer then X time its minutes (instead of seconds), more then Y its hours?
            // Update the console with how long the scan took. Formatting to remove decimal places.

            AppendLine(consoleRichTextBox, $"Scan took {scanDuration.TotalSeconds.ToString("N0")} seconds");

            hashMode = "";

            outputFile.Close();

            // Enable UI:
            EnableUI();
        }

        private string GetTextBoxText(System.Windows.Controls.TextBox tb)
        {
            string text = "";
            tb.Dispatcher.Invoke(delegate
            {
                text = tb.Text;
            });
            return text;
        }

        private void Check()
        {
            // Disable UI:
            DisableUI();

            string reportLocation = GetTextBoxText(reportTextBox);

            // Create a list of files from the existing report via a method:
            List<FileDetails> report = GetFileContents(reportLocation);

            AppendLine(consoleRichTextBox, "------------------------------------------------------------");
            AppendLine(consoleRichTextBox, "Checking files against existing report:");
            AppendLine(consoleRichTextBox, reportLocation);

            if (UIDCheck(reportLocation)){
                AppendLine(consoleRichTextBox, "Report file passed UID check", Brushes.Green);

                // Set totalProgressBar back to 0 (in case previously run):
                SetProgressBar(totalProgressBar, 0);

                SetPercentageLabel(totalPercentageLabel, "0%");

                // Set totalProgressBar max value to the total of the files we are going to loop through:
                SetProgressBarMax(totalProgressBar, report.Count);

                int i = 1;

                bool corruptFile = false;

                string reportHashMode = HashMode(reportLocation);

                AppendLine(consoleRichTextBox, $"Report hash mode is {reportHashMode}");

                foreach (FileDetails file in report)
                {
                    string hash = "";

                    AppendLine(consoleRichTextBox, $"Checking {file.FilePath}...");

                    if (reportHashMode == "MD5")
                    {
                        hash = CalculateMD5(file.FilePath);
                    }
                    else if (reportHashMode == "SHA256")
                    {
                        hash = CalculateSHA256(file.FilePath);
                    }
                    
                    DateTime lastModified = GetFileLastModified(file.FilePath);

                    if (file.Hash == hash & DateTimeSecondsEquals(file.LastModified, lastModified))
                    {
                        AppendLine(consoleRichTextBox, "Good", Brushes.Green);
                    }
                    else if (file.Hash != hash & DateTimeSecondsEquals(file.LastModified, lastModified))
                    {
                        corruptFile = true;
                        AppendLine(consoleRichTextBox, "!!!BAD!!!", Brushes.Red);
                    }
                    else if (file.Hash != hash & !(DateTimeSecondsEquals(file.LastModified, lastModified)))
                    {
                        AppendLine(consoleRichTextBox, "!!!FILE HAS BEEN USER MODIFIED!!!", Brushes.Orange);
                    }
                    else
                    {
                        AppendLine(consoleRichTextBox, "!!!ERROR!!!", Brushes.Red);
                    }
                    SetProgressBar(totalProgressBar, i);
                    string d = 100 / report.Count * i + "%";
                    SetPercentageLabel(totalPercentageLabel, d);
                    if (i == report.Count)
                    {
                        SetPercentageLabel(totalPercentageLabel, "100%");

                        if (corruptFile)
                        {
                            AppendLine(consoleRichTextBox, "THERE WAS CORRUPTION DETECTED", Brushes.Red);
                        }
                        else
                        {
                            AppendLine(consoleRichTextBox, "---No corruption detected---", Brushes.Green);
                            AppendLine(consoleRichTextBox, "---------------------------------Check Finished---------------------------------");
                        }
                    }
                    i = i + 1;
                }
            } else
            {
                AppendLine(consoleRichTextBox, "!!!ERROR!!! Report file failed UID check", Brushes.Red);
            }

            // Enable UI:
            EnableUI();
        }

        private bool UIDCheck(string file)
        {
            if (File.Exists(file))
            {
                StreamReader reader = new StreamReader(File.OpenRead(file));
                List<FileDetails> outputList = new List<FileDetails>();
                string uid = reader.ReadLine();
                if (uid != @"f7c07c6ab1c9faaccee57b77b826ff51\r\ncf3b9982a86afb4dd9b50556f1817aa9\r\ne1d4aa4e629ee2d4e553c538bfcad177\r\ne724f3d18bf2985fea81a912c642ffba\r\nc37dd1fc6a3ddd4fdd77fe3ceedf8974\r\n96d0ee391f51eb3e2a00064ed3a25eac\r\n11ae1e159900bfd8fe6731825ffe43e9\r\nbaa9d174ce36a1ac00a5fdbde0b55898\r\n0957faec8615cc14a0c3b229c92d38ab\r\nfa388c8c3a97671b1557a4ae53bc468b")
                {
                    return false;
                }
                reader.Close();
                return true;
            }
            return false;
        }

        private string HashMode(string file)
        {
            if (File.Exists(file))
            {
                StreamReader reader = new StreamReader(File.OpenRead(file));
                List<FileDetails> outputList = new List<FileDetails>();
                string uid = reader.ReadLine();
                string header = reader.ReadLine();
                var values = header.Split(',');
                reader.Close();
                return values[values.Length-1];
            }
            throw new ArgumentException();
        }

        private List<FileDetails> GetFileContents(string file)
        {
            if (File.Exists(file))
            {
                StreamReader reader = new StreamReader(File.OpenRead(file));
                List<FileDetails> outputList = new List<FileDetails>();
                string uid = reader.ReadLine();
                string headerline = reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    outputList.Add(line);
                }
                reader.Close();
                return outputList;
            }
            return null;
        }

        private StreamWriter OpenFile(string filePath)
        {
            // TODO: System.IO.IOException: 'The process cannot access the file 'E:\deleteme\output.csv' because it is being used by another process.'
            StreamWriter writer = new StreamWriter(filePath);
            return writer;
        }

        private void WriteLine(StreamWriter writer, string line)
        {
            writer.WriteLine(line);
        }

        private void CloseFile(StreamWriter writer)
        {
            writer.Close();
        }

        static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        static string CalculateSHA256(string filename)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        static DateTime GetFileLastModified(string filename)
        {
            return File.GetLastWriteTime(filename);
        }

        private void DisableUI()
        {
            // Disable all buttons:
            DisableButton(checkButton);
            DisableButton(scanButton);
            DisableButton(sourceButton);
            DisableButton(outputButton);

            // Disable all textbox's:
            DisableTextBox(dirTextBox);
            DisableTextBox(reportTextBox);
            DisableTextBox(outputTextBox);

            // Disable all combobox's:
            DisableComboBox(hashModeComboBox);
        }

        private void EnableUI()
        {
            // Enable all buttons:
            EnableButton(checkButton);
            EnableButton(scanButton);
            EnableButton(sourceButton);
            EnableButton(outputButton);

            // Enable all textbox's:
            EnableTextBox(dirTextBox);
            EnableTextBox(reportTextBox);
            EnableTextBox(outputTextBox);

            // Enable all combobox's:
            EnableComboBox(hashModeComboBox);
        }

        private void DisableButton(System.Windows.Controls.Button btn)
        {
            SetButtonStatus(btn, false);
        }

        private void EnableButton(System.Windows.Controls.Button btn)
        {
            SetButtonStatus(btn, true);
        }

        private void SetButtonStatus(System.Windows.Controls.Button btn, bool status)
        {
            if (btn.Dispatcher.CheckAccess())
            {
                btn.IsEnabled = status;
            }
            else
            {
                btn.Dispatcher.Invoke(delegate
                    {
                    btn.IsEnabled = status;
                });
            }
        }

        private void DisableTextBox(System.Windows.Controls.TextBox tb)
        {
            SetTextBoxStatus(tb, false);
        }

        private void EnableTextBox(System.Windows.Controls.TextBox tb)
        {
            SetTextBoxStatus(tb, true);
        }

        private void SetTextBoxStatus(System.Windows.Controls.TextBox tb, bool status)
        {
            if (tb.Dispatcher.CheckAccess())
            {
                tb.IsEnabled = status;
            }
            else
            {
                tb.Dispatcher.Invoke(delegate
                {
                    tb.IsEnabled = status;
                });
            }
        }

        private void DisableComboBox(System.Windows.Controls.ComboBox cb)
        {
            SetComboBoxStatus(cb, false);
        }

        private void EnableComboBox(System.Windows.Controls.ComboBox cb)
        {
            SetComboBoxStatus(cb, true);
        }

        private void SetComboBoxStatus(System.Windows.Controls.ComboBox cb, bool status)
        {
            if (cb.Dispatcher.CheckAccess())
            {
                cb.IsEnabled = status;
            }
            else
            {
                cb.Dispatcher.Invoke(delegate
                {
                    cb.IsEnabled = status;
                });
            }
        }

        private void AppendLine(System.Windows.Controls.RichTextBox rtb, string text)
        {
            if (rtb.Dispatcher.CheckAccess())
            {
               // rtb.AppendText(text);

                TextRange rangeOfText = new TextRange(rtb.Document.ContentEnd, rtb.Document.ContentEnd);
                rangeOfText.Text = text;
                rangeOfText.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White);
                rtb.AppendText(Environment.NewLine);
                rtb.ScrollToEnd();
            }
            else
            {
                rtb.Dispatcher.Invoke(delegate
                {
                    AppendLine(rtb, text);
                });
            }
        }

        private void AppendLine(System.Windows.Controls.RichTextBox rtb, string text, Brush color)
        {
            if (rtb.Dispatcher.CheckAccess())
            {
                TextRange rangeOfText = new TextRange(rtb.Document.ContentEnd, rtb.Document.ContentEnd);
                rangeOfText.Text = text;
                rangeOfText.ApplyPropertyValue(TextElement.ForegroundProperty, color);

                rtb.AppendText(Environment.NewLine);
                rtb.ScrollToEnd();
            }
            else
            {
                rtb.Dispatcher.Invoke(delegate
                {
                    AppendLine(rtb, text, color);
                });
            }
        }

        private void SetProgressBar(System.Windows.Controls.ProgressBar pb, int value)
        {
            if (pb.Dispatcher.CheckAccess())
            {
                pb.Value = value;
            }
            else
            {
                pb.Dispatcher.Invoke(delegate
                {
                    pb.Value = value;
                });
            }
        }

        private void SetProgressBarMax(System.Windows.Controls.ProgressBar pb, int value)
        {
            if (pb.Dispatcher.CheckAccess())
            {
                pb.Maximum = value;
            }
            else
            {
                pb.Dispatcher.Invoke(delegate
                {
                    pb.Maximum = value;
                });
            }
        }

        private void SetPercentageLabel(System.Windows.Controls.Label label, string value)
        {
            if (label.Dispatcher.CheckAccess())
            {
                label.Content = value;
            }
            else
            {
                label.Dispatcher.Invoke(delegate
                {
                    label.Content = value;
                });
            }
        }

        private string ReadTBText(System.Windows.Controls.TextBox tb)
        {
            if (tb.Dispatcher.CheckAccess())
            {
                return tb.Text;
            }
            else
            {
                tb.Dispatcher.Invoke(delegate
                {
                    return tb.Text;
                });
            }
        }

        public static bool DateTimeSecondsEquals(DateTime dt1, DateTime dt2)
        {
            return dt1.Year == dt2.Year && dt1.Month == dt2.Month && dt1.Day == dt2.Day &&
                dt1.Hour == dt2.Hour && dt1.Minute == dt2.Minute && dt1.Second == dt2.Second;
        }

        private void MinButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaxButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState= WindowState.Normal;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (this.WindowState == WindowState.Normal)
                {
                    this.WindowState = WindowState.Maximized;
                }
                else if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                }
            }
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void scanButton_Click_1(object sender, RoutedEventArgs e)
        {

        }
    }
}