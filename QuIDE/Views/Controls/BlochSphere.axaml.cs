using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
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
        // if(newSize >= 300)
        //     return;
        vm.RenderSize = newSize;
        
    }
    //
    // private Timer? _resizeDebounceTimer;
    // private int _lastKnownSize;
    // private const int DebounceTimeMs = 250; // 250ms delay
    //
    // public BlochSphere()
    // {
    //     InitializeComponent();
    //     this.SizeChanged += OnBlochSphereSizeChanged;
    // }
    //
    // private void OnBlochSphereSizeChanged(object? sender, SizeChangedEventArgs e)
    // {
    //     // Stop any pending timer from the previous event
    //     _resizeDebounceTimer?.Dispose();
    //
    //     // Store the latest size from the event arguments
    //     _lastKnownSize = (int)Math.Min(e.NewSize.Width, e.NewSize.Height);
    //
    //     // Start a new timer that will fire once after the delay
    //     _resizeDebounceTimer = new Timer(
    //         callback: _ => OnDebounceTimerElapsed(), 
    //         state: null, 
    //         dueTime: DebounceTimeMs, 
    //         period: Timeout.Infinite);
    // }
    //
    // private void OnDebounceTimerElapsed()
    // {
    //     // The timer callback executes on a background thread.
    //     // We must dispatch the ViewModel update to the UI thread.
    //     Dispatcher.UIThread.Post(() =>
    //     {
    //         if (DataContext is BlochSphereViewModel vm)
    //         {
    //             // Now, update the ViewModel with the final size.
    //             // This will trigger the regeneration.
    //             vm.RenderSize = _lastKnownSize;
    //         }
    //     });
    // }
    //
    // // Clean up the timer when the control is no longer needed
    // public void Dispose()
    // {
    //     _resizeDebounceTimer?.Dispose();
    //     GC.SuppressFinalize(this);
    // }
}