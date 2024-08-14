using ConfigTypeFinder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CFA
{
    public partial class ObjectCreator : Form
    {
        private string _variablePath;
        public List<ConfigVariable> CreatedVariables { get; private set; }

        public ObjectCreator(string path)
        {
            _variablePath = path;
            CreatedVariables = new List<ConfigVariable>();
            InitializeComponent();
        }

        private void ObjectCreater_Load(object sender, EventArgs e)
        {
            if (dataGridView != null)
            {
                dataGridView.Columns.Clear();
                DataGridViewTextBoxColumn nameColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Name",
                    HeaderText = "Name",
                    Width = 250,
                };

                var types = TypeManager.GetAllTypes();
                DataGridViewComboBoxColumn typeColumn = new DataGridViewComboBoxColumn
                {
                    Name = "Type",
                    HeaderText = "Type",
                    Width = 250,
                    DataSource = new BindingSource(types.Keys, null),
                    DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
                };

                DataGridViewTextBoxColumn valueColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Value",
                    HeaderText = "Value",
                    Width = 250
                };
                dataGridView.Columns.Add(nameColumn);
                dataGridView.Columns.Add(typeColumn);
                dataGridView.Columns.Add(valueColumn);
            }
        }

        private void createButton_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (row.Cells["Name"].Value != null && row.Cells["Type"].Value != null)
                {
                    var value = row.Cells["Value"].Value == null ? string.Empty : row.Cells["Value"].Value.ToString();
                    var ConfigVariable = new ConfigVariable(_variablePath, row.Cells["Name"].Value.ToString(), row.Cells["Type"].Value.ToString(), value);
                    TypeManager.ConvertTypeNameToType(ConfigVariable);
                    CreatedVariables.Add(ConfigVariable);
                    this.DialogResult = DialogResult.OK;
                    this.Close();

                }
            }

        }
        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
    public class ConfigVariableEventArgs : EventArgs
    {
        public List<ConfigVariable> Variables { get; }

        public ConfigVariableEventArgs(List<ConfigVariable> variables)
        {
            Variables = variables;
        }
    }
}
