using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Praktika.Сomponents;



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
            FilesHelper.CreateFolderPath();

            if (IsError())
                return;

            try
            {
                string[] columnsToKeep = { "N", "AList", "BEist", "Q", "FLOW_RATE", "Cx", "Cy", "Cz", "Mx", "My", "Mz", "Cxa", "Cya", "Cza", "Mxa", "Mya", "Mza", "Bx", "TVINT1", "VINT1" };
                int[] columnIndicesToKeep = new int[columnsToKeep.Length]; // Индексы столбцов для сохранения
                int columnIndicesCount = 0; //счётчик нацденных индексов
                Dictionary<int, int> columnWidths = new Dictionary<int, int>(); //ключ - индекс столбца, значение его макс ширина
                string[] tableLines = new string[1000];
                int tableLinesCount = 0;
                bool isTableSection = false;

                using (StreamReader sr = new StreamReader(fileDirectory, Encoding.UTF8))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();

                        if (!isTableSection)
                        {
                            if (line.TrimStart().StartsWith("N "))
                            {
                                isTableSection = true;
                                string[] headers = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                for (int i = 0; i < headers.Length; i++)
                                {
                                    for (int j = 0; j < columnsToKeep.Length; j++)
                                    {
                                        if (headers[i] == columnsToKeep[j])
                                        {
                                            columnIndicesToKeep[columnIndicesCount] = i;
                                            columnWidths[i] = headers[i].Length;
                                            columnIndicesCount++;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(line))
                        {
                            tableLines[tableLinesCount] = line;
                            tableLinesCount++;

                            string[] columns = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < columnIndicesCount; i++)
                            {
                                int columnIndex = columnIndicesToKeep[i];
                                if (columnIndex < columns.Length)
                                {
                                    if (columnWidths.ContainsKey(columnIndex))
                                    {
                                        columnWidths[columnIndex] = Math.Max(columnWidths[columnIndex], columns[columnIndex].Length);
                                    }
                                }
                            }
                        }
                    }
                }

                using (StreamReader sr = new StreamReader(fileDirectory, Encoding.UTF8))
                using (StreamWriter sw = new StreamWriter($"Protocols\\{filename}", false, Encoding.UTF8))
                {
                    string pattern = @"(Protocol_Number\s+=\s+)|(ModelName\s+=\s+)|(ExpName\s+=\s+)|(PROTOCOL_DATE\s+=\s+)|(AL:\s+\d{1,2})|(PROCESSING_DATE\s+=\s+)";
                    isTableSection = false;
                    int tableLineIndex = 0;

                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();

                        if (!isTableSection)
                        {
                            Match match = Regex.Match(line, pattern);
                            if (match.Success)
                            {
                                if (match.Groups[1].Success)
                                    sw.WriteLine($"Protocol_Number = {tbProtocolNumber.Text}");
                                else if (match.Groups[2].Success)
                                    sw.WriteLine($"ModelName = {tbModelName.Text}");
                                else if (match.Groups[3].Success)
                                    sw.WriteLine("ExpName = Практика");
                                else if (match.Groups[6].Success)
                                    sw.WriteLine($"PROCESSING_DATE = {DateTime.Now}");
                                else
                                    sw.WriteLine(line);
                            }
                            else if (line.TrimStart().StartsWith("N "))
                            {
                                isTableSection = true;
                                string[] headers = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                string[] filteredHeaders = new string[columnIndicesCount];

                                //Заголовки
                                for (int i = 0; i < columnIndicesCount; i++)
                                {
                                    int columnIndex = columnIndicesToKeep[i];
                                    if (i == 0)
                                        filteredHeaders[i] = headers[columnIndex];
                                    else
                                        filteredHeaders[i] = headers[columnIndex].PadLeft(columnWidths[columnIndex]);
                                }

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

                            if (tableLineIndex == 0 || tableLineIndex == tableLinesCount - 1) //не записываем нули
                            {
                                tableLineIndex++;
                                continue;
                            }

                            string[] columns = tableLines[tableLineIndex].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            string[] filteredColumns = new string[columnIndicesCount];
                            tableLineIndex++;

                            for (int i = 0; i < columnIndicesCount; i++)
                            {
                                int columnIndex = columnIndicesToKeep[i];
                                if (columnIndex < columns.Length)
                                {
                                    if (i == 0)
                                        filteredColumns[i] = columns[columnIndex];
                                    else
                                        filteredColumns[i] = columns[columnIndex].PadLeft(columnWidths[columnIndex]);
                                }
                            }

                            sw.WriteLine(string.Join("    ", filteredColumns));
                        }
                    }
                }

                MessageBox.Show("Успешное преобразование файла");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка, попробуйте ещё раз, ошибка {ex}");
            }
        }
        public bool IsError() // проверка входных данных 
        {
            StringBuilder errors = new StringBuilder("Есть ошибки, мешающие продолжить работу: ");
            int errorCount = 0;

            string numFormat = @"\d{5}";

            if (string.IsNullOrEmpty(fileDirectory))
            {
                errors.AppendLine("Файл не открыт, сначала откройте исходный файл");
                errorCount++;
            }
            if (tbModelName.Text == null || !Regex.IsMatch(tbModelName.Text, numFormat))
            {
                errors.AppendLine("Номер модели отсутствует или находится в неправильном формате (5 цифр)");
                errorCount++;
            }
            if (tbProtocolNumber.Text == null || !Regex.IsMatch(tbProtocolNumber.Text, numFormat))
            {
                errors.AppendLine("Номер протокола отсутствует или находится в неправильном формате (5 цифр)");
                errorCount++;
            }

            if (errorCount > 0)
            {
                MessageBox.Show(errors.ToString());
                errors.Clear();
                return true;
            }
            return false;
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new();
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

        private void btnInterpolSender_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new InterpolSenderPage());
        }
    }
}
