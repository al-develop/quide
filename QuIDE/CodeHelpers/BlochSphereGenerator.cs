using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Resources;
using Avalonia.Platform;
using QuIDE.Properties;
using ScottPlot;
using ScottPlot.Plottables;
using SkiaSharp;

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
        _azimuthDegrees = Mod360(horizontal);
        _elevationDegrees = Mod360(vertical);
    }

    /// <summary>
    /// Helper method, to normalize rotation degrees to 0 - 360
    /// </summary>
    public static int Mod360(int degree)
    {
        degree %= 360;
        if (degree < 0)
            degree += 360;

        var result = (degree == 0) ? 360 : degree;
        return result;
    }

    /// <summary>
    /// Generate a square Bloch Sphere Image from a 2x2 density matrix.
    /// This method calculates the Bloch vector from the density matrix and renders it.
    /// </summary>
    /// <param name="densityMatrix">The 2x2 complex density matrix of the qubit.</param>
    /// <param name="imgSize">Size of the image. Used for width and height.</param>
    /// <returns>A ScottPlot.Image representing the Bloch Sphere with the state vector.</returns>
    public ScottPlot.Image GeneratePlot(Complex[,] densityMatrix, int imgSize)
    {
        // density Matrix is already normalized
        Coord3D blochVector = ConvertToBlochVector(densityMatrix);
        var color = GetVectorColor(blochVector);
        return GetPlotFromComplexAmplitudes(imgSize, color, blochVector);
    }

    private Image GetPlotFromComplexAmplitudes(int imgSize, Color vectorColor, Coord3D blochVector)
    {
        const double plotLimit = 1.3;
        Plot plot = new();
        plot.Title("");
        plot.Layout.Frameless();
        plot.HideGrid();
        plot.Axes.SetLimits(-plotLimit, plotLimit, -plotLimit, plotLimit);
        plot.Axes.Rules.Clear();

        DrawSphereWireframe(plot);
        DrawAxesAndLabels(plot);
        DrawStateVector(plot, vectorColor, blochVector);

        return plot.GetImage(imgSize, imgSize);
    }

    // // "Mapping" from alpha/beta to "angles" theta/psi to find any point on the surface
    // private Coord3D ConvertToBlochVector(Complex alpha, Complex beta)
    // {
    //     Complex num = 2 * (Complex.Conjugate(alpha) * beta); // conjugate alpha = theta/2
    //     double real = num.Real;
    //     double imaginary = num.Imaginary; // y
    //     double z = Math.Pow(alpha.Magnitude, 2) - Math.Pow(beta.Magnitude, 2);
    //     return new Coord3D(real, imaginary, z);
    // }

    /// <summary>
    /// Converts a 2x2 density matrix into a Bloch vector (x, y, z) coordinates.
    /// </summary>
    /// <param name="densityMatrix">The 2x2 complex density matrix. Assumed to be trace-normalized.</param>
    /// <returns>A Coord3D representing the Bloch vector. Its magnitude will be less than or equal to 1.</returns>
    private Coord3D ConvertToBlochVector(Complex[,] densityMatrix)
    {
        Complex num = 2 * densityMatrix[0, 1];
        double x = num.Real;
        double y = num.Imaginary;
        double z = densityMatrix[0, 0].Real - densityMatrix[1, 1].Real;

        // The resulting Bloch vector accurately represents pure states (on the surface) and mixed states (inside).
        // No further normalization of the vector is needed here, as the density matrix is already normalized.
        return new Coord3D(x, y, z);
    }

    private Color GetVectorColor(Coord3D blochVector)
    {
        // Pure states (length ≈ 1) are red.
        // Mixed states (length < 1) are blue.
        double length = Math.Round(Math.Sqrt(Math.Pow(blochVector.X, 2) + Math.Pow(blochVector.Y, 2) + Math.Pow(blochVector.Z, 2)), 3);
        return (length < 1) ? Colors.Blue : Colors.Red;
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
        AddText(plot, Resources.KetZero, new Coord3D(0, 0, 1.1));
        AddText(plot, Resources.KetOne, new Coord3D(0, 0, -1.1));
        AddText(plot, Resources.KetPlus, new Coord3D(1.1, 0, 0));
        AddText(plot, Resources.KetMinus, new Coord3D(-1.1, 0, 0));
        AddText(plot, Resources.KetNegative_I, new Coord3D(0, 1.1, 0));
        AddText(plot, Resources.KetPositive_I, new Coord3D(0, -1.1, 0));
    }

    private void DrawStateVector(Plot plot, Color vectorColor, Coord3D vector)
    {
        List<Coord3D> coordinates = new() { new(0, 0, 0), vector };
        Draw3DLine(plot, coordinates, vectorColor, 3, LinePattern.Solid, true);

        var (_, projectedEnd) = Project(vector);
        Marker arrowHead = plot.Add.Marker(projectedEnd);
        arrowHead.MarkerStyle.FillStyle.Color = vectorColor;
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
        plot.DataBackground.Color = bg; // inside the data area

        // hide axes/ticks/frame
        plot.Axes.Frameless();
        plot.Axes.SetLimits(0, 1, 0, 1);
        plot.HideGrid();

        // centered text
        var label = plot.Add.Text(text, 0.5, 0.5);
        label.Alignment = Alignment.MiddleCenter;
        label.LabelFontColor = new ScottPlot.Color(60, 60, 60);
        label.LabelFontSize = 18;

        return plot.GetImage(imgSize, imgSize);
    }
}