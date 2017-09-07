using System;

namespace RealwearImageAnalysis
{
    public class Pred
    {
        public string Pred_String { get; set; }
        public double Pred_Double { get; set; }

        public Tuple<int, int> Coordinates { get; set; }

        public Pred(string str, double db, int r, int c)
        {
            Pred_String = str;
            Pred_Double = db;
            Coordinates = new Tuple<int, int>(r, c);

        }
    }
}