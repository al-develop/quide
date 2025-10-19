#region

using System.Reflection;
using Avalonia.Controls;

#endregion

namespace QuIDE.Views.Dialog;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        this.txtVersion.Text = typeof(App).Assembly
            .GetCustomAttribute<AssemblyFileVersionAttribute>()?
            .Version;
    }
}