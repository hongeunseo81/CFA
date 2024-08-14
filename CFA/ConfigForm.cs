using BrightIdeasSoftware;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using YamlDotNet.RepresentationModel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using CFA.Manager;
namespace CFA
{
    public partial class ConfigForm : Form
    {
        public string ConfigFilePath;
        private string _basePath;
        private ImageManager _imageManager;
        private VariableHandler _variableProvider;
        private CommandManager _commandManager;

        private bool _isExpanded = true;
        private bool _isEditMode = false;
        private System.Windows.Forms.ToolTip _toolTip;
        private ContextMenuStrip _contextMenuStrip;
        private int activeAfterIndex = 0;

        public ConfigForm(string configFilePath, object config)
        {
            InitializeComponent();
            Init(configFilePath, config );

            SetupImages();
            SetupMenuItems();
            SetupData();
        }
        
        private void Init(string configFilePath, object config)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            _basePath = Path.GetDirectoryName(assemblyPath);
            FileHandler.SetPath(_basePath, configFilePath, config);
            ConfigFileTextBox.Text = FileHandler.s_configFilePath;
            BackupPathTextBox.Text = FileHandler.s_backupFilePath;
            _imageManager = new ImageManager(_basePath);
            _variableProvider = new VariableHandler();
            _commandManager = new CommandManager(_variableProvider);
            _variableProvider.ExtractCsVariables();
            _variableProvider.ExtractYmlVariables();

            ConfigFileTextBox.Text = FileHandler.s_configFilePath;
            BackupPathTextBox.Text = FileHandler.s_backupFilePath;
        }
        private void SetupImages()
        {
            Icon = _imageManager.LogoIcon;
            LogoPictureBox.Image = _imageManager.LogoImage;
            _toolTip = new System.Windows.Forms.ToolTip();
            _imageManager.SetButtonImage(ExpandAllButton, "collapse", _imageManager.ExpandImageList, false);
            _imageManager.SetButtonImage(ModeButton, "edit-off", _imageManager.EditModeImageList, false);
            _toolTip.SetToolTip(ModeButton, "Current View: Read-Only, Click to Edit");

            _imageManager.SetButtonImage(ConfigBrowseButton, "browse", _imageManager.ButtonImageList, false);
            _toolTip.SetToolTip(ConfigBrowseButton, "Browse other config files");

            _imageManager.SetButtonImage(BackupBrowseButton, "browse", _imageManager.ButtonImageList, false);
            _toolTip.SetToolTip(BackupBrowseButton, "Change backup file path");

            _imageManager.SetButtonImage(FixAllButton, "fix", _imageManager.ButtonImageList);
            _toolTip.SetToolTip(FixAllButton, "Fix All Errors");

            _imageManager.SetButtonImage(SaveAsButton, "save-as", _imageManager.ButtonImageList);
            _toolTip.SetToolTip(SaveAsButton, "Save As");

            _imageManager.SetButtonImage(ResetButton, "reset", _imageManager.ButtonImageList);
            _toolTip.SetToolTip(ResetButton, "Reset");
        }
        private void SetupMenuItems()
        {
            _contextMenuStrip = new ContextMenuStrip();
            var addChildMenuItem = new ToolStripMenuItem("Add Child");
            var addRowMenuItem = new ToolStripMenuItem("Add Row");
            var deleteRowMenuItem = new ToolStripMenuItem("Delete Row");
            _contextMenuStrip.Items.AddRange(new ToolStripItem[] { addChildMenuItem, addRowMenuItem, deleteRowMenuItem });
            addChildMenuItem.Click += AddChildMenuItem_Click;
            addRowMenuItem.Click += AddRowMenuItem_Click;
            deleteRowMenuItem.Click += DeleteRowMenuItem_Click;
        }
        private void SetupData()
        {
            var result = _variableProvider.GetCompareResult();
            AddVariablesToDataGridView(result, VariableDataTreeListView);
            SetResultPicture();
        }

