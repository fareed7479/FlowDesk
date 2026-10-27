using System;
using System.IO;
using System.Linq;
using FlowDesk.Core;
using FlowDesk.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace FlowDesk.App;
public sealed class WorkspaceView : UserControl {
    private readonly WindowService service;
    private readonly Func<bool> blocked;
    private readonly WorkspaceController controller;
    private readonly Action<string> report;
    private readonly string path=Path.Combine(Program.DataDirectory,"library.json");
    private WorkspaceLibrary? library;
    private readonly ListView groups=new() {Height=180,SelectionMode=ListViewSelectionMode.Single};
    private readonly ListView members=new() {Height=220,SelectionMode=ListViewSelectionMode.Multiple};
    private readonly TextBox name=new() {Header="Workspace name",PlaceholderText="Design, Research, Writing…",MaxLength=64};
    private readonly TextBlock previewText=new() {TextWrapping=TextWrapping.Wrap};
    private RestorePlan? preview;
    public StageManager Stage {get;}
    public WorkspaceView(WindowService service,WorkspaceController controller,Action<string> report,Func<bool>? blocked=null) {
        this.blocked=blocked??(()=>false); this.service=service; this.controller=controller; this.report=report; Stage=new(service);
        var panel=new StackPanel {Padding=new Thickness(20),Spacing=12};
        panel.Children.Add(new TextBlock {Text="Workspaces & Stage Manager",FontSize=24});
        panel.Children.Add(new TextBlock {Text="Save selected windows as a named group. Stage switches minimize only windows from groups activated in this session. Leave Stage restores their original placement.",TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(groups);
        groups.SelectionChanged+=(_,_)=> {preview=null; previewText.Text=""; if(groups.SelectedItem is WorkspaceGroup g) name.Text=g.Name;};
        var groupActions=new StackPanel {Orientation=Orientation.Horizontal,Spacing=8};
        Button(groupActions,"Preview",()=>{
            preview=null; var plan=controller.Preview(Selected().Workspace); preview=plan;
            previewText.Text=$"{plan.Moves.Count} matched; {plan.Skipped} skipped.\n"+string.Join("\n",plan.Moves.Select(m=>$"{m.Window.Title} → {m.Target.Width} × {m.Target.Height}"));
        });
        Button(groupActions,"Apply restore",()=>{var plan=preview ?? throw new InvalidOperationException("Preview a group first."); preview=null; var r=controller.Apply(plan); report($"Restore requests: {r.Applied}; skipped: {r.Skipped}; errors: {r.Errors.Count}. "+string.Join(" ",r.Errors));});
        Button(groupActions,"Stage group",()=>{var g=Selected(); var r=Stage.Activate(g); report($"Stage {g.Name}: presented {r.Presented}, minimized {r.Minimized}, skipped {r.Skipped}. "+(r.FocusAccepted==false ? "Windows declined foreground activation. ":"")+string.Join(" ",r.Errors));});
        Button(groupActions,"Leave Stage",()=>{var r=Stage.Leave(); report($"Restored {r.Presented}; skipped {r.Skipped}. "+(r.FocusAccepted==false ? "Windows declined foreground activation. ":"")+string.Join(" ",r.Errors));});
        panel.Children.Add(groupActions); panel.Children.Add(previewText); panel.Children.Add(name);
        panel.Children.Add(new TextBlock {Text="Select the current windows to include in a new group or replacement:"});
        panel.Children.Add(members);
        var editActions=new StackPanel {Orientation=Orientation.Horizontal,Spacing=8};
        Button(editActions,"Refresh windows",()=>members.ItemsSource=service.Enumerate());
        Button(editActions,"Save new",()=>Save(false));
        Button(editActions,"Replace selected",()=>Save(true));
        Button(editActions,"Remove group",()=>{
            var group=Selected(); var dialog=new ContentDialog {Title="Remove workspace?",Content=$"Remove {group.Name} from the library? Its windows will stay open.",PrimaryButtonText="Remove",CloseButtonText="Cancel",XamlRoot=XamlRoot};
            ConfirmRemoval(dialog,group);
        });
        panel.Children.Add(editActions); Content=new ScrollViewer {Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        try {
            library=LibraryStore.Load(path);
            var legacy=Path.Combine(AppContext.BaseDirectory,"data","workspace.json");
            if(!File.Exists(path) && File.Exists(legacy)) { library=library.Upsert("Imported workspace",Workspaces.Load(legacy)); LibraryStore.Save(path,library); }
            Reload(); members.ItemsSource=service.Enumerate();
        } catch(Exception ex) { library=null; report("Workspace library unavailable; it will not be overwritten. "+ex.Message); }
    }
    private async void ConfirmRemoval(ContentDialog dialog,WorkspaceGroup group) {
        try { if(await dialog.ShowAsync()==ContentDialogResult.Primary) { var next=RequireLibrary().Remove(group.Id); LibraryStore.Save(path,next); library=next; Reload(); report("Workspace removed. Any active stage still needs Leave Stage."); } } catch(Exception ex) {report(ex.Message);}
    }
    private WorkspaceLibrary RequireLibrary() => library ?? throw new InvalidOperationException("Workspace library could not be loaded. Repair or back it up before saving.");
    private WorkspaceGroup Selected() => groups.SelectedItem as WorkspaceGroup ?? throw new InvalidOperationException("Select a saved workspace.");
    private void Reload() {groups.ItemsSource=RequireLibrary().Groups; preview=null; previewText.Text="";}
    private void Save(bool replace) {
        var chosen=members.SelectedItems.Cast<DesktopWindow>().ToArray();
        var current=service.Enumerate().Where(w=>chosen.Any(c=>c.Handle==w.Handle && c.ProcessId==w.ProcessId && c.ProcessPath==w.ProcessPath && c.Title==w.Title)).ToArray();
        if(current.Length==0) throw new InvalidOperationException("Select at least one current window.");
        if(current.Any(service.IsMinimized)) throw new InvalidOperationException("Restore minimized windows before saving a group layout.");
        var next=RequireLibrary().Upsert(name.Text,Workspaces.Capture(current),replace?Selected().Id:null);
        LibraryStore.Save(path,next); library=next; Reload(); report("Workspace library saved locally.");
    }
    private void Button(StackPanel panel,string text,Action action) {
        var button=new Button {Content=text}; button.Click+=(_,_)=>{try{if(blocked() && text is "Stage group" or "Apply restore")throw new InvalidOperationException("End focus/fullscreen before changing workspaces.");action();}catch(Exception ex){report(ex.Message);}}; panel.Children.Add(button);
    }
}
