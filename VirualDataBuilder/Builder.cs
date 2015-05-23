using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualDataBuilder
{
    using System.IO;
    using System.Linq;
    using System.Runtime.Remoting.Messaging;

    using log4net;

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

        private int changedHeatTotal = 0;

        private int iQuDuanFenTanReLiang = 32;

        List<string[]> content= new List<string[]>();
        private ILog log = LogManager.GetLogger(typeof(Builder));
       

        public string Build(string controllerFile1, string heaterFile)
        {
            //read controller file
            string[] tempContent = File.ReadAllLines(controllerFile1,Encoding.Default);
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
                Path.GetDirectoryName(controllerFile1),
                Path.GetFileNameWithoutExtension(controllerFile1) + "_" + DateTime.Now.ToString("yyyyMMddHHmm") + ".csv");
            if(File.Exists(newFileName))File.Delete(newFileName);
            File.Copy(controllerFile1,newFileName);
            using (StreamWriter sw = new StreamWriter(newFileName, true, Encoding.Default))
            {
                sw.WriteLine();
                HeaterReader hr = new HeaterReader();
                hr.ReadHeater(heaterFile);
                int runTime = int.Parse(content[0][iRunTime]);
                int totalTime = int.Parse(content[0][this.iTotalTime]);
                int deltaHeat = 0;
                for (int i = 1; i < hr.Result.Count; i++)
                {
                    var heat1 = hr.Result.Keys.ElementAt(i-1);
                    var heat2 = hr.Result.Keys.ElementAt(i);
                    int heatDiff = hr.Result[heat2] - hr.Result[heat1];
                    if (heatDiff < 0)
                    {
                        throw new InvalidDataException("热量表热量错误, 时间："+heat2);
                    }
                    heatDiff += +deltaHeat;
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
                        int changedHeat = (int)Math.Floor(distribution);
                        heatTotalThisTime += changedHeat;
                        newData[iTotalQuantityHeat] = (int.Parse(content[j][iTotalQuantityHeat]) + changedHeat).ToString();
                        newData[iQuDuanFenTanReLiang] = Math.Round(distribution * 1000,0).ToString("0");
                        sw.WriteLine(string.Join(",", newData));
                        //sw.WriteLine(Encoding.Default.GetString(Encoding.UTF8.GetBytes(string.Join(",", newData))));
                        content[j] = (string[])newData.Clone();
                    }
                    deltaHeat = heatDiff - heatTotalThisTime;
                }
            }
            return newFileName;
        }

        public string Build(string controllerFile1, string controllerFile2, string heaterFile)
        {
            //read controller file
            string[] tempContent1 = File.ReadAllLines(controllerFile1, Encoding.Default);
            string[] tempContent2 = File.ReadAllLines(controllerFile2, Encoding.Default);
            if (tempContent1.Length != tempContent2.Length)
            {
                log.Error("控制器开始数据和结束数据有差异");
                throw new InvalidDataException("控制器开始数据和结束数据有差异");
            }
            int[] controllerChangedHeat=new int[tempContent1.Length];

            int maxId = 0;
            for (int i = 1; i < tempContent1.Length; i++)
            {
                if (string.IsNullOrEmpty(tempContent1[i])) continue;
                string[] temp1 = tempContent1[i].Split(new[] { ',' });
                string[] temp2 = tempContent2[i].Split(new[] { ',' });

                maxId = int.Parse(temp1[0]);
                content.Add(temp1);
                int roomSize;
                if (int.TryParse(temp1[iRoomSize], out roomSize))
                {
                    roomSizeTotal += roomSize;
                }
                else
                {
                    log.Error("Parse room size failed: [" + temp1[iRoomSize] + "]");
                }
                int heat1 = int.Parse(temp1[iTotalQuantityHeat]);
                int heat2 = int.Parse(temp2[iTotalQuantityHeat]);
                controllerChangedHeat[i-1] = heat2 - heat1;
                if (controllerChangedHeat[i - 1] < 0)
                {
                    throw new InvalidDataException("控制器热量变小, id： ["+ tempContent1[i][0]+"]");
                }
                changedHeatTotal += controllerChangedHeat[i-1];
            }

            //begin write
            string newFileName = Path.Combine(
                Path.GetDirectoryName(controllerFile1),
                Path.GetFileNameWithoutExtension(controllerFile1) + "_" + DateTime.Now.ToString("yyyyMMddHHmm") + ".csv");
            if (File.Exists(newFileName)) File.Delete(newFileName);
            File.Copy(controllerFile1, newFileName);
            using (StreamWriter sw = new StreamWriter(newFileName, true, Encoding.Default))
            {
                sw.WriteLine();
                HeaterReader hr = new HeaterReader();
                hr.ReadHeater(heaterFile);
                int runTime = int.Parse(content[0][iRunTime]);
                int totalTime = int.Parse(content[0][this.iTotalTime]);
                int deltaHeat = 0;

                double[] remainHeat=new double[content.Count];
                for (int i = 1; i < hr.Result.Count; i++)
                {
                    var heat1 = hr.Result.Keys.ElementAt(i - 1);
                    var heat2 = hr.Result.Keys.ElementAt(i);
                    int heatDiff = hr.Result[heat2] - hr.Result[heat1] + deltaHeat;
                    int heatTotalThisTime = 0;
                    TimeSpan timeDiff = heat2 - heat1;

                    for (int j = 0; j < content.Count; j++)
                    {
                        string[] newData = (string[])content[j].Clone();
                        //runTime += timeDiff.Hours;
                        //totalTime += timeDiff.Hours;
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
                        double distribution = heatDiff * (controllerChangedHeat[j]*1.0) / changedHeatTotal;
                        int changedHeat = (int)Math.Floor(distribution);
                        remainHeat[j] += distribution - changedHeat;//热量的小数部分
                        heatTotalThisTime += changedHeat;
                        newData[iTotalQuantityHeat] = (int.Parse(content[j][iTotalQuantityHeat]) + changedHeat).ToString();
                        newData[iQuDuanFenTanReLiang] = Math.Round(distribution * 1000, 0).ToString("0");
                        sw.WriteLine(string.Join(",", newData));
                        content[j] = (string[])newData.Clone();
                    }
                    deltaHeat = heatDiff - heatTotalThisTime;

                }
            }
            return newFileName;
        }

    }
}
