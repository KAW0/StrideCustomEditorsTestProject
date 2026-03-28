using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace StrideCustomEditorsTestProject.Editor.SceneWidgets;

public sealed class TestSceneStateWidgetViewModel : TestSceneWidgetViewModelBase
{
    public override string DisplayName { get; set; } = "Test Scene State";

    public ICommandBase SpawnTorusCommand { get; }

    public ICommandBase SpawnSphereCommand { get; }

    public TestSceneStateWidgetViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        SpawnTorusCommand = new AnonymousTaskCommand(serviceProvider, () => SpawnPrefabInSceneAsync(TorusPrefabUrl, "Torus"));
        SpawnSphereCommand = new AnonymousTaskCommand(serviceProvider, () => SpawnPrefabInSceneAsync(SpherePrefabUrl, "Sphere"));
    }
}
