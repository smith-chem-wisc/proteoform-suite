﻿using NUnit.Framework;
using System;
using Proteomics;
using ProteoformSuiteInternal;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    [TestFixture]
    class TestGoTermNumber
    {
        [Test]
        public void testLogOddsRatio()
        {
            int q = 1;//number of enriched proteins with the term
            int k = 2;//number of enriched proteins
            int m = 2;//number of proteins in the background with the term
            int t = 4;//number of proteins in the background

            DatabaseReference d = new DatabaseReference("GO", ":1", new List<Tuple<string, string>> { new Tuple<string, string>("term", "P:one") });
            GoTerm g = new GoTerm(d);
            GoTermNumber gtn = new GoTermNumber(g, q, k, m, t);
            Assert.AreEqual(gtn.log_odds_ratio, 0);
            Assert.AreEqual(Math.Round((decimal)gtn.p_value,6), 0.833333);

            q = 1;//number of enriched proteins with the term
            k = 2;//number of enriched proteins
            m = 4;//number of proteins in the background with the term
            t = 4;//number of proteins in the background

            gtn = new GoTermNumber(g, q, k, m, t);
            Assert.AreEqual(gtn.log_odds_ratio, -1);
            Assert.AreEqual(Math.Round((decimal)gtn.p_value, 6), 1);

            q = 2;//number of enriched proteins with the term
            k = 2;//number of enriched proteins
            m = 2;//number of proteins in the background with the term
            t = 4;//number of proteins in the background

            gtn = new GoTermNumber(g, q, k, m, t);
            Assert.AreEqual(gtn.log_odds_ratio, 1);
            Assert.AreEqual(Math.Round((decimal)gtn.p_value, 6), 0.166667);
        }

        [Test]
        public void testBinomialCoefficient()
        {
            int n = 10;
            int k = 2;
            Assert.AreEqual(45, (int)GoTermNumber.binomialCoefficient(n, k));
        }

        [Test]
        public void testGoTerm_pValue()
        {
            int q = 2; //count of proteins in selected subset with the particular Go term
            int k = 4; //count of proteins in selected subset
            int m = 4; //count of proteins in background with the particular Go term
            int t = 10; //count of proteins in background

            Assert.AreEqual(0.54762, Math.Round((double)GoTermNumber.GoTerm_pValue(q, k, m, t), 5));
        }

        [Test]
        public void get_interesting_goterm_families()
        {
            DatabaseReference d1 = new DatabaseReference("GO", "GO:1", new List<Tuple<string, string>> { new Tuple<string, string>("term", "P:1") });
            DatabaseReference d2 = new DatabaseReference("GO", "GO:2", new List<Tuple<string, string>> { new Tuple<string, string>("term", "P:2") });
            DatabaseReference d3 = new DatabaseReference("GO", "GO:1", new List<Tuple<string, string>> { new Tuple<string, string>("term", "P:1") });
            GoTerm g1 = new GoTerm(d1);
            GoTerm g2 = new GoTerm(d2);
            GoTerm g3 = new GoTerm(d3);
            ProteinWithGoTerms p1 = new ProteinWithGoTerms("", "T1", new Dictionary<int, List<Modification>>(), new int?[] { 0 }, new int?[] { 0 }, new string[] { "" }, "T2", "T3", true, false, new List<DatabaseReference> { d1 }, new List<GoTerm> { g1 });
            ProteinWithGoTerms p2 = new ProteinWithGoTerms("", "T2", new Dictionary<int, List<Modification>>(), new int?[] { 0 }, new int?[] { 0 }, new string[] { "" }, "T2", "T3", true, false, new List<DatabaseReference> { d2 }, new List<GoTerm> { g2 });
            ProteinWithGoTerms p3 = new ProteinWithGoTerms("", "T3", new Dictionary<int, List<Modification>>(), new int?[] { 0 }, new int?[] { 0 }, new string[] { "" }, "T2", "T3", true, false, new List<DatabaseReference> { d3 }, new List<GoTerm> { g3 });
            Dictionary<InputFile, Protein[]> dict = new Dictionary<InputFile, Protein[]> {
                { new InputFile("fake.txt", Purpose.ProteinDatabase), new Protein[] { p1 } },
                { new InputFile("fake.txt", Purpose.ProteinDatabase), new Protein[] { p2 } },
                { new InputFile("fake.txt", Purpose.ProteinDatabase), new Protein[] { p3 } },
            };
            ExperimentalProteoform e1 = new ExperimentalProteoform("E");
            ExperimentalProteoform e2 = new ExperimentalProteoform("E");
            e1.quant.intensitySum = 1;
            e1.quant.FDR = 0;
            e1.quant.logFoldChange = 1;
            e2.quant.intensitySum = 1;
            e2.quant.FDR = 0;
            e2.quant.logFoldChange = 1;
            TheoreticalProteoform t = new TheoreticalProteoform("T1_T1_asdf", "", p1, true, 0, 0, new PtmSet(new List<Ptm>()), 0, true, true, dict);
            TheoreticalProteoform u = new TheoreticalProteoform("T2_T1_asdf_asdf", "", p2, true, 0, 0, new PtmSet(new List<Ptm>()), 0, true, true, dict);
            TheoreticalProteoform v = new TheoreticalProteoform("T3_T1_asdf_Asdf_Asdf", "", p3, true, 0, 0, new PtmSet(new List<Ptm>()), 0, true, true, dict);
            t.proteinList = new List<ProteinWithGoTerms> { p1 };
            u.proteinList = new List<ProteinWithGoTerms> { p2 };
            v.proteinList = new List<ProteinWithGoTerms> { p3 };
            ProteoformFamily f = new ProteoformFamily(new List<Proteoform> { t, v, e1 }, 0); // two theoreticals with the same GoTerms... expecting one GoTerm number but two theoretical proteins
            ProteoformFamily h = new ProteoformFamily(new List<Proteoform> { u, e2 }, 0);
            List<ProteoformFamily> families = new List<ProteoformFamily> { f, h };
            t.family = f;
            v.family = f;
            e1.family = f;
            u.family = h;
            e2.family = h;
            List<ExperimentalProteoform> fake_significant = new List<ExperimentalProteoform> { e1 };
            List<ProteinWithGoTerms> significant_proteins = Lollipop.getInducedOrRepressedProteins(fake_significant, 0, 1, 0);
            List<GoTermNumber> gtn = Lollipop.getGoTermNumbers(significant_proteins, new List<ProteinWithGoTerms> { p1, p2, p3 });
            Assert.AreEqual(2, significant_proteins.Count);
            Assert.AreEqual(1, gtn.Count);
            Assert.AreEqual("1", gtn.First().Id);
            Assert.AreEqual(0 - (decimal)Math.Log(2d / 3d, 2), gtn.First().log_odds_ratio);

            List<ProteoformFamily> fams = Lollipop.getInterestingFamilies(gtn, families);
            Assert.AreEqual(1, fams.Count);
            Assert.AreEqual(2, fams[0].theoretical_proteoforms.Count);
        }

        [Test]
        public void test_goterm_analysis()
        {
            DatabaseReference d1 = new DatabaseReference("GO", "GO:1", new List<Tuple<string, string>> { new Tuple<string, string>("term", "P:1") });
            DatabaseReference d2 = new DatabaseReference("GO", "GO:2", new List<Tuple<string, string>> { new Tuple<string, string>("term", "P:2") });
            DatabaseReference d3 = new DatabaseReference("GO", "GO:1", new List<Tuple<string, string>> { new Tuple<string, string>("term", "P:1") });
            GoTerm g1 = new GoTerm(d1);
            GoTerm g2 = new GoTerm(d2);
            GoTerm g3 = new GoTerm(d3);
            ProteinWithGoTerms p1 = new ProteinWithGoTerms("", "T1", new Dictionary<int, List<Modification>>(), new int?[] { 0 }, new int?[] { 0 }, new string[] { "" }, "T2", "T3", true, false, new List<DatabaseReference> { d1 }, new List<GoTerm> { g1 });
            ProteinWithGoTerms p2 = new ProteinWithGoTerms("", "T2", new Dictionary<int, List<Modification>>(), new int?[] { 0 }, new int?[] { 0 }, new string[] { "" }, "T2", "T3", true, false, new List<DatabaseReference> { d2 }, new List<GoTerm> { g2 });
            ProteinWithGoTerms p3 = new ProteinWithGoTerms("", "T3", new Dictionary<int, List<Modification>>(), new int?[] { 0 }, new int?[] { 0 }, new string[] { "" }, "T2", "T3", true, false, new List<DatabaseReference> { d3 }, new List<GoTerm> { g3 });
            Dictionary<InputFile, Protein[]> dict = new Dictionary<InputFile, Protein[]> {
                { new InputFile("fake.txt", Purpose.ProteinDatabase), new Protein[] { p1 } },
                { new InputFile("fake.txt", Purpose.ProteinDatabase), new Protein[] { p2 } },
                { new InputFile("fake.txt", Purpose.ProteinDatabase), new Protein[] { p3 } },
            };
            ExperimentalProteoform e1 = new ExperimentalProteoform("E");
            ExperimentalProteoform e2 = new ExperimentalProteoform("E");
            e1.quant.intensitySum = 1;
            e1.quant.FDR = 0;
            e1.quant.logFoldChange = 1;
            e2.quant.intensitySum = 1;
            e2.quant.FDR = 0;
            e2.quant.logFoldChange = 1;
            TheoreticalProteoform t = new TheoreticalProteoform("T1_T1_asdf", "", p1, true, 0, 0, new PtmSet(new List<Ptm>()), 0, true, true, dict);
            TheoreticalProteoform u = new TheoreticalProteoform("T2_T1_asdf_asdf", "", p2, true, 0, 0, new PtmSet(new List<Ptm>()), 0, true, true, dict);
            TheoreticalProteoform v = new TheoreticalProteoform("T3_T1_asdf_Asdf_Asdf", "", p3, true, 0, 0, new PtmSet(new List<Ptm>()), 0, true, true, dict);
            t.proteinList = new List<ProteinWithGoTerms> { p1 };
            u.proteinList = new List<ProteinWithGoTerms> { p2 };
            v.proteinList = new List<ProteinWithGoTerms> { p3 };
            ProteoformFamily f = new ProteoformFamily(new List<Proteoform> { t, v, e1 }, 0); // two theoreticals with the same GoTerms... expecting one GoTerm number but two theoretical proteins
            ProteoformFamily h = new ProteoformFamily(new List<Proteoform> { u, e2 }, 0);
            List<ProteoformFamily> families = new List<ProteoformFamily> { f, h };
            t.family = f;
            v.family = f;
            e1.family = f;
            u.family = h;
            e2.family = h;
            Lollipop.inducedOrRepressedProteins = Lollipop.getInducedOrRepressedProteins(new List<ExperimentalProteoform> { e1 }, 0, 1, 0);
            Lollipop.allTheoreticalProteins = true;
            Lollipop.expanded_proteins = new ProteinWithGoTerms[] { p1, p2, p3 };
            Lollipop.GO_analysis();
            Assert.AreEqual(2, Lollipop.inducedOrRepressedProteins.Count);
            Assert.AreEqual(1, Lollipop.goTermNumbers.Count);
            Assert.AreEqual("1", Lollipop.goTermNumbers.First().Id);
            Assert.AreEqual(0 - (decimal)Math.Log(2d / 3d, 2), Lollipop.goTermNumbers.First().log_odds_ratio);

            List<ProteoformFamily> fams = Lollipop.getInterestingFamilies(Lollipop.goTermNumbers, families);
            Assert.AreEqual(1, fams.Count);
            Assert.AreEqual(2, fams[0].theoretical_proteoforms.Count);
        }
    }
}
