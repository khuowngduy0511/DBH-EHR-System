using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using DBH.UnitTest.TestMapper.Models;
using DBH.UnitTest.TestMapper.Parsers;
using DBH.UnitTest.TestMapper.Utilities;

namespace DBH.UnitTest.TestMapper
{
    class SimpleTestMapper
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Simple Test Case Mapper ===\n");
            
            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            var command = args[0].ToLower();
            
            switch (command)
            {
                case "--list":
                case "-l":
                    ListFunctions();
                    break;
                    
                case "--function":
                case "-f":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Error: Function name required");
                        ShowHelp();
                        return;
                    }
                    MapFunction(args[1]);
                    break;
                    
                case "--test":
                case "-t":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Error: Test method name required");
                        ShowHelp();
                        return;
                    }
                    MapTest(args[1]);
                    break;
                    
                case "--help":
                case "-h":
                case "/?":
                    ShowHelp();
                    break;

                case "--generate-template":
                case "-gt":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Error: Template .xlsx path required");
                        ShowHelp();
                        return;
                    }

                    GenerateTemplate(args[1], args.Length >= 3 ? args[2] : null);
                    break;

                case "--process-json":
                case "-p":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Error: Template .xlsx path required");
                        ShowHelp();
                        return;
                    }

                    ProcessAllJsonToExcel(args[1], args.Length >= 3 ? args[2] : null);
                    break;

                default:
                    Console.WriteLine($"Unknown command: {args[0]}");
                    ShowHelp();
                    break;
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  SimpleTestMapper --list                    List all available test functions");
            Console.WriteLine("  SimpleTestMapper --function <name>         Map all tests for a function");
            Console.WriteLine("  SimpleTestMapper --test <method>           Map specific test method");
            Console.WriteLine("  SimpleTestMapper --generate-template <src.xlsx> [out.xlsx]  Recreate template workbook");
            Console.WriteLine("  SimpleTestMapper --help                    Show this help");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  SimpleTestMapper --list");
            Console.WriteLine("  SimpleTestMapper --function RegisterAsync");
            Console.WriteLine("  SimpleTestMapper --test RegisterAsync_01");
            Console.WriteLine("  SimpleTestMapper --generate-template \"Report5_Unit Test.xlsx\" \"output/Report5_Template_Generated.xlsx\"");
        }

        static void ListFunctions()
        {
            var parser = new TestFileParser();
            Console.WriteLine("=== Available Test Functions ===\n");
            
            var testFiles = parser.FindTestFiles();
            if (testFiles.Count == 0)
            {
                Console.WriteLine("No test files found.");
                return;
            }

            foreach (var file in testFiles)
            {
                var functions = parser.GetAvailableFunctions(file);
                if (functions.Count > 0)
                {
                    Console.WriteLine($"File: {Path.GetFileName(file)}");
                    foreach (var function in functions)
                    {
                        Console.WriteLine($"  - {function}");
                    }
                    Console.WriteLine();
                }
            }

            // Save list to JSON
            var allFunctions = testFiles
                .SelectMany(f => parser.GetAvailableFunctions(f).Select(func => new
                {
                    File = Path.GetFileName(f),
                    Function = func
                }))
                .ToList();

            var json = JsonSerializer.Serialize(allFunctions, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            Directory.CreateDirectory("output");
            File.WriteAllText("output/available_functions.json", json);
            Console.WriteLine($"✓ Saved function list to: output/available_functions.json");
        }

        static void MapFunction(string functionName)
        {
            var parser = new TestFileParser();
            
            // Find test file
            var testFiles = parser.FindTestFiles();
            string testFilePath = null;
            
            foreach (var file in testFiles)
            {
                var functions = parser.GetAvailableFunctions(file);
                if (functions.Contains(functionName))
                {
                    testFilePath = file;
                    break;
                }
            }

            if (string.IsNullOrEmpty(testFilePath))
            {
                Console.WriteLine($"Error: Could not find test file for function '{functionName}'");
                return;
            }

            Console.WriteLine($"Using test file: {Path.GetFileName(testFilePath)}");
            
            var result = parser.ParseTestFile(testFilePath, functionName);
            
            // Ensure output directory exists
            Directory.CreateDirectory("output");
            
            // Save to JSON
            string outputFilePath = Path.Combine("output", $"{functionName}.json");
            SaveJsonOutput(result, outputFilePath);
            
            // Also show text output
            DisplayTextOutput(result);
            
            Console.WriteLine($"\n✓ Saved {result.TestCases.Count} test cases to: {outputFilePath}");
        }

        static void MapTest(string testMethod)
        {
            var parser = new TestFileParser();
            
            // Extract function name from test method (e.g., RegisterAsync_01 -> RegisterAsync)
            var functionName = testMethod.Split('_').First();
            
            // Find test file
            var testFiles = parser.FindTestFiles();
            string testFilePath = null;
            
            foreach (var file in testFiles)
            {
                var functions = parser.GetAvailableFunctions(file);
                if (functions.Contains(functionName))
                {
                    testFilePath = file;
                    break;
                }
            }

            if (string.IsNullOrEmpty(testFilePath))
            {
                Console.WriteLine($"Error: Could not find test file for function '{functionName}'");
                return;
            }

            Console.WriteLine($"Using test file: {Path.GetFileName(testFilePath)}");
            
            var result = parser.ParseTestFile(testFilePath, functionName);
            
            // Filter to only the specific test method
            var specificTest = result.TestCases.FirstOrDefault(t => t.TestMethodName == testMethod);
            if (specificTest != null)
            {
                result.TestCases = new List<TestCaseMapping> { specificTest };
                result.Metadata.TestCases = result.TestCases;
            }
            else
            {
                Console.WriteLine($"Warning: Test method '{testMethod}' not found in file");
                result.TestCases.Clear();
            }
            
            // Ensure output directory exists
            Directory.CreateDirectory("output");
            
            // Save to JSON
            string outputFilePath = Path.Combine("output", $"{testMethod}.json");
            SaveJsonOutput(result, outputFilePath);
            
            // Also show text output
            DisplayTextOutput(result);
            
            Console.WriteLine($"\n✓ Saved test case to: {outputFilePath}");
        }

        static void DisplayTextOutput(TestMappingResult result)
        {
            Console.WriteLine($"\n=== Test Case Mapping: {result.Metadata.Function} ===");
            Console.WriteLine($"File: {result.Metadata.File}");
            Console.WriteLine($"Test Count: {result.TestCases.Count}");
            Console.WriteLine($"Timestamp: {result.Metadata.Timestamp:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            if (result.TestCases.Count == 0)
            {
                Console.WriteLine("No test cases found.");
                return;
            }

            for (int i = 0; i < result.TestCases.Count; i++)
            {
                var tc = result.TestCases[i];
                Console.WriteLine($"\n{'='.ToString().PadRight(60, '=')}");
                Console.WriteLine($"{i + 1}. {tc.TestMethodName}");
                Console.WriteLine($"   Display Name: {tc.DisplayName}");
                Console.WriteLine($"   Test Type: {tc.TestType}");
                Console.WriteLine($"   Expected: {tc.ExpectedResult}");
                
                // Precondition
                if (tc.Inputs.ContainsKey("_precondition"))
                {
                    Console.WriteLine($"   Precondition: {tc.Inputs["_precondition"]}");
                }

                // Service calls chain
                if (tc.ServiceCalls.Count > 0)
                {
                    Console.WriteLine($"   Service Calls ({tc.ServiceCalls.Count}):");
                    foreach (var call in tc.ServiceCalls)
                    {
                        Console.WriteLine($"     - {call.MethodName}(");
                        foreach (var arg in call.Arguments)
                        {
                            Console.WriteLine($"         {arg.Key} = \"{arg.Value}\"");
                        }
                        Console.WriteLine($"       )");
                    }
                }

                // Inputs (excluding metadata keys like _precondition)
                var inputKeys = tc.Inputs.Keys.Where(k => !k.StartsWith("_")).ToList();
                if (inputKeys.Count > 0)
                {
                    Console.WriteLine($"   Inputs:");
                    foreach (var key in inputKeys)
                    {
                        Console.WriteLine($"     - {key} = {tc.Inputs[key]}");
                    }
                }

                // Return type (Console.WriteLine with serialized response)
                if (!string.IsNullOrEmpty(tc.ReturnType))
                {
                    Console.WriteLine($"   Return Type (Console.WriteLine):");
                    Console.WriteLine($"     {tc.ReturnType}");
                }

                // Assertions (simple list format)
                if (tc.Assertions.Count > 0)
                {
                    Console.WriteLine($"   Assertions ({tc.Assertions.Count}):");
                    foreach (var assertion in tc.Assertions)
                    {
                        Console.WriteLine($"     - {assertion}");
                    }
                }
            }
        }

        static void SaveJsonOutput(TestMappingResult result, string outputFilePath)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(result, options);
            File.WriteAllText(outputFilePath, json);
        }

        static void GenerateTemplate(string templatePath, string? outputPath)
        {
            try
            {
                var generator = new ExcelTemplateGenerator();
                var generatedPath = generator.GenerateFromTemplate(templatePath, outputPath);
                Console.WriteLine($"✓ Generated template workbook: {generatedPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating template workbook: {ex.Message}");
            }
        }

        static void ProcessAllJsonToExcel(string templatePath, string? outputDirArg)
        {
            var outputDir = outputDirArg ?? "save";
            var jsonDir = Path.Combine(Directory.GetCurrentDirectory(), "output");

            if (!Directory.Exists(jsonDir))
            {
                Console.WriteLine($"Error: JSON directory not found: {jsonDir}");
                return;
            }

            var jsonFiles = Directory.GetFiles(jsonDir, "*.json")
                .Where(f => !Path.GetFileName(f).Equals("available_functions.json", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f)
                .ToList();

            if (jsonFiles.Count == 0)
            {
                Console.WriteLine("No JSON files found in 'output/' directory.");
                return;
            }

            var templateFullPath = Path.GetFullPath(templatePath);
            if (!File.Exists(templateFullPath))
            {
                Console.WriteLine($"Error: Template file not found: {templateFullPath}");
                return;
            }

            var outputFullDir = Path.GetFullPath(outputDir);
            Console.WriteLine($"Template: {templateFullPath}");
            Console.WriteLine($"JSON directory: {jsonDir}");
            Console.WriteLine($"Output directory: {outputFullDir}");
            Console.WriteLine($"Found {jsonFiles.Count} JSON file(s) to process.\n");

            var mapper = new JsonToExcelMapper();
            int successCount = 0;
            int failCount = 0;

            foreach (var jsonFile in jsonFiles)
            {
                var fileName = Path.GetFileName(jsonFile);
                Console.Write($"  [{fileName}] ");
                var result = mapper.ProcessJson(jsonFile, templateFullPath, outputFullDir);
                if (result != null)
                    successCount++;
                else
                    failCount++;
            }

            Console.WriteLine($"\n=== Done: {successCount} succeeded, {failCount} failed ===");
        }
    }
}
