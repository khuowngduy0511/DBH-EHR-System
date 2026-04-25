using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DBH.UnitTest.TestMapper.Models;

namespace DBH.UnitTest.TestMapper.Parsers
{
    /// <summary>
    /// Flexible parser for extracting test cases from C# test files
    /// </summary>
    public class TestFileParser
    {
        /// <summary>
        /// Parses a test file and extracts test cases for a specific function
        /// </summary>
        public TestMappingResult ParseTestFile(string filePath, string functionName)
        {
            var result = new TestMappingResult
            {
                Metadata = new MappingMetadata
                {
                    Function = functionName,
                    File = Path.GetFileName(filePath),
                    Timestamp = DateTime.UtcNow
                }
            };

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return result;
            }

            var fileContent = File.ReadAllText(filePath);
            var testCases = new List<TestCaseMapping>();
            
            var escapedFunctionName = Regex.Escape(functionName);
            var testMethodPattern = $@"\[Fact\(DisplayName\s*=\s*""{escapedFunctionName}::.*?""\)\][\s\S]*?public\s+(async\s+)?(?:Task|void)\s+({escapedFunctionName}_[^\s(]+)";
            var matches = Regex.Matches(fileContent, testMethodPattern, RegexOptions.Multiline);

            if (matches.Count == 0)
            {
                // Fallback for tests that use [Fact] without DisplayName and follow FunctionName_suffix naming.
                var fallbackPattern = $@"\[Fact(?:\([^\)]*\))?\]\s*public\s+(async\s+)?(?:Task|void)\s+({escapedFunctionName}_[^\s(]+)";
                matches = Regex.Matches(fileContent, fallbackPattern, RegexOptions.Multiline);
            }

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var testMethodName = match.Groups[2].Value;
                    var testCase = ParseTestMethod(fileContent, testMethodName, functionName, filePath);
                    if (testCase != null)
                        testCases.Add(testCase);
                }
            }

            result.TestCases = testCases;
            result.Metadata.TestCases = testCases;
            return result;
        }

        private TestCaseMapping? ParseTestMethod(string fileContent, string testMethodName, string functionName, string sourceFile)
        {
            // Use balanced braces: find opening { then match the corresponding closing }
            var methodPattern = $@"public\s+(async\s+)?(?:Task|void)\s+{Regex.Escape(testMethodName)}\(\)\s*";
            var methodMatch = Regex.Match(fileContent, methodPattern, RegexOptions.Multiline);
            if (!methodMatch.Success) return null;

            // Find the opening brace after the method signature
            var afterSignature = fileContent.Substring(methodMatch.Index + methodMatch.Length);
            int braceStart = afterSignature.IndexOf('{');
            if (braceStart < 0) return null;

            // Match balanced braces
            int depth = 0;
            int braceEnd = -1;
            for (int i = braceStart; i < afterSignature.Length; i++)
            {
                if (afterSignature[i] == '{') depth++;
                else if (afterSignature[i] == '}') depth--;
                if (depth == 0)
                {
                    braceEnd = i;
                    break;
                }
            }
            if (braceEnd < 0) return null;

            var methodContent = afterSignature.Substring(0, braceEnd + 1);
            var displayNameMatch = Regex.Match(methodContent, @"DisplayName\s*=\s*""([^""]+)""");
            var displayName = displayNameMatch.Success ? displayNameMatch.Groups[1].Value : testMethodName;

            var testCase = new TestCaseMapping
            {
                TestMethodName = testMethodName,
                FunctionName = functionName,
                DisplayName = displayName,
                SourceFile = Path.GetFileName(sourceFile)
            };

            ExtractPrecondition(methodContent, testCase);
            ExtractInputs(methodContent, testCase);
            ExtractServiceCalls(methodContent, testCase);
            ExtractAssertions(methodContent, testCase);
            ExtractConsoleWriteLine(methodContent, testCase);
            DetermineTestTypeAndExpectedResult(testCase);
            return testCase;
        }

        private void ExtractPrecondition(string methodContent, TestCaseMapping testCase)
        {
            var match = Regex.Match(methodContent, @"//\s*(?:Precondition|Condition):\s*(.+)");
            if (match.Success)
                testCase.Inputs["_precondition"] = match.Groups[1].Value.Trim();
        }

        private void ExtractInputs(string methodContent, TestCaseMapping testCase)
        {
            ExtractFromObjectInitialization(methodContent, testCase);
            ExtractFromDirectMethodCalls(methodContent, testCase);
            if (testCase.Inputs.Count == 0)
                ExtractFromComments(methodContent, testCase);
        }

        private void ExtractFromObjectInitialization(string methodContent, TestCaseMapping testCase)
        {
            var objectPatterns = new[]
            {
                @"new\s+(\w+Request|RegisterRequest|LoginRequest)\s*\{[\s\S]*?\}",
                @"new\s+(\w+(?:\.\w+)+)\s*\{[\s\S]*?\}",
                @"new\s+(\w+)\s*\{[\s\S]*?\}",
                @"=\s*new\s+(\w+(?:\.\w+)+)\s*\{[\s\S]*?\}"
            };

            foreach (var pattern in objectPatterns)
            {
                var requestMatch = Regex.Match(methodContent, pattern);
                if (requestMatch.Success)
                {
                    var requestContent = requestMatch.Value;
                    var kvpPattern = @"(\w+)\s*=\s*(""[^""]*""|'[^']*'|[^,;\n}]+)";
                    var kvpMatches = Regex.Matches(requestContent, kvpPattern);
                    foreach (Match kvpMatch in kvpMatches)
                    {
                        if (kvpMatch.Success && kvpMatch.Groups.Count >= 3)
                        {
                            var key = kvpMatch.Groups[1].Value.Trim();
                            var value = kvpMatch.Groups[2].Value.Trim().Trim('"', '\'', ' ');
                            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                                testCase.Inputs[key] = value;
                        }
                    }
                    break;
                }
            }
        }

        private void ExtractFromDirectMethodCalls(string methodContent, TestCaseMapping testCase)
        {
            var calls = FindAllServiceCalls(methodContent);

            foreach (var (methodName, argsString) in calls)
            {
                if (string.IsNullOrEmpty(argsString)) continue;
                if (methodName == "CreateService") continue;

                var args = SplitArguments(argsString);
                for (int i = 0; i < args.Count; i++)
                {
                    var argKey = $"call_{methodName}_arg{i}";
                    var argValue = args[i].Trim();

                    if (argValue.StartsWith("\"") && argValue.EndsWith("\""))
                        argValue = argValue.Trim('"');
                    else if (argValue.StartsWith("Guid.") || argValue.StartsWith("Guid::"))
                        argValue = "Guid.NewGuid()";
                    else if (argValue == "null" || argValue == "default")
                        argValue = "null/default";
                    else if (argValue.Contains("DateTime(") || argValue.Contains("new ") || argValue.Contains("("))
                    {
                        if (argValue.Length > 50) argValue = "[complex expression]";
                    }

                    testCase.Inputs[$"{methodName}_param{i}"] = argValue;
                }

                if (methodName.Equals(testCase.FunctionName, StringComparison.OrdinalIgnoreCase))
                    break;
            }
        }

        private void ExtractFromComments(string methodContent, TestCaseMapping testCase)
        {
            var inputCommentMatch = Regex.Match(methodContent, @"//\s*Input:\s*(.+)");
            if (inputCommentMatch.Success)
            {
                var inputText = inputCommentMatch.Groups[1].Value;
                var kvpMatches = Regex.Matches(inputText, @"(\w+)=('[^']*'|\""[^\""]*\"")");
                foreach (Match kvpMatch in kvpMatches)
                {
                    if (kvpMatch.Success && kvpMatch.Groups.Count >= 3)
                    {
                        var key = kvpMatch.Groups[1].Value;
                        var value = kvpMatch.Groups[2].Value.Trim('\'', '"');
                        testCase.Inputs[key] = value;
                    }
                }
            }
        }

        /// <summary>
        /// Extracts the full argument string from a service method call, handling nested parentheses
        /// </summary>
        private string ExtractFullArgsString(string content, int openParenIndex)
        {
            int depth = 0;
            int start = openParenIndex + 1;
            for (int i = openParenIndex; i < content.Length; i++)
            {
                if (content[i] == '(') depth++;
                else if (content[i] == ')') depth--;
                if (depth == 0)
                    return content.Substring(start, i - start);
            }
            return "";
        }

        /// <summary>
        /// Finds all service method calls with their full argument strings
        /// </summary>
        private List<(string MethodName, string ArgsString)> FindAllServiceCalls(string content)
        {
            var results = new List<(string, string)>();
            var pattern = @"(?:await\s+)?service\.(\w+)\(";
            var matches = Regex.Matches(content, pattern);

            foreach (Match match in matches)
            {
                if (!match.Success) continue;
                var methodName = match.Groups[1].Value;
                var argsString = ExtractFullArgsString(content, match.Index + match.Length - 1); // -1 to point at '('
                results.Add((methodName, argsString));
            }
            return results;
        }

        private void ExtractServiceCalls(string methodContent, TestCaseMapping testCase)
        {
            var calls = FindAllServiceCalls(methodContent);

            foreach (var (methodName, argsString) in calls)
            {
                if (methodName == "CreateService") continue;
                var args = SplitArguments(argsString);

                var serviceCall = new ServiceCallInfo { MethodName = methodName };
                for (int i = 0; i < args.Count; i++)
                {
                    var argValue = args[i].Trim();
                    if (argValue.StartsWith("\"") && argValue.EndsWith("\""))
                        argValue = argValue.Trim('"');
                    serviceCall.Arguments[$"param{i}"] = argValue;
                }
                testCase.ServiceCalls.Add(serviceCall);
            }
        }

        private void ExtractAssertions(string methodContent, TestCaseMapping testCase)
        {
            var assertPattern = @"Assert\.(\w+)\(([^()]*(?:\([^()]*\)[^()]*)*)\)";
            var assertMatches = Regex.Matches(methodContent, assertPattern);

            foreach (Match assertMatch in assertMatches)
            {
                if (!assertMatch.Success) continue;
                var assertType = assertMatch.Groups[1].Value;
                var assertCondition = assertMatch.Groups[2].Value.Trim();
                var assertion = $"Assert.{assertType}({assertCondition})";
                testCase.Assertions.Add(assertion);
            }
        }

        /// <summary>
        /// Extracts the Console.WriteLine with JsonSerializer.Serialize from the test method
        /// Used to capture the actual serialized response for returnType
        /// </summary>
        private void ExtractConsoleWriteLine(string methodContent, TestCaseMapping testCase)
        {
            // Find Console.WriteLine that contains "response" keyword and JsonSerializer.Serialize
            var consoleIndex = methodContent.IndexOf("Console.WriteLine(", StringComparison.Ordinal);
            while (consoleIndex >= 0)
            {
                // Check if it contains "response" 
                var snippet = methodContent.Substring(consoleIndex, Math.Min(200, methodContent.Length - consoleIndex));
                if (snippet.Contains("response") && snippet.Contains("JsonSerializer.Serialize"))
                {
                    // Extract the full Console.WriteLine statement using balanced parentheses
                    var openParenIndex = consoleIndex + "Console.WriteLine".Length;
                    var closeParenIndex = openParenIndex;
                    int depth = 0;
                    for (int i = openParenIndex; i < methodContent.Length; i++)
                    {
                        if (methodContent[i] == '(') depth++;
                        else if (methodContent[i] == ')') depth--;
                        if (depth == 0)
                        {
                            closeParenIndex = i;
                            break;
                        }
                    }

                    if (closeParenIndex > openParenIndex)
                    {
                        var fullCall = methodContent.Substring(consoleIndex, closeParenIndex - consoleIndex + 1);
                        if (fullCall.Length > 200)
                            fullCall = fullCall.Substring(0, 200) + "...";
                        testCase.ReturnType = fullCall.Trim();
                    }
                    break;
                }

                // Find next Console.WriteLine
                consoleIndex = methodContent.IndexOf("Console.WriteLine(", consoleIndex + 1, StringComparison.Ordinal);
            }
        }

        private List<string> SplitArguments(string argsString)
        {
            var args = new List<string>();
            int depth = 0, start = 0;
            bool inQuotes = false;
            for (int i = 0; i < argsString.Length; i++)
            {
                var c = argsString[i];
                if (c == '"') inQuotes = !inQuotes;
                if (inQuotes) continue;
                if (c == '(') depth++;
                if (c == ')') depth--;
                if (c == ',' && depth == 0)
                {
                    args.Add(argsString.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
            if (start < argsString.Length)
                args.Add(argsString.Substring(start).Trim());
            return args;
        }

        private List<string> SplitAssertEqualArgs(string condition)
        {
            var parts = new List<string>();
            int depth = 0, start = 0;
            bool inQuotes = false;
            for (int i = 0; i < condition.Length; i++)
            {
                var c = condition[i];
                if (c == '"') inQuotes = !inQuotes;
                if (inQuotes) continue;
                if (c == '(' || c == '<') depth++;
                if (c == ')' || c == '>') depth--;
                if (c == ',' && depth == 0)
                {
                    parts.Add(condition.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }
            if (start < condition.Length)
                parts.Add(condition.Substring(start).Trim());
            return parts;
        }

        private void DetermineTestTypeAndExpectedResult(TestCaseMapping testCase)
        {
            bool hasSuccessTrue = testCase.Assertions.Any(a => a.Contains("True(result.Success)") || a.Contains(".Success)"));
            bool hasSuccessFalse = testCase.Assertions.Any(a => a.Contains("False(result.Success)"));
            bool hasNotNullResult = testCase.Assertions.Any(a => a.Contains("NotNull(result"));
            bool hasErrors = testCase.Assertions.Any(a => a.Contains("NotNull(result.Errors)") || a.Contains("NotEmpty(result.Errors)"));
            bool hasNullResult = testCase.Assertions.Any(a => a.Contains("Null(result"));
            bool hasThrowsException = testCase.Assertions.Any(a => a.Contains("ThrowsAsync"));
            bool hasNotNullData = testCase.Assertions.Any(a => a.Contains("NotNull(result.Data)"));
            bool hasTokenCheck = testCase.Assertions.Any(a => a.Contains("result.Token"));
            bool hasVerify = testCase.Assertions.Any(a => a.Contains("Verify("));

            if (hasThrowsException)
            {
                testCase.TestType = "ErrorCase";
                testCase.ExpectedResult = "Throws exception on invalid input";
            }
            else if (hasSuccessTrue && hasNotNullData && hasTokenCheck)
            {
                testCase.TestType = "HappyPath";
                testCase.ExpectedResult = "Returns success with data and token";
            }
            else if (hasSuccessTrue && hasNotNullData)
            {
                testCase.TestType = "HappyPath";
                testCase.ExpectedResult = "Returns success with data";
            }
            else if (hasSuccessTrue)
            {
                testCase.TestType = "HappyPath";
                testCase.ExpectedResult = "Returns success";
            }
            else if (hasSuccessFalse && hasErrors)
            {
                testCase.TestType = "InvalidInput";
                testCase.ExpectedResult = "Returns validation error with error details";
            }
            else if (hasSuccessFalse)
            {
                testCase.TestType = "InvalidInput";
                testCase.ExpectedResult = "Returns failure response";
            }
            else if (hasNullResult)
            {
                testCase.TestType = "NotFound";
                testCase.ExpectedResult = "Returns null or not found";
            }
            else if (hasNotNullResult && !hasSuccessTrue)
            {
                testCase.TestType = "Other";
                testCase.ExpectedResult = "Returns non-null response";
            }
            else
            {
                if (testCase.TestMethodName.Contains("HappyPath") || testCase.TestMethodName.EndsWith("_01"))
                {
                    testCase.TestType = "HappyPath";
                    testCase.ExpectedResult = "Returns success response";
                }
                else if (testCase.TestMethodName.Contains("Invalid") || testCase.TestMethodName.EndsWith("_02"))
                {
                    testCase.TestType = "InvalidInput";
                    testCase.ExpectedResult = "Returns validation error";
                }
                else if (testCase.TestMethodName.Contains("Unauthorized") || 
                         testCase.TestMethodName.Contains("Forbidden") ||
                         testCase.TestMethodName.EndsWith("_03"))
                {
                    testCase.TestType = "Unauthorized";
                    testCase.ExpectedResult = "Returns unauthorized/forbidden";
                }
                else if (testCase.TestMethodName.Contains("NotFound") || testCase.TestMethodName.Contains("NotExist"))
                {
                    testCase.TestType = "NotFound";
                    testCase.ExpectedResult = "Returns null or empty";
                }
                else if (testCase.TestMethodName.Contains("EmptyGuid") || testCase.TestMethodName.Contains("EmptyString"))
                {
                    testCase.TestType = "InvalidInput";
                    testCase.ExpectedResult = "Returns validation error for empty value";
                }
                else if (testCase.Assertions.Any(a => a.Contains("Verify(")))
                {
                    testCase.TestType = "Verification";
                    testCase.ExpectedResult = "Verifies mock interactions";
                }
                else
                {
                    testCase.TestType = "Other";
                    testCase.ExpectedResult = "Returns expected result based on assertions";
                }
            }
        }

        private List<string> ExtractResultProperties(TestCaseMapping testCase)
        {
            var properties = new HashSet<string>();
            foreach (var assertion in testCase.Assertions)
            {
                var match = Regex.Match(assertion, @"result(\.\w+(\.\w+)*)");
                if (match.Success)
                    properties.Add(match.Groups[1].Value.TrimStart('.'));
            }
            return properties.OrderBy(p => p).ToList();
        }

        public List<string> GetAvailableFunctions(string filePath)
        {
            var functions = new List<string>();
            if (!File.Exists(filePath)) return functions;

            var fileContent = File.ReadAllText(filePath);
            var displayNamePattern = @"DisplayName\s*=\s*""([^""]+)::";
            var matches = Regex.Matches(fileContent, displayNamePattern);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var fullDisplayName = match.Groups[1].Value;
                    var functionName = fullDisplayName.Split('.').Last();
                    if (!functions.Contains(functionName))
                        functions.Add(functionName);
                }
            }

            // Fallback for tests without DisplayName attributes, using FunctionName_suffix convention.
            var methodPattern = @"\[Fact(?:\([^\)]*\))?\]\s*public\s+(?:async\s+)?(?:Task|void)\s+([A-Za-z0-9]+?)_[^\s(]+\(\)";
            var methodMatches = Regex.Matches(fileContent, methodPattern, RegexOptions.Multiline);
            foreach (Match methodMatch in methodMatches)
            {
                if (methodMatch.Success)
                {
                    var functionName = methodMatch.Groups[1].Value;
                    if (!functions.Contains(functionName))
                        functions.Add(functionName);
                }
            }

            return functions.OrderBy(f => f).ToList();
        }

        public List<string> FindTestFiles(string searchDirectory = null)
        {
            var testFiles = new List<string>();
            string baseDir;
            string testFolderName = null;

            static string? FindExistingTestFolder(string dir)
            {
                var candidateNames = new[] { "unitTest", "UnitTests", "unitTests", "UnitTest" };
                foreach (var name in candidateNames)
                {
                    var candidate = Path.GetFullPath(Path.Combine(dir, name));
                    if (Directory.Exists(candidate))
                        return name;
                }
                return null;
            }

            if (!string.IsNullOrEmpty(searchDirectory))
            {
                baseDir = searchDirectory;
                testFolderName = FindExistingTestFolder(baseDir);
            }
            else
            {
                var currentDir = Directory.GetCurrentDirectory();
                var possibleDirs = new[]
                {
                    Path.Combine(currentDir, "..", ".."),
                    Path.Combine(currentDir, "..", "..", "..", ".."),
                    Path.Combine(currentDir, "..", "..", ".."),
                    currentDir,
                    Path.Combine(currentDir, "..", "..", "..", "..", ".."),
                };

                baseDir = null;
                foreach (var dir in possibleDirs)
                {
                    var foundFolder = FindExistingTestFolder(dir);
                    if (!string.IsNullOrEmpty(foundFolder))
                    {
                        baseDir = dir;
                        testFolderName = foundFolder;
                        break;
                    }
                }

                if (baseDir == null)
                {
                    var dir = currentDir;
                    while (dir != null)
                    {
                        var srcDbhUnitTestDir = Path.Combine(dir, "src", "DBH.UnitTest");
                        var dbhUnitTestDir = Path.Combine(dir, "DBH.UnitTest");

                        var srcFolder = FindExistingTestFolder(srcDbhUnitTestDir);
                        if (!string.IsNullOrEmpty(srcFolder))
                        {
                            baseDir = srcDbhUnitTestDir;
                            testFolderName = srcFolder;
                            break;
                        }

                        var dbhFolder = FindExistingTestFolder(dbhUnitTestDir);
                        if (!string.IsNullOrEmpty(dbhFolder))
                        {
                            baseDir = dbhUnitTestDir;
                            testFolderName = dbhFolder;
                            break;
                        }
                        dir = Path.GetDirectoryName(dir);
                    }
                }

                if (baseDir == null)
                {
                    Console.WriteLine($"Warning: Could not find unitTest directory from {currentDir}");
                    return testFiles;
                }
            }

            testFolderName ??= FindExistingTestFolder(baseDir);
            if (string.IsNullOrEmpty(testFolderName))
            {
                Console.WriteLine($"Warning: Could not find test directory under: {baseDir}");
                return testFiles;
            }

            var testDir = Path.Combine(baseDir, testFolderName);
            if (!Directory.Exists(testDir))
            {
                Console.WriteLine($"Warning: unitTest directory not found at: {testDir}");
                return testFiles;
            }

            var csFiles = Directory.GetFiles(testDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => f.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith("Test.cs", StringComparison.OrdinalIgnoreCase) ||
                           f.Contains("test", StringComparison.OrdinalIgnoreCase))
                .ToList();
            return csFiles;
        }
    }
}
