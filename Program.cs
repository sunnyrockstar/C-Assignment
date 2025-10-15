using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace EmployeeTableGenerator
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private const string ApiUrl = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";

        static async Task Main(string[] args)
        {
            try
            {
                
                var employees = await GetEmployeeDataAsync();
                
                
                var processedData = ProcessEmployeeData(employees);
                
                
                string htmlContent = GenerateHtmlTable(processedData);
                
                
                string filePath = "employees_table.html";
                await File.WriteAllTextAsync(filePath, htmlContent);
                
                Console.WriteLine($"HTML table generated successfully!");
                Console.WriteLine($"File saved as: {Path.GetFullPath(filePath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public static async Task<List<EmployeeEntry>> GetEmployeeDataAsync()
        {
            HttpResponseMessage response = await client.GetAsync(ApiUrl);
            response.EnsureSuccessStatusCode();
            
            string jsonString = await response.Content.ReadAsStringAsync();
            var employees = JsonSerializer.Deserialize<List<EmployeeEntry>>(jsonString);
            
            return employees ?? new List<EmployeeEntry>();
        }

        public static List<EmployeeSummary> ProcessEmployeeData(List<EmployeeEntry> entries)
        {
            
            var employeeGroups = entries
                .Where(e => !string.IsNullOrEmpty(e.EmployeeName))
                .GroupBy(e => e.EmployeeName)
                .Select(g => new EmployeeSummary
                {
                    Name = g.Key,
                    TotalHours = g.Sum(e => CalculateHoursWorked(e.StarTimeUtc, e.EndTimeUtc))
                })
                .Where(e => e.TotalHours > 0) // Filter out employees with invalid time data
                .OrderByDescending(e => e.TotalHours)
                .ToList();

            return employeeGroups;
        }

        public static double CalculateHoursWorked(DateTime startTime, DateTime endTime)
        {
            if (startTime >= endTime) return 0;
            
            TimeSpan timeWorked = endTime - startTime;
            return Math.Round(timeWorked.TotalHours, 2);
        }

        public static string GenerateHtmlTable(List<EmployeeSummary> employees)
        {
            var htmlBuilder = new StringBuilder();

            htmlBuilder.AppendLine("<!DOCTYPE html>");
            htmlBuilder.AppendLine("<html lang=\"en\">");
            htmlBuilder.AppendLine("<head>");
            htmlBuilder.AppendLine("    <meta charset=\"UTF-8\">");
            htmlBuilder.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            htmlBuilder.AppendLine("    <title>Employee Work Hours</title>");
            htmlBuilder.AppendLine("    <style>");
            htmlBuilder.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; }");
            htmlBuilder.AppendLine("        h1 { color: #333; text-align: center; }");
            htmlBuilder.AppendLine("        table { width: 80%; margin: 0 auto; border-collapse: collapse; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }");
            htmlBuilder.AppendLine("        th, td { padding: 12px 15px; text-align: left; border-bottom: 1px solid #ddd; }");
            htmlBuilder.AppendLine("        th { background-color: #4CAF50; color: white; font-weight: bold; }");
            htmlBuilder.AppendLine("        tr:hover { background-color: #f5f5f5; }");
            htmlBuilder.AppendLine("        .less-than-100 { background-color: #ffcccc; }");
            htmlBuilder.AppendLine("        .less-than-100:hover { background-color: #ffb3b3; }");
            htmlBuilder.AppendLine("        .total-hours { font-weight: bold; color: #2c3e50; }");
            htmlBuilder.AppendLine("    </style>");
            htmlBuilder.AppendLine("</head>");
            htmlBuilder.AppendLine("<body>");
            htmlBuilder.AppendLine("    <h1>Employee Work Hours Summary</h1>");
            htmlBuilder.AppendLine("    <table>");
            htmlBuilder.AppendLine("        <thead>");
            htmlBuilder.AppendLine("            <tr>");
            htmlBuilder.AppendLine("                <th>Name</th>");
            htmlBuilder.AppendLine("                <th>Total Time Worked (Hours)</th>");
            htmlBuilder.AppendLine("            </tr>");
            htmlBuilder.AppendLine("        </thead>");
            htmlBuilder.AppendLine("        <tbody>");

            foreach (var employee in employees)
            {
                string rowClass = employee.TotalHours < 100 ? "class=\"less-than-100\"" : "";
                
                htmlBuilder.AppendLine($"            <tr {rowClass}>");
                htmlBuilder.AppendLine($"                <td>{EscapeHtml(employee.Name)}</td>");
                htmlBuilder.AppendLine($"                <td class=\"total-hours\">{employee.TotalHours}</td>");
                htmlBuilder.AppendLine("            </tr>");
            }

            htmlBuilder.AppendLine("        </tbody>");
            htmlBuilder.AppendLine("    </table>");
            htmlBuilder.AppendLine("</body>");
            htmlBuilder.AppendLine("</html>");

            return htmlBuilder.ToString();
        }

        private static string EscapeHtml(string input)
        {
            return System.Net.WebUtility.HtmlEncode(input);
        }
    }

    
    public class EmployeeEntry
    {
        public string EmployeeName { get; set; }
        public DateTime StarTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
    }

    public class EmployeeSummary
    {
        public string Name { get; set; }
        public double TotalHours { get; set; }
    }
}