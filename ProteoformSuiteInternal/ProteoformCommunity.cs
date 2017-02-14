﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ProteoformSuiteInternal;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace ProteoformSuiteInternal
{
    public class ProteoformCommunity
    {
        //Please do not list {get;set} for new fields, so they are properly recorded in save all AC161103
        public ExperimentalProteoform[] experimental_proteoforms = new ExperimentalProteoform[0];
        public TheoreticalProteoform[] theoretical_proteoforms = new TheoreticalProteoform[0];
        public TopDownProteoform[] topdown_proteoforms = new TopDownProteoform[0];

        public bool has_e_proteoforms
        {
            get { return experimental_proteoforms.Length > 0; }
        }
        public bool has_e_and_t_proteoforms
        {
            get { return experimental_proteoforms.Length > 0 && theoretical_proteoforms.Length > 0; }
        }
        public Dictionary<string, TheoreticalProteoform[]> decoy_proteoforms = new Dictionary<string, TheoreticalProteoform[]>();
        public List<ProteoformRelation> relations_in_peaks = new List<ProteoformRelation>();
        public List<DeltaMassPeak> delta_mass_peaks = new List<DeltaMassPeak>();
        public List<ProteoformFamily> families = new List<ProteoformFamily>();
        //public static double maximum_delta_mass_peak_fdr = 25;

        //BUILDING RELATIONSHIPS
        public List<ProteoformRelation> relate_et(Proteoform[] pfs1, Proteoform[] pfs2, ProteoformComparison relation_type)
        {
            ConcurrentBag<ProteoformRelation> relations = new ConcurrentBag<ProteoformRelation>();

            if (!Lollipop.notch_search_et)
            {
                Parallel.ForEach(pfs1, pf1 =>
                {
                    List<Proteoform> candidate_pfs2 = pfs2.
                        Where(pf2 => (!Lollipop.neucode_labeled || pf2.lysine_count == pf1.lysine_count)
                            && (pf1.modified_mass - pf2.modified_mass) >= Lollipop.et_low_mass_difference
                            && (pf1.modified_mass - pf2.modified_mass) <= Lollipop.et_high_mass_difference).ToList();

                    foreach (string accession in new HashSet<string>(candidate_pfs2.Select(p => p.accession)))
                    {
                        List<Proteoform> candidate_pfs2_with_accession = candidate_pfs2.Where(x => x.accession == accession).ToList();
                        candidate_pfs2_with_accession.Sort(Comparer<Proteoform>.Create((x, y) => Math.Abs(pf1.modified_mass - x.modified_mass).CompareTo(Math.Abs(pf1.modified_mass - y.modified_mass))));
                        Proteoform best_pf2 = candidate_pfs2_with_accession.First();
                        lock (relations) relations.Add(new ProteoformRelation(pf1, best_pf2, relation_type, pf1.modified_mass - best_pf2.modified_mass));
                    }
                });
            }
            else
            {
                Parallel.ForEach(pfs1, pf1 =>
                {
                    foreach (double mass in Lollipop.notch_masses_et)
                    {
                        List<Proteoform> candidate_pfs2 = pfs2.
                            Where(pf2 => (!Lollipop.neucode_labeled || pf2.lysine_count == pf1.lysine_count)
                                && (pf1.modified_mass - pf2.modified_mass) >= mass - Lollipop.peak_width_base_et
                                && (pf1.modified_mass - pf2.modified_mass) <= mass + Lollipop.peak_width_base_et).ToList();

                        foreach (string accession in new HashSet<string>(candidate_pfs2.Select(p => p.accession)))
                        {
                            List<Proteoform> candidate_pfs2_with_accession = candidate_pfs2.Where(x => x.accession == accession).ToList();
                            candidate_pfs2_with_accession.Sort(Comparer<Proteoform>.Create((x, y) => Math.Abs(pf1.modified_mass - x.modified_mass).CompareTo(Math.Abs(pf1.modified_mass - y.modified_mass))));
                            Proteoform best_pf2 = candidate_pfs2_with_accession.First();
                            lock (relations) relations.Add(new ProteoformRelation(pf1, best_pf2, relation_type, pf1.modified_mass - best_pf2.modified_mass));
                        }
                    }
                });
            }

            count_nearby_relations(relations.ToList());
            return relations.ToList();
        }


        public List<ProteoformRelation> relate_ee(ExperimentalProteoform[] pfs1, ExperimentalProteoform[] pfs2, ProteoformComparison relation_type)
        {
            List<ProteoformRelation> relations_in_NC = new List<ProteoformRelation>();
            List<ProteoformRelation> relations = new List<ProteoformRelation>(
                from pf1 in pfs1
                from pf2 in pfs2
                where allowed_ee_relation(pf1, pf2)
                select new ProteoformRelation(pf1, pf2, relation_type, pf1.modified_mass - pf2.modified_mass)
            );
            count_nearby_relations(relations);
            return relations;
        }

        public bool allowed_ee_relation(ExperimentalProteoform pf1, ExperimentalProteoform pf2)
        {
            if (!Lollipop.notch_search_ee)
            {
                return pf1.modified_mass >= pf2.modified_mass
                    && pf1 != pf2
                    && (!Lollipop.neucode_labeled || pf1.lysine_count == pf2.lysine_count)
                    && pf1.modified_mass - pf2.modified_mass <= Lollipop.ee_max_mass_difference
                    && Math.Abs(pf1.agg_rt - pf2.agg_rt) <= Lollipop.ee_max_RetentionTime_difference;
            }
            else
            {
                foreach (double mass in Lollipop.notch_masses_ee)
                {
                    if (
                    pf1.modified_mass >= pf2.modified_mass
                    && pf1 != pf2
                    && (!Lollipop.neucode_labeled || pf1.lysine_count == pf2.lysine_count)
                    && pf1.modified_mass - pf2.modified_mass <= mass + Lollipop.peak_width_base_ee
                    && pf1.modified_mass - pf2.modified_mass >= mass - Lollipop.peak_width_base_ee
                    && Math.Abs(pf1.agg_rt - pf2.agg_rt) <= Lollipop.ee_max_RetentionTime_difference)
                        return true;
                else continue;
                }
                return false;
            }
            //where ProteoformRelation.mass_difference_is_outside_no_mans_land(pf1.modified_mass - pf2.modified_mass)
            //putative counts include no-mans land, currently
        }

        public bool allowed_ef_relation(ExperimentalProteoform pf1, ExperimentalProteoform pf2)
        {
            if (!Lollipop.notch_search_ee)
            { 
                return pf1.modified_mass >= pf2.modified_mass
                    && pf1 != pf2
                    && pf1.modified_mass > pf2.modified_mass
                    && (!Lollipop.neucode_labeled || pf1.lysine_count != pf2.lysine_count)
                    && (Lollipop.neucode_labeled || Math.Abs(pf1.agg_rt - pf2.agg_rt) > 2 * Lollipop.ee_max_RetentionTime_difference)
                    && pf1.modified_mass - pf2.modified_mass <= Lollipop.ee_max_mass_difference
                    && (!Lollipop.neucode_labeled || Math.Abs(pf1.agg_rt - pf2.agg_rt) <= Lollipop.ee_max_RetentionTime_difference);
            }
            else
            {
                foreach (double mass in Lollipop.notch_masses_ee)
                {
                    if (
                    pf1.modified_mass >= pf2.modified_mass
                    && pf1 != pf2
                    && pf1.modified_mass > pf2.modified_mass
                    && (!Lollipop.neucode_labeled || pf1.lysine_count != pf2.lysine_count)
                    && (Lollipop.neucode_labeled || Math.Abs(pf1.agg_rt - pf2.agg_rt) > 2 * Lollipop.ee_max_RetentionTime_difference)
                    && pf1.modified_mass - pf2.modified_mass <= mass + Lollipop.peak_width_base_ee
                    && pf1.modified_mass - pf2.modified_mass >= mass - Lollipop.peak_width_base_ee
                    && (!Lollipop.neucode_labeled || Math.Abs(pf1.agg_rt - pf2.agg_rt) <= Lollipop.ee_max_RetentionTime_difference))
                    return true;
                    else continue;
                }
                return false;
            }
            //where ProteoformRelation.mass_difference_is_outside_no_mans_land(pf1.modified_mass - pf2.modified_mass)
            //putative counts include no-mans land, currently
        }

        private static void count_nearby_relations(List<ProteoformRelation> all_relations)
        {
            //PARALLEL PROBLEM
            //Parallel.ForEach<ProteoformRelation>(relations, relation => relation.set_nearby_group(relations));
            foreach (ProteoformRelation relation in all_relations) relation.set_nearby_group(all_relations);
        }

        public Dictionary<string, List<ProteoformRelation>> relate_ed()
        {
            Dictionary<string, List<ProteoformRelation>> ed_relations = new Dictionary<string, List<ProteoformRelation>>();
            Parallel.ForEach(decoy_proteoforms, decoys =>
            {
                ed_relations[decoys.Key] = relate_et(experimental_proteoforms.Where(p => p.accepted).ToArray(), decoys.Value, ProteoformComparison.ed);
            });
            return ed_relations;
        }

        public List<ProteoformRelation> relate_ef()
        {
            List<ProteoformRelation> ef_relations = new List<ProteoformRelation>();
            ExperimentalProteoform[] pfs1 = new List<ExperimentalProteoform>(this.experimental_proteoforms.Where(p => p.accepted)).ToArray();
            ExperimentalProteoform[] pfs2 = new List<ExperimentalProteoform>(this.experimental_proteoforms.Where(p => p.accepted)).ToArray();
            foreach (ExperimentalProteoform pf1 in pfs1)
            {
                int num_relations = pfs2.Where(pf2 => allowed_ee_relation(pf1, pf2)).Count(); //number that would be chosen with equal lysine counts or w/in retention time from a randomized set
                new Random().Shuffle(pfs2);
                List<ProteoformRelation> ef_relation_addition = pfs2.Where(pf2 => allowed_ef_relation(pf1, pf2))
                    .Take(num_relations)
                    .Select(pf2 => new ProteoformRelation(pf1, pf2, ProteoformComparison.ef, pf1.modified_mass - pf2.modified_mass)).ToList();
                ef_relations.AddRange(ef_relation_addition);
            }
                count_nearby_relations(ef_relations);
            return ef_relations;
        }

        public List<ProteoformRelation> relate_td(List<ExperimentalProteoform> experimentals, List<TheoreticalProteoform> theoreticals, List<TopDownProteoform> topdowns)
        {
            List<ProteoformRelation> td_relations = new List<ProteoformRelation>();

            int max_missed_monoisotopics = Convert.ToInt32(Lollipop.missed_monos);
            List<int> missed_monoisotopics_range = Enumerable.Range(-max_missed_monoisotopics, max_missed_monoisotopics * 2 + 1).ToList();
            foreach (TopDownProteoform topdown in topdowns)
            {
                foreach (int m in missed_monoisotopics_range)
                {
                    double shift = m * Lollipop.MONOISOTOPIC_UNIT_MASS;
                    double mass_tol = (topdown.monoisotopic_mass + shift) / 1000000 * Convert.ToInt32(Lollipop.mass_tolerance);
                    double low = topdown.monoisotopic_mass + shift - mass_tol;
                    double high = topdown.monoisotopic_mass + shift + mass_tol;
                    List<ExperimentalProteoform> matching_e = experimentals.Where(ep => ep.modified_mass >= low && ep.modified_mass <= high
                    && Math.Abs(ep.agg_rt - topdown.agg_rt) < Convert.ToDouble(Lollipop.retention_time_tolerance)).ToList();
                    foreach (ExperimentalProteoform e in matching_e)
                    {
                        ProteoformRelation td_relation = new ProteoformRelation(topdown, e, ProteoformComparison.etd, (e.modified_mass - topdown.monoisotopic_mass));
                        td_relation.accepted = true;
                        td_relation.connected_proteoforms[0].relationships.Add(td_relation);
                        td_relation.connected_proteoforms[1].relationships.Add(td_relation);
                        td_relations.Add(td_relation);
                    }
                }

                //match each td proteoform group to the closest theoretical w/ same accession and number of modifications. (if no match always make relationship with unmodified)
                if (theoreticals.Count > 0)
                {
                    TheoreticalProteoform theo = null;
                    try
                    {
                        theo = theoreticals.Where(t => t.accession_reduced == topdown.accession.Split('_')[0] && t.ptm_list.Count
                            == topdown.ptm_list.Count).OrderBy(t => Math.Abs(t.modified_mass - topdown.theoretical_mass)).First();
                    }
                    catch
                    {

                        if (topdown.ptm_list.Count == 0) continue; //if can't find match to unmodified topdown, nothing to do (not in database)
                                                                   //if modified topdown, compare with unmodified theoretical
                        else
                        {
                            try
                            {
                                theo = theoreticals.Where(t => t.accession_reduced == topdown.accession.Split('_')[0] && t.ptm_list.Count == 0).OrderBy(
                          t => Math.Abs(t.modified_mass - topdown.theoretical_mass)).First();
                            }
                            catch { continue; }
                        }
                    }

                    ProteoformRelation t_td_relation = new ProteoformRelation(topdown, theo, ProteoformComparison.ttd, (topdown.theoretical_mass - theo.modified_mass));
                    t_td_relation.accepted = true;
                    t_td_relation.connected_proteoforms[0].relationships.Add(t_td_relation);
                    t_td_relation.connected_proteoforms[1].relationships.Add(t_td_relation);
                    td_relations.Add(t_td_relation);
                }
            }
            return td_relations;
        }

        public List<ProteoformRelation> relate_targeted_td(List<ExperimentalProteoform> experimentals, List<TopDownProteoform> topdown_proteoforms)
        {
            List<ProteoformRelation> td_relations = new List<ProteoformRelation>();
            int max_missed_monoisotopics = Convert.ToInt32(Lollipop.missed_monos);
            List<int> missed_monoisotopics_range = Enumerable.Range(-max_missed_monoisotopics, max_missed_monoisotopics * 2 + 1).ToList();
            foreach (TopDownProteoform td_proteoform in topdown_proteoforms)
            {
                foreach (int m in missed_monoisotopics_range)
                {
                    double shift = m * Lollipop.MONOISOTOPIC_UNIT_MASS;
                    double mass_tol = (td_proteoform.modified_mass + shift) / 1000000 * Convert.ToInt32(Lollipop.mass_tolerance);
                    double low = td_proteoform.modified_mass + shift - mass_tol;
                    double high = td_proteoform.modified_mass + shift + mass_tol;
                    List<ExperimentalProteoform> matching_e = experimentals.Where(ep => ep.modified_mass >= low && ep.modified_mass <= high).ToList();
                    foreach (ExperimentalProteoform e in matching_e)
                    {
                        ProteoformRelation td_relation = new ProteoformRelation(td_proteoform, e, ProteoformComparison.ettd, (e.modified_mass - td_proteoform.modified_mass));
                        td_relation.accepted = true;
                        td_relation.connected_proteoforms[0].relationships.Add(td_relation);
                        td_relation.connected_proteoforms[1].relationships.Add(td_relation);
                        td_relations.Add(td_relation);
                    }
                }
            }
            return td_relations;
        }

        //GROUP and ANALYZE RELATIONS
        public List<DeltaMassPeak> accept_deltaMass_peaks(List<ProteoformRelation> relations, Dictionary<string, List<ProteoformRelation>> decoy_relations)
        {
            //order by E intensity, then by descending unadjusted_group_count (running sum) before forming peaks, and analyze only relations outside of no-man's-land
            List<ProteoformRelation> grouped_relations = new List<ProteoformRelation>();
            List<ProteoformRelation> remaining_relations_outside_no_mans = relations.OrderByDescending(r => r.nearby_relations_count).
                ThenByDescending(r => r.agg_intensity_1).Where(r => r.outside_no_mans_land).ToList(); // Group count is the primary sort
            List<DeltaMassPeak> peaks = new List<DeltaMassPeak>();
            while (remaining_relations_outside_no_mans.Count > 0)
            {
                ProteoformRelation top_relation = remaining_relations_outside_no_mans[0];
                if (top_relation.relation_type != ProteoformComparison.ee && top_relation.relation_type != ProteoformComparison.et)
                    throw new Exception("Only EE and ET peaks can be accepted");

                DeltaMassPeak new_peak = new DeltaMassPeak(top_relation, remaining_relations_outside_no_mans);
                if (Lollipop.decoy_databases > 0 && top_relation.connected_proteoforms[1] is TheoreticalProteoform) new_peak.calculate_fdr(decoy_relations);
                else if (Lollipop.ef_relations.Count > 0 && top_relation.connected_proteoforms[1] is ExperimentalProteoform) new_peak.calculate_fdr(new Dictionary<string, List<ProteoformRelation>>() { { "ef_relations", Lollipop.ef_relations }});
                peaks.Add(new_peak);

                List<ProteoformRelation> mass_differences_in_peak = new_peak.grouped_relations;
                relations_in_peaks.AddRange(mass_differences_in_peak);
                grouped_relations.AddRange(mass_differences_in_peak);
                remaining_relations_outside_no_mans = exclusive_relation_group(remaining_relations_outside_no_mans, grouped_relations);
            }

            this.delta_mass_peaks.AddRange(peaks);
            return peaks;
        }

        public static ProteoformRelation find_next_root(List<ProteoformRelation> ordered, List<ProteoformRelation> running)
        {
            return ordered.FirstOrDefault(r =>
                running.All(s =>
                    r.delta_mass < s.delta_mass - 4 || r.delta_mass > s.delta_mass + 4));

            //if (top_relation.relation_type != ProteoformComparison.ee && top_relation.relation_type != ProteoformComparison.et)
            //    throw new Exception("Only EE and ET peaks can be accepted");
        }

        public List<DeltaMassPeak> accept_deltaMass_peaks(List<ProteoformRelation> relations, List<ProteoformRelation> false_relations)
        {
            return accept_deltaMass_peaks(relations, new Dictionary<string, List<ProteoformRelation>> { { "", false_relations } });
        }

        private List<ProteoformRelation> exclusive_relation_group(List<ProteoformRelation> relations, List<ProteoformRelation> grouped_relations)
        {
            return relations.Except(grouped_relations).OrderByDescending(r => r.nearby_relations_count).ThenByDescending(r => r.agg_intensity_1).ToList();
        }

        //CONSTRUCTING FAMILIES
        public void construct_families()
        {
            clean_up_td_relations();
            List<Proteoform> inducted = new List<Proteoform>();
            List<Proteoform> remaining = new List<Proteoform>(this.experimental_proteoforms.Where(e => e.accepted).ToList());
            remaining.AddRange(Lollipop.proteoform_community.topdown_proteoforms.ToList());
            // foreach (TopDownProteoformGroup g in topdown_proteoform_groups) remaining.AddRange(g.topdown_proteoforms.Where(p => p.relationships.Count > 0).ToList());
            int family_id = 1;
            while (remaining.Count > 0)
            {
                ProteoformFamily new_family = new ProteoformFamily(construct_family(new List<Proteoform> { remaining[0] }), family_id);
                this.families.Add(new_family);
                inducted.AddRange(new_family.proteoforms);
                remaining = remaining.Except(inducted).ToList();
                foreach (Proteoform member in new_family.proteoforms) member.family = new_family;
                family_id++;
            }
        }

        public List<Proteoform> construct_family(List<Proteoform> seed)
        {
            List<Proteoform> seed_expansion = seed.SelectMany(p => p.get_connected_proteoforms().Except(seed)).ToList();
            if (seed_expansion.Except(seed).Count() == 0) return seed;
            seed.AddRange(seed_expansion);
            return construct_family(seed);
        }

        //if E in relation w/ T and TD of diff accesions, TD takes priority because has retention time evidence as well 
        public void clean_up_td_relations()
        {
            foreach (ExperimentalProteoform e in this.experimental_proteoforms.Where(e => e.accepted && e.relationships.Where(r =>
                r.accepted && r.relation_type == ProteoformComparison.et).Count() >= 1 && e.relationships.Where(r => r.relation_type == ProteoformComparison.etd).Count() == 1))
            {
                string accession = e.relationships.Where(r => r.relation_type == ProteoformComparison.etd).First().accession_1.Split('_')[0];
                foreach (ProteoformRelation relation in e.relationships.Where(r => r.relation_type == ProteoformComparison.et && r.accession_2.Split('_')[0] != accession))
                {
                    relation.accepted = false;
                }
            }
        }
    }
}

