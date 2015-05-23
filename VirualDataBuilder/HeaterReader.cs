using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VirtualDataBuilder
{
    using System.IO;

    using log4net;

    public class HeaterReader
    {
        private ILog log = LogManager.GetLogger(typeof(HeaterReader));

        public int iUploadTime = 3;
        private int iHeat = 13;
        private int iMeterId = 24;

        List<string[]> heater = new List<string[]>();
        public Dictionary<DateTime, int> Result = new Dictionary<DateTime, int>();
        private Dictionary<DateTime, int> verify = new Dictionary<DateTime, int>();
        public void ReadHeater(string fileName)
        {
            DateTime preTime = DateTime.Now;
            string[] contents = File.ReadAllLines(fileName, Encoding.Default);
            for (int i = 1; i < contents.Length; i++)
            {
                var line = contents[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                string[] temp = line.Split(new[] { ',' });
                //heater.Add(temp);
                DateTime time;
                int heat;
                if (DateTime.TryParse(temp[iUploadTime], out time) && int.TryParse(temp[this.iHeat], out heat))
                {
                    if (time != preTime)
                    {
                        double timeDiff = Math.Abs((preTime - time).TotalMinutes);
                        if (timeDiff < 5) 
                            time = preTime;
                    }
                    if (this.Result.ContainsKey(time))
                    {
                        this.Result[time] += heat;
                    }
                    else
                    {
                        this.Result.Add(time, heat);
                        preTime = time;
                    }
                    if (verify.ContainsKey(time)) verify[time]++;
                    else verify.Add(time, 1);
                }
                else
                {
                    log.Error(string.Format("Parse failed: [{0}], [{1}]", temp[iUploadTime], temp[iHeat]));
                }
            }

            //verify
            int count = verify.ElementAt(0).Value;
            foreach (var item in verify)
            {
                if(item.Value!=count)
                    throw new InvalidDataException("热表文件数据错误，请检查上传时间！");
            }
        }
    }
}
