using System.Windows.Controls;
using Stride.Assets.Presentation.SceneEditor.Widgets;
using Stride.Editor.CustomEditors.Annotations;
using Stride.Editor.CustomEditors.SceneWidgets;

namespace StrideCustomEditorsTestProject.Editor.SceneWidgets;

[SceneWidget(typeof(TestSceneToolsWidgetViewModel), "Test Scene Tools", DefaultPlacement = SceneWidgetPlacement.Top, AllowMultiple = true)]
public partial class TestSceneToolsWidgetView : UserControl
{
    public TestSceneToolsWidgetView()
    {
        InitializeComponent();
    }
}
