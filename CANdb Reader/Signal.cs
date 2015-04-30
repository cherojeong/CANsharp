using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CANsharp
{
    public class Signal
    {

        public String Name { get; set; }
        public bool Signed { get; set; }
        public ByteOrder Order { get; set; }
        public int Length { get; set; }
        private double factor { get; set; }
        public double Factor
        {
            get { return this.factor; }
            set
            {
                if (value == 0)
                {
                    this.factor = 1.0;
                }
                else
                {
                    this.factor = value;
                }
            }
        }

        public double Offset { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }

        private double value = 0.0;
        public double Value 
        {
            get { return this.value; }
            set
            {
                if (value > Max)
                {
                    this.value = Max;
                }
                else if (value < Min)
                {
                    this.value = Min;
                }
                else
                {
                    this.value = value;
                }
            }
        }

        public Signal(String name, bool signed, ByteOrder order, int length, double factor, double offset, double min, double max)
        {
            this.Name = name;
            this.Signed = signed;
            this.Order = order;
            this.Length = length;
            this.Factor = factor;
            this.Offset = offset;
            if (min == max)
            {
                if (signed)
                {
                    this.Max = Math.Pow(2.0, length - 1) - 1;
                    this.Min = Math.Pow(2.0, length - 1) * (-1);
                }
                else
                {
                    this.Max = Math.Pow(2.0, length) - 1;
                    this.Min = 0.0;
                }
            }
            else
            {
                this.Min = min;
                this.Max = max;
            }
            Debug.WriteLine("min = " + this.Min + ", max = " + this.Max);
        }

        public long GetValue()
        {
            int multiplier = Convert.ToInt32((Factor == 0) ? 1 : 1 / Factor);
            return Convert.ToInt64(Value * multiplier + Offset);
        }

        public String ToString()
        {
            String output = String.Format("SG_ {0} {1}@{2}{3} ({4},{5}) [{6}|{7}]", Name, Length, Order, (Signed)? "+": "-", Factor, Offset, Min, Max);
            return output;
        }
    }
}
