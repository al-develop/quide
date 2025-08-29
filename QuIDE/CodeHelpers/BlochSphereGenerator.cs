using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ScottPlot;
using ScottPlot.Plottables;

namespace QuIDE.CodeHelpers;

public record struct Coord3D(double X, double Y, double Z);

public class BlochSphereGenerator
{
    public int DefaultAzimuthDegrees => 135;
    public int DefaultElevationDegrees => 245;

    private int _azimuthDegrees;
    private int _elevationDegrees;

    public BlochSphereGenerator()
    {
        _azimuthDegrees = DefaultAzimuthDegrees;
        _elevationDegrees = DefaultElevationDegrees;
    }

    /// <summary>
    /// Set Camera position to look at the Bloch Sphere
    /// </summary>
    /// <param name="horizontal">Horizontal angle to look from (azimuth)</param>
    /// <param name="vertical">Vertical angle to look from (elevation)</param>
    public void SetViewpoint(int horizontal, int vertical)
    {
        _azimuthDegrees = horizontal == 0 ? _azimuthDegrees : horizontal;
        _elevationDegrees = vertical == 0 ? _elevationDegrees : vertical;
    }

    /// <summary>
    /// Generate a square Bloch Sphere Image from Complex arguments. 
    /// </summary>
    /// <param name="alpha">Amplitude alpha</param>
    /// <param name="beta">Amplitude beta</param>
    /// <param name="imgSize">Size of the image. Used for width and height.</param>
    /// <returns></returns>
    public ScottPlot.Image GeneratePlot(Complex alpha, Complex beta, int imgSize, uint phaseColor)
    {
        const double plotLimit = 1.3;
        
        Normalize(ref alpha, ref beta);
        Coord3D blochVector = ConvertToBlochVector(alpha, beta);
        Plot plot = new();
        plot.Title("");
        plot.Layout.Frameless();
        plot.HideGrid();
        plot.Axes.SetLimits(-plotLimit, plotLimit, -plotLimit, plotLimit);
        plot.Axes.Rules.Clear();
        
        DrawSphereWireframe(plot);
        DrawAxesAndLabels(plot);
        DrawStateVector(plot, blochVector, phaseColor);

        return plot.GetImage(imgSize, imgSize);     // Allow only squared images
    }

    private void Normalize(ref Complex alpha, ref Complex beta)
    {
        double magnitude = Math.Sqrt(Math.Pow(alpha.Magnitude, 2) + Math.Pow(beta.Magnitude, 2));
        if (magnitude == 0)
            magnitude = 1;
        alpha /= magnitude; // "Scale" down to probabilities (between 0 and 1)
        beta /= magnitude;
    }

    // "Mapping" from alpha/beta to "angles" theta/psi to find any point on the surface
    private Coord3D ConvertToBlochVector(Complex alpha, Complex beta)
    {
        Complex num = 2 * (Complex.Conjugate(alpha) * beta); // conjugate alpha = theta/2
        double real = num.Real;
        double imaginary = num.Imaginary; // y
        double z = Math.Pow(alpha.Magnitude, 2) - Math.Pow(beta.Magnitude, 2);
        return new Coord3D(real, imaginary, z);
    }

    private void DrawSphereWireframe(Plot plot)
    {
        const double negativeHalfPi = -Math.PI / 2;
        const double halfPi = -(negativeHalfPi);
        const double eightStep = Math.PI / 8;
        const double doublePi = 2 * Math.PI;

        // Latitude lines
        for (double lat = negativeHalfPi; lat <= halfPi; lat += eightStep)
        {
            List<Coord3D> points = new();
            for (double lon = 0; lon <= doublePi; lon += 0.1)
            {
                points.Add(new Coord3D(
                    Math.Cos(lat) * Math.Cos(lon),
                    Math.Cos(lat) * Math.Sin(lon),
                    Math.Sin(lat)
                ));
            }

            Draw3DLine(plot, points, Colors.LightGray, 1, LinePattern.Solid);
        }

        // Longitude
        for (double lon = 0; lon < doublePi; lon += eightStep)
        {
            List<Coord3D> points = new();
            for (double lat = negativeHalfPi; lat <= halfPi; lat += 0.1)
            {
                points.Add(new Coord3D(
                    Math.Cos(lat) * Math.Cos(lon),
                    Math.Cos(lat) * Math.Sin(lon),
                    Math.Sin(lat)
                ));
            }

            Draw3DLine(plot, points, Colors.LightGray, 1, LinePattern.Solid);
        }
    }

