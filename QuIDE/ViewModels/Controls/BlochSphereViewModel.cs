using System;
using System.IO;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using QuIDE.CodeHelpers;

namespace QuIDE.ViewModels.Controls;

public class BlochSphereViewModel : ViewModelBase
{
    private Bitmap _blochImage;
    private int _horizontalDegree;
    private int _verticalDegree;
    private string _stateVector;
    private int _renderSize = 400;
    private readonly int _defaultAzimuthDegrees;
    private readonly int _defaultElevationDegrees;

    public string StateVector
    {
        get => _stateVector;
        set
        {
            _stateVector = value;
            this.OnPropertyChanged(nameof(StateVector));
        }
    }

    public int VerticalDegree
    {
        get => _verticalDegree;
        set
        {
            _verticalDegree = value;
            this.OnPropertyChanged(nameof(VerticalDegree));
        }
    }

    public int HorizontalDegree
    {
        get => _horizontalDegree;
        set
        {
            _horizontalDegree = value;
            this.OnPropertyChanged(nameof(HorizontalDegree));
        }
    }

    public Bitmap BlochImage
    {
        get => _blochImage;
        set
        {
            _blochImage = value;
            this.OnPropertyChanged(nameof(BlochImage));
        }
    }

    public int RenderSize
    {
        get => _renderSize;
        set
        {
            // Only update if the change is significant to avoid rapid-fire updates
            if (Math.Abs(_renderSize - value) < 5) 
                return;
            
            _renderSize = value;
            OnPropertyChanged(nameof(RenderSize));
        }
    }
    
    
    public ICommand ResetViewCmd { get; }


    public BlochSphereViewModel()
    {
        // needed for avalonia binding
    }
    
    public BlochSphereViewModel(int defaultAzimuthDegrees, int defaultElevationDegrees)
    {
        ResetViewCmd = new DelegateCommand(ResetView, _ => true);
        _defaultAzimuthDegrees = defaultAzimuthDegrees;
        _defaultElevationDegrees = defaultElevationDegrees;
        ResetView(null);
    }

    public void ResetView(object obj)
    {
        HorizontalDegree = _defaultAzimuthDegrees;
        VerticalDegree = _defaultElevationDegrees;
    }

    /// <summary>
    /// Converts the ScottPlot plot image to an Avalonia Bitmap. 
    /// </summary>
    /// <param name="plot">A plot image, created through ScottPlot.GetImage(width, height)</param>
    /// <returns>Avlonia Bitmap, which can be used for UI Bindings</returns>
    public Avalonia.Media.Imaging.Bitmap ToBitmap(ScottPlot.Image plot)
    {
        if (plot == null)
            ClearImage("No Bloch Sphere to render");
        
        using (MemoryStream ms = new MemoryStream(plot.GetImageBytes()))
        {
            return new Bitmap(ms);
        }
    }
    
    public void ClearImage(string message)
    {
        BlochImage = null;
        StateVector = message;
    }
}