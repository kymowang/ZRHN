using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualDataBuilder
{
    using System.IO;
    using System.Linq;
    using System.Runtime.Remoting.Messaging;

    using log4net;

    public class HeatReader
    {
        private int iUploadTime = 3;
        private int iHeat = 13;
        private int iMeterId = 24;

        List<string[]> heater = new List<string[]>();
        public Dictionary<DateTime, int> Result = new Dictionary<DateTime, int>();

        private ILog log = LogManager.GetLogger(typeof(HeatReader));
        public void ReadHeater(string fileName)
        {
            string[] contents = File.ReadAllLines(fileName, Encoding.UTF8);
            for (int i = 1; i < contents.Length; i++)
            {
                var line = contents[i].Trim();
                if(string.IsNullOrEmpty(line))continue;
                string[] temp = line.Split(new []{','});
                //heater.Add(temp);
                DateTime time;
                int heat;
                if (DateTime.TryParse(temp[iUploadTime], out time) && int.TryParse(temp[this.iHeat], out heat))
                {
                    if (this.Result.ContainsKey(time))
                    {
                        this.Result[time] += heat;
                    }
                    else
                    {
                        this.Result.Add(time, heat);
                    }
                }
                else
                {
                    log.Error(string.Format("Parse failed: [{0}], [{1}]",temp[iUploadTime], temp[iHeat]));
                }
            }
        }


       



    }

    public class Builder
    {
        private int iRunTime = 10;

        private int iTotalTime = 11;

        private int iRoomTemperature = 14;

        private int iSetTemperature = 15;

        private int iShangQuDuanKaiQiShiJian = 24;

        private int iShangQuDuanZongShiJian = 25;

        private int iShangQuDuanKaiDu = 26;
        private int iShangQuDuanPingJunWenDu = 27;

        private int iTotalQuantityHeat = 28;

        private int iRoomSize = 8;

        private int roomSizeTotal = 0;

        private int iQuDuanFenTanReLiang = 32;

        List<string[]> content= new List<string[]>();
        private ILog log = LogManager.GetLogger(typeof(Builder));
       

        public string Build(string controllerFile, string heaterFile)
        {
            //read controller file
            string[] tempContent = File.ReadAllLines(controllerFile,Encoding.UTF8);
            int maxId = 0;
           for(int i=1;i< tempContent.Length;i++)
           {
               if (string.IsNullOrEmpty(tempContent[i])) continue;
               string[] temp = tempContent[i].Split(new []{','});
               
               maxId = int.Parse(temp[0]);
               content.Add(temp);
               int roomSize;
               if (int.TryParse(temp[iRoomSize], out roomSize))
               {
                   roomSizeTotal += roomSize;
               }
               else
               {
                   log.Error("Parse room size failed: ["+temp[iRoomSize]+"]");
               }
            }

            //begin write
            string newFileName = Path.Combine(
                Path.GetDirectoryName(controllerFile),
                Path.GetFileNameWithoutExtension(controllerFile) + "_" + DateTime.Now.ToString("yyyyMMddHHmm") + ".csv");
            if(File.Exists(newFileName))File.Delete(newFileName);
            File.Copy(controllerFile,newFileName);
            using (StreamWriter sw = new StreamWriter(newFileName, true, Encoding.UTF8))
            {
                sw.WriteLine();
                HeatReader hr = new HeatReader();
                hr.ReadHeater(heaterFile);
                int runTime = int.Parse(content[0][iRunTime]);
                int totalTime = int.Parse(content[0][this.iTotalTime]);
                int deltaHeat = 0;
                for (int i = 1; i < hr.Result.Count; i++)
                {
                    var heat1 = hr.Result.Keys.ElementAt(i-1);
                    var heat2 = hr.Result.Keys.ElementAt(i);
                    int heatDiff = hr.Result[heat2] - hr.Result[heat1]+deltaHeat;
                    int heatTotalThisTime = 0;
                    TimeSpan timeDiff = heat2 - heat1;

                    for (int j = 0; j < content.Count; j++)
                    {
                        string[] newData = (string[])content[j].Clone();
                        runTime += timeDiff.Hours;
                        totalTime += timeDiff.Hours;
                        //newData[0] = (++maxId).ToString();
                        newData[1] = heat2.ToString("yyyy/M/d H:mm");
                        //newData[iRunTime] = runTime.ToString("0");
                        //newData[this.iTotalTime] = totalTime.ToString("0");
                        //newData[iRoomTemperature] = "0";
                        //newData[iSetTemperature] = "0";
                        //newData[iShangQuDuanKaiQiShiJian] = "2";
                        //newData[iShangQuDuanZongShiJian] = "2";
                        //newData[iShangQuDuanKaiDu] = "1000";
                        //newData[iShangQuDuanPingJunWenDu] = "1000";
                        double distribution = heatDiff * (int.Parse(newData[iRoomSize]) * 1.0) / roomSizeTotal;
                        int changedHeat = (int)Math.Round(distribution, 0);
                        heatTotalThisTime += changedHeat;
                        newData[iTotalQuantityHeat] = (int.Parse(content[j][iTotalQuantityHeat]) + changedHeat).ToString();
                        newData[iQuDuanFenTanReLiang] = Math.Round(distribution * 1000,0).ToString("0");
                        sw.WriteLine(string.Join(",",newData));
                        content[j] = (string[])newData.Clone();
                    }
                    deltaHeat = heatDiff - heatTotalThisTime;
                }
            }
            return newFileName;
        }
    }
}
