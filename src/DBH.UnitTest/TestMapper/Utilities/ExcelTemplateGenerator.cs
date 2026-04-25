using System;
using System.IO;
using ClosedXML.Excel;

namespace DBH.UnitTest.TestMapper.Utilities
{
    /// <summary>
    /// Reads an Excel template and recreates a new workbook from it.
    /// This is not a byte-copy: the workbook is loaded and written again.
    /// </summary>
    public class ExcelTemplateGenerator
    {
        public string GenerateFromTemplate(string templatePath, string? outputPath = null)
        {
            if (string.IsNullOrWhiteSpace(templatePath))
                throw new ArgumentException("Template path is required.", nameof(templatePath));

            var fullTemplatePath = Path.GetFullPath(templatePath);
            if (!File.Exists(fullTemplatePath))
                throw new FileNotFoundException("Template .xlsx file not found.", fullTemplatePath);

            if (!string.Equals(Path.GetExtension(fullTemplatePath), ".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Template file must be a .xlsx workbook.");

            var fullOutputPath = ResolveOutputPath(fullTemplatePath, outputPath);
            var outputDir = Path.GetDirectoryName(fullOutputPath);
            if (!string.IsNullOrWhiteSpace(outputDir))
                Directory.CreateDirectory(outputDir);

            using var sourceWorkbook = new XLWorkbook(fullTemplatePath);
            using var targetWorkbook = new XLWorkbook();

            CopyWorkbookProperties(sourceWorkbook, targetWorkbook);

            foreach (var sourceSheet in sourceWorkbook.Worksheets)
            {
                sourceSheet.CopyTo(targetWorkbook, sourceSheet.Name);
            }

            targetWorkbook.SaveAs(fullOutputPath);
            return fullOutputPath;
        }

        private static void CopyWorkbookProperties(XLWorkbook sourceWorkbook, XLWorkbook targetWorkbook)
        {
            targetWorkbook.Properties.Author = sourceWorkbook.Properties.Author;
            targetWorkbook.Properties.Category = sourceWorkbook.Properties.Category;
            targetWorkbook.Properties.Comments = sourceWorkbook.Properties.Comments;
            targetWorkbook.Properties.Company = sourceWorkbook.Properties.Company;
            targetWorkbook.Properties.Keywords = sourceWorkbook.Properties.Keywords;
            targetWorkbook.Properties.Manager = sourceWorkbook.Properties.Manager;
            targetWorkbook.Properties.Subject = sourceWorkbook.Properties.Subject;
            targetWorkbook.Properties.Title = sourceWorkbook.Properties.Title;
            targetWorkbook.Properties.Status = sourceWorkbook.Properties.Status;
        }

        private static string ResolveOutputPath(string templatePath, string? outputPath)
        {
            if (!string.IsNullOrWhiteSpace(outputPath))
                return Path.GetFullPath(outputPath);

            var directory = Path.GetDirectoryName(templatePath) ?? Directory.GetCurrentDirectory();
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(templatePath);
            var timeTag = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(directory, $"{fileNameWithoutExtension}_generated_{timeTag}.xlsx");
        }
    }
}
