#if TOOLS
using System.Diagnostics;
using System.Linq;
using Godot;

// ReSharper disable once CheckNamespace
namespace GodotResourceGroups;

[Tool]
public partial class GodotResourceGroups : EditorPlugin {
    private const string REBUILD_SETTING = "godot_resource_groups/auto_rebuild";

    private ResourceGroupScanner GroupScanner => field ??= new(EditorInterface.Singleton.GetResourceFilesystem());
    
    private GodotResourceGroupsExportPlugin _exportPlugin = null!;

    public static GodotResourceGroups? Instance { get; private set; }

    public override void _EnterTree() {
        Instance = this;

        AddToolMenuItem("Rebuild project resource groups", Callable.From(RebuildResourceGroups));

        // Try to get the setting, if it doesn't exist, set it to true.
        var autoRebuild = ProjectSettings.GetSetting(REBUILD_SETTING, true).AsBool();

        // Make sure it is there.
        ProjectSettings.SetSetting(REBUILD_SETTING, autoRebuild);
        // Add property info so it shows up in the editor.
        ProjectSettings.AddPropertyInfo(new() {
            { "name", REBUILD_SETTING },
            { "description", "Automatically rebuild resource groups when the project is built." },
            { "type", (int)Variant.Type.Bool },
        });

        // Register the export plugin.
        _exportPlugin = new(RebuildResourceGroups);
        AddExportPlugin(_exportPlugin);
    }

    public override void _ExitTree() {
        Instance = null;
        RemoveToolMenuItem("Rebuild project resource groups");
        RemoveExportPlugin(_exportPlugin);
    }

    public override bool _Build() {
        // Always read the setting to make sure it is up to date.
        var autoRebuild = ProjectSettings.GetSetting(REBUILD_SETTING, true).AsBool();
        if (autoRebuild) {
            RebuildResourceGroups();
        }

        return true;
    }

    public void RebuildResourceGroups() {
        var timer = new Stopwatch();
        timer.Start();
        
        GroupScanner.Scan()
            .AsParallel()
            .ForAll(resourceGroup => {
                var resourceScanner = new ResourceScanner(resourceGroup, EditorInterface.Singleton.GetResourceFilesystem());
                var resourcePaths = resourceScanner.Scan();

                resourceGroup.Paths = [..resourcePaths];

                ResourceSaver.Save(resourceGroup);
                EditorInterface.Singleton.GetResourceFilesystem().UpdateFile(resourceGroup.ResourcePath);
            });

        timer.Stop();
        GD.Print($"Rebuilt resource groups in {timer.Elapsed.TotalSeconds:0.00} seconds.");
    }
}

#endif // TOOLS