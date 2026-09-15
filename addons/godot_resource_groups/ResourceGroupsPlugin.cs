#if TOOLS
using Godot;

// ReSharper disable once CheckNamespace
namespace GodotResourceGroups;

public static class ResourceGroupsPlugin {
    /// <summary>
    /// Rebuilds all resource groups. Useful if you have custom editor scripts that create or modify resources
    /// and want to update the resource groups via script.
    /// </summary>
    public static void RebuildResourceGroups() {
        if (GodotResourceGroups.Instance == null) {
            GD.PushError("Resource group plugin is not active, please check if it is enabled in the project settings.");
            return;
        }

        GodotResourceGroups.Instance.RebuildResourceGroups();
    }
}
#endif // TOOLS
