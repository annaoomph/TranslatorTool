using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel; 
using System.Text; 
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using DBLibrary;
using Microsoft.Office.Interop;
using System.Windows.Media.Animation;
using System.Threading;

namespace TranslatorTool
{
    /// <summary>
    /// Логика взаимодействия для основного окна
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Windows.Forms.Timer autosave = new System.Windows.Forms.Timer();
        int previewFocus = -1; //элемент, получающий фокус
        List<Dictionary<string, string>> Result;
        bool includeDate = false;
        bool notSearched = true;
        bool selectingmode = false;
        bool includeLang = false;    
        SettingsWindow winTool;
        Dictionary<int, string> comments;
        Dictionary<string, string> Searched = new Dictionary<string, string>();
        bool nodb = false;
        int selectedid = -1;
        bool edit = false, update = false;
        string authorName, textName;
        string textRu, textEn;
        bool searchSong = false;
        int prev_selection = -1;
        bool authorTip = false;
        bool nameTip = false;
        bool nametrTip = false;
        bool Mfocused = false;
        List<string> copyList;
        int sort = 0;        
        string AutoSavePath="Autosave\\";
        bool reversed = true;        

        public MainWindow()
        {           
            InitializeComponent();         
            LoadAll(); 
        }        
     
        /// <summary>
        /// Загрузка начальной страницы и инициализация элементов
        /// </summary>
        private void LoadAll()
        {
            ListOfTranslations.Items.Clear();
            if (Properties.Settings.Default.InclTips)
            { 
                Tip.Height = 40; 
                Tip.Visibility = Visibility.Visible; 
            } else 
            { 
                Tip.Height = 0; 
                Tip.Visibility = Visibility.Hidden; 
            }
            if (DBLibrary.TranslatorDB.Connect())
            {
                
                Result = DBLibrary.TranslatorDB.SelectAll(sort);
                Result.Reverse();
                if (Result == null)
                    NoDB();
                else
                {
                    ShowList();
                    List<string> Languages = DBLibrary.TranslatorDB.GetLang();
                    LanguagesList.Items.Clear();
                    foreach (string item in Languages)
                        LanguagesList.Items.Add(item);
                }
                DateFrom.SelectedDate = null;
                DateTo.SelectedDate = null;
                complete.IsChecked = null;
                ListOfTranslations.SelectedIndex = 0;
            }
            else                       
                NoDB();
            autosave.Tick += autosave_Tick;
            
        }

        /// <summary>
        /// Автосохранение, если включено
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void autosave_Tick(object sender, EventArgs e)
        {
            DirectoryInfo di = new DirectoryInfo(AutoSavePath);
            bool exists = di.Exists;
            if (!exists)
                Directory.CreateDirectory(AutoSavePath);
            string filename = "";
            if (authorName.Length == 0)
                filename = "AutoSave " + DateTime.Now;
            else filename = authorName + " - " + textName;
            FileStream fs = new FileStream(AutoSavePath + filename + ".txt", FileMode.Create);
            using (StreamWriter sr = new StreamWriter(fs))
            {                
                for (int i = 0; i < EnBox.Items.Count; i++)
                {
                    TextBox tb = (TextBox)EnBox.Items[i];
                    sr.WriteLine(tb.Text);                   
                }
                sr.WriteLine("#");
                for (int i = 0; i < RuBox.Items.Count; i++)
                {
                    TextBox tb = (TextBox)RuBox.Items[i];
                    if (comments.ContainsKey(i))
                       sr.WriteLine(tb.Text + '*');
                    else sr.WriteLine(tb.Text);                    
                }               
                for (int i = 0; i < comments.Count; i++)
                    sr.WriteLine("* " + comments.Values.ElementAt(i));
            }
        }
        
        /// <summary>
        /// Решение проблемы с подключением к БД (отключение некоторых функций)
        /// </summary>
        private void NoDB()
        {
            MessageBox.Show("Проблемы с подключением к БД!");
            nodb = true;
            searchDB.IsEnabled = false;
            Filter.IsEnabled = false;
            Reset.IsEnabled = false;
            LanguagesList.IsEnabled = false;
            complete.IsEnabled = false;
            DateFrom.IsEnabled = false;
            DateTo.IsEnabled = false;
            EditFile.IsEnabled = false;
            sortauthor.IsEnabled = false;
            sortname.IsEnabled = false;
            sortdata.IsEnabled = false;            
        }

        /// <summary>
        /// Обновить (загрузить) список переводов
        /// </summary>
        private void ShowList()
        {
            ListOfTranslations.Items.Clear();
            int i = 0;
            foreach (Dictionary<string, string> item in Result)
            {
                Label l = new Label();
                l.Style = (Style)this.Resources["ListLabel"];
                l.Content = item["Author"] + " - " + item["Name"];
                l.Name = "Tlabel" + i;
                if (Convert.ToInt32(item["Complete"]) == 1)
                    l.Foreground = (Brush)this.Resources["completeBrush"];
                ListOfTranslations.Items.Add(l);
                i++;                
            }
            amount.Content = "Файлов: " + Result.Count;
        }
       
