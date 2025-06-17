using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace Praktika.Pages
{
    /// <summary>
    /// Логика взаимодействия для HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        string fileDirectory = null;
        string filename = null;
        public HomePage()
        {
            InitializeComponent();
        }

        private void btnRender_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder errors = new StringBuilder("Есть ошибки мешающие продолжить работу: ");
            int countNumber = 0;

            string numFormat = @"\d{5}";

            if (string.IsNullOrEmpty(fileDirectory))
            {
                errors.AppendLine("Файл не открыт, сначала ткройте исходный файл");
                countNumber++;
            }
            if (tbModelName.Text == null || !Regex.IsMatch(tbModelName.Text, numFormat))
            {
                errors.AppendLine("Номер модели отсутствует или находится в неправильном формате(5 чисел)");
                countNumber++;
            }
            if (tbProtocolNumber.Text == null || !Regex.IsMatch(tbProtocolNumber.Text, numFormat))
            {
                errors.AppendLine("Номер протокола отсутствует или находится в неправильном формате(5 чисел)");
                countNumber++;
            }    
            
            if(countNumber > 0)
            {
                MessageBox.Show(errors.ToString());
                errors.Clear();
                return;
            }

            try
            {
                var columnsToKeep = new List<string> { "N", "AList", "BEist", "Q", "FLOW_RATE", "Cx", "Cy", "Cz", "Mx", "My", "Mz", "Cxa", "Cya", "Cza", "Mxa", "Mya", "Mza", "Bx", "TVINT1", "VINT1" };
                var columnIndicesToKeep = new List<int>();
                var columnWidths = new Dictionary<int, int>();
                var tableLines = new List<string>();
                bool isTableSection = false;

                // Чтение и анализ файла
                foreach (var line in File.ReadLines(fileDirectory, Encoding.UTF8))
                {
                    if (!isTableSection)
                    {
                        if (line.TrimStart().StartsWith("N "))
                        {
                            isTableSection = true;
                            var headers = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < headers.Length; i++)
                            {
                                if (columnsToKeep.Contains(headers[i]))
                                {
                                    columnIndicesToKeep.Add(i);
                                    columnWidths[i] = headers[i].Length;
                                }
                            }
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        tableLines.Add(line);
                        var columns = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (int i in columnIndicesToKeep)
                        {
                            if (i < columns.Length)
                            {
                                columnWidths[i] = Math.Max(columnWidths[i], columns[i].Length);
                            }
                        }
                    }
                }

                // Запись отфильтрованного результата
                using var sr = new StreamReader(fileDirectory, Encoding.UTF8);
                using var sw = new StreamWriter($@"C:\Users\Mickey\Desktop\{filename}", false, Encoding.UTF8);
                string pattern = @"(Protocol_Number\s+=\s+)|(ModelName\s+=\s+)|(ExpName\s+=\s+)|(PROTOCOL_DATE\s+=\s+)|(PROCESSING_DATE\s+=\s+)";
                isTableSection = false;
                int tableLineIndex = 0;

                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    if (!isTableSection)
                    {
                        Match m = Regex.Match(line, pattern);
                        if (m.Success)
                        {
                            sw.WriteLine(m.Groups[1].Success ? $"Protocol_Number = {tbProtocolNumber.Text}" :
                                           m.Groups[2].Success ? $"ModelName = {tbModelName.Text}" :
                                           m.Groups[3].Success ? "ExpName = Практика" :
                                           m.Groups[5].Success ? $"PROCESSING_DATE = {DateTime.Now}" : line);
                        }
                        else if (line.TrimStart().StartsWith("N "))
                        {
                            isTableSection = true;
                            var headers = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            var filteredHeaders = columnIndicesToKeep
                                .Select((i, idx) => idx == 0 ? headers[i] : headers[i].PadLeft(columnWidths[i]))
                                .ToList();
                            sw.WriteLine(string.Join("    ", filteredHeaders));
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            sw.WriteLine();
                            continue;
                        }

                        if (tableLineIndex == 0 || tableLineIndex == tableLines.Count - 1)
                        {
                            tableLineIndex++;
                            continue;
                        }

                        var columns = tableLines[tableLineIndex++].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var filteredColumns = columnIndicesToKeep
                            .Select((i, idx) => idx == 0 ? (i < columns.Length ? columns[i] : "") : (i < columns.Length ? columns[i].PadLeft(columnWidths[i]) : "".PadLeft(columnWidths[i])))
                            .ToList();
                        sw.WriteLine(string.Join("    ", filteredColumns));
                    }
                }
                MessageBox.Show("Успешное преобразование файла");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка, попробуйте ещё раз, ошибка {ex}");
            }
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Text documents (*.txt)|*.txt";
            dialog.FilterIndex = 1;

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {
                fileDirectory = dialog.FileName;
                filename = dialog.SafeFileName;
            }

            lbFilename.Content = "Файл: " + filename;
        }

        private void btnInterpol_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new InterpolPage());
        }
    }
}
