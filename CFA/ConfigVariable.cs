using CFA;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CFA
{
    public class ConfigVariable
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public Type Type { get; set; }
        public string TypeName { get; set; }
        public object Value { get; set; }
        public object DefaultValue { get; set; }
        public Result Result { get; set; }
        public List<ConfigVariable> Children { get; set; }
        public List<ConfigVariable> EnumValues { get; set; }
        public ConfigVariable()
        {
            Children = new List<ConfigVariable>();
        }
        public ConfigVariable(string path, string name, string typeName, Object value)
        {
            Name = name;
            FullName = path == string.Empty ? name : $"{path}.{Name}";
            TypeName = typeName;
            Value = value;
            DefaultValue = string.Empty;
            Result = Result.Ok;
            Children = new List<ConfigVariable>();
        }
        public ConfigVariable(string name, Type type, object value)
        {
            FullName = name;
            Name = FullName.Split('.').Last();
            Type = type;
            TypeName = type.Name;
            Children = new List<ConfigVariable>();
            Value = value;
        }
        public ConfigVariable(string name, string typeName, object value)
        {
            FullName = name;
            Name = FullName.Split('.').Last();
            Type = typeof(string);
            TypeName = typeName;
            Children = new List<ConfigVariable>();
            Value = value;
        }

        public void SetEnumValues(List<ConfigVariable> variables)
        {
            if (EnumValues == null)
            {

                EnumValues = new List<ConfigVariable>();
            }
            EnumValues = variables;
            DefaultValue = variables[0].Value;
        }
        public bool HasChildren()
        {
            return Children.Count > 0;
        }
        public bool IsEnumType()
        {
            return EnumValues != null && EnumValues.Count > 0;
        }
    }
}
