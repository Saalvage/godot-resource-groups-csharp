#if TOOLS
using System;
using Godot;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace GodotResourceGroups;

/// <summary>
/// The resource group scanner finds all resource groups currently in the project.
/// </summary>
public class ResourceGroupScanner(EditorFileSystem fileSystem) {
    private static readonly StringName Resource = "Resource";
    
    /// <summary>
    /// Scans the whole project for resources that match the group definition.
    /// </summary>
    public List<ResourceGroup> Scan() {
        List<ResourceGroup> result = [];
        ScanFolder(fileSystem.GetFilesystem(), result);
        return result;
    }

    private void ScanFolder(EditorFileSystemDirectory folder, List<ResourceGroup> results) {
        // Get all files in the folder.
        for (var i = 0; i < folder.GetFileCount(); i++) {
            if (folder.GetFileType(i) == Resource) {
                var path = folder.GetFilePath(i);
                if (path.EndsWith(".tres", StringComparison.Ordinal)) {
                    var resource = ResourceLoader.Load(path);
                    if (resource is ResourceGroup resourceGroup) {
                        results.Add(resourceGroup);
                    }
                }
            }
        }

        // For each file first check if it matches the group definition, before trying to load it.
        for (var j = 0; j < folder.GetSubdirCount(); j++) {
            ScanFolder(folder.GetSubdir(j), results);
        }
    }
}

#endif // TOOLS
