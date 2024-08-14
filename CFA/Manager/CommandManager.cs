using DevExpress.Utils.Filtering;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CFA;
namespace CFA.Manager
{
    public class CommandManager
    {
        private Stack<Command> _undoStack { get; set; }
        private Stack<Command> _redoStack { get; set; }
        private VariableHandler _variableHandler { get; set; }
        private string _log { get; set; }
        public CommandManager(VariableHandler variableHandler)
        {
            _undoStack = new Stack<Command>();
            _redoStack = new Stack<Command>();
            _variableHandler = variableHandler;
        }

        public string Execute(Command command)
        {
            if(command.ParentConfigVariable == null && command.Index <0)
            {
                command.ParentConfigVariable = _variableHandler.GetParent(command.ConfigVariable.FullName);
                var parentChildren = command.ParentConfigVariable == null ? _variableHandler.YmlVariables : command.ParentConfigVariable.Children;  
                command.Index = parentChildren.IndexOf(command.ConfigVariable) ;
            }
            var log = ExecuteCommand(command, isUndo: false);
            if (string.IsNullOrEmpty(log))
            {
                _undoStack.Push(command);
                _redoStack.Clear();
            }

            return log;
        }
        public void MakeLog(string errorMessage, Command command, CommandType commandType, object oldValue, object newValue)
        {
            if(errorMessage != string.Empty)
            {
                _log = errorMessage;
            }
            else
            {
                StringBuilder logMessage = new StringBuilder();
                var variable = command.ConfigVariable;
                var fullName = variable.FullName;
                logMessage.Append($"[{DateTime.Now}] {commandType} {fullName} value: ");
                if (commandType == CommandType.Update)
                {
                    logMessage.Append($"{oldValue} -> {newValue}");
                }
                else
                {
                    logMessage.Append($" as {command.Index} th children.");
                }

                _log = logMessage.ToString();
            }
            
        }
        public string GetLog()
        { 
            return _log; 
        }

        public string Undo()
        {
            string log = null;
            if (_undoStack.Count > 0)
            {
                var command = _undoStack.Pop();
                ExecuteCommand(command, isUndo: true);
                _redoStack.Push(command);
                log = GetLog();
            }
            return log;
        }
        
        private string ExecuteCommand(Command command, bool isUndo)
        {
            var configVariable = command.ConfigVariable;
            var parentConfigVariable = command.ParentConfigVariable;
            var index = command.Index;
            var errorMessage = "A problem occurred while editing the cell.";
            var commandType = command.CommandType;
            object newValue = "";
            object oldValue = "";
            switch (command.CommandType)
            {
                case CommandType.Create:
                    if (isUndo)
                    {
                        commandType = CommandType.Delete;
                        errorMessage = _variableHandler.RemoveVariable(parentConfigVariable, configVariable) ? string.Empty : errorMessage;
                    }   
                    else
                    {
                        errorMessage = _variableHandler.AddVariable(parentConfigVariable, configVariable, index) ? string.Empty : errorMessage;
                    }
                    break;
                case CommandType.Delete:
                    if (isUndo)
                    {
                        commandType = CommandType.Create;
                        errorMessage = _variableHandler.AddVariable(parentConfigVariable, configVariable, index) ? string.Empty : errorMessage;
                    }
                    else
                    {
                        errorMessage = _variableHandler.RemoveVariable(parentConfigVariable, configVariable) ? string.Empty : errorMessage;
                    }
                    break;
                case CommandType.Update:
                    newValue = isUndo ? command.OldValue : command.NewValue;
                    oldValue = isUndo ? command.NewValue : command.OldValue;

                    newValue = newValue == null ? string.Empty : newValue;
                    oldValue = oldValue == null ? string.Empty : oldValue;
                    errorMessage = _variableHandler.UpdateChild(configVariable.FullName, newValue, isUndo);
                    break;
                case CommandType.Insert:
                    if (isUndo)
                    {
                        commandType = CommandType.Delete;
                        errorMessage = _variableHandler.RemoveVariable(parentConfigVariable, configVariable) ? string.Empty : errorMessage;
                    }
                    else
                    {
                        errorMessage = _variableHandler.InsertVariable(parentConfigVariable, configVariable, command.Index,command.IsChild) ? string.Empty : errorMessage;
                    }
                    break;
                default:
                    errorMessage = "Invalid command type.";
                    break;
            }
            MakeLog(errorMessage, command, commandType, oldValue, newValue);
            return errorMessage;
        }
        public string Redo()
        {
            string log = null;
            if (_redoStack.Count > 0)
            {
                var command = _redoStack.Pop();
                ExecuteCommand(command, isUndo: false);
                _undoStack.Push(command);

                log = GetLog();
            }
            return log;
        }

        public Command ConvertStringToCommand(string logMessage)
        {
            var info = logMessage.Split(']')[1].Split(' ');
            var commandType = info[1];
            Command command = GetCommandType(commandType);
            if (command == null) 
            { 
                return null; 
            }
            var variableFullName = info[2];
            var value = info[4];
            ConfigVariable variable = new ConfigVariable(variableFullName, value.GetType().Name, value);
            command.ParentConfigVariable = _variableHandler.GetParent(variableFullName);
            if (command.CommandType == CommandType.Update)
            {
                command.OldValue = value;
                command.NewValue = info[6];
            }
            else
            {
                if (int.TryParse(info[6], out int index))
                {
                    command.Index = index;
                }
                else
                {
                    command.Index = 0;
                }
            }
            command.ConfigVariable = variable;
            return command;
        }
        public Command GetCommandType(string type)
        {
            Command command = new Command();
            if (type == CommandType.Update.ToString())
            {
                command.CommandType = CommandType.Update;
            }
            else if (type == CommandType.Create.ToString())
            {
                command.CommandType = CommandType.Create;
            }
            else if (type == CommandType.Delete.ToString())
            {
                command.CommandType = CommandType.Delete;
            }
            else if (type == CommandType.Insert.ToString())
            {
                command.CommandType = CommandType.Insert;
            }
            else
            {
                command = null;
            }
            return command;

        }

    }
}
