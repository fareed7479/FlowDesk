using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlowDesk.Core;
using FlowDesk.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FlowDesk.App.Hybrid;

/// <summary>Runnable native host initialization. Pass a built local frontend directory;
/// the scene library and StageManager belong to this window's single owner.</summary>
public sealed class HybridWorkspaceWindow : Window
{
    private readonly StageManager stages;
    private readonly InfoBar status = new() { Severity = InfoBarSeverity.Error, IsClosable = false };
    private readonly TaskCompletionSource initialization = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool started;
    public HybridWebViewHost Host { get; }
    public Task Initialization => initialization.Task;
    public string? LastCloseError { get; private set; }

    public HybridWorkspaceWindow(string webBuildDirectory, string dataDirectory, WindowService service,
        StageManager stages, Func<IReadOnlyList<WorkspaceGroup>> scenes, Func<bool> mutationsBlocked)
    {
        this.stages = stages;
        Title = "FlowDesk · Hybrid workspace";
        AppWindow.Resize(new(1240, 900));
        Host = new(this, webBuildDirectory, Path.Combine(dataDirectory, "WebView2"), service, stages, scenes, mutationsBlocked);
        var root = new Grid(); root.RowDefinitions.Add(new() { Height = new(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        root.Children.Add(Host); Grid.SetRow(status, 1); root.Children.Add(status); Content = root;
        Host.Loaded += Loaded;
        AppWindow.Closing += Closing;
        Closed += (_, _) =>
        {
            try
            {
                if (Host.Bridge?.IsBusy != true && stages.IsActive)
                {
                    var restored = stages.Leave();
                    if (stages.IsActive) LastCloseError = string.Join(" ", restored.Errors);
                }
                else if (Host.Bridge?.IsBusy == true) LastCloseError = "An interrupted operation retains its journal for startup recovery.";
            }
            catch (Exception ex) { LastCloseError = ex.Message; }
            finally { Host.Bridge?.Dispose(); Host.Dispose(); }
        };
    }

    private async void Loaded(object sender, RoutedEventArgs e)
    {
        if (started) return; started = true;
        try
        {
            if (stages.IsActive)
            {
                var recovered = await Task.Run(stages.Leave);
                if (stages.IsActive) throw new InvalidOperationException("Restore the existing stage journal before opening a new session. " + string.Join(" ", recovered.Errors));
            }
            await Host.InitializeAsync(); initialization.TrySetResult();
        }
        catch (Exception ex)
        {
            status.Title = "Workspace could not start"; status.Message = ex.Message; status.IsOpen = true;
            initialization.TrySetException(ex);
        }
    }

    // WinUI Window.Close() can bypass AppWindow.Closing. Programmatic closes must
    // run the same recovery guard as the native caption's close request.
    public new void Close() { if (PrepareClose()) base.Close(); }

    private void Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs e) => e.Cancel = !PrepareClose();

    private bool PrepareClose()
    {
        if (!initialization.Task.IsCompleted || Host.Bridge?.IsBusy == true)
        {
            status.Title = "Finishing the current operation";
            status.Message = "Close the workspace after initialization or the native command finishes."; status.IsOpen = true; return false;
        }
        try
        {
            if (stages.IsActive)
            {
                var restored = stages.Leave();
                if (stages.IsActive) throw new InvalidOperationException("Some staged windows still need recovery. " + string.Join(" ", restored.Errors));
            }
            Host.Dispose(); return true;
        }
        catch (Exception ex)
        {
            LastCloseError = ex.Message;
            status.Title = "Workspace remains open"; status.Message = ex.Message; status.IsOpen = true; return false;
        }
    }
}
