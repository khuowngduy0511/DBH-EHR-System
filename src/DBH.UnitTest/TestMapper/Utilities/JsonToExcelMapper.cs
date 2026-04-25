using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClosedXML.Excel;
using DBH.UnitTest.TestMapper.Models;

namespace DBH.UnitTest.TestMapper.Utilities
{
    /// <summary>
    /// Reads a JSON test case file and a template .xlsx, then populates
    /// the template with input mappings, assertion mappings, and a
    /// test-case coverage matrix — all with Tahoma font formatting.
    /// </summary>
    public class JsonToExcelMapper
    {
        // Input: key in column B, value in column D
        private const int InputKeyColumnIndex = 2;  // B
        private const int InputValueColumnIndex = 4; // D
        private const int InputStartRow = 12;
        // Assertion: label at D51, values start D52
        private const int AssertionLabelRow = 61;
        private const int AssertionStartRow = 62;
        // Metadata
        private const int TestCountCellColumnIndex = 15; // O = column 15
        private const int TestCountCellRow = 7;
        private const int NameCellColumnIndex1 = 3; // C
        private const int NameCellColumnIndex2 = 4; // D
        private const int NameCellColumnIndex3 = 12; // L
        private const int NameCellRow = 2;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Processes a single JSON file and writes the populated workbook to the output folder.
        /// </summary>
        /// <param name="jsonFilePath">Full path to the JSON file.</param>
        /// <param name="templatePath">Full path to the template .xlsx.</param>
        /// <param name="outputDir">Directory where the result will be saved.</param>
        /// <returns>The full path to the generated .xlsx, or null on failure.</returns>
        public string? ProcessJson(string jsonFilePath, string templatePath, string outputDir)
        {
            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine($"  ✗ JSON file not found: {jsonFilePath}");
                return null;
            }
            if (!File.Exists(templatePath))
            {
                Console.WriteLine($"  ✗ Template file not found: {templatePath}");
                return null;
            }

            // 1. Parse JSON
            var jsonText = File.ReadAllText(jsonFilePath);
            var mappingResult = JsonSerializer.Deserialize<TestMappingResult>(jsonText, JsonOptions);
            if (mappingResult == null || mappingResult.TestCases == null || mappingResult.TestCases.Count == 0)
            {
                Console.WriteLine($"  ✗ No test cases found in: {Path.GetFileName(jsonFilePath)}");
                return null;
            }

            var functionName = mappingResult.Metadata?.Function ?? Path.GetFileNameWithoutExtension(jsonFilePath);
            var testCases = mappingResult.TestCases;
            var testCount = testCases.Count;

            Console.WriteLine($"  Processing '{functionName}' ({testCount} test cases)...");

            // 2. Build master lists
            var (inputRows, assertionRows) = BuildMasterLists(testCases);

            // 3. Determine which columns each test case maps to
            //    F = first test case, G = second, H = third, ...
            var testCaseColumns = BuildTestCaseColumns(testCases, inputRows, assertionRows);

            // 4. Clone template and fill
            var outputFileName = $"{SanitizeFileName(functionName)}_output.xlsx";
            var outputPath = Path.Combine(outputDir, outputFileName);

