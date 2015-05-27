using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirtualDataBuilder
{
    using System.IO;
    using System.Windows.Forms;

    class HeaterMeterBuilder
    {
#region const
        private const int i1Time = 0;

        private const int i1HeatEnergy = 1;

        private const int i1Volume = 2;

        private const int i1Power = 3;

        private const int i1Flow = 4;
        private const int i1TemperatureFlow = 5;
        private const int i1TemperatureReturn = 6;
       

        private const int i2UploadTime = 2;
        private const int i2TimeYear = 5;
        private const int i2TimeMonth = 6;
        private const int i2TimeDay = 7;
        private const int i2TimeHour = 8;
        private const int i2TimeMinute = 9;
        private const int i2TimeSecond = 10;
        private const int i2totalQuantityHeat = 12;
        private const int i2totalQuantityFlow = 13;
        private const int i2thermalPower = 14;
        private const int i2flowSpeed = 15;
        private const int i2temWaterIn = 16;
        private const int i2temWaterOut = 17;
        private const int i2heatBaseNumber = 18;
        private const int i2sectionHeat = 19;
        private const int i2heatMeterID= 22;
#endregion

        public string[] MeterInputFiles;
        List<List<string[]>> meterFileContents= new List<List<string[]>>();
        List<List<DateTime>> meterFileTime = new List<List<DateTime>>();
        List<List<string[]>> resultFile =new List<List<string[]>>();
        List<string[]> outputSample=new List<string[]>();

        private string outputHeader;

        private string outputDir;

        private string sampleFile;

        public HeaterMeterBuilder(string[] inputFiles, string outputFile)
        {
            Array.Sort(inputFiles);
            this.MeterInputFiles = inputFiles;
            sampleFile = outputFile;
            outputDir = Path.GetDirectoryName(outputFile);

        }

        void ReadSampleFile()
        {
            string[] content = File.ReadAllLines(sampleFile);
            outputHeader = content[0];
            for (int i = 1; i < content.Length; i++)
            {
                if(string.IsNullOrEmpty(content[i]))continue;
                
                outputSample.Add(content[i].Split(','));
            }
        }

        void ReadMeterFiles()
        {
            for (int i = 0; i < this.MeterInputFiles.Length; i++)
            {
                string[] tempContent = File.ReadAllLines(this.MeterInputFiles[i]);
                List<string[]> oneFileContent = new List<string[]>();
                List<DateTime> oneFileTime = new List<DateTime>();
                for (int j = 1; j < tempContent.Length; j++)
                {
                    if (string.IsNullOrEmpty(tempContent[i])) continue;
                    string[] lineParts = tempContent[j].Split(',');
                    oneFileContent.Add(lineParts);

                    DateTime dt = DateTime.Parse(lineParts[0]);
                    DateTime newDt = dt;
                    if (dt.Minute >= 30)
                    {
                        newDt = dt.AddHours(1);
                    }
                    newDt = new DateTime(newDt.Year, newDt.Month, newDt.Day, newDt.Hour, 0, 0, 0);
                    oneFileTime.Add(newDt);
                }
                meterFileContents.Add(oneFileContent);
                this.meterFileTime.Add(oneFileTime);
            }
        }

        public string Build()
        {
            this.ReadSampleFile();
            this.ReadMeterFiles();

            for (int i = 0; i < meterFileContents.Count; i++)
            {
                List<string[]> oneMeterFile = meterFileContents[i];
                List<DateTime> oneDT = meterFileTime[i];
                List<string[]> oneResultFile=new List<string[]>();
                oneResultFile.Add(outputSample[i]);
                //int prevHeat = int.Parse(oneFile[i][i1HeatEnergy]);
                //process one file
                for (int j = 1; j < oneMeterFile.Count; j++)
                {
                    string[] prevLine = oneMeterFile[j - 1];
                    string[] line = oneMeterFile[j];
                    DateTime prevDT = DateTime.Parse(prevLine[i1Time]);

                   

                    TimeSpan ts = oneDT[j] - oneDT[j - 1];
                    int insertCount = (int)ts.TotalHours;
                    int prevHeat =(int) (double.Parse(prevLine[i1HeatEnergy])*1000);
                    int heat =(int) (double.Parse(line[i1HeatEnergy])*1000);

                    double preFlow = double.Parse(prevLine[i1Volume]);
                    double flow = double.Parse(line[i1Volume]);

                    int deltaHeat = (int)Math.Round((heat - prevHeat) * 1.0 / insertCount, 0);
                    double deltaFlow =Math.Round((flow - preFlow) / insertCount, 1);
                    
                    ;
                    for (int p = 0; p < insertCount-1; p++)
                    {
                        string[] resultLine = (string[])outputSample[i].Clone();
                        DateTime tempDT = prevDT.AddHours(1).AddMinutes(new Random(GetRandomSeed()).Next(0,5));
                        resultLine[i2UploadTime] = tempDT.ToString("yyyy/M/d H:mm");
                        resultLine[i2TimeYear] = tempDT.Year.ToString().Substring(2);
                        resultLine[i2TimeMonth] = tempDT.Month.ToString();
                        resultLine[i2TimeDay] = tempDT.Day.ToString();
                        resultLine[i2TimeHour] = tempDT.Hour.ToString();
                        resultLine[i2TimeMinute] = tempDT.Minute.ToString();
                        resultLine[i2TimeSecond] = tempDT.Second.ToString();
                        prevHeat = prevHeat + deltaHeat;
                        resultLine[i2totalQuantityHeat] = prevHeat.ToString();
                        preFlow += deltaFlow;
                        resultLine[i2totalQuantityFlow] = preFlow.ToString(".0");

                        int power1 = (int)(Math.Floor(float.Parse(prevLine[i1Power]) * 10));
                        int power2 = (int)(Math.Floor(float.Parse(line[i1Power]) * 10));
                        int power = power1 > power2 ? (new Random(GetRandomSeed()).Next(power2, power1)) : (new Random().Next(power1, power2));
                        resultLine[i2thermalPower] = Math.Round(power / 10.0, 1).ToString();

                        int flowSpeed1 = (int)(Math.Floor(float.Parse(prevLine[i1Flow]) * 10));
                        int flowSpeed2 = (int)(Math.Floor(float.Parse(line[i1Flow]) * 10));
                        int flowSpeed = flowSpeed1 > flowSpeed2 ? (new Random(GetRandomSeed()).Next(flowSpeed2, flowSpeed1)) : (new Random().Next(flowSpeed1, flowSpeed2));
                        resultLine[i2flowSpeed] = Math.Round(flowSpeed / 10.0, 1).ToString();

                        int waterIn1 = (int)(Math.Floor(float.Parse(prevLine[i1TemperatureFlow]) * 100));
                        int waterIn2 = (int)(Math.Floor(float.Parse(line[i1TemperatureFlow]) * 100));
                        int waterIn = waterIn1 > waterIn2 ? (new Random(GetRandomSeed()).Next(waterIn2, waterIn1)) : (new Random().Next(waterIn1, waterIn2));
                        resultLine[i2temWaterIn] = Math.Round(waterIn / 100.0,2).ToString();

                        int waterOut1 = (int)(Math.Floor(float.Parse(prevLine[i1TemperatureReturn]) * 100));
                        int waterOut2 = waterIn-1;
                        int waterOut = waterOut1 > waterOut2 ? (new Random(GetRandomSeed()).Next(waterOut2, waterOut1)) : (new Random().Next(waterOut1, waterOut2));
                        resultLine[i2temWaterOut] = Math.Round(waterOut / 100.0, 2).ToString();

                        resultLine[i2heatBaseNumber] = resultLine[i2totalQuantityHeat];
                        resultLine[i2sectionHeat] = deltaHeat.ToString();
                        //todo
                        resultLine[i2heatMeterID] = (i + 1).ToString();
                        oneResultFile.Add((string[])resultLine.Clone());
                        prevDT = tempDT;
                    }

                    //copy end line
                    string[] copyLine = (string[])outputSample[i].Clone();
                    copyLine[i2UploadTime] = line[i1Time];
                    DateTime dtx = DateTime.Parse(line[i1Time]);
                    copyLine[i2TimeYear] = dtx.Year.ToString().Substring(2);
                    copyLine[i2TimeMonth] = dtx.Month.ToString();
                    copyLine[i2TimeDay] = dtx.Day.ToString();
                    copyLine[i2TimeHour] = dtx.Hour.ToString();
                    copyLine[i2TimeMinute] = dtx.Minute.ToString();
                    copyLine[i2TimeSecond] = dtx.Second.ToString();
                    copyLine[i2totalQuantityHeat] = ((int)(double.Parse(line[i1HeatEnergy])*1000)).ToString();
                    copyLine[i2totalQuantityFlow] = line[i1Volume];

                    copyLine[i2thermalPower] = line[i1Power];

                    copyLine[i2flowSpeed] = line[i1Flow];

                    copyLine[i2temWaterIn] = line[i1TemperatureFlow];

                    copyLine[i2temWaterOut] = line[i1TemperatureReturn];

                    copyLine[i2heatBaseNumber] = copyLine[i2totalQuantityHeat];
                    copyLine[i2sectionHeat] = deltaHeat.ToString();
                    //todo
                    copyLine[i2heatMeterID] = (i + 1).ToString();

                    oneResultFile.Add(copyLine);
                }
                resultFile.Add(oneResultFile);
            }

            string fileNames = string.Empty;
            for (int i = 0; i < resultFile.Count; i++)
            {
                string tempFile = Path.Combine(outputDir, (i + 1) + ".csv");
                using (StreamWriter sw = new StreamWriter(tempFile, false))
                {
                    sw.WriteLine(outputHeader);
                    foreach (var item in resultFile[i])
                    {
                        sw.WriteLine(string.Join(",", item));
                    }
                }
                fileNames += tempFile + Environment.NewLine;
            }
            return fileNames;
        }

        static int GetRandomSeed()
        {
            byte[] bytes = new byte[4];
            System.Security.Cryptography.RNGCryptoServiceProvider rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
            rng.GetBytes(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

    }
}
