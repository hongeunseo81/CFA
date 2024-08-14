using CoPick.Setting;
using ConfigTypeFinder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using YamlDotNet.RepresentationModel;

namespace CFA
{
    public class VariableHandler
    {
        private Object _instance;
        private YamlMappingNode _root;
        private Object _config;
        private ConfigValidator _configValidator;
        private Dictionary<String, ConfigVariable> _errorVariables = new Dictionary<string, ConfigVariable>();
        public List<ConfigVariable> CsVariables;
        public List<ConfigVariable> YmlVariables;

        public VariableHandler()
        {
            _config = FileHandler.s_config;
            _configValidator = new ConfigValidator();
            TypeManager.Init();
        }

        public void ExtractYmlVariables()
        {
            _root = FileHandler.s_root;
            YmlVariables = new List<ConfigVariable>();
            ExtractYmlVariablesRecursive(_root, YmlVariables, "");
        }
        private void ExtractYmlVariablesRecursive(YamlNode node, List<ConfigVariable> variables, string prefix)
        {
            if (node is YamlMappingNode mappingNode)
            {
                foreach (var entry in mappingNode.Children)
                {
                    var key = ((YamlScalarNode)entry.Key).Value;
                    var fullName = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";

                    if (entry.Value is YamlMappingNode)
                    {
                        var childInfo = new ConfigVariable(fullName, typeof(Dictionary<,>), string.Empty);
                        ExtractYmlVariablesRecursive(entry.Value, childInfo.Children, fullName);
                        variables.Add(childInfo);
                    }
                    else if (entry.Value is YamlSequenceNode)
                    {
                        var childInfo = new ConfigVariable(fullName, typeof(List<>), string.Empty);
                        ExtractYmlVariablesRecursive(entry.Value, childInfo.Children, fullName);
                        variables.Add(childInfo);
                    }
                    else
                    {
                        var type = typeof(string);
                        variables.Add(new ConfigVariable(fullName, type, (entry.Value)));
                    }
                }
            }
            else if (node is YamlSequenceNode sequenceNode)
            {
                int index = 0;
                foreach (var childNode in sequenceNode.Children)
                {
                    var childPrefix = $"{prefix}.[{index}]";

                    if (childNode is YamlMappingNode)
                    {
                        var childInfo = new ConfigVariable(childPrefix, typeof(Dictionary<,>), string.Empty);
                        ExtractYmlVariablesRecursive(childNode, childInfo.Children, childPrefix);
                        variables.Add(childInfo);
                    }
                    else if (childNode is YamlSequenceNode)
                    {
                        var childInfo = new ConfigVariable(childPrefix, typeof(List<>), string.Empty);
                        ExtractYmlVariablesRecursive(childNode, childInfo.Children, childPrefix);
                        variables.Add(childInfo);
                    }
                    else if (childNode is YamlScalarNode scalarNode)
                    {
                        variables.Add(new ConfigVariable(childPrefix, typeof(string), scalarNode.Value));
                    }
                    index++;
                }
            }
            else if (node is YamlScalarNode scalarNode)
            {
                variables.Add(new ConfigVariable(prefix, typeof(string), scalarNode.Value));
            }
        }

        public void ExtractCsVariables()
        {
            CsVariables = new List<ConfigVariable>();
            var type = _config.GetType();
            _instance = Activator.CreateInstance(type);
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object defaultValue = GetDefaultValue(property, _instance);
                ExtractCsVariablesRecursive(property.Name, property.PropertyType, defaultValue, CsVariables);
            }
        }
        private void ExtractCsVariablesRecursive(string propertyName, Type propertyType, Object value, List<ConfigVariable> variables)
        {
            ConfigVariable ConfigVariable;
            if (!propertyType.IsGenericType)
            {
                ConfigVariable = new ConfigVariable(propertyName, propertyType, value);
                if (propertyType.IsEnum)
                {
                    ConfigVariable.SetEnumValues(ExtractEnumValues(ConfigVariable, propertyType));
                    if (propertyName.Split('.').Last() != "Key")
                    {
                        TypeManager.AddType(ConfigVariable.TypeName, ConfigVariable.Type);
                    }
                }
                variables.Add(ConfigVariable);
                return;
            }
            Type genericTypeDefinition = propertyType.GetGenericTypeDefinition();
            Type[] genericArgs = propertyType.GetGenericArguments();
            ConfigVariable = new ConfigVariable(propertyName, propertyType, string.Empty);
            if (genericTypeDefinition == typeof(Dictionary<,>))
            {
                ExtractCsVariablesRecursive($"{propertyName}.Key", genericArgs[0], "", ConfigVariable.Children);
                ExtractCsVariablesRecursive($"{propertyName}.Value", genericArgs[1], "", ConfigVariable.Children);

            }
            else if (genericTypeDefinition == typeof(List<>))
            {
                ExtractCsVariablesRecursive($"{propertyName}.Item", genericArgs[0], "", ConfigVariable.Children);
            }
            variables.Add(ConfigVariable);
        }

