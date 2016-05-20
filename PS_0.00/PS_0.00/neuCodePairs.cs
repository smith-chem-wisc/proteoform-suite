﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;


namespace PS_0._00
{
    public partial class NeuCodePairs : Form
    {
        public NeuCodePairs()
        {
            InitializeComponent();
            this.ct_IntensityRatio.MouseMove += new MouseEventHandler(ct_IntensityRatio_MouseMove);
            this.ct_LysineCount.MouseMove += new MouseEventHandler(ct_LysineCount_MouseMove);
        }

        public void NeuCodePairs_Load(object sender, EventArgs e)
        {
            if (Lollipop.rawNeuCodePairs.Count == 0)
            {
                Lollipop.FillRawNeuCodePairsDataTable();
                FillNeuCodePairsDGV(); //Filling DGV part of the working logic, now, since it seems to take a while
            }
            GraphLysineCount();
            GraphIntensityRatio();
        }

        private void FillNeuCodePairsDGV()
        {
            BindingSource bs = new BindingSource();
            bs.DataSource = Lollipop.rawNeuCodePairs;
            dgv_RawExpNeuCodePairs.DataSource = bs;
            dgv_RawExpNeuCodePairs.ReadOnly = true;
            //dgv_RawExpNeuCodePairs.Columns["Acceptable"].ReadOnly = false;
            //dgv_RawExpNeuCodePairs.Columns["Light Mass"].DefaultCellStyle.Format = "0.####";
            //dgv_RawExpNeuCodePairs.Columns["Light Mass Corrected"].DefaultCellStyle.Format = "0.####";
            //dgv_RawExpNeuCodePairs.Columns["Heavy Mass"].DefaultCellStyle.Format = "0.####";
            //dgv_RawExpNeuCodePairs.Columns["Intensity Ratio"].DefaultCellStyle.Format = "0.####";
            //dgv_RawExpNeuCodePairs.Columns["Apex RT"].DefaultCellStyle.Format = "0.##";
            //dgv_RawExpNeuCodePairs.Columns["Light Intensity"].DefaultCellStyle.Format = "0";
            //dgv_RawExpNeuCodePairs.Columns["Heavy Intensity"].DefaultCellStyle.Format = "0";
            dgv_RawExpNeuCodePairs.DefaultCellStyle.BackColor = System.Drawing.Color.LightGray;
            dgv_RawExpNeuCodePairs.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.DarkGray;
        }
        
        private void GraphIntensityRatio()
        {
            DataTable intensityRatioHistogram = new DataTable();
            intensityRatioHistogram.Columns.Add("intRatio", typeof(double));
            intensityRatioHistogram.Columns.Add("numPairsAtThisIntRatio", typeof(int));

            int ymax = 0;
            for (double i = 0; i <= 20; i = i + 0.05)
            {
                string expression = "[Intensity Ratio] >= " + (i - .025) + "AND [Intensity Ratio] < " + (i + .025);
                List<Proteoform> proteoforms_by_intensityRatio = Lollipop.rawNeuCodePairs.Where(p => p.intensity_ratio >= i - 0.025 && p.intensity_ratio < i + 0.025).ToList();
                if (proteoforms_by_intensityRatio.Count > ymax)
                    ymax = proteoforms_by_intensityRatio.Count;
                intensityRatioHistogram.Rows.Add(i, proteoforms_by_intensityRatio.Count);
            }

            ct_IntensityRatio.Series["intensityRatio"].XValueMember = "intRatio";
            ct_IntensityRatio.Series["intensityRatio"].YValueMembers = "numPairsAtThisIntRatio";

            yMaxIRat.Maximum = ymax;
            yMaxIRat.Minimum = 0;

            yMinIRat.Maximum = ymax;
            yMinIRat.Minimum = 0;

            xMaxIRat.Maximum = 20;
            xMaxIRat.Minimum = 0;

            xMinIRat.Maximum = 20;
            xMinIRat.Minimum = 0;

            yMaxIRat.Value = ymax;
            yMinIRat.Value = 0;
            xMaxIRat.Value = 20;
            xMinIRat.Value = 0;

            IRatMaxAcceptable.Value = Lollipop.max_intensity_ratio;
            IRatMinAcceptable.Maximum = 20;
            IRatMinAcceptable.Minimum = 0;

            IRatMinAcceptable.Value = Lollipop.min_intensity_ratio;
            IRatMinAcceptable.Maximum = 20;
            IRatMinAcceptable.Minimum = 0;

            ct_IntensityRatio.ChartAreas[0].AxisX.Title = "Intensity Ratio of a Pair";
            ct_IntensityRatio.ChartAreas[0].AxisY.Title = "Number of NeuCode Pairs";

            ct_IntensityRatio.DataSource = intensityRatioHistogram;
            ct_IntensityRatio.DataBind();

        }

