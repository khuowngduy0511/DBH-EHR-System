using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DBH.UnitTest.TestMapper.Models
{
    /// <summary>
    /// Represents a mapped test case with structured data
    /// </summary>
    public class TestCaseMapping
    {
        /// <summary>
        /// Test method name (e.g., "RegisterAsync_01")
        /// </summary>
        [JsonPropertyName("testMethod")]
        public string TestMethodName { get; set; } = string.Empty;

        /// <summary>
        /// Function being tested (e.g., "RegisterAsync")
        /// </summary>
        [JsonPropertyName("function")]
        public string FunctionName { get; set; } = string.Empty;

        /// <summary>
        /// Display name from Fact attribute
        /// </summary>
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Type of test (HappyPath, InvalidInput, Unauthorized, etc.)
        /// </summary>
        [JsonPropertyName("testType")]
        public string TestType { get; set; } = string.Empty;

        /// <summary>
        /// Expected result description
        /// </summary>
        [JsonPropertyName("expectedResult")]
        public string ExpectedResult { get; set; } = string.Empty;

        /// <summary>
        /// Input parameters as key-value pairs
        /// </summary>
        [JsonPropertyName("inputs")]
        public Dictionary<string, object> Inputs { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// List of assertion statements
        /// </summary>
        [JsonPropertyName("assertions")]
        public List<string> Assertions { get; set; } = new List<string>();

        /// <summary>
        /// The Console.WriteLine that serializes the actual response for debugging
        /// Extracted from test methods where JsonSerializer.Serialize(result, ...) is used
        /// </summary>
        [JsonPropertyName("returnType")]
        public string ReturnType { get; set; } = string.Empty;

        /// <summary>
        /// Source file containing the test
        /// </summary>
        [JsonPropertyName("sourceFile")]
        public string SourceFile { get; set; } = string.Empty;

        /// <summary>
        /// If the test chains multiple service calls (e.g., Register then Login), lists them
        /// </summary>
        [JsonPropertyName("serviceCalls")]
        public List<ServiceCallInfo> ServiceCalls { get; set; } = new List<ServiceCallInfo>();

        /// <summary>
        /// Returns a string representation of the test case
        /// </summary>
        public override string ToString()
        {
            return $"{TestMethodName} -> Tests {FunctionName} -> {ExpectedResult}";
        }

        /// <summary>
        /// Gets input as formatted string
        /// </summary>
        public string GetInputsFormatted()
        {
            if (Inputs.Count == 0)
                return "No inputs";

            var items = new List<string>();
            foreach (var kvp in Inputs)
            {
                items.Add($"{kvp.Key} = {FormatValue(kvp.Value)}");
            }
            return string.Join(", ", items);
        }

        /// <summary>
        /// Gets assertions as formatted string
        /// </summary>
        public string GetAssertionsFormatted()
        {
            if (Assertions.Count == 0)
                return "No assertions";

            return string.Join("; ", Assertions);
        }

        private static string FormatValue(object value)
        {
            if (value == null)
                return "null";

            if (value is string str)
                return $"\"{str}\"";

            return value.ToString() ?? "null";
        }
    }

    /// <summary>
    /// Structured assertion detail with target property and expected value
    /// </summary>
    public class AssertionDetail
    {
        [JsonPropertyName("assertType")]
        public string AssertType { get; set; } = string.Empty;

        [JsonPropertyName("targetProperty")]
        public string TargetProperty { get; set; } = string.Empty;

        [JsonPropertyName("expectedValue")]
        public string ExpectedValue { get; set; } = string.Empty;

        /// <summary>
        /// Raw full assertion string
        /// </summary>
        [JsonPropertyName("rawAssertion")]
        public string RawAssertion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a service method call within a test (for chained calls)
    /// </summary>
    public class ServiceCallInfo
    {
        [JsonPropertyName("methodName")]
        public string MethodName { get; set; } = string.Empty;

        [JsonPropertyName("arguments")]
        public Dictionary<string, string> Arguments { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Container for test case mapping results with metadata
    /// </summary>
    public class TestMappingResult
    {
        /// <summary>
        /// Metadata about the mapping operation
        /// </summary>
        [JsonPropertyName("metadata")]
        public MappingMetadata Metadata { get; set; } = new MappingMetadata();

        /// <summary>
        /// List of mapped test cases
        /// </summary>
        [JsonPropertyName("testCases")]
        public List<TestCaseMapping> TestCases { get; set; } = new List<TestCaseMapping>();
    }

    /// <summary>
    /// Metadata about the test mapping operation
    /// </summary>
    public class MappingMetadata
    {
        /// <summary>
        /// Function being mapped
        /// </summary>
        [JsonPropertyName("function")]
        public string Function { get; set; } = string.Empty;

        /// <summary>
        /// Source file analyzed
        /// </summary>
        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        /// <summary>
        /// When the mapping was performed
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Number of test cases found
        /// </summary>
        [JsonPropertyName("testCount")]
        public int TestCount => TestCases?.Count ?? 0;

        /// <summary>
        /// List of test cases (for internal use)
        /// </summary>
        [JsonIgnore]
        public List<TestCaseMapping>? TestCases { get; set; }
    }
}