        private List<ConfigVariable> ExtractEnumValues(ConfigVariable ConfigVariable, Type propertyType)
        {
            List<ConfigVariable> enumValues = new List<ConfigVariable>();
            foreach (var item in propertyType.GetEnumValues())
            {
                var enumField = propertyType.GetField(item.ToString());
                var validatorAttributeData = enumField.GetCustomAttributesData().FirstOrDefault(x => x.AttributeType == typeof(ValidatorTypeAttribute));

                if (validatorAttributeData == null)
                {
                    Type type = typeof(string);
                    var converterAttribute = enumField.GetCustomAttributes(typeof(TypeConverterAttribute), false).FirstOrDefault() as TypeConverterAttribute;
                    if (converterAttribute != null)
                    {
                        if (converterAttribute.ConverterTypeName.Contains("Boolean"))
                        {
                            type = typeof(bool);
                        }
                    }
                    enumValues.Add(new ConfigVariable(enumField.Name, type, ""));
                }
                else
                {
                    var constructorArguments = validatorAttributeData.ConstructorArguments;
                    var validatorType = (ValidatorType)constructorArguments[0].Value;
                    TypeManager.MakeFunction(enumField.Name, validatorType, constructorArguments[1].ToString());
                    enumValues.Add(new ConfigVariable(enumField.Name, validatorType.ToString(), ""));
                }
            }
            return enumValues;
        }
        private object GetDefaultValue(PropertyInfo property, object instance)
        {
            if (instance != null)
            {
                var value = property.GetValue(instance);
                if (value != null)
                {
                    return value;
                }
                
            }
            return string.Empty;
        }
        public List<ConfigVariable> GetCompareResult()
        {
            var result = _configValidator.CompareVariables(YmlVariables, CsVariables);
            _errorVariables = _configValidator.ErrorVariables;
            return result;
        }
        public List<ConfigVariable> GetParentVariables(string fullName)
        {
            var names = fullName.Split('.');
            int depth = names.Length;

            ConfigVariable parent = null;
            var currentList = YmlVariables;

            for (int i = 0; i < depth - 1; i++)
            {
                var currentVar = currentList.Find(v => v.Name == names[i]);
                if (currentVar == null)
                {
                    return null;
                }
                parent = currentVar;
                currentList = currentVar.Children;
            }
            return parent == null ? YmlVariables : parent.Children;
        }

        public ConfigVariable GetParent(string fullName)
        {
            var names = fullName.Split('.');
            int depth = names.Length;

            ConfigVariable parent = null;
            var currentList = YmlVariables;

            for (int i = 0; i < depth - 1; i++)
            {
                var currentVar = currentList.Find(v => v.Name == names[i]);
                if (currentVar == null)
                {
                    return null;
                }
                parent = currentVar;
                currentList = currentVar.Children;
            }
            return parent;
        }