        private void GraphLysineCount()
        {
            DataTable lysCtHistogram = new DataTable();
            lysCtHistogram.Columns.Add("numLysines", typeof(int));
            lysCtHistogram.Columns.Add("numPairsAtThisLysCt", typeof(int));

            int ymax = 0;
            //double xInt = 0.2;

            for (int i = 0; i <= 28; i++)
            {
                List<Proteoform> pf_by_lysCt = Lollipop.rawNeuCodePairs.Where(p => p.lysine_count == i).ToList();
                if (pf_by_lysCt.Count > ymax)
                    ymax = pf_by_lysCt.Count;
                lysCtHistogram.Rows.Add(i, pf_by_lysCt.Count);
            }

            ct_LysineCount.Series["lysineCount"].XValueMember = "numLysines";
            ct_LysineCount.Series["lysineCount"].YValueMembers = "numPairsAtThisLysCt";

            yMaxKCt.Maximum = ymax;
            yMaxKCt.Minimum = 0;

            yMinKCt.Maximum = ymax;
            yMinKCt.Minimum = 0;

            xMaxKCt.Maximum = 28;
            xMaxKCt.Minimum = 0;

            xMinKCt.Maximum = 28;
            xMinKCt.Minimum = 0;

            yMaxKCt.Value = ymax;
            yMinKCt.Value = 0;
            xMaxKCt.Value = 28;
            xMinKCt.Value = 0;

            KMaxAcceptable.Value = Lollipop.max_lysine_ct;
            KMaxAcceptable.Maximum = 28;
            KMaxAcceptable.Minimum = 0;
            KMinAcceptable.Value = Lollipop.min_lysine_ct;
            KMinAcceptable.Maximum = 28;
            KMinAcceptable.Minimum = 0;

            ct_LysineCount.ChartAreas[0].AxisX.Title = "Lysine Count";
            ct_LysineCount.ChartAreas[0].AxisY.Title = "Number of NeuCode Pairs";

            ct_LysineCount.DataSource = lysCtHistogram;
            ct_LysineCount.DataBind();

        }

        Point? ct_intensityRatio_prevPosition = null;
        ToolTip ct_intensityRatio_tt = new ToolTip();

        void ct_IntensityRatio_MouseMove(object sender, MouseEventArgs e)
        {
            tooltip_graph_display(ct_intensityRatio_tt, e, ct_IntensityRatio, ct_intensityRatio_prevPosition);
        }

        Point? ct_LysineCount_prevPosition = null;
        ToolTip ct_LysineCount_tt = new ToolTip();

        void ct_LysineCount_MouseMove(object sender, MouseEventArgs e)
        {
            tooltip_graph_display(ct_LysineCount_tt, e, ct_LysineCount, ct_LysineCount_prevPosition);
        }

        private void tooltip_graph_display(ToolTip t, MouseEventArgs e, Chart c, Point? p)
        {
            var pos = e.Location;
            if (p.HasValue && pos == p.Value) return;
            t.RemoveAll();
            p = pos;
            var results = c.HitTest(pos.X, pos.Y, false, ChartElementType.DataPoint);
            foreach (var result in results)
            {
                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    var prop = result.Object as DataPoint;
                    if (prop != null)
                    {
                        var pointXPixel = result.ChartArea.AxisX.ValueToPixelPosition(prop.XValue);
                        var pointYPixel = result.ChartArea.AxisY.ValueToPixelPosition(prop.YValues[0]);

                        // check if the cursor is really close to the point (2 pixels around the point)
                        if (Math.Abs(pos.X - pointXPixel) < 2) //&&
                                                               // Math.Abs(pos.Y - pointYPixel) < 2)
                        {
                            t.Show("X=" + prop.XValue + ", Y=" + prop.YValues[0], c, pos.X, pos.Y - 15);
                        }
                    }
                }
            }
        }

        private void yMaxKCt_ValueChanged(object sender, EventArgs e)
        {
            ct_LysineCount.ChartAreas[0].AxisY.Maximum = double.Parse(yMaxKCt.Value.ToString());
        }

        private void yMinKCt_ValueChanged(object sender, EventArgs e)
        {
            ct_LysineCount.ChartAreas[0].AxisY.Minimum = double.Parse(yMinKCt.Value.ToString());
        }

