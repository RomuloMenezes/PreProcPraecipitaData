using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PreProcPraecipitaData
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = "D:\\_GIT\\Projetos\\Projeto Andorinhas (GPD3)\\Dados CD";
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                textBox1.Text = folderBrowserDialog1.SelectedPath;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DirectoryInfo rootDir = new DirectoryInfo(textBox1.Text);
            DirectoryInfo[] folders = rootDir.GetDirectories();

            foreach(DirectoryInfo currFolder in folders)
            {
                foreach (FileInfo currFile in currFolder.GetFiles())
                {
                    if (currFile.Name.IndexOf(".dat") > 0)
                    {
                        StreamReader currInputFile = new StreamReader(currFile.FullName);

                    }
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(dateTimePicker1.Enabled)
            {
                label2.Enabled = false;
                dateTimePicker1.Enabled = false;
                label3.Enabled = false;
                dateTimePicker2.Enabled = false;
            }
            else
            {
                label2.Enabled = true;
                dateTimePicker1.Enabled = true;
                label3.Enabled = true;
                dateTimePicker2.Enabled = true;
            }
        }
    }
}
