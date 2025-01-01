using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WpfApp.Extensions
{

    public static class MethodSignatureHelper
    {
        public static (string RegexPattern, string RegexReplacement) GenerateRegexPatternAndReplacement(string oldSignature, string newSignature)
        {
            if (string.IsNullOrEmpty(newSignature))
            {
                // Return the pattern to match the old signature and empty replacement to remove it
                var simpleRegexPattern = CreateSimpleRegexPattern(oldSignature);
                return (simpleRegexPattern, "");
            }

            // Parse old and new signatures
            var oldMethod = ParseMethodSignature(oldSignature);
            var newMethod = ParseMethodSignature(newSignature);

            // Generate regex pattern
            var regexPattern = GenerateRegexPattern(oldMethod);

            // Generate regex replacement
            var regexReplacement = GenerateRegexReplacement(oldMethod, newMethod);

            return (regexPattern, regexReplacement);
        }

        private static MethodSignature ParseMethodSignature(string signature)
        {
            signature = signature.Trim();

            var methodNamePattern = @"^(?<MethodName>\w+)\s*\((?<Parameters>.*)\)$";
            var match = Regex.Match(signature, methodNamePattern);
            if (!match.Success)
                throw new Exception("Invalid method signature: " + signature);

            var methodName = match.Groups["MethodName"].Value;
            var parameters = match.Groups["Parameters"].Value.Trim();

            var parameterList = new List<Parameter>();
            if (!string.IsNullOrEmpty(parameters))
            {
                var paramParts = SplitParameters(parameters);
                int paramCounter = 1;
                foreach (var param in paramParts)
                {
                    var paramMatch = Regex.Match(param.Trim(), @"^(?<Type>[\w\.\[\]]+)\s*(?<Name>\w+)?$");
                    if (!paramMatch.Success)
                        throw new Exception("Invalid parameter in signature: " + param);

                    var paramType = paramMatch.Groups["Type"].Value;
                    var paramName = paramMatch.Groups["Name"].Value;

                    // Generate a unique name if name is missing
                    if (string.IsNullOrEmpty(paramName))
                    {
                        paramName = $"param{paramCounter}";
                    }

                    parameterList.Add(new Parameter { Type = paramType, Name = paramName });
                    paramCounter++;
                }
            }

            return new MethodSignature
            {
                MethodName = methodName,
                Parameters = parameterList,
            };
        }

        private static List<string> SplitParameters(string parameters)
        {
            var result = new List<string>();
            var currentParam = "";
            var bracketDepth = 0;

            foreach (var ch in parameters)
            {
                if (ch == ',' && bracketDepth == 0)
                {
                    result.Add(currentParam);
                    currentParam = "";
                }
                else
                {
                    if (ch == '<' || ch == '(' || ch == '[')
                        bracketDepth++;
                    else if (ch == '>' || ch == ')' || ch == ']')
                        bracketDepth--;
                    currentParam += ch;
                }
            }

            if (!string.IsNullOrWhiteSpace(currentParam))
                result.Add(currentParam);

            return result;
        }

        private static string GenerateRegexPattern(MethodSignature method)
        {
            var pattern = new List<string>();
            pattern.Add($@"{Regex.Escape(method.MethodName)}\s*\(");
            int paramIndex = 1;
            foreach (var param in method.Parameters)
            {
                var regexParam = $@"\s*(?<Type{paramIndex}>{Regex.Escape(param.Type)})\s+(?<Name{paramIndex}>\w+)";
                pattern.Add(regexParam);
                if (paramIndex < method.Parameters.Count)
                {
                    pattern.Add(@"\s*,");
                }
                paramIndex++;
            }
            pattern.Add(@"\s*\)");

            return string.Join("", pattern);
        }

        private static string GenerateRegexReplacement(MethodSignature oldMethod, MethodSignature newMethod)
        {
            var replacement = new List<string>();
            replacement.Add($"{newMethod.MethodName}(");
            var paramReplacements = new List<string>();
            int paramCounter = 1;

            foreach (var newParam in newMethod.Parameters)
            {
                // Try to find matching parameter in old method by type and name
                var oldParamIndex = oldMethod.Parameters.FindIndex(p => p.Type == newParam.Type && p.Name == newParam.Name);
                if (oldParamIndex >= 0)
                {
                    var paramIndex = oldParamIndex + 1;
                    paramReplacements.Add($"${{Type{paramIndex}}} ${{Name{paramIndex}}}");
                }
                else
                {
                    var paramIndex = oldParamIndex + 1;
                    // New parameter, ensure it has a name
                    var paramName = !string.IsNullOrEmpty(newParam.Name) ? newParam.Name : GenerateParameterName(newParam.Type, paramCounter);
                    paramReplacements.Add($"{newParam.Type} {(oldMethod.Parameters.Count <= paramCounter ? paramName : $"${{Name{paramCounter}}}")}");
                }
                paramCounter++;
            }

            replacement.Add(string.Join(", ", paramReplacements));
            replacement.Add(")");

            return string.Join("", replacement);
        }

        private static string GenerateParameterName(string type, int index)
        {
            var baseName = type.Replace(".", "").Replace("[]", "").ToLower();
            return $"{baseName}{index}";
        }

        private static string CreateSimpleRegexPattern(string signature)
        {
            var escapedSignature = Regex.Escape(signature)
                .Replace("\\(", "\\s*\\(")
                .Replace("\\)", "\\s*\\)")
                .Replace(",", "\\s*,\\s*");

            return escapedSignature;
        }
    }

    public class MethodSignature
    {
        public string MethodName { get; set; }
        public List<Parameter> Parameters { get; set; }
    }

    public class Parameter
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }
}
