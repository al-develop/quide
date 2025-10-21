using System;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using QuIDE.CodeHelpers;
using QuIDE.ViewModels.Controls;

namespace QuIDE.Views.Controls;

public partial class BlochSphere : UserControl
{
    private bool _isDragging;
    private Point _pressPoint;
    private int _startAzimuth;
    private int _startElevation;
    private const double DegreesPerPixel = 0.5;

    public BlochSphere()
    {
        InitializeComponent();
        this.SizeChanged += OnBlochSphereSizeChanged;
    }

    private void OnBlochSphereSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is not BlochSphereViewModel vm)
            return;

        // Use the smaller of the width or height to maintain a square aspect ratio
        int newSize = (int)Math.Min(e.NewSize.Width, e.NewSize.Height);
        vm.RenderSize = newSize;
    }

    private void BlochSphereImage_OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
            
        
        if (DataContext is not BlochSphereViewModel vm)
            return;

        if (e.ClickCount == 2)
        {
            vm.ResetView(null);
            return;
        }
        var pt = e.GetCurrentPoint(BlochSphereImage);
        if (!pt.Properties.IsLeftButtonPressed)
            return;

        _isDragging = true;
        _pressPoint = e.GetPosition(BlochSphereImage);
        _startAzimuth = vm.HorizontalDegree;
        _startElevation = vm.VerticalDegree;

        // Capture pointer so drag continues even if the pointer leaves the image.
        e.Pointer.Capture(BlochSphereImage);

        // if (GetVisualRoot() is TopLevel top)
        // Cursor = new Cursor(StandardCursorType.SizeAll);
    }

    private void BlochSphereImage_OnPointerMoved(object sender, PointerEventArgs e)
    {
        if (!_isDragging)
            return;
        if (DataContext is not BlochSphereViewModel vm)
            return;

        var p = e.GetPosition(BlochSphereImage);
        var dx = p.X - _pressPoint.X;
        var dy = p.Y - _pressPoint.Y;

        // Horizontal mouse movement -> azimuth (increase to the right)
        // Vertical mouse movement -> elevation (drag up decreases dy, typically increase elevation)
        //      change - to + to invert direction of rotation.
        var newAzimuth = Normalize360(_startAzimuth - (int)(dx * DegreesPerPixel));
        var newElevation = Normalize360(_startElevation - (int)(dy * DegreesPerPixel));

        vm.HorizontalDegree = newAzimuth;
        vm.VerticalDegree = newElevation;
    }

    private void BlochSphereImage_OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging)
            return;
        
        _isDragging = false;
        e.Pointer.Capture(null);
    }

    private int Normalize360(int degrees)
    {
        // keep 0 mapped to 360 for consistency.
        return BlochSphereGenerator.Mod360(degrees);
    }
}