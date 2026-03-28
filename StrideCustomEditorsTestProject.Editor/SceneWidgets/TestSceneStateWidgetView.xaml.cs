using System.Windows.Controls;
using Stride.Assets.Presentation.SceneEditor.Widgets;
using Stride.Editor.CustomEditors.Annotations;
using Stride.Editor.CustomEditors.SceneWidgets;

namespace StrideCustomEditorsTestProject.Editor.SceneWidgets;

[SceneWidget(typeof(TestSceneStateWidgetViewModel), "Test Scene State", DefaultPlacement = SceneWidgetPlacement.Top, AllowMultiple = true)]
public partial class TestSceneStateWidgetView : UserControl
{
    public TestSceneStateWidgetView()
    {
        InitializeComponent();
    }
}
