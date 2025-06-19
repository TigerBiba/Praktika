using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Praktika.Pages
{
    /// <summary>
    /// Логика взаимодействия для InterpolPage.xaml
    /// </summary>
    public partial class InterpolPage : Page
    {
        string fileDirectory = null;
        string fileName = null;
        public InterpolPage()
        {
            InitializeComponent();
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
                fileName = dialog.SafeFileName;
            }

            lbFilename.Content = "Файл: " + fileName;
        }

        private void btnInterpol_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder errors = new StringBuilder("Есть ошибки мешающие продолжить работу: ");
            int countNumber = 0;

            if (string.IsNullOrEmpty(fileDirectory))
            {
                errors.AppendLine("Файл не открыт, сначала ткройте файл");
                countNumber++;
            }
            if (!Double.TryParse(tbFirstX2.Text, out double firstX2) | !Double.TryParse(tbSecondX2.Text, out double secondX2) | !Double.TryParse(tbThirdX2.Text, out double thirdX2) | !Double.TryParse(tbFourthX2.Text, out double fourthX2) | !Double.TryParse(tbFifthX2.Text, out double fifthX2) | !Double.TryParse(tbSixthX2.Text, out double sixthX2))
            {
                errors.AppendLine("Номер модели отсутствует или находится в неправильном формате(5 чисел)");
                countNumber++;
            }

            if (countNumber > 0)
            {
                MessageBox.Show(errors.ToString());
                return;
            }

            double[] x2Numbers = [firstX2, secondX2, thirdX2, fourthX2, fifthX2, sixthX2];

            string[] columnNames = ["Cx", "Cy", "Cz", "Mx", "My", "Mz", "Cxa", "Cya", "Cza", "Mxa", "Mya", "Mza", "Bx"];

            Dictionary<string, int> columnsIndex = new Dictionary<string, int>(); // нужные индексы столбцов
            Dictionary<string, List<double>> inputCoulmnsData = new Dictionary<string, List<double>>(); // изначальные значения

            bool IsTable = false;
            bool enterWhitespace = false;

            foreach (string line in File.ReadLines(fileDirectory, Encoding.UTF8))
            {
                if (line.TrimStart().StartsWith("N ") && !IsTable) // чисто для заголовков
                {
                    IsTable = true;

                    var headers = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // удаляет ненужные пробелы и получается список из заголовков

                    for (int i = 0; i < headers.Length; i++)
                    {
                        if (columnNames.Contains(headers[i].Trim())) // Contains ищет совпадения в строке, если они есть то добавляет в словарь
                        {
                            columnsIndex[headers[i]] = i;
                            inputCoulmnsData[headers[i]] = new List<double>();
                        }

                    }
                }
                else if (IsTable && enterWhitespace)
                {
                    var columnsData = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // для строк таблицы

                    for (int i = 0; i < columnsData.Length; i++)
                    {
                        foreach (var item in columnsIndex)
                        {
                            if (i == item.Value)
                                inputCoulmnsData[item.Key].Add(Double.Parse(columnsData[i], CultureInfo.InvariantCulture)); // культура т.к. написано в инглиш формате
                        }
                    }
                }
                else if (IsTable && !enterWhitespace)
                {
                    enterWhitespace = true;
                }
            }

            Dictionary<double, (double x1, double x3, int indexLeft, int indexRight)> xValues = new Dictionary<double, (double x1, double x3, int indexLeft, int indexRight)>(); // словарь x2 ключ - вводимый x2, значение 1 - граничное слева, значение 2 - граничное справа

            for (int i = 0; i < inputCoulmnsData["Bx"].Count; i++) // заполнение x2 граничными значениями и их индексами
            {

                if (i == 0 && x2Numbers[i] < inputCoulmnsData["Bx"][i])
                {
                    xValues[x2Numbers[i]] = (inputCoulmnsData["Bx"][i], inputCoulmnsData["Bx"][i + 1], i, i + 1);
                    continue;
                }

                if (i + 1 > inputCoulmnsData["Bx"].Count - 1 & x2Numbers[i] > inputCoulmnsData["Bx"][i])
                {
                    xValues[x2Numbers[i]] = (inputCoulmnsData["Bx"][i - 1], inputCoulmnsData["Bx"][i], i - 1 , i);
                    continue;
                }

                if (i + 1 < inputCoulmnsData["Bx"].Count - 1 && x2Numbers[i] > inputCoulmnsData["Bx"][i] && x2Numbers[i] < inputCoulmnsData["Bx"][i + 1])
                {
                    xValues[x2Numbers[i]] = (inputCoulmnsData["Bx"][i], inputCoulmnsData["Bx"][i + 1], i , i + 1);
                    continue;
                }

                if (i > 0 && x2Numbers[i] > inputCoulmnsData["Bx"][i - 1] && x2Numbers[i] < inputCoulmnsData["Bx"][i])
                {
                    xValues[x2Numbers[i]] = (inputCoulmnsData["Bx"][i - 1], inputCoulmnsData["Bx"][i], i - 1 , i );
                    continue;
                }

                if (i > 0 && x2Numbers[i] > inputCoulmnsData["Bx"][i] && x2Numbers[i] < inputCoulmnsData["Bx"][i+1])
                    xValues[x2Numbers[i]] = (inputCoulmnsData["Bx"][i], inputCoulmnsData["Bx"][i+1], i , i + 1 );
            }

            Dictionary<string, List<double>> outputColumnData = new Dictionary<string, List<double>>(); // изменённые значения
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
                        outputColumnData["Bx"].Add(x2); // Для Bx просто добавляем x2
                        continue;
                    }

                    double y1 = inputCoulmnsData[column][indexLeft];
                    double y3 = inputCoulmnsData[column][indexRight];
                    double y2 = ((x2 - x1) * (y3 - y1)) / (x3 - x1); // Формула интерполяции
                    outputColumnData[column].Add(y2);
                }
            }

            using (StreamReader sr = new StreamReader(fileDirectory, Encoding.UTF8))
            using (StreamWriter sw = new StreamWriter($@"C:\Users\Mickey\Desktop\{1 + fileName}", false, Encoding.UTF8))
            {
                bool isTable = false;
                int resultIndex = 0; // Индекс для прохода по interpolatedResults
                string[] headers = null; // Для хранения заголовков
                int[] columnWidths = null; // Для хранения ширины столбцов
                List<string[]> tableRows = new List<string[]>(); // Для хранения строк таблицы
                List<string> nonTableLines = new List<string>(); // Для хранения нетабличных строк

                // Читаем файл и собираем данные
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    if (!isTable)
                    {
                        // Сохраняем строки до таблицы
                        if (line.TrimStart().StartsWith("N "))
                        {
                            isTable = true;
                            headers = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            columnWidths = headers.Select(h => h.Length).ToArray();
                        }
                        else
                        {
                            nonTableLines.Add(line); // Сохраняем нетабличные строки
                        }
                    }
                    else
                    {
                        // Пропускаем пустые строки внутри таблицы
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        // Разбиваем строку на значения
                        var data = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (data.Length != headers.Length)
                        {
                            continue;
                        }

                        // Сохраняем строку для последующей обработки
                        tableRows.Add(data);
                    }
                }

                // Вычисляем ширину столбцов на основе данных и интерполированных значений
                foreach (var data in tableRows)
                {
                    string[] newRow = new string[data.Length];
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (i == 0) // Столбец N сохраняем без изменений
                        {
                            newRow[i] = data[i];
                            columnWidths[i] = Math.Max(columnWidths[i], newRow[i].Length);
                            continue;
                        }

                        // Проверяем, есть ли столбец в columnsIndex
                        var column = columnsIndex.FirstOrDefault(x => x.Value == i).Key;
                        if (column != null && outputColumnData.ContainsKey(column) && resultIndex < x2Numbers.Length)
                        {
                            // Интерполированные значения
                            double value = outputColumnData[column][resultIndex];
                            newRow[i] = value.ToString("F7", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            // Форматируем числа с 7 знаками после точки
                            if (double.TryParse(data[i], NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                            {
                                newRow[i] = value.ToString("F7", CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                newRow[i] = data[i];
                            }
                        }

                        // Обновляем ширину столбца
                        columnWidths[i] = Math.Max(columnWidths[i], newRow[i].Length);
                    }

                    resultIndex++;
                }

                // Записываем нетабличные строки
                foreach (var line in nonTableLines)
                {
                    sw.WriteLine(line);
                }

                // Записываем заголовок таблицы
                string formattedHeader = string.Join("    ", headers.Select((h, i) => h.PadLeft(columnWidths[i])));
                sw.WriteLine(formattedHeader);

                // Записываем одну пустую строку после заголовка
                sw.WriteLine();

                // Сбрасываем resultIndex для записи таблицы
                resultIndex = 0;

                // Записываем таблицу
                foreach (var data in tableRows)
                {
                    string[] newRow = new string[data.Length];
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (i == 0) // Столбец N сохраняем без изменений
                        {
                            newRow[i] = data[i].PadLeft(columnWidths[i]);
                            continue;
                        }

                        // Форматируем значения
                        var column = columnsIndex.FirstOrDefault(x => x.Value == i).Key;
                        if (column != null && outputColumnData.ContainsKey(column) && resultIndex < x2Numbers.Length)
                        {
                            // Интерполированные значения
                            double value = outputColumnData[column][resultIndex];
                            newRow[i] = value.ToString("F7", CultureInfo.InvariantCulture).PadLeft(columnWidths[i]);
                        }
                        else
                        {
                            // Форматируем исходные числа
                            if (double.TryParse(data[i], NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                            {
                                newRow[i] = value.ToString("F7", CultureInfo.InvariantCulture).PadLeft(columnWidths[i]);
                            }
                            else
                            {
                                newRow[i] = data[i].PadLeft(columnWidths[i]);
                            }
                        }
                    }

                    // Формируем отформатированную строку
                    string formattedRow = string.Join("    ", newRow);
                    sw.WriteLine(formattedRow);

                    resultIndex++;
                }
            }

            MessageBox.Show("Интерполяция выполнена успешно");            
        }

        private void btnFileOpen_Click(object sender, RoutedEventArgs e)
        {
            string path = $@"C:\Users\Mickey\Desktop\{1 + fileName}";
            Process.Start(new ProcessStartInfo { FileName = "explorer", Arguments = $"/n,select,{path}" });
        }

        private void btnReadFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filePath = $@"C:\Users\Mickey\Desktop\{fileName}"; // Убедитесь, что fileName — строка, например, "example.txt"
                string fullText = File.ReadAllText(filePath, Encoding.UTF8);
                tblTextFile.Text = fullText;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении файла: {ex.Message}");
            }
        }
    }
}
