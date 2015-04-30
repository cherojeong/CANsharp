using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CANsharp
{
    public class Parser
    {
        public CANdb read(String filePath)
        {
            CANdb candb = new CANdb();

            Message currentMessage = null;

            String line;
            System.IO.StreamReader file = new System.IO.StreamReader(filePath);
            while ((line = file.ReadLine()) != null)
            {
                if (line.StartsWith("BO_"))
                {
                    String[] array = line.Split(' ');
                    uint id = Convert.ToUInt32(array[1]);
                    String name = array[2].Remove(array[2].Length - 1);
                    uint dlc = Convert.ToUInt32(array[3]);

                    Message msg = new Message(id,name,dlc,new Dictionary<string,int>());
                    currentMessage = msg;
                    candb.messages.Add(name,msg);
                } else if (line.Trim().StartsWith("SG_") && currentMessage != null) {
                    String[] array = line.Trim().Split(' ');
                    String name = array[1];

                    String[] plos = array[3].Split('@'); //["7|16" , "0+"]
                    bool signed = (plos[1].Contains("-"))? true: false;
                    ByteOrder order = (plos[1].Contains("0"))? ByteOrder.Motorola: ByteOrder.Intel;
                    int length = Convert.ToInt32(plos[0].Split('|')[1]);
                    int position = Convert.ToInt32(plos[0].Split('|')[0]);

                    String[] faof = array[4].Substring(1, (array[4].Length - 2)).Split(',');
                    double factor = Convert.ToDouble(faof[0]);
                    double offset = Convert.ToDouble(faof[1]);

                    String[] mima = array[5].Substring(1, (array[5].Length - 2)).Split('|');
                    double min = Convert.ToDouble(mima[0]);
                    double max = Convert.ToDouble(mima[1]);

                    Signal sgn = new Signal(name, signed, order, length, factor, offset, min, max);
                    candb.signals.Add(name,sgn);
                    candb.AddOnSignalToMessages(sgn,currentMessage);
                    candb.AddPositionToMessage(sgn, currentMessage, position);
                }
            }

            return candb;
        }
    }
}
