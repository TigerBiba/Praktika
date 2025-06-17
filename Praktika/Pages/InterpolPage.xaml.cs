using System;
using System.Collections.Generic;
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

            double firstX2 = 0;
            double secondX2 = 0;
            double thirdX2 = 0;
            double fourthX2 = 0;
            double fifthX2 = 0;
            double sixthX2 = 0;

            if (string.IsNullOrEmpty(fileDirectory))
            {
                errors.AppendLine("Файл не открыт, сначала ткройте файл");
                countNumber++;
            }
            if (!Double.TryParse(tbFirstX2.Text, out firstX2))
            {
                errors.AppendLine("Номер модели отсутствует или находится в неправильном формате(5 чисел)");
                countNumber++;
            }
            if (!Double.TryParse(tbSecondX2.Text, out secondX2))
            {
                errors.AppendLine("Номер модели отсутствует или находится в неправильном формате(5 чисел)");
                countNumber++;
            }
            if (!Double.TryParse(tbThirdX2.Text, out thirdX2))
            {
                errors.AppendLine("Номер модели отсутствует или находится в неправильном формате(5 чисел)");
                countNumber++;
            }
            if (!Double.TryParse(tbFourthX2.Text, out fourthX2))
            {
                errors.AppendLine("Номер модели отсутствует или находится в неправильном формате(5 чисел)");
                countNumber++;
            }
            if (!Double.TryParse(tbFifthX2.Text, out fifthX2))
            {
                errors.AppendLine("Номер модели отсутствует или находится в неправильном формате(5 чисел)");
                countNumber++;
            }
            if (!Double.TryParse(tbSixthX2.Text, out sixthX2))
            {
                errors.AppendLine("Номер модели отсутствует или находится в неправильном формате(5 чисел)");
                countNumber++;
            }

            if (countNumber > 0)
            {
                MessageBox.Show(errors.ToString());
                errors.Clear();
                return;
            }


        }
    }
}