        // Form Load
        private void ConfigForm_Load(object sender, EventArgs e)
        {
            this.KeyPreview = true;
            VariableDataTreeListView.CellEditStarting += DataTreeListView_CellEditStarting;
            VariableDataTreeListView.CellEditFinishing += DataTreeListView_CellEditFinishing;
            VariableDataTreeListView.KeyDown += VariableDataTreeListView_KeyDown;

            LogListBox.MouseDoubleClick += LogListBox_DoubleClick;
            LogListBox.MouseMove += LogListBox_MouseMove;
        }


        private void LogListBox_DoubleClick(object sender, MouseEventArgs e)
        {
            int index = LogListBox.IndexFromPoint(e.Location);
            if (index >= activeAfterIndex)
            {
                var logMessage = LogListBox.SelectedItem.ToString();
                Command command = _commandManager.ConvertStringToCommand(logMessage);
                if (command != null)
                {
                    _commandManager.Execute(command);
                    RefreshResult();
                    AddCommandLog(_commandManager.GetLog());
                }
            }
        }
        private void LogListBox_MouseMove(object sender, MouseEventArgs e)
        {
            int index = LogListBox.IndexFromPoint(e.Location);
            if (index >= activeAfterIndex)
            {
                LogListBox.Cursor = Cursors.Hand;
            }
            else
            {
                LogListBox.Cursor = Cursors.Default;
            }
        }

        private void VariableDataTreeListView_KeyDown(object sender, KeyEventArgs e)
        {
            string log = null;
            if (e.Control && e.KeyCode == Keys.Z)
            {
                log = _commandManager.Undo();
                e.SuppressKeyPress = true;
                RefreshResult();

            }
            else if (e.Control && e.KeyCode == Keys.R)
            {
                log = _commandManager.Redo();
                e.SuppressKeyPress = true;
                RefreshResult();
            }
            if (log != null)
            {
                AddCommandLog(_commandManager.GetLog());
            }
        }

