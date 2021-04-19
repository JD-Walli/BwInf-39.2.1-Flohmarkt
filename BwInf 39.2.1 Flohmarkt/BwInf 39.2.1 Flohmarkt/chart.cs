using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BwInf_39._2._1_Flohmarkt {
    public partial class chart : Form {
        public chart(float[] xValues, float[] yValues) {
            InitializeComponent();
            for (int i = 0; i < xValues.Length; i++) {
                chart1.Series["Energien"].Points.AddXY(xValues[i], yValues[i]);
            }
        }

        private void chart_Load(object sender, EventArgs e) {

        }
    }
}
