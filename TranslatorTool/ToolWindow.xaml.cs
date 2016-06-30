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
using System.Windows.Shapes;
using System.Configuration;
using System.Xml;
namespace TranslatorTool
{
    /// <summary>
    /// Логика взаимодействия для ToolWindow.xaml
    /// </summary>
    public partial class ToolWindow : Window
    {
        public ToolWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (key.Length > 0) {
                Properties.Settings.Default.keyCode= key;
                Properties.Settings.Default.Save();

                }
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        { 
            
            KeyCode.Content = Properties.Settings.Default.keyCode;
        }

        string key;
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            
            if ((bool)KeyCode.IsChecked) { key = e.Key.ToString();
            KeyCode.IsChecked = false;
            KeyCode.Content = e.Key.ToString();

           
            }
        }
    }
}
