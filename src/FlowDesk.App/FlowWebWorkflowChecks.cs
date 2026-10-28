using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlowDesk.FlowWeb;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
namespace FlowDesk.App;
internal static class FlowWebWorkflowChecks {
    private static readonly List<string> passed=new();
    private static string Report=>Path.Combine(Program.ProjectRoot(),".cache","flowweb-desktop-result.json");
    private static void State(string stage,string? error=null){File.WriteAllText(Report+".tmp",JsonSerializer.Serialize(new{stage,passed,error}));File.Move(Report+".tmp",Report,true);}
    private static void Check(bool value,string label){if(!value)throw new InvalidOperationException(label);passed.Add(label);State("running");}
    private static async Task Wait(Func<bool> value,int seconds=40){for(int i=0;i<seconds*10;i++){if(value())return;await Task.Delay(100);}throw new TimeoutException("FlowWeb browser workflow timed out.");}
    private static async Task Click(Button button){((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();await Task.Delay(200);}
    private static Button? FindButton(DependencyObject root,string label){if(root is Button b && b.Content?.ToString()==label)return b;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var found=FindButton(VisualTreeHelper.GetChild(root,i),label);if(found is not null)return found;}return null;}
    internal static async Task Run(MainWindow window) {
        var view=window.WebPanel;
        try {
            window.Tabs.SelectedIndex=6;view.Sharing.IsOn=true;State("ready");
            await Wait(()=>view.Bridge.Sessions.Length==1 && view.Bridge.Sessions[0].Tabs.Length==2);
            view.Render();Check(view.Browsers.Items.Count==1 && view.Tabs.Items.Count==2,"FlowDesk UI receives the extension's real browser tabs");
            WebTab First()=>view.Bridge.Sessions[0].Tabs.OrderBy(t=>t.Id).First();
            void SelectFirst(){view.Render();view.Tabs.SelectedItems.Clear();view.Tabs.SelectedItems.Add(((System.Collections.IEnumerable)view.Tabs.ItemsSource).Cast<WebTab>().Single(t=>t.Id==First().Id));}
            SelectFirst();await Click(view.Buttons["Toggle mute"]);await Wait(()=>First().Muted);Check(First().Muted,"desktop mute control reaches the browser and returns a snapshot");
            SelectFirst();await Click(view.Buttons["Toggle pin"]);await Wait(()=>First().Pinned);Check(First().Pinned,"desktop pin control reaches the browser");
            SelectFirst();await Click(view.Buttons["Activate tab"]);await Wait(()=>First().Active);Check(First().Active,"desktop activate control reaches the browser");
            view.Render();view.Tabs.SelectedItems.Clear();foreach(var tab in view.Bridge.Sessions[0].Tabs)view.Tabs.SelectedItems.Add(((System.Collections.IEnumerable)view.Tabs.ItemsSource).Cast<WebTab>().Single(t=>t.Id==tab.Id));
            view.WorkspaceName.Text="FlowWeb fixture "+Guid.NewGuid().ToString("N")[..8];await Click(view.Buttons["Save selected tabs"]);
            var saved=view.Library!.Workspaces.Last();Check(saved.Urls.Length==2,"desktop browser workspace saves selected URLs");view.Workspaces.SelectedItem=saved;
            await Click(view.Buttons["Open workspace"]);await Wait(()=>view.PendingDialog?.IsLoaded==true);var primary=FindButton(view.PendingDialog!,"Open tabs")??throw new InvalidOperationException("Workspace confirmation was not rendered.");await Click(primary);
            await Wait(()=>view.Bridge.Sessions[0].Tabs.Length==4);Check(view.Bridge.Sessions[0].Tabs.Length==4,"confirmed browser workspace restore opens new tabs");State("awaiting-disconnect");
            await Wait(()=>view.Bridge.Sessions.Length==0);view.Render();Check(view.Tabs.Items.Count==0,"browser disconnect removes tab metadata from the desktop UI");
            view.Sharing.IsOn=false;Check(!view.Bridge.Enabled,"desktop bridge can be disabled after disconnect");State("complete");Environment.ExitCode=0;window.Close();
        }catch(Exception ex){State("failed",ex.ToString());Environment.ExitCode=1;window.Close();}
    }
}