// THREADING DELTAMASSPEAK FINDING (draft, not validated)

//public List<ProteoformRelation> grouped_relations = new List<ProteoformRelation>();
//public List<ProteoformRelation> remaining_relations_outside_no_mans = new List<ProteoformRelation>();
//public List<DeltaMassPeak> accept_deltaMass_peaks(List<ProteoformRelation> relations, Dictionary<string, List<ProteoformRelation>> decoy_relations)
//{
//    //order by E intensity, then by descending unadjusted_group_count (running sum) before forming peaks, and analyze only relations outside of no-man's-land
//    List<ProteoformRelation> remaining_relations_outside_no_mans = relations.OrderByDescending(r => r.nearby_relations_count).ThenByDescending(r => r.agg_intensity_1).Where(r => r.outside_no_mans_land).ToList(); // Group count is the primary sort
//    List<DeltaMassPeak> peaks = new List<DeltaMassPeak>();

//    ProteoformRelation root = remaining_relations_outside_no_mans[0];
//    List<ProteoformRelation> running = new List<ProteoformRelation>();
//    List<Thread> active = new List<Thread>();
//    while (remaining_relations_outside_no_mans.Count > 0 || active.Count > 0)
//    {
//        while (root != null && active.Count < Environment.ProcessorCount)
//        {
//            Thread t = new Thread(new ThreadStart(root.generate_peak));
//            t.Start();
//            running.Add(root);
//            active.Add(t);
//            root = find_next_root(remaining_relations_outside_no_mans, running);
//        }

//        foreach (Thread t in active)
//        {
//            t.Join();
//        }

//        foreach (ProteoformRelation r in running)
//        {
//            peaks.Add(r.peak);
//            List<ProteoformRelation> mass_differences_in_peak = r.peak.grouped_relations;
//            relations_in_peaks.AddRange(mass_differences_in_peak);
//            grouped_relations.AddRange(mass_differences_in_peak);
//            remaining_relations_outside_no_mans = exclusive_relation_group(remaining_relations_outside_no_mans, grouped_relations);
//        }

//        running.Clear();
//        active.Clear();
//        root = find_next_root(remaining_relations_outside_no_mans, running);
//    }
//    this.delta_mass_peaks.AddRange(peaks);
//    return peaks;
//}