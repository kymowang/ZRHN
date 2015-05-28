using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualDataBuilder
{
    using System.IO;
    using System.Linq;
    using System.Runtime.Remoting.Messaging;
    using System.Threading;

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

        private int iCurrentOpening = 13;

        List<string[]> controller1Content = new List<string[]>();
        List<string[]> controller2Content = new List<string[]>();
        private ILog log = LogManager.GetLogger(typeof(Builder));


        public string Build(string controllerFile1, string heaterFile)
        {
            //read controller file
            string[] tempContent = File.ReadAllLines(controllerFile1, Encoding.Default);
            int maxId = 0;
            for (int i = 1; i < tempContent.Length; i++)
            {
                if (string.IsNullOrEmpty(tempContent[i])) continue;
                string[] temp = tempContent[i].Split(new[] { ',' });

                maxId = int.Parse(temp[0]);
                this.controller1Content.Add(temp);
                int roomSize;
                if (int.TryParse(temp[iRoomSize], out roomSize))
                {
                    roomSizeTotal += roomSize;
                }
                else
                {
                    log.Error("Parse room size failed: [" + temp[iRoomSize] + "]");
                }
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
                int runTime = int.Parse(this.controller1Content[0][iRunTime]);
                int totalTime = int.Parse(this.controller1Content[0][this.iTotalTime]);
                int deltaHeat = 0;
                for (int i = 1; i < hr.Result.Count; i++)
                {
                    var heat1 = hr.Result.Keys.ElementAt(i - 1);
                    var heat2 = hr.Result.Keys.ElementAt(i);
                    int heatDiff = hr.Result[heat2] - hr.Result[heat1];
                    if (heatDiff < 0)
                    {
                        throw new InvalidDataException("热量表热量错误, 时间：" + heat2);
                    }
                    heatDiff += deltaHeat;
                    int heatTotalThisTime = 0;
                    TimeSpan timeDiff = heat2 - heat1;

                    for (int j = 0; j < this.controller1Content.Count; j++)
                    {
                        string[] newData = (string[])this.controller1Content[j].Clone();
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
                        newData[iTotalQuantityHeat] = (int.Parse(this.controller1Content[j][iTotalQuantityHeat]) + changedHeat).ToString();
                        newData[iQuDuanFenTanReLiang] = Math.Round(distribution * 1000, 0).ToString("0");
                        sw.WriteLine(string.Join(",", newData));
                        //sw.WriteLine(Encoding.Default.GetString(Encoding.UTF8.GetBytes(string.Join(",", newData))));
                        this.controller1Content[j] = (string[])newData.Clone();
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
            int[] controllerChangedHeat = new int[tempContent1.Length];

            int maxId = 0;
            for (int i = 1; i < tempContent1.Length; i++)
            {
                if (string.IsNullOrEmpty(tempContent1[i])) continue;
                string[] temp1 = tempContent1[i].Split(new[] { ',' });
                string[] temp2 = tempContent2[i].Split(new[] { ',' });

                maxId = int.Parse(temp1[0]);
                this.controller1Content.Add(temp1);
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
                controllerChangedHeat[i - 1] = heat2 - heat1;
                if (controllerChangedHeat[i - 1] < 0)
                {
                    throw new InvalidDataException("控制器热量变小, id： [" + tempContent1[i][0] + "]");
                }
                changedHeatTotal += controllerChangedHeat[i - 1];
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
                int runTime = int.Parse(this.controller1Content[0][iRunTime]);
                int totalTime = int.Parse(this.controller1Content[0][this.iTotalTime]);
                int deltaHeat = 0;

                double[] remainHeat = new double[this.controller1Content.Count];
                for (int i = 1; i < hr.Result.Count; i++)
                {
                    var heat1 = hr.Result.Keys.ElementAt(i - 1);
                    var heat2 = hr.Result.Keys.ElementAt(i);
                    int heatDiff = hr.Result[heat2] - hr.Result[heat1];// + deltaHeat;

                    if (heatDiff < 0)
                    {
                        throw new InvalidDataException("热量表热量错误, 时间：" + heat2);
                    }
                    int heatTotalThisTime = 0;
                    TimeSpan timeDiff = heat2 - heat1;

                    for (int j = 0; j < this.controller1Content.Count; j++)
                    {
                        string[] newData = (string[])this.controller1Content[j].Clone();
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
                        double distribution = heatDiff * (controllerChangedHeat[j] * 1.0) / changedHeatTotal;
                        int changedHeat = (int)Math.Floor(distribution);

                        remainHeat[j] += distribution - changedHeat;//热量的小数部分
                        int fixHeat = 0;
                        if (remainHeat[j] > 1) fixHeat = (int)Math.Floor(remainHeat[j]);
                        remainHeat[j] = remainHeat[j] - fixHeat;

                        heatTotalThisTime += changedHeat;
                        newData[iTotalQuantityHeat] = (int.Parse(this.controller1Content[j][iTotalQuantityHeat]) + changedHeat + fixHeat).ToString();
                        newData[iQuDuanFenTanReLiang] = Math.Round(distribution * 1000, 0).ToString("0");
                        sw.WriteLine(string.Join(",", newData));
                        this.controller1Content[j] = (string[])newData.Clone();
                    }
                    deltaHeat = heatDiff - heatTotalThisTime;

                }
            }
            return newFileName;
        }

        public string Build2(string controllerFile1, string controllerFile2, string heaterFile)
        {
            //read controller file
            string[] tempContent1 = File.ReadAllLines(controllerFile1, Encoding.Default);
            string[] tempContent2 = File.ReadAllLines(controllerFile2, Encoding.Default);
            if (tempContent1.Length != tempContent2.Length)
            {
                log.Error("控制器开始数据和结束数据有差异");
                throw new InvalidDataException("控制器开始数据和结束数据有差异");
            }
            int[] controllerChangedHeat = new int[tempContent1.Length];

            int maxId = 0;
            for (int i = 1; i < tempContent1.Length; i++)
            {
                if (string.IsNullOrEmpty(tempContent1[i])) continue;
                string[] temp1 = tempContent1[i].Split(new[] { ',' });
                string[] temp2 = tempContent2[i].Split(new[] { ',' });

                //maxId = int.Parse(temp1[0]);
                controller1Content.Add(temp1);
                controller2Content.Add(temp2);
                int roomSize;
                if (int.TryParse(temp1[iRoomSize], out roomSize))
                {
                    roomSizeTotal += roomSize;
                }
                else
                {
                    log.Error("Parse room size failed: [" + temp1[iRoomSize] + "]");
                }

                //verify heat quantity
                int heat1 = int.Parse(temp1[iTotalQuantityHeat]);
                int heat2 = int.Parse(temp2[iTotalQuantityHeat]);
                controllerChangedHeat[i - 1] = heat2 - heat1;
                if (controllerChangedHeat[i - 1] < 0)
                {
                    throw new InvalidDataException("控制器热量变小, id： [" + tempContent1[i][0] + "]");
                }
                //changedHeatTotal += controllerChangedHeat[i - 1];//
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
                int runTime = int.Parse(this.controller1Content[0][iRunTime]);
                int totalTime = int.Parse(this.controller1Content[0][this.iTotalTime]);
                int deltaHeat = 0;

                double[] remainHeat = new double[this.controller1Content.Count];
                bool[] passCalc = new bool[controller1Content.Count];

                //init
                for (int i = 0; i < passCalc.Length; i++)
                {
                    passCalc[i] = false;
                } int thisRoomSizeTotal = roomSizeTotal;
                for (int i = 1; i < hr.Result.Count; i++)
                {
                    var heat1 = hr.Result.Keys.ElementAt(i - 1);
                    var heat2 = hr.Result.Keys.ElementAt(i);
                    int heatDiff = hr.Result[heat2] - hr.Result[heat1];// + deltaHeat;

                    if (heatDiff < 0)
                    {
                        throw new InvalidDataException("热量表热量错误, 时间：" + heat2);
                    }
                    int heatTotalThisTime = 0;
                    TimeSpan timeDiff = heat2 - heat1;
                    
                   
                    for (int j = 0; j < this.controller1Content.Count; j++)
                    {
                        int temperatureDelta = new Random(GetRandomSeed()).Next(8, 13);
                        //Thread.Sleep(1);
                       
                        //log.Debug(temperatureDelta);
                        string[] newData = (string[])this.controller1Content[j].Clone();
                        runTime = int.Parse(controller1Content[j][iRunTime])+timeDiff.Hours;
                        if(runTime<int.Parse(controller2Content[j][iRunTime])) newData[iRunTime] = runTime.ToString();
                        else
                        {
                            newData[iRunTime] = controller2Content[j][iRunTime];
                        }
                        totalTime = int.Parse(controller1Content[j][iTotalTime])+timeDiff.Hours;
                        if (totalTime < int.Parse(controller2Content[j][iTotalTime])) newData[iTotalTime] = totalTime.ToString();
                        else
                        {
                            newData[iTotalTime] = controller2Content[j][iTotalTime];
                        }
                        //newData[0] = (++maxId).ToString();
                        newData[1] = heat2.ToString("yyyy/M/d H:mm");
                      
                        //newData[iRoomTemperature] = "0";
                        //newData[iSetTemperature] = "0";
                        //newData[iShangQuDuanKaiQiShiJian] = "2";
                        //newData[iShangQuDuanZongShiJian] = "2";
                        //newData[iShangQuDuanKaiDu] = "1000";
                        //newData[iShangQuDuanPingJunWenDu] = "1000";
                        double distribution = heatDiff * (int.Parse(newData[iRoomSize]) * 1.0) / thisRoomSizeTotal;
                        int changedHeat = (int)Math.Floor(distribution);

                        remainHeat[j] += distribution - changedHeat;//热量的小数部分
                        int fixHeat = 0;
                        if (remainHeat[j] > 1) fixHeat = (int)Math.Floor(remainHeat[j]);
                        remainHeat[j] = remainHeat[j] - fixHeat;
                        //log.Debug(fixHeat + " ： "+remainHeat[j]);


                        heatTotalThisTime += changedHeat;
                        if (newData[iSetTemperature] == "0") { newData[iRoomTemperature] = "0"; }
                        else
                        {
                            int currentTemperature = int.Parse(newData[iSetTemperature]);
                            if (!passCalc[j])
                            { newData[iRoomTemperature] = (currentTemperature - temperatureDelta).ToString(); }
                            else
                            {
                                newData[iRoomTemperature] = (currentTemperature + temperatureDelta).ToString();
                            }
                        }


                        if (!passCalc[j])
                        {
                            newData[iTotalQuantityHeat] =
                                (int.Parse(this.controller1Content[j][iTotalQuantityHeat]) + changedHeat + fixHeat).ToString();
                            log.Debug(j + " : " + changedHeat + " : " + fixHeat + " : " + newData[iTotalQuantityHeat]);
                            if (int.Parse(newData[iTotalQuantityHeat])
                                >= int.Parse(controller2Content[j][iTotalQuantityHeat]))
                            {
                                newData[iTotalQuantityHeat] = controller2Content[j][iTotalQuantityHeat];
                                thisRoomSizeTotal -= int.Parse(newData[iRoomSize]);
                                passCalc[j] = true;
                            }

                            newData[iQuDuanFenTanReLiang] = Math.Round(distribution * 1000, 0).ToString("0");
                            newData[iCurrentOpening] = "1";

                        }
                        else
                        {
                            newData[iCurrentOpening] = "0";
                        }
                        sw.WriteLine(string.Join(",", newData));

                        this.controller1Content[j] = (string[])newData.Clone();
                    }
                    //deltaHeat = heatDiff - heatTotalThisTime;

                }
            }
            return newFileName;
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