            try
            {
                using var workbook = new XLWorkbook(templatePath);
                var sheet = workbook.Worksheet(1); // work on the first sheet

                // --- Header / Metadata ---
                sheet.Cell(NameCellRow, NameCellColumnIndex1).Value = functionName;
                sheet.Cell(NameCellRow, NameCellColumnIndex2).Value = functionName;
                sheet.Cell(NameCellRow, NameCellColumnIndex3).Value = functionName;
                sheet.Cell(TestCountCellRow, TestCountCellColumnIndex).Value = testCount;

                // --- Write Input rows (key in B, value in D, starting B12/D12) ---
                WriteInputRows(sheet, inputRows);

                // --- Write Assertion label at D51, values starting D52 ---
                WriteAssertionRows(sheet, assertionRows);

                // --- Write "O" matrix ---
                WriteMatrix(sheet, testCaseColumns, inputRows, assertionRows);

                // --- Save ---
                Directory.CreateDirectory(outputDir);
                workbook.SaveAs(outputPath);
                Console.WriteLine($"  ✓ Saved: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error writing Excel: {ex.Message}");
                return null;
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Master list building
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// An entry in the master input list – either a key row (bold)
        /// or a value row (normal).
        /// </summary>
        private class InputRowEntry
        {
            public bool IsKey { get; set; }
            public string Key { get; set; } = "";
            public string Value { get; set; } = "";
            /// <summary>
            /// Which test-case columns this value row maps to.
            /// </summary>
            public HashSet<int> MappedTestCases { get; set; } = new();
        }

        private class AssertionEntry
        {
            public string Text { get; set; } = "";
            public HashSet<int> MappedTestCases { get; set; } = new();
        }

        /// <summary>
        /// Builds the deduplicated input list and assertion list from all test cases.
        /// </summary>
        private (List<InputRowEntry> inputRows, List<AssertionEntry> assertionRows) BuildMasterLists(
            List<TestCaseMapping> testCases)
        {
            // ---- Inputs ----
            // We'll track each key and its associated value rows.
            // key -> list of (value, set-of-test-case-indices)
            var inputMap = new Dictionary<string, List<(string Value, HashSet<int> TestCaseIndices)>>();

            for (int tcIdx = 0; tcIdx < testCases.Count; tcIdx++)
            {
                var tc = testCases[tcIdx];
                foreach (var kvp in tc.Inputs)
                {
                    var key = kvp.Key;
                    var value = kvp.Value?.ToString() ?? "";

                    if (!inputMap.ContainsKey(key))
                    {
                        inputMap[key] = new List<(string, HashSet<int>)>();
                    }

                    var valueList = inputMap[key];
                    var found = false;
                    for (int i = 0; i < valueList.Count; i++)
                    {
                        if (valueList[i].Value == value)
                        {
                            valueList[i] = (valueList[i].Value, valueList[i].TestCaseIndices);
                            valueList[i].TestCaseIndices.Add(tcIdx);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        valueList.Add((value, new HashSet<int> { tcIdx }));
                    }
                }
            }

            // Flatten: key row, then value rows for each distinct value
            var inputRows = new List<InputRowEntry>();
            foreach (var kvp in inputMap)
            {
                // Key row (bold)
                var keyEntry = new InputRowEntry
                {
                    IsKey = true,
                    Key = kvp.Key,
                    Value = kvp.Key
                };
                inputRows.Add(keyEntry);

                // Value rows (normal)
                foreach (var (val, tcIndices) in kvp.Value)
                {
                    var valEntry = new InputRowEntry
                    {
                        IsKey = false,
                        Key = kvp.Key,
                        Value = val,
                        MappedTestCases = tcIndices
                    };
                    inputRows.Add(valEntry);
                }
            }

            // ---- Assertions ----
            var assertionMap = new Dictionary<string, HashSet<int>>();

            for (int tcIdx = 0; tcIdx < testCases.Count; tcIdx++)
            {
                var tc = testCases[tcIdx];
                foreach (var assertion in tc.Assertions)
                {
                    if (!assertionMap.ContainsKey(assertion))
                    {
                        assertionMap[assertion] = new HashSet<int>();
                    }
                    assertionMap[assertion].Add(tcIdx);
                }
            }

            var assertionRows = assertionMap
                .Select(kvp => new AssertionEntry
                {
                    Text = kvp.Key,
                    MappedTestCases = kvp.Value
                })
                .ToList();

            return (inputRows, assertionRows);
        }

        // ──────────────────────────────────────────────────────────────
        //  Test-case column mapping
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// For each test case (by index), determine which rows in the
        /// master input/assertion lists it maps to.
        /// Returns a list where index = test case index, value = list of
        /// (rowType, rowIndex)  — rowType "I" for input, "A" for assertion.
        /// </summary>
        private List<List<(string Type, int RowIndex)>> BuildTestCaseColumns(
            List<TestCaseMapping> testCases,
            List<InputRowEntry> inputRows,
            List<AssertionEntry> assertionRows)
        {
            var result = new List<List<(string, int)>>();

            for (int tcIdx = 0; tcIdx < testCases.Count; tcIdx++)
            {
                var mappings = new List<(string, int)>();

                // Find matching input rows (value rows only, not key rows)
                for (int i = 0; i < inputRows.Count; i++)
                {
                    if (!inputRows[i].IsKey && inputRows[i].MappedTestCases.Contains(tcIdx))
                    {
                        mappings.Add(("I", i));
                    }
                }

                // Find matching assertion rows
                for (int i = 0; i < assertionRows.Count; i++)
                {
                    if (assertionRows[i].MappedTestCases.Contains(tcIdx))
                    {
                        mappings.Add(("A", i));
                    }
                }

                result.Add(mappings);
            }

            return result;
        }

        // ──────────────────────────────────────────────────────────────
        //  Writing rows to Excel
        // ──────────────────────────────────────────────────────────────

        private void WriteInputRows(IXLWorksheet sheet, List<InputRowEntry> inputRows)
        {
            for (int i = 0; i < inputRows.Count; i++)
            {
                var row = InputStartRow + i;
                var entry = inputRows[i];

                if (entry.IsKey)
                {
                    // Key goes to column B (bold, Tahoma 8)
                    var cell = sheet.Cell(row, InputKeyColumnIndex);
                    cell.Value = entry.Key;
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontName = "Tahoma";
                    cell.Style.Font.FontSize = 8;
                }
                else
                {
                    // Value goes to column D (normal, Tahoma 8)
                    var cell = sheet.Cell(row, InputValueColumnIndex);
                    cell.Value = entry.Value;
                    cell.Style.Font.Bold = false;
                    cell.Style.Font.FontName = "Tahoma";
                    cell.Style.Font.FontSize = 8;
                }
            }
        }

        private void WriteAssertionRows(IXLWorksheet sheet, List<AssertionEntry> assertionRows)
        {
            // Write the "Assertion" label at D51
            var labelCell = sheet.Cell("D" + AssertionLabelRow);
            labelCell.Value = "Assertion";
            labelCell.Style.Font.FontName = "Tahoma";
            labelCell.Style.Font.FontSize = 8;
            labelCell.Style.Font.Bold = false;

            // Write assertion texts starting at D52
            for (int i = 0; i < assertionRows.Count; i++)
            {
                var row = AssertionStartRow + i;
                var cell = sheet.Cell("D" + row);
                cell.Value = assertionRows[i].Text;
                cell.Style.Font.FontName = "Tahoma";
                cell.Style.Font.FontSize = 8;
                cell.Style.Font.Bold = false;
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Writing the "O" matrix
        // ──────────────────────────────────────────────────────────────

        private void WriteMatrix(
            IXLWorksheet sheet,
            List<List<(string Type, int RowIndex)>> testCaseColumns,
            List<InputRowEntry> inputRows,
            List<AssertionEntry> assertionRows)
        {
            // Column offset: F = column 6, G = 7, H = 8, ...
            for (int tcIdx = 0; tcIdx < testCaseColumns.Count; tcIdx++)
            {
                var columnLetter = GetExcelColumnLetter(6 + tcIdx); // 6 = F

                foreach (var (type, rowIndex) in testCaseColumns[tcIdx])
                {
                    int excelRow;

                    if (type == "I")
                    {
                        // input value row
                        excelRow = InputStartRow + rowIndex;
                    }
                    else // "A"
                    {
                        // assertion row
                        excelRow = AssertionStartRow + rowIndex;
                    }

                    var cell = sheet.Cell(columnLetter + excelRow);
                    cell.Value = "O";
                    cell.Style.Font.FontName = "Tahoma";
                    cell.Style.Font.FontSize = 13;
                    cell.Style.Font.Bold = true;
                }
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Converts a 1-based column index to an Excel column letter (A, B, ..., Z, AA, AB, ...).
        /// </summary>
        private static string GetExcelColumnLetter(int columnIndex)
        {
            // columnIndex is 1-based
            var columnName = "";
            while (columnIndex > 0)
            {
                var remainder = (columnIndex - 1) % 26;
                columnName = (char)('A' + remainder) + columnName;
                columnIndex = (columnIndex - 1) / 26;
            }
            return columnName;
        }

        /// <summary>
        /// Removes characters that are invalid in file names.
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var parts = name.Split(invalid, StringSplitOptions.RemoveEmptyEntries);
            return string.Join("_", parts);
        }
    }
}
