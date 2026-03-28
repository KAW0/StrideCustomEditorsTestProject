using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.SceneInteractions;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewRegistry;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels;
using Stride.Assets.Presentation.ViewModel;
using Stride.Core;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.CustomEditors.CustomTools;
using Stride.Editor.CustomEditors.Models;
using Stride.Editor.CustomEditors.Services;
using Stride.Editor.CustomEditors.ViewModel;


namespace StrideCustomEditorsTestProject.Editor;

public class TestEditorViewModel : CustomEditorViewModel, IDestroyable
{
    private const string SelectToolId = "test.select";
    private const string FillToolId = "test.fill";
    private const string PaintPointToolId = "test.paint-point";
    private const string BoxPrefabUrl = "Prefabs/Box";
    private const string FloorPrefabUrl = "Prefabs/Floor";
    private const string SpherePrefabUrl = "Prefabs/Sphere";
    private const string TorusPrefabUrl = "Prefabs/Torus";

    private string spawnTag;
    private int toolActionCount;
    private string lastToolAction;
    private string activeSpawnPrefabUrl;
    private string activeSpawnPrefabDisplayName;

    public string SpawnTag
    {
        get => spawnTag;
        set => SetValue(ref spawnTag, value);
    }

    public int ToolActionCount
    {
        get => toolActionCount;
        private set => SetValue(ref toolActionCount, value, nameof(ToolActionCount), nameof(StatusText));
    }

    public string ActiveToolLabel
    {
        get
        {
            if (IsSelectToolActive) return "Spawn";
            if (IsFillToolActive) return "Delete";
            if (IsPaintPointToolActive) return "Paint Point";
            return "None";
        }
    }

    public string? ActiveToolId => GetToolIdForCurrentView();

    public string ActiveToolHint => GetToolHint(ActiveToolId, activeSpawnPrefabDisplayName);

    public string ActivePlacementPrefabLabel => $"Active spawn prefab: {activeSpawnPrefabDisplayName}";

    public string ActiveSceneViewLabel => GetActiveSceneContext() is { } activeScene
        ? $"Active scene view: {activeScene.ViewId}"
        : "Active scene view: none";

    public string StatusText => $"Actions executed: {ToolActionCount}. | Active tool in current view: {ActiveToolLabel}";

    public string LastToolAction
    {
        get => lastToolAction;
        private set => SetValue(ref lastToolAction, value);
    }

    public ICommand ToggleSelectToolCommand { get; }

    public ICommand ToggleFillToolCommand { get; }

    public ICommand TogglePaintPointToolCommand { get; }

    public ICommand ExecuteActiveToolCommand { get; }

    public bool IsSelectToolActive => IsCurrentViewToolActive(SelectToolId);

    public bool IsFillToolActive => IsCurrentViewToolActive(FillToolId);

    public bool IsPaintPointToolActive => IsCurrentViewToolActive(PaintPointToolId);

    public TestEditorViewModel(
        IViewModelServiceProvider serviceProvider,
        CustomEditorContext context)
        : base(serviceProvider, context)
    {
        spawnTag = "Custom tool";
        lastToolAction = "Choose a tool and use it in the active scene.";
        activeSpawnPrefabUrl = BoxPrefabUrl;
        activeSpawnPrefabDisplayName = "Box";

        ToggleSelectToolCommand = new AnonymousCommand(serviceProvider, () => ToggleTool(SelectToolId));
        ToggleFillToolCommand = new AnonymousCommand(serviceProvider, () => ToggleTool(FillToolId));
        TogglePaintPointToolCommand = new AnonymousCommand(serviceProvider, () => ToggleTool(PaintPointToolId));
        ExecuteActiveToolCommand = new AnonymousTaskCommand(serviceProvider, ExecuteActiveToolAsync, () => ActiveToolId != null);
        if (serviceProvider.TryGet<ICustomToolService>() is { } toolService)
            toolService.ActiveToolChanged += OnActiveToolChanged;

        if (serviceProvider.TryGet<ISceneEditorViewRegistry>() is { } sceneViewRegistry)
            sceneViewRegistry.ActiveViewChanged += OnActiveSceneViewChanged;
    }

