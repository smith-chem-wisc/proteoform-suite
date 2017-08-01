﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ProteoformSuiteInternal
{
    public class TusherValues2
        : IStatisiticalSignificance, ITusherValues
    {

        #region Tusher Analysis Properties

        public List<BiorepTechrepIntensity> numeratorOriginalIntensities { get; set; }
        public List<BiorepTechrepIntensity> denominatorOriginalIntensities { get; set; }
        public List<BiorepTechrepIntensity> numeratorImputedIntensities { get; set; }
        public List<BiorepTechrepIntensity> denominatorImputedIntensities { get; set; }
        public Dictionary<Tuple<string, string, string>, BiorepTechrepIntensity> allIntensities { get; set; }

        public decimal numeratorIntensitySum { get; set; } = 0;
        public decimal denominatorIntensitySum { get; set; } = 0;
        public decimal scatter { get; set; } = 0;
        public bool significant { get; set; } = false;
        public decimal relative_difference { get; set; }
        public decimal correspondingAvgSortedRelDiff { get; set; }
        public decimal roughSignificanceFDR { get; set; } = 0;

        #endregion Tusher Analysis Properties

        #region Public Methods

        public void impute_biorep_intensities(List<BiorepTechrepIntensity> intensities, Dictionary<string, List<string>> conditionBioReps, string numerator_condition, string denominator_condition, string induced_condition, decimal bkgdAverageIntensity, decimal bkgdStDev, decimal sKnot)
        {
            //bkgdAverageIntensity is log base 2
            //bkgdStDev is log base 2

            significant = false;
            numeratorOriginalIntensities = intensities.Where(b => b.condition == numerator_condition).ToList();
            numeratorImputedIntensities = imputedIntensities(numeratorOriginalIntensities, Sweet.lollipop.get_files(Sweet.lollipop.input_files, Purpose.Quantification), bkgdAverageIntensity, bkgdStDev, numerator_condition, conditionBioReps[numerator_condition]);
            numeratorIntensitySum = (decimal)numeratorOriginalIntensities.Sum(i => i.intensity_sum) + (decimal)numeratorImputedIntensities.Sum(i => i.intensity_sum);
            List<BiorepTechrepIntensity> allNumeratorIntensities = numeratorOriginalIntensities.Concat(numeratorImputedIntensities).ToList();

            denominatorOriginalIntensities = intensities.Where(b => b.condition == denominator_condition).ToList();
            denominatorImputedIntensities = imputedIntensities(denominatorOriginalIntensities, Sweet.lollipop.get_files(Sweet.lollipop.input_files, Purpose.Quantification), bkgdAverageIntensity, bkgdStDev, denominator_condition, conditionBioReps[denominator_condition]);
            denominatorIntensitySum = (decimal)denominatorOriginalIntensities.Sum(i => i.intensity_sum) + (decimal)denominatorImputedIntensities.Sum(i => i.intensity_sum);
            List<BiorepTechrepIntensity> allDenominatorIntensities = denominatorOriginalIntensities.Concat(denominatorImputedIntensities).ToList();

            allIntensities = allNumeratorIntensities.Concat(allDenominatorIntensities).ToDictionary(x => new Tuple<string, string, string>(x.condition, x.biorep, x.techrep), x => x);
        }

        public void determine_proteoform_statistics(string induced_condition, decimal sKnot)
        {
            List<BiorepTechrepIntensity> allNumeratorIntensities = numeratorOriginalIntensities.Concat(numeratorImputedIntensities).ToList();
            List<BiorepTechrepIntensity> allDenominatorIntensities = denominatorOriginalIntensities.Concat(denominatorImputedIntensities).ToList();

            // We are using linear intensities, like in Tusher et al. (2001).
            // This is a non-parametric test, and so it makes no assumptions about the incoming probability distribution, unlike a simple t-test.
            // Therefore, the right-skewed intensity distributions is okay for this test.
            scatter = StdDev(allNumeratorIntensities, allDenominatorIntensities);
            List<BiorepTechrepIntensity> induced = allIntensities.Where(kv => kv.Key.Item1 == induced_condition).Select(kv => kv.Value).ToList();
            List<BiorepTechrepIntensity> uninduced = allIntensities.Where(kv => kv.Key.Item1 != induced_condition).Select(kv => kv.Value).ToList();
            relative_difference = getSingleTestStatistic(induced, uninduced, scatter, sKnot);
        }

        /// <summary>
        /// Returns imputed intensities for a certain condition for biological replicates this proteoform was not observed in.
        /// </summary>
        /// <param name="observedBioreps"></param>
        /// <param name="bkgdAverageIntensity"></param>
        /// <param name="bkgdStDev"></param>
        /// <param name="condition"></param>
        /// <param name="bioreps"></param>
        /// <returns></returns>
        public static List<BiorepTechrepIntensity> imputedIntensities(List<BiorepTechrepIntensity> observedBioreps, IEnumerable<InputFile> files, decimal bkgdAverageIntensity, decimal bkgdStDev, string condition, List<string> bioreps)
        {
            //bkgdAverageIntensity is log base 2
            //bkgdStDev is log base 2

            List<Tuple<string, string>> bt = files.Select(x => new Tuple<string, string>(x.biological_replicate, x.technical_replicate)).Distinct().ToList();

            return (
                from x in bt
                where !observedBioreps.Any(k => k.condition == condition && k.biorep == x.Item1 && k.techrep == x.Item2)
                select new BiorepTechrepIntensity(true, x.Item1, condition, x.Item2, QuantitativeProteoformValues.imputed_intensity(bkgdAverageIntensity, bkgdStDev)))
                .ToList();
        }

        #endregion Public Methods

        #region Relative Difference Methods

        /// <summary>
        /// Calculates the pooled standard deviation across unlogged intensities for the two conditions for this proteoform.
        /// This is known as the "scatter s(i)" in the Tusher et al. paper.
        /// </summary>
        /// <param name="allInduced"></param>
        /// <param name="allUninduced"></param>
        /// <returns></returns>
        public decimal StdDev(IEnumerable<BiorepTechrepIntensity> allInduced, IEnumerable<BiorepTechrepIntensity> allUninduced)
        {
            int numerator_count = allInduced.Count();
            int denominator_count = allUninduced.Count();
            if ((numerator_count + denominator_count) == 2)
                return 1000000m;

            decimal a = (decimal)((1d / (double)numerator_count + 1d / (double)denominator_count) / ((double)numerator_count + (double)denominator_count - 2d));
            double avgNumerator = allInduced.Average(x => x.intensity_sum);
            double avgDenominator = allUninduced.Average(x => x.intensity_sum);
            decimal numeratorSumSquares = allInduced.Sum(n => (decimal)Math.Pow(n.intensity_sum - avgNumerator, 2d));
            decimal denominatorSumSquares = allUninduced.Sum(d => (decimal)Math.Pow(d.intensity_sum - avgDenominator, 2d));
            decimal stdev = (decimal)Math.Sqrt((double)((numeratorSumSquares + denominatorSumSquares) * a));
            return stdev;
        }

        /// <summary>
        /// This is the relative difference from Tusher, et al. (2001) using unlogged intensity values
        /// d(i) = { Average(measurement x from state I) - Average(measurement x from state U) } / { (pooled std dev from I and U) - s_knot }
        /// </summary>
        /// <param name="allInduced"></param>
        /// <param name="allUninduced"></param>
        /// <param name="pooledStdDev"></param>
        /// <param name="sKnot">
        /// A constant intended to "minimize the coefficient of variation"
        /// </param>
        /// <returns></returns>
        public decimal getSingleTestStatistic(List<BiorepTechrepIntensity> allInduced, List<BiorepTechrepIntensity> allUninduced, decimal pooledStdDev, decimal sKnot)
        {
            double t = (allInduced.Average(l => l.intensity_sum) - allUninduced.Average(h => h.intensity_sum)) / ((double)(pooledStdDev + sKnot));
            return (decimal)t;
        }

        #endregion Relative Difference Methods

    }
}