    private void DrawAxesAndLabels(Plot plot)
    {
        // Axes
        List<Coord3D> X_Axis = new() { new(-1, 0, 0), new(1, 0, 0) };
        List<Coord3D> Y_Axis = new() { new(0, -1, 0), new(0, 1, 0) };
        List<Coord3D> Z_Axis = new() { new(0, 0, -1), new(0, 0, 1) };

        Draw3DLine(plot, X_Axis, Colors.Gray, 1, LinePattern.DenselyDashed);
        Draw3DLine(plot, Y_Axis, Colors.Gray, 1, LinePattern.DenselyDashed);
        Draw3DLine(plot, Z_Axis, Colors.Gray, 1, LinePattern.DenselyDashed);

        // Labels
        AddText(plot, "|0⟩", new Coord3D(0, 0, 1.1));
        AddText(plot, "|1⟩", new Coord3D(0, 0, -1.1));
        AddText(plot, "|+⟩", new Coord3D(1.1, 0, 0));
        AddText(plot, "|–⟩", new Coord3D(-1.1, 0, 0));
        AddText(plot, "|i⟩", new Coord3D(0, 1.1, 0));
        AddText(plot, "|-i⟩", new Coord3D(0, -1.1, 0));
    }

    private void DrawStateVector(Plot plot, Coord3D vector, uint phaseColor)
    {
        Color fillStyleColor = ScottPlot.Color.FromARGB(phaseColor);
        
        List<Coord3D> coordinates = new() { new(0, 0, 0), vector };
        Draw3DLine(plot, coordinates, fillStyleColor, 3, LinePattern.Solid, true);
        
        var (_, projectedEnd) = Project(vector);
        Marker arrowHead = plot.Add.Marker(projectedEnd);
        arrowHead.MarkerStyle.FillStyle.Color = fillStyleColor;
        arrowHead.MarkerStyle.Shape = MarkerShape.FilledTriangleUp;
        arrowHead.MarkerStyle.Size = 7f;
    }

    private Scatter Draw3DLine(Plot plot, List<Coord3D> points3d, Color color, float lineWidth, LinePattern pattern, bool ignoreDepth = false)
    {
        // Project 3D to 2D -> create illusion of 3D on a 2D plane
        var projectedPoints = points3d.Select(s => Project(s).projected).ToList();
        var avgDepth = points3d.Average(a => Project(a).rotated.Z);

        var scatter = plot.Add.Scatter(projectedPoints);
        scatter.Color = color;
        scatter.LineWidth = lineWidth;

        if (ignoreDepth)
            scatter.LinePattern = pattern;
        else
            scatter.LinePattern = (avgDepth < -0.1) ? LinePattern.Dotted : pattern;

        scatter.MarkerSize = 0;
        return scatter;
    }

    private void AddText(Plot plt, string text, Coord3D position)
    {
        var (_, projectedPos) = Project(position);
        var label = plt.Add.Text(text, projectedPos);
        label.LabelStyle.FontSize = 18;
        label.LabelStyle.Bold = true;
        label.LabelStyle.Alignment = Alignment.MiddleCenter;
    }

    /// <summary>
    /// Project 3D coordinate into a 2D coordinate
    /// </summary>
    private (Coord3D rotated, Coordinates projected) Project(Coord3D coord)
    {
        // convert view angles to radiants
        double azimuth = _azimuthDegrees * Math.PI / 180.0;
        double elevation = _elevationDegrees * Math.PI / 180.0;

        // apply rotation matrix
        // > around Z-axis (azimuth - horizontal)
        double x_1 = coord.X * Math.Cos(azimuth) - coord.Y * Math.Sin(azimuth);
        double y_1 = coord.X * Math.Sin(azimuth) + coord.Y * Math.Cos(azimuth);
        double z_1 = coord.Z;

        // > around X-axis (elevation - vertical)
        double x_2 = x_1;
        double y_2 = y_1 * Math.Cos(elevation) - z_1 * Math.Sin(elevation);
        double z_2 = y_1 * Math.Sin(elevation) + z_1 * Math.Cos(elevation); // depth

        Coord3D result3D = new(x_2, y_2, z_2);
        Coordinates result2D = new(x_2, y_2);
        return (result3D, result2D);
    }
    
    public static ScottPlot.Image GeneratePlaceholder(int imgSize, string text)
    {
        var plot = new Plot();

        // uniform gray background
        var bg = new ScottPlot.Color(211, 211, 211, 100);
        plot.FigureBackground.Color = bg; // outside the data area
        plot.DataBackground.Color = bg;   // inside the data area

        // hide axes/ticks/frame
        plot.Axes.Frameless();
        plot.Axes.SetLimits(0, 1, 0, 1);
        plot.HideGrid();

        // centered text
        var label = plot.Add.Text(text, 0.5, 0.5);
        label.Alignment = Alignment.MiddleCenter;
        label.Color = new ScottPlot.Color(60, 60, 60);
        label.FontSize = 24;
        
        return plot.GetImage(imgSize, imgSize);
    }
}