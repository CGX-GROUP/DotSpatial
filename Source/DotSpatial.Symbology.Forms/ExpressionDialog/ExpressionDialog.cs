﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DotSpatial.Symbology;
using System.IO;
using DotSpatial.Data;
using System.Reflection;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;





namespace DotSpatial.Symbology.Forms
{
    public partial class ExpressionDialog : UserControl
    {
        public string sComplexEmpty = "def Main():\n   \n \n  return \"no value\"";

        public event EventHandler ExpressionTextChanged;

        IFeatureLayer _ActiveLayer = null;
        IDataTable _DataTable = null; // CGX AERO GLZ
        private string _Expression = "";

        public bool IsComplexExpression(string Expression)
        {
            if (!string.IsNullOrEmpty(Expression))
            {
                /*if (Expression == sComplexEmpty)
                {
                    return false;
                }*/

                if (Expression.StartsWith("def Main():"))
                {
                    return true;
                }
                else
                    return false;


            }
            return false;
        }
        public void FillExpression(string value)
        {
            if (IsComplexExpression(value))
            {
                // TB_Simple.Text = "";
                TB_Advanced.Text = value;
                TabControl.SelectedTab = TabAdvanced;
            }
            else
            {
                TB_Simple.Text = value;
                //TB_Advanced.Text = sComplexEmpty;
                TabControl.SelectedTab = TabSimple;
            }
            _Expression = value;
        }
        public string Expression
        {
            get
            {
                UpdateExpression();
                return _Expression;
            }
            set
            {
                FillExpression(value);
                _Expression = value;
            }
        }

        public ExpressionDialog()
        {
            InitializeComponent();


        }

        public IFeatureLayer ActiveLayer
        {
            set
            {
                _ActiveLayer = value;
                _DataTable = _ActiveLayer.DataSet.DataTable;
                FillListFields();
            }

        }

        private void FillListFields()
        {
            try
            {
                if (_DataTable != null) // CGX AERO GLZ
                {
                    listViewFields.Items.Clear();
                    foreach (DataColumn dc in _DataTable.Columns)
                        listViewFields.Items.Add("[" + dc.ColumnName + "]");
                }
            }
            catch (Exception ex)
            { System.Diagnostics.Debug.WriteLine(ex.ToString()); }
        }

        private void B_Preview_Click(object sender, EventArgs e)
        {
            Preview();
        }

        private void UpdateExpression()
        {
            _Expression = TB_Simple.Text;
            if (TabControl.SelectedTab == TabAdvanced)
                _Expression = TB_Advanced.Text;

        }
        private void Preview()
        {
            try
            {
                if ((_ActiveLayer as FeatureLayer).DataSet.Features.Count > 0)
                {

                    string sResult = "";


                    if (IsComplexExpression(Expression))
                    {
                        Expression = TB_Advanced.Text;
                        sResult = Compute(Expression);
                        sResult = Python.Script.EvaluateWithDialog(sResult);
                    }
                    else
                    {
                        Expression = TB_Simple.Text;
                        sResult = Compute(Expression);
                    }


                    PanelPreview.Refresh();

                    richTextBoxViewer.Text = sResult;
                }
                else
                    richTextBoxViewer.Text = "Unabled to verify the script, no feature found";
            }
            catch (Exception ex)
            { System.Diagnostics.Debug.WriteLine(ex.ToString()); }
        }


