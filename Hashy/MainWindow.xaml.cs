using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using File = System.IO.File;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using System.Windows.Threading;
using System.Reflection;
using System.Windows.Controls.Primitives;
using Button = System.Windows.Controls.Button;

//Links that helped me build this:
//https://stackoverflow.com/questions/13435699/why-wont-the-wpf-progressbar-stretch-to-fit
//https://yqnn.github.io/svg-path-editor/
//https://stackoverflow.com/questions/48540883/change-the-button-path-data-based-on-condition-in-wpf

namespace Hashy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static bool isMaximized = false;
        private static Rect originalWindowRect;

        private ObservableCollection<ConsoleMessage> consoleMessages;

        private string? hashMode;

        private DispatcherTimer timer = new DispatcherTimer();
        private TimeSpan elapsedTime;
        private bool isTimerRunning;

        public MainWindow()
        {
            InitializeComponent();

            InitializeTimer();

            // TEMP testing parameters not meant for production:
            dirTextBox.Text = "C:\\Hashy Test Folder\\Source";
            outputTextBox.Text = "C:\\Hashy Test Folder\\output.csv";
            reportTextBox.Text = "C:\\Hashy Test Folder\\output.csv";

            // Set the default hash mode to 0 (being MD5):
            hashModeComboBox.SelectedIndex = 0;

            SetScanButtonStatus();
            SetCheckButtonStatus();
            SetClearButtonStatus();

            consoleMessages = new ObservableCollection<ConsoleMessage>();
            consoleListBox.ItemsSource = consoleMessages;
        }

        public class ConsoleMessage
        {
            public string Message { get; set; }
            public Brush Color { get; set; }

            public ConsoleMessage(string message, Brush color)
            {
                Message = message;
                Color = color;
            }
        }

        // TEST debug button not meant for production version:
        private void debugButton_Click(object sender, EventArgs e)
        {
            //var about = new About();
            //about.Show();
            hashModeComboBox.IsEnabled = false;
            Console.ReadLine();
        }

        private void scanButton_Click(object sender, EventArgs e)
        {
            // Grab the selected hash mode into a string:
            hashMode = ((ComboBoxItem)hashModeComboBox.SelectedItem).Content.ToString()!;

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
            VistaFolderBrowserDialog folderBrowserDialog = new VistaFolderBrowserDialog();
            folderBrowserDialog.Description = "Select folder to scan...";
            folderBrowserDialog.UseDescriptionForTitle = true;
            folderBrowserDialog.ShowDialog();
            dirTextBox.Text = folderBrowserDialog.SelectedPath;

            SetScanButtonStatus();
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

            SetScanButtonStatus();
        }

        private void existingReportButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = "csv";
            openFileDialog.Title = "Select existing report...";
            if (openFileDialog.ShowDialog() == true)
            {
                reportTextBox.Text = openFileDialog.FileName;
            }

            SetCheckButtonStatus();
        }

        private void clearTerminalButton_Click(object sender, EventArgs e)
        {
            consoleMessages.Clear();
            SetClearButtonStatus();
        }

        private void InitialScan()
        {
            // Disable UI:
            DisableUI();

            string scanRoot = GetTextBoxText(dirTextBox);
            string reportDest = GetTextBoxText(outputTextBox);

            AppendLine(consoleListBox, "---------------------------------Scan Started---------------------------------");

            if (File.Exists(reportDest))
            {
                MessageBoxResult result = MessageBox.Show("Output file already exists, do you want to overwrite?", "ALERT", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    AppendLine(consoleListBox, "!!!SCAN ABORTED!!!", Brushes.Red);

                    // Enable UI:
                    EnableUI();

                    return;
                }
            }

            StartTimer();

            // Variable to hold when we started the scan:
            DateTime started = DateTime.Now;

            AppendLine(consoleListBox, $"Scan started at {started}");
            AppendLine(consoleListBox, "Report will be saved here:");
            AppendLine(consoleListBox, reportDest);

            AppendLine(consoleListBox, "Building list of files to scan, this can take a while...");

            // Create a list called files that will hold the output from the recursive file processor:
            List<string> files = RecursiveFileProcessor.RecursiveFileSearch(GetTextBoxText(dirTextBox));

            int total = files.Count;
            float totalFloat = files.Count;

            // Set totalProgressBar back to 0 (in case previously run):
            SetProgressBar(totalProgressBar, 0);

            UnhideLabel(totalPercentageLabel);

            SetLabelContent(totalPercentageLabel, "0%");

            // Set totalProgressBar max value to the total of the files we are going to loop through:
            SetProgressBarMax(totalProgressBar, total);

            StreamWriter outputFile = OpenFile(GetTextBoxText(outputTextBox));

            WriteLine(outputFile, @"f7c07c6ab1c9faaccee57b77b826ff51\r\ncf3b9982a86afb4dd9b50556f1817aa9\r\ne1d4aa4e629ee2d4e553c538bfcad177\r\ne724f3d18bf2985fea81a912c642ffba\r\nc37dd1fc6a3ddd4fdd77fe3ceedf8974\r\n96d0ee391f51eb3e2a00064ed3a25eac\r\n11ae1e159900bfd8fe6731825ffe43e9\r\nbaa9d174ce36a1ac00a5fdbde0b55898\r\n0957faec8615cc14a0c3b229c92d38ab\r\nfa388c8c3a97671b1557a4ae53bc468b");

            string outputHeader = $"{started.ToString()},{GetTextBoxText(dirTextBox)},{hashMode}";
            WriteLine(outputFile, outputHeader);

            int i = 1;

            int error = 0;

            // Loop through all the files:
            foreach (var file in files)
            {
                string value;

                AppendLine(consoleListBox, $"{i} of {total} - Checking {file}");

                if (hashMode == "MD5")
                {
                    // Fill value with the output from the MD5 file calculation:
                    try
                    {
                        value = CalculateMD5(file);
                    }
                    catch (FileNotFoundException)
                    {
                        error = error + 1;

                        MessageBox.Show($"ERROR - File not found:\n{file}", "ERROR - File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                        AppendLine(consoleListBox, "ERROR - File not found", Brushes.Red);

                        SetProgressBar(totalProgressBar, i);
                        string f = 100 / total * i + "%";
                        SetLabelContent(totalPercentageLabel, f);
                        if (i == total)
                        {
                            SetLabelContent(totalPercentageLabel, "100%");
                        }
                        i = i + 1;

                        continue;
                    }
                    catch (Exception e)
                    {
                        error = error + 1;

                        MessageBox.Show($"ERROR - An unknown error occured with file:\n{file}\nThe error message was:\n{e}", "ERROR - An Unknown Error Occured", MessageBoxButton.OK, MessageBoxImage.Error);
                        AppendLine(consoleListBox, "ERROR - An unknown error occured", Brushes.Red);
                        AppendLine(consoleListBox, "The error message was:", Brushes.Red);
                        AppendLine(consoleListBox, $"{e}", Brushes.Red);

                        SetProgressBar(totalProgressBar, i);
                        string f = 100 / total * i + "%";
                        SetLabelContent(totalPercentageLabel, f);
                        if (i == total)
                        {
                            SetLabelContent(totalPercentageLabel, "100%");
                        }
                        i = i + 1;

                        continue;
                    }
                }
                else if (hashMode == "SHA256")
                {
                    try
                    {
                        // Fill value with the output from the SHA256 file calculation:
                        value = CalculateSHA256(file);
                    }
                    catch (FileNotFoundException)
                    {
                        error = error + 1;

                        MessageBox.Show($"ERROR - File not found:\n{file}", "ERROR - File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);

                        SetProgressBar(totalProgressBar, i);
                        string f = 100 / total * i + "%";
                        SetLabelContent(totalPercentageLabel, f);
                        if (i == total)
                        {
                            SetLabelContent(totalPercentageLabel, "100%");
                        }
                        i = i + 1;

                        continue;
                    }
                    catch (Exception e)
                    {
                        error = error + 1;

                        MessageBox.Show($"ERROR - An unknown error occured with file:\n{file}\nThe error message was:\n{e}", "ERROR - An Unknown Error Occured", MessageBoxButton.OK, MessageBoxImage.Error);

                        SetProgressBar(totalProgressBar, i);
                        string f = 100 / total * i + "%";
                        SetLabelContent(totalPercentageLabel, f);
                        if (i == total)
                        {
                            SetLabelContent(totalPercentageLabel, "100%");
                        }
                        i = i + 1;

                        continue;
                    }
                }
                else
                {
                    error = error + 1;

                    continue;
                }

                //Add the value output to the fileDetails list along with the current file:
                string line = $"{file},{value},{GetFileLastModified(file)}";
                WriteLine(outputFile, line);

                SetProgressBar(totalProgressBar, i);
                string d = Math.Round(100 / totalFloat * i, 0) + "%";
                SetLabelContent(totalPercentageLabel, d);
                if (i == total)
                {
                    SetLabelContent(totalPercentageLabel, "100%");
                }
                i = i + 1;
            }

            // Clear the fileDetails list:
            CloseFile(outputFile);

            // Clear the files list:
            files.Clear();

            if (error > 0)
            {
                AppendLine(consoleListBox, $"Warning! There were {error} errors", Brushes.Red);
            }

            DateTime finished = DateTime.Now;

            AppendLine(consoleListBox, $"Scan finished at {finished}");

            string scanDurationSeconds = Math.Floor(elapsedTime.TotalSeconds).ToString();

            StopTimer();

            // TODO : Rework logic so that if it takes longer then X time its minutes (instead of seconds), more then Y its hours?
            AppendLine(consoleListBox, $"Scan took {scanDurationSeconds} seconds");

            AppendLine(consoleListBox, "---------------------------------Scan Finished---------------------------------");

            hashMode = "";

            outputFile.Close();

            // Enable UI:
            EnableUI();
        }

        private void Check()
        {
            // Disable UI:
            DisableUI();

            // Variable to hold when we started the scan:
            DateTime started = DateTime.Now;

            StartTimer();

            string reportLocation = GetTextBoxText(reportTextBox);

            // Create a list of files from the existing report via a method:
            List<FileDetails> report = GetFileContents(reportLocation);

            AppendLine(consoleListBox, "---------------------------------Check Started---------------------------------");
            AppendLine(consoleListBox, "Checking files against existing report:");
            AppendLine(consoleListBox, reportLocation);

            int total = report.Count;
            float totalFloat = report.Count;

            // Set totalProgressBar back to 0 (in case previously run):
            SetProgressBar(totalProgressBar, 0);

            UnhideLabel(totalPercentageLabel);

            if (UIDCheck(reportLocation))
            {
                AppendLine(consoleListBox, "Report file passed UID check", Brushes.Green);

                // Set totalProgressBar back to 0 (in case previously run):
                SetProgressBar(totalProgressBar, 0);

                SetLabelContent(totalPercentageLabel, "0%");

                // Set totalProgressBar max value to the total of the files we are going to loop through:
                SetProgressBarMax(totalProgressBar, report.Count);

                int i = 1;

                bool corruptFile = false;

                string reportHashMode = HashMode(reportLocation);

                AppendLine(consoleListBox, $"Report hash mode is {reportHashMode}");

                int error = 0;

                foreach (FileDetails file in report)
                {
                    if (file.Hash == "!!!ERROR!!!")
                    {
                        SetProgressBar(totalProgressBar, i);
                        string e = 100 / report.Count * i + "%";
                        SetLabelContent(totalPercentageLabel, e);

                        continue;
                    }

                    string hash = "";

                    AppendLine(consoleListBox, $"Checking {file.FilePath}...");

                    if (reportHashMode == "MD5")
                    {
                        try
                        {
                            hash = CalculateMD5(file.FilePath);
                        }
                        catch (FileNotFoundException)
                        {
                            error = error + 1;

                            MessageBox.Show($"ERROR - File not found:\n{file}", "ERROR - File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                            AppendLine(consoleListBox, "ERROR - File not found", Brushes.Red);

                            SetProgressBar(totalProgressBar, i);
                            string f = 100 / report.Count * i + "%";
                            SetLabelContent(totalPercentageLabel, f);
                            if (i == report.Count)
                            {
                                SetLabelContent(totalPercentageLabel, "100%");
                            }

                            i = i + 1;

                            continue;
                        }
                        catch (Exception e)
                        {
                            error = error + 1;

                            MessageBox.Show($"ERROR - An unknown error occured with file:\n{file}\nThe error message was:\n{e}", "ERROR - An Unknown Error Occured", MessageBoxButton.OK, MessageBoxImage.Error);
                            AppendLine(consoleListBox, "ERROR - An unknown error occured", Brushes.Red);
                            AppendLine(consoleListBox, "The error message was:", Brushes.Red);
                            AppendLine(consoleListBox, $"{e}", Brushes.Red);

                            SetProgressBar(totalProgressBar, i);
                            string f = 100 / report.Count * i + "%";
                            SetLabelContent(totalPercentageLabel, f);
                            if (i == report.Count)
                            {
                                SetLabelContent(totalPercentageLabel, "100%");
                            }

                            i = i + 1;

                            continue;
                        }
                    }
                    else if (reportHashMode == "SHA256")
                    {
                        hash = CalculateSHA256(file.FilePath);
                    }

                    DateTime lastModified = GetFileLastModified(file.FilePath);

                    if (file.Hash == hash & DateTimeSecondsEquals(file.LastModified, lastModified))
                    {
                        AppendLine(consoleListBox, "Good", Brushes.Green);
                    }
                    else if (file.Hash != hash & DateTimeSecondsEquals(file.LastModified, lastModified))
                    {
                        corruptFile = true;
                        AppendLine(consoleListBox, "!!!BAD!!!", Brushes.Red);
                    }
                    else if (file.Hash != hash & !(DateTimeSecondsEquals(file.LastModified, lastModified)))
                    {
                        AppendLine(consoleListBox, "!!!FILE HAS BEEN USER MODIFIED!!!", Brushes.Orange);
                    }
                    else
                    {
                        AppendLine(consoleListBox, "!!!ERROR!!!", Brushes.Red);
                    }
                    SetProgressBar(totalProgressBar, i);
                    string d = Math.Round(100 / totalFloat * i, 0) + "%";
                    SetLabelContent(totalPercentageLabel, d);
                    if (i == report.Count)
                    {
                        SetLabelContent(totalPercentageLabel, "100%");

                        if (corruptFile)
                        {
                            AppendLine(consoleListBox, "THERE WAS CORRUPTION DETECTED", Brushes.Red);
                        }
                        else
                        {
                            AppendLine(consoleListBox, "---No corruption detected---", Brushes.Green);
                        }
                    }
                    i = i + 1;
                }
                if (error > 0)
                {
                    AppendLine(consoleListBox, $"Warning! There were {error} errors", Brushes.Red);
                }

                DateTime finished = DateTime.Now;

                AppendLine(consoleListBox, $"Scan finished at {finished}");

                string scanDurationSeconds = Math.Floor(elapsedTime.TotalSeconds).ToString();

                StopTimer();

                // TODO : Rework logic so that if it takes longer then X time its minutes (instead of seconds), more then Y its hours?
                AppendLine(consoleListBox, $"Scan took {scanDurationSeconds} seconds");
                AppendLine(consoleListBox, "---------------------------------Check Finished---------------------------------");
            }
            else
            {
                AppendLine(consoleListBox, "!!!ERROR!!! Report file failed UID check", Brushes.Red);
                AppendLine(consoleListBox, "---------------------------------Check ABORTED---------------------------------");
            }

            // Enable UI:
            EnableUI();
        }

        public static void MaximizeWindow(Window window)
        {
            var screen = Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(window).Handle);
            var workingArea = screen.WorkingArea;

            var presentationSource = PresentationSource.FromVisual(window);
            var screenDpiScaleX = presentationSource?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            var screenDpiScaleY = presentationSource?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
            var windowDpiScaleX = GetDpiScaleX(window);
            var windowDpiScaleY = GetDpiScaleY(window);

            if (!isMaximized)
            {
                originalWindowRect = new Rect(
                    window.Left / (screenDpiScaleX * windowDpiScaleX),
                    window.Top / (screenDpiScaleY * windowDpiScaleY),
                    window.Width / (screenDpiScaleX * windowDpiScaleX),
                    window.Height / (screenDpiScaleY * windowDpiScaleY)
                );

                window.Left = workingArea.Left / screenDpiScaleX;
                window.Top = workingArea.Top / screenDpiScaleY;
                window.Width = workingArea.Width / screenDpiScaleX;
                window.Height = workingArea.Height / screenDpiScaleY;

                isMaximized = true;
            }
            else
            {
                window.Left = originalWindowRect.Left * (screenDpiScaleX * windowDpiScaleX);
                window.Top = originalWindowRect.Top * (screenDpiScaleY * windowDpiScaleY);
                window.Width = originalWindowRect.Width * (screenDpiScaleX * windowDpiScaleX);
                window.Height = originalWindowRect.Height * (screenDpiScaleY * windowDpiScaleY);

                isMaximized = false;
            }
        }

        private static double GetDpiScaleX(Visual visual)
        {
            var source = PresentationSource.FromVisual(visual);
            return source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        }

        private static double GetDpiScaleY(Visual visual)
        {
            var source = PresentationSource.FromVisual(visual);
            return source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
        }

        private void SetScanButtonStatus()
        {
            if (Directory.Exists(GetTextBoxText(dirTextBox)) && Directory.Exists(Path.GetDirectoryName(GetTextBoxText(outputTextBox))))
            {
                EnableButton(scanButton);
            }
            else
            {
                DisableButton(scanButton);
            }
        }

        private void SetCheckButtonStatus()
        {
            if (File.Exists(GetTextBoxText(reportTextBox)))
            {
                EnableButton(checkButton);
            }
            else
            {
                DisableButton(checkButton);
            }
        }

        private void SetClearButtonStatus()
        {
            if (consoleListBox.Items.Count == 0)
            {
                DisableButton(clearTerminalButton);
            }
            else
            {
                EnableButton(clearTerminalButton);
            }
        }

        private bool UIDCheck(string file)
        {
            if (File.Exists(file))
            {
                StreamReader reader = new StreamReader(File.OpenRead(file));
                List<FileDetails> outputList = new List<FileDetails>();
                string uid = reader.ReadLine()!;
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
                string uid = reader.ReadLine()!;
                string header = reader.ReadLine()!;
                var values = header.Split(',');
                reader.Close();
                return values[values.Length - 1];
            }
            throw new ArgumentException();
        }

        private List<FileDetails> GetFileContents(string file)
        {
            if (File.Exists(file))
            {
                StreamReader reader = new StreamReader(File.OpenRead(file));
                List<FileDetails> outputList = new List<FileDetails>();
                string uid = reader.ReadLine()!;
                string headerline = reader.ReadLine()!;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine()!;
                    outputList.Add(line);
                }
                reader.Close();
                return outputList;
            }
            return null!;
        }

        private StreamWriter OpenFile(string filePath)
        {
            // TODO : System.IO.IOException: 'The process cannot access the file 'E:\deleteme\output.csv' because it is being used by another process.'
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
                try
                {
                    using (var stream = File.OpenRead(filename))
                    {
                        var hash = md5.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
                catch
                {
                    throw;
                }

            }
        }

        static string CalculateSHA256(string filename)
        {
            using (var sha256 = SHA256.Create())
            {
                try
                {
                    using (var stream = File.OpenRead(filename))
                    {
                        var hash = sha256.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
                catch
                {
                    MessageBox.Show($"ERROR - Unable to read file:\n{filename}");
                    return "";
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
            DisableButton(existingReportButton);

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
            EnableButton(sourceButton);
            EnableButton(outputButton);
            EnableButton(existingReportButton);
            SetScanButtonStatus();
            SetCheckButtonStatus();
            SetClearButtonStatus();

            // Enable all textbox's:
            EnableTextBox(dirTextBox);
            EnableTextBox(reportTextBox);
            EnableTextBox(outputTextBox);

            // Enable all combobox's:
            EnableComboBox(hashModeComboBox);
        }

        private void HideLabel(System.Windows.Controls.Label lbl)
        {
            SetLabelVisibility(lbl, System.Windows.Visibility.Hidden);
        }

        private void UnhideLabel(System.Windows.Controls.Label lbl)
        {
            SetLabelVisibility(lbl, System.Windows.Visibility.Visible);
        }

        private void SetLabelContent(System.Windows.Controls.Label label, string value)
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

        private void SetLabelVisibility(System.Windows.Controls.Label lbl, System.Windows.Visibility visibility)
        {
            if (lbl.Dispatcher.CheckAccess())
            {
                lbl.Visibility = visibility;
            }
            else
            {
                lbl.Dispatcher.Invoke(delegate
                {
                    SetLabelVisibility(lbl, visibility);
                });
            }
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
                    SetButtonStatus(btn, status);
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

        private string GetTextBoxText(System.Windows.Controls.TextBox tb)
        {
            if (tb.Dispatcher.CheckAccess())
            {
                return tb.Text;
            }
            else
            {
                string text = "";
                tb.Dispatcher.Invoke(delegate
                {
                    text = tb.Text;
                });
                return text;
            }
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
                    SetTextBoxStatus(tb, status);
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

        private void AppendLine(System.Windows.Controls.ListBox lb, string text)
        {
            if (lb.Dispatcher.CheckAccess())
            {
                var message = new ConsoleMessage(text, Brushes.White);
                consoleMessages.Add(message);
                consoleListBox.ScrollIntoView(message);

                SetClearButtonStatus();
            }
            else
            {
                lb.Dispatcher.Invoke(delegate
                {
                    AppendLine(lb, text);
                    SetClearButtonStatus();
                });
            }
        }

        private void AppendLine(System.Windows.Controls.ListBox lb, string text, Brush color)
        {
            if (lb.Dispatcher.CheckAccess())
            {
                var message = new ConsoleMessage(text, color);
                consoleMessages.Add(message);
                consoleListBox.ScrollIntoView(message);
            }
            else
            {
                lb.Dispatcher.Invoke(delegate
                {
                    AppendLine(lb, text, color);
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
            MaximizeWindow(this);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeWindow(this);
            }
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            elapsedTime += TimeSpan.FromSeconds(1);
            timerLabel.Content = elapsedTime.ToString();
        }

        private void StartTimer()
        {
            if (!isTimerRunning)
            {
                timer.Start();
                isTimerRunning = true;
            }
        }

        private void PauseTimer()
        {
            if (isTimerRunning)
            {
                timer.Stop();
                isTimerRunning = false;
            }
        }

        private void StopTimer()
        {
            timer.Stop();
            SetLabelContent(timerLabel, elapsedTime.ToString());
            elapsedTime = TimeSpan.Zero;
            isTimerRunning = false;
        }

        private void ResetTimer()
        {
            elapsedTime = TimeSpan.Zero;
            SetLabelContent(timerLabel, elapsedTime.ToString());
            isTimerRunning = false;
        }

        // Handles the click event of the main menu buttons (File, Edit, View, Help)
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            string popupName = clickedButton.Tag.ToString();

            // Get the corresponding popup by name
            Popup popup = (Popup)this.FindName(popupName);

            // Toggle the popup visibility
            if (popup != null)
            {
                popup.IsOpen = !popup.IsOpen;
            }
        }

        // Handles click event for submenu items
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Button clickedItem = sender as Button;

            string windowName = clickedItem.Tag.ToString();

            try
            {
                // Get the current assembly (where all the windows are defined)
                Assembly currentAssembly = Assembly.GetExecutingAssembly();

                // Find the type (class) in the current assembly that matches the window name
                Type windowType = currentAssembly.GetType($"Hashy.{windowName}");

                // Ensure the type exists and is a subclass of Window
                if (windowType != null && windowType.IsSubclassOf(typeof(Window)))
                {
                    // Create an instance of the window
                    Window windowInstance = (Window)Activator.CreateInstance(windowType);

                    // Open the window
                    windowInstance.Show();
                }
                else
                {
                    MessageBox.Show($"Window '{windowName}' not found or is not a valid window class.");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur (e.g., if the window class cannot be instantiated)
                MessageBox.Show($"Error opening window '{windowName}': {ex.Message}");
            }
        }
    }
}