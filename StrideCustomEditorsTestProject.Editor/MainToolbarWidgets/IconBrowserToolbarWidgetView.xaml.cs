using Stride.Editor.CustomEditors.Annotations;
using Stride.Editor.CustomEditors.Wpf.Views;

namespace StrideCustomEditorsTestProject.Editor.MainToolbarWidgets;

[MainToolbarWidget<IconBrowserToolbarWidgetViewModel>("test.icon-browser.toolbar", "Icon Browser")]
public partial class IconBrowserToolbarWidgetView : MainToolbarWidgetView
{
    public IconBrowserToolbarWidgetView()
    {
        InitializeComponent();
    }
}