        /// <summary>
        /// Обработчик клика по кнопке интернет-поиска
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ISearchButton(object sender, RoutedEventArgs e)
        {       
            Search();
        }

        /// <summary>
        /// Интернет-поиск по нажатию клавиши Enter 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void search_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate { wait.Content = "Пожалуйста, подождите..."; }));
                Search();
            }
        }

        /// <summary>
        /// Скрытие Placeholdera для поиска в интернете
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void search_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!searchSong)
            {
                searchSong = true;
                search.Text = "";
            }
        }

        /// <summary>
        /// Поиск текста в интернете
        /// </summary>
        private void Search() {
            SearchResults.Items.Clear();
            if (search.Text.Length > 0)
            {         
                DoubleAnimation da = new DoubleAnimation();
                string Search = search.Text;
                Searched = InternetWorker.Search(Search);               
                if (Searched.Count == 0)
                    MessageBox.Show("Ничего не найдено!");
                else
                {
                    for (int i = 0; i < Searched.Count; i++)
                        SearchResults.Items.Add(Searched.Keys.ElementAt(i));          
                    da = new DoubleAnimation();
                    da.From = 0;
                    da.To = ISearch.RowDefinitions[1].ActualHeight;
                    da.Duration = TimeSpan.FromSeconds(1);
                    SearchResults.BeginAnimation(ListBox.HeightProperty, da);
                }
            }
        }

        /// <summary>
        /// Выбор и загрузка текста, найденного в интернете
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                string str = (string)e.AddedItems[0];
                string path = Searched[str];
                RuBox.Items.Clear();
                commentForm.Clear();
                EnBox.Items.Clear();
                List<string> enItems = InternetWorker.GetText(path);
                int ind = str.IndexOf('-');
                authorName = str.Substring(0, ind - 1);
                textName = str.Substring(ind + 2);
                edit = true;
                for (int i = 0; i < enItems.Count; i++)
                {
                    TextBox nb = new TextBox();
                    nb.Style = (Style)this.Resources["flatTextBox"];
                    nb.Text = enItems[i];
                    nb.Name = "tb_" + i;
                    nb.LostFocus += this.TextBox_LostFocus;
                    nb.PreviewKeyDown += this.TextBox_EnPreviewKeyDown;
                    nb.HorizontalAlignment = HorizontalAlignment.Stretch;
                    nb.PreviewMouseDown += this.TextBox_EnMouseDown;
                    EnBox.Items.Add(nb);
                    nb = new TextBox();
                    nb.Style = (Style)this.Resources["flatTextBox"];
                    nb.Name = "rb_" + i;
                    nb.LostFocus += this.TextBox_LostFocus;
                    nb.HorizontalAlignment = HorizontalAlignment.Stretch;
                    nb.PreviewMouseDown += this.TextBox_RuMouseDown;
                    nb.PreviewKeyDown += this.TextBox_RuPreviewKeyDown;
                    RuBox.Items.Add(nb);
                }
                sliderselection.Maximum = enItems.Count;
                sliderselection.Interval = 1;
                comments = new Dictionary<int, string>();
                RuBox.SelectedIndex = 0;
                EnBox.SelectedIndex = 0;
                ISearch.Visibility = Visibility.Hidden;
                if (Properties.Settings.Default.AutoSave == true) 
                {
                    autosave.Interval = Properties.Settings.Default.AutoSaveTime*1000*60;
                    autosave.Start();
                    AutoSavePath = Properties.Settings.Default.AutoSavePath;
                }
                Translation.Visibility = Visibility.Visible;
                wait.Visibility = Visibility.Hidden;
            }
         
        }

        /// <summary>
        /// Создание нового файла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewFile(object sender, RoutedEventArgs e)
        {
            edit = false;
            update = false;            
            ListTranslate.Visibility = Visibility.Hidden;
            Import.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Открытие текстового файла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.OpenFileDialog myDialog = new Microsoft.Win32.OpenFileDialog();
                bool result = (bool)myDialog.ShowDialog();
                string path = "";
                string text = "";
                if (result)
                {
                    path = myDialog.FileName;
                    Encoding encoding;
                    Stream fs = new FileStream(path, FileMode.Open);
                    using (StreamReader sr = new StreamReader(fs, true))
                        encoding = sr.CurrentEncoding;
                    if (path.IndexOf("doc") != -1)
                    {
                        Microsoft.Office.Interop.Word.Application app = new Microsoft.Office.Interop.Word.Application();
                        Object fileName = @path;
                        Object missing = Type.Missing;
                        app.Documents.Open(ref fileName, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing, ref missing);
                        Microsoft.Office.Interop.Word.Document doc = app.ActiveDocument;
                        doc.GrammarChecked = false;
                        Object start = Type.Missing;
                        Object end = Type.Missing;
                        Microsoft.Office.Interop.Word.Range rng = doc.Range(ref start, ref end);
                        rng.Select();
                        text = rng.Text.ToString();
                        app.ActiveDocument.Close();
                        app.Quit();
                    }
                    else
                        text = File.ReadAllText(path, encoding);
                    FlowDocument document = new FlowDocument();
                    Paragraph paragraph = new Paragraph();
                    paragraph.Inlines.Add(new Run(text));
                    document.Blocks.Add(paragraph);
                    richTextBox1.Document = document;
                }
            }
            catch (Exception) {
                MessageBox.Show("Выбранный вами файл, по видимому, не содержит текста, или открытие данного типа файлов не поддерживается программой.");
            }
        }
        
       /// <summary>
       /// Обработчик нажатия на кнопку импорта
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
        private void ImportNext(object sender, RoutedEventArgs e) 
        {
            RuBox.Items.Clear();
            commentForm.Clear();
            EnBox.Items.Clear();          
            string Str = new TextRange(richTextBox1.Document.ContentStart, richTextBox1.Document.ContentEnd).Text;
            List<string> enItems;
            if ((bool)splitter.IsChecked)
                enItems = TextWorker.Split(Str);  
            else enItems = TextWorker.Create(Str);
            if (enItems.Count > 0)
            {
                for (int i = 0; i < enItems.Count; i++)
                {
                    TextBox nb = new TextBox();
                    nb.Style = (Style)this.Resources["flatTextBox"];
                    nb.Text = enItems[i];
                    nb.Name = "tb_" + i;
                    nb.LostFocus += this.TextBox_LostFocus;
                    nb.PreviewKeyDown += this.TextBox_EnPreviewKeyDown;
                    nb.HorizontalAlignment = HorizontalAlignment.Stretch;
                    nb.PreviewMouseDown += this.TextBox_EnMouseDown;
                    EnBox.Items.Add(nb);
                    nb = new TextBox();
                    nb.Style = (Style)this.Resources["flatTextBox"];
                    nb.Name = "rb_" + i;
                    nb.LostFocus += this.TextBox_LostFocus;
                    nb.HorizontalAlignment = HorizontalAlignment.Stretch;
                    nb.PreviewMouseDown += this.TextBox_RuMouseDown;
                    nb.PreviewKeyDown += this.TextBox_RuPreviewKeyDown;
                    RuBox.Items.Add(nb);
                }
                sliderselection.Maximum = enItems.Count;
                sliderselection.Interval = 1;
                comments = new Dictionary<int, string>();
                Import.Visibility = Visibility.Hidden;
                if (Properties.Settings.Default.AutoSave == true) 
                {
                    autosave.Interval = Properties.Settings.Default.AutoSaveTime*1000*60;
                    autosave.Start();
                    AutoSavePath = Properties.Settings.Default.AutoSavePath;
                }
                Translation.Visibility = Visibility.Visible;
                RuBox.SelectedIndex = 0;
                EnBox.SelectedIndex = 0;          
            }
        }

       /// <summary>
       /// Открытие файла из БД для редактирования
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
        private void EditFileBD(object sender, RoutedEventArgs e)
        {
            if (selectedid >= 0 && ListOfTranslations.Items.Count>0)
            {
                RuBox.Items.Clear();
                commentForm.Clear();
                EnBox.Items.Clear();
                comments = new Dictionary<int, string>();
                string pathFile = Result[selectedid]["Path"];
                try
                {
                    FileStream file1 = new FileStream(pathFile, FileMode.Open);
                    StreamReader reader = new StreamReader(file1);
                    reader.ReadLine();
                    reader.ReadLine();
                    reader.ReadLine();
                    bool end = true;
                    string line = "";
                    int i = 0;
                    while (!reader.EndOfStream && end)
                    {
                        line = reader.ReadLine();
                        if (line == "#")
                            end = false;
                        else
                        {
                            TextBox nb = new TextBox();
                            nb.Style = (Style)this.Resources["flatTextBox"];
                            nb.Text = line;
                            nb.Name = "tb_" + i;
                            nb.LostFocus += this.TextBox_LostFocus;
                            nb.PreviewKeyDown += this.TextBox_EnPreviewKeyDown;
                            nb.HorizontalAlignment = HorizontalAlignment.Stretch;
                            nb.PreviewMouseDown += this.TextBox_EnMouseDown;
                            EnBox.Items.Add(nb);
                            i++;
                        }
                    }
                    end = true;
                    i = 0;
                    List<int> Keys = new List<int>();
                    while (!reader.EndOfStream && end)
                    {
                        line = reader.ReadLine();
                        if (line == "#")
                            end = false;
                        else
                        {
                            if (line.Length == 0)
                                line = " ";
                            if (line[line.Length - 1] == '*')
                            {
                                Keys.Add(i);
                                line = line.Substring(0, line.Length - 1);
                            }
                            TextBox nb = new TextBox();
                            nb.Style = (Style)this.Resources["flatTextBox"];
                            nb.Text = line;
                            nb.Name = "rb_" + i;
                            nb.LostFocus += this.TextBox_LostFocus;
                            nb.PreviewKeyDown += this.TextBox_RuPreviewKeyDown;
                            nb.HorizontalAlignment = HorizontalAlignment.Stretch;
                            nb.PreviewMouseDown += this.TextBox_RuMouseDown;
                            RuBox.Items.Add(nb);
                            i++;
                        }
                    }
                    if (reader.ReadLine() == "1")
                        splitter.IsChecked = true;
                    else splitter.IsChecked = false;
                    foreach (int key in Keys)
                    {
                        string com = reader.ReadLine();
                        com = com.Substring(1);
                        comments.Add(key, com);
                    } 
                    reader.Close();
                    sliderselection.Maximum = i;
                    sliderselection.Interval = 1;
                    authorName = Result[selectedid]["Author"];
                    textName = Result[selectedid]["Name"];
                    ListTranslate.Visibility = Visibility.Hidden;
                      if (Properties.Settings.Default.AutoSave == true) 
                      {
                          autosave.Interval = Properties.Settings.Default.AutoSaveTime*1000*60;
                          autosave.Start();
                          AutoSavePath = Properties.Settings.Default.AutoSavePath;
                      }
                    Translation.Visibility = Visibility.Visible;
                    RuBox.SelectedIndex = 0;
                    EnBox.SelectedIndex = 0;
                    edit = true;
                    update = true;
                   
                }
                catch (Exception)
                {
                    MessageBoxResult mr = MessageBox.Show("Выбранный файл не может быть найден. Возможно, он был удален или перемещен в другое место. Удалить запись из Базы Данных?", "Внимание", MessageBoxButton.YesNo);
                    if (mr == MessageBoxResult.Yes)
                    {
                        TranslatorDB.Delete(Result[selectedid]["ID"]);
                        LoadAll();
                    }
                }
            }
        }

        /// <summary>
        /// Удалить из БД
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteFromBD_Click(object sender, RoutedEventArgs e)
        {
            if (selectedid >= 0)
            {
                MessageBoxResult mr = MessageBox.Show("Вы уверены?", "Внимание", MessageBoxButton.YesNo);
                if (mr == MessageBoxResult.Yes)
                {
                    TranslatorDB.Delete(Result[selectedid]["ID"]);
                    LoadAll();
                }
            }
        }

        /// <summary>
        /// Перейти на страницу поиска
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GoToSearchPage(object sender, RoutedEventArgs e)
        {
            edit = false;
            update = false;
            ListTranslate.Visibility = Visibility.Hidden;
            ISearch.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Отображение информации о файле
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListOfTranslations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                Label l = (Label)e.AddedItems[0];
                string str = l.Name;
                string strsub = str.Substring(6);               
                int id = Convert.ToInt32( strsub);
                selectedid = id;
                string Info = "Дата создания: " + Result[id]["Date"] + '\n';
                Info = Info + "Язык: " + Result[id]["Language"] + '\n';               
                Info = Info + "Файл: " + Result[id]["Path"] + '\n';
                InfoBox.Text = Info;
            }
        }

        /// <summary>
        /// Поиск перевода по слову
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchWord(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (searchDB.Text.Length > 0)
                {
                    if ((bool)searchInText.IsChecked)
                        SearchInText(searchDB.Text);
                    else
                    {
                        Result = DBLibrary.TranslatorDB.Search(searchDB.Text, sort);
                        Result.Reverse();
                        ShowList();
                    }
                }
            }
        }

        /// <summary>
        /// Поиск по текстам переводов
        /// </summary>
        /// <param name="word">слово</param>
        private void SearchInText(string word){
            ListOfTranslations.Items.Clear();
            int i = 0;
            int num = 0;
            foreach (Dictionary<string, string> item in Result)
            {
                Label l = new Label();
                l.Style = (Style)this.Resources["ListLabel"];
                l.Content = item["Author"] + " - " + item["Name"];
                l.Name = "Tlabel" + i;
                
                    l.Foreground = (Brush)this.Resources["completeBrush"];                
                i++;
                try
                {
                    string p = item["Path"];
                    FileStream file1 = new FileStream(p, FileMode.Open);
                    StreamReader reader = new StreamReader(file1);
                    while (!reader.EndOfStream)
                    {
                        string it = reader.ReadLine();
                        int ind = it.IndexOf(word);
                        if (ind >= 0)
                        {
                            num++;
                            l.Content += "\n\"" + it+"\"";
                            ListOfTranslations.Items.Add(l);
                        }
                            
                    }
                    reader.Close();
                }
                catch (Exception) { };
            }
            amount.Content = "Файлов: " + num;
        }
        
        /// <summary>
        /// Включение даты в фильтр
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            includeDate = true;
        }

        /// <summary>
        /// Включение даты в фильтр
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DatePicker_SelectedDateChanged_1(object sender, SelectionChangedEventArgs e)
        {
            includeDate = true;
        }

        /// <summary>
        /// Изменение текста на Checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void change_Click(object sender, RoutedEventArgs e)
        {
            if (complete.IsChecked == null) complete.Content = "Неважно"; else 
                if (complete.IsChecked == true) complete.Content = "Завершен"; else
                    if (complete.IsChecked == false) complete.Content = "Не завершен";
        }

        /// <summary>
        /// Включение языка перевода в фильтр
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LanguagesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            includeLang = true;
        }

        /// <summary>
        /// Сброс фильтров в начальное состояние
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetFilters(object sender, RoutedEventArgs e)
        {
            includeLang = false;
            includeDate = false;
            LanguagesList.SelectedItems.Clear();
            complete.IsChecked = null;
            searchDB.Text = "";           
            DateFrom.SelectedDate = null;
            DateTo.SelectedDate = null;
            LoadAll();
        }

        /// <summary>
        /// Применение фильтров и поиск по БД
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplyFilters(object sender, RoutedEventArgs e)
        {
            string word = searchDB.Text;
            string dateFrom = "null";
            string dateTo = "null";
            if (includeDate)
            {
                DateTime dt = new DateTime();
                dt = (DateTime)DateFrom.SelectedDate;
                dateFrom = dt.Year + "-" + dt.Month + "-" + dt.Day;
                dt = (DateTime)DateTo.SelectedDate;
                dateTo = dt.Year + "-" + dt.Month + "-" + dt.Day;
            }
            bool? compl = complete.IsChecked;
            List<string> langs = new List<string>();
            if (includeLang) {
                for (int i=0; i<LanguagesList.SelectedItems.Count; i++)
                    langs.Add(LanguagesList.SelectedItems[i].ToString());
            }
            if ((bool)searchInText.IsChecked)
            { Result = DBLibrary.TranslatorDB.CompleteSearch("", dateFrom, dateTo, compl, langs, sort); SearchInText(word);
            Result.Reverse();
            }
            else
            {
                Result = DBLibrary.TranslatorDB.CompleteSearch(word, dateFrom, dateTo, compl, langs, sort);
                Result.Reverse();
                ShowList();
            }
           
        }

        /// <summary>
        /// Скрытие Placeholdera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>      
        private void searchDB_GotFocus(object sender, RoutedEventArgs e)
        {
            if (notSearched) { searchDB.Text = ""; notSearched = false; }
        }

        /// <summary>
        /// Потеря фокуса строкой перевода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_LostFocus(object sender, RoutedEventArgs e) 
        {
            TextBox t = (TextBox)sender;
            t.Style = (Style)this.Resources["flatTextBox"];
            previewFocus = -1;
        }

        /// <summary>
        /// Нажатие на строку оригинала
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_EnMouseDown(object sender, MouseButtonEventArgs e) 
        {
            TextBox t = (TextBox)sender; //получаем элемент
            string str = t.Name;
            string strsub = str.Substring(3);
            int id = Convert.ToInt32(strsub);
            int index = Convert.ToInt32(strsub);
            if (previewFocus == index) //если он уже выделен, разрешаем редактирование
            {
                t.Style = (Style)this.Resources["simpleTextBox"];
                t.Focus();
            }
            else
            {
                if (previewFocus >= 0) //иначе меняем выделение
                {
                    TextBox tb = (TextBox)EnBox.Items[previewFocus];
                    tb.Style = (Style)this.Resources["flatTextBox"];
                }
                EnBox.SelectedIndex = Convert.ToInt32(index);
                previewFocus = index;
            }
        }

        /// <summary>
        /// Нажатие на строку перевода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_RuMouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBox t = (TextBox)sender;
            string str = t.Name;
            string strsub = str.Substring(3);

            int index = Convert.ToInt32(strsub);
            if (previewFocus == index)
            {
                t.Style = (Style)this.Resources["simpleTextBox"];
                t.Focus();
            }
            else
            {
                if (previewFocus >= 0)
                {
                    TextBox tb = (TextBox)RuBox.Items[previewFocus];
                    tb.Style = (Style)this.Resources["flatTextBox"];
                }
                RuBox.SelectedIndex = Convert.ToInt32(index);
                previewFocus = index;
            }

        }

        /// <summary>
        /// Обратная смена подсвеченной строки для контейнера перевода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!selectingmode && e.AddedItems.Count > 0)
            {
                TextBox tb = (TextBox)e.AddedItems[0];
                string str = tb.Name;
                string strsub = str.Substring(3);
                int index = Convert.ToInt32(strsub);
                RuBox.SelectedIndex = index;
            }
        }

        /// <summary>
        /// Обратная смена подсвеченной строки для контейнера перевода и задание параметров выделения,
        /// если текст русский
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RuBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!selectingmode && e.AddedItems.Count > 0)
            {
                TextBox tb = (TextBox)e.AddedItems[0];
                string str = tb.Name;
                string strsub = str.Substring(3);
                int index = Convert.ToInt32(strsub);
                sliderselection.SelectionEnd = index;
                sliderselection.SelectionStart = index;
                sliderselection.Value = index;
                EnBox.SelectedIndex = index;
                if (comments.ContainsKey(index))
                    commentForm.Text = comments[index];
                else commentForm.Clear();
            }
        }

        /// <summary>
        /// Переключение между строками оригинала и обработка перевода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_EnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down || e.Key == Key.Up)
            {
                TextBox t = (TextBox)sender;
                t.Style = (Style)this.Resources["flatTextBox"];
                string str = t.Name;
                string strsub = str.Substring(3);
                int index = Convert.ToInt32(strsub);
                if (e.Key == Key.Up)
                { 
                    index--;
                    if (index < 0) 
                        index = EnBox.Items.Count - 1; 
                }
                else
                    index++;
                previewFocus = index;
                if (EnBox.Items.Count == previewFocus)
                    previewFocus = 0;

                EnBox.SelectedIndex = previewFocus;
                TextBox tb = (TextBox)EnBox.Items[previewFocus];
                tb.Style = (Style)this.Resources["simpleTextBox"];
                tb.Focus();
            }

            if (e.Key.ToString() == Properties.Settings.Default.keyCode) 
            {
                TextBox t = (TextBox)sender;
                string word = t.SelectedText;
                if (word.Length > 0)
                {
                    string page = InternetWorker.GetWord(t.SelectedText);
                    page = "<!DOCTYPE html ><html><meta http-equiv='Content-Type' content='text/html;charset=UTF-8'><head></head><body>" + page + "</body></html>";
                    multitran.NavigateToString(page);
                }
            }
        }

        /// <summary>
        /// Переключение между строками перевода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_RuPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down || e.Key == Key.Up)
            {
                TextBox t = (TextBox)sender;
                t.Style = (Style)this.Resources["flatTextBox"];
                string str = t.Name;
                string strsub = str.Substring(3);
                int index = Convert.ToInt32(strsub);
                if (e.Key == Key.Up)
                { 
                    index--;
                    if (index < 0) 
                        index = RuBox.Items.Count - 1; 
                }
                else
                    index++;
                previewFocus = index;
                if (RuBox.Items.Count == previewFocus)
                    previewFocus = 0;
                RuBox.SelectedIndex = previewFocus;
                TextBox tb = (TextBox)RuBox.Items[previewFocus];
                tb.Style = (Style)this.Resources["simpleTextBox"];
                tb.Focus();
            }
        }

        /// <summary>
        /// Открытие строки перевода для редактирования
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RuBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox t = (TextBox)RuBox.SelectedItem;
                t.Style = (Style)this.Resources["simpleTextBox"];
                t.Focus();
            }
        }

        /// <summary>
        /// Открытие строки оригинала для редактирования
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox t = (TextBox)EnBox.SelectedItem;
                t.Style = (Style)this.Resources["simpleTextBox"];
                t.Focus();
            }
        }

        /// <summary>
        /// Процесс выделения строк
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sliderselection_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (selectingmode)
                    if ((int)sliderselection.Value < RuBox.Items.Count && (int)sliderselection.Value >= sliderselection.SelectionStart)
                    {
                        if (prev_selection != -1)
                            if ((int)sliderselection.Value < prev_selection)
                                RuBox.SelectedItems.RemoveAt(prev_selection - (int)sliderselection.SelectionStart);
                            else
                                RuBox.SelectedItems.Add(RuBox.Items[(int)sliderselection.Value]);
                        sliderselection.SelectionEnd = sliderselection.Value;
                        prev_selection = (int)sliderselection.Value;
                    }
            }
            catch { }
        }

        /// <summary>
        /// Поиск перевода слова
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void S_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string word = searchMultitran.Text;
                if (word.Length > 0)
                {
                    string page = InternetWorker.GetWord(word);
                    page = "<!DOCTYPE html ><html><meta http-equiv='Content-Type' content='text/html;charset=UTF-8'><head></head><body>" + page + "</body></html>";
                    multitran.NavigateToString(page);
                }

            }
        }

        /// <summary>
        /// СКрытие Placeholdera для строки поиска перевода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void searchM_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!Mfocused)
            {
                searchMultitran.Text = "";
                Mfocused = true;
            }
        }

        /// <summary>
        /// Начало или окончание выделения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BeginSelect(object sender, RoutedEventArgs e)
        {
            selectingmode = !selectingmode;
            if (selectingmode)
                sliderselection.Visibility = Visibility.Visible;
            else
            {
                sliderselection.Visibility = Visibility.Hidden;
                prev_selection = -1; 
            }
        }

        /// <summary>
        /// Копировать строки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyItems(object sender, RoutedEventArgs e)
        {
            copyList = new List<string>();
            foreach (object item in RuBox.SelectedItems)
            {
                TextBox tItem = (TextBox)item;
                copyList.Add(tItem.Text);
            }
        }

        /// <summary>
        /// Вставить строки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PasteItems(object sender, RoutedEventArgs e)
        {
            if (copyList != null && RuBox.SelectedItems.Count > 0)
            {
                for (int i = RuBox.SelectedIndex; i < copyList.Count + RuBox.SelectedIndex; i++)
                {
                    if (i < RuBox.Items.Count)
                    {
                        TextBox temp = (TextBox)(RuBox.Items[i]);
                        temp.Text = copyList[i - RuBox.SelectedIndex];
                    }
                }
            }
        }

        /// <summary>
        /// Удаление строки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteItem(object sender, RoutedEventArgs e)
        {
            int id = RuBox.SelectedIndex;
            RuBox.Items.RemoveAt(RuBox.SelectedIndex);
            EnBox.Items.RemoveAt(EnBox.SelectedIndex);
            if (id == RuBox.Items.Count - 1) id--;
            RuBox.SelectedIndex = id;
            EnBox.SelectedIndex = id;
        }

        /// <summary>
        /// Добавление комента
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void commentForm_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string com = commentForm.Text;
                int commentIndex = RuBox.SelectedIndex;
                if (comments.ContainsKey(commentIndex))
                    comments[commentIndex] = com;
                else
                    comments.Add(commentIndex, com);
                RuBox.Focus();
            }
        }
        
        /// <summary>
        /// Переход к предпросмотру и сохранению файла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviewFile(object sender, RoutedEventArgs e)
        {
            Translation.Visibility = Visibility.Hidden;
            if (Properties.Settings.Default.AutoSave == true)
            {
                autosave.Stop();
            }
            string ru = "";
            for (int i = 0; i < RuBox.Items.Count; i++)
            {
                TextBox tb = (TextBox)RuBox.Items[i];
                ru += tb.Text + '\n';
            }
            string en = "";
            for (int i = 0; i < EnBox.Items.Count; i++)
            {
                TextBox tb = (TextBox)EnBox.Items[i];
                en += tb.Text + '\n';
            }       
            Originaltext.AppendText(en);
            Translatedtext.AppendText(ru);
            textRu = ru;
            textEn = en;
            if (!nodb)
            {
                List<string> Languages = DBLibrary.TranslatorDB.GetLang();
                LanguagesCombo.Items.Clear();
                foreach (string item in Languages)                
                    LanguagesCombo.Items.Add(item);                
                LanguagesCombo.SelectedIndex = 0;
            }
            if (edit)
            {
                authorTip = true;
                nameTip = true;
                author.Text = authorName;
                textname.Text = textName;
            }
            Preview.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Нажатие кнопки Домой
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GoHomeButton(object sender, RoutedEventArgs e)
        {
            ISearch.Visibility = Visibility.Hidden;
            Preview.Visibility = Visibility.Hidden;
            ListTranslate.Visibility = Visibility.Visible;
            Import.Visibility = Visibility.Hidden;
            Translation.Visibility = Visibility.Hidden;
            if (Properties.Settings.Default.AutoSave==true)
                autosave.Stop();
            RuBox.Items.Clear();
            EnBox.Items.Clear();
            Originaltext.Document.Blocks.Clear();
            Translatedtext.Document.Blocks.Clear();
            richTextBox1.Document.Blocks.Clear();
        }
       
        /// <summary>
        /// Сохранение файла в БД
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Save(object sender, RoutedEventArgs e) //
        {
            if (nodb)
                MessageBox.Show("Вы не подключили Базу Данных. Сохранение невозможно - поробуйте экспорт.");
            else
            {
                Microsoft.Win32.SaveFileDialog myDialog = new Microsoft.Win32.SaveFileDialog();
                myDialog.FileName = author.Text + " - " + textname.Text;
                myDialog.DefaultExt = ".txt";
                bool result = (bool)myDialog.ShowDialog();
                if (result)
                {
                    string path = "";
                    path = myDialog.FileName;
                    if (!TextWorker.CheckStrings(author.Text, textname.Text))
                        MessageBox.Show("Сохранение невозвожно. Проверьте, чтобы в названии файла не было запрещенных знаков, таких как \', \", \\ и др.");
                    else
                    {
                        List<string> text = new List<string>();
                        text.Add(author.Text + " - " + textname.Text);
                        text.Add(textnametr.Text);
                        text.Add("#");
                        string Str = new TextRange(Originaltext.Document.ContentStart, Originaltext.Document.ContentEnd).Text;
                        List<string> enItems;
                        enItems = TextWorker.Create(Str);
                        for (int i = 0; i < enItems.Count; i++)
                            text.Add(enItems[i]);
                        text.Add("#");
                        Str = new TextRange(Translatedtext.Document.ContentStart, Translatedtext.Document.ContentEnd).Text;
                        List<string> ruItems;
                        ruItems = TextWorker.Create(Str);
                        for (int i = 0; i < ruItems.Count; i++)
                        {
                            if (comments.ContainsKey(i))
                                text.Add(ruItems[i] + '*');
                            else text.Add(ruItems[i]);
                        }
                        text.Add("#");
                        text.Add((bool)splitter.IsChecked == true ? "1" : "0");
                        for (int i = 0; i < comments.Count; i++)   
                            text.Add("* " + comments.Values.ElementAt(i));           
                        System.IO.File.WriteAllLines(path, text);
                        if (!update)
                            DBLibrary.TranslatorDB.SaveFile(author.Text, textname.Text, (string)LanguagesCombo.SelectedItem, Convert.ToInt32(completeIs.IsChecked), path);
                        else
                            DBLibrary.TranslatorDB.UpdateFile(Result[selectedid]["ID"], author.Text, textname.Text, (string)LanguagesCombo.SelectedItem, Convert.ToInt32(completeIs.IsChecked), path);
                        MessageBox.Show("Готово!", "Файл записан!");
                        edit = false;
                        update = false;
                        Originaltext.Document.Blocks.Clear();
                        Translatedtext.Document.Blocks.Clear();                        
                        LoadAll();
                        Preview.Visibility = Visibility.Hidden;
                        ListTranslate.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        /// <summary>
        /// Экспорт файла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Export(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog myDialog = new Microsoft.Win32.SaveFileDialog();
            myDialog.FileName = author.Text + " - " + textname.Text;
            myDialog.DefaultExt = ".txt";
            bool result = (bool)myDialog.ShowDialog();
            string path = "";
            List<string> text = new List<string>();
            text.Add(author.Text + " - " + textname.Text);
            text.Add(textnametr.Text);
            string Str = new TextRange(Translatedtext.Document.ContentStart, Translatedtext.Document.ContentEnd).Text;
            List<string> ruItems;
            ruItems = TextWorker.Create(Str);
            if ((bool)splitter.IsChecked)
            {
                string paragraph = "";
                int commentIndex = 1;
                for (int i = 0; i < ruItems.Count; i++)
                {
                    if (ruItems[i].Length == 0)
                    {
                        text.Add(paragraph);
                        paragraph = "";
                    }
                    else
                    {
                        if (comments.ContainsKey(i))
                        {
                            paragraph += ruItems[i] + " (" + commentIndex + ")";
                            commentIndex++;
                        }
                        else paragraph += ruItems[i];
                    }
                }
                if (paragraph.Length > 0)
                    text.Add(paragraph);
            }
            else
            {
                int commentIndex = 1;
                for (int i = 0; i < ruItems.Count; i++)
                {

                    if (comments.ContainsKey(i))
                    {
                        text.Add(ruItems[i] + " (" + commentIndex + ")");
                        commentIndex++;
                    }
                    else text.Add(ruItems[i]);
                }
            }
            for (int i = 1; i <= comments.Count; i++)            
                text.Add(i + " - " + comments.Values.ElementAt(i - 1));            
            if (result)
            {
                path = myDialog.FileName;
                System.IO.File.WriteAllLines(path, text);
                MessageBox.Show("Готово!", "Файл экспортирован!");
            }
        }
        
        /// <summary>
        /// Скрытие PLaceholdera Автор
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void author_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!authorTip)
            {
                authorTip = true;
                author.Clear();
            }
        }

        /// <summary>
        /// Скрытие PLaceholdera Название
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textname_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!nameTip)
            {
                nameTip = true;
                textname.Clear();
            }
        }

        /// <summary>
        /// Скрытие PLaceholdera перевод названия
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textnametr_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!nametrTip)
            {
                nametrTip = true;
                textnametr.Clear();
            }
        }
                  
        /// <summary>
        /// Окно настроек
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            winTool= new SettingsWindow(ref this.Tip);
            winTool.Owner = this;            
            winTool.Show();
            winTool.AddL.IsEnabled = !nodb;
        }

        /// <summary>
        ////Отображение подсказки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TipMouseEnter(object sender, MouseEventArgs e)
        {
            if (Properties.Settings.Default.InclTips)
            {
                Control but = (Control)sender;
                string Name = but.Name;
                Tip.Content = Properties.Resources.ResourceManager.GetString(Name);
            }
        }

        /// <summary>
        /// Скрытие подсказки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TipMouseLeave(object sender, MouseEventArgs e)
        {
            Tip.Content = "Здесь будут отображаться подсказки. Просто наведите мышью на элемент.";
        }

        /// <summary>
        /// Сортировка по автору
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)sortauthor.IsChecked)
            {
                sortname.IsChecked = false;
                sortdata.IsChecked = false;
                if ((bool)sortauthor.IsChecked) sort = 1;
                if ((bool)sortname.IsChecked) sort = 2;
                if ((bool)sortdata.IsChecked) sort = 3;
            }
            else sort = 0;
            LoadAll();
        }

        /// <summary>
        /// Сортировка по названию
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sortname_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)sortname.IsChecked)
            {
                sortauthor.IsChecked = false;
                sortdata.IsChecked = false;
                if ((bool)sortauthor.IsChecked) sort = 1;
                if ((bool)sortname.IsChecked) sort = 2;
                if ((bool)sortdata.IsChecked) sort = 3;
            }
            else sort = 0;
            LoadAll();
        }

        /// <summary>
        /// Сортировка по дате
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sortdata_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)sortdata.IsChecked)
            {
                sortname.IsChecked = false;
                sortauthor.IsChecked = false;
                if ((bool)sortauthor.IsChecked) sort = 1;
                if ((bool)sortname.IsChecked) sort = 2;
                if ((bool)sortdata.IsChecked) sort = 3;
            }
            else sort = 0;
            LoadAll();
        }


        private void ReverseArray(object sender, RoutedEventArgs e)
        {
       
            if (reverse.Content.ToString().CompareTo("˅")==0)
            {
                reverse.Content = "˄";               
            }
            else
            {
                reverse.Content = "˅";    
                reversed = false;              
            }
            if (Result != null)
                Result.Reverse();
            ShowList();
               
        }    

      
    }
}