        private void listViewFields_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                if (listViewFields.SelectedIndices.Count > 0)
                {
                    string Text = listViewFields.Items[listViewFields.SelectedIndices[0]].Text;
                    if (TabControl.SelectedTab == TabSimple)
                        TB_Simple.SelectedText += Text;
                    if (TabControl.SelectedTab == TabAdvanced)
                        TB_Advanced.SelectedText += Text;
                }
                UpdateExpression();
            }
            catch (Exception ex)
            { System.Diagnostics.Debug.WriteLine(ex.ToString()); }
        }

        private void B_Import_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog OFD = new OpenFileDialog();
                OFD.Filter = "Script Files (*.py)|*.py";
                if (OFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    TB_Advanced.Clear();
                    StreamReader SR = new StreamReader(OFD.FileName);
                    TB_Advanced.Text = SR.ReadToEnd();
                    SR.Close();

                    //this.B_Preview_Click(sender, e);
                }
            }
            catch (Exception ex)
            { System.Diagnostics.Debug.WriteLine(ex.ToString()); }
        }

        private void B_Export_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog SFD = new SaveFileDialog();
                SFD.Filter = "Script Files (*.py)|*.py";
                if (SFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // Création du fichier script
                    string sScriptPath = SFD.FileName;
                    bool bContinue = true;
                    if (File.Exists(sScriptPath))
                    {
                        DialogResult DR = MessageBox.Show("File already exists.\nOverwrite ?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (DR == System.Windows.Forms.DialogResult.Yes)
                            File.Delete(sScriptPath);
                        if (DR == System.Windows.Forms.DialogResult.No)
                            bContinue = false;
                    }

                    if (bContinue)
                    {
                        StreamWriter SW = new StreamWriter(sScriptPath);
                        SW.Write(TB_Advanced.Text);
                        SW.Close();
                    }
                }
            }
            catch (Exception ex)
            { System.Diagnostics.Debug.WriteLine(ex.ToString()); }
        }



        private List<String> GetFields(string sExpression)
        {

            List<String> ListFields = new List<string>();
            string temp = sExpression;

            int iIndex = 0;
            while (iIndex < temp.Length)
            {
                int start = temp.IndexOf("[", iIndex);
                int end = temp.IndexOf("]", iIndex);

                if ((start != -1) && (end != -1))
                {
                    string sResult = temp.Substring(start + 1, (end - start) - 1);
                    ListFields.Add(sResult);
                    iIndex = end + 1;
                }
                else
                    iIndex++;
            }

            return ListFields;
        }

        private string Compute(string sExpresion)
        {
            string TextTmp = "";
            try
            {

                List<String> ListFields = GetFields(sExpresion);


                if (_ActiveLayer is FeatureLayer)
                {
                    FeatureLayer fl = _ActiveLayer as FeatureLayer;

                    if (fl.DataSet.Features.Count > 0)
                    {
                        IFeature f = fl.DataSet.Features[0];
                        // Récupération pour de la chaîne de remplacement
                        string sCompute = ReplaceFieldsByValue(GetFields(sExpresion), GetLayersFields(f), fl);

                        TextTmp += sCompute + Environment.NewLine;

                    }

                }


            }
            catch (Exception ex)
            { System.Diagnostics.Debug.WriteLine(ex.ToString()); }


            return TextTmp;
        }

        private Dictionary<string, Object> GetLayersFields(IFeature f)
        {
            Dictionary<string, Object> featureFields = new Dictionary<string, Object>();
            try
            {

                if (f is IFeature)
                {
                    IFeature feature = (IFeature)f;
                    for (int i = 0; i < feature.ParentFeatureSet.DataTable.Columns.Count; i++)
                    {
                        try
                        {
                            IDataTable DT = f.ParentFeatureSet.DataTable; // CGX AERO GLZ
                            DataColumn Column = DT.Columns[i];
                            if (Column != null)
                            {
                                if (DT.Columns.Contains(Column.ColumnName))
                                {
                                    object Row = feature.DataRow[Column.ColumnName];
                                    if (Row != DBNull.Value)
                                        featureFields.Add(Column.ColumnName, Row);
                                }
                            }
                        }
                        catch (Exception ex)
                        { System.Diagnostics.Debug.WriteLine(ex.ToString()); }
                    }
                }
            }
            catch (Exception ex)
            { System.Diagnostics.Debug.WriteLine(ex.ToString()); }



            return featureFields;
        }

        private string ReplaceFieldsByValue(List<String> ListFields, Dictionary<string, Object> featureFields, FeatureLayer fl)
        {
            string sCompute = Expression;

            try
            {
                foreach (string field in ListFields)
                {
                    foreach (string key in featureFields.Keys)
                    {
                        if (key == field)
                        {

                            if (TabControl.SelectedTab == TabSimple)
                                sCompute = sCompute.Replace("[" + field + "]", featureFields[key].ToString());
                            if (TabControl.SelectedTab == TabAdvanced)
                                sCompute = sCompute.Replace("[" + field + "]", GetFormattedField(fl, field, featureFields[key]));
                        }
                    }
                }
            }
            catch (Exception ex)
            { System.Diagnostics.Debug.WriteLine(ex.ToString()); }

            return sCompute;
        }

        private string GetFormattedField(FeatureLayer fl, string sFieldName, object oValue)
        {
            string sResult = "";

            try
            {

                DataColumn Column = fl.DataSet.DataTable.Columns[sFieldName];
                if (Column.DataType == typeof(string))
                    sResult = "\"" + oValue.ToString() + "\"";

            }
            catch (Exception ex)
            { System.Diagnostics.Debug.WriteLine(ex.ToString()); }

            return sResult;
        }

        private void TB_Simple_TextChanged(object sender, EventArgs e)
        {

            if (ExpressionTextChanged != null) ExpressionTextChanged(this, new EventArgs());


        }

        private void TB_Advanced_TextChanged(object sender, EventArgs e)
        {
            if (ExpressionTextChanged != null) ExpressionTextChanged(this, new EventArgs());
        }

        private void TB_Simple_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            e.IsInputKey = e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter;
        }

        private void TB_Advanced_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            e.IsInputKey = e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter;
        }

        private void richTextBoxViewer_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            e.IsInputKey = e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter;
        }

        private void PanelPreview_Paint(object sender, PaintEventArgs e)
        {
            string sResult = "";
            if (IsComplexExpression(Expression))
            {
                Expression = TB_Advanced.Text;
                sResult = Compute(Expression);
                sResult = Python.Script.Evaluate(sResult);
            }
            else
            {
                Expression = TB_Simple.Text;
                sResult = Compute(Expression);
            }
            if (string.IsNullOrEmpty(sResult)) { return; }


            var g = e.Graphics;
            var p = sender as Panel;

            var format = new StringFormat { Alignment = StringAlignment.Near };
            Font textFont = new System.Drawing.Font("Arial", 10);
            RectangleF currentView = new RectangleF(PanelPreview.DisplayRectangle.X + 10, PanelPreview.DisplayRectangle.Y + 10, PanelPreview.DisplayRectangle.Width, PanelPreview.DisplayRectangle.Height);

            RectangleF labelBounds = TextExpression.GetComputedLabelBounds(g, sResult, textFont, format, currentView, 1.0F);

            RectangleF fTitleClip = new RectangleF();
            string[] sSplitted = sResult.Split('\n');
            PointF pos = new PointF(labelBounds.X, labelBounds.Y);
            foreach (string sSplit in sSplitted)
            {
                using (var gp2 = new GraphicsPath())
                {
                    FontStyle fontStyle = TextExpression.GetFontFromTag(sSplit);
                    FontFamily fontFamily = textFont.FontFamily;
                    FontFamily customFontFamily = null;
                    if (TextExpression.GetFontFamilyFromTag(sSplit, out customFontFamily))
                    {
                        fontFamily = customFontFamily;
                    }
                    Font newFont = new Font(fontFamily, TextExpression.GetSizeFromTag(sSplit, textFont.SizeInPoints));
                    Color textColor = Color.Black;
                    Color tempColor = Color.Black;
                    if (TextExpression.GetColorFromTag(sSplit, out tempColor))
                    {
                        textColor = tempColor;
                    }
                    Bullet.style bulletStyle = Bullet.style.None;
                    bool bDrawBullet = TextExpression.GetBulletStyle(sSplit, out bulletStyle);

                    string sTextWithoutTag = TextExpression.GetTextWithoutTag(sSplit);
                    SizeF stringfSize = g.MeasureString(sTextWithoutTag, newFont);

                    if (!TextExpression.IsMorse(sSplit))
                    {
                        PointF textPosition = new PointF(pos.X, pos.Y);

                        if (TextExpression.IsTitle(sSplit))
                        {
                            textPosition = new PointF((labelBounds.X + labelBounds.Width / 2) - (stringfSize.Width / 2), labelBounds.Y);

                            fTitleClip = new RectangleF(textPosition, stringfSize);
                            //gp2.AddString(sTextWithoutTag, newFont.FontFamily, (int)fontStyle, newFont.Size * g.DpiY / 72F, textPosition, format);
                            e.Graphics.DrawString(sTextWithoutTag, newFont, new SolidBrush(textColor), textPosition, format);

                            labelBounds = new RectangleF(labelBounds.X, labelBounds.Y + (stringfSize.Height / 2), labelBounds.Width, labelBounds.Height - (stringfSize.Height / 2));
                        }
                        else
                        {
                            //gp2.AddString(sTextWithoutTag, newFont.FontFamily, (int)fontStyle, newFont.Size * g.DpiY / 72F, textPosition, format);
                            if (bulletStyle == Bullet.style.Left || bulletStyle == Bullet.style.Both)
                                textPosition.X += (stringfSize.Height / 2);
                            e.Graphics.DrawString(sTextWithoutTag, newFont, new SolidBrush(textColor), textPosition, format);
                        }

                        // Draw a limit above the text if needed
                        TextExpression.DrawUpperScore(g, sSplit, textPosition, textColor, newFont, 1.0F);
                        TextExpression.DrawUnderScore(g, sSplit, textPosition, textColor, newFont, 1.0F);
                        if (bDrawBullet) TextExpression.DrawBullet(g, textColor, bulletStyle, stringfSize, textPosition);

                        pos.Y += stringfSize.Height;
                    }
                    else
                    {
                        TextExpression.DrawMorseText(g, gp2, ref pos, sSplit, newFont, fontStyle, StringAlignment.Near, labelBounds);
                    }

                    // Draw a label
                    //g.FillPath(new SolidBrush(textColor), gp2);
                    gp2.Dispose();
                }
            }
            e.Graphics.DrawRectangle(new Pen(Color.Black), (int)labelBounds.X, (int)labelBounds.Y, (int)labelBounds.Width, (int)labelBounds.Height);
        }
    }
}
