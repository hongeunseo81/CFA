using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CoPick.Setting;
using CFA;
using System.Windows.Forms;
namespace ConfigTypeFinder
{
    public class TypeManager
    {
        private static Dictionary<string, Func<object, (bool, string)>> s_validatorFunction;
        private static Dictionary<string, object[]> s_functionArgs;
        private static Dictionary<string, Type> s_types;

        public static void Init()
        {
            s_validatorFunction = new Dictionary<string, Func<object, (bool, string)>>();
            s_functionArgs = new Dictionary<string, object[]>();
            s_types = new Dictionary<string, Type>
            {
                { typeof(Dictionary<,>).Name, typeof(Dictionary<,>) },
                { typeof(List<>).Name, typeof(List<>) },
                { typeof(string).Name, typeof(string) },
                { typeof(bool).Name, typeof(bool) }
            };
        }

        public static Dictionary<string, Type> GetAllTypes()
        {
            return s_types;
        }
        public static void AddType(string typeName, Type type)
        {
            if (!s_types.ContainsKey(typeName))
            {
                s_types.Add(typeName, type);
            }
        }
        public static void MakeFunction(string name, ValidatorType validatorType, string info)
        {
            var infoArray = info.Split(' ');
            var size = infoArray[1].Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
            if (int.TryParse(size[1], out int objectSize))
            {
                object[] args = new object[objectSize];
                string valuesString = info.Substring(info.IndexOf('{') + 1).Trim(' ', '}');
                string[] valueStrings = valuesString.Split(new char[] { '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => s.Trim())
                                    .Where(s => !string.IsNullOrWhiteSpace(s))
                                    .ToArray();


                for (int i = 0; i < objectSize; i++)
                {
                    string typeString = valueStrings[i * 2];
                    string valueString = valueStrings[i * 2 + 1];
                    if (typeString.Contains("Int32") && int.TryParse(valueString, out int intValue))
                    {
                        args[i] = intValue;
                    }
                    else if (typeString.Contains("Single") && float.TryParse(valueString, out float floatValue))
                    {
                        args[i] = floatValue;
                    }
                    else if (typeString.Contains("Double") && double.TryParse(valueString, out double doubleValue))
                    {
                        args[i] = doubleValue;
                    }

                }
                if (TypeValidator.ValidationDict.TryGetValue(validatorType, out var validatorFunc))
                {
                    if (!s_validatorFunction.ContainsKey(name))
                    {
                        s_validatorFunction.Add(name, validatorFunc(args));
                        if (args.Count() > 0)
                        {
                            s_functionArgs.Add(name, args);
                        }
                    }
                }
            }
            AddType(validatorType.ToString(), typeof(string));
        }
        public static string IsValidateType(ConfigVariable ConfigVariable, string value)
        {
            if (ConfigVariable.Type == typeof(DateTime))
            {
                string format = "yyyy-MM-ddTHH:mm:ss.fffffffK"; // 지정된 날짜/시간 형식

                var isValid = DateTime.TryParseExact(value.ToString(), format, null, System.Globalization.DateTimeStyles.None, out DateTime dateTimeValue);
                return isValid ? string.Empty : "Please enter a format yyyy-MM-ddTHH:mm:ss.fffffffK";
            }
            if (value == "")
            {
                return string.Empty;
            }
            if (ConfigVariable.Type == typeof(bool))
            {
                return (value == "True" || value == "False") ? string.Empty : "Please enter True or False.";
            }
            if (s_validatorFunction.TryGetValue(ConfigVariable.Name, out var validatorFunc))
            {
                var isValidate = validatorFunc(value).Item1;
                var message = validatorFunc(value).Item2;
                if (!isValidate && s_functionArgs.ContainsKey(ConfigVariable.Name))
                {
                    object[] args = s_functionArgs[ConfigVariable.Name];
                    message = $"Please enter a value between {args[0]} and {args[1]}.";
                }
                return message;
            }
            else
            {
                bool isValid = false;
                string message = string.Empty;
                if (ConfigVariable.Type == typeof(Int32))
                {
                    isValid = Int32.TryParse(value.ToString(), out int intValue);
                    message = isValid ? string.Empty : "Please enter a value between -2,147,483,648 and 2,147,483,647";
                }
                else if (ConfigVariable.Type == typeof(Int64))
                {
                    isValid = Int64.TryParse(value.ToString(), out long longValue);
                    message = isValid ? string.Empty : "Please enter a value between -9,223,372,036,854,775,808 and 9,223,372,036,854,775,807";
                }

                return message;
            }

        }

        public static void ConvertTypeNameToType(ConfigVariable newVariable)
        {
            var typeName = newVariable.TypeName;
            newVariable.Type = s_types[typeName];
        }
    }

}