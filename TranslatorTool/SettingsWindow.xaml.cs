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
using DBLibrary;
using System.IO;
namespace TranslatorTool
{
    /// <summary>
    /// Логика взаимодействия для Окна настроек
    /// </summary>
    public partial class SettingsWindow : Window
    {
        Label T;
        string key;

        /// <summary>
        /// Конструктор окна настроек
        /// </summary>
        /// <param name="tip">строка подсказки</param>
        public SettingsWindow(ref Label tip)
        {
            T = tip;
            InitializeComponent();
        }

        /// <summary>
        /// Отмена
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Сохранение настроек
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save(object sender, RoutedEventArgs e)
        {
            if (key!=null)
                if (key.Length > 0)
            {
                Properties.Settings.Default.keyCode = key;
                Properties.Settings.Default.Save();       
            }
            Properties.Settings.Default.InclTips = (bool)IncludeTips.IsChecked;
            Properties.Settings.Default.Save();
            if ((bool)IncludeTips.IsChecked)
            {
                T.Visibility = Visibility.Visible;
                T.Height = 40;
            }
            else
            {
                T.Visibility = Visibility.Hidden; 
                T.Height = 0; 
            }
            Properties.Settings.Default.AutoSave = (bool)autosave.IsChecked;
            Properties.Settings.Default.Save();
            if ((bool)autosave.IsChecked == true)
            {
                Properties.Settings.Default.AutoSavePath = Pathtosave.Text;
                Properties.Settings.Default.Save();
                Properties.Settings.Default.AutoSaveTime = (int)autoint.Value;
                Properties.Settings.Default.Save();
            }
            this.Close();
        }

        /// <summary>
        /// Инициализация
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            KeyCode.Content = Properties.Settings.Default.keyCode;
            IncludeTips.IsChecked = Properties.Settings.Default.InclTips;
            autosave.IsChecked = Properties.Settings.Default.AutoSave;
            Pathtosave.Text = Properties.Settings.Default.AutoSavePath;
            autoint.Value = Properties.Settings.Default.AutoSaveTime;
            autointLabel.Content = "Интервал времени: " + Properties.Settings.Default.AutoSaveTime + " мин";
            if ((bool)autosave.IsChecked!=true)
            {
                Pathtosave.IsEnabled = false;
                autoint.IsEnabled = false;

            }
        }

        /// <summary>
        /// Получить горячую клавишу
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetKey(object sender, KeyEventArgs e)
        {
            if ((bool)KeyCode.IsChecked)
            {
                key = e.Key.ToString();
                KeyCode.IsChecked = false;
                KeyCode.Content = e.Key.ToString();
            }
        }

        /// <summary>
        /// Изменение подписи CheckBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IncludeTips_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)IncludeTips.IsChecked)            
                lbl1.Content = "Включить подказки в статусной строке";             
            else 
                lbl1.Content = "Выключить подказки в статусной строке";
        }

        /// <summary>
        /// Добавление нового языка
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddNewLanguage(object sender, RoutedEventArgs e)
        {
            if (NewLanguage.Text.Length > 0)
                if (!TranslatorDB.AddLang(NewLanguage.Text))
                    MessageBox.Show("Не удалось подключение к Базе Данных!");
        }

        /// <summary>
        /// Смена значения слайдера для интервала автосохранения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void autoint_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int val = (int)Math.Round(autoint.Value);
            autoint.Value = val;
            autointLabel.Content = "Интервал времени: " + autoint.Value + " мин";
        }

        /// <summary>
        /// Включение/выключение автосохранения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void autosave_Checked(object sender, RoutedEventArgs e)
        {
           
                Pathtosave.IsEnabled = true;
                autoint.IsEnabled = true;
            
        }

        /// <summary>
        /// Сменить папку автосохранения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fd = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = fd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
                Pathtosave.Text = fd.SelectedPath;
            }
        }

        private void autosave_Unchecked(object sender, RoutedEventArgs e)
        {

            Pathtosave.IsEnabled = false;
            autoint.IsEnabled = false;
        }
    }
}