        private void AddVariablesToDataGridView(List<ConfigVariable> variables, DataTreeListView objectListView)
        {
            objectListView.SmallImageList = _imageManager.CommandImageList;
            objectListView.SetObjects(variables);
            objectListView.CanExpandGetter = x => ((ConfigVariable)x).HasChildren();
            objectListView.ChildrenGetter = x => ((ConfigVariable)x).Children;

            if (objectListView.AllColumns.Count == 0)
            {
                OLVColumn nameColumn = new OLVColumn("Name", "Name")
                {
                    AspectName = "Name",
                    Width = 300,
                    AspectGetter = delegate (object rowObject)
                    {
                        var ConfigVariable = (ConfigVariable)rowObject;
                        return ConfigVariable.Name;
                    },
                    ImageGetter = delegate (object rowObject)
                    {
                        var ConfigVariable = (ConfigVariable)rowObject;
                        if (ConfigVariable.Result == Result.OnlyInYml)
                        {
                            return "minus";
                        }
                        else if (ConfigVariable.Result == Result.OnlyInCs)
                        {
                            return "plus";
                        }
                        else if (ConfigVariable.Result == Result.WrongValue)
                        {
                            return "caution";
                        }
                        return null;
                    },
                    Renderer = new MultiImageRenderer()
                };

                objectListView.AllColumns.Add(nameColumn);
                objectListView.AllColumns.Add(new OLVColumn("Type", "Type") { AspectName = "Type", Width = 250 });
                objectListView.AllColumns.Add(new OLVColumn("Value", "Value") { AspectName = "Value", Width = 250 });

                objectListView.AllColumns.Add(new OLVColumn("Message", "Message")
                {
                    AspectName = "Result",
                    Width = 250,
                    AspectToStringConverter = value =>
                    {
                        if (value is Result result && (result == Result.Ok))
                        {
                            return string.Empty;
                        }
                        return value.ToString();
                    }
                });

                objectListView.FormatRow += PaintRows;
                objectListView.RebuildColumns();
            }

            objectListView.ExpandAll();
        }
        private void PaintRows(object sender, FormatRowEventArgs e)
        {
            var ConfigVariable = (ConfigVariable)e.Model;

            if (ConfigVariable.Result == Result.OnlyInCs)
            {
                e.Item.BackColor = Color.PaleGreen;

            }
            else if (ConfigVariable.Result == Result.OnlyInYml)
            {
                e.Item.BackColor = Color.LightPink;
            }
            else if (ConfigVariable.Result == Result.WrongValue)
            {
                e.Item.BackColor = Color.Gold;
            }
        }
        private void SetEditable(bool isEditable)
        {
            foreach (OLVColumn column in VariableDataTreeListView.Columns)
            {
                if (column.AspectName == "Value")
                {
                    column.IsEditable = isEditable;
                }
                else
                {
                    column.IsEditable = false;
                }
            }

            if (isEditable)
            {
                VariableDataTreeListView.CellEditActivation = ObjectListView.CellEditActivateMode.SingleClick;
                VariableDataTreeListView.BackColor = System.Drawing.ColorTranslator.FromHtml("#e8e8fa");
                VariableDataTreeListView.ForeColor = Color.Black;
                VariableDataTreeListView.CellClick += DataTreeListView_CellClick;
                VariableDataTreeListView.CellRightClick += VariableDataTreeListView_CellRightClick;
                VariableDataTreeListView.CellOver += DataTreeListView_CellOver;
            }
            else
            {
                VariableDataTreeListView.CellEditActivation = ObjectListView.CellEditActivateMode.None;
                VariableDataTreeListView.BackColor = SystemColors.Window;
                VariableDataTreeListView.ForeColor = SystemColors.ControlText;
                VariableDataTreeListView.CellClick -= DataTreeListView_CellClick;
                VariableDataTreeListView.CellRightClick -= VariableDataTreeListView_CellRightClick;
                VariableDataTreeListView.CellOver -= DataTreeListView_CellOver;
            }
        }
        private void VariableDataTreeListView_CellRightClick(object sender, CellRightClickEventArgs e)
        {
            var selectedObject = VariableDataTreeListView.GetModelObject(e.RowIndex) as ConfigVariable;
            bool isGenericType = selectedObject.Type.IsGenericType;

            foreach (ToolStripItem item in _contextMenuStrip.Items)
            {
                if (item is ToolStripMenuItem menuItem && menuItem.Text == "Add Child")
                {
                    menuItem.Visible = isGenericType;
                    break;
                }
            }
            var screenPosition = VariableDataTreeListView.PointToScreen(e.Location);
            _contextMenuStrip.Show(screenPosition);

            e.Handled = true;
        }

