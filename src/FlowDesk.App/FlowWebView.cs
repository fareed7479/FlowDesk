using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlowDesk.FlowWeb;
using FlowDesk.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace FlowDesk.App;
internal sealed class FlowWebView : UserControl,IDisposable {
    internal readonly FlowWebBridge Bridge;
    internal readonly ToggleSwitch Sharing=new(){Header="Enable FlowWeb bridge",OffContent="Off",OnContent="On"};
    internal readonly ComboBox Browsers=new(){Header="Connected browser",HorizontalAlignment=HorizontalAlignment.Stretch};
    internal readonly ListView Tabs=new(){Height=200,SelectionMode=ListViewSelectionMode.Multiple};
    internal readonly ListView Workspaces=new(){Height=100,SelectionMode=ListViewSelectionMode.Single};
    internal readonly TextBox WorkspaceName=new(){Header="Browser workspace name",MaxLength=64};
    internal readonly Dictionary<string,Button> Buttons=new();
    internal ContentDialog? PendingDialog;
    internal WebLibrary? Library;
    private readonly string libraryPath;
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap};
    private readonly TextBlock connectionStatus=new(){TextWrapping=TextWrapping.Wrap};
    private readonly Func<bool> modeActive;
    private bool disposing,rendering;
    public FlowWebView(Func<bool> modeActive) {
        this.modeActive=modeActive;libraryPath=Path.Combine(Program.DataDirectory,"flowweb-workspaces.json");Bridge=new(Path.Combine(Program.DataDirectory,"flowweb-bridge.json"));
        var panel=new StackPanel{Spacing=10,Padding=new Thickness(20)};
        panel.Children.Add(new TextBlock{Text="FlowWeb",FontSize=26});
        panel.Children.Add(new TextBlock{Text="Enable the local bridge, then connect from the FlowWeb extension. Connected web-tab metadata stays in memory until you explicitly save a browser workspace.",TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(Sharing);panel.Children.Add(Browsers);panel.Children.Add(Tabs);
        Sharing.Toggled+=(_,_)=>{try{if(Sharing.IsOn)Bridge.Enable();else Bridge.Disable();Render();}catch(Exception ex){status.Text=ex.Message;Sharing.IsOn=false;}};
        Browsers.SelectionChanged+=(_,_)=>{if(!rendering){Tabs.SelectedItems.Clear();RenderTabs();}};
        var actions=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        Add(actions,"Activate tab",async()=>await Send(new Command("activate",SelectedTab().Id)));
        Add(actions,"Toggle mute",async()=>{var tab=SelectedTab();await Send(new Command("mute",tab.Id,!tab.Muted));});
        Add(actions,"Toggle pin",async()=>{var tab=SelectedTab();await Send(new Command("pin",tab.Id,!tab.Pinned));});
        panel.Children.Add(actions);panel.Children.Add(connectionStatus);panel.Children.Add(status);
        panel.Children.Add(new TextBlock{Text="Browser workspaces",FontSize=22});panel.Children.Add(new TextBlock{Text="Saved workspaces contain URLs in local plaintext. Restoring opens a new browser window with up to 25 HTTP/HTTPS tabs.",TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(WorkspaceName);panel.Children.Add(Workspaces);
        var savedActions=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        Add(savedActions,"Save selected tabs",()=>{
            var selected=Tabs.SelectedItems.Cast<WebTab>().ToArray();if(selected.Length==0)selected=SelectedBrowser().Tabs;
            var next=(Library??throw new InvalidOperationException("Browser workspace storage is unavailable.")).Add(WorkspaceName.Text,selected.Select(t=>t.Url));next.Save(libraryPath);Library=next;RenderLibrary();status.Text="Browser workspace saved locally.";return Task.CompletedTask;
        });
        Add(savedActions,"Open workspace",async()=>{
            var workspace=Workspaces.SelectedItem as WebWorkspace??throw new InvalidOperationException("Select a browser workspace.");
            var session=SelectedBrowser().Id;
            PendingDialog=new ContentDialog{Title="Open browser workspace?",Content=$"Open {workspace.Urls.Length} saved web tabs in a new {SelectedBrowser().Browser} window?",PrimaryButtonText="Open tabs",CloseButtonText="Cancel",XamlRoot=XamlRoot};
            var result=await PendingDialog.ShowAsync();PendingDialog=null;if(result==ContentDialogResult.Primary){var answer=await Bridge.Send(session,new Command("openWorkspace",Urls:workspace.Urls));status.Text=answer.Ok?"Browser workspace opened.":answer.Error;}
        });
        Add(savedActions,"Remove workspace",async()=>{
            var workspace=Workspaces.SelectedItem as WebWorkspace??throw new InvalidOperationException("Select a browser workspace.");
            var dialog=new ContentDialog{Title="Remove browser workspace?",Content=$"Remove {workspace.Name} from the local library?",PrimaryButtonText="Remove",CloseButtonText="Cancel",XamlRoot=XamlRoot};
            if(await dialog.ShowAsync()==ContentDialogResult.Primary){var next=Library!.Remove(workspace.Id);next.Save(libraryPath);Library=next;RenderLibrary();}
        });
        panel.Children.Add(savedActions);Content=Design.Page(panel,"Bring your browser into the flow","Connect intentionally, control shared tabs, and save the web workspaces you want to return to.","FLOWWEB");
        Bridge.Changed+=()=>DispatcherQueue.TryEnqueue(()=>{if(!disposing)Render();});
        try{Library=WebLibrary.Load(libraryPath);RenderLibrary();}catch(Exception ex){status.Text="Browser library is preserved and unavailable: "+ex.Message;}
        Render();
    }
    private BrowserSession SelectedBrowser()=>Browsers.SelectedItem as BrowserSession??throw new InvalidOperationException("Connect and select a browser.");
    private WebTab SelectedTab()=>Tabs.SelectedItems.Count==1?(WebTab)Tabs.SelectedItems[0]:throw new InvalidOperationException("Select exactly one shared tab.");
    private async Task Send(Command command){var result=await Bridge.Send(SelectedBrowser().Id,command);status.Text=result.Ok?"Browser command completed.":result.Error;}
    private void RenderLibrary(){var id=(Workspaces.SelectedItem as WebWorkspace)?.Id;Workspaces.ItemsSource=Library!.Workspaces;if(id.HasValue)Workspaces.SelectedItem=Library.Workspaces.SingleOrDefault(w=>w.Id==id);}
    internal void Render(){
        var selected=(Browsers.SelectedItem as BrowserSession)?.Id;var selectedTabs=Tabs.SelectedItems.Cast<WebTab>().Select(t=>t.Id).ToHashSet();var sessions=Bridge.Sessions;
        rendering=true;try{Browsers.ItemsSource=sessions;Browsers.SelectedItem=sessions.FirstOrDefault(s=>s.Id==selected)??sessions.FirstOrDefault();if((Browsers.SelectedItem as BrowserSession)?.Id!=selected)selectedTabs.Clear();Tabs.ItemsSource=(Browsers.SelectedItem as BrowserSession)?.Tabs??Array.Empty<WebTab>();foreach(var tab in ((System.Collections.IEnumerable)Tabs.ItemsSource).Cast<WebTab>())if(selectedTabs.Contains(tab.Id))Tabs.SelectedItems.Add(tab);}finally{rendering=false;}
        connectionStatus.Text=Bridge.Enabled?$"Bridge enabled · {sessions.Length} browser connection(s). Share at most 50 web tabs per connection.":"Bridge disabled. Enable it here, then connect in the extension.";
    }
    private void RenderTabs(){var ids=Tabs.SelectedItems.Cast<WebTab>().Select(t=>t.Id).ToHashSet();Tabs.ItemsSource=(Browsers.SelectedItem as BrowserSession)?.Tabs??Array.Empty<WebTab>();foreach(var t in ((System.Collections.IEnumerable)Tabs.ItemsSource).Cast<WebTab>())if(ids.Contains(t.Id))Tabs.SelectedItems.Add(t);}
    private void Add(StackPanel panel,string label,Func<Task> action){var b=new Button{Content=label};b.Click+=async(_,_)=>{try{if(modeActive() && label is "Activate tab" or "Open workspace")throw new InvalidOperationException("End focus/fullscreen before changing browser focus.");await action();}catch(Exception ex){status.Text=ex.Message;}};Buttons[label]=b;panel.Children.Add(b);}
    public void Dispose(){disposing=true;Bridge.Dispose();}
}
