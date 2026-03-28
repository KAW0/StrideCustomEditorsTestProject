using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.SceneInteractions;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewRegistry;
using Stride.Assets.Presentation.SceneEditor.Widgets;
using Stride.Assets.Presentation.ViewModel;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.CustomEditors.CustomTools;
using Stride.Editor.CustomEditors.SceneWidgets;
using Stride.Editor.CustomEditors.Services;
using Stride.Editor.CustomEditors.ViewModel;

namespace StrideCustomEditorsTestProject.Editor.SceneWidgets;

public abstract class TestSceneWidgetViewModelBase : SceneWidgetViewModel
{
    protected const string BoxPrefabUrl = "Prefabs/Box";
    protected const string FloorPrefabUrl = "Prefabs/Floor";
    protected const string SpherePrefabUrl = "Prefabs/Sphere";
    protected const string TorusPrefabUrl = "Prefabs/Torus";

    private string lastAction;
    private string activeSpawnPrefabUrl;
    private string activeSpawnPrefabDisplayName;

    public string LastAction
    {
        get => lastAction;
        protected set => SetValue(ref lastAction, value);
    }

    public string ActiveSpawnPrefabDisplayName => activeSpawnPrefabDisplayName;

    public string SceneViewLabel => $"Scene view: {Context.SceneViewId}";

    public int SelectedEntityCount => Editor.SelectedItems.Count;

    public bool DisplaySelectionMask
    {
        get => Editor.DisplaySelectionMask;
        set
        {
            if (Editor.DisplaySelectionMask == value)
                return;

            Editor.DisplaySelectionMask = value;
            OnPropertyChanged(nameof(DisplaySelectionMask));
            LastAction = $"Selection mask {(value ? "enabled" : "disabled")} for this scene view.";
        }
    }

    public bool MaterialSelectionMode
    {
        get => Editor.MaterialSelectionMode;
        set
        {
            if (Editor.MaterialSelectionMode == value)
                return;

            Editor.MaterialSelectionMode = value;
            OnPropertyChanged(nameof(MaterialSelectionMode));
            LastAction = $"Material selection mode {(value ? "enabled" : "disabled")} for this scene view.";
        }
    }

    public string SceneStatusText => Editor.CompilingAssets ? "Scene assets are compiling." : "Scene is ready.";

    public ICommandBase UseBoxPrefabCommand { get; }

    public ICommandBase UseFloorPrefabCommand { get; }

    public ICommandBase UseSpherePrefabCommand { get; }

    public ICommandBase UseTorusPrefabCommand { get; }

    public ICommandBase RefreshSceneStateCommand { get; }

    public ICommandBase SpawnActivePrefabCommand { get; }

    public ICommandBase DeleteSelectionCommand { get; }

    protected TestSceneWidgetViewModelBase(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        lastAction = "Ready.";
        activeSpawnPrefabUrl = BoxPrefabUrl;
        activeSpawnPrefabDisplayName = "Box";

        UseBoxPrefabCommand = new AnonymousCommand(serviceProvider, () => SetActiveSpawnPrefab(BoxPrefabUrl, "Box"));
        UseFloorPrefabCommand = new AnonymousCommand(serviceProvider, () => SetActiveSpawnPrefab(FloorPrefabUrl, "Floor"));
        UseSpherePrefabCommand = new AnonymousCommand(serviceProvider, () => SetActiveSpawnPrefab(SpherePrefabUrl, "Sphere"));
        UseTorusPrefabCommand = new AnonymousCommand(serviceProvider, () => SetActiveSpawnPrefab(TorusPrefabUrl, "Torus"));
        RefreshSceneStateCommand = new AnonymousCommand(serviceProvider, RefreshSceneState);
        SpawnActivePrefabCommand = new AnonymousTaskCommand(serviceProvider, () => SpawnPrefabInSceneAsync(activeSpawnPrefabUrl, activeSpawnPrefabDisplayName));
        DeleteSelectionCommand = new AnonymousTaskCommand(serviceProvider, DeleteSelectedEntitiesAsync);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RefreshSceneState();
    }

    protected void SetActiveSpawnPrefab(string prefabUrl, string prefabDisplayName)
    {
        activeSpawnPrefabUrl = prefabUrl;
        activeSpawnPrefabDisplayName = prefabDisplayName;
        OnPropertyChanged(nameof(ActiveSpawnPrefabDisplayName));
        LastAction = $"Active prefab changed to '{prefabDisplayName}'.";
    }

