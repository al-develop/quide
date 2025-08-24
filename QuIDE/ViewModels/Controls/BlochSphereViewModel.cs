using Avalonia.Media.Imaging;

namespace QuIDE.ViewModels.Controls;

public class BlochSphereViewModel : ViewModelBase
{
    private Bitmap _blochImage;
    private int _horizontalDegree;
    private int _verticalDegree;
    private string _stateVector;
    private int _imgWidth;
    private int _imgHeight;

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

    public int ImgWidth
    {
        get => _imgWidth;
        set
        {
            _imgWidth = value;
            OnPropertyChanged(nameof(ImgWidth));
        }
    }

    public int ImgHeight
    {
        get => _imgHeight;
        set
        {
            _imgHeight = value;
            OnPropertyChanged(nameof(ImgHeight));
        }
    }

    public void SetImageSize(int imgSize)
    {
        this.ImgWidth = imgSize;
        this.ImgHeight = imgSize;
    }
}