using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Praktika.Сomponents;

namespace Praktika.Pages
{
    /// <summary>
    /// Логика взаимодействия для InterpolPage.xaml
    /// </summary>
    public partial class InterpolPage : Page
    {
        string fileDirectory = null;
        string fileName = null;

        readonly string[] columnNames = ["Cx", "Cy", "Cz", "Mx", "My", "Mz", "Cxa", "Cya", "Cza", "Mxa", "Mya", "Mza", "Bx"];

        public InterpolPage()
        {
            InitializeComponent();
        }

        private void btnInterpol_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void btnInterpolExecute_Click(object sender, RoutedEventArgs e)
        {
            DirectoryHelper.CreateFolderPath();

            double[] x2Numbers = TryX2Number();

            if(x2Numbers.Length == 0)
                return;

            Dictionary<string, int> columnsIndex = new Dictionary<string, int>();
            Dictionary<string, List<double>> inputCoulmnsData = new Dictionary<string, List<double>>();

            bool IsTable = false;
            bool enterWhitespace = false;

            foreach (string line in File.ReadLines(fileDirectory, Encoding.UTF8))
            {
                if (line.TrimStart().StartsWith("N ") && !IsTable)
                {
                    IsTable = true;

                    var headers = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < headers.Length; i++)
                    {
                        if (columnNames.Contains(headers[i].Trim()))
                        {
                            columnsIndex[headers[i]] = i;
                            inputCoulmnsData[headers[i]] = new List<double>();
                        }

                    }
                }
                else if (IsTable && enterWhitespace)
                {
                    var columnsData = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < columnsData.Length; i++)
                    {
                        foreach (var item in columnsIndex)
                        {
                            if (i == item.Value)
                            {
                                if (Double.TryParse(columnsData[i], CultureInfo.InvariantCulture, out double value))
                                    inputCoulmnsData[item.Key].Add(value);
                            }
                        }
                    }
                }
                else if (IsTable && !enterWhitespace)
                {
                    enterWhitespace = true;
                }
            }

            if (inputCoulmnsData["Bx"].Count > 6)
            {
                MessageBox.Show("Ошибка в чтении, возможно выбран неотфильтрованный файл");
                return;
            }

            var xValues = GetX2Values(inputCoulmnsData, x2Numbers);
            if (xValues == null)
                return;

            var outputColumnData = GetOutputColumnData(columnNames, x2Numbers, xValues, inputCoulmnsData);
            if (outputColumnData == null)
                return;

            try
            {
                using (StreamReader sr = new StreamReader(fileDirectory, Encoding.UTF8))
                using (StreamWriter sw = new StreamWriter(DirectoryHelper.ReturnFolderPath() + @$"\{"Interpolate_protocol_" + fileName}", false, Encoding.UTF8))
                {
                    bool isTable = false;
                    int resultIndex = 0; // Индекс для прохода по interpolatedResults
                    string[] headers = null;
                    int[] columnWidths = null;
                    List<string[]> tableRows = new();
                    List<string> nonTableLines = new();

                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();

                        if (!isTable)
                        {
                            if (line.TrimStart().StartsWith("N "))
                            {
                                isTable = true;
                                headers = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                columnWidths = headers.Select(h => h.Length).ToArray();
                            }
                            else
                                sw.WriteLine(line);
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(line))
                                continue;

                            var data = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            tableRows.Add(data);
                        }
                    }

