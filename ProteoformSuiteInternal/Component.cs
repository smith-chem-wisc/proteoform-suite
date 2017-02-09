﻿using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace ProteoformSuiteInternal
{
    public class Component
    {
        // PROPERTIES
        public InputFile input_file { get; set; }
        public string id { get; set; } // deconvolution 4.0 assigns a component id. This is made unique by appending the inputFile id.
        public double reported_monoisotopic_mass { get; set; }  //from deconvolution 4.0
        public double intensity_reported { get; set; } //from deconvolution 4.0         
        public double intensity_sum_olcs { get; set; } = 0; //intensity sum for overlapping charge states -> determined when grouped into neucode pairs.
        public double delta_mass { get; set; }
        public double relative_abundance { get; set; }
        public double fract_abundance { get; set; }
        public string scan_range { get; set; }
        public string rt_range { get; set; }
        public double rt_apex { get; set; }
        public List<ChargeState> charge_states { get; set; } = new List<ChargeState>();
        public List<Component> incorporated_missed_monoisotopics = new List<Component>();

        private double _manual_mass_shift { get; set; } = 0; // added or substracted from weighted monoisotopic mass. This value is adjusted manually after observing ET histograms. Eventually also after see EE histograms. 
        public double manual_mass_shift
        {
            get { return _manual_mass_shift; }
            set
            {
                _manual_mass_shift = value;
                calculate_properties();
            }
        }
        public double neuCodeCorrection { get; set; } = 0;
        private int Num_charge_states { get; set; } = 0;
        public int num_charge_states
        {
            get
            {
                return Num_charge_states;
            }
            set
            {
                if (!calculating_properties && charge_states.Count > 0)
                    throw new ArgumentException("Charge state data exists that can't be overwritten with input");
                else
                    Num_charge_states = value;
            }
        }

        private double Intensity_sum { get; set; } = 0;
        public double intensity_sum //intensity sum for all charge states. Different value than what is reported by deconv 4.0 for some reason
        {
            get
            {
                return Intensity_sum; 
            }
            set
            {
                if (!calculating_properties && charge_states.Count > 0)
                    throw new ArgumentException("Charge state data exists that can't be overwritten with intensity input");
                else
                    Intensity_sum = value;
            }
        }

        private double Weighted_monoisotopic_mass { get; set; } = 0;
        public double weighted_monoisotopic_mass
        {
            get
            {
                return Weighted_monoisotopic_mass;
            }
            set
            {
                if (!calculating_properties && charge_states.Count > 0)
                    throw new ArgumentException("Charge state data exists that can't be overwritten with mass input");
                else
                    Weighted_monoisotopic_mass = value;
            }
        } //this is computed as the weighted sum of charge state masses.

        public bool calculating_properties { get; set; } = false;
        public double max_signal_to_noise { get; set; }
        

        public void attemptToSetWeightedMonoisotopic_mass(double fromFileMass)
        {
            if (charge_states.Select(cs => cs.charge_count).ToList().Count() > 0)
                throw new ArgumentException("Charge state data exists that can't be overwritten with input");
            else
                Weighted_monoisotopic_mass = fromFileMass;
        }

        public int num_detected_intervals { get; set; }
        public bool accepted { get; set; }


        // CONSTRUCTORS
        public Component()
        { }

        public Component(List<string> cellStrings, InputFile input_file) // this is used when we read stored data from previous computation.
        {
            this.id = Convert.ToInt32(cellStrings[0]).ToString();
            this.input_file = input_file;
            this.id = input_file.UniqueId.ToString() + "_" + Convert.ToInt32(cellStrings[0]);
            this.reported_monoisotopic_mass = Convert.ToDouble(cellStrings[1]);
            this.weighted_monoisotopic_mass = Convert.ToDouble(cellStrings[1]); // this will get immediately replaced and updated as charge states are added.
            this.intensity_reported = Convert.ToDouble(cellStrings[2]);
            this.num_charge_states = (Convert.ToInt32(cellStrings[3]));
            this.num_detected_intervals = Convert.ToInt32(cellStrings[4]);
            this.delta_mass = Convert.ToDouble(cellStrings[5]);
            this.relative_abundance = Convert.ToDouble(cellStrings[6]);
            this.fract_abundance = Convert.ToDouble(cellStrings[7]);
            this.scan_range = cellStrings[8];
            this.rt_range = cellStrings[9];
            this.rt_apex = Convert.ToDouble(cellStrings[10]);
            this.intensity_sum = Convert.ToDouble(cellStrings[2]); // this needs to be fixed.       
            this.accepted = true;
            this.charge_states = new List<ChargeState>();
        }

        public Component(Component c) // To open TSV files with saved Component data and duplicate components
        {
            this.input_file = c.input_file;
            this.id = c.id;
            this.reported_monoisotopic_mass = c.reported_monoisotopic_mass;
            this.weighted_monoisotopic_mass = c.weighted_monoisotopic_mass;
            //this.corrected_mass = c.corrected_mass;
            //this.manual_mass_shift = c.manual_mass_shift; //This messes up the corrected mass. Because we're not loading in charge states, the weighted monoisotopic mass is 0. This recalculates the corrected mass to 0.
            this.intensity_reported = c.intensity_reported;
            this.intensity_sum = c.intensity_sum;
            this.delta_mass = c.delta_mass;
            this.relative_abundance = c.relative_abundance;
            this.fract_abundance = c.fract_abundance;
            this.scan_range = c.scan_range;
            this.rt_apex = c.rt_apex;
            this.rt_range = c.rt_range;
            this.charge_states = c.charge_states;
            this.intensity_sum_olcs = c.intensity_sum_olcs;
            this.accepted = c.accepted;
            this.num_detected_intervals = c.num_detected_intervals;
            if (c.charge_states.Count > 0) this.charge_states = c.charge_states;
            else this.num_charge_states = c.num_charge_states;
            this.calculate_properties();
        }


        // METHODS
        public void add_charge_state(List<string> charge_row)
        {
            this.charge_states.Add(new ChargeState(charge_row));
        }

        public void calculate_properties()
        {
            if (charge_states.Count > 0)
            {
                this.calculating_properties = true;
                this.intensity_sum = charge_states.Select(cs => cs.intensity).Sum();
                this.weighted_monoisotopic_mass = this.charge_states.Select(charge_state => charge_state.intensity / this.intensity_sum * charge_state.calculated_mass).Sum() + manual_mass_shift + neuCodeCorrection;
                this.num_charge_states = charge_states.Count;
                //put here?
                this.calculating_properties = false;
            }
        }

        public double calculate_sum_intensity_olcs(List<int> charges_to_sum)
        {
            this.intensity_sum_olcs = this.charge_states.Where(cs => charges_to_sum.Contains(cs.charge_count)).Select(charge_state => charge_state.intensity).Sum();
            return this.intensity_sum_olcs;
        }

        public Component mergeTheseComponents(Component cpToMerge) //this method is used just after initial read of components to get rid of missed monoisotopics in the same scan.
        {
            //Note: the max missed monoisotopics is hard coded for now. Need more analysis to see whether this should be subject to user adjustment.
            if (Math.Abs((this.weighted_monoisotopic_mass - cpToMerge.weighted_monoisotopic_mass)) <= (3*Lollipop.MONOISOTOPIC_UNIT_MASS + Math.Max(this.weighted_monoisotopic_mass, cpToMerge.weighted_monoisotopic_mass)/1000000d*(double)Lollipop.mass_tolerance))
            {// we're merging missed monoisotopics
                foreach (ChargeState cpCS in cpToMerge.charge_states)
                {
                    if (cpCS.calculated_mass > this.weighted_monoisotopic_mass)
                    {
                        double monoIsoTopicDifference = Math.Round(cpCS.calculated_mass - this.weighted_monoisotopic_mass) * Lollipop.MONOISOTOPIC_UNIT_MASS;
                        cpCS.calculated_mass = cpCS.calculated_mass - monoIsoTopicDifference;
                        cpCS.mz_centroid = (cpCS.calculated_mass + Lollipop.PROTON_MASS * cpCS.charge_count) / cpCS.charge_count;
                    }
                    else
                    {
                        double monoIsoTopicDifference = Math.Round(this.weighted_monoisotopic_mass - cpCS.calculated_mass) * Lollipop.MONOISOTOPIC_UNIT_MASS;
                        cpCS.calculated_mass = cpCS.calculated_mass + monoIsoTopicDifference;
                        cpCS.mz_centroid = (cpCS.calculated_mass + Lollipop.PROTON_MASS * cpCS.charge_count) / cpCS.charge_count;
                    }
                    if (this.charge_states.Exists(thisCS => thisCS.charge_count == cpCS.charge_count))
                        this.charge_states.Where(thisCS => thisCS.charge_count == cpCS.charge_count).First().mergeTheseChargeStates(cpCS);
                    else
                        this.charge_states.Add(cpCS);

                    this.calculate_properties();
                }
            }
            else // we're merging harmonics
            {
                foreach (ChargeState cpCS in cpToMerge.charge_states)
                {
                    if(cpCS.calculated_mass > this.weighted_monoisotopic_mass)
                    {
                        double harmonicRatio = Math.Round(cpCS.calculated_mass / this.weighted_monoisotopic_mass);// this should be 2 or 3
                        cpCS.charge_count = (int)(cpCS.charge_count / harmonicRatio);//charge counts of harmonics are doubled or tripled and need to be lowered
                        cpCS.calculated_mass = cpCS.calculated_mass / harmonicRatio;
                        cpCS.calculated_mass = cpCS.calculated_mass - (Math.Round(cpCS.calculated_mass - this.weighted_monoisotopic_mass)) * Lollipop.MONOISOTOPIC_UNIT_MASS;
                        cpCS.mz_centroid = (cpCS.calculated_mass + Lollipop.PROTON_MASS * cpCS.charge_count) / cpCS.charge_count;
                    }
                    else
                    {
                        double harmonicRatio = Math.Round(this.weighted_monoisotopic_mass/cpCS.calculated_mass);// this should be 2 or 3
                        cpCS.charge_count = (int)(cpCS.charge_count * harmonicRatio);//charge counts of harmonics are halved or thirded and need to be raised
                        cpCS.calculated_mass = cpCS.calculated_mass * harmonicRatio;
                        cpCS.calculated_mass = cpCS.calculated_mass - (Math.Round(cpCS.calculated_mass - this.weighted_monoisotopic_mass)) * Lollipop.MONOISOTOPIC_UNIT_MASS;
                        cpCS.mz_centroid = (cpCS.calculated_mass + Lollipop.PROTON_MASS * cpCS.charge_count) / cpCS.charge_count;
                    }
                    if (this.charge_states.Exists(thisCS => thisCS.charge_count == cpCS.charge_count))
                        this.charge_states.Where(thisCS => thisCS.charge_count == cpCS.charge_count).First().mergeTheseChargeStates(cpCS);
                    else
                        this.charge_states.Add(cpCS);

                    this.calculate_properties();
                }
            }
            return this;
        }


        // TESTING
        public void add_charge_state(ChargeState cs)
        {
            this.charge_states.Add(new ChargeState(cs));
        }
    }

    public class ChargeState
    {
        public int charge_count { get; set; } //value from deconv 4.0
        public double intensity { get; set; } //value from deconv 4.0
        public double mz_centroid { get; set; } 
        public double calculated_mass { get; set; }  // the value reported by decon 4.0 is incorrect, so we calculate it from m/z and charge (including correction when necessary)
        public double signal_to_noise { get; set; }  //intensity of average isotope peak / noise at that peak

        public ChargeState(List<string> charge_row) //the correction used is determined from measurement of lock-mass compound. It is read in at the same time the data is read in. We do not keep track of the correction because it adds confusion when charge states are combined.
        {
            this.charge_count = Convert.ToInt32(charge_row[0]);
            this.intensity = Convert.ToDouble(charge_row[1]);
            this.mz_centroid = Convert.ToDouble(charge_row[2]); //no point to keeping the uncorrected mz if there is a correction.
            this.calculated_mass = correct_calculated_mass();
        }

        public ChargeState(ChargeState cs)
        {
            this.charge_count = cs.charge_count;
            this.intensity = cs.intensity;
            this.mz_centroid = cs.mz_centroid;
            this.calculated_mass = cs.calculated_mass;
        }

        //For testing
        public ChargeState(int charge_count, double intensity, double mz_centroid, double mz_correction)
        {
            this.charge_count = charge_count;
            this.intensity = intensity;
            this.mz_centroid = correct_calculated_mz(mz_centroid, mz_correction);
            this.calculated_mass = correct_calculated_mass();
        }

        public double correct_calculated_mz(double mz, double mz_correction) // the correction is a linear shift to m/z
        {
            return (mz + mz_correction);//Thermo deconvolution 4.0 miscalculates the monoisotopic mass from the reported mz and charge state values.
        }

        public double correct_calculated_mass() // the correction is a linear shift to m/z
        {
            return (this.charge_count * this.mz_centroid - this.charge_count * 1.00727645D);//Thermo deconvolution 4.0 miscalculates the monoisotopic mass from the reported mz and charge state values.
        }


        public ChargeState mergeTheseChargeStates(ChargeState csToMerge)
        {
            if(csToMerge != null)
            {
                double totalIntensity = this.intensity + csToMerge.intensity;
                this.calculated_mass = (this.intensity * this.calculated_mass + csToMerge.intensity * csToMerge.calculated_mass) /totalIntensity; 
                this.mz_centroid = (this.calculated_mass + 1.00727645D * this.charge_count) /this.charge_count;
                this.intensity = totalIntensity;
            }
            return this;
        }

        public override string ToString()
        {
            return String.Join("\t", new List<string> { charge_count.ToString(), intensity.ToString() });
        }
    }
}
