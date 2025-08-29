using System;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using QuIDE.ViewModels.Controls;

namespace QuIDE.Views.Controls;

public partial class BlochSphere : UserControl
{
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

    private void BlochSphereImage_OnPointerMoved(object sender, PointerEventArgs e)
    {
     
    }
}