        private void xMinKCt_ValueChanged(object sender, EventArgs e)
        {
            ct_LysineCount.ChartAreas[0].AxisX.Minimum = double.Parse(xMinKCt.Value.ToString());
        }

        private void xMaxKCt_ValueChanged(object sender, EventArgs e)
        {
            ct_LysineCount.ChartAreas[0].AxisX.Maximum = double.Parse(xMaxKCt.Value.ToString());
        }

        private void yMaxIRat_ValueChanged(object sender, EventArgs e)
        {
            ct_IntensityRatio.ChartAreas[0].AxisY.Maximum = double.Parse(yMaxIRat.Value.ToString());
        }

        private void yMinIRat_ValueChanged(object sender, EventArgs e)
        {
            ct_IntensityRatio.ChartAreas[0].AxisY.Minimum = double.Parse(yMinIRat.Value.ToString());
        }

        private void xMinIRat_ValueChanged(object sender, EventArgs e)
        {
            ct_IntensityRatio.ChartAreas[0].AxisX.Minimum = double.Parse(xMinIRat.Value.ToString());
        }

        private void xMaxIRat_ValueChanged(object sender, EventArgs e)
        {
            ct_IntensityRatio.ChartAreas[0].AxisX.Maximum = double.Parse(xMaxIRat.Value.ToString());
        }

        private void parse_neucode_param_change(List<Proteoform> selected_pf)
        {
            Parallel.ForEach(selected_pf, p => { p.accepted = false; });
            dgv_RawExpNeuCodePairs.Refresh();
        }

        private void KMinAcceptable_ValueChanged(object sender, EventArgs e)
        {
            List<Proteoform> selected_pf = Lollipop.rawNeuCodePairs.Where(p => p.lysine_count < double.Parse(KMinAcceptable.Value.ToString())).ToList();
            Parallel.ForEach(selected_pf, p => { p.accepted = false; });
            dgv_RawExpNeuCodePairs.Refresh();
        }

        private void KMaxAcceptable_ValueChanged(object sender, EventArgs e)
        {
            List<Proteoform> selected_pf = Lollipop.rawNeuCodePairs.Where(p => p.lysine_count > double.Parse(KMaxAcceptable.Value.ToString())).ToList();
            Parallel.ForEach(selected_pf, p => { p.accepted = false; });
            dgv_RawExpNeuCodePairs.Refresh();
        }

        private void IRatMinAcceptable_ValueChanged(object sender, EventArgs e)
        {
            List<Proteoform> selected_pf = Lollipop.rawNeuCodePairs.Where(p => p.intensity_ratio < double.Parse(IRatMinAcceptable.Value.ToString())).ToList();
            Parallel.ForEach(selected_pf, p => { p.accepted = false; });
            dgv_RawExpNeuCodePairs.Refresh();
        }

        private void IRatMaxAcceptable_ValueChanged(object sender, EventArgs e)
        {
            List<Proteoform> selected_pf = Lollipop.rawNeuCodePairs.Where(p => p.intensity_ratio > double.Parse(IRatMaxAcceptable.Value.ToString())).ToList();
            Parallel.ForEach(selected_pf, p => { p.accepted = false; });
            dgv_RawExpNeuCodePairs.Refresh();
        }

        public override string ToString()
        {
            return String.Join(System.Environment.NewLine, new string[] {
                "NeuCodePairs|KMaxAcceptable.Value\t" + KMaxAcceptable.Value.ToString(),
                "NeuCodePairs|KMinAcceptable.Value\t" + KMinAcceptable.Value.ToString(),
                "NeuCodePairs|IRatMaxAcceptable.Value\t" + IRatMaxAcceptable.Value.ToString(),
                "NeuCodePairs|IRatMinAcceptable.Value\t" + IRatMinAcceptable.Value.ToString()
            });
        }

        public void loadSetting(string setting_specs)
        {
            string[] fields = setting_specs.Split('\t');
            switch (fields[0].Split('|')[1])
            {
                case "KMaxAcceptable.Value":
                    KMaxAcceptable.Value = Convert.ToDecimal(fields[1]);
                    break;
                case "KMinAcceptable.Value":
                    KMinAcceptable.Value = Convert.ToDecimal(fields[1]);
                    break;
                case "IRatMaxAcceptable.Value":
                    IRatMaxAcceptable.Value = Convert.ToDecimal(fields[1]);
                    break;
                case "IRatMinAcceptable.Value":
                    IRatMinAcceptable.Value = Convert.ToDecimal(fields[1]);
                    break;
            }
        }
    }
}
