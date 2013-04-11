using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ServerBase.Transaction
{
    public partial class ReadXMLResult : Form
    {
        public ReadXMLResult(DataSet ds)
        {
            InitializeComponent();
            this.dataGridView1.DataSource = ds;
        }
    }
}
