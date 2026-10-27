using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlowDesk.Core;
using FlowDesk.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
namespace FlowDesk.App;
internal static class AdvancedWorkflowChecks {
    internal static async Task Run(MainWindow window) {
        var passed=new List<string>();var processes=new List<Process>();var service=new WindowService();
        string result=Path.Combine(Program.ProjectRoot(),".cache","advanced-workflow-results.json");
        void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);passed.Add(text);}
        async Task Wait(Func<bool> predicate){for(int i=0;i<100;i++){if(predicate())return;await Task.Delay(100);}throw new TimeoutException("Advanced UI did not reach the expected state.");}
        async Task Click(Button b){((IInvokeProvider)new ButtonAutomationPeer(b).GetPattern(PatternInterface.Invoke)).Invoke();await Task.Delay(300);}
        try {
            for(int i=0;i<8;i++){window.Tabs.SelectedIndex=i;await Wait(()=>window.Navigation.Content is FrameworkElement p && p.IsLoaded);Check(window.Navigation.Content is FrameworkElement p && p.IsLoaded,$"navigation renders page {i+1}");}
            var view=window.OverviewPanel;window.Tabs.SelectedIndex=2;
            string prefix="FlowDesk advanced UI "+Guid.NewGuid().ToString("N");
            view.Search.Text=prefix;await view.Refresh();Check(view.VisibleCount==0,"overview shows an empty result before fixtures exist");
            for(int i=0;i<2;i++){
                string fixture=Path.Combine(Program.ProjectRoot(),"tests","FlowDesk.Checks","bin","Debug","net10.0-windows","FlowDesk.Checks.exe");
                var start=new ProcessStartInfo(fixture){UseShellExecute=false};start.Environment["DOTNET_ROOT_X64"]=Path.Combine(Program.ProjectRoot(),".tools","dotnet");start.ArgumentList.Add("--fixture");start.ArgumentList.Add(prefix+i);processes.Add(Process.Start(start)!);
            }
            await Wait(()=>view.VisibleCount==2);Check(view.VisibleCount==2,"native inventory events refresh the visible overview without pressing Refresh");
            await Wait(()=>view.RegisteredPreviews>=1);Check(view.RegisteredPreviews>=1,"visible window cards register real DWM thumbnail relationships");
            var fixtures=service.Enumerate().Where(w=>w.Title.StartsWith(prefix,StringComparison.Ordinal)).OrderBy(w=>w.Title).ToArray();
            await Click(view.Buttons["Select "+fixtures[0].Handle]);view.Monitor.SelectedItem=view.Monitor.Items.OfType<DesktopDisplay>().First();
            var before=service.CaptureState(fixtures[0]);await Click(view.Buttons["Move selected to display"]);
            Check(view.Selected?.Handle==fixtures[0].Handle,"window card selects the native fixture for a display move");
            await Click(view.Buttons["Undo display move"]);Check(service.CaptureState(fixtures[0]).NormalBounds==before.NormalBounds,"display move UI undo restores fixture bounds");
            await view.Refresh();view.Search.Text=prefix;
            await Click(view.Buttons["Pin "+fixtures[0].Handle]);
            Check(DockPreferences.Load(Path.Combine(Program.DataDirectory,"dock.json")).Pins.Contains(fixtures[0].ProcessPath),"window card pins the app into persistent dock preferences");
            await Click(view.Buttons["Show floating dock"]);Check(view.Dock is not null && view.Dock.AppWindow.Size.Height>8,"floating dock opens as an actual owned Windows window");
            view.AutoHide.IsOn=true;
            await Wait(()=>view.Dock?.IsCollapsed==true);Check(view.Dock!.IsCollapsed,"floating dock automatically collapses after idle pointer/focus");
            await Click(view.Buttons["Hide floating dock"]);Check(view.Dock is null,"hide dock closes the floating window cleanly");
            view.Pins.SelectedItem=view.Pins.Items.Cast<string>().Single(p=>p==fixtures[0].ProcessPath);await Click(view.Buttons["Unpin selected app"]);Check(!DockPreferences.Load(Path.Combine(Program.DataDirectory,"dock.json")).Pins.Contains(fixtures[0].ProcessPath),"pinned-app management removes a saved pin independently of running cards");
            var ws=window.WorkspacePanel;window.Tabs.SelectedIndex=1;await Task.Delay(200);
            await Click(ws.Buttons["Refresh windows"]);
            var inventory=((System.Collections.IEnumerable)ws.members.ItemsSource).Cast<DesktopWindow>().ToArray();
            ws.members.SelectedItems.Clear();ws.members.SelectedItems.Add(inventory.Single(w=>w.Handle==fixtures[0].Handle));ws.name.Text="Advanced UI group";await Click(ws.Buttons["Save new"]);
            Check(ws.groups.Items.Count>0,"workspace UI saves a named window group");
            ws.groups.SelectedItem=ws.groups.Items.Cast<WorkspaceGroup>().Single(g=>g.Name=="Advanced UI group");
            await Click(ws.Buttons["Preview"]);await Click(ws.Buttons["Stage group"]);
            Check(ws.Stage.IsActive && File.Exists(Path.Combine(Program.DataDirectory,"stage-recovery.json")),"Stage UI writes its recovery journal before staging");
            await Click(ws.Buttons["Leave Stage"]);Check(!ws.Stage.IsActive,"Leave Stage UI restores the managed fixture");
            window.Tabs.SelectedIndex=7;await Task.Delay(200);var appearance=(AppearanceView)window.Navigation.Content;
            appearance.Theme.SelectedIndex=2;Check(((FrameworkElement)window.Content).RequestedTheme==ElementTheme.Dark && AppData.LoadTheme()==ElementTheme.Dark,"dark appearance updates and persists");
            appearance.Theme.SelectedIndex=1;Check(((FrameworkElement)window.Content).RequestedTheme==ElementTheme.Light && AppData.LoadTheme()==ElementTheme.Light,"light appearance updates and persists");
            appearance.Theme.SelectedIndex=0;Check(((FrameworkElement)window.Content).RequestedTheme==ElementTheme.Default,"appearance returns to the Windows setting");
            window.Tabs.SelectedIndex=2;await view.Refresh();view.Search.Text=prefix;
            processes[1].CloseMainWindow();await processes[1].WaitForExitAsync();await Wait(()=>view.VisibleCount==1);Check(view.VisibleCount==1,"native destroy events remove closed fixtures from overview");
            File.WriteAllText(result,JsonSerializer.Serialize(new {Success=true,Passed=passed},new JsonSerializerOptions{WriteIndented=true}));Environment.ExitCode=0;
        }catch(Exception ex){File.WriteAllText(result,JsonSerializer.Serialize(new{Success=false,Passed=passed,Error=ex.ToString()},new JsonSerializerOptions{WriteIndented=true}));Environment.ExitCode=1;}
        finally {
            window.WorkspacePanel.Stage.Leave();
            window.OverviewPanel.Dispose();
            foreach(var process in processes){if(!process.HasExited){process.CloseMainWindow();if(!process.WaitForExit(3000))process.Kill();}process.Dispose();}
            window.Close();
        }
    }
}