        // Edit cells
        private void AddChildMenuItem_Click(object sender, EventArgs e)
        {
            var selectedObject = VariableDataTreeListView.SelectedObject as ConfigVariable;
            string path = "";
            if (selectedObject != null)
            {
                string fullName = selectedObject.FullName;
                int lastIndex = fullName.LastIndexOf('.');
                if (lastIndex != -1)
                {
                    path = fullName;
                }

                using (ObjectCreator objectCreater = new ObjectCreator(path))
                {
                    if (objectCreater.ShowDialog() == DialogResult.OK)
                    {
                        List<ConfigVariable> newObjects = objectCreater.CreatedVariables;
                        var status = CommandType.Insert;
                        for (int i = newObjects.Count - 1; i >= 0; --i)
                        {
                            var command = new Command(CommandType.Insert, selectedObject, newObjects[i]);
                            command.Index = selectedObject.HasChildren() ? selectedObject.Children.Count : 0;
                            command.IsChild = true;
                            var errorMessage = _commandManager.Execute(command);
                            if (errorMessage != string.Empty)
                            {
                                MessageBox.Show("Adding row failed.");
                                status = CommandType.Failed;
                            }
                            else
                            {
                                selectedObject.Result = selectedObject.Result == Result.NoChild ? Result.Ok : selectedObject.Result;
                                VariableDataTreeListView.SetObjects(_variableProvider.YmlVariables);
                                VariableDataTreeListView.SelectedObject = newObjects[i];
                                AddCommandLog(_commandManager.GetLog());
                            }

                        }
                        VariableDataTreeListView.SelectedIndex = selectedObject.Children.Count + newObjects.Count;
                        RefreshResult();
                    }
                }
            }
        }
        private void AddRowMenuItem_Click(object sender, EventArgs e)
        {
            var selectedObject = VariableDataTreeListView.SelectedObject as ConfigVariable;
            var selectedIndex = VariableDataTreeListView.SelectedIndex;
            string path = "";
            if (selectedObject != null)
            {
                string fullName = selectedObject.FullName;
                int lastIndex = fullName.LastIndexOf('.');
                if (lastIndex != -1)
                {
                    path = fullName.Substring(0, lastIndex);
                }
            }
            using (ObjectCreator objectCreater = new ObjectCreator(path))
            {
                if (objectCreater.ShowDialog() == DialogResult.OK)
                {
                    var status = CommandType.Insert;
                    List<ConfigVariable> newObjects = objectCreater.CreatedVariables;
                    for (int i = newObjects.Count - 1; i >= 0; --i)
                    {
                        var parent = _variableProvider.GetParent(selectedObject.FullName);
                        var children = parent == null ? _variableProvider.YmlVariables : parent.Children;
                        var index = children.IndexOf(selectedObject) + 1;
                        var command = new Command(CommandType.Insert, parent, newObjects[i]);
                        command.Index = index;
                        var errorMessage = _commandManager.Execute(command);
                        if (errorMessage != string.Empty)
                        {
                            MessageBox.Show("Adding row failed.");
                            status = CommandType.Failed;
                        }
                        else
                        {
                            VariableDataTreeListView.SetObjects(_variableProvider.YmlVariables);
                            VariableDataTreeListView.SelectedObject = newObjects[i];
                            AddCommandLog(_commandManager.GetLog());
                        }
                    }
                    VariableDataTreeListView.SelectedIndex = selectedIndex + newObjects.Count;
                    RefreshResult();
                }
            }
        }
        private void DeleteRowMenuItem_Click(object sender, EventArgs e)
        {
            var selectedObject = VariableDataTreeListView.SelectedObject as ConfigVariable;
            if (selectedObject != null)
            {

                CommandType status = CommandType.Delete;
                var parent = _variableProvider.GetParent(selectedObject.FullName);
                var index = _variableProvider.GetParentVariables(selectedObject.FullName).IndexOf(selectedObject) + 1;
                var command = new Command(CommandType.Insert, parent, selectedObject);
                command.Index = index;
                if (_commandManager.Execute(new Command(CommandType.Delete, selectedObject)) != string.Empty)
                {
                    MessageBox.Show("Row deletion failed.");
                    status = CommandType.Failed;
                }
                else
                {
                    VariableDataTreeListView.SetObjects(_variableProvider.YmlVariables);
                    AddCommandLog(_commandManager.GetLog());
                    RefreshResult();
                }
            }
        }
        private void DataTreeListView_CellOver(object sender, CellOverEventArgs e)
        {
            if (e.Model == null || e.SubItem == null)
                return;

            var ConfigVariable = (ConfigVariable)e.Model;
            if (ConfigVariable.Result != Result.Ok && ConfigVariable.Result != Result.NoChild)
            {
                Cursor = Cursors.Hand;
            }
            else
            {
                Cursor = Cursors.Default;
            }
        }
        private void DataTreeListView_CellClick(object sender, CellClickEventArgs e)
        {
            if (e.Model == null)
                return;

            var ConfigVariable = (ConfigVariable)e.Model;
            if (ConfigVariable.Result != Result.Ok && ConfigVariable.Result != Result.NoChild)
            {
                CommandType status = FixError(ConfigVariable.Result, ConfigVariable);
                if (status != CommandType.Failed)
                {
                    _variableProvider.RemoveError(ConfigVariable.FullName);
                    ConfigVariable.Result = Result.Ok;
                    SetResultPicture();
                }
            }
        }
        private void DataTreeListView_CellEditStarting(object sender, CellEditEventArgs e)
        {
            ConfigVariable ConfigVariable = (ConfigVariable)e.RowObject;

            if (ConfigVariable.TypeName != typeof(Dictionary<,>).Name && ConfigVariable.TypeName != typeof(List<>).Name)
            {
                e.Cancel = false;
                e.Control.Bounds = e.CellBounds;
                e.Control.Width = e.CellBounds.Width;
                e.Control.Height = e.CellBounds.Height;

                if (ConfigVariable.Type == typeof(bool))
                {
                    CheckBox cb = new CheckBox();
                    cb.Bounds = e.CellBounds;
                    cb.Checked = ConfigVariable.Value.ToString() == "True" ? true : false;
                    cb.CheckedChanged += (s, args) =>
                    {
                        e.NewValue = cb.Checked;
                    };
                    e.Control = cb;
                }
            }
            else
            {
                e.Cancel = true;
            }
        }
        private void DataTreeListView_CellEditFinishing(object sender, CellEditEventArgs e)
        {
            ConfigVariable variable = (ConfigVariable)e.RowObject;

            if (e.NewValue != null)
            {
                if (e.Value != null && e.Value.ToString() != e.NewValue.ToString())
                {
                    var message = _commandManager.Execute(new Command(CommandType.Update, variable, e.NewValue));
                    if (message != string.Empty)
                    {
                        e.Cancel = true;
                        MessageBox.Show(message);
                    }
                    else
                    {
                        AddCommandLog(_commandManager.GetLog());
                    }

                }
            }
        }
        // Fix Error
        private CommandType FixError(Result result, ConfigVariable configVariable)
        {
            CommandType command = CommandType.Failed;
            switch (result)
            {
                case Result.OnlyInCs:
                    if (_commandManager.Execute(new Command(CommandType.Create, configVariable)) == string.Empty)
                    {
                        command = CommandType.Create;
                    }
                    break;
                case Result.OnlyInYml:
                    if (_commandManager.Execute(new Command(CommandType.Delete, configVariable)) == string.Empty)
                    {
                        VariableDataTreeListView.RemoveObject(configVariable);
                        command = CommandType.Delete;
                    }
                    break;
                case Result.WrongValue:
                    if (configVariable.HasChildren())
                    {
                        configVariable.Children.Clear();
                        VariableDataTreeListView.RemoveObjects(configVariable.Children);
                    }
                    if (_commandManager.Execute(new Command(CommandType.Update, configVariable)) == string.Empty)
                    {
                        command = CommandType.Update;
                    }
                    break;
                default:
                    break;
            }
            AddCommandLog(_commandManager.GetLog());
            return command;
        }

