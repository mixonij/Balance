using System;

namespace Lab7.Models
{
    public class OutputData
    {
        public double[] X { get; set; }
        public double DisbalanceOriginal { get; set; }
        public double Disbalance { get; set; }
        public TimeSpan TimePreparation { get; set; }
        public TimeSpan TimeCalculation { get; set; }
    }
}
