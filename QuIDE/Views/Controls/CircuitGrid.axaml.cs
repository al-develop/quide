#region

using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using QuIDE.CodeHelpers;
using QuIDE.QuantumModel;
using QuIDE.ViewModels;
using QuIDE.ViewModels.Controls;
using QuIDE.ViewModels.Helpers;

#endregion

namespace QuIDE.Views.Controls;

public partial class CircuitGrid : UserControl
{
    private readonly SolidColorBrush _drawingColor = new(Colors.Black);
    private Line _line;
    private bool _pendingClick;

    // fields to enable single-click for placing Gates
    private GateViewModel _pendingVm;
    private KeyModifiers _pendingModifiers;
    private Control _pendingSource;
    private Point _pressPoint;
    private const double DragThreshold = 4.0; // px 

    public CircuitGrid()
    {
        InitializeComponent();
        drawing.AddHandler(DragDrop.DropEvent, Drawing_Drop);
        drawing.AddHandler(DragDrop.DragOverEvent, Drawing_DragOver, RoutingStrategies.Tunnel);
    }

    private void LayoutRoot_PreviewMouseWheel(object sender, PointerWheelEventArgs e)
    {
        var vm = DataContext as CircuitGridViewModel;
        vm.LayoutRoot_PreviewMouseWheel(e);
    }

