using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

// ReSharper disable once CheckNamespace
namespace GodotResourceGroups;

// ReSharper disable once Godot.MissingParameterlessConstructor
public partial class ResourceGroupBackgroundLoader : RefCounted {
    private readonly List<string> _open = [];
    private readonly Action<ResourceLoadingInfo> _callback;
    private bool _cancelled;
    private int _total;
    private int _finished;

    /// <summary>
    /// A loaded resource. This will be given as argument to the callback function.
    /// </summary>
    /// <param name="Success">Whether loading of this resource was successful</param>
    /// <param name="Path">The path from which the resource was loaded</param>
    /// <param name="Resource">The loaded resource. Is null when <see cref="Success"/> is false</param>
    /// <param name="Progress">The overall progress of the background loading, can be used to drive a progress indicator</param>
    /// <param name="Last">Whether this has been the last resource</param>
    public readonly record struct ResourceLoadingInfo(bool Success, string Path, Resource? Resource, float Progress,
        bool Last);

    /// <param name="paths">Paths to load</param>
    /// <param name="callback">Invoked for each loaded resource</param>
    public ResourceGroupBackgroundLoader(IEnumerable<string> paths, Action<ResourceLoadingInfo> callback) {
        _callback = callback;

        // Copy all into the open array and reverse the order so we can use the array as a stack.
        _open.AddRange(paths.Reverse());

        // Make note of the total amount of things.
        _total = _open.Count;

        // Start loading.
        CallDeferred(Next);
    }

    /// <summary>
    /// Cancels the currently running background loading process
    /// as soon as possible. If the process is already finished,
    /// nothing will happen. This function will immediately return.
    /// </summary>
    public void Cancel() {
        _cancelled = true;
    }

    /// <summary>
    /// Checks if the background loading process is done.
    /// </summary>
    public bool IsDone() => _open.Count == 0 || _cancelled;

    /// <summary>
    /// Looks like CallDeferred does not work properly when not in a node
    /// context (it doesn't seem to defer there...), so we roll our own here.
    /// </summary>
    private async ValueTask CallDeferred(Action action) {
        if (Engine.GetMainLoop() is not SceneTree sceneTree) {
            return;
        }

        await ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
        action();
    }

    private async ValueTask CallDeferred<T>(Action<T> action, T arg) {
        if (Engine.GetMainLoop() is not SceneTree sceneTree) {
            return;
        }

        await ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
        action(arg);
    }

    /// <summary>
    /// Starts the loading process for the next file in background.
    /// </summary>
    private void Next() {
        // If we're done, stop it here.
        if (IsDone()) {
            return;
        }

        // Fetch the next path and ask the resource loader to load it.
        var path = _open[^1];
        _open.RemoveAt(_open.Count - 1);
        var result = ResourceLoader.LoadThreadedRequest(path);
        if (result != Error.Ok) {
            GD.PushWarning($"Unable to load path {path}, return code {result}");
        }

        CallDeferred(Fetch, path);
    }

    /// <summary>
    /// Tries to fetch a file currently being loaded in background.
    /// </summary>
    private void Fetch(string path) {
        var status = ResourceLoader.LoadThreadedGetStatus(path);
        switch (status) {
            case ResourceLoader.ThreadLoadStatus.InvalidResource:
                GD.PushWarning($"Loading resource at {path} failed. Invalid resource. Ignoring and moving on.");
                Failed(path);
                break;

            case ResourceLoader.ThreadLoadStatus.Failed:
                GD.PushWarning($"Loading resource at {path} failed. Ignoring and moving on.");
                Failed(path);
                break;

            // If it's ready, ship it to the callback and move on.
            case ResourceLoader.ThreadLoadStatus.Loaded:
                var resource = ResourceLoader.LoadThreadedGet(path);
                Succeeded(path, resource);
                break;

            // We're still loading, so try again next frame.
            case ResourceLoader.ThreadLoadStatus.InProgress:
                CallDeferred(Fetch, path);
                break;
        }
    }

    // Called when a path loading ultimately failed. Informs the callback and moves on.
    private void Failed(string path) {
        _finished += 1;
        CallCallback(false, path, null);
        CallDeferred(Next);
    }

    /// <summary>
    /// Called when a path loading ultimately succeeded. Informs the callback and moves on.
    /// </summary>
    private void Succeeded(string path, Resource? resource) {
        _finished += 1;
        CallCallback(true, path, resource);
        CallDeferred(Next);
    }

    /// <summary>
    /// Called when an operation is finished. Will invoke the callback.
    /// </summary>
    private void CallCallback(bool success, string path, Resource? resource) {
        // Don't call the callback anymore if the process was canceled.
        if (_cancelled) {
            return;
        }

        var progress = _total == 0 ? 1.0f : (float)_finished / _total;
        var isLast = _open.Count == 0;
        var result = new ResourceLoadingInfo(success, path, resource, progress, isLast);
        _callback(result);
    }
}
