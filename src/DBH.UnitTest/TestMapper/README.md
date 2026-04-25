# Test Case Mapper

A CLI tool for extracting and mapping unit test cases from C# test files into structured JSON output.

## Location

`src/DBH.UnitTest/TestMapper/`

## Usage

```bash
# From the solution root directory
dotnet run --project "src/DBH.UnitTest/TestMapper/TestMapper.csproj" -- <command>
```

### Commands

| Command | Description |
|---------|-------------|
| `--list` | List all available test functions across all test files |
| `--function <name>` | Map all test cases for a specific function |
| `--test <method>` | Map a specific test method |
| `--generate-template <src.xlsx> [out.xlsx]` | Read and recreate an Excel template workbook |

### Examples

```bash
# List all available functions
dotnet run --project "src/DBH.UnitTest/TestMapper/TestMapper.csproj" -- --list

# Map all RegisterAsync test cases
dotnet run --project "src/DBH.UnitTest/TestMapper/TestMapper.csproj" -- --function RegisterAsync

# Map a specific test case
dotnet run --project "src/DBH.UnitTest/TestMapper/TestMapper.csproj" -- --test RegisterAsync_01

# Read template and generate a recreated workbook
dotnet run --project "src/DBH.UnitTest/TestMapper/TestMapper.csproj" -- --generate-template "Report5_Unit Test.xlsx" "output/Report5_Template_Generated.xlsx"
```

## Output

- **Console**: Displays test case details (test name, inputs, assertions, type, expected result)
- **JSON**: Saved to `output/{function}.json` or `output/{testMethod}.json` in the solution root
- **Function list**: Saved to `output/available_functions.json`

## Project Structure

```
TestMapper/
├── Models/
│   └── TestCaseMapping.cs     # Data models for test case mapping
├── Parsers/
│   └── TestFileParser.cs      # Parser for extracting test cases from C# files
├── SimpleTestMapper.cs         # Main CLI entry point
├── TestMapper.csproj           # Project file
└── README.md                   # This file
```

## Test Files Discovered

The tool scans the `src/DBH.UnitTest/unitTest/` directory for C# test files. Currently discovers:

- **11 test files** across 9 service directories
- **Auth service**: 28 functions (~170 test cases)
- **EHR service**: 17 functions (~170 test cases)
- **Organization service**: 26 functions (~115 test cases)
- **Shared infrastructure**: ~58 functions (~254 test cases)
- **Appointment, Audit, Consent, Notification, Payment services**: Additional test cases
