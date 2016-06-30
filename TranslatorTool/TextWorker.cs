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
using DBLibrary;
using System.Windows.Media.Animation;


namespace TranslatorTool
{
    /// <summary>
    /// Класс, содержащий необходимые методы для любой работы с текстом и
    /// текстовыми массивами. Сюда входит парсинг html-страниц, разбивка
    /// текстов на массивы строк и проверки строк
    /// </summary>
    class TextWorker
    {
        /// <summary>
        /// Проверка названия файла на запрещенные символы, которые могут привести к ошибке пр изаписи в БД
        /// </summary>
        /// <param name="author">Автор текста</param>
        /// <param name="name">Название текста</param>
        /// <returns>True - провекра пройдена, False - не прйдена</returns>
        public static bool CheckStrings(string author, string name)
        {
            if (author.IndexOf('\'') >= 0 || author.IndexOf('\"') >= 0 || author.IndexOf('\\') >= 0 || name.IndexOf('\'') >= 0 || name.IndexOf('\"') >= 0 || name.IndexOf('\\') >= 0)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Получить из текста массив строк (без дополнительной разбивки)
        /// </summary>
        /// <param name="text">Текст</param>
        /// <returns>Список из строк</returns>
        public static List<string> Create(string text)
        {
            List<string> newList = new List<string>();
            int i = 0;
            string line = "";
            while (i < text.Length)
            {
                if ((text[i]) == '\r' || text[i] == '\n')
                {
                    newList.Add(line);
                    line = "";
                    i++;
                }
                else
                    line += text[i];
                i++;
            }
            if (line.Length != 0)
                newList.Add(line);
            return newList;
        }

        /// <summary>
        /// Разбить текст на строки
        /// </summary>
        /// <param name="text">Текст</param>
        /// <returns>Список строк</returns>
        public static List<string> Split(string text)
        {
            List<string> newList = new List<string>();
            bool outer_cycle = true;
            while (outer_cycle)
            {
                int index = text.IndexOf('\r');
                if (index == -1)
                    index = text.IndexOf('\n');
                if (index == -1)
                    outer_cycle = false;
                else
                {
                    string paragraph = text.Substring(0, index + 1);
                    text = text.Substring(index + 1);
                    bool inner_cycle = true;
                    while (inner_cycle)
                    {
                        int index2 = paragraph.IndexOf('.');
                        if (index2 == -1)
                        {
                            index2 = paragraph.IndexOf(',');
                            if (index2 == -1)
                            {
                                index2 = paragraph.IndexOf('!');
                                if (index2 == -1)
                                {
                                    index2 = paragraph.IndexOf('?');
                                    if (index2 == -1)
                                    {
                                        index2 = paragraph.IndexOf(';');
                                        if (index2 == -1)
                                        {
                                            index2 = paragraph.IndexOf(':');
                                            if (index2 == -1)
                                            {
                                                index2 = paragraph.IndexOf('(');
                                                if (index2 == -1)
                                                {
                                                    index2 = paragraph.IndexOf(')');
                                                    if (index2 == -1)
                                                        inner_cycle = false;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        string cut_sent = "";
                        if (inner_cycle)
                        {
                            string sentence = paragraph.Substring(0, index2 + 1);
                            paragraph = paragraph.Substring(index2 + 1);
                            if (sentence.Length < 50)
                                newList.Add(sentence);
                            else
                            {
                                bool inin_cycle = true;
                                while (inin_cycle)
                                {
                                    int index3 = sentence.IndexOf(' ');
                                    if (index3 == -1)
                                        inin_cycle = false;
                                    else
                                    {
                                        cut_sent += sentence.Substring(0, index3 + 1);
                                        if (cut_sent.Length > 50)
                                        {
                                            newList.Add(cut_sent);
                                            cut_sent = "";
                                        }
                                        sentence = sentence.Substring(index3 + 1);
                                    }
                                }
                                if (cut_sent.Length > 0 || sentence.Length>0)
                                    newList.Add(cut_sent + sentence);
                                 
                            }
                        }
                    }
                    if (paragraph.Length > 0)
                        newList.Add(paragraph);
                    newList.Add("");
                }
            }
            return newList;
        }

        /// <summary>
        /// Парсинг страницы перевода слова, возвращающий лишь ту ее часть, которая необходима
        /// </summary>
        /// <param name="text">Страница в html</param>
        /// <returns>Нужная часть страницы</returns>
        public static string ParseTranslated(string text)
        {
            int index = text.IndexOf("createAutoComplete();");
            if (index > 0)
            {
                index += 30;
                string initialText = text.Remove(0, index);
                int index2 = initialText.IndexOf("</table>");
                if (index2 > 0)
                {
                    string fragment = initialText.Remove(index2 + 8);
                    return fragment;
                }
            }
            return null;
        }

        /// <summary>
        /// Парсинг результатов поиска по текстам (разбивка на список)
        /// </summary>
        /// <param name="result">Страница в html</param>
        /// <returns>Результат</returns>
        public static Dictionary<string, string> ParseSearch(string result)
        {
            Dictionary<string, string> searched = new Dictionary<string, string>();
            int index = result.IndexOf("Tracks");
            if (index != -1)
            {
                result = result.Substring(index);
                index = result.IndexOf("box-content");
                if (index != -1)
                {
                    result = result.Substring(index);
                    string name, artist, path;
                    string[] mas = new string[1];
                    mas[0] = "class=\"title";
                    string[] results = result.Split(mas, System.StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 1; i < results.Length; i++)
                    {
                        result = results[i];
                        index = result.IndexOf("href");
                        result = result.Substring(index);
                        int index4 = result.IndexOf("data");
                        path = result.Substring(6, index4 - 8);
                        index = result.IndexOf("span");
                        result = result.Substring(index);
                        index = result.IndexOf('>');
                        result = result.Substring(index + 1);
                        int index2 = result.IndexOf("</span>");
                        name = result.Substring(0, index2);
                        index = result.IndexOf("class=\"artist\"");
                        result = result.Substring(index);
                        index = result.IndexOf('>');
                        result = result.Substring(index + 1);
                        index2 = result.IndexOf("</a>");
                        artist = result.Substring(0, index2);
                        try
                        {
                            searched.Add(artist + " - " + name, "https://www.musixmatch.com" + path);
                        }
                        catch
                        { }
                    }
                }
            }
            return searched;
        }
    }
}
