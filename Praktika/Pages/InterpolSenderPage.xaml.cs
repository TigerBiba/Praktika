using Microsoft.Win32;
using Praktika.Сomponents;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Praktika.Pages
{
    /// <summary>
    /// Логика взаимодействия для InterpolSenderPage.xaml
    /// </summary>
    public partial class InterpolSenderPage : Page
    {
        List<string> filesNames = new();
        List<string> filesDirectores = new();
        Dictionary<string, int> filesAndLambda = new();

        public InterpolSenderPage()
        {
            InitializeComponent();
        }

        private void btnEnterFiles_Click(object sender, RoutedEventArgs e)
        {
            filesNames.Clear();
            filesDirectores.Clear();
            filesAndLambda.Clear();

            try
            {
                OpenFileDialog dialog = new();

                dialog.Filter = "Text documents (*.txt)|*.txt";
                dialog.FilterIndex = 1;
                dialog.Multiselect = true;
                dialog.InitialDirectory = DirectoryHelper.ReturnFolderPath();

                bool? result = dialog.ShowDialog();

                if (result == true)
                {
                    foreach (var filePath in dialog.FileNames)
                    {
                        using (StreamReader sr = new(filePath, Encoding.UTF8))
                        {
                            while (!sr.EndOfStream)
                            {
                                string line = sr.ReadLine();

                                GetFilesAndLambda(line, filePath, ref filesAndLambda);
                            }
                        }
                        filesDirectores.Add(filePath);
                    }
                    foreach (var fileName in dialog.SafeFileNames)
                    {
                        filesNames.Add(fileName);
                    }
                }

                filesAndLambda = filesAndLambda.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

                tbFilesNames.Text = String.Join(" ", filesNames);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }
        public void GetFilesAndLambda(string line, string filePath, ref Dictionary<string, int> filesAndLambda)
        {
            string pattern = @"AL:\s+\d{1,2}";

            if (Regex.IsMatch(line, pattern))
            {
                var al = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                filesAndLambda.Add(filePath, int.Parse(al[1]));
            }
        }

        private void btnInterpolSenderExecute_Click(object sender, RoutedEventArgs e)
        {
            DirectoryHelper.CreateFolderPath();

            List<double> allBx = new();
            string tableHeader = null;
            int bxIndex = default;

            try
            {
                if (filesDirectores.Count == 0)
                    throw new ArgumentNullException();

                if (filesAndLambda.Count < 2 || filesDirectores.Count < 2)
                {
                    MessageBox.Show("Ошибка, выбранно меньше 2-х файлов, или в файлах не найдена лямбда", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (StreamReader sr = new StreamReader(filesDirectores[0], Encoding.UTF8))
                using (StreamWriter sw = new StreamWriter(DirectoryHelper.ReturnFolderPath() + $@"\Final_protocol.txt", false, Encoding.UTF8))
                {
                    double bx = default;
                    bool isTable = false;

                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        string[] headers = null;

                        if (line.TrimStart().StartsWith("N "))
                        {
                            isTable = true;
                            tableHeader = line;
                            headers = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < headers.Length; i++)
                            {
                                if (headers[i].Contains("Bx".Trim()))
                                    bxIndex = i;
                            }
                        }
                        else if (isTable)
                        {
                            if (string.IsNullOrWhiteSpace(line))
                                continue;
                            else
                            {
                                var tableLine = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (double.TryParse(tableLine[bxIndex], CultureInfo.InvariantCulture, out double value))
                                {
                                    bx = value;
                                    allBx.Add(bx);
                                }
                                else
                                {
                                    MessageBox.Show("Ошибка в чтении файла, Bx был неверного формата", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
                            }
                        }
                        else if (!isTable)
                            sw.WriteLine(line);
                    }
                }

                using (StreamWriter sw = new StreamWriter(DirectoryHelper.ReturnFolderPath() + $@"\Final_protocol.txt", true, Encoding.UTF8))
                {
                    for (int i = 0; i < allBx.Count; i++)
                    {
                        sw.WriteLine($"Bx: {allBx[i]}\n{tableHeader}\n");

                        foreach (var item in filesAndLambda)
                        {
                            using (StreamReader sr = new StreamReader(item.Key, Encoding.UTF8))
                            {
                                bool IsTable = false;
                                while (!sr.EndOfStream)
                                {
                                    string line = sr.ReadLine();

                                    if (line.TrimStart().StartsWith("N "))
                                        IsTable = true;

                                    else if (IsTable)
                                    {
                                        if (string.IsNullOrWhiteSpace(line))
                                            continue;

                                        var tableLine = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                        if (Double.TryParse(tableLine[bxIndex], CultureInfo.InvariantCulture, out double parsedValue) && allBx[i] == parsedValue)
                                        {
                                            sw.WriteLine(line);
                                        }
                                    }
                                }
                            }
                        }
                        sw.WriteLine();
                    }
                }
                MessageBox.Show("Успешное создание итогового файла", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch(ArgumentNullException)
            {
                MessageBox.Show("Ошибка, файл не выбран", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка{ex}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConverterPage_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new protocolConversionPage());
        }

        private void btnInterpol_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new InterpolPage());
        }
        private void btnInterpolSender_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}