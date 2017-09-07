using System;

//Class for a Pred object. 

namespace RealwearImageAnalysis
{
    public class Pred
    {
        //Object representing each classification from custom vision.
        //Pred_String is the name of the tag
        public string Pred_String { get; set; }

        //Pred_Double is the probability associated with the Pred_String tag 
        public double Pred_Double { get; set; }

        //stores the row and column coordinates for the subimage
        public Tuple<int, int> Coordinates { get; set; }

        public Pred(string str, double db, int r, int c)
        {
            Pred_String = str;
            Pred_Double = db;
            Coordinates = new Tuple<int, int>(r,c);

        }
    }
}