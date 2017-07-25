﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proteomics;

namespace ProteoformSuiteInternal
{

    public class TopDownHit
    {
        public int ms2ScanNumber { get; set; }
        public double ms2_retention_time { get; set; }
        public string filename { get; set; }
        public string uniprot_id { get; set; }
        public string sequence { get; set; }
        public int start_index { get; set; } //position one based
        public int stop_index { get; set; } //position one based
        public List<Ptm> ptm_list { get; set; } = new List<Ptm>(); //position one based. this list is empty if unmodified.
        public double theoretical_mass { get; set; }
        public string accession { get; set; }
        public string name { get; set; }
        public double pvalue { get; set; }
        public double reported_mass { get; set; } //reported in TD results file
        public double score { get; set; }//C-score
        public TopDownResultType tdResultType { get; set; }
        public InputFile file { get; set; }
        public string pfr { get; set; }

        //for mass calibration
        public int biological_replicate { get; set; } = 0;
        public int technical_replicate { get; set; } = 0;
        public int fraction { get; set; } = 0;
        public double mz { get; set; }
        public int charge { get; set; }
        public double ms1_retention_time { get; set; }


        public TopDownHit(Dictionary<char, double> aaIsotopeMassList, InputFile file, TopDownResultType tdResultType, string accession, string pfr, string uniprot_id, string name, string sequence, int start_index, int stop_index, List<Ptm> modifications, double reported_mass, double theoretical_mass, int scan, double retention_time, string filename, double pvalue, double score)
        {
            this.pfr = pfr;
            this.file = file;
            this.tdResultType = tdResultType;
            this.accession = accession;
            this.uniprot_id = uniprot_id;
            this.name = name;
            this.sequence = sequence;
            this.start_index = start_index;
            this.stop_index = stop_index;
            this.ptm_list = modifications;
            this.reported_mass = reported_mass;
            this.theoretical_mass = CalculateProteoformMass(sequence, aaIsotopeMassList) + ptm_list.Sum(p => p.modification.monoisotopicMass);
            this.ms2ScanNumber = scan;
            this.ms2_retention_time = retention_time;
            this.filename = filename;
            this.score = score;
            this.pvalue = pvalue;
        }

        public TopDownHit(TopDownHit h)
        {
            this.file = h.file;
            this.tdResultType = h.tdResultType;
            this.accession = h.accession;
            this.uniprot_id = h.uniprot_id;
            this.name = h.name;
            this.sequence = h.sequence;
            this.start_index = h.start_index;
            this.stop_index = h.stop_index;
            this.ptm_list = h.ptm_list;
            this.reported_mass = h.reported_mass;
            this.theoretical_mass = h.theoretical_mass;
            this.ms2ScanNumber = h.ms2ScanNumber; ;
            this.ms2_retention_time = h.ms2_retention_time;
            this.filename = h.filename;
            this.pvalue = pvalue;
            this.score = h.score;
        }

        public TopDownHit()
        {

        }

        private double CalculateProteoformMass(string pForm, Dictionary<char, double> aaIsotopeMassList)
        {
            double proteoformMass = 18.010565; // start with water
            char[] aminoAcids = pForm.ToCharArray();
            List<double> aaMasses = new List<double>();
            for (int i = 0; i < pForm.Length; i++)
            {
                //for top-down do unlabeled K whether or not neucode labeled intact mass data loaded in...
                if (aminoAcids[i] == 'K') aaMasses.Add(128.094963);
                else if (aaIsotopeMassList.ContainsKey(aminoAcids[i])) aaMasses.Add(aaIsotopeMassList[aminoAcids[i]]);
            }
            return proteoformMass + aaMasses.Sum();
        }

        //calibration
        public string GetSequenceWithChemicalFormula()
        {

            var sbsequence = new StringBuilder();

            // variable modification on peptide N-terminus
            ModificationWithMass pep_n_term_variable_mod = ptm_list.Where(p => p.position == 1).Select(m => m.modification).FirstOrDefault();
            if (pep_n_term_variable_mod != null)
            {
                var jj = pep_n_term_variable_mod as ModificationWithMassAndCf;
                if (jj != null && Math.Abs(jj.chemicalFormula.MonoisotopicMass - jj.monoisotopicMass) < 1e-5)
                    sbsequence.Append('[' + jj.chemicalFormula.Formula + ']');
                else
                    return null;
            }

            for (int r = 0; r < sequence.Length; r++)
            {
                if (Sweet.lollipop.carbamidomethylation && sequence[r] == 'C')
                {
                    sbsequence.Append("[H3C2N1O1]");
                }
                sbsequence.Append(sequence[r]);
                // variable modification on this residue
                ModificationWithMass residue_variable_mod = ptm_list.Where(p => p.position == r + 2).Select(m => m.modification).FirstOrDefault();
                if (residue_variable_mod != null)
                {
                    var jj = residue_variable_mod as ModificationWithMassAndCf;
                    if (jj != null && Math.Abs(jj.chemicalFormula.MonoisotopicMass - jj.monoisotopicMass) < 1e-5)
                        sbsequence.Append('[' + jj.chemicalFormula.Formula + ']');
                    else
                        return null;
                }
            }

            // variable modification on peptide C-terminus
            ModificationWithMass pep_c_term_variable_mod = ptm_list.Where(p => p.position == sequence.Length + 2).Select(m => m.modification).FirstOrDefault();
            if (pep_c_term_variable_mod != null)
            {
                var jj = pep_c_term_variable_mod as ModificationWithMassAndCf;
                if (jj != null && Math.Abs(jj.chemicalFormula.MonoisotopicMass - jj.monoisotopicMass) < 1e-5)
                    sbsequence.Append('[' + jj.chemicalFormula.Formula + ']');
                else
                    return null;
            }

            return sbsequence.ToString();
        }

    }


    public enum TopDownResultType
    {
        Biomarker,
        TightAbsoluteMass,
        Unknown //not read in if unknown...
    }

}