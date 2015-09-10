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
            uint iIndex;
            uint itemsReadCount = 0;
            string currLine = "";
            string[] lineElements = new string[4];
            string[] dateTime = new string[2];
            string[] dateElement = new string[3];
            string outputFileName = "";
            string varName = "";
            float varValue = 0;
            DateTime startDate = Convert.ToDateTime("2000-01-01");
            DateTime endDate = Convert.ToDateTime("2000-01-01");
            DateTime currDate = Convert.ToDateTime("2000-01-01");
            DateTime auxDate = Convert.ToDateTime("2000-01-01");
            List<string> variablesChecked = new List<string>();

            variablesChecked = VerifyCheckedVariables();

            if(textBox1.Text == "")
            {
                MessageBox.Show("No folder chosen. Please choose one.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (variablesChecked.Count == 0)
            {
                MessageBox.Show("No variables chosen. Please chose at least one", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                float[] minValue = new float[variablesChecked.Count];
                float[] maxValue = new float[variablesChecked.Count];
                float[] accum = new float[variablesChecked.Count];
                double[] accumSqr = new double[variablesChecked.Count];
                List<float>[] values = new List<float>[variablesChecked.Count];
                float[] mean = new float[variablesChecked.Count];
                double[] stdDev = new double[variablesChecked.Count];
                StreamWriter currOutputFile;
                
                for (iIndex = 0; iIndex < variablesChecked.Count; iIndex++)
                {
                    minValue[iIndex] = float.MaxValue;
                    maxValue[iIndex] = float.MinValue;
                    accum[iIndex] = 0;
                    accumSqr[iIndex] = 0;
                    values[iIndex] = new List<float>();
                }

                DirectoryInfo rootDir = new DirectoryInfo(textBox1.Text);

                if (checkBox1.Checked)
                {
                    startDate = DateTime.ParseExact("2000-" + dateTimePicker1.Value.Month.ToString("00") + "-" + dateTimePicker1.Value.Day.ToString("00"), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    endDate = DateTime.ParseExact("2000-" + dateTimePicker2.Value.Month.ToString("00") + "-" + dateTimePicker2.Value.Day.ToString("00"), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                }

                foreach (FileInfo currFile in rootDir.GetFiles())
                {
                    if (currFile.Name.IndexOf(".dat") > 0)
                    {
                        StreamReader currInputFile = new StreamReader(currFile.FullName);
                        while((currLine = currInputFile.ReadLine()) != null)
                        {
                            lineElements = currLine.Split(';'); // [0] - Station name; [1] - Timestamp; [2] - Variable name; [3] - Value
                            if(lineElements[3]!="NULL")
                            {
                                dateTime = lineElements[1].Split(' '); // [0] - Date; [1] - Time
                                dateElement = dateTime[0].Split('/');
                                auxDate = DateTime.ParseExact("2000-" + dateElement[1] + "-" + dateElement[0], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                                currDate = Convert.ToDateTime(dateTime[0]);
                                if (!checkBox1.Checked || checkBox1.Checked && auxDate >= startDate && auxDate <= endDate)
                                {
                                    varName = lineElements[2].Replace("_", " ");
                                    if (variablesChecked.Contains(varName))
                                    {
                                        iIndex = Convert.ToUInt32(variablesChecked.IndexOf(varName));
                                        values[iIndex].Add(Convert.ToSingle(lineElements[3]));
                                        if (Convert.ToSingle(lineElements[3]) < minValue[iIndex])
                                            minValue[iIndex] = Convert.ToSingle(lineElements[3]);
                                        if (Convert.ToSingle(lineElements[3]) > maxValue[iIndex])
                                            maxValue[iIndex] = Convert.ToSingle(lineElements[3]);
                                        accum[iIndex] += Convert.ToSingle(lineElements[3]);
                                        itemsReadCount++;
                                    }
                                }
                            }
                        }
                        currInputFile.Close();
                    }
                }
                
                // Calculating the means
                for(iIndex=0;iIndex<variablesChecked.Count;iIndex++)
                {
                    mean[iIndex] = accum[iIndex] / itemsReadCount;
                }

                // Calculating the standard deviations and saving statistics on output file
                for (iIndex = 0; iIndex < variablesChecked.Count; iIndex++)
                {
                    foreach(float currValue in values[iIndex])
                    {
                        accumSqr[iIndex] += Math.Pow(currValue - mean[iIndex], 2);
                    }
                    stdDev[iIndex] = Math.Sqrt(accumSqr[iIndex] / (itemsReadCount - 1));

                    outputFileName = textBox1.Text + "\\" + variablesChecked[Convert.ToInt32(iIndex)] + "_stats.dat";
                    currOutputFile = new StreamWriter(outputFileName);
                    currOutputFile.WriteLine("MinValue: " + minValue[iIndex].ToString());
                    currOutputFile.WriteLine("MaxValue: " + maxValue[iIndex].ToString());
                    currOutputFile.WriteLine("Mean: " + mean[iIndex].ToString());
                    currOutputFile.WriteLine("StdDev: " + stdDev[iIndex].ToString());
                    currOutputFile.Close();
                }
                MessageBox.Show("Statistics successfully calculated", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private List<string> VerifyCheckedVariables()
        {
            List<string> returnList = new List<string>();
            if (checkBox2.Checked)
                returnList.Add(checkBox2.Text);
            if (checkBox3.Checked)
                returnList.Add(checkBox3.Text);
            if (checkBox4.Checked)
                returnList.Add(checkBox4.Text);
            if (checkBox5.Checked)
                returnList.Add(checkBox5.Text);
            if (checkBox6.Checked)
                returnList.Add(checkBox6.Text);
            if (checkBox7.Checked)
                returnList.Add(checkBox7.Text);
            if (checkBox8.Checked)
                returnList.Add(checkBox8.Text);
            if (checkBox9.Checked)
                returnList.Add(checkBox9.Text);
            if (checkBox10.Checked)
                returnList.Add(checkBox10.Text);
            if (checkBox11.Checked)
                returnList.Add(checkBox11.Text);
            if (checkBox12.Checked)
                returnList.Add(checkBox12.Text);
            if (checkBox13.Checked)
                returnList.Add(checkBox13.Text);
            if (checkBox14.Checked)
                returnList.Add(checkBox14.Text);
            if (checkBox15.Checked)
                returnList.Add(checkBox15.Text);
            if (checkBox16.Checked)
                returnList.Add(checkBox16.Text);
            if (checkBox17.Checked)
                returnList.Add(checkBox17.Text);
            if (checkBox18.Checked)
                returnList.Add(checkBox18.Text);
            return returnList;
        }

        static class VariablesIndices
        {
            static Dictionary<string, int> variblesIndices = new Dictionary<string, int>
            {
                {"TEMPERATURA DO AR (°C)",0},
                {"UMIDADE RELATIVA DO AR (%)",1},
                {"TEMPERATURA DO PONTO DE ORVALHO (°C)",2},
                {"TEMPERATURA MAXIMA (°C)",3},
                {"TEMPERATURA MINIMA (°C)",4},
                {"TEMPERATURA MÁXIMA DO PONTO DE ORVALHO (°C)",5},
                {"TEMPERATURA MÍNIMA DO PONTO DE ORVALHO (°C)",6},
                {"UMIDADE RELATIVA MAXIMA DO AR (%)",7},
                {"UMIDADE RELATIVA MINIMA DO AR (%)",8},
                {"PRESSÃO ATMOSFERICA (hPa)",9},
                {"VENTO VELOCIDADE",10},
                {"VENTO, DIREÇÃO (graus)",11},
                {"RADIACAO GLOBAL (KJ/M²)",12},
                {"PRECIPITAÇÃO (mm)",13},
                {"VENTO, RAJADA MAXIMA (m/s)",14},
                {"PRESSÃO ATMOSFÉRICA MÁXIMA (hPa)",15},
                {"PRESSÃO ATMOSFÉRICA MÍNIMA (hPa)",16}
            };

            public static int GetIndex(string variableName)
            {
                // Try to get the result in the static Dictionary
                int index;
                if (variblesIndices.TryGetValue(variableName, out index))
                {
                    return index;
                }
                else
                {
                    return -1;
                }
            }
        }
    }
}
