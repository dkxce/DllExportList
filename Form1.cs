//
// C# (.Net Framework)
// dkxce.DllExportList
// v 0.1, 17.07.2023
// dkxce (https://github.com/dkxce/DllExportList)
// en,ru,1251,utf-8
//

using MSol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace DllExportList
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                string fExt = Path.GetExtension(file).ToLower();
                if (fExt != ".dll") continue;
                LoadDll(file);
            };
        }

        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            try { System.Diagnostics.Process.Start("https://github.com/dkxce/DllExportList"); } catch { };
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
        }

        private void LoadDll(string fileName)
        {
            try
            {
                string dllName = Path.GetFileName(fileName);
                dkxce.DllExportList dll = dkxce.DllExportList.GetDllExportFunctions(fileName);
                foreach(dkxce.DllExportList.ExportFunction f in dll.Functions)
                {
                    DllLVI lvi = new DllLVI(fileName, dll, new string[] { dllName, dll.x86.ToString(), dll.x64.ToString(), dll.ModuleName, f.EntryPoint, f.Ordinal.ToString(), f.Name, fileName });
                    listView1.Items.Add(lvi);
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Dll Export List", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
        }

        public class DllLVI: ListViewItem
        {
            public dkxce.DllExportList dll;
            public string dllName;
            public DllLVI(string dllName, dkxce.DllExportList dll, string[] data): base(data) { this.dllName = dllName; this.dll = dll; }
        }

        private void saveToXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.Items.Count == 0) return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = System.Environment.GetCommandLineArgs()[0];
            sfd.DefaultExt = ".xml";
            sfd.Filter = "XML Files (*.xml)|*.xml|CSV Files (*.csv)|*.csv";
            if(sfd.ShowDialog() == DialogResult.OK) 
            {
                if (Path.GetExtension(sfd.FileName).ToLower() == ".xml")
                    Save(sfd.FileName, true);
                else
                    Save(sfd.FileName, false);
            }
            sfd.Dispose();
        }

        private void Save(string fileName, bool toXML)
        {
            if (listView1.Items.Count == 0) return;
            string prevName = "";
            if (toXML)
            {
                List<dkxce.DllExportList> exl = new List<dkxce.DllExportList>();
                for (int i = 0; i < listView1.Items.Count; i++)
                {
                    DllLVI lvi = (DllLVI)listView1.Items[i];
                    if (lvi.dllName == prevName) continue;
                    prevName = lvi.dllName;
                    exl.Add(lvi.dll);
                };
                if (exl.Count == 0) return;
                XMLSaved<dkxce.DllExportList[]>.Save(fileName, exl.ToArray());
            }
            else
            {
                string text = "# Dll Export List by dkxce\r\n# https://github.com/dkxce/DllExportList\r\n#\r\n";
                for (int x = 0; x < listView1.Columns.Count; x++)
                    text += $"{listView1.Columns[x].Text};";
                text += "\r\n";
                for (int i = 0; i < listView1.Items.Count; i++)
                {
                    for (int x = 0; x < listView1.Columns.Count; x++)
                        text += $"{listView1.Items[i].SubItems[x].Text};";
                    text += "\r\n";
                };
                File.WriteAllText(fileName, text);
            };
        }
    }
}
