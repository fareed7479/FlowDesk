using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowDesk.Core;
using FlowDesk.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace FlowDesk.App;
public sealed class DesktopOverviewView : UserControl {
    private readonly WindowService service;
    private readonly Action<string> report;
    private IReadOnlyList<DesktopWindow> inventory=Array.Empty<DesktopWindow>();
    private readonly TextBox search=new() {PlaceholderText="Search window titles or app names",Header="Mission Control • window overview"};
    private readonly ListView overview=new() {Height=240,SelectionMode=ListViewSelectionMode.Single,IsItemClickEnabled=true};
    private readonly StackPanel dock=new() {Orientation=Orientation.Horizontal,Spacing=8};
    private readonly Dictionary<string,int> cycle=new(StringComparer.OrdinalIgnoreCase);
    public DesktopOverviewView(WindowService service,Action<string> report) {
        this.service=service; this.report=report;
        var panel=new StackPanel {Padding=new Thickness(20),Spacing=12};
        panel.Children.Add(new TextBlock {Text="Mission Control",FontSize=24});
        panel.Children.Add(new TextBlock {Text="Search and activate current windows. This first overview uses window titles; live thumbnails and virtual desktops come later.",TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(search); panel.Children.Add(overview);
        search.TextChanged+=(_,_)=>Filter();
        overview.ItemClick+=(_,e)=>Activate((DesktopWindow)e.ClickedItem);
        var refresh=new Button {Content="Refresh overview & dock"}; refresh.Click+=(_,_)=>Refresh(); panel.Children.Add(refresh);
        panel.Children.Add(new TextBlock {Text="FlowDock • running apps",FontSize=22});
        panel.Children.Add(new TextBlock {Text="Click an app to cycle through its windows. Windows may decline foreground activation.",TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(new ScrollViewer {Content=dock,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollMode=ScrollMode.Enabled,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled});
        Content=new ScrollViewer {Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto}; Loaded+=(_,_)=>Refresh();
    }
    private void Activate(DesktopWindow window) {
        try {report(service.Activate(window)?"Window activated.":"Windows declined foreground activation. Use the taskbar to select the app.");} catch(Exception ex) {report(ex.Message);}
    }
    private void Filter() {var term=search.Text.Trim(); overview.ItemsSource=inventory.Where(w=>w.Title.Contains(term,StringComparison.OrdinalIgnoreCase) || Path.GetFileNameWithoutExtension(w.ProcessPath).Contains(term,StringComparison.OrdinalIgnoreCase)).ToArray();}
    private void Refresh() {
        try {
            inventory=service.Enumerate(); Filter(); dock.Children.Clear();
            foreach(var group in inventory.GroupBy(w=>w.ProcessPath,StringComparer.OrdinalIgnoreCase).OrderBy(g=>Path.GetFileNameWithoutExtension(g.Key))) {
                string path=group.Key;
                var button=new Button {Content=$"{Path.GetFileNameWithoutExtension(path)} ({group.Count()})"};
                button.Click+=(_,_)=>{
                    try {
                        var current=service.Enumerate().Where(w=>StringComparer.OrdinalIgnoreCase.Equals(w.ProcessPath,path)).OrderBy(w=>w.Handle).ToArray();
                        if(current.Length==0) {report("No windows remain for this app. Refresh the dock.");return;}
                        int index=cycle.GetValueOrDefault(path)%current.Length; cycle[path]=index+1; Activate(current[index]);
                    } catch(Exception ex) {report(ex.Message);}
                }; dock.Children.Add(button);
            }
        } catch(Exception ex) {report(ex.Message);}
    }
}
