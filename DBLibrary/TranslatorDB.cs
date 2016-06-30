using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.Common;
using System.Configuration;

namespace DBLibrary
{
    /// <summary>
    /// Класс для работы с Базой Данных (выполнение запросов)
    /// </summary>
    public class TranslatorDB
    {
        static DbProviderFactory df;
        static string cnStr;

        /// <summary>
        /// Соединение с БД. Первая проверка возможности соединения и генерация ошибки, если необходимо
        /// </summary>
        /// <returns>True - соединение прошло успешно</returns>
        public static bool Connect()
        {
            try
            {
                DbProviderFactory sqlFactory = DbProviderFactories.GetFactory("System.Data.SqlClient");
                string dp = ConfigurationManager.AppSettings["provider"];
                cnStr = ConfigurationManager.AppSettings["cnStr"];
                df = DbProviderFactories.GetFactory(dp);
            }
            catch (Exception) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Добавить в БД новый язык
        /// </summary>
        /// <param name="lang">Название языка</param>
        /// <returns>True - операция прошла успешно</returns>
        public static bool AddLang(string lang)
        {
            try {
            using (DbConnection cn = df.CreateConnection())
            {
                cn.ConnectionString = cnStr;
                cn.Open();
                DbCommand cmd = df.CreateCommand();
                cmd = df.CreateCommand();
                cmd.Connection = cn;            
                cmd.CommandText = @"INSERT INTO Language VALUES('"+lang+"')";
                cmd.ExecuteScalar();
            } 
            }
            catch { return false; }
            return true;
        }
        
        /// <summary>
        /// Сохранение файла в БД
        /// </summary>
        /// <param name="Author">Автор</param>
        /// <param name="Name">Название</param>
        /// <param name="Lang">Язык</param>
        /// <param name="complete">Завершен ли</param>
        /// <param name="path">Путь к файлу</param>
        public static void SaveFile(string Author, string Name, string Lang, int complete, string path)
        {
            using (DbConnection cn = df.CreateConnection())
            {
                cn.ConnectionString = cnStr;
                cn.Open();
                DbCommand cmd = df.CreateCommand();
                cmd.Connection = cn;
                cmd.CommandText = @"Select LanguageID FROM Language WHERE Name = '" + Lang + "'";
                int id = 0;
                using (DbDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        id = Convert.ToInt32(dr["LanguageID"]);
                }
                cmd = df.CreateCommand();
                cmd.Connection = cn;
                string date = "";
                date = DateTime.Now.Year.ToString() + '-' + DateTime.Now.Month.ToString() + '-' + DateTime.Now.Day.ToString();
                cmd.CommandText = @"INSERT INTO Translation VALUES('" + Author + "','" + Name + "','" + date + "'," + id + ',' + complete + ",'" + path + "')";
                cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Удаление файла из БД
        /// </summary>
        /// <param name="ID">ID файла</param>
        public static void Delete(string ID) {
            using (DbConnection cn = df.CreateConnection())
            {
                cn.ConnectionString = cnStr;
                cn.Open();
                DbCommand cmd = df.CreateCommand();
                cmd.Connection = cn;
                cmd.CommandText = @"DELETE FROM Translation WHERE ID = "+ID;
                cmd.ExecuteScalar();
            }
        }
       
        /// <summary>
        /// Редактирование файла, уже существующего в БД
        /// </summary>
        /// <param name="ID">ID</param>
        /// <param name="Author">Автор</param>
        /// <param name="Name">Название</param>
        /// <param name="Lang">Язык перевода</param>
        /// <param name="complete">Завершен ли</param>
        /// <param name="path">Путь к файлу</param>
        public static void UpdateFile(string ID,string Author, string Name, string Lang, int complete, string path)
        {
            using (DbConnection cn = df.CreateConnection())
            {
                cn.ConnectionString = cnStr;
                cn.Open();
                DbCommand cmd = df.CreateCommand();
                cmd.Connection = cn;
                cmd.CommandText = @"Select LanguageID FROM Language WHERE Name = '" + Lang + "'";
                int id = 0;                
                using (DbDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                     id = Convert.ToInt32(dr["LanguageID"]);
                }
                cmd = df.CreateCommand();
                cmd.Connection = cn;
                string date = "";
                date = DateTime.Now.Year.ToString() + '-' + DateTime.Now.Month.ToString() + '-' + DateTime.Now.Day.ToString();
                cmd.CommandText = @"UPDATE Translation SET Author='" + Author + "',Name='" + Name + "',Date='" + date + "',LanguageID=" + id + ",Complete=" + complete + ",Path='" + path + "'\nWHERE ID="+ID;
                cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Получение всех файлов из БД
        /// </summary>
        /// <returns>Список</returns>
        public static List<Dictionary<string, string>> SelectAll(int sort)
        {
            try
            {
                List<Dictionary<string, string>> Collection = new List<Dictionary<string, string>>();
                using (DbConnection cn = df.CreateConnection())
                {
                    cn.ConnectionString = cnStr;
                    cn.Open();
                    DbCommand cmd = df.CreateCommand();
                    cmd.Connection = cn;
                    string order = "";
                    if (sort == 1)
                        order = " ORDER BY T.Author";
                    else if (sort == 2)
                        order = " ORDER BY T.Name";
                    else if (sort == 3)
                        order = " ORDER BY T.Date";
                    cmd.CommandText = @"Select T.ID, T.Name, T.Author, T.Complete, T.Date, T.Path, L.Name AS 'Language' FROM Translation T, Language L WHERE L.LanguageID = T.LanguageID" + order;
                    using (DbDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            Dictionary<string, string> element = new Dictionary<string, string>();
                            element.Add("ID", dr["ID"].ToString());
                            element.Add("Name", dr["Name"].ToString());
                            element.Add("Author", dr["Author"].ToString());
                            element.Add("Date", dr["Date"].ToString());
                            element.Add("Language", dr["Language"].ToString());
                            element.Add("Complete", dr["Complete"].ToString());
                            element.Add("Path", dr["Path"].ToString());
                            Collection.Add(element);
                        }
                    }
                    return Collection;
                }
            }
            catch { return null; }     
        }

        /// <summary>
        /// Получение языков, записанных в БД
        /// </summary>
        /// <returns>Список языков</returns>
        public static List<string> GetLang() {           
                List<string> Collection = new List<string>();
                using (DbConnection cn = df.CreateConnection())
                {
                    cn.ConnectionString = cnStr;
                    cn.Open();
                    DbCommand cmd = df.CreateCommand();
                    cmd.Connection = cn;
                    cmd.CommandText = @"Select Name FROM Language";                    
                    using (DbDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())                      
                            Collection.Add(dr["Name"].ToString());                     
                    }
                    return Collection;
                }         
        }

        /// <summary>
        /// Поиск по БД с применением фильтров
        /// </summary>
        /// <param name="word">Часть названия</param>
        /// <param name="dateFrom">Нижняя граница даты</param>
        /// <param name="dateTo">Верхняя граница даты</param>
        /// <param name="compl">Завершенность</param>
        /// <param name="langs">Список языков</param>
        /// <returns>Список</returns>
        public static List<Dictionary<string, string>> CompleteSearch(string word, string dateFrom, string dateTo, bool? compl, List<string> langs, int sort) {
            List<Dictionary<string, string>> Collection = new List<Dictionary<string, string>>();
            using (DbConnection cn = df.CreateConnection())
            {
                cn.ConnectionString = cnStr;
                cn.Open();
                DbCommand cmd = df.CreateCommand();
                cmd.Connection = cn;
                string order = "";
                if (sort > 0)
                    if (sort == 1)
                        order = " ORDER BY T.Author";
                    else if (sort == 2)
                        order = " ORDER BY T.Name";
                    else if (sort == 3)
                        order = " ORDER BY T.Date";
                cmd.CommandText = @"Select T.ID, T.Name, T.Author, T.Complete, T.Date, T.Path, L.Name AS 'Language' FROM Translation T, Language L WHERE L.LanguageID = T.LanguageID AND (T.Name like '%" + word + "%' OR T.Author like '%" + word + "%')";
                if (compl != null)
                {
                    cmd.CommandText += "AND T.Complete = "; 
                    cmd.CommandText += compl == true ? "1" : "0";
                }
                if (langs.Count != 0)
                {
                    cmd.CommandText += " AND (";
                    bool first = true;
                    foreach (string item in langs)
                    {
                        if (!first)
                        cmd.CommandText += " OR L.Name = '" + item+"'";
                        else cmd.CommandText += " L.Name = '" + item+"'";
                        first = false;
                    }
                    cmd.CommandText += ") ";
                }
                if (dateFrom != "null" && dateTo != "null")
                {
                    cmd.CommandText += " AND T.Date < '"+dateTo+"' AND T.Date > '"+dateFrom+"'";
                }
                cmd.CommandText += order;
                using (DbDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Dictionary<string, string> element = new Dictionary<string, string>();
                        element.Add("ID", dr["ID"].ToString());
                        element.Add("Name", dr["Name"].ToString());
                        element.Add("Author", dr["Author"].ToString());
                        element.Add("Date", dr["Date"].ToString());                       
                        element.Add("Language", dr["Language"].ToString());
                        element.Add("Complete", dr["Complete"].ToString());                       
                        element.Add("Path", dr["Path"].ToString());
                        Collection.Add(element);
                    }
                }
                return Collection;
            }
        }
    
        /// <summary>
        /// Сокращенный поиск (только по части названия)
        /// </summary>
        /// <param name="word">Часть названия</param>
        /// <returns>Список</returns>
        public static List<Dictionary<string, string>> Search(string word, int sort)
        {
            List<Dictionary<string, string>> Collection = new List<Dictionary<string, string>>();
            using (DbConnection cn = df.CreateConnection())
            {
                cn.ConnectionString = cnStr;
               
                    cn.Open();
                    DbCommand cmd = df.CreateCommand();
                    cmd.Connection = cn;
                    string order = "";
                    if (sort > 0)
                        if (sort == 1)
                            order = " ORDER BY T.Author";
                        else if (sort == 2)
                            order = " ORDER BY T.Name";
                        else if (sort == 3)
                            order = " ORDER BY T.Date";
                    cmd.CommandText = @"Select T.ID, T.Name, T.Author, T.Complete, T.Date, T.Path, L.Name AS 'Language' FROM Translation T, Language L WHERE L.LanguageID = T.LanguageID AND (T.Name like '%" + word + "%' OR T.Author like '%" + word + "%')" + order;
                    using (DbDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            Dictionary<string, string> element = new Dictionary<string, string>();
                            element.Add("ID", dr["ID"].ToString());
                            element.Add("Name", dr["Name"].ToString());
                            element.Add("Author", dr["Author"].ToString());
                            element.Add("Date", dr["Date"].ToString());
                            element.Add("Language", dr["Language"].ToString());
                            element.Add("Complete", dr["Complete"].ToString());
                            element.Add("Path", dr["Path"].ToString());
                            Collection.Add(element);
                        }
                    }
                    return Collection;
              
               
            }
        }
    }
}
