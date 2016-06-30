using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;


namespace TranslatorTool
{
    /// <summary>
    /// Класс, содержащий методы для работы с интернетом - получение необходимой информации,
    /// поиск по интернету. Помогает получить страницу с результатом поиска на некоторых серверах.
    /// </summary>
    class InternetWorker
    {
        /// <summary>
        /// Поиск слова в интернете на сайте musixmatch.com
        /// Функция осуществляет поисковый запрос и загружает страницу, а затем отправляет результат на парсинг
        /// </summary>
        /// <param name="word">Искомое слово</param>
        /// <returns>Словарь с парой ключ-значение, где ключ - название, значение - путь к файлу</returns>
        public static Dictionary<string,string> Search(string word) {                  
            WebClient client = new WebClient();
            client.Encoding = Encoding.UTF8;
            string userAgentString = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Win64; x64; Trident/4.0; Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1) ; .NET CLR 2.0.50727; SLCC2; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; Tablet PC 2.0; .NET4.0C; .NET4.0E)";
            client.Headers.Add("user-agent", userAgentString);
            string connectionString = "https://www.musixmatch.com/search/"+word;
            string result = "";
            Stream stream = client.OpenRead(connectionString);
            StreamReader sr = new StreamReader(stream, Encoding.UTF8);
            string newLine;            
            while ((newLine = sr.ReadLine()) != null)                           
                result += newLine;            
            stream.Close();       
            return TextWorker.ParseSearch(result);            
        }
        
        /// <summary>
        /// Получение текста по ссылке. Парсинг результата
        /// </summary>
        /// <param name="path">Путь к файлу в интернете</param>
        /// <returns>Список из строк, на которые разбит текст</returns>
        public static List<string> GetText(string path) {
            List<string> returned = new List<string>();
            WebClient client = new WebClient();
            client.Encoding = Encoding.UTF8;
            string userAgentString = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Win64; x64; Trident/4.0; Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1) ; .NET CLR 2.0.50727; SLCC2; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; Tablet PC 2.0; .NET4.0C; .NET4.0E)";
            client.Headers.Add("user-agent", userAgentString);
            string connectionString = path;
            string result = "";          
            Stream stream = client.OpenRead(connectionString);
            StreamReader sr = new StreamReader(stream, Encoding.UTF8);
            string newLine;
            bool textb = false;
            while ((newLine = sr.ReadLine()) != null)
            {               
                int index2 = newLine.IndexOf("mxm-lyrics__content");
                int index1 = newLine.IndexOf("</p>");
                if (textb && index1 != -1)
                {
                    newLine = newLine.Substring(0, index1);
                    returned.Add(newLine);
                    textb = false;
                }
                if (textb)
                    returned.Add(newLine);
                if (index2 != -1)
                {
                    textb = true;
                    newLine = newLine.Substring(index2);
                    int index4 = newLine.IndexOf('>');
                    newLine = newLine.Substring(index4 + 1);
                    returned.Add(newLine);
                }
                result += newLine;
            }
            stream.Close();
            return returned;
        }

        /// <summary>
        /// Получить перевод слова. Функция обращается к серверу multitran и парсит полученную страницу
        /// </summary>
        /// <param name="word">Искомое слово</param>
        /// <returns>Часть html страницы с переводом</returns>
        public static string GetWord(string word)
        {
            try
            {
                WebClient client = new WebClient();
                client.Encoding = Encoding.GetEncoding("windows-1251");
                string userAgentString = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Win64; x64; Trident/4.0; Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1) ; .NET CLR 2.0.50727; SLCC2; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; Tablet PC 2.0; .NET4.0C; .NET4.0E)";
                client.Headers.Add("user-agent", userAgentString);
                string connectionString = "http://www.multitran.ru/c/m.exe?CL=1&s=" + word + "&l1=1";
                string result = "";
                Stream stream = client.OpenRead(connectionString);
                StreamReader sr = new StreamReader(stream, Encoding.GetEncoding("windows-1251"));
                string newLine;
                while ((newLine = sr.ReadLine()) != null)
                    result += newLine;
                stream.Close();
                return TextWorker.ParseTranslated(result);
            }
            catch (Exception) { return "Проблемы с интернетом"; }
        }        
    }
}
