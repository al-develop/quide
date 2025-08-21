using Avalonia.Media.Imaging;

namespace QuIDE.ViewModels.Controls;

public class BlochSphereViewModel : ViewModelBase
{
    private Bitmap _blochImage;
    private int _horizontalDegree;
    private int _verticalDegree;
    private string _stateVector;

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
}