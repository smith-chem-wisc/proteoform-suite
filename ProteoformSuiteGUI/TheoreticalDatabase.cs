﻿using ProteoformSuiteInternal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace ProteoformSuiteGUI
{
    public partial class TheoreticalDatabase : Form
    {
        OpenFileDialog openAccessionListDialog = new OpenFileDialog();
        bool initial_load = true;

        public TheoreticalDatabase()
        {
            InitializeComponent();
        }

        public void TheoreticalDatabase_Load(object sender, EventArgs e)
        {
            InitializeSettings();
            initial_load = false;
        }

        public void load_dgv()
        {
            DisplayUtility.FillDataGridView(dgv_Database, Lollipop.proteoform_community.theoretical_proteoforms);
            this.initialize_table_bindinglist();
            DisplayUtility.FormatTheoreticalProteoformTable(dgv_Database);
        }

        public DataGridView GetDGV()
        {
            return dgv_Database;
        }

        private void InitializeSettings()
        {
            if (Lollipop.neucode_labeled)
                btn_NeuCode_Lt.Checked = true;
            else
            {
                btn_NeuCode_Lt.Checked = false;
                btn_NaturalIsotopes.Checked = true;
            }

            nUD_MaxPTMs.Minimum = 0;
            nUD_MaxPTMs.Maximum = 5;
            nUD_MaxPTMs.Value = Lollipop.max_ptms;

            nUD_NumDecoyDBs.Minimum = 0;
            nUD_NumDecoyDBs.Maximum = 50;
            nUD_NumDecoyDBs.Value = Lollipop.decoy_databases;

            nUD_MinPeptideLength.Minimum = 0;
            nUD_MinPeptideLength.Maximum = 20;
            nUD_MinPeptideLength.Value = Lollipop.min_peptide_length;

            ckbx_combineIdenticalSequences.Checked = Lollipop.combine_identical_sequences;
            ckbx_combineTheoreticalsByMass.Checked = Lollipop.combine_theoretical_proteoforms_byMass;

            tb_modTypesToExclude.Text = String.Join(",", Lollipop.mod_types_to_exclude);

            tb_tableFilter.TextChanged -= tb_tableFilter_TextChanged;
            tb_tableFilter.Text = "";
            tb_tableFilter.TextChanged += tb_tableFilter_TextChanged;
        }


        public void FillDataBaseTable(string table)
        {
            if (table == "Target")
                DisplayUtility.FillDataGridView(dgv_Database, Lollipop.proteoform_community.theoretical_proteoforms);
            else if (Lollipop.proteoform_community.decoy_proteoforms.ContainsKey(table))
                DisplayUtility.FillDataGridView(dgv_Database, Lollipop.proteoform_community.decoy_proteoforms[table]);
        }

        private void set_Make_Database_Button()
        {
            btn_Make_Databases.Enabled = Lollipop.get_files(Lollipop.input_files, Purpose.ProteinDatabase).Count() > 0;
        }

        private void btn_Make_Databases_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            make_databases(); 
            DisplayUtility.FillDataGridView(dgv_Database, Lollipop.proteoform_community.theoretical_proteoforms);
            this.initialize_table_bindinglist();
            DisplayUtility.FormatTheoreticalProteoformTable(dgv_Database);
            this.Cursor = Cursors.Default;
        }

        public void make_databases()
        {
            Lollipop.get_theoretical_proteoforms();
            ((ProteoformSweet)MdiParent).experimentalTheoreticalComparison.ClearListsAndTables();
            tb_totalTheoreticalProteoforms.Text = Lollipop.proteoform_community.theoretical_proteoforms.Length.ToString();
        }

        public void initialize_table_bindinglist()
        {
            List<string> databases = new List<string> { "Target" };
            if (Lollipop.proteoform_community.decoy_proteoforms.Keys.Count > 0)
                foreach (string name in Lollipop.proteoform_community.decoy_proteoforms.Keys)
                    databases.Add(name);
            cmbx_DisplayWhichDB.DataSource = new BindingList<string>(databases.ToList());
        }

        private void cmbx_DisplayWhichDB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!initial_load)
            {
                string table = cmbx_DisplayWhichDB.SelectedItem.ToString();
                if (table == "Target")
                    DisplayUtility.FillDataGridView(dgv_Database, Lollipop.proteoform_community.theoretical_proteoforms);
                else
                    DisplayUtility.FillDataGridView(dgv_Database, Lollipop.proteoform_community.decoy_proteoforms[table]);
            }
        }


        // ADD AND CLEAR
        private void btn_addFiles_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = cmb_loadTable.SelectedItem.ToString();
            openFileDialog.Filter = Lollipop.file_filters[cmb_loadTable.SelectedIndex];
            openFileDialog.Multiselect = true;

            DialogResult dr = openFileDialog.ShowDialog();
            if (dr == DialogResult.OK)
                Lollipop.enter_input_files(openFileDialog.FileNames, Lollipop.acceptable_extensions[cmb_loadTable.SelectedIndex], Lollipop.file_types[cmb_loadTable.SelectedIndex], Lollipop.input_files);

            DisplayUtility.FillDataGridView(dgv_loadFiles, Lollipop.get_files(Lollipop.input_files, Lollipop.file_types[cmb_loadTable.SelectedIndex]));
            set_Make_Database_Button();
        }

        private void btn_clearFiles_Click(object sender, EventArgs e)
        {
            Lollipop.input_files = Lollipop.input_files.Except(Lollipop.get_files(Lollipop.input_files, Lollipop.file_types[cmb_loadTable.SelectedIndex])).ToList();
            DisplayUtility.FillDataGridView(dgv_loadFiles, Lollipop.get_files(Lollipop.input_files, Lollipop.file_types[cmb_loadTable.SelectedIndex]));
            set_Make_Database_Button();
        }


        // CHECKBOXES
        private void ckbx_combineIdenticalSequences_CheckedChanged(object sender, EventArgs e)
        {
            Lollipop.combine_identical_sequences = ckbx_combineIdenticalSequences.Checked;
        }

        private void ckbx_combineTheoreticalsByMass_CheckedChanged(object sender, EventArgs e)
        {
            Lollipop.combine_theoretical_proteoforms_byMass = ckbx_combineTheoreticalsByMass.Checked;
        }

        private void ckbx_OxidMeth_CheckedChanged(object sender, EventArgs e)
        {
            Lollipop.methionine_oxidation = ckbx_OxidMeth.Checked;
        }

        private void ckbx_Carbam_CheckedChanged(object sender, EventArgs e)
        {
            Lollipop.carbamidomethylation = ckbx_Carbam.Checked;
        }

        private void ckbx_Meth_Cleaved_CheckedChanged(object sender, EventArgs e)
        {
            Lollipop.methionine_cleavage = ckbx_Meth_Cleaved.Checked;
        }

        private void btn_NaturalIsotopes_CheckedChanged(object sender, EventArgs e)
        {
            Lollipop.natural_lysine_isotope_abundance = btn_NaturalIsotopes.Checked;
        }

        private void btn_NeuCode_Lt_CheckedChanged(object sender, EventArgs e)
        {
            Lollipop.neucode_light_lysine = btn_NeuCode_Lt.Checked;
        }

        private void btn_NeuCode_Hv_CheckedChanged(object sender, EventArgs e)
        {
            Lollipop.neucode_heavy_lysine = btn_NeuCode_Hv.Checked;
        }

        private void nUD_MaxPTMs_ValueChanged(object sender, EventArgs e)
        {
            Lollipop.max_ptms = Convert.ToInt32(nUD_MaxPTMs.Value);
        }

        private void nUD_NumDecoyDBs_ValueChanged(object sender, EventArgs e)
        {
            Lollipop.decoy_databases = Convert.ToInt32(nUD_NumDecoyDBs.Value);
        }

        private void nUD_MinPeptideLength_ValueChanged(object sender, EventArgs e)
        {
            Lollipop.min_peptide_length = Convert.ToInt32(nUD_MinPeptideLength.Value);
        }

        // LOAD DATABASES GRID VIEW
        private void cmb_loadTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmb_loadTable.SelectedIndex != 2) MessageBox.Show("Use the Load Deconvolution Results page to load data.");
            cmb_loadTable.SelectedIndex = 2;
        }

        private void dgv_loadFiles_DragDrop(object sender, DragEventArgs e)
        {
            drag_drop(e, cmb_loadTable, dgv_loadFiles);
        }

        private void drag_drop(DragEventArgs e, ComboBox cmb, DataGridView dgv)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Lollipop.enter_input_files(files, Lollipop.acceptable_extensions[cmb.SelectedIndex], Lollipop.file_types[cmb.SelectedIndex], Lollipop.input_files);
            DisplayUtility.FillDataGridView(dgv, Lollipop.get_files(Lollipop.input_files, Lollipop.file_types[cmb.SelectedIndex]));
        }

        public void reload_database_list()
        {
            cmb_loadTable.Items.Clear();
            cmb_loadTable.Items.AddRange(Lollipop.file_lists);
            cmb_loadTable.SelectedIndex = 2;
            DisplayUtility.FillDataGridView(dgv_loadFiles, Lollipop.get_files(Lollipop.input_files, Lollipop.file_types[cmb_loadTable.SelectedIndex]));
            set_Make_Database_Button();
        }

        private void tb_tableFilter_TextChanged(object sender, EventArgs e)
        {
            IEnumerable<object> selected_theoreticals = tb_tableFilter.Text == "" ?
                Lollipop.proteoform_community.theoretical_proteoforms :
                ExtensionMethods.filter(Lollipop.proteoform_community.theoretical_proteoforms, tb_tableFilter.Text);
            DisplayUtility.FillDataGridView(dgv_Database, selected_theoreticals);
            if (selected_theoreticals.Count() > 0) DisplayUtility.FormatTheoreticalProteoformTable(dgv_Database);
        }

        Regex substituteWhitespace = new Regex(@"\s+");
        private void tb_modTypesToExclude_TextChanged(object sender, EventArgs e)
        {
            Lollipop.mod_types_to_exclude = substituteWhitespace.Replace(tb_modTypesToExclude.Text, "").Split(',');
        }
    }
}