    protected void RefreshSceneState()
    {
        OnPropertyChanged(
            nameof(SceneViewLabel),
            nameof(SelectedEntityCount),
            nameof(DisplaySelectionMask),
            nameof(MaterialSelectionMode),
            nameof(SceneStatusText),
            nameof(ActiveSpawnPrefabDisplayName));
    }

    protected void ActivateSceneTool(string toolId, Action<SceneToolDefinition>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolId);

        if (ServiceProvider.TryGet<ICustomToolService>() is not { } toolService)
            return;

        var tool = new SceneToolDefinition(toolId)
        {
            Scope = SceneToolScope.ToolDefined,
            IsEnabledForView = context => string.Equals(context.ViewId, Context.SceneViewId, StringComparison.OrdinalIgnoreCase),
        };
        configure?.Invoke(tool);
        toolService.ActivateTool(WidgetId, tool);
    }

    protected bool IsSceneToolActive(string toolId)
    {
        return ServiceProvider.TryGet<ICustomToolService>()?.IsToolActive(WidgetId, toolId, Context.SceneViewId) ?? false;
    }

    protected bool DeactivateSceneTool(string toolId)
    {
        return ServiceProvider.TryGet<ICustomToolService>()?.DeactivateTool(WidgetId, toolId) ?? false;
    }

    protected SceneEditorViewRegistration? GetSceneViewRegistration()
    {
        return ServiceProvider.TryGet<ISceneEditorViewRegistry>()?.Views.FirstOrDefault(x => string.Equals(x.ViewId, Context.SceneViewId, StringComparison.OrdinalIgnoreCase));
    }

    protected Task SpawnPrefabInSceneAsync(string prefabUrl, string prefabDisplayName)
    {
        return Dispatcher.InvokeTask(async () =>
        {
            var session = SessionViewModel.Instance;
            if (session == null)
            {
                LastAction = "No active Stride session was found.";
                return;
            }

            var sceneView = GetSceneViewRegistration();
            var targetEditor = sceneView?.SceneEditor ?? Editor;

            var prefab = session.AllAssets.OfType<PrefabViewModel>().FirstOrDefault(x => MatchesUrl(x.Url?.ToString(), prefabUrl));
            if (prefab == null)
            {
                LastAction = $"Prefab '{prefabDisplayName}' was not found in the project.";
                return;
            }

            var children = new object[] { prefab };
            if (!((IAddChildViewModel)targetEditor).CanAddChildren(children, AddChildModifiers.None, out var message))
            {
                LastAction = $"Cannot spawn '{prefabDisplayName}': {message}";
                return;
            }

            ((IAddChildViewModel)targetEditor).AddChildren(children, AddChildModifiers.None);
            LastAction = $"Spawned '{prefabDisplayName}' in view '{Context.SceneViewId}'.";
            RefreshSceneState();
            await Task.CompletedTask;
        });
    }

    protected Task DeleteSelectedEntitiesAsync()
    {
        return Dispatcher.InvokeTask(async () =>
        {
            var sceneView = GetSceneViewRegistration();
            var targetEditor = sceneView?.SceneEditor ?? Editor;

            if (targetEditor.SelectedItems.Count == 0)
            {
                LastAction = "Select at least one entity in this scene view before deleting.";
                return;
            }

            var deletedCount = targetEditor.SelectedItems.Count;
            targetEditor.DeleteCommand.Execute();
            LastAction = $"Deleted {deletedCount} selected entr{(deletedCount == 1 ? "y" : "ies")} from view '{Context.SceneViewId}'.";
            RefreshSceneState();
            await Task.CompletedTask;
        });
    }

    public override IReadOnlyList<SceneWidgetMenuItem> GetMenuItems()
    {
        return
        [
            new SceneWidgetMenuItem("Use Box", UseBoxPrefabCommand),
            new SceneWidgetMenuItem("Use Floor", UseFloorPrefabCommand),
            new SceneWidgetMenuItem("Use Sphere", UseSpherePrefabCommand),
            new SceneWidgetMenuItem("Use Torus", UseTorusPrefabCommand),
            new SceneWidgetMenuItem("Refresh", RefreshSceneStateCommand),
        ];
    }

    protected static bool MatchesUrl(string? actualUrl, string expectedUrl)
    {
        if (string.IsNullOrWhiteSpace(actualUrl))
            return false;

        return actualUrl.Equals(expectedUrl, StringComparison.OrdinalIgnoreCase)
               || actualUrl.EndsWith(expectedUrl, StringComparison.OrdinalIgnoreCase)
               || actualUrl.EndsWith($"{expectedUrl}.sdprefab", StringComparison.OrdinalIgnoreCase)
               || actualUrl.EndsWith($"{expectedUrl}.sdscene", StringComparison.OrdinalIgnoreCase);
    }
}