        // Toggle 
        private void ModeButton_Click(object sender, EventArgs e)
        {
            if (_isEditMode)
            {
                _isEditMode = false;
                ModeButton.Image = _imageManager.EditModeImageList.Images["edit-off"];
                _toolTip.SetToolTip(ModeButton, "Current View: Read View, Click to Edit View");
            }
            else
            {
                _isEditMode = true;
                ModeButton.Image = _imageManager.EditModeImageList.Images["edit-on"];
                _toolTip.SetToolTip(ModeButton, "Current View: Edit, Click to Read View");
            }
            modeLabel.Text = _isEditMode ? "On" : "Off";
            SetEditable(_isEditMode);
        }
        private void ExpandAllButton_Click(object sender, EventArgs e)
        {
            if (_isExpanded)
            {
                VariableDataTreeListView.CollapseAll();
                ExpandAllButton.Image = _imageManager.ExpandImageList.Images["expand"];
                _isExpanded = false;
            }
            else
            {
                VariableDataTreeListView.ExpandAll();
                ExpandAllButton.Image = _imageManager.ExpandImageList.Images["collapse"];
                _isExpanded = true;
            }
        }

        // Control Buttons
        private void ResetButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Do you really want to reset?");
            if (result == DialogResult.OK)
            {
                _variableProvider.ExtractYmlVariables();
                SetupData();
                AddResetLog(FileHandler.s_configFilePath);
            }

        }
        private void SaveAsButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "YAML files (*.yml)|*.yml";
            saveFileDialog.Title = "Save a Config file";
            saveFileDialog.FileName = "config.yml";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = saveFileDialog.FileName;
                YamlMappingNode root = _variableProvider.ConvertYamlFromCode();
                FileHandler.Save(root, filePath);
                MessageBox.Show("File saved successfully at: " + filePath);
            }
        }
        private void FixAllButton_Click(object sender, EventArgs e)
        {
            var errors = _variableProvider.GetErrors();
            if (errors != null && errors.Count > 0)
            {
                List<string> fixedErrorsList = new List<string>();
                foreach (var key in errors.Keys)
                {
                    var status = FixError(errors[key].Result, errors[key]);
                    if (status == CommandType.Failed)
                    {
                        MessageBox.Show("A problem occurred while fixing the error.");
                        return;
                    }
                    else
                    {
                        fixedErrorsList.Add(key);
                    }
                }
                foreach (var fixedError in fixedErrorsList)
                {
                    _variableProvider.RemoveError(fixedError);
                }
                RefreshResult();
            }
            else
            {
                MessageBox.Show("There are no errors to fix.");
            }
        }

        private void RefreshResult()
        {
            var compareResult = _variableProvider.GetCompareResult();
            AddVariablesToDataGridView(compareResult, VariableDataTreeListView);
            SetResultPicture();
        }
        private void SetResultPicture()
        {
            var errors = _variableProvider.GetErrors();
            if (errors != null && errors.Count > 0)
            {
                resultPictureBox.Image = _imageManager.ResultImageList.Images[1];
            }
            else
            {
                resultPictureBox.Image = _imageManager.ResultImageList.Images[0];
            }
        }
        private void NextButton_Click(object sender, EventArgs e)
        {
            var errors = _variableProvider.GetErrors();
            if (errors != null && errors.Count > 0)
            {
                MessageBox.Show("There is an error.");
                SetResultPicture();
            }
            else
            {
                using (var dialog = new BackupDialogForm())
                {
                    var result = dialog.ShowDialog(this);
                    if (result == DialogResult.OK)
                    {
                        if (dialog.BackupChecked)
                        {
                            FileHandler.MakeBackup();
                        }
                        YamlMappingNode rootDocument = _variableProvider.ConvertYamlFromCode();
                        FileHandler.Save(rootDocument, null);
                        ConfigFilePath = FileHandler.s_configFilePath;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
            }
        }
        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void ConfigBrowseButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "YAML files (*.yml)|*.yml";
                openFileDialog.Title = "Select a Config file";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var oldFile = FileHandler.s_configFilePath;
                    var newFile = openFileDialog.FileName;
                    FileHandler.SetConfigFilePath(openFileDialog.FileName, ConfigFileTextBox);
                    AddFileLog("Config", oldFile, newFile);
                    activeAfterIndex = LogListBox.Items.Count;
                    _variableProvider.ExtractYmlVariables();
                    SetupData();
                }
            }

        }
        private void BackupBrowseButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select a folder to back up your data.";
                folderBrowserDialog.ShowNewFolderButton = true;

                DialogResult result = folderBrowserDialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                {
                    var oldFolder = FileHandler.s_backupFilePath;
                    var newFolder = folderBrowserDialog.SelectedPath;
                    FileHandler.SetBackupPath(newFolder, BackupPathTextBox);
                    AddFileLog("Backup path", oldFolder, newFolder);
                }
            }
        }
        private void AddResetLog(string fileName)
        {
            StringBuilder logMessage = new StringBuilder($"[{DateTime.Now}]: ").Append($"{fileName} has been reset.");
            LogListBox.Items.Add(logMessage);
            LogListBox.SelectedIndex = LogListBox.Items.Count - 1;
        }
        private void AddFileLog(string fileType, string oldFile, string newFile)
        {
            StringBuilder logMessage = new StringBuilder($"[{DateTime.Now}]: ").Append($"{fileType} has been changed. {oldFile} -> {newFile}");
            LogListBox.Items.Add(logMessage);
            LogListBox.SelectedIndex = LogListBox.Items.Count - 1;
        }
        private void AddCommandLog(string logMessage)
        {
            LogListBox.Items.Add(logMessage);
            LogListBox.SelectedIndex = LogListBox.Items.Count - 1;
        }

    }
}
