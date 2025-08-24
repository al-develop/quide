using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
        if(newSize >= 300)
            return;
        vm.RenderSize = newSize;
        
    }
}