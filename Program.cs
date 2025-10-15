using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace EmployeePieChart
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private const string ApiUrl = "https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code=vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";

        // Color palette for the pie chart segments
        private static readonly Color[] ChartColors = new[]
        {
            Color.FromArgb(255, 99, 132),   // Red
            Color.FromArgb(54, 162, 235),   // Blue
            Color.FromArgb(255, 205, 86),   // Yellow
            Color.FromArgb(75, 192, 192),   // Green
            Color.FromArgb(153, 102, 255),  // Purple
            Color.FromArgb(255, 159, 64),   // Orange
            Color.FromArgb(201, 203, 207),  // Gray
            Color.FromArgb(255, 99, 255),   // Pink
            Color.FromArgb(50, 168, 82),    // Dark Green
            Color.FromArgb(123, 36, 28)     // Brown
        };

        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Fetching employee data...");
                
                // Fetch employee data from API
                var employees = await GetEmployeeDataAsync();
                
                // Process and calculate percentages
                var chartData = ProcessEmployeeData(employees);
                
                // Generate pie chart
                Console.WriteLine("Generating pie chart...");
                string imagePath = "employee_pie_chart.png";
                GeneratePieChart(chartData, imagePath, 800, 600);
                
                Console.WriteLine($"Pie chart generated successfully!");
                Console.WriteLine($"File saved as: {Path.GetFullPath(imagePath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
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

        public static List<EmployeeChartData> ProcessEmployeeData(List<EmployeeEntry> entries)
        {
            // Group by employee and calculate total hours
            var employeeGroups = entries
                .Where(e => !string.IsNullOrEmpty(e.EmployeeName))
                .GroupBy(e => e.EmployeeName)
                .Select(g => new 
                {
                    Name = g.Key,
                    TotalHours = g.Sum(e => CalculateHoursWorked(e.StarTimeUtc, e.EndTimeUtc))
                })
                .Where(e => e.TotalHours > 0)
                .OrderByDescending(e => e.TotalHours)
                .ToList();

            double totalAllHours = employeeGroups.Sum(e => e.TotalHours);

            // Convert to chart data with percentages
            var chartData = employeeGroups
                .Select((e, index) => new EmployeeChartData
                {
                    Name = e.Name,
                    TotalHours = e.TotalHours,
                    Percentage = (e.TotalHours / totalAllHours) * 100,
                    Color = ChartColors[index % ChartColors.Length]
                })
                .ToList();

            return chartData;
        }

        public static double CalculateHoursWorked(DateTime startTime, DateTime endTime)
        {
            if (startTime >= endTime) return 0;
            
            TimeSpan timeWorked = endTime - startTime;
            return timeWorked.TotalHours;
        }

        public static void GeneratePieChart(List<EmployeeChartData> data, string filePath, int width, int height)
        {
            using (Bitmap bitmap = new Bitmap(width, height))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                // Set background color
                graphics.Clear(Color.White);

                // Anti-aliasing for better quality
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Define chart area (with padding)
                Rectangle chartArea = new Rectangle(50, 50, width - 300, height - 100);
                Rectangle legendArea = new Rectangle(width - 230, 50, 200, height - 100);

                // Draw pie chart
                DrawPieChart(graphics, data, chartArea);

                // Draw legend
                DrawLegend(graphics, data, legendArea);

                // Draw title
                DrawTitle(graphics, width);

                // Save image
                bitmap.Save(filePath, ImageFormat.Png);
            }
        }

        private static void DrawPieChart(Graphics graphics, List<EmployeeChartData> data, Rectangle chartArea)
        {
            float startAngle = 0f;

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                float sweepAngle = (float)(item.Percentage * 3.6f); // 360 degrees / 100%

                using (Brush brush = new SolidBrush(item.Color))
                {
                    graphics.FillPie(brush, chartArea, startAngle, sweepAngle);
                    
                    // Draw segment border
                    using (Pen pen = new Pen(Color.Black, 1))
                    {
                        graphics.DrawPie(pen, chartArea, startAngle, sweepAngle);
                    }
                }

                startAngle += sweepAngle;
            }
        }

        private static void DrawLegend(Graphics graphics, List<EmployeeChartData> data, Rectangle legendArea)
        {
            using (Font legendFont = new Font("Arial", 10))
            using (Font boldFont = new Font("Arial", 10, FontStyle.Bold))
            {
                // Draw legend title
                graphics.DrawString("Employee Work Hours", boldFont, Brushes.Black, 
                    legendArea.X, legendArea.Y - 25);

                int yPos = legendArea.Y;
                const int lineHeight = 20;
                const int colorBoxSize = 15;
                const int colorBoxPadding = 5;

                for (int i = 0; i < data.Count; i++)
                {
                    var item = data[i];
                    
                    // Draw color box
                    using (Brush colorBrush = new SolidBrush(item.Color))
                    {
                        graphics.FillRectangle(colorBrush, legendArea.X, yPos, colorBoxSize, colorBoxSize);
                    }
                    graphics.DrawRectangle(Pens.Black, legendArea.X, yPos, colorBoxSize, colorBoxSize);

                    // Draw legend text
                    string legendText = $"{item.Name} ({item.Percentage:F1}%)";
                    
                    // Truncate long names
                    if (legendText.Length > 25)
                    {
                        legendText = legendText.Substring(0, 22) + "...";
                    }

                    graphics.DrawString(legendText, legendFont, Brushes.Black, 
                        legendArea.X + colorBoxSize + colorBoxPadding, yPos);

                    yPos += lineHeight;

                    // Check if we need to create a new column
                    if (yPos + lineHeight > legendArea.Bottom && i < data.Count - 1)
                    {
                        // This is a simple implementation - for many items, you'd want a better layout
                        break;
                    }
                }
            }
        }

        private static void DrawTitle(Graphics graphics, int width)
        {
            using (Font titleFont = new Font("Arial", 16, FontStyle.Bold))
            {
                string title = "Employee Work Hours Distribution";
                SizeF titleSize = graphics.MeasureString(title, titleFont);
                float titleX = (width - titleSize.Width) / 2;
                
                graphics.DrawString(title, titleFont, Brushes.DarkBlue, titleX, 10);
            }
        }
    }

    // Data models
    public class EmployeeEntry
    {
        public string EmployeeName { get; set; }
        public DateTime StarTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
    }

    public class EmployeeChartData
    {
        public string Name { get; set; }
        public double TotalHours { get; set; }
        public double Percentage { get; set; }
        public Color Color { get; set; }
    }
}