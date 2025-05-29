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


namespace XRayApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController : ControllerBase
    {
        private Image<Rgba32> GenerateChart(List<double> teta, List<double> XI)
        {
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
                    var textSize = TextMeasurer.MeasureSize(xLabel, new TextOptions(fontSmall));
                    ctx.DrawText(xLabel, fontSmall, Color.Black, new PointF(xPos - textSize.Width / 2, height - marginBottom + 10));
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
                    var textSize = TextMeasurer.MeasureSize(yLabel, new TextOptions(fontSmall));
                    ctx.DrawText(yLabel, fontSmall, Color.Black, new PointF(marginLeft - textSize.Width - 12, yPos - textSize.Height / 2));
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

        [HttpPost("analyze")]
        public ActionResult<AnalysisResult> Analyze([FromBody] AnalysisInput input)
        {

            //Modify data
            double WaveLength = Convert.ToDouble(input.Wavelength);
            double AusteniteProcentage = Convert.ToDouble(input.AusteniteContent)/100;
            double CarbonProcentage = Convert.ToDouble(input.CarbonContent)/100;
            double Resolution = Convert.ToDouble(input.Resolution);
            double HalfWidth = Convert.ToDouble(input.HalfWidth);

            var teta = new List<double>();
            var XReyIntensity = new List<double>();

            //Const Parameters
            double aM = 2.8663;
            double aA = 3.656;

            //Calculation of c parameter

            double deformedTetragonEdge = aM * (1 + 0.031 * CarbonProcentage);
            double TetragonEdge = aM;

            //Calculation of d parameters

            double directionA111 = aA / Math.Sqrt(3);
            double directionA200 = aA / Math.Sqrt(4);
            double directionA220 = aA / Math.Sqrt(8);
            double directionM110 = TetragonEdge / Math.Sqrt(2);
            double directionM200 = TetragonEdge / Math.Sqrt(4);
            double directionM211 = Math.Sqrt(1 / (((4 + 1) / (TetragonEdge * TetragonEdge)) + (1 / (deformedTetragonEdge * deformedTetragonEdge))));
            double directionMT110 = Math.Sqrt(1 / (((1 + 0) / (TetragonEdge * TetragonEdge)) + (1 / (deformedTetragonEdge * deformedTetragonEdge))));
            double directionMT200 = Math.Sqrt(1 / (((0 + 0) / (TetragonEdge * TetragonEdge)) + (4 / (deformedTetragonEdge * deformedTetragonEdge))));
            double directionMT211 = Math.Sqrt(1 / (((1 + 1) / (TetragonEdge * TetragonEdge)) + (4 / (deformedTetragonEdge * deformedTetragonEdge))));

            //Calculation of 2teta
            double tetaA111 = 2 * Math.Asin(WaveLength / 2 / directionA111) * (180 / Math.PI);
            double tetaA200 = 2 * Math.Asin(WaveLength / 2 / directionA200) * (180 / Math.PI);
            double tetaA220 = 2 * Math.Asin(WaveLength / 2 / directionA220) * (180 / Math.PI);
            double tetaM110 = 2 * Math.Asin(WaveLength / 2 / directionM110) * (180 / Math.PI);
            double tetaM200 = 2 * Math.Asin(WaveLength / 2 / directionM200) * (180 / Math.PI);
            double tetaM211 = 2 * Math.Asin(WaveLength / 2 / directionM211) * (180 / Math.PI);
            double tetaMT110 = 2 * Math.Asin(WaveLength / 2 / directionMT110) * (180 / Math.PI);
            double tetaMT200 = 2 * Math.Asin(WaveLength / 2 / directionMT200) * (180 / Math.PI);
            double tetaMT211 = 2 * Math.Asin(WaveLength / 2 / directionMT211) * (180 / Math.PI);

            //Calculation of Intensity
            double MartensiteProcentage = 1 - AusteniteProcentage;
            double C = 1.645003;
            double A = AusteniteProcentage / (C - C * AusteniteProcentage);
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

            //Calculation of data

            double I1;
            double I2;
            double I3;
            double I4;
            double I5;
            double I6;
            double I7;
            double I8;
            double I9;

            for (int i = 10; i < 180 / Resolution; i++)
            {
                double x = i * Resolution;
                teta.Add(x);

                I1 = Math.Exp(-(x - tetaA111) * (x - tetaA111) / 2 / (HalfWidth * HalfWidth)) * IA111;
                I2 = Math.Exp(-(x - tetaA200) * (x - tetaA200) / 2 / (HalfWidth * HalfWidth)) * IA200;
                I3 = Math.Exp(-(x - tetaA220) * (x - tetaA220) / 2 / (HalfWidth * HalfWidth)) * IA220;
                I4 = Math.Exp(-(x - tetaM110) * (x - tetaM110) / 2 / (HalfWidth * HalfWidth)) * IM110;
                I5 = Math.Exp(-(x - tetaM200) * (x - tetaM200) / 2 / (HalfWidth * HalfWidth)) * IM200;
                I6 = Math.Exp(-(x - tetaM211) * (x - tetaM211) / 2 / (HalfWidth * HalfWidth)) * IM211;
                I7 = Math.Exp(-(x - tetaMT110) * (x - tetaMT110) / 2 / (HalfWidth * HalfWidth)) * IMT110;
                I8 = Math.Exp(-(x - tetaMT200) * (x - tetaMT200) / 2 / (HalfWidth * HalfWidth)) * IMT200;
                I9 = Math.Exp(-(x - tetaMT211) * (x - tetaMT211) / 2 / (HalfWidth * HalfWidth)) * IMT211;

                XReyIntensity.Add(0.95 * (I1 + I2 + I3 + I4 + I5 + I6 + I7 + I8 + I9) + 5);
            }
            var image = GenerateChart(teta, XReyIntensity);
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            ms.Position = 0;
            return File(ms.ToArray(), "image/png");
        }
    }
}
