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

        private int iUploadTime = 3;
        private int iHeat = 13;
        private int iMeterId = 24;

        List<string[]> heater = new List<string[]>();
        public Dictionary<DateTime, int> Result = new Dictionary<DateTime, int>();

        public void ReadHeater(string fileName)
        {
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
                    log.Error(string.Format("Parse failed: [{0}], [{1}]", temp[iUploadTime], temp[iHeat]));
                }
            }
        }
    }
}