        public bool InsertVariable(ConfigVariable parentVariable, ConfigVariable newVariable, int index, bool isChild = false)
        {
            index = index < 0 ? 0 : index;
            if (parentVariable == null)
            {
                var ymlVariableDict = YmlVariables.ToDictionary(v => v.Name);
                if (ymlVariableDict.ContainsKey(newVariable.Name))
                {
                    return false;
                }

                newVariable.Result = Result.OnlyInYml;
                YmlVariables.Insert(index, newVariable);
                return true;
            }

            var parentVariables = parentVariable == null ? YmlVariables : parentVariable.Children;

            if (isChild)
            {
                var parentVariableDict = parentVariables.ToDictionary(v => v.Name);
                if (parentVariable.Type == typeof(List<>))
                {
                    newVariable.Name = $"[{parentVariables.Count}]";
                }
                if (parentVariableDict.ContainsKey(newVariable.Name))
                {
                    return false;
                }
                newVariable.FullName = $"{parentVariable.FullName}.{newVariable.Name}";
                parentVariables.Add(newVariable);
                return true;
            }
            else
            {
                if (index < 0)
                {
                    return false;
                }

                var parentVariableDict = parentVariables.ToDictionary(v => v.Name);
                if (parentVariableDict.ContainsKey(newVariable.Name))
                {
                    return false;
                }
                if (index >= parentVariables.Count)
                {
                    parentVariables.Add(newVariable);
                }
                else
                {
                    parentVariables.Insert(index, newVariable);
                }
                return true;
            }
        }
        public bool AddVariable(ConfigVariable ParentVariable, ConfigVariable ConfigVariable, int index)
        {
            var parentVariables = ParentVariable == null ? YmlVariables : ParentVariable.Children;
            var target = parentVariables.Find(v => v.Name == ConfigVariable.Name);
            if (target != null)
            {
                target.Result = Result.Ok;
                return true;
            }
            else if (index >= 0)
            {
                parentVariables.Insert(index, ConfigVariable);
                return true;
            }
            return false;
        }
        public bool RemoveVariable(ConfigVariable ParentVariable, ConfigVariable ConfigVariable)
        {
            var parentVariables = ParentVariable == null ? YmlVariables : ParentVariable.Children;
            var target = parentVariables.Find(v => v.Name == ConfigVariable.Name);
            if (target != null)
            {
                if (target.HasChildren())
                {
                    target.Children.Clear();
                }
                parentVariables.Remove(target);
                return true;
            }
            return false;
        }
        public string UpdateChild(string fullName, object value, bool hideErrorMessage = true)
        {
            var parentVariables = GetParentVariables(fullName);
            var names = fullName.Split('.');
            var target = parentVariables.Find(v => v.Name == names.Last());
            var resultMessage = string.Empty;
            if (target != null)
            {
                resultMessage = TypeManager.IsValidateType(target, value.ToString());
                if (resultMessage == string.Empty)
                {
                    target.Value = value;
                    target.Result = Result.Ok;
                }
                else
                {
                    if (hideErrorMessage)
                    {
                        target.Value = value;
                        target.Result = Result.Ok;
                        resultMessage = string.Empty;
                    }
                }
            }
            else
            {
                resultMessage = $"{fullName} Variable not found.";
            }

            return resultMessage;
        }

        public void RemoveError(string error)
        {
            _errorVariables.Remove(error);
        }
        public Dictionary<string, ConfigVariable> GetErrors()
        {
            return _errorVariables;
        }

        public YamlMappingNode ConvertYamlFromCode()
        {
            YamlMappingNode root = new YamlMappingNode();
            foreach (var variable in YmlVariables)
            {
                AddVariableToYamlNode(root, variable);
            }
            return root;
        }

        private void AddVariableToYamlNode(YamlNode parentNode, ConfigVariable variable)
        {
            if (variable.HasChildren())
            {
                YamlNode childNode;

                if (variable.TypeName == typeof(Dictionary<,>).Name)
                {
                    childNode = new YamlMappingNode();
                }
                else
                {
                    childNode = new YamlSequenceNode();
                }

                foreach (var child in variable.Children)
                {
                    AddVariableToYamlNode(childNode, child);
                }

                if (parentNode is YamlMappingNode mappingNode)
                {
                    mappingNode.Add(variable.Name, childNode);
                }
                else if (parentNode is YamlSequenceNode sequenceNode)
                {
                    sequenceNode.Add(childNode);
                }
            }
            else
            {
                if (parentNode is YamlMappingNode mappingNode)
                {
                    if (variable.Value == null || variable.Value.ToString() == string.Empty)
                    {
                        if(variable.Type == typeof(string))
                        {
                            mappingNode.Add(variable.Name, null);
                        }
                        else if(variable.TypeName == typeof(Dictionary<,>).Name)
                        {
                            mappingNode.Add(variable.Name, new YamlMappingNode());
                        }
                        else
                        {
                            mappingNode.Add(variable.Name, new YamlSequenceNode());
                        }
                    }
                    else
                    {
                        mappingNode.Add(variable.Name, new YamlScalarNode(variable.Value.ToString()));
                    }
                }
                else if (parentNode is YamlSequenceNode sequenceNode)
                {
                    var value = variable.Value == null ? null : variable.Value.ToString();
                    sequenceNode.Add(value);
                }
            }
        }
    }
}