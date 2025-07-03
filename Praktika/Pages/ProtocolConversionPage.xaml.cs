using Praktika.Сomponents;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Логика взаимодействия для ProtocolConversionPage.xaml
    /// </summary>
    public partial class protocolConversionPage : Page
    {
        private string fileDirectory = null;
        private string filename = null;

        public protocolConversionPage()
        {
            InitializeComponent();
        }
        
        private void btnRender_Click(object sender, RoutedEventArgs e)
        {
            DirectoryHelper.CreateFolderPath();

            if (IsError())
                return;

            string protocolNumber = tbProtocolNumber.Text;
            string modelName = tbModelName.Text;

            try
            {
                GetColumnWidthForFile(fileDirectory, out int[] columnIndicesToKeep, out int columnIndicesCount, out Dictionary<int, int> columnWidths, out List<string> tableLines, out int tableLinesCount);

                using (StreamReader sr = new StreamReader(fileDirectory, Encoding.UTF8))
                using (StreamWriter sw = new StreamWriter($"Protocols\\{filename}", false, Encoding.UTF8))
                {
                    bool isTableSection = false;
                    int tableLineIndex = 0;

                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();

                        if (!isTableSection)
                        {
                            line = WriteHeaderAndFindTable(line, columnIndicesCount, columnIndicesToKeep, columnWidths, protocolNumber, modelName, ref isTableSection);

                            if (line != null)
                                sw.WriteLine(line);
                        }
                        else
                        {
                            line = WriteTableRow(line, tableLines, columnIndicesCount, columnIndicesToKeep, columnWidths, ref tableLineIndex, tableLinesCount);

                            if (line != null)
                                sw.WriteLine(line);
                        }
                    }
                }

                MessageBox.Show("Успешное преобразование файла", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (ArgumentNullException)
            {
                MessageBox.Show("Ошибка. Файл для редактирования не выбран", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException)
            {
                MessageBox.Show("Ошибка, нельзя форматировать файл из корневой папки, выберите файл из другой папки и после форматирования он появится в корневой папке", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public static void GetColumnWidthForFile(string fileDirectory, out int[] columnIndicesToKeep, out int columnIndicesCount, out Dictionary<int, int> columnWidths, out List<string> tableLines, out int tableLinesCount)
        {
            columnIndicesToKeep = null; // Индексы столбцов для сохранения
            columnIndicesCount = 0; //счётчик нацденных индексов
            columnWidths = new(); //ключ - индекс столбца, значение его макс ширина
            tableLines = new();
            tableLinesCount = 0;
            bool isTableSection = false;

            using (StreamReader sr = new(fileDirectory, Encoding.UTF8))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    if (!isTableSection)
                    {
                        if (line.TrimStart().StartsWith("N "))
                        {
                            isTableSection = true;
                            ParseHeadereLine(line, ref columnIndicesToKeep, ref columnIndicesCount, ref columnWidths);
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        tableLines.Add(line);
                        tableLinesCount++;

                        ParseTableLine(line, columnIndicesCount, columnIndicesToKeep, ref columnWidths);
                    }
                }
            }
        }
        public static void ParseHeadereLine(string line, ref int[] columnIndicesToKeep, ref int columnIndicesCount, ref Dictionary<int, int> columnWidths)
        {
            string[] columnsToKeep = { "N", "AList", "BEist", "Q", "FLOW_RATE", "Cx", "Cy", "Cz", "Mx", "My", "Mz", "Cxa", "Cya", "Cza", "Mxa", "Mya", "Mza", "Bx", "TVINT1", "VINT1" };
            columnIndicesToKeep = new int[columnsToKeep.Length];

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

        public static void ParseTableLine(string line, int columnIndicesCount, int[] columnIndicesToKeep, ref Dictionary<int, int> columnWidths)
        {
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

        static string WriteHeaderAndFindTable(string line, int columnIndicesCount, int[] columnIndicesToKeep, Dictionary<int, int> columnWidths, string protocolNumber, string modelName, ref bool isTableSection)
        {
            if (line.TrimStart().StartsWith("N "))
            {
                isTableSection = true;
                string[] headers = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string[] filteredHeaders = new string[columnIndicesCount];

                for (int i = 0; i < columnIndicesCount; i++)
                {
                    int columnIndex = columnIndicesToKeep[i];
                    if (i == 0)
                        filteredHeaders[i] = headers[columnIndex];
                    else
                        filteredHeaders[i] = headers[columnIndex].PadLeft(columnWidths[columnIndex]);
                }

                return (string.Join("    ", filteredHeaders));
            }
            else
            {
                line = ReplaceLineIfMatch(line, protocolNumber, modelName);

                if (line != null)
                    return (line);
            }

            isTableSection = false;
            return (null);
        }

        static string WriteTableRow(string line, List<string> tableLines, int columnIndicesCount, int[] columnIndicesToKeep, Dictionary<int, int> columnWidths,ref int tableLineIndex, int tableLinesCount)
        {

            if (string.IsNullOrWhiteSpace(line))
            {
                return " ";
            }

            if (tableLineIndex == 0 || tableLineIndex == tableLinesCount - 1)
            {
                tableLineIndex++;
                return null;
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

            return(string.Join("    ", filteredColumns));
        }

        public static string ReplaceLineIfMatch(string line, string protocolNumber, string modelName)
        {
            string pattern = @"(Protocol_Number\s+=\s+)|(ModelName\s+=\s+)|(ExpName\s+=\s+)|(PROTOCOL_DATE\s+=\s+)|(AL:\s+\d{1,2})|(PROCESSING_DATE\s+=\s+)";

            Match match = Regex.Match(line, pattern);
            if (match.Success)
            {
                if (match.Groups[1].Success)
                    return ($"Protocol_Number = {protocolNumber}");
                else if (match.Groups[2].Success)
                    return ($"ModelName = {modelName}");
                else if (match.Groups[3].Success)
                    return ("ExpName = Практика");
                else if (match.Groups[6].Success)
                    return ($"PROCESSING_DATE = {DateTime.Now}");
                else
                    return (line);
            }
            return null;
        }

        public bool IsError() // проверка входных данных 
        {
            StringBuilder errors = new StringBuilder("Есть ошибки, мешающие продолжить работу: ");
            int errorCount = 0;

            string numFormat = @"\d{5}";

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
                MessageBox.Show(errors.ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

            bool? result = dialog.ShowDialog();

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
