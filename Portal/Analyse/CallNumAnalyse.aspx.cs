﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using DevExpress.XtraCharts;
using JN.Esb.Portal.ServiceMgt.日志分析;
using DevExpress.Web.ASPxEditors;
using DevExpress.XtraCharts.Web;

public partial class Analyse_CallNumAnalyse : BasePage
{
    AuditAnalyseService aas = new AuditAnalyseService();

    ASPxComboBox cbType
    {
        get
        {
            return this.mnuToolbar.Items[5].FindControl("cbType") as ASPxComboBox;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        HideSourceCodeTable();

        if (!IsCallback && !IsPostBack)
            InitPage();

        if(!IsCallback)
            BindingData();
    }

    protected void InitPage()
    {
        mnuToolbar.AutoSeparators = DevExpress.Web.ASPxMenu.AutoSeparatorMode.None;
    }

    /// <summary>
    /// 绑定到按服务细分的数据
    /// </summary>
    /// <returns></returns>
    private void BindingDataByService()
    {
        chart.DataSource = aas.GetCallNumAnalyseDataByService();

        //((XYDiagram)chart.Diagram).Rotated = false;

        ChartTitle ct = new ChartTitle();
        ct.Text = "按服务细分的调用次数统计";
        chart.Titles.Clear();
        chart.Titles.Add(ct);

        chart.Series.Clear();
        chart.SeriesDataMember = "ServiceName";

        chart.SeriesTemplate.View = new StackedBarSeriesView();
        chart.SeriesTemplate.ValueDataMembers.AddRange(new string[] { "NUM" });
        chart.SeriesTemplate.ArgumentDataMember = "BusinessName";
        chart.SeriesTemplate.PointOptions.PointView = PointView.Values;
        chart.SeriesTemplate.PointOptions.ValueNumericOptions.Format = NumericFormat.General;

        chart.SeriesNameTemplate.BeginText = "服务:";

        chart.DataBind();
    }

    /// <summary>
    /// 获取到所有的数据
    /// </summary>
    /// <returns></returns>
    private void BindingAllData()
    {
        ChartTitle ct = new ChartTitle();
        ct.Text = "所有调用次数统计";
        chart.Titles.Clear();
        chart.Titles.Add(ct);
        chart.SeriesTemplate.View = new PieSeriesView();

        Series series1 = new Series("Line Series 1", ViewType.Pie);
        DataTable dt = aas.GetCallNumAnalyseData();

        for (int i = 0; i < dt.Rows.Count; i++)
        {
            string title = dt.Rows[i]["BusinessFullName"].ToString();
            double num = double.Parse(dt.Rows[i]["NUM"].ToString());

            series1.Points.Add(new SeriesPoint(title, new double[] { num }));
        }

        ((PieSeriesLabel)series1.Label).Position = PieSeriesLabelPosition.Outside;
        ((PiePointOptions)series1.PointOptions).PointView = PointView.Values;
        ((PiePointOptions)series1.PointOptions).PercentOptions.ValueAsPercent = true;
        ((PiePointOptions)series1.PointOptions).ValueNumericOptions.Format = NumericFormat.Percent;
        ((PiePointOptions)series1.PointOptions).ValueNumericOptions.Precision = 0;

        series1.LegendPointOptions.PointView = PointView.ArgumentAndValues;
        series1.LegendPointOptions.ValueNumericOptions.Format = NumericFormat.FixedPoint;
        series1.LegendPointOptions.ValueNumericOptions.Precision = 0;
        ((PiePointOptions)series1.LegendPointOptions).PercentOptions.ValueAsPercent = false;

        chart.Series.Add(series1);
    }

    protected void chart_CustomCallback(object sender, CustomCallbackEventArgs e)
    {
        if (e.Parameter == "Type")
            BindingData();
    }

    private void BindingData()
    {
        if(cbType == null)
            BindingAllData();
        else
            if (cbType.Value == "all")
                BindingAllData();
            else
                BindingDataByService();
    }

}
