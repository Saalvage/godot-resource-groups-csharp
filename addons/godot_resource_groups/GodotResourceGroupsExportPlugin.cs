#if TOOLS
using System;
using Godot;

// ReSharper disable once CheckNamespace
namespace GodotResourceGroups;

[Tool]
public partial class GodotResourceGroupsExportPlugin(Action? onExport) : EditorExportPlugin {
    public GodotResourceGroupsExportPlugin() : this(null) { }

    public override void _ExportBegin(string[] features, bool isDebug, string path, uint flags) {
        onExport?.Invoke();
    }

    public override string _GetName() => "Godot Resource Groups Export Plugin";
}

#endif // TOOLS
