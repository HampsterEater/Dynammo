/// -----------------------------------------------------------------
///  Dynamic MMO (Dynammo)
///  Dissertation project at University of Derby
/// -----------------------------------------------------------------
///  Written by Timothy Leonard
/// -----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization;
using System.Windows.Forms.DataVisualization.Charting;

namespace Dynammo.Common
{
    /// <summary>
    ///     Stores information about a graph data point.
    /// </summary>
    public class GraphDataPoint
    {
        public string Series;
        public int Time;
        public float Value;
    }

    /// <summary>
    ///     This class is used to update and display a graph based panel.
    /// </summary>
    public class GraphPanel : Control
    {
        #region Private members

        private Chart m_chart = null;

        private int m_max_points = 480;//40 * 10;

        private int m_first_point_time = 0;

        #endregion
        #region Properties


        #endregion
        #region Public Methods

        /// <summary>
        ///     Constructs a new instance of this class.
        /// </summary>
        public GraphPanel()
        {
            m_chart = new Chart();
            m_chart.Dock = System.Windows.Forms.DockStyle.Fill;
            m_chart.Location = new System.Drawing.Point(0, 0);
            m_chart.Size = new System.Drawing.Size(Width, Height);

            m_chart.ChartAreas.Add("ChartArea1");
            m_chart.Legends.Add("Legend1");

            Controls.Add(m_chart);
        }

        /// <summary>
        /// 
        /// </summary>
        public void AddDataPoint(string series_name, float value, int time)
        {
            Series series = null;

            if (value < 0)
            {
                value = 0;
            }

            // Try and find old series.
            foreach (Series s in m_chart.Series)
            {
                if (s.Name == series_name)
                {
                    series = s;
                    break;
                }
            }

            // No series available? Add a new one.
            if (series == null)
            {
                series = new Series(series_name, 10000);
                series.IsXValueIndexed = true;
                series.ChartType = SeriesChartType.Line;
                m_chart.Series.Add(series);
            }
            
            // Add the point to the series.
            series.Points.AddXY(time, value);
        }

        /// <summary>
        ///     Updates the panel with the new points.
        /// </summary>
        public void Update()
        {
            foreach (Series s in m_chart.Series)
            {
                // Limit the number of points available.
                while (s.Points.Count > m_max_points)
                {
                    s.Points.RemoveAt(0);
                }
            }

            m_chart.ResetAutoValues();
        }

        #endregion
    }

}
