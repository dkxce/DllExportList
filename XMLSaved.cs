//
// C# (.Net Framework)
// dkxce.DllExportList
// v 0.1, 17.07.2023
// dkxce (https://github.com/dkxce/DllExportList)
// en,ru,1251,utf-8
//


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace MSol
{
    /// <summary>
    ///     KeyValuePair Serializable (XML, JSON, Binary)
    /// </summary>
    [Serializable]
    [XmlType(TypeName = "kvp")]
    public struct NameValue
    {
        /// <summary>
        ///     Parameter Name
        /// </summary>
        public string n { get; set; }
        /// <summary>
        ///     Parameter Value
        /// </summary>
        public string v { get; set; }

        public NameValue(string n, string v) { this.n = n; this.v = v; }

        public override string ToString()
        {
            return n + ": " + v;
        }
    }

    [Serializable]
    public class XMLSaved<T>
    {
        /// <summary>
        ///     Сохранение структуры в файл
        /// </summary>
        /// <param name="file">Полный путь к файлу</param>
        /// <param name="obj">Структура</param>
        public static void Save(string file, T obj)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces(); ns.Add("", "");
            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(T));
            System.IO.StreamWriter writer = System.IO.File.CreateText(file);
            xs.Serialize(writer, obj, ns);
            writer.Flush();
            writer.Close();
        }

        public static void SaveHere(string file, T obj)
        {
            Save(System.IO.Path.Combine(CurrentDirectory(), file), obj);
        }

        public static string Save(T obj)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces(); ns.Add("", "");
            System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(T));
            System.IO.MemoryStream ms = new MemoryStream();
            System.IO.StreamWriter writer = new StreamWriter(ms);
            xs.Serialize(writer, obj, ns);
            writer.Flush();
            ms.Position = 0;
            byte[] bb = new byte[ms.Length];
            ms.Read(bb, 0, bb.Length);
            writer.Close();
            return System.Text.Encoding.UTF8.GetString(bb); ;
        }

        /// <summary>
        ///     Подключение структуры из файла
        /// </summary>
        /// <param name="file">Полный путь к файлу</param>
        /// <returns>Структура</returns>
        public static T Load(string file)
        {
            try
            {
                System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer(typeof(T));
                System.IO.StreamReader reader = System.IO.File.OpenText(file);
                T c = (T)xs.Deserialize(reader);
                reader.Close();
                return c;
            }
            catch { };
            {
                Type type = typeof(T);
                System.Reflection.ConstructorInfo c = type.GetConstructor(new Type[0]);
                return (T)c.Invoke(null);
            };
        }

        public static T LoadHere(string file)
        {
            return Load(System.IO.Path.Combine(CurrentDirectory(), file));
        }

        public static T Load()
        {
            try { return Load(CurrentDirectory() + @"\config.xml"); }
            catch { };
            Type type = typeof(T);
            System.Reflection.ConstructorInfo c = type.GetConstructor(new Type[0]);
            return (T)c.Invoke(null);
        }

        public static string CurrentDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
            // return Application.StartupPath;
            // return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            // return System.IO.Directory.GetCurrentDirectory();
            // return Environment.CurrentDirectory;
            // return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            // return System.IO.Path.GetDirectory(Application.ExecutablePath);
        }
    }
}
