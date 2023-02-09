using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
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
using TextBox = System.Windows.Controls.TextBox;

namespace RevitAPITraining_ArrangeElemBtwTwoPoints
{
    /// <summary>
    /// Логика взаимодействия для MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView(ExternalCommandData commandData)
        {
            InitializeComponent();
            MainViewViewModel vm = new MainViewViewModel(commandData);
            vm.CloseRequest += (s, e) => this.Close();
            vm.HideRequest += (s, e) => this.Hide();
            vm.ShowRequest += (s, e) => this.Show();
            DataContext = vm;
        }
        private void TextBox1_LostFocus(object sender, RoutedEventArgs e)
        {
            MainViewViewModel vm = DataContext as MainViewViewModel;
            TextBox textBox = sender as TextBox;

            if (!int.TryParse(textBox.Text, out int result) || textBox.Text == null)
            {
                MessageBox.Show("Введите целое число.", "Ошибка ввода");
            }
            else if (result <= 2)
            {
                vm.NumElem = 2;
                TextBox1.Text = vm.NumElem.ToString();
            }
            else
            {
                vm.NumElem = (int)Math.Round((double)result);
                TextBox1.Text = vm.NumElem.ToString();
            }
        }
    }
}