    private void ToggleTool(string toolId)
    {
        var activeScene = GetActiveSceneContext();
        if (activeScene == null)
        {
            LastToolAction = "Open and focus a scene view before activating a tool.";
            RefreshToolState();
            return;
        }

        if (IsToolActive(toolId))
        {
            DeactivateTool(toolId);
            LastToolAction = $"Disabled {GetToolDisplayName(toolId)} for view '{activeScene.ViewId}'.";
        }
        else
        {
            ActivateTool(toolId, ConfigureToolDefinition);
            LastToolAction = $"Activated {GetToolDisplayName(toolId)} for view '{activeScene.ViewId}'.";
        }

        RefreshToolState();
    }

    private void OnActiveToolChanged(object? sender, CustomToolChangedEventArgs e)
    {
        RefreshToolState();
    }

    private void OnActiveSceneViewChanged(object? sender, SceneEditorViewChangedEventArgs e)
    {
        RefreshToolState();
    }

    private void RefreshToolState()
    {
        OnPropertyChanged(
            nameof(IsSelectToolActive),
            nameof(IsFillToolActive),
            nameof(IsPaintPointToolActive),
            nameof(ActiveToolId),
            nameof(ActiveToolLabel),
            nameof(ActiveToolHint),
            nameof(ActivePlacementPrefabLabel),
            nameof(ActiveSceneViewLabel),
            nameof(StatusText));
    }

    private void ConfigureToolDefinition(SceneToolDefinition tool)
    {
        var toolId = tool.Id;
        tool.DisplayName = GetToolDisplayName(toolId);
        tool.ExpandDefaultContextMenu = true;
        tool.GetContextMenuItems = _ => CreateSceneContextMenuItems(toolId);
        tool.CanHandleClick = clickContext => CanHandleSceneMouseClick(toolId, clickContext);
        tool.HandleClick = clickContext => HandleSceneMouseClick(toolId, clickContext);
        tool.IsDefaultInputEnabled = (_, input) => toolId == FillToolId && input == SceneEditorDefaultInput.LeftClickSelection;
    }

    private IEnumerable<SceneEditorContextMenuItem> CreateSceneContextMenuItems(string toolId)
    {
        if (toolId == SelectToolId)
        {
            yield return CreateSpawnSelectionMenuItem(BoxPrefabUrl, "Box");
            yield return CreateSpawnSelectionMenuItem(FloorPrefabUrl, "Floor");
            yield return CreateSpawnSelectionMenuItem(SpherePrefabUrl, "Sphere");
            yield return CreateSpawnSelectionMenuItem(TorusPrefabUrl, "Torus");
            yield break;
        }

        if (toolId == PaintPointToolId)
        {
            yield return new SceneEditorContextMenuItem("Spawn Sphere", new AnonymousTaskCommand(ServiceProvider, () => SpawnPrefabInSceneAsync(SpherePrefabUrl, "Sphere")));
            yield return new SceneEditorContextMenuItem("Spawn Torus", new AnonymousTaskCommand(ServiceProvider, () => SpawnPrefabInSceneAsync(TorusPrefabUrl, "Torus")));
        }
    }

    private SceneEditorContextMenuItem CreateSpawnSelectionMenuItem(string prefabUrl, string prefabDisplayName)
    {
        return new SceneEditorContextMenuItem(
            $"Use {prefabDisplayName}",
            new AnonymousCommand(ServiceProvider, () => SetActiveSpawnPrefab(prefabUrl, prefabDisplayName)));
    }

    private void SetActiveSpawnPrefab(string prefabUrl, string prefabDisplayName)
    {
        activeSpawnPrefabUrl = prefabUrl;
        activeSpawnPrefabDisplayName = prefabDisplayName;
        LastToolAction = $"Spawn tool will now place '{prefabDisplayName}'.";
        OnPropertyChanged(nameof(ActiveToolHint), nameof(ActivePlacementPrefabLabel));
    }

