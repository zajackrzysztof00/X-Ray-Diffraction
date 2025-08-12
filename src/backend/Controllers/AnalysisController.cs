using Microsoft.AspNetCore.Mvc;
using XRayApi.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.Fonts;
using System;
using System.Linq;
using System.Numerics;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace XRayApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController : ControllerBase
    {
        private readonly ILogger<AnalysisController> _logger;
        public AnalysisController(ILogger<AnalysisController> logger)
        {
            _logger = logger;
        }

        private const double DefaultWavelength = 1.54;
        private const double DefaultResolution = 0.1;
        private const double DefaultHalfWidth = 0.5;
        private const double DefaultAusteniteContent = 50.0;
        private const double DefaultCarbonContent = 0.2;

        /// <summary>
        /// Generates a chart image from the given data.
        /// </summary>
    private Image<Rgba32> GenerateChart(List<double> teta, List<double> XI)
        {
            // ...existing code...
            int width = 1920;
            int height = 1080;
            int marginLeft = 70;
            int marginBottom = 70;
            int marginTop = 50;
            int marginRight = 40;

            var image = new Image<Rgba32>(width, height);

            image.Mutate(ctx =>
            {
                ctx.Fill(Color.WhiteSmoke);

                var axisPen = Pens.Solid(Color.Black, 3);
                var gridPen = Pens.Solid(Color.LightGray, 1);
                var linePen = Pens.Solid(Color.Red, 1);
                var font = SystemFonts.CreateFont("Arial", 22);
                var fontSmall = SystemFonts.CreateFont("Arial", 20);

                double xMin = teta.Min();
                double xMax = teta.Max();
                double yMin = XI.Min();
                double yMax = XI.Max();

                PointF Scale(double x, double y)
                {
                    float px = (float)((x - xMin) / (xMax - xMin) * (width - marginLeft - marginRight) + marginLeft);
                    float py = (float)(height - marginBottom - (y - yMin) / (yMax - yMin) * (height - marginBottom - marginTop));
                    return new PointF(px, py);
                }

                // Draw grid lines and ticks on X axis
                int xTicks = 10;
                for (int i = 0; i <= xTicks; i++)
                {
                    double xVal = xMin + i * (xMax - xMin) / xTicks;
                    float xPos = (float)(marginLeft + i * (width - marginLeft - marginRight) / xTicks);

                    ctx.DrawLine(gridPen, new PointF(xPos, marginTop), new PointF(xPos, height - marginBottom));
                    ctx.DrawLine(axisPen, new PointF(xPos, height - marginBottom), new PointF(xPos, height - marginBottom + 8));

                    string xLabel = xVal.ToString("0.##");
                }

                // Draw grid lines and ticks on Y axis
                int yTicks = 8;
                for (int i = 0; i <= yTicks; i++)
                {
                    double yVal = yMin + i * (yMax - yMin) / yTicks;
                    float yPos = (float)(height - marginBottom - i * (height - marginBottom - marginTop) / yTicks);
                    ctx.DrawLine(gridPen, new PointF(marginLeft, yPos), new PointF(width - marginRight, yPos));
                    ctx.DrawLine(axisPen, new PointF(marginLeft - 8, yPos), new PointF(marginLeft, yPos));
                    string yLabel = yVal.ToString("0.##");
                }

                // Draw axes
                ctx.DrawLine(axisPen,
                    new PointF(marginLeft, height - marginBottom),
                    new PointF(width - marginRight, height - marginBottom));
                ctx.DrawLine(axisPen,
                    new PointF(marginLeft, height - marginBottom),
                    new PointF(marginLeft, marginTop));

                // Draw axis labels
                string xAxisLabel = "2θ (degrees)";
                string yAxisLabel = "Intensity";

                var xAxisTextSize = TextMeasurer.MeasureSize(xAxisLabel, new TextOptions(font));
                ctx.DrawText(xAxisLabel, font, Color.Black,
                    new PointF((width + marginLeft - marginRight) / 2 - xAxisTextSize.Width / 2, height - marginBottom + 40));

                ctx.Rotate(-90);
                var yAxisTextSize = TextMeasurer.MeasureSize(yAxisLabel, new TextOptions(font));
                ctx.DrawText(yAxisLabel, font, Color.Black,
                    new PointF(-(height + marginBottom + marginTop) / 2 - yAxisTextSize.Width / 2, marginLeft - 50));
                ctx.Rotate(90);

                // Draw chart title
                string title = "X-Ray Diffraction Analysis";
                var titleSize = TextMeasurer.MeasureSize(title, new TextOptions(font));
                ctx.DrawText(title, font, Color.DarkBlue,
                    new PointF((width - titleSize.Width) / 2, 10));

                // Draw data line only (no points)
                var points = teta.Zip(XI, (x, y) => Scale(x, y)).ToArray();

                var pathBuilder = new PathBuilder();
                pathBuilder.AddLines(points);
                IPath path = pathBuilder.Build();

                ctx.Draw(linePen, path);
            });

            return image;
        }

        /// <summary>
        /// Exports the data to CSV format.
        /// </summary>
    private byte[] ExportToCsv(List<double> teta, List<double> intensity)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Teta,Intensity");
            for (int i = 0; i < teta.Count; i++)
            {
                sb.AppendLine($"{teta[i]},{intensity[i]}");
            }
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        /// <summary>
        /// Performs X-Ray diffraction analysis and returns a chart image or CSV data.
        /// </summary>
        [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] AnalysisInput input, [FromQuery] string? format = null)
        {
            try
            {
                // Validate input and use defaults if necessary
                double waveLength = input.Wavelength ?? DefaultWavelength;
                double austeniteProcentage = (input.AusteniteContent ?? DefaultAusteniteContent) / 100.0;
                double carbonProcentage = input.CarbonContent ?? DefaultCarbonContent;
                double resolution = input.Resolution ?? DefaultResolution;
                double halfWidth = input.HalfWidth ?? DefaultHalfWidth;

                if (waveLength <= 0 || resolution <= 0 || halfWidth <= 0)
                {
                    return BadRequest("Wavelength, resolution, and half width must be positive numbers.");
                }
                if (austeniteProcentage < 0 || austeniteProcentage > 1)
                {
                    return BadRequest("Austenite content must be between 0 and 100.");
                }

                var teta = new List<double>();
                var xRayIntensity = new List<double>();

                // Constants
                const double aM = 2.8663;
                const double aA = 3.656;

                // Calculation of c parameter
                double deformedTetragonEdge = aM * (1 + 0.031 * carbonProcentage);
                double tetragonEdge = aM;

                // Calculation of d parameters
                double directionA111 = aA / Math.Sqrt(3);
                double directionA200 = aA / Math.Sqrt(4);
                double directionA220 = aA / Math.Sqrt(8);
                double directionM110 = tetragonEdge / Math.Sqrt(2);
                double directionM200 = tetragonEdge / Math.Sqrt(4);
                double directionM211 = Math.Sqrt(1 / (((4 + 1) / (tetragonEdge * tetragonEdge)) + (1 / (deformedTetragonEdge * deformedTetragonEdge))));
                double directionMT110 = Math.Sqrt(1 / (((1 + 0) / (tetragonEdge * tetragonEdge)) + (1 / (deformedTetragonEdge * deformedTetragonEdge))));
                double directionMT200 = Math.Sqrt(1 / (((0 + 0) / (tetragonEdge * tetragonEdge)) + (4 / (deformedTetragonEdge * deformedTetragonEdge))));
                double directionMT211 = Math.Sqrt(1 / (((1 + 1) / (tetragonEdge * tetragonEdge)) + (4 / (deformedTetragonEdge * deformedTetragonEdge))));

                // Calculation of 2teta
                double tetaA111 = 2 * Math.Asin(waveLength / 2 / directionA111) * (180 / Math.PI);
                double tetaA200 = 2 * Math.Asin(waveLength / 2 / directionA200) * (180 / Math.PI);
                double tetaA220 = 2 * Math.Asin(waveLength / 2 / directionA220) * (180 / Math.PI);
                double tetaM110 = 2 * Math.Asin(waveLength / 2 / directionM110) * (180 / Math.PI);
                double tetaM200 = 2 * Math.Asin(waveLength / 2 / directionM200) * (180 / Math.PI);
                double tetaM211 = 2 * Math.Asin(waveLength / 2 / directionM211) * (180 / Math.PI);
                double tetaMT110 = 2 * Math.Asin(waveLength / 2 / directionMT110) * (180 / Math.PI);
                double tetaMT200 = 2 * Math.Asin(waveLength / 2 / directionMT200) * (180 / Math.PI);
                double tetaMT211 = 2 * Math.Asin(waveLength / 2 / directionMT211) * (180 / Math.PI);

                // Calculation of Intensity
                double martensiteProcentage = 1 - austeniteProcentage;
                double C = 1.645003;
                double A = austeniteProcentage / (C - C * austeniteProcentage);
                double IM = 10000 / (100 + 100 * A);
                double IA = 100 - IM;

                double IA111 = IA;
                double IA200 = 0.44 * IA;
                double IA220 = 0.27 * IA;
                double IM110 = IM * 1 / 3;
                double IM200 = 0.16 * IM * 1 / 3;
                double IM211 = 0.4 * IM * 1 / 3;
                double IMT110 = IM * 2 / 3;
                double IMT200 = 0.16 * IM * 2 / 3;
                double IMT211 = 0.4 * IM * 2 / 3;

                // Calculation of data
                for (int i = 10; i < 180 / resolution; i++)
                {
                    double x = i * resolution;
                    teta.Add(x);

                    double I1 = Math.Exp(-(x - tetaA111) * (x - tetaA111) / 2 / (halfWidth * halfWidth)) * IA111;
                    double I2 = Math.Exp(-(x - tetaA200) * (x - tetaA200) / 2 / (halfWidth * halfWidth)) * IA200;
                    double I3 = Math.Exp(-(x - tetaA220) * (x - tetaA220) / 2 / (halfWidth * halfWidth)) * IA220;
                    double I4 = Math.Exp(-(x - tetaM110) * (x - tetaM110) / 2 / (halfWidth * halfWidth)) * IM110;
                    double I5 = Math.Exp(-(x - tetaM200) * (x - tetaM200) / 2 / (halfWidth * halfWidth)) * IM200;
                    double I6 = Math.Exp(-(x - tetaM211) * (x - tetaM211) / 2 / (halfWidth * halfWidth)) * IM211;
                    double I7 = Math.Exp(-(x - tetaMT110) * (x - tetaMT110) / 2 / (halfWidth * halfWidth)) * IMT110;
                    double I8 = Math.Exp(-(x - tetaMT200) * (x - tetaMT200) / 2 / (halfWidth * halfWidth)) * IMT200;
                    double I9 = Math.Exp(-(x - tetaMT211) * (x - tetaMT211) / 2 / (halfWidth * halfWidth)) * IMT211;

                    xRayIntensity.Add(0.95 * (I1 + I2 + I3 + I4 + I5 + I6 + I7 + I8 + I9) + 5);
                }

                if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
                {
                    var csvBytes = ExportToCsv(teta, xRayIntensity);
                    return File(csvBytes, "text/csv", "xray_analysis.csv");
                }

                var image = GenerateChart(teta, xRayIntensity);
                using var ms = new MemoryStream();
                await image.SaveAsPngAsync(ms);
                ms.Position = 0;
                return File(ms.ToArray(), "image/png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during analysis");
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
    }
}
