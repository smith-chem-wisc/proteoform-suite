﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProteoformSuiteInternal
{
    public class Envelope
    {
        public int scan_num;
        public int charge;
        public double abundance;
        public double mz;
        public double fit;
        public double monoisotopicMass;
        public int FeatureID;
        public double retentionTime;

        public enum DeconType { mzLib, ProMex }

        public Envelope(string line, DeconType type)
        {
            if (type == DeconType.ProMex)
            {
                var split = line.Split(',');

                scan_num = int.Parse(split[0]);
                charge = int.Parse(split[1]);
                abundance = double.Parse(split[2]);
                mz = double.Parse(split[3]);
                fit = double.Parse(split[4]);
                monoisotopicMass = double.Parse(split[5]);
                FeatureID = int.Parse(split[6]);
            }
            else if (type == DeconType.mzLib)
            {
                var split = line.Split('\t');

                scan_num = int.Parse(split[0]);
                charge = int.Parse(split[1]);
                abundance = double.Parse(split[2]);
                mz = double.Parse(split[3]);
                fit = double.Parse(split[4]);
                monoisotopicMass = double.Parse(split[6]);
                FeatureID = int.Parse(split[7]);
            }
        }
    }
}