    private bool CanHandleSceneMouseClick(string toolId, SceneEditorClickContext clickContext)
    {
        var button = clickContext.Button;
        return toolId switch
        {
            SelectToolId => button == SceneEditorMouseButton.Left,
            FillToolId => button == SceneEditorMouseButton.Left,
            PaintPointToolId => button == SceneEditorMouseButton.Left,
            _ => false,
        };
    }

    private bool HandleSceneMouseClick(string toolId, SceneEditorClickContext clickContext)
    {
        var button = clickContext.Button;
        switch (toolId)
        {
            case SelectToolId:
                if (button == SceneEditorMouseButton.Left)
                {
                    _ = SpawnPrefabInSceneAsync(clickContext, activeSpawnPrefabUrl, activeSpawnPrefabDisplayName);
                    return true;
                }

                return false;

            case PaintPointToolId:
                if (button == SceneEditorMouseButton.Left)
                {
                    _ = SpawnPrefabInSceneAsync(clickContext, toolId);
                    return true;
                }

                return false;

            case FillToolId:
                if (button == SceneEditorMouseButton.Left)
                {
                    _ = DeleteSelectedEntitiesAsync(clickContext);
                    return true;
                }

                return false;

            default:
                return false;
        }
    }

    private Task ExecuteActiveToolAsync()
    {
        if (ActiveToolId == null)
            return Task.CompletedTask;

        return ActiveToolId switch
        {
            SelectToolId => SpawnPrefabInSceneAsync(activeSpawnPrefabUrl, activeSpawnPrefabDisplayName),
            FillToolId => DeleteSelectedEntitiesAsync(),
            _ => SpawnPrefabInSceneAsync(ActiveToolId),
        };
    }

    private Task SpawnPrefabInSceneAsync(string toolId)
    {
        return SpawnPrefabInSceneAsync(GetActiveSceneClickContext(), GetPrefabPath(toolId), GetPrefabDisplayName(toolId));
    }

    private Task SpawnPrefabInSceneAsync(string prefabUrl, string prefabDisplayName)
    {
        return SpawnPrefabInSceneAsync(GetActiveSceneClickContext(), prefabUrl, prefabDisplayName);
    }

    private Task SpawnPrefabInSceneAsync(SceneEditorClickContext? clickContext, string toolId)
    {
        return SpawnPrefabInSceneAsync(clickContext, GetPrefabPath(toolId), GetPrefabDisplayName(toolId));
    }

    private Task SpawnPrefabInSceneAsync(SceneEditorClickContext? clickContext, string prefabUrl, string prefabDisplayName)
    {
        return RunOnDispatcherTaskAsync(async () =>
        {
            var session = SessionViewModel.Instance;
            if (session == null)
            {
                LastToolAction = "No active Stride session was found.";
                return;
            }

            var prefab = session.AllAssets.OfType<PrefabViewModel>().FirstOrDefault(x => MatchesUrl(x.Url?.ToString(), prefabUrl));
            if (prefab == null)
            {
                LastToolAction = $"Prefab '{prefabDisplayName}' was not found in the project.";
                return;
            }

            var sceneEditor = clickContext?.SceneEditor;
            var sceneAsset = clickContext?.SceneAsset;
            if (sceneEditor == null || sceneAsset == null)
            {
                LastToolAction = "This tool requires a concrete scene view context.";
                return;
            }

            var children = new object[] { prefab };
            if (!((IAddChildViewModel)sceneEditor).CanAddChildren(children, AddChildModifiers.None, out var message))
            {
                LastToolAction = $"Cannot spawn '{prefabDisplayName}': {message}";
                return;
            }

            ((IAddChildViewModel)sceneEditor).AddChildren(children, AddChildModifiers.None);
            ToolActionCount++;
            var actorName = string.IsNullOrWhiteSpace(spawnTag) ? "Custom tool" : spawnTag.Trim();
            LastToolAction = $"{actorName} spawned '{prefabDisplayName}' in scene '{sceneAsset.Url}'.";
        });
    }

    private Task DeleteSelectedEntitiesAsync()
    {
        return DeleteSelectedEntitiesAsync(GetActiveSceneClickContext());
    }