                    // Вычисляем ширину столбцов на основе данных и интерполированных значений
                    foreach (var data in tableRows)
                    {
                        string[] newRow = new string[data.Length];
                        for (int i = 0; i < data.Length; i++)
                        {
                            if (i == 0)
                            {
                                newRow[i] = data[i];
                                columnWidths[i] = Math.Max(columnWidths[i], newRow[i].Length);
                                continue;
                            }

                            var column = columnsIndex.FirstOrDefault(x => x.Value == i).Key;
                            if (column != null && outputColumnData.ContainsKey(column) && resultIndex < x2Numbers.Length)
                            {
                                double value = outputColumnData[column][resultIndex];
                                newRow[i] = value.ToString("F7", CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                if (double.TryParse(data[i], NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                                {
                                    newRow[i] = value.ToString("F7", CultureInfo.InvariantCulture);
                                }
                                else
                                {
                                    newRow[i] = data[i];
                                }
                            }

                            columnWidths[i] = Math.Max(columnWidths[i], newRow[i].Length);
                        }

                        resultIndex++;
                    }

                    sw.WriteLine(string.Join("    ", headers.Select((h, i) => h.PadLeft(columnWidths[i]))));

                    sw.WriteLine();

                    resultIndex = 0;

                    foreach (var data in tableRows)
                    {
                        string[] newRow = new string[data.Length];
                        for (int i = 0; i < data.Length; i++)
                        {
                            if (i == 0)
                            {
                                newRow[i] = data[i].PadLeft(columnWidths[i]);
                                continue;
                            }

                            var column = columnsIndex.FirstOrDefault(x => x.Value == i).Key;
                            if (column != null && outputColumnData.ContainsKey(column) && resultIndex < x2Numbers.Length)
                            {
                                double value = outputColumnData[column][resultIndex];
                                newRow[i] = value.ToString("F7", CultureInfo.InvariantCulture).PadLeft(columnWidths[i]);
                            }
                            else
                            {
                                if (double.TryParse(data[i], NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                                {
                                    newRow[i] = value.ToString("F7", CultureInfo.InvariantCulture).PadLeft(columnWidths[i]);
                                }
                                else
                                    newRow[i] = data[i].PadLeft(columnWidths[i]);
                            }
                        }

                        string formattedRow = string.Join("    ", newRow);
                        sw.WriteLine(formattedRow);

                        resultIndex++;
                    }
                }

                fileDirectory = DirectoryHelper.ReturnFolderPath() + $@"\{"Interpolate_protocol_" + fileName}";

                MessageBox.Show("Интерполяция выполнена успешно");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при попытке записи файла: {ex}");
            }
        }

        private void btnFileOpen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (fileDirectory == null)
                    throw new ArgumentNullException();

                Process process = new Process();
                process.StartInfo.FileName = "explorer";
                process.StartInfo.Arguments = "\"" + fileDirectory + "\"";
                process.Start();
            }
            catch(ArgumentNullException)
            {
                MessageBox.Show("Ошибка. Файл не выбран");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии файла: {ex.Message}");
            }
        }
        public double[] TryX2Number()
        {
            StringBuilder errors = new StringBuilder("Есть ошибки мешающие продолжить работу: ");
            int countNumber = 0;

            if (string.IsNullOrEmpty(fileDirectory))
            {
                errors.AppendLine("Файл не открыт, сначала ткройте файл");
                countNumber++;
            }
            if (!Double.TryParse(tbFirstX2.Text, CultureInfo.InvariantCulture, out double firstX2) | !Double.TryParse(tbSecondX2.Text, CultureInfo.InvariantCulture, out double secondX2) | !Double.TryParse(tbThirdX2.Text, CultureInfo.InvariantCulture, out double thirdX2) | !Double.TryParse(tbFourthX2.Text, CultureInfo.InvariantCulture, out double fourthX2) | !Double.TryParse(tbFifthX2.Text, CultureInfo.InvariantCulture, out double fifthX2) | !Double.TryParse(tbSixthX2.Text, CultureInfo.InvariantCulture, out double sixthX2)
                 || firstX2 >= secondX2 || firstX2 >= thirdX2 || firstX2 >= fourthX2 || firstX2 >= fifthX2 || firstX2 >= sixthX2
                 || secondX2 >= thirdX2 || secondX2 >= fourthX2 || secondX2 >= fifthX2 || secondX2 >= sixthX2
                 || thirdX2 >= fourthX2 || thirdX2 >= fifthX2 || thirdX2 >= sixthX2
                 || fourthX2 >= fifthX2 || fourthX2 >= sixthX2
                 || fifthX2 >= sixthX2
                 )
            {
                errors.AppendLine("Не введены желаемые значения интерполяции или введены невалидные значения");
                countNumber++;
            }

            if (countNumber > 0)
            {
                MessageBox.Show(errors.ToString());

                double[] x2Number = [];
                return x2Number;
            }

            double[] x2Numbers = [firstX2, secondX2, thirdX2, fourthX2, fifthX2, sixthX2];

            return x2Numbers;
        }