    /// <summary>
    ///     Is called by PointerPressed event on GateButton.
    /// </summary>
    /// <param name="sender">GateButton</param>
    /// <param name="e">event</param>
    private async void GateButton_MouseDown(object sender, PointerPressedEventArgs e)
    {
        var source = sender as Control;

        if (e.GetCurrentPoint(source).Properties.IsRightButtonPressed)
            return;

        var shiftPressed = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var vm = source.DataContext as GateViewModel;

        // perform drawing operation for control gate
        ActionName action = MainWindowViewModel.SelectedAction;
        if (action == ActionName.Control && !shiftPressed)
        {
            ExecuteDrawingControlGate(source);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);  // ensure visuals render before starting to drag
            
            var data = new Tuple<int, RegisterRefModel>(vm.Column, vm.Row);
            var dragData = new DataObject();
            dragData.Set(typeof(Tuple<int, RegisterRefModel>).ToString(), data);
            try
            {
                await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Link);
            }
            catch (COMException exception)
            {
                var msg = exception.Message;
                Console.WriteLine(msg);
                // SimpleDialogHandler.ShowSimpleMessage(msg);
            }
            finally
            {
                CleanupDragVisual();
            }
        }

        if (action == ActionName.Selection)
        {
            ExecuteSingleClickSelection(e, vm);
            return;
        }

        // For all other actions (single-qubit gates, Measure, CPhaseShift/InvCPhaseShift, Composite, Ungroup):
        // Enable single-click placement. If the pointer moves, start a drag (for tools that support cross-row spans).
        _pendingClick = true;
        _pendingVm = vm;
        _pendingModifiers = e.KeyModifiers;
        _pendingSource = source;
        _pressPoint = e.GetPosition(source);
        AttachPendingHandlers(source);
    }

    private void ExecuteSingleClickSelection(PointerPressedEventArgs e, GateViewModel vm)
    {
        // single click selection
        vm.SetGate(vm.Column, vm.Row, e.KeyModifiers);
        if (DataContext is CircuitGridViewModel circuitVm)
            circuitVm.SelectedObject = vm;

        // Start drag immediately to allow range selection
        var dataSelection = new Tuple<int, RegisterRefModel>(vm.Column, vm.Row);
        var dragDataSelection = new DataObject();
        dragDataSelection.Set(typeof(Tuple<int, RegisterRefModel>).ToString(), dataSelection);

        try
        {
            DragDrop.DoDragDrop(e, dragDataSelection, DragDropEffects.Link);
        }
        catch (COMException ex)
        {
            Console.WriteLine(ex);
        }

        return;
    }

    private void ExecuteDrawingControlGate(Control source)
    {
        var button = source as Button;

        var coordinates = new Point(0, 0).Transform(button.TransformToVisual(drawing)
            .GetValueOrDefault()) / (DataContext as CircuitGridViewModel).ScaleFactor;

        const double diameter = 12;

        var centerX = coordinates.X + 0.5 * CircuitGridViewModel.GateWidth;
        var centerY = coordinates.Y + 0.5 * CircuitGridViewModel.QubitSize;

        var ctrlPoint = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Fill = _drawingColor,
            Stroke = _drawingColor,
            StrokeThickness = 1
        };

        // ctrlPoint.SetValue(DragDrop.AllowDropProperty, true); //AllowDrop = true;
        // ctrlPoint.AddHandler(DragDrop.DropEvent, ctrlPoint_Drop);

        drawing.Children.Add(ctrlPoint);
        Canvas.SetTop(ctrlPoint, centerY - 0.5 * diameter);
        Canvas.SetLeft(ctrlPoint, centerX - 0.5 * diameter);

        _line = new Line
        {
            StartPoint = new Point(centerX, centerY),
            EndPoint = new Point(centerX, centerY),
            Stroke = _drawingColor,
            StrokeThickness = 1
        };

        drawing.Children.Add(_line);
    }

    private void AttachPendingHandlers(Control source)
    {
        source.AddHandler(InputElement.PointerMovedEvent, Pending_PointerMoved, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        source.AddHandler(InputElement.PointerReleasedEvent, Pending_PointerReleased, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    private void DetachPendingHandlers()
    {
        if (_pendingSource is null)
            return;

        _pendingSource.RemoveHandler(InputElement.PointerMovedEvent, Pending_PointerMoved);
        _pendingSource.RemoveHandler(InputElement.PointerReleasedEvent, Pending_PointerReleased);
        _pendingSource = null;
    }

    private void ClearPending()
    {
        _pendingClick = false;
        _pendingVm = null;
        _pendingModifiers = KeyModifiers.None;
        _pressPoint = default;

        DetachPendingHandlers();
    }

    private void Pending_PointerMoved(object sender, PointerEventArgs e)
    {
        if (!_pendingClick || _pendingVm is null || _pendingSource is null)
            return;

        var curr = e.GetPosition(_pendingSource);
        var dx = curr.X - _pressPoint.X;
        var dy = curr.Y - _pressPoint.Y;
        if ((dx * dx + dy * dy) < DragThreshold * DragThreshold)
            return;

        // movement exceeded threshold -> start drag
        try
        {
            var data = new Tuple<int, RegisterRefModel>(_pendingVm.Column, _pendingVm.Row);
            var dragData = new DataObject();
            dragData.Set(typeof(Tuple<int, RegisterRefModel>).ToString(), data);
            _pendingClick = false; // this is a drag, not a click

            // Start drag from this move event (PointerEventArgs is acceptable)
            DragDrop.DoDragDrop(e, dragData, DragDropEffects.Link);
        }
        catch (COMException ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            ClearPending();
        }
    }

    private void Pending_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (!_pendingClick || _pendingVm is null)
        {
            ClearPending();
            return;
        }

        // No drag was started -> treat as click
        try
        {
            _pendingVm.SetGate(_pendingVm.Column, _pendingVm.Row, _pendingModifiers);
            // Select the cell/gate after click placement
            if (DataContext is CircuitGridViewModel circuitVM)
                circuitVM.SelectedObject = _pendingVm;
        }
        finally
        {
            ClearPending();
        }
    }

    private void ctrlPoint_Drop(object sender, PointerEventArgs pointerEventArgs) => CleanupDragVisual();

    private void Drawing_DragOver(object sender, DragEventArgs e)
    {
        if (_line == null)
            return;
        var scaleFactor = (DataContext as CircuitGridViewModel)?.ScaleFactor ?? 1.0;
        var offset = new Vector(-10, 4);
        var mouse = (e.GetPosition(drawing) + offset) / scaleFactor;
        _line.EndPoint = new Point(mouse.X, mouse.Y);
        e.Handled = true;
    }

    private void Drawing_Drop(object sender, DragEventArgs e) => CleanupDragVisual();

    private void CleanupDragVisual()
    {
        _line = null;
        drawing.Children.Clear();
        if (this.GetVisualRoot() is TopLevel top)
            top.Cursor = null;
    }

    private void GateButton_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.Contains(typeof(Tuple<int, RegisterRefModel>).ToString()))
        {
            e.DragEffects = DragDropEffects.Link; //All
            return;
        }

        e.DragEffects = DragDropEffects.None;
    }

    private void GateButton_DragOver(object sender, DragEventArgs e)
    {
        if (_line == null)
            return;

        var scaleFactor = (DataContext as CircuitGridViewModel).ScaleFactor;

        var offset = new Vector(-10, 4);

        var mouse = (e.GetPosition(drawing) + offset) / scaleFactor;

        _line.EndPoint = new Point(mouse.X, mouse.Y);
    }

    private void GateButton_Drop(object sender, DragEventArgs e)
    {
        var target = sender as Control;

        // check for Tuple<int, RegisterRefModel>
        var dataFormat = typeof(Tuple<int, RegisterRefModel>).ToString();

        if (!e.Data.Contains(dataFormat))
            return;

        var data = e.Data.Get(dataFormat) as Tuple<int, RegisterRefModel>;
        var vm = target.DataContext as GateViewModel;

        vm.SetGate(data.Item1, data.Item2, e.KeyModifiers);

        CleanupDragVisual();

        var circuitVM = DataContext as CircuitGridViewModel;
        circuitVM.SelectedObject = vm;
    }

    private void GatesScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        //TODO:
        // if (e.OffsetDelta.Y != 0) //e.VerticalChange != 0)
        // {
        //     RegisterScroll.ScrollToVerticalOffset(e.VerticalOffset);
        //     GatesScroll.ScrollToVerticalOffset = RegisterScroll.VerticalOffset;
        // }

        // if added step
        var extentWidthChange = e.ExtentDelta.X;
        if (!(extentWidthChange > 0))
            return;

        var circuitVM = DataContext as CircuitGridViewModel;
        var addedColumn = circuitVM.LastStepAdded;

        if (addedColumn <= 0)
            return;

        // if newly added step is not fully visible
        var scrollNeeded = extentWidthChange * (addedColumn + 1) - GatesScroll.Offset.X -
                           GatesScroll.Viewport.Width;
        if (scrollNeeded > 0)
        {
            //GatesScroll.ScrollToHorizontalOffset(GatesScroll.HorizontalOffset + scrollNeeded);
        }
    }

    private void RegisterScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        //GatesScroll.ScrollToVerticalOffset(e.VerticalOffset);
    }
}