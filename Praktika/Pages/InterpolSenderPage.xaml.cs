using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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

namespace Praktika.Pages
{
    /// <summary>
    /// Логика взаимодействия для InterpolSenderPage.xaml
    /// </summary>
    public partial class InterpolSenderPage : Page
    {
        List<string> filesNames = new();
        List<string> filesDirectores = new();
        public InterpolSenderPage()
        {
            InitializeComponent();
        }

        private void btnEnterFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new();

            dialog.Filter = "Text documents (*.txt)|*.txt";
            dialog.FilterIndex = 1;
            dialog.Multiselect = true;
            dialog.InitialDirectory = "Protocols\\";

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
            {
                foreach (var filePath in dialog.FileNames)
                {
                    filesDirectores.Add(filePath);
                }
                foreach (var fileName in dialog.SafeFileNames)
                {
                    filesNames.Add(fileName);
                }
            }

            tbFilesNames.Text = String.Join(" ", filesNames);
        }

        private void btnInterpolSender_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder fileHeader = new();

            using(StreamReader sr = new StreamReader(filesDirectores[0], Encoding.UTF8))
            {
                double bx = default;
                bool isTable = false;
                int bxIndex = default;

                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] headers = null;
                    
                    if (line.TrimStart().StartsWith("N ") && !isTable)
                    {
                        isTable = true;
                        headers = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < headers.Length; i++)
                        {
                            if (headers[i].Contains("Bx".Trim()))
                                bxIndex = i;
                        }
                    }
                    else if(isTable)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;
                        else
                        {
                            var tableLine = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            bx = Double.Parse(tableLine[bxIndex], CultureInfo.InvariantCulture);
                            fileHeader.Append($"Bx: {bx}");
                        }
                    }
                    else if(!isTable)
                        fileHeader.Append(line); 
                }
                MessageBox.Show(fileHeader.ToString());
            }    
            using (StreamWriter sw = new StreamWriter($@"Protocols/final_protocol", false, Encoding.UTF8))
            {

            }
        }
    }
}
