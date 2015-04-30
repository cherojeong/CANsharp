using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CANsharp
{
    public class Message
    {
        public uint Id { get; set; }
        public String Name { get; set; }
        public uint Dlc { get; set; }
        public Dictionary<String, int> Position;
        public List<String> Signals { get; set; }
        public byte[] Array;

        public Message(uint id, String name, uint dlc, Dictionary<String, int> position)
        {
            this.Id = id;
            this.Name = name;
            this.Dlc = dlc;
            this.Position = position;
            this.Signals = new List<String>();
            Array = new byte[dlc];
            for (int i = 0; i < dlc; i++)
            {
                Array[i] = 0 & 0x00;
            }
        }

        public void Update(Signal signal)
        {
            // 0 ~ 63 (depends on message dlc)
            int start = Position[signal.Name];
            // 0 ~ 7 (depends on message dlc)
            //int startRow = Convert.ToInt32(Math.Ceiling(start / 8.0));
            int startRow = start / 8;
            // 0 ~ 7 (always)
            int startCol = start % 8;
            // 0 ~ 63 (depends on signal)
            int length = signal.Length;

            //Console.WriteLine(String.Format("Updating message about signal {0}, vlaue = {1}", signal, signal.GetValue()));

            if (signal.Order.Equals(ByteOrder.Motorola))
            {
                if (length <= startCol + 1) 
                {
                    byte newRowValue = getNewRowValue(startCol, Array[startRow], length, Convert.ToInt32(signal.GetValue()));
                    Array[startRow] = newRowValue;
                    //Console.WriteLine("update message for " + signal.Name + " => " + Convert.ToString(newRowValue, 2));
                }
                else
                {
                    // 2 ~ 8
                    int arraySize = Convert.ToInt32(Math.Ceiling((length - (startCol + 1)) / 8.0)) + 1;
                    int remaining = length - (startCol + 1);

                    int firstValue = Convert.ToInt32(signal.GetValue() >> (remaining)) & 0xFF;
                    byte newRowValue = getNewRowValue(startCol, Array[startRow], startCol, firstValue);
                    Array[startRow] = (byte)firstValue;
                    //Console.WriteLine("update message for " + signal.Name + " (0) => " + Convert.ToString(Array[startRow], 2));

                    for (int i = 1; i < arraySize; i++)
                    {
                        int signalValue;

                        if (remaining > 8)
                        {
                            remaining -= 8;
                            signalValue = Convert.ToInt32(signal.GetValue() >> remaining) & 0xFF;
                            Array[i] = getNewRowValue(7, Array[i], 8, signalValue);
                        }
                        else
                        {
                            signalValue = Convert.ToInt32(signal.GetValue() ) & Convert.ToByte(Math.Pow(2.0, remaining) - 1);
                            Array[i] = getNewRowValue(7, Array[i], remaining, signalValue);
                            remaining = 0;
                        }
                        //Console.WriteLine("update message for " + signal.Name + " (" + i + ") => " + Convert.ToString(Array[i], 2));
                    }
                } 
            } 
            else if (signal.Order.Equals(ByteOrder.Intel)) 
            {
                /*
                 * getNewRowValue() method is designed for Motorola bits order
                 */
                int startColForIntel = (startCol + length) - 1;
                if ((startCol + length) <= 8)
                {
                    byte newRowValue = getNewRowValue(startColForIntel, Array[startRow], length, Convert.ToInt32(signal.GetValue()));
                    Array[startRow] = newRowValue;
                    //Console.WriteLine("update message for " + signal.Name + " (" + startRow + ") => " + Convert.ToString(newRowValue, 2) + " (Intel)");
                }
                else
                {
                    // 2 ~ 63
                    int end = start + length - 1;
                    // 1 ~ 8
                    int firstRowBits = (8 - startCol);
                    // 2 ~ 8
                    int arraySize = Convert.ToInt32(Math.Ceiling((end - firstRowBits) / 8.0)) + 1;

                    int firstValue = Convert.ToInt32(signal.GetValue() << startCol) & 0xFF;
                    byte newRowValue = getNewRowValue(startCol, Array[startRow], startColForIntel, firstValue);
                    Array[startRow] = (byte)firstValue;

                    //Console.WriteLine("update message for " + signal.Name + " (" + startRow + ") => " + Convert.ToString(Array[startRow], 2) + " (Intel)");

                    int sum = firstRowBits;

                    for (int i = 1; i < arraySize; i++)
                    {
                        int signalValue;

                        if (length - sum > 8)
                        {
                            signalValue = Convert.ToInt32(signal.GetValue() >> sum) & 0xFF;
                            Array[i] = getNewRowValue(7, Array[i], 8, signalValue);
                            sum += 8;
                        }
                        else
                        {
                            int remaining = length - sum;
                            signalValue = Convert.ToInt32(signal.GetValue() >> sum) & 0xFF;
                            Array[i] = getNewRowValue(remaining - 1, Array[i], remaining, signalValue);
                            sum += remaining;
                        }
                        //Console.WriteLine("update message for " + signal.Name + " (" + (startRow + i) + ") => " + Convert.ToString(Array[i], 2) + " (Intel)");
                    }
                }
            }
        }

        private byte getNewRowValue(int startCol, int originalValue, int length, int signalValue)
        {
            int shift = startCol + 1 - length;
            byte value = (byte)((signalValue << shift) & 0xFF);

            byte before = (byte)Convert.ToInt32(Math.Pow(2.0, shift) - 1);

            byte middle = (byte)(Convert.ToInt32(Math.Pow(2.0, length) - 1) << shift);
            byte after = (byte)Convert.ToByte((Math.Pow(2.0, 8) - 1) - (middle + before));

            int formattedValue = (originalValue & before) + (value & middle) + (originalValue & after);
            return (byte)formattedValue;
        }

        public String ToString()
        {
            return String.Format("BO_ {0} {1}: {2} ", Id, Name, Dlc);
        }

        public String GetArrayString()
        {
            String s = "";
            foreach (byte b in Array)
            {
                s += b + "\n";
            }
            return s;
        }
    }
}
