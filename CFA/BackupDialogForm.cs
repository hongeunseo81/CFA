using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace CFA
{
    public partial class BackupDialogForm : Form
    {
        public bool BackupChecked { get; private set; }
        public BackupDialogForm()
        {
            InitializeComponent();
        }

        private void BackupDialogForm_Load(object sender, EventArgs e)
        {
            this.StartPosition = FormStartPosition.CenterParent;
            YesButton.Click += YesButton_Click;
            NoButton.Click += NoButton_Click;
        }

        private void YesButton_Click(object sender, EventArgs e)
        {
            this.BackupChecked = BackupCheckBox.Checked;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void NoButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;
            this.Close();
        }

    }
}