        public static Dictionary<double, (double x1, double x3, int indexLeft, int indexRight)> GetX2Values(Dictionary<string, List<double>> inputCoulmnsData, double[] x2Numbers)
        {
            Dictionary<double, (double x1, double x3, int indexLeft, int indexRight)> xValues = new Dictionary<double, (double x1, double x3, int indexLeft, int indexRight)>(); // словарь x2 ключ - вводимый x2, значение 1 - граничное слева, значение 2 - граничное справа
            try
            {
                for (int i = 0; i < inputCoulmnsData["Bx"].Count; i++) // заполнение x2 граничными значениями и их индексами
                {

                    if (i == 0 && x2Numbers[i] < inputCoulmnsData["Bx"][i])
                    {
                        xValues[x2Numbers[i]] = (inputCoulmnsData["Bx"][i], inputCoulmnsData["Bx"][i + 1], i, i + 1);
                        continue;
                    }

                    if (i + 1 > inputCoulmnsData["Bx"].Count - 1 & x2Numbers[i] > inputCoulmnsData["Bx"][i])
                    {
                        xValues[x2Numbers[i]] = (inputCoulmnsData["Bx"][i - 1], inputCoulmnsData["Bx"][i], i - 1, i);
                        continue;
                    }

                    if (i + 1 < inputCoulmnsData["Bx"].Count - 1 && x2Numbers[i] > inputCoulmnsData["Bx"][i] && x2Numbers[i] < inputCoulmnsData["Bx"][i + 1])
                    {
                        xValues[x2Numbers[i]] = (inputCoulmnsData["Bx"][i], inputCoulmnsData["Bx"][i + 1], i, i + 1);
                        continue;
                    }

                    if (i > 0 && x2Numbers[i] > inputCoulmnsData["Bx"][i - 1] && x2Numbers[i] < inputCoulmnsData["Bx"][i])
                    {
                        xValues[x2Numbers[i]] = (inputCoulmnsData["Bx"][i - 1], inputCoulmnsData["Bx"][i], i - 1, i);
                        continue;
                    }

                    if (i > 0 && x2Numbers[i] > inputCoulmnsData["Bx"][i] && x2Numbers[i] < inputCoulmnsData["Bx"][i + 1])
                        xValues[x2Numbers[i]] = (inputCoulmnsData["Bx"][i], inputCoulmnsData["Bx"][i + 1], i, i + 1);
                }
                return xValues;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex}");
                return null;
            }
        }

        public static Dictionary<string, List<double>> GetOutputColumnData(string[] columnNames, double[] x2Numbers, Dictionary<double, (double x1, double x3, int indexLeft, int indexRight)> xValues, Dictionary<string, List<double>> inputCoulmnsData)
        {
            Dictionary<string, List<double>> outputColumnData = new();

            try
            {
                foreach (var column in columnNames)
                {
                    outputColumnData[column] = new List<double>();
                }

                foreach (var x2 in x2Numbers)
                {
                    var (x1, x3, indexLeft, indexRight) = xValues[x2];

                    foreach (var column in columnNames)
                    {
                        if (column == "Bx")
                        {
                            outputColumnData["Bx"].Add(x2);
                            continue;
                        }

                        double y1 = inputCoulmnsData[column][indexLeft];
                        double y3 = inputCoulmnsData[column][indexRight];
                        double y2 = ((x2 - x1) * (y3 - y1)) / (x3 - x1);
                        outputColumnData[column].Add(y2);
                    }
                }
                return outputColumnData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex}");
                return null;
            }
        }
        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();

            dialog.Filter = "Text documents (*.txt)|*.txt";
            dialog.FilterIndex = 1;
            dialog.InitialDirectory = DirectoryHelper.ReturnFolderPath();

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {
                fileDirectory = dialog.FileName;
                fileName = dialog.SafeFileName;
            }

            lbFilename.Content = "Файл: " + fileName;
        }
        private void btnReadFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string fullText = File.ReadAllText(fileDirectory, Encoding.UTF8);
                tblTextFile.Text = fullText;
                svReadFile.Visibility = Visibility.Visible;
            }
            catch (ArgumentNullException)
            {
                MessageBox.Show($"Ошибка, файл не выбран");
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex}");
            }
        }

        private void ConverterPage_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new protocolConversionPage());
        }

        private void btnInterpolSender_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new InterpolSenderPage());
        }
    }
}