    private Task DeleteSelectedEntitiesAsync(SceneEditorClickContext? clickContext)
    {
        return RunOnDispatcherTaskAsync(() =>
        {
            var sceneEditor = clickContext?.SceneEditor;
            var sceneAsset = clickContext?.SceneAsset;
            if (sceneEditor == null || sceneAsset == null)
            {
                LastToolAction = "This tool requires a concrete scene view context.";
                return Task.CompletedTask;
            }

            if (sceneEditor.SelectedItems.Count == 0)
            {
                LastToolAction = "Delete tool removes the current scene selection. Select an entity before clicking.";
                return Task.CompletedTask;
            }

            var deletedCount = sceneEditor.SelectedItems.Count;
            sceneEditor.DeleteCommand.Execute();
            ToolActionCount++;
            var actorName = string.IsNullOrWhiteSpace(spawnTag) ? "Custom tool" : spawnTag.Trim();
            LastToolAction = $"{actorName} deleted {deletedCount} selected entr{(deletedCount == 1 ? "y" : "ies")} from scene '{sceneAsset.Url}'.";
            return Task.CompletedTask;
        });
    }

    private static string GetToolDisplayName(string toolId)
    {
        if (toolId == SelectToolId)
            return "Spawn";
        if (toolId == FillToolId)
            return "Delete";
        if (toolId == PaintPointToolId)
            return "Paint Point";
        return toolId;
    }

    private static string GetToolHint(string? toolId, string activeSpawnPrefabDisplayName)
    {
        if (toolId == SelectToolId)
            return $"PPM selects the prefab to place. LPM places '{activeSpawnPrefabDisplayName}'.";
        if (toolId == FillToolId)
            return "LPM deletes the current scene selection.";
        if (toolId == PaintPointToolId)
            return "LPM spawns Sphere. PPM opens a shape menu.";
        return "Activate a tool to use it in the scene.";
    }

    private static string GetPrefabPath(string toolId)
    {
        if (toolId == SelectToolId)
            return BoxPrefabUrl;
        if (toolId == PaintPointToolId)
            return SpherePrefabUrl;
        return toolId;
    }

    private static string GetPrefabDisplayName(string? toolId)
    {
        if (toolId == SelectToolId)
            return "Box";
        if (toolId == PaintPointToolId)
            return "Sphere";
        if (string.IsNullOrWhiteSpace(toolId))
            return "None";
        return toolId;
    }

    private SceneEditorClickContext? GetActiveSceneClickContext()
    {
        var activeScene = GetActiveSceneContext();
        return activeScene == null ? null : new SceneEditorClickContext(SceneEditorMouseButton.Left, activeScene.View);
    }

    private bool IsCurrentViewToolActive(string toolId)
    {
        var activeScene = GetActiveSceneContext();
        return activeScene != null && IsToolActive(toolId, activeScene.ViewId);
    }

    private string? GetToolIdForCurrentView()
    {
        var activeScene = GetActiveSceneContext();
        var activeTool = activeScene == null
            ? null
            : ServiceProvider.TryGet<ICustomToolService>()?.GetActiveTool(activeScene.ViewId);
        return activeTool?.Definition.Id;
    }

    private static bool MatchesUrl(string? actualUrl, string expectedUrl)
    {
        if (string.IsNullOrWhiteSpace(actualUrl))
            return false;

        return actualUrl.Equals(expectedUrl, StringComparison.OrdinalIgnoreCase)
               || actualUrl.EndsWith(expectedUrl, StringComparison.OrdinalIgnoreCase)
               || actualUrl.EndsWith($"{expectedUrl}.sdprefab", StringComparison.OrdinalIgnoreCase)
               || actualUrl.EndsWith($"{expectedUrl}.sdscene", StringComparison.OrdinalIgnoreCase);
    }

    public override void Destroy()
    {
        if (ServiceProvider.TryGet<ICustomToolService>() is { } toolService)
            toolService.ActiveToolChanged -= OnActiveToolChanged;

        if (ServiceProvider.TryGet<ISceneEditorViewRegistry>() is { } sceneViewRegistry)
            sceneViewRegistry.ActiveViewChanged -= OnActiveSceneViewChanged;

        base.Destroy();
    }
}
