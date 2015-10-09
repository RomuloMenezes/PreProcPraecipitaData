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
            uint variablesIndex = 0;
            uint daysIndex = 0;
            uint hoursIndex = 0;
            string currLine = "";
            string[] lineElements = new string[4];
            string[] dateTime = new string[2];
            string[] dateElement = new string[3];
            string outputFileName = "";
            string varName = "";
            string stationName = "";
            float? varValue = 0;
            DateTime startDate = Convert.ToDateTime("2000-01-01");
            DateTime endDate = Convert.ToDateTime("2000-01-01");
            DateTime currDate = Convert.ToDateTime("2000-01-01");
            DateTime auxDate = Convert.ToDateTime("2000-01-01");
            DateTime auxDateTime = Convert.ToDateTime("2000-01-01 00:00:00");
            List<string> variablesChecked = new List<string>();
            string[] weekDays = new string[7] {"DOM","SEG","TER","QUA","QUI","SEX","SAB"};
            string previousTimestamp = "";

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
                Cursor.Current = Cursors.WaitCursor;
                textBox2.Text = "Calculating statistics";
                textBox2.Refresh();
                float[,,] minValue = new float[variablesChecked.Count,7,24];
                float[,,] maxValue = new float[variablesChecked.Count,7,24];
                float[,,] accum = new float[variablesChecked.Count,7,24];
                double[,,] accumSqr = new double[variablesChecked.Count,7,24];
                List<float>[,,] values = new List<float>[variablesChecked.Count,7,24];
                float[,,] mean = new float[variablesChecked.Count,7,24];
                double[,,] stdDev = new double[variablesChecked.Count,7,24];
                uint[,,] itemsReadCount = new uint[variablesChecked.Count, 7, 24];
                Dictionary<DateTime, float?>[] valuesMatrix = new Dictionary<DateTime,float?>[variablesChecked.Count];
                StreamWriter currOutputFile;

                // Initialization of variables
                textBox2.Text = "Initialization of variables";
                textBox2.Refresh();
                for (variablesIndex = 0; variablesIndex < variablesChecked.Count; variablesIndex++)
                {
                    for (daysIndex = 0; daysIndex < 7; daysIndex++)
                    {
                        for (hoursIndex = 0; hoursIndex < 24; hoursIndex++)
                        {
                            minValue[variablesIndex, daysIndex, hoursIndex] = float.MaxValue;
                            maxValue[variablesIndex, daysIndex, hoursIndex] = float.MinValue;
                            accum[variablesIndex, daysIndex, hoursIndex] = 0;
                            accumSqr[variablesIndex, daysIndex, hoursIndex] = 0;
                            values[variablesIndex, daysIndex, hoursIndex] = new List<float>();
                            itemsReadCount[variablesIndex, daysIndex, hoursIndex] = 0;
                        }
                    }
                    valuesMatrix[variablesIndex] = new Dictionary<DateTime, float?>();
                }

                DirectoryInfo rootDir = new DirectoryInfo(textBox1.Text);

                if (checkBox1.Checked)
                {
                    startDate = DateTime.ParseExact("2000-" + dateTimePicker1.Value.Month.ToString("00") + "-" + dateTimePicker1.Value.Day.ToString("00"), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    endDate = DateTime.ParseExact("2000-" + dateTimePicker2.Value.Month.ToString("00") + "-" + dateTimePicker2.Value.Day.ToString("00"), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                }

                #region Read input files
                foreach (FileInfo currFile in rootDir.GetFiles())
                {
                    if (currFile.Name.IndexOf(".dat") > 0)
                    {
                        textBox2.Text = "Calculating statistics" + Environment.NewLine + "Reading file " + currFile.Name;
                        textBox2.Refresh();
                        StreamReader currInputFile = new StreamReader(currFile.FullName);
                        while((currLine = currInputFile.ReadLine()) != null)
                        {
                            lineElements = currLine.Split(';'); // [0] - Station name; [1] - Timestamp; [2] - Variable name; [3] - Value
                            if (stationName == "")
                                stationName = lineElements[0];
                            dateTime = lineElements[1].Split(' '); // [0] - Date; [1] - Time

                            // The timestamp in the meteorological data is GMT. The following if transforms load data to GMT, also treating daylight saving time.
                            if (lineElements[2] == "CARGA")
                            {
                                auxDateTime = Convert.ToDateTime(lineElements[1]);
                                if (IsDaylighSavingTime(dateTime[0]))
                                    if(lineElements[1]==previousTimestamp) // Last hour of daylight saving time. PI generates 2 records for 23hs
                                        auxDateTime = auxDateTime.AddHours(3);
                                    else
                                        auxDateTime = auxDateTime.AddHours(2);
                                else
                                    auxDateTime = auxDateTime.AddHours(3);
                                auxDate = DateTime.ParseExact("2000-" + auxDateTime.Month.ToString("00") + "-" + auxDateTime.Day.ToString("00"), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                                currDate = auxDateTime;
                                daysIndex = Convert.ToUInt32(currDate.DayOfWeek);
                                hoursIndex = Convert.ToUInt32(currDate.Hour);
                                previousTimestamp = lineElements[1];
                            }
                            else
                            {
                                dateElement = dateTime[0].Split('/');
                                auxDate = DateTime.ParseExact("2000-" + dateElement[1] + "-" + dateElement[0], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                                currDate = Convert.ToDateTime(lineElements[1]);
                                daysIndex = Convert.ToUInt32(currDate.DayOfWeek);
                                hoursIndex = Convert.ToUInt32(dateTime[1].ToString().Substring(0, 2));
                            }
                            if (textBox3.Text == "" || (currDate.Year >= Convert.ToInt32(textBox3.Text) && currDate.Year <= Convert.ToInt32(textBox4.Text)))
                            {
                                if (!checkBox1.Checked || checkBox1.Checked && auxDate >= startDate && auxDate <= endDate)
                                {
                                    varName = lineElements[2].Replace("_", " ");
                                    if (variablesChecked.Contains(varName))
                                    {
                                        variablesIndex = Convert.ToUInt32(variablesChecked.IndexOf(varName));
                                        if (lineElements[3] != "NULL")
                                        {
                                            varValue = Convert.ToSingle(lineElements[3].Replace(".", ","));
                                            values[variablesIndex, daysIndex, hoursIndex].Add((float)varValue);
                                            if (varValue < minValue[variablesIndex, daysIndex, hoursIndex])
                                                minValue[variablesIndex, daysIndex, hoursIndex] = (float)varValue;
                                            if (varValue > maxValue[variablesIndex, daysIndex, hoursIndex])
                                                maxValue[variablesIndex, daysIndex, hoursIndex] = (float)varValue;
                                            accum[variablesIndex, daysIndex, hoursIndex] += (float)varValue;
                                            itemsReadCount[variablesIndex, daysIndex, hoursIndex]++;
                                        }
                                        else
                                        {
                                            varValue = null;
                                        }
                                        valuesMatrix[variablesIndex].Add(currDate, varValue);
                                    }
                                }
                            }
                        }
                        currInputFile.Close();
                    }
                }
                #endregion

                #region Calculating statistics and printing .stat file
                // Calculating the means
                textBox2.Text = "Calculating statistics" + Environment.NewLine + "Calculating means";
                textBox2.Refresh();
                for(variablesIndex=0;variablesIndex<variablesChecked.Count;variablesIndex++)
                {
                    for (daysIndex = 0; daysIndex < 7; daysIndex++)
                    {
                        for (hoursIndex = 0; hoursIndex < 24; hoursIndex++)
                        {
                            mean[variablesIndex, daysIndex, hoursIndex] = accum[variablesIndex, daysIndex, hoursIndex] / itemsReadCount[variablesIndex, daysIndex, hoursIndex];
                        }
                    }
                }

                string interval = "";

                // Calculating the standard deviations and saving statistics on output file
                textBox2.Text = "Calculating statistics" + Environment.NewLine + "Calculating standard deviations and printing output files";
                textBox2.Refresh();
                for (variablesIndex = 0; variablesIndex < variablesChecked.Count; variablesIndex++)
                {
                    for (daysIndex = 0; daysIndex < 7; daysIndex++)
                    {
                        for (hoursIndex = 0; hoursIndex < 24; hoursIndex++)
                        {
                            foreach (float currValue in values[variablesIndex,daysIndex,hoursIndex])
                            {
                                accumSqr[variablesIndex, daysIndex, hoursIndex] += Math.Pow(currValue - mean[variablesIndex, daysIndex, hoursIndex], 2);
                            }
                            stdDev[variablesIndex, daysIndex, hoursIndex] = Math.Sqrt(accumSqr[variablesIndex, daysIndex, hoursIndex] / (itemsReadCount[variablesIndex,daysIndex,hoursIndex] - 1));
                        }
                    }

                    if (checkBox1.Checked)
                    {
                        interval = dateTimePicker1.Value.ToString("MMM").ToUpper() + "a" + dateTimePicker2.Value.ToString("MMM").ToUpper();
                        outputFileName = textBox1.Text + "\\" + stationName + "_" + variablesChecked[Convert.ToInt32(variablesIndex)] + "_" + interval + ".stat";
                    }
                    else
                        outputFileName = textBox1.Text + "\\" + stationName + "_" + variablesChecked[Convert.ToInt32(variablesIndex)] + ".stat";

                    if(outputFileName.IndexOf("/") > 0)
                        outputFileName = outputFileName.Replace("/", "-");

                    currOutputFile = new StreamWriter(outputFileName);
                    currOutputFile.WriteLine("========================== Parameters ==========================");
                    currOutputFile.WriteLine("Station: " + lineElements[0]);
                    currOutputFile.WriteLine("Interval start: " + dateTimePicker1.Value.ToString("dd/MMM"));
                    currOutputFile.WriteLine("Interval end: " + dateTimePicker2.Value.ToString("dd/MMM"));
                    currOutputFile.WriteLine("Initial year: " + textBox3.Text);
                    currOutputFile.WriteLine("Final year: " + textBox4.Text);
                    currOutputFile.WriteLine("Outliers: Mean +- " + textBox5.Text + " stdev");
                    currOutputFile.WriteLine("================================================================");
                    currOutputFile.WriteLine();

                    // Printing minValues
                    currOutputFile.WriteLine("Variable: " + variablesChecked.ElementAt(Convert.ToInt32(variablesIndex)).ToString());
                    currOutputFile.WriteLine("minValues");
                    for (daysIndex = 0; daysIndex < 7; daysIndex++)
                    {
                        currLine = weekDays[daysIndex] + ": ";
                        //currLine = daysIndex.ToString() + ": ";
                        for (hoursIndex = 0; hoursIndex < 24; hoursIndex++)
                        {
                            currLine+=Convert.ToDouble(minValue[variablesIndex,daysIndex,hoursIndex]).ToString("0.00") + ";";
                        }
                        currOutputFile.WriteLine(currLine.Substring(0,currLine.Length-1)); // Excludes the final semicolon
                    }
                    currOutputFile.WriteLine();
                    currOutputFile.WriteLine();

                    // Printing maxValues
                    currOutputFile.WriteLine("maxValues");
                    for (daysIndex = 0; daysIndex < 7; daysIndex++)
                    {
                        currLine = weekDays[daysIndex] + ": ";
                        // currLine = daysIndex.ToString() + ": ";
                        for (hoursIndex = 0; hoursIndex < 24; hoursIndex++)
                        {
                            currLine += maxValue[variablesIndex, daysIndex, hoursIndex].ToString("0.00") + ";";
                        }
                        currOutputFile.WriteLine(currLine.Substring(0, currLine.Length - 1)); // Excludes the final semicolon
                    }
                    currOutputFile.WriteLine();
                    currOutputFile.WriteLine();

                    // Printing mean
                    currOutputFile.WriteLine("Means");
                    for (daysIndex = 0; daysIndex < 7; daysIndex++)
                    {
                        currLine = weekDays[daysIndex] + ": ";
                        // currLine = daysIndex.ToString() + ": ";
                        for (hoursIndex = 0; hoursIndex < 24; hoursIndex++)
                        {
                            currLine += mean[variablesIndex, daysIndex, hoursIndex].ToString("0.00") + ";";
                        }
                        currOutputFile.WriteLine(currLine.Substring(0, currLine.Length - 1)); // Excludes the final semicolon
                    }
                    currOutputFile.WriteLine();
                    currOutputFile.WriteLine();

                    // Printing stdDev
                    currOutputFile.WriteLine("Standard deviations");
                    for (daysIndex = 0; daysIndex < 7; daysIndex++)
                    {
                        currLine = weekDays[daysIndex] + ": ";
                        // currLine = daysIndex.ToString() + ": ";
                        for (hoursIndex = 0; hoursIndex < 24; hoursIndex++)
                        {
                            currLine += stdDev[variablesIndex, daysIndex, hoursIndex].ToString("0.00") + ";";
                        }
                        currOutputFile.WriteLine(currLine.Substring(0, currLine.Length - 1)); // Excludes the final semicolon
                    }
                    currOutputFile.WriteLine();
                    currOutputFile.WriteLine();

                    currOutputFile.Close();
                }

                // Calculating delta matrix. It contains, in each position, the delta that has to be added to the previous value in order to calculate the missing value correponding to that position.
                // For example: if hour 3am of 21/01/2011 is missing, the code will add the delta in the position corresponding to 3am to the (non-missing) value of 2am.
                textBox2.Text = "Calculating statistics" + Environment.NewLine + "Building correction matrices";
                textBox2.Refresh();
                float deltaValue = 0;
                float[,,] accumDelta = new float[variablesChecked.Count, 7, 24];
                uint[,,] itemsCount = new uint[variablesChecked.Count, 7, 24];
                float[, ,] deltaMatrix = new float[variablesChecked.Count, 7, 24];
                int currDay = 0;
                int currHour = 0;
                DateTime previousDate;
                for (variablesIndex = 0; variablesIndex < variablesChecked.Count; variablesIndex++)
                { 
                    foreach(KeyValuePair<DateTime,float?> pair in valuesMatrix[variablesIndex])
                    {
                        if(pair.Value.HasValue)
                        {
                            currDate = pair.Key;
                            previousDate = currDate.AddHours(-1);
                            if (valuesMatrix[variablesIndex].ContainsKey(previousDate))
                            {
                                if (valuesMatrix[variablesIndex][previousDate].HasValue)
                                {
                                    currDay = Convert.ToInt32(currDate.DayOfWeek);
                                    currHour = Convert.ToInt32(currDate.Hour.ToString("00"));
                                    deltaValue = (float)(pair.Value) - (float)(valuesMatrix[variablesIndex][previousDate]);
                                    accumDelta[variablesIndex, currDay, currHour] += deltaValue;
                                    itemsCount[variablesIndex, currDay, currHour]++;
                                }
                            }
                        }
                    }

                    if(checkBox1.Checked)
                        outputFileName = textBox1.Text + "\\" + stationName + "_" + variablesChecked[Convert.ToInt32(variablesIndex)] + "_" + interval + ".stat";
                    else
                        outputFileName = textBox1.Text + "\\" + stationName + "_" + variablesChecked[Convert.ToInt32(variablesIndex)] + ".stat";
                    if (outputFileName.IndexOf("/") > 0)
                        outputFileName = outputFileName.Replace("/", "-");

                    currOutputFile = new StreamWriter(outputFileName, true);
                    currOutputFile.WriteLine("Variable: " + variablesChecked.ElementAt(Convert.ToInt32(variablesIndex)).ToString());
                    currOutputFile.WriteLine("Mean delta matrix");

                    for(daysIndex=0;daysIndex<7;daysIndex++)
                    {
                        currLine = weekDays[daysIndex] + ": ";
                        // currLine = daysIndex.ToString() + ": ";
                        for(hoursIndex=0;hoursIndex<24;hoursIndex++)
                        {
                            deltaMatrix[variablesIndex, daysIndex, hoursIndex] = accumDelta[variablesIndex, daysIndex, hoursIndex] / itemsCount[variablesIndex, daysIndex, hoursIndex];
                            currLine += deltaMatrix[variablesIndex, daysIndex, hoursIndex].ToString("0.00") + ";";
                        }
                        currOutputFile.WriteLine(currLine.Substring(0, currLine.Length - 1)); // Excludes the final semicolon
                    }
                    currOutputFile.Close();
                }
                #endregion

                #region Detecting, reporting and replacing outliers by NULL. Printing .step1 and .outliers files
                // Printing output files
                // Replacing outliers by NULL
                string checkedVarName = "";
                string outputLine = "";
                string currStep1FileName = "";
                string outliersReportFileName = "";
                StreamWriter[] step1OutputFiles = new StreamWriter[variablesChecked.Count];
                StreamWriter[] detectedOutliers = new StreamWriter[variablesChecked.Count];

                foreach (string checkedVariable in variablesChecked)
                {
                    variablesIndex = Convert.ToUInt32(variablesChecked.IndexOf(checkedVariable));
                    if (checkedVariable.IndexOf("/") > 0)
                        checkedVarName = checkedVariable.Replace("/", "-");
                    else
                        checkedVarName = checkedVariable;
                    if (checkBox1.Checked)
                    {
                        currStep1FileName = textBox1.Text + "\\" + stationName + "_" + checkedVarName + "_" + interval + ".step1";
                        outliersReportFileName = textBox1.Text + "\\" + stationName + "_" + checkedVarName + "_" + interval + ".outliers";
                    }
                    else
                    {
                        currStep1FileName = textBox1.Text + "\\" + stationName + "_" + checkedVarName + ".step1";
                        outliersReportFileName = textBox1.Text + "\\" + stationName + "_" + checkedVarName + ".outliers";
                    }
                    step1OutputFiles[variablesIndex] = new StreamWriter(currStep1FileName);
                    detectedOutliers[variablesIndex] = new StreamWriter(outliersReportFileName);
                }

                #region Rereading .dat files (commented)
                //foreach (FileInfo currFile in rootDir.GetFiles())
                //{
                //    if (currFile.Name.IndexOf(".dat") > 0)
                //    {
                //        textBox2.Text = "Serching for outliers on file " + currFile.Name;
                //        textBox2.Refresh();
                //        StreamReader currInputFile = new StreamReader(currFile.FullName);

                //        while ((currLine = currInputFile.ReadLine()) != null)
                //        {
                //            lineElements = currLine.Split(';'); // [0] - Station name; [1] - Timestamp; [2] - Variable name; [3] - Value
                //            if (lineElements[3] != "NULL")
                //            {
                //                varName = lineElements[2].Replace("_", " ");
                //                if (variablesChecked.Contains(varName))
                //                {
                //                    varValue = Convert.ToSingle(lineElements[3].Replace(".", ","));
                //                    variablesIndex = Convert.ToUInt32(variablesChecked.IndexOf(varName));
                //                    dateTime = lineElements[1].Split(' '); // [0] - Date; [1] - Time
                //                    dateElement = dateTime[0].Split('/');
                //                    auxDate = DateTime.ParseExact("2000-" + dateElement[1] + "-" + dateElement[0], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                //                    hoursIndex = Convert.ToUInt32(dateTime[1].ToString().Substring(0, 2));
                //                    currDate = Convert.ToDateTime(dateTime[0]);
                //                    daysIndex = Convert.ToUInt32(currDate.DayOfWeek);
                //                    if (textBox3.Text == "" || (currDate.Year >= Convert.ToInt32(textBox3.Text) && currDate.Year <= Convert.ToInt32(textBox4.Text)))
                //                    {
                //                        if (!checkBox1.Checked || checkBox1.Checked && auxDate >= startDate && auxDate <= endDate)
                //                        {
                //                            if (varValue < mean[variablesIndex, daysIndex, hoursIndex] - Convert.ToInt32(textBox5.Text) * stdDev[variablesIndex, daysIndex, hoursIndex] ||
                //                                varValue > mean[variablesIndex, daysIndex, hoursIndex] + Convert.ToInt32(textBox5.Text) * stdDev[variablesIndex, daysIndex, hoursIndex]) // Outlier
                //                            {
                //                                outputLine = lineElements[0] + ";" + lineElements[1] + ";" + lineElements[2] + ";NULL";
                //                                detectedOutliers[variablesIndex].WriteLine(currLine);
                //                            }
                //                            else
                //                                outputLine = lineElements[0] + ";" + lineElements[1] + ";" + lineElements[2] + ";" + lineElements[3];
                //                            step1OutputFiles[variablesIndex].WriteLine(outputLine);
                //                        }
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}
                #endregion

                #region Reading data from locar variables populated while reading .dat files
                foreach (string currCheckedVar in variablesChecked)
                {
                    textBox2.Text = "Serching for outliers on variable " + currCheckedVar;
                    textBox2.Refresh();

                    variablesIndex = Convert.ToUInt32(variablesChecked.IndexOf(currCheckedVar));
                    if (currCheckedVar.IndexOf("/") > 0)
                        checkedVarName = currCheckedVar.Replace("/", "-");
                    else
                        checkedVarName = currCheckedVar;

                    foreach (DateTime currKey in valuesMatrix[variablesIndex].Keys)
                    {
                        varValue = valuesMatrix[variablesIndex][currKey];
                        auxDate = DateTime.ParseExact("2000-" + currKey.Month.ToString("00") + "-" + currKey.Day.ToString("00"), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                        hoursIndex = Convert.ToUInt32(currKey.Hour);
                        daysIndex = Convert.ToUInt32(currKey.DayOfWeek);
                        if (textBox3.Text == "" || (currKey.Year >= Convert.ToInt32(textBox3.Text) && currKey.Year <= Convert.ToInt32(textBox4.Text)))
                        {
                            if (!checkBox1.Checked || checkBox1.Checked && auxDate >= startDate && auxDate <= endDate)
                            {
                                if(varValue.HasValue)
                                {
                                    if (varValue < mean[variablesIndex, daysIndex, hoursIndex] - Convert.ToInt32(textBox5.Text) * stdDev[variablesIndex, daysIndex, hoursIndex] ||
                                    varValue > mean[variablesIndex, daysIndex, hoursIndex] + Convert.ToInt32(textBox5.Text) * stdDev[variablesIndex, daysIndex, hoursIndex]) // Outlier
                                    {
                                        outputLine = stationName + ";" + currKey.ToString("dd/MM/yyyy HH:mm") + ";" + checkedVarName + ";NULL";
                                        currLine = stationName + ";" + currKey.ToString("dd/MM/yyyy HH:mm") + ";" + checkedVarName + ";" + varValue;
                                        detectedOutliers[variablesIndex].WriteLine(currLine);
                                    }
                                    else
                                        outputLine = stationName + ";" + currKey.ToString("dd/MM/yyyy HH:mm") + ";" + checkedVarName + ";" + varValue;
                                }
                                else
                                    outputLine = stationName + ";" + currKey.ToString("dd/MM/yyyy HH:mm") + ";" + checkedVarName + ";NULL";
                                step1OutputFiles[variablesIndex].WriteLine(outputLine);
                            }
                        }
                    }
                }
                #endregion

                foreach (string checkedVariable in variablesChecked)
                {
                    variablesIndex = Convert.ToUInt32(variablesChecked.IndexOf(checkedVariable));
                    step1OutputFiles[variablesIndex].Close();
                    detectedOutliers[variablesIndex].Close();
                }
                #endregion

                #region Replacing NULLs - missing data and outliers. Printing .final file
                // Replacing missing values
                string previousLine = "";
                string[] previousLineElements;
                double? previousValue = 0;
                double interpolatedValue = 0;

                #region Reading .step1 file (commented)
                //foreach (FileInfo currFile in rootDir.GetFiles())
                //{
                //    if (currFile.Name.IndexOf(".step1") > 0)
                //    {
                //        textBox2.Text = "Replacing missing values on file " + currFile.Name;
                //        textBox2.Refresh();
                //        StreamReader currInputFile = new StreamReader(currFile.FullName);
                //        outputFileName = currFile.FullName.Substring(0, currFile.FullName.Length - 5) + "final";
                //        currOutputFile = new StreamWriter(outputFileName);

                //        while ((currLine = currInputFile.ReadLine()) != null)
                //        {
                //            lineElements = currLine.Split(';'); // [0] - Station name; [1] - Timestamp; [2] - Variable name; [3] - Value
                //            if (lineElements[3] == "NULL")
                //            {
                //                if(previousLine!="")
                //                {
                //                    varName = lineElements[2].Replace("_", " ");
                //                    if (variablesChecked.Contains(varName))
                //                    {
                //                        previousLineElements = previousLine.Split(';');
                //                        variablesIndex = Convert.ToUInt32(variablesChecked.IndexOf(varName));
                //                        dateTime = lineElements[1].Split(' '); // [0] - Date; [1] - Time
                //                        hoursIndex = Convert.ToUInt32(dateTime[1].ToString().Substring(0, 2));
                //                        currDate = Convert.ToDateTime(dateTime[0]);
                //                        daysIndex = Convert.ToUInt32(currDate.DayOfWeek);
                //                        previousValue = Convert.ToDouble(previousLineElements[3].Replace(".",","));
                //                        interpolatedValue = (double)previousValue + Convert.ToDouble(deltaMatrix[variablesIndex, daysIndex, hoursIndex]);
                //                        outputLine = stationName + ";" + lineElements[1] + ";" + lineElements[2] + ";" + interpolatedValue.ToString("0.00");
                //                        currOutputFile.WriteLine(outputLine);
                //                        previousLine = outputLine;
                //                    }
                //                }
                //            }
                //            else
                //            {
                //                currOutputFile.WriteLine(currLine);
                //                previousLine = currLine;
                //            }
                //        }
                //        currInputFile.Close();
                //        currOutputFile.Close();
                //    }
                //}
                #endregion

                #region Reading data from local variables
                foreach (string currCheckedVar in variablesChecked)
                {
                    textBox2.Text = "Replacing missing values and outliers on variable " + currCheckedVar;
                    textBox2.Refresh();

                    variablesIndex = Convert.ToUInt32(variablesChecked.IndexOf(currCheckedVar));
                    if (currCheckedVar.IndexOf("/") > 0)
                        checkedVarName = currCheckedVar.Replace("/", "-");
                    else
                        checkedVarName = currCheckedVar;
                    if (checkBox1.Checked)
                        outputFileName = textBox1.Text + "\\" + stationName + "_" + checkedVarName + "_" + interval + ".final";
                    else
                        outputFileName = textBox1.Text + "\\" + stationName + "_" + checkedVarName + ".final";
                    currOutputFile = new StreamWriter(outputFileName);

                    foreach (DateTime currKey in valuesMatrix[variablesIndex].Keys)
                    {
                        if (!valuesMatrix[variablesIndex][currKey].HasValue)
                        {
                            if (previousValue != null)
                            {
                                varName = checkedVarName.Replace("_", " ");
                                if (variablesChecked.Contains(checkedVarName))
                                {
                                    variablesIndex = Convert.ToUInt32(variablesChecked.IndexOf(varName));
                                    hoursIndex = Convert.ToUInt32(currKey.Hour);
                                    daysIndex = Convert.ToUInt32(currKey.DayOfWeek);
                                    interpolatedValue = (double)previousValue + Convert.ToDouble(deltaMatrix[variablesIndex, daysIndex, hoursIndex]);
                                    outputLine = stationName + ";" + currKey.ToString() + ";" + currCheckedVar + ";" + interpolatedValue.ToString("0.00").Replace(",", ".");
                                    currOutputFile.WriteLine(outputLine);
                                    previousValue = interpolatedValue;
                                }
                            }
                        }
                        else
                        {
                            currLine = stationName + ";" + currKey.ToString("dd/MM/yyyy HH:mm") + ";" + currCheckedVar + ";" + Convert.ToDouble(valuesMatrix[variablesIndex][currKey]).ToString("0.00").Replace(",",".");
                            currOutputFile.WriteLine(currLine);
                            previousValue = (float)valuesMatrix[variablesIndex][currKey];
                        }
                    }
                    currOutputFile.Close();
                }
                #endregion

                #endregion

                textBox2.Text = "";
                textBox2.Refresh();
                Cursor.Current = Cursors.Default;
                MessageBox.Show("Data successfully processed", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(dateTimePicker1.Enabled)
            {
                label2.Enabled = false;
                dateTimePicker1.Enabled = false;
                label3.Enabled = false;
                dateTimePicker2.Enabled = false;
                label4.Enabled = false;
                textBox3.Enabled = false;
                label5.Enabled = false;
                textBox4.Enabled = false;
            }
            else
            {
                label2.Enabled = true;
                dateTimePicker1.Enabled = true;
                label3.Enabled = true;
                dateTimePicker2.Enabled = true;
                label4.Enabled = true;
                textBox3.Enabled = true;
                label5.Enabled = true;
                textBox4.Enabled = true;
            }
        }

        private bool IsDaylighSavingTime(string sDate)
        {
            bool bReturn = false;
            DateTime date = Convert.ToDateTime(sDate);

            //Daylight Saving Time 2013-2014
            DateTime startDayightSavingTime = new DateTime(2013, 10, 20);
            DateTime endDayightSavingTime = new DateTime(2014, 02, 15);
            if (date >= startDayightSavingTime && date <= endDayightSavingTime)
                bReturn = true;

            //Daylight Saving Time 2014-2015
            startDayightSavingTime = new DateTime(2014, 10, 19);
            endDayightSavingTime = new DateTime(2015, 02, 21);
            if (date >= startDayightSavingTime && date <= endDayightSavingTime)
                bReturn = true;

            return bReturn;
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
            if (checkBox19.Checked)
                returnList.Add(checkBox19.Text);
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

        private void groupBox2_DoubleClick(object sender, System.EventArgs e)
        {
            if(checkBox2.Checked)
            {
                checkBox2.Checked = false;
                checkBox3.Checked = false;
                checkBox4.Checked = false;
                checkBox5.Checked = false;
                checkBox6.Checked = false;
                checkBox7.Checked = false;
                checkBox8.Checked = false;
                checkBox9.Checked = false;
                checkBox10.Checked = false;
                checkBox11.Checked = false;
                checkBox12.Checked = false;
                checkBox13.Checked = false;
                checkBox14.Checked = false;
                checkBox15.Checked = false;
                checkBox16.Checked = false;
                checkBox17.Checked = false;
                checkBox18.Checked = false;
                checkBox19.Checked = false;
            }
            else
            {
                checkBox2.Checked = true;
                checkBox3.Checked = true;
                checkBox4.Checked = true;
                checkBox5.Checked = true;
                checkBox6.Checked = true;
                checkBox7.Checked = true;
                checkBox8.Checked = true;
                checkBox9.Checked = true;
                checkBox10.Checked = true;
                checkBox11.Checked = true;
                checkBox12.Checked = true;
                checkBox13.Checked = true;
                checkBox14.Checked = true;
                checkBox15.Checked = true;
                checkBox16.Checked = true;
                checkBox17.Checked = true;
                checkBox18.Checked = true;
                checkBox19.Checked = true;
            }
        }

    }
}
