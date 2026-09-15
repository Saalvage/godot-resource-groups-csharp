#if TOOLS
using System;
using System.Collections.Generic;
using Godot;

// ReSharper disable once CheckNamespace
namespace GodotResourceGroups;

/// <summary>
/// The resource scanner scans the project for resources matching the given definition and returns a list of matching
/// resources.
/// </summary>
public class ResourceScanner(ResourceGroup group, EditorFileSystem fileSystem) {
    private readonly PathVerifier _verifier
        = new(group.BaseFolder, group.Includes, group.Excludes);

    /// <summary>
    /// Scans the whole project for resources that match the group definition.
    /// </summary>
    public List<string> Scan() {
        List<string> result = [];
        var folder = group.BaseFolder;

        if (folder == "") {
            GD.PushWarning(
                $"In resource group '{group.ResourcePath}': Base folder is not set. Resource group will be empty.");
            return result;
        }

        var root = fileSystem.GetFilesystemPath(folder);
        if (root == null) {
            GD.PushWarning(
                $"In resource group '{group.ResourcePath}': Base folder '{folder}' does not exist. Resource group will be empty.");
            return result;
        }

        Scan(root, result);
        return result;
    }

    private void Scan(EditorFileSystemDirectory folder, List<string> results) {
        // Get all files in the folder.
        // For each file first check if it matches the group definition, before trying to load it.
        for (var i = 0; i < folder.GetFileCount(); i++) {
            var fullName = folder.GetFilePath(i);
            if (MatchesGroupDefinition(fullName)) {
                if (ResourceLoader.Exists(fullName)) {
                    results.Add(fullName);
                } else {
                    GD.PushWarning(
                        $"In resource group '{group.ResourcePath}': File '{fullName}' exists, but is not a supported Godot resource. It will be ignored.");
                }
            }
        }

        // Recurse into subfolders.
        for (var j = 0; j < folder.GetSubdirCount(); j++) {
            Scan(folder.GetSubdir(j), results);
        }
    }

    private bool MatchesGroupDefinition(string file) {
        // Skip import files.
        if (file.EndsWith(".import", StringComparison.Ordinal)) {
            return false;
        }

        return _verifier.Matches(file);
    }
}

#endif // TOOLS
