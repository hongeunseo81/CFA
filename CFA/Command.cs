using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFA
{
    public enum CommandType
    {
        Create,
        Delete,
        Update,
        Insert,
        Failed
    }
    public class Command
    {
        public CommandType CommandType { get; set; }
        public ConfigVariable ParentConfigVariable { get; set; }
        public ConfigVariable ConfigVariable { get; set; }
        public int Index = -1;
        public bool IsChild { get; set; }
        public object NewValue { get; set; }
        public object OldValue { get; set; }
        public Command() { }
        public Command(CommandType commandType, ConfigVariable configVariable)
        {
            CommandType = commandType;
            ConfigVariable = configVariable;
            OldValue = configVariable.Value;
            NewValue = configVariable.DefaultValue;
        }
        public Command(CommandType commandType, ConfigVariable parentVariable, ConfigVariable configVariable)
        {
            CommandType = commandType;
            ConfigVariable = configVariable;
            ParentConfigVariable = parentVariable;
            NewValue = configVariable.DefaultValue;
        }
        public Command(CommandType commandType, ConfigVariable configVariable, object newValue)
        {
            CommandType = commandType;
            ConfigVariable = configVariable;
            OldValue = configVariable.Value;
            NewValue = newValue;
        }


    }
}
