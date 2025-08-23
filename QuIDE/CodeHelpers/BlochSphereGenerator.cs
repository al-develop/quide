using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualBasic.CompilerServices;
using ScottPlot;
using ScottPlot.Plottables;

namespace QuIDE.CodeHelpers;

public record struct Coord3D(double X, double Y, double Z);

public class BlochSphereGenerator
{
    private int _azimuthDegrees = 135;
    private int _elevationDegrees = 245;

    public void SetViewpoint(int horizontal, int vertical)
    {
        _azimuthDegrees = horizontal == 0 ? _azimuthDegrees : horizontal;
        _elevationDegrees = vertical == 0 ? _elevationDegrees : vertical;
    }

    public ScottPlot.Image GeneratePlot(Complex alpha, Complex beta, int width, int height)
    {
        const double plotLimit = 1.3;
        
        Normalize(ref alpha, ref beta);
        Coord3D blochVector = ConvertToBlochVector(alpha, beta);
        Plot plot = new();
        plot.Title("");
        plot.Layout.Frameless();
        plot.Axes.SetLimits(-plotLimit, plotLimit, -plotLimit, plotLimit);
        plot.Axes.Rules.Clear();
        
        DrawSphereWireframe(plot);
        DrawAxesAndLabels(plot);
        DrawStateVector(plot, blochVector);

        return plot.GetImage(width, height);
    }

    public void Normalize(ref Complex alpha, ref Complex beta)
    {
        double magnitude = Math.Sqrt(Math.Pow(alpha.Magnitude, 2) + Math.Pow(beta.Magnitude, 2));
        if (magnitude == 0)
            magnitude = 1;
        alpha /= magnitude; // "Scale" down to probabilities (between 0 and 1)
        beta /= magnitude;
    }

    // "Mapping" from alpha/beta to "angles" theta/psi to find any point on the surface
    public Coord3D ConvertToBlochVector(Complex alpha, Complex beta)
    {
        Complex num = 2 * (Complex.Conjugate(alpha) * beta); // conjugate alpha = theta/2
        double real = num.Real;
        double imaginary = num.Imaginary; // y
        double z = Math.Pow(alpha.Magnitude, 2) - Math.Pow(beta.Magnitude, 2);
        return new Coord3D(real, imaginary, z);
    }

    public void DrawSphereWireframe(Plot plot)
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

    public void DrawAxesAndLabels(Plot plot)
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

    public void DrawStateVector(Plot plot, Coord3D vector)
    {
        List<Coord3D> coordinates = new() { new(0, 0, 0), vector };
        Scatter line = Draw3DLine(plot, coordinates, Colors.Red, 3, LinePattern.Solid);
        var (_, projectedEnd) = Project(vector);
        var arrowHead = plot.Add.Marker(projectedEnd);
        arrowHead.MarkerStyle.FillStyle.Color = Colors.Red;
        arrowHead.MarkerStyle.Shape = MarkerShape.FilledTriangleUp;
        arrowHead.MarkerStyle.Size = 7f;
    }

    private Scatter Draw3DLine(Plot plot, List<Coord3D> points3d, Color color, float lineWidth, LinePattern pattern)
    {
        // Project 3D to 2D -> create illusion of 3D on a 2D plane
        var projectedPoints = points3d.Select(s => Project(s).projected).ToList();
        var avgDepth = points3d.Average(a => Project(a).rotated.Z);

        var scatter = plot.Add.Scatter(projectedPoints);
        scatter.Color = color;
        scatter.LineWidth = lineWidth;
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
        // view angles to radiants
        double azimuth = _azimuthDegrees * Math.PI / 180.0;
        double elevation = _elevationDegrees * Math.PI / 180.0;

        // rotation matrix
        // > around Z-axis (azimuth - horizontal)
        double x_1 = coord.X * Math.Cos(azimuth) - coord.Y * Math.Sin(azimuth);
        double y_1 = coord.X * Math.Sin(azimuth) - coord.Y * Math.Cos(azimuth);
        double z_1 = coord.Z;

        // > around X-axis (elevation - vertical)
        double x_2 = x_1;
        double y_2 = y_1 * Math.Cos(elevation) - z_1 * Math.Sin(elevation);
        double z_2 = y_1 * Math.Sin(elevation) + z_1 * Math.Cos(elevation); // depth

        Coord3D result3D = new(x_2, y_2, z_2);
        Coordinates result2D = new(x_2, y_2);
        return (result3D, result2D);
    }

    public Avalonia.Media.Imaging.Bitmap ToBitmap(ScottPlot.Image plot)
    {
        using (MemoryStream ms = new MemoryStream(plot.GetImageBytes()))
        {
            return new Avalonia.Media.Imaging.Bitmap(ms);
        }
    }
}