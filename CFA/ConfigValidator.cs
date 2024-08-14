using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using YamlDotNet.RepresentationModel;
using ConfigTypeFinder;
using YamlDotNet.Core.Tokens;

namespace CFA
{
    public enum Result
    {
        Ok,
        OnlyInCs,
        OnlyInYml,
        WrongValue,
        NoChild
    }

    public class ConfigValidator
    {
        public Dictionary<String, ConfigVariable> ErrorVariables;
        public List<ConfigVariable> CompareVariables(List<ConfigVariable> ymlVariables, List<ConfigVariable> csVariables)
        {
            ErrorVariables = new Dictionary<string, ConfigVariable>();
            var ymlDict = ymlVariables.ToDictionary(v => v.Name);

            foreach (var csVariable in csVariables)
            {
                if (ymlDict.TryGetValue(csVariable.Name, out var ymlVariable))
                {
                    SetDefaultType(ymlVariable, csVariable);

                    if (ymlVariable.Type != typeof(string) && csVariable.TypeName != ymlVariable.TypeName)
                    {
                        if (csVariable.Type.IsEnum)
                        {
                            ymlVariable.SetEnumValues(csVariable.EnumValues);
                        }
                        SetResult(ymlVariable, Result.WrongValue);
                    }
                    else
                    {
                        CompareChild(csVariable, ymlVariable, null);
                    }
                }
                else
                {
                    var variable = new ConfigVariable(csVariable.Name, csVariable.Type, csVariable.Value);
                    if (csVariable.Type.IsEnum)
                    {
                        variable.SetEnumValues(csVariable.EnumValues);
                    }
                    SetResult(variable, Result.OnlyInCs);
                    ymlVariables.Add(variable);
                }
            }

            foreach (var ymlVariable in ymlVariables)
            {
                if (!csVariables.Any(v => v.Name == ymlVariable.Name))
                {
                    SetResult(ymlVariable, Result.OnlyInYml);
                }
            }
            return ymlVariables;
        }
        private void CompareChild(ConfigVariable csVariable, ConfigVariable ymlVariable, ConfigVariable csParentVariable)
        {
            Result result = ymlVariable.Result;

            // 둘다 자식이 있는 경우
            if (csVariable.HasChildren() && ymlVariable.HasChildren())
            {
                if (csVariable.TypeName == ymlVariable.TypeName)
                {
                    foreach (var child in ymlVariable.Children)
                    {
                        CompareChild(csVariable.Children.Last(), child, csVariable);
                    }
                }
                else
                {
                    SetDefaultType(ymlVariable, csVariable);
                    result = Result.WrongValue;
                }
            }

            // 둘다 자식이 없는 경우
            else if (!csVariable.HasChildren() && !ymlVariable.HasChildren())
            {
                if (ymlVariable.Result == Result.Ok)
                {
                    if (csParentVariable != null && csParentVariable.Children[0].Type.IsEnum)
                    {
                        result = IsValidatedEnumValue(ymlVariable, csParentVariable.Children[0]);
                    }
                    else
                    {
                        result = CompareSingleValue(ymlVariable, csVariable);

                    }
                }

            }
            else if (csVariable.HasChildren() && !ymlVariable.HasChildren())
            {
                result = Result.NoChild;
            }
            else
            {
                result = Result.WrongValue;
            }

            SetResult(ymlVariable, result);
        }

        private void SetResult(ConfigVariable ConfigVariable, Result result)
        {
            ConfigVariable.Result = result;
            if (result != Result.Ok && result != Result.NoChild)
            {
                ErrorVariables.Add(ConfigVariable.FullName, ConfigVariable);
            }
        }
        private Result CompareSingleValue(ConfigVariable ymlVariable, ConfigVariable csVariable)
        {
            if (csVariable.IsEnumType())
            {
                ymlVariable.SetEnumValues(csVariable.EnumValues);
                ymlVariable.DefaultValue = csVariable.Value;
                var values = csVariable.EnumValues.ToDictionary(v => v.Name);
                if (values.ContainsKey(ymlVariable.Value.ToString()))
                {
                    Type targetType = csVariable.Value.GetType();
                    ymlVariable.Value = Enum.Parse(targetType, ymlVariable.Value.ToString());
                    return Result.Ok;
                }
                return Result.WrongValue;
            }
            else
            {
                if (TypeManager.IsValidateType(ymlVariable, ymlVariable.Value.ToString()) != string.Empty)
                {
                    return Result.WrongValue;
                }
                return Result.Ok;
            }
        }
        private void SetDefaultType(ConfigVariable ymlVariable, ConfigVariable csVariable)
        {
            ymlVariable.Type = csVariable.Type;
            ymlVariable.TypeName = csVariable.TypeName;
            ymlVariable.DefaultValue = csVariable.Value.ToString();

        }
        private Result IsValidatedEnumValue(ConfigVariable ymlVariable, ConfigVariable csVariable)
        {
            var values = csVariable.EnumValues.ToDictionary(v => v.Name);
            if (values.ContainsKey(ymlVariable.Name))
            {
                ymlVariable.Type = values[ymlVariable.Name].Type;
                ymlVariable.TypeName = values[ymlVariable.Name].TypeName;
                if (TypeManager.IsValidateType(ymlVariable, ymlVariable.Value.ToString()) == string.Empty)
                {
                    return Result.Ok;
                }
                return Result.WrongValue;
            }
            return Result.OnlyInYml;

        }
    }
}