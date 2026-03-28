using System.Windows.Controls;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Editor.CustomEditors.Annotations;
using Stride.Editor.CustomEditors.Wpf.Views;

namespace StrideCustomEditorsTestProject.Editor;

[CustomEditor<IconBrowserEditorViewModel>("test.icon-browser", "Icon Browser")]
public partial class IconBrowserEditorView : CustomEditorView
{
    public IconBrowserEditorView()
    {
        InitializeComponent();
    }

    private void IconList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PreviewImage.Source = (sender as ListBox)?.SelectedItem is IconBrowserEditorViewModel.IconEntry selectedIcon
            ? selectedIcon.Image
            : null;
    }
}
