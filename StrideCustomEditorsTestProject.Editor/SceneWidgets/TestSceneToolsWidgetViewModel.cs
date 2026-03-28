using System.Collections.Generic;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.SceneInteractions;
using Stride.Assets.Presentation.SceneEditor.Widgets;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.CustomEditors.CustomTools;

namespace StrideCustomEditorsTestProject.Editor.SceneWidgets;

public sealed class TestSceneToolsWidgetViewModel : TestSceneWidgetViewModelBase
{
    private const string SpawnToolId = "test.widget.spawn";
    private const string DeleteToolId = "test.widget.delete";
    private const string PaintPointToolId = "test.widget.paint-point";

    public override string DisplayName { get; set; } = "Test Scene Tools";

    public ICommandBase ToggleSpawnToolCommand { get; }

    public ICommandBase ToggleDeleteToolCommand { get; }

    public ICommandBase TogglePaintPointToolCommand { get; }

    public ICommandBase ExecuteActiveToolCommand { get; }

    public ICommandBase ClearWidgetToolsCommand { get; }

    public bool IsSpawnToolActive => IsSceneToolActive(SpawnToolId);

    public bool IsDeleteToolActive => IsSceneToolActive(DeleteToolId);

    public bool IsPaintPointToolActive => IsSceneToolActive(PaintPointToolId);

    public string? ActiveToolId
    {
        get
        {
            if (IsSpawnToolActive) return SpawnToolId;
            if (IsDeleteToolActive) return DeleteToolId;
            if (IsPaintPointToolActive) return PaintPointToolId;
            return null;
        }
    }

    public string ActiveToolLabel => ActiveToolId switch
    {
        SpawnToolId => "Spawn",
        DeleteToolId => "Delete",
        PaintPointToolId => "Paint Point",
        _ => "None",
    };

    public string ActiveToolHint => ActiveToolId switch
    {
        SpawnToolId => $"LPM places '{ActiveSpawnPrefabDisplayName}' in this scene view.",
        DeleteToolId => "LPM deletes the current selection in this scene view.",
        PaintPointToolId => "LPM spawns Sphere in this scene view.",
        _ => "Choose a tool to use it in this scene view.",
    };

    public TestSceneToolsWidgetViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        ToggleSpawnToolCommand = new AnonymousCommand(serviceProvider, () => ToggleTool(SpawnToolId));
        ToggleDeleteToolCommand = new AnonymousCommand(serviceProvider, () => ToggleTool(DeleteToolId));
        TogglePaintPointToolCommand = new AnonymousCommand(serviceProvider, () => ToggleTool(PaintPointToolId));
        ExecuteActiveToolCommand = new AnonymousTaskCommand(serviceProvider, ExecuteActiveToolAsync, () => ActiveToolId is not null);
        ClearWidgetToolsCommand = new AnonymousCommand(serviceProvider, ClearWidgetTools);
    }

    private void ToggleTool(string toolId)
    {
        if (IsSceneToolActive(toolId))
        {
            DeactivateSceneTool(toolId);
            LastAction = $"Disabled {GetToolDisplayName(toolId)} for view '{Context.SceneViewId}'.";
        }
        else
        {
            ActivateSceneTool(toolId, tool => ConfigureTool(toolId, tool));
            LastAction = $"Activated {GetToolDisplayName(toolId)} for view '{Context.SceneViewId}'.";
        }

        RefreshToolState();
    }

    private void ConfigureTool(string toolId, SceneToolDefinition tool)
    {
        tool.DisplayName = GetToolDisplayName(toolId);
        tool.ExpandDefaultContextMenu = true;
        tool.GetContextMenuItems = _ => CreateSceneContextMenuItems(toolId);
        tool.CanHandleClick = clickContext => CanHandleClick(toolId, clickContext);
        tool.HandleClick = clickContext => HandleClick(toolId, clickContext);
        tool.IsDefaultInputEnabled = (_, input) => toolId == DeleteToolId && input == SceneEditorDefaultInput.LeftClickSelection;
    }

    private IEnumerable<SceneEditorContextMenuItem> CreateSceneContextMenuItems(string toolId)
    {
        if (toolId == SpawnToolId)
        {
            yield return new SceneEditorContextMenuItem("Use Box", UseBoxPrefabCommand);
            yield return new SceneEditorContextMenuItem("Use Floor", UseFloorPrefabCommand);
            yield return new SceneEditorContextMenuItem("Use Sphere", UseSpherePrefabCommand);
            yield return new SceneEditorContextMenuItem("Use Torus", UseTorusPrefabCommand);
            yield break;
        }

        if (toolId == PaintPointToolId)
        {
            yield return new SceneEditorContextMenuItem("Spawn Sphere", new AnonymousTaskCommand(ServiceProvider, () => SpawnPrefabInSceneAsync(SpherePrefabUrl, "Sphere")));
            yield return new SceneEditorContextMenuItem("Spawn Torus", new AnonymousTaskCommand(ServiceProvider, () => SpawnPrefabInSceneAsync(TorusPrefabUrl, "Torus")));
        }
    }

    private bool CanHandleClick(string toolId, SceneEditorClickContext clickContext)
    {
        if (!string.Equals(clickContext.ViewId, Context.SceneViewId, System.StringComparison.OrdinalIgnoreCase))
            return false;

        return toolId switch
        {
            SpawnToolId => clickContext.Button == SceneEditorMouseButton.Left,
            DeleteToolId => clickContext.Button == SceneEditorMouseButton.Left,
            PaintPointToolId => clickContext.Button == SceneEditorMouseButton.Left,
            _ => false,
        };
    }

    private bool HandleClick(string toolId, SceneEditorClickContext clickContext)
    {
        switch (toolId)
        {
            case SpawnToolId when clickContext.Button == SceneEditorMouseButton.Left:
                _ = SpawnPrefabInSceneAsync(ActiveSpawnPrefabDisplayName switch
                {
                    "Floor" => FloorPrefabUrl,
                    "Sphere" => SpherePrefabUrl,
                    "Torus" => TorusPrefabUrl,
                    _ => BoxPrefabUrl,
                }, ActiveSpawnPrefabDisplayName);
                return true;

            case DeleteToolId when clickContext.Button == SceneEditorMouseButton.Left:
                _ = DeleteSelectedEntitiesAsync();
                return true;

            case PaintPointToolId when clickContext.Button == SceneEditorMouseButton.Left:
                _ = SpawnPrefabInSceneAsync(SpherePrefabUrl, "Sphere");
                return true;

            default:
                return false;
        }
    }

    private System.Threading.Tasks.Task ExecuteActiveToolAsync()
    {
        return ActiveToolId switch
        {
            SpawnToolId => SpawnPrefabInSceneAsync(ActiveSpawnPrefabDisplayName switch
            {
                "Floor" => FloorPrefabUrl,
                "Sphere" => SpherePrefabUrl,
                "Torus" => TorusPrefabUrl,
                _ => BoxPrefabUrl,
            }, ActiveSpawnPrefabDisplayName),
            DeleteToolId => DeleteSelectedEntitiesAsync(),
            PaintPointToolId => SpawnPrefabInSceneAsync(SpherePrefabUrl, "Sphere"),
            _ => System.Threading.Tasks.Task.CompletedTask,
        };
    }

    private void ClearWidgetTools()
    {
        DeactivateSceneTool(SpawnToolId);
        DeactivateSceneTool(DeleteToolId);
        DeactivateSceneTool(PaintPointToolId);
        LastAction = $"Cleared widget tools for view '{Context.SceneViewId}'.";
        RefreshToolState();
    }

    public override IReadOnlyList<SceneWidgetMenuItem> GetMenuItems()
    {
        var items = new List<SceneWidgetMenuItem>(base.GetMenuItems())
        {
            new("Clear Widget Tools", ClearWidgetToolsCommand),
        };
        return items;
    }

    private void RefreshToolState()
    {
        ExecuteActiveToolCommand.IsEnabled = ActiveToolId is not null;
        RefreshSceneState();
        OnPropertyChanged(nameof(IsSpawnToolActive), nameof(IsDeleteToolActive), nameof(IsPaintPointToolActive), nameof(ActiveToolId), nameof(ActiveToolLabel), nameof(ActiveToolHint));
    }

    private static string GetToolDisplayName(string toolId)
    {
        return toolId switch
        {
            SpawnToolId => "Spawn",
            DeleteToolId => "Delete",
            PaintPointToolId => "Paint Point",
            _ => toolId,
        };
    }
}

