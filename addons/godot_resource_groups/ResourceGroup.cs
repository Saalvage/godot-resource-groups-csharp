using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace GodotResourceGroups;

/// <summary>
/// A resource group is a set of resources in the project that you want to access at runtime.
/// </summary>
[Tool]
[GlobalClass]
[Icon("resource_group.svg")]
public partial class ResourceGroup : Resource {
    /// <summary>
    /// The base folder for locating files in this resource group.
    /// </summary>
    [Export(PropertyHint.Dir)]
    public string BaseFolder { get; set; } = "";

    /// <summary>
    /// Files that should be included. Can contain ant-style wildcards:
    /// ** - matches zero or more characters (including "/")
    /// * - matches zero or more characters (excluding "/")
    /// ? - matches one character
    /// </summary>
    [Export]
    public string[] Includes { get; set; } = [];

    /// <summary>
    /// Files which should be excluded. Is applied after the include filter.
    /// Can also contain ant-style wildcards.
    /// </summary>
    [Export]
    public string[] Excludes { get; set; } = [];

    /// <summary>
    /// The paths of the project that match this resource group.
    /// </summary>
    [Export]
    public string[] Paths { get; set; } = [];

    /// <summary>
    /// Enumerates all resources in this resource group.
    /// </summary>
    public IEnumerable<Resource> EnumerateAll()
        => Paths.Select(x => ResourceLoader.Load(x));

    /// <summary>
    /// Enumerates all resources in this resource group. Validates that all resources are of the specified type.
    /// If an item is not of the required type, an error will be printed and the item is skipped.
    /// </summary>
    public IEnumerable<T> EnumerateAll<T>() where T : class
        => EnumerateAll()
            .Where(ValidateResourceType<T>)
            .Cast<T>();

    /// <summary>
    /// Loads all resources in this resource group and returns them.
    /// </summary>
    public List<Resource> LoadAll() {
        return EnumerateAll().ToList();
    }

    /// <summary>
    /// Loads all resources and stores them into the given array. Allows
    /// to load resources into typed arrays for better type safety. If
    /// the item is not of the required type, an error will be printed and
    /// the item is skipped.
    /// </summary>
    public void LoadAllInto(ICollection<Resource> destination) {
        foreach (var resource in EnumerateAll()) {
            destination.Add(resource);
        }
    }

    /// <summary>
    /// Loads all resources and stores them into the given array. Allows
    /// to load resources into typed arrays for better type safety. If
    /// the item is not of the required type, an error will be printed and
    /// the item is skipped.
    /// </summary>
    public void LoadAllInto<T>(ICollection<T> destination) where T : class {
        foreach (var item in EnumerateAll<T>()) {
            destination.Add(item);
        }
    }

    /// <summary>
    /// Gets all paths of resources inside of this resource group that
    /// match the given include and exclude criteria.
    /// </summary>
    public IEnumerable<string> GetMatchingPaths(IEnumerable<string> includePatterns, IEnumerable<string> excludePatterns) {
        var pathVerifier = new PathVerifier(BaseFolder, includePatterns, excludePatterns);
        return Paths.Where(pathVerifier.Matches);
    }

    /// <summary>
    /// Enumerates all resources in this resource group that match the given include and exclude criteria.
    /// </summary>
    public IEnumerable<Resource> EnumerateMatching(IEnumerable<string> includePatterns,
        IEnumerable<string> excludePatterns)
        => GetMatchingPaths(includePatterns, excludePatterns)
            .Select(x => ResourceLoader.Load(x));
    
    /// <summary>
    /// Enumerates all resources in this resource group that match the given include and exclude criteria.
    /// Validates that all resources are of the specified type. If an item is not of the required type,
    /// an error will be printed and the item is skipped.
    /// </summary>
    public IEnumerable<T> EnumerateMatching<T>(IEnumerable<string> includePatterns,
        IEnumerable<string> excludePatterns) where T : class
        => EnumerateMatching(includePatterns, excludePatterns)
            .Where(ValidateResourceType<T>)
            .Cast<T>();
    
    /// <summary>
    /// Loads all resources in this resource group that match the given include and exclude criteria.
    /// </summary>
    public List<Resource> LoadMatching(IEnumerable<string> includePatterns, IEnumerable<string> excludePatterns)
        => EnumerateMatching(includePatterns, excludePatterns).ToList();

    /// <summary>
    /// Loads all resources in this resource group that match the given
    /// include and exclude criteria and stores them into the given array.
    /// Allows to load resources into typed arrays for better type safety. If
    /// the item is not of the required type, an error will be printed and
    /// the item is skipped.
    /// </summary>
    public void LoadMatchingInto(ICollection<Resource> destination, IEnumerable<string> includePatterns,
        IEnumerable<string> excludePatterns) {
        foreach (var path in EnumerateMatching(includePatterns, excludePatterns)) {
            destination.Add(path);
        }
    }

    /// <summary>
    /// Loads all resources in this resource group that match the given
    /// include and exclude criteria and stores them into the given array.
    /// Allows to load resources into typed arrays for better type safety. If
    /// the item is not of the required type, an error will be printed and
    /// the item is skipped.
    /// </summary>
    public void LoadMatchingInto<T>(ICollection<T> destination, IEnumerable<string> includePatterns,
        IEnumerable<string> excludePatterns) where T : class {
        var items = LoadMatching(includePatterns, excludePatterns);
        foreach (var item in items) {
            if (item is T casted) {
                destination.Add(casted);
            } else {
                GD.PushError($"Item {item} is not of required type {typeof(T).Name}. Skipping.");
            }
        }
    }

    /// <summary>
    /// Loads all resources in this resource group in background. Returns
    /// a ResourceGroupBackgroundLoader object which can be used to check the
    /// status and collect the results. Will call the onResourceLoaded callable for each
    /// loaded resource.
    /// </summary>
    public ResourceGroupBackgroundLoader LoadAllInBackground(
        Action<ResourceGroupBackgroundLoader.ResourceLoadingInfo> onResourceLoaded)
        => new(Paths, onResourceLoaded);

    /// <summary>
    /// Loads all resources in this resource group that match the given
    /// include and exclude criteria in background. ResourceGroupBackgroundLoader object which can be used to check the
    /// status and collect the results. Will call the onResourceLoaded callable for each
    /// loaded resource.
    /// </summary>
    public ResourceGroupBackgroundLoader LoadMatchingInBackground(IEnumerable<string> includePatterns,
        IEnumerable<string> excludePatterns, Action<ResourceGroupBackgroundLoader.ResourceLoadingInfo> onResourceLoaded)
        => new(GetMatchingPaths(includePatterns, excludePatterns), onResourceLoaded);

    private static bool ValidateResourceType<T>(Resource resource) {
        if (resource is T) {
            return true;
        }
        
        GD.PushError($"Resource {resource.ResourcePath} is not of required type {typeof(T).Name}. Skipping.");
        return false;
    }
}
