using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlowDesk.Core;
using FlowDesk.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
namespace FlowDesk.App;
public sealed class WorkspaceView : UserControl,IDisposable {
    private readonly WindowService service;
    private readonly Func<bool> blocked;
    private readonly WorkspaceController controller;
    private readonly Action<string> report;
    private readonly string path=Path.Combine(Program.DataDirectory,"library.json");
    private WorkspaceLibrary? library;
    internal readonly ListView groups=new(){MaxHeight=360,SelectionMode=ListViewSelectionMode.Single};
    internal readonly ListView members=new(){Height=220,SelectionMode=ListViewSelectionMode.Multiple};
    internal readonly TextBox name=new(){Header="Workspace name",PlaceholderText="Research, Writing, Design…",MaxLength=64};
    private readonly TextBlock previewText=Design.Caption("");
    private readonly TextBlock empty=Design.Caption("No workspaces yet. Select your windows below and give the first one a name.");
    private readonly TextBlock selectedTitle=Design.Heading("Choose a workspace",24);
    private readonly TextBlock stateText=Design.Caption("Preview a group before switching.");
    private readonly StackPanel stagePreviews=new(){Spacing=12};
    private readonly Expander editor=new(){Header="Create or edit workspace",HorizontalAlignment=HorizontalAlignment.Stretch};
    private RestorePlan? preview;
    private StageStrip? strip;
    private bool disposed;
    internal bool IsBusy {get;private set;}
    internal StageStrip? Strip=>strip;
    public StageManager Stage {get;}
    internal readonly Dictionary<string,Button> Buttons=new();
    internal WorkspaceGroup[] Groups=>library?.Groups??Array.Empty<WorkspaceGroup>();
    internal double StageScale {get {var group=groups.SelectedItem as WorkspaceGroup;var candidate=group is null?null:Stage.Preview(group).Moves.FirstOrDefault()?.Window;return candidate is null?1:Math.Max(.75,service.Dpi(candidate)/96d);}}
    internal IReadOnlyList<DesktopWindow> Inventory()=>service.Enumerate();
    internal void StageHint(string message)=>report(message);
    internal Rect StageWorkArea() {
        var group=groups.SelectedItem as WorkspaceGroup;var first=group is null?null:Stage.Preview(group).Moves.FirstOrDefault()?.Window;
        return service.WorkArea(first?.Handle??0);
    }
    private Rect Canvas() {var area=StageWorkArea();int inset=(int)(232*StageScale)+36;return area.Width>inset+320?new(area.X+inset,area.Y+12,area.Width-inset-12,Math.Max(1,area.Height-24)):area;}
    public WorkspaceView(WindowService service,WorkspaceController controller,Action<string> report,Func<bool>? blocked=null) {
        this.service=service;this.controller=controller;this.report=report;this.blocked=blocked??(()=>false);
        Stage=new(service,Path.Combine(Program.DataDirectory,"stage-recovery.json"));
        if(Stage.IsActive){var r=Stage.Leave();report($"Stage recovery: restored {r.Presented}; skipped {r.Skipped}. "+string.Join(" ",r.Errors));}
        var panel=new StackPanel{Spacing=20};
        panel.Children.Add(Design.Heading("Workspaces"));panel.Children.Add(Design.Caption(""));
        var createBar=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        Button(createBar,"New workspace",()=>{groups.SelectedItem=null;members.SelectedItems.Clear();name.Text="";editor.IsExpanded=true;name.Focus(FocusState.Programmatic);});
        Button(createBar,"Show stage strip",ShowStrip);
        panel.Children.Add(createBar);panel.Children.Add(empty);
        groups.ItemTemplate=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><StackPanel Padding='8' Spacing='4'><TextBlock Text='{Binding Name}' FontSize='18' FontWeight='SemiBold'/><TextBlock Text='{Binding Workspace.Windows.Length}' FontSize='12' Opacity='0.65'/></StackPanel></DataTemplate>");
        panel.Children.Add(groups);
        groups.SelectionChanged+=(_,_)=>{preview=null;previewText.Text="";RenderSelection();};
        var detail=new StackPanel{Spacing=16};detail.Children.Add(selectedTitle);detail.Children.Add(stateText);detail.Children.Add(stagePreviews);
        var stageActions=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        Button(stageActions,"Stage group",()=>SwitchStage(Selected()));Design.Primary(Buttons["Stage group"]);
        Button(stageActions,"Preview",()=>{preview=Stage.Preview(Selected());previewText.Text=$"{preview.Moves.Count} available; {preview.Skipped} skipped. No windows have moved.";RenderStagePreview(preview);Buttons["Apply restore"].IsEnabled=!Stage.IsActive;});
        Button(stageActions,"Leave Stage",()=>LeaveStage());
        detail.Children.Add(stageActions);detail.Children.Add(previewText);
        var more=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        Button(more,"Apply restore",()=>{if(Stage.IsActive)throw new InvalidOperationException("Leave Stage before applying an independent layout.");var plan=preview??throw new InvalidOperationException("Preview a group first.");var r=controller.Apply(plan);preview=null;report($"Restored {r.Applied}; skipped {r.Skipped}. "+string.Join(" ",r.Errors));});
        Button(more,"Edit members",EditMembers);
        Button(more,"Remove group",async()=>{var group=Selected();if(Stage.IsActive)throw new InvalidOperationException("Leave Stage before removing a workspace.");var dialog=new ContentDialog{Title="Remove workspace?",Content=$"Remove {group.Name}? Its windows stay open.",PrimaryButtonText="Remove",CloseButtonText="Cancel",XamlRoot=XamlRoot};if(await dialog.ShowAsync()==ContentDialogResult.Primary){var next=RequireLibrary().Remove(group.Id);LibraryStore.Save(path,next);library=next;Reload();strip?.Refresh(true);}});
        detail.Children.Add(more);panel.Children.Add(Design.Surface(detail));
        var edit=new StackPanel{Spacing=14};edit.Children.Add(name);edit.Children.Add(Design.Caption("Choose current, restored windows. Saving captures their layout; it does not open or close applications."));edit.Children.Add(members);
        var editActions=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        Button(editActions,"Refresh windows",()=>members.ItemsSource=service.Enumerate());
        Button(editActions,"Save new",()=>Save(false));Design.Primary(Buttons["Save new"]);
        Button(editActions,"Replace selected",()=>Save(true));edit.Children.Add(editActions);editor.Content=edit;panel.Children.Add(editor);
        Content=Design.Page(panel,"Workspaces","A stage for every kind of work. Switch without losing your place.","STAGES");
        try {
            library=LibraryStore.Load(path);var legacy=Path.Combine(Program.DataDirectory,"workspace.json");
            if(!File.Exists(path) && File.Exists(legacy)){library=library.Upsert("Imported workspace",Workspaces.Load(legacy));LibraryStore.Save(path,library);}
            Reload();members.ItemsSource=service.Enumerate();
        }catch(Exception ex){library=null;report("Workspace library preserved and unavailable: "+ex.Message);}
    }
    private WorkspaceLibrary RequireLibrary()=>library??throw new InvalidOperationException("Repair or back up the unreadable workspace library before saving.");
    private WorkspaceGroup Selected()=>groups.SelectedItem as WorkspaceGroup??throw new InvalidOperationException("Select a workspace.");
    private void Reload(){var id=(groups.SelectedItem as WorkspaceGroup)?.Id;groups.ItemsSource=Groups;groups.SelectedItem=Groups.FirstOrDefault(g=>g.Id==id);groups.Visibility=Groups.Length==0?Visibility.Collapsed:Visibility.Visible;empty.Visibility=Groups.Length==0?Visibility.Visible:Visibility.Collapsed;editor.IsExpanded=Groups.Length==0;preview=null;RenderSelection();}
    private void RenderSelection(){
        stagePreviews.Children.Clear();
        Buttons["Leave Stage"].IsEnabled=!IsBusy && Stage.IsActive;
        Buttons["Apply restore"].IsEnabled=!IsBusy && !Stage.IsActive && preview is not null;
        Buttons["Preview"].IsEnabled=!IsBusy && groups.SelectedItem is WorkspaceGroup;
        Buttons["Edit members"].IsEnabled=!IsBusy && !Stage.IsActive && groups.SelectedItem is WorkspaceGroup;
        Buttons["Remove group"].IsEnabled=Buttons["Edit members"].IsEnabled;
        editor.IsEnabled=!IsBusy && !Stage.IsActive;
        if(groups.SelectedItem is not WorkspaceGroup group){selectedTitle.Text="Choose a workspace";stateText.Text="Select a saved group or create one below.";Buttons["Stage group"].IsEnabled=false;return;}
        name.Text=group.Name;selectedTitle.Text=group.Name;
        if(IsBusy)return;
        var plan=Stage.Preview(group);stateText.Text=Stage.RecoveryError is not null?"Recovery needs attention: "+Stage.RecoveryError:Stage.ActiveGroupId==group.Id?$"● Active stage · {plan.Moves.Count} available":plan.Moves.Count==0?"No matching windows. Open the original applications or edit members.":$"{plan.Moves.Count} window(s) available · {plan.Skipped} unavailable";
        Buttons["Leave Stage"].IsEnabled=Stage.IsActive;Buttons["Apply restore"].IsEnabled=!Stage.IsActive && preview is not null;Buttons["Edit members"].IsEnabled=!Stage.IsActive;Buttons["Remove group"].IsEnabled=!Stage.IsActive;editor.IsEnabled=!Stage.IsActive;
        Buttons["Stage group"].Content=Stage.IsActive?"Switch stage":"Start stage";Buttons["Stage group"].IsEnabled=plan.Moves.Count>0 && Stage.RecoveryError is null;
        RenderStagePreview(plan);
    }
    private void RenderStagePreview(RestorePlan plan){
        stagePreviews.Children.Clear();var row=new StackPanel{Orientation=Orientation.Horizontal,Spacing=12};
        foreach(var move in plan.Moves.Take(4)){var card=new StackPanel{Width=240,Spacing=8};card.Children.Add(new LivePreview(move.Window.Handle,120));card.Children.Add(Design.Caption(move.Window.Title));row.Children.Add(Design.Surface(card));}
        if(plan.Moves.Count>0)stagePreviews.Children.Add(new ScrollViewer{Content=row,HorizontalScrollMode=ScrollMode.Enabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollMode=ScrollMode.Disabled});
    }
    private void EditMembers(){
        if(Stage.IsActive)throw new InvalidOperationException("Leave Stage before editing group membership.");
        var group=Selected();var live=service.Enumerate();members.ItemsSource=live;members.SelectedItems.Clear();
        foreach(var move in Stage.Preview(group).Moves){var match=live.FirstOrDefault(w=>w.Handle==move.Window.Handle);if(match is not null)members.SelectedItems.Add(match);}
        name.Text=group.Name;editor.IsExpanded=true;
    }
    private void Save(bool replace){
        if(Stage.IsActive)throw new InvalidOperationException("Leave Stage before changing saved group layouts.");
        var chosen=members.SelectedItems.Cast<DesktopWindow>().ToArray();var live=service.Enumerate().Where(w=>chosen.Any(c=>c.Handle==w.Handle && c.ProcessId==w.ProcessId && c.ProcessPath==w.ProcessPath && c.Title==w.Title)).ToArray();
        if(live.Length==0)throw new InvalidOperationException("Select at least one current window.");if(live.Any(service.IsMinimized))throw new InvalidOperationException("Restore minimized members before capturing their layout.");
        var next=RequireLibrary().Upsert(name.Text,Workspaces.Capture(live),replace?Selected().Id:null);LibraryStore.Save(path,next);library=next;Reload();groups.SelectedItem=Groups.Last();editor.IsExpanded=false;strip?.Refresh(true);report("Workspace saved locally.");
    }
    private void ShowStrip(){if(disposed)return;if(Groups.Length==0)throw new InvalidOperationException("Create a workspace first.");if(strip is null){strip=new StageStrip(this);strip.Closed+=(_,_)=>strip=null;}strip.ShowStrip();}
    private void Busy(bool busy){IsBusy=busy;groups.IsEnabled=!busy;editor.IsEnabled=!busy;foreach(var button in Buttons.Values)button.IsEnabled=!busy;strip?.Busy(busy);}
    internal async Task SwitchStage(WorkspaceGroup group){
        if(IsBusy)return;
        if(blocked()){report("End focus/fullscreen before switching stages.");return;}
        Busy(true);stateText.Text="Switching stage…";StageResult? result=null;
        try{groups.SelectedItem=Groups.FirstOrDefault(g=>g.Id==group.Id);var area=Canvas();result=await Task.Run(()=>Stage.Activate(group,area));report($"Stage {group.Name}: presented {result.Presented}; minimized {result.Minimized}; skipped {result.Skipped}. "+string.Join(" ",result.Errors));}
        catch(Exception ex){report("Stage unchanged or recovery required: "+ex.Message);}
        finally{Busy(false);if(disposed){Stage.Leave();}else{RenderSelection();if(result?.Presented>0){ShowStrip();strip?.Refresh(true);if(stagePreviews.IsLoaded)SurfaceMotion.Enter(stagePreviews,SurfaceMotion.StageDuration);}}}
    }
    internal async Task LeaveStage(){
        if(IsBusy)return;Busy(true);
        try{var r=await Task.Run(Stage.Leave);report($"Restored {r.Presented}; skipped {r.Skipped}. "+string.Join(" ",r.Errors));}
        catch(Exception ex){report(ex.Message);}
        finally{Busy(false);RenderSelection();if(!Stage.IsActive)strip?.Close();else strip?.Refresh(true);}
    }
    private void Button(StackPanel panel,string label,Action action)=>Button(panel,label,()=>{action();return Task.CompletedTask;});
    private void Button(StackPanel panel,string label,Func<Task> action){var button=new Button{Content=label};Design.Name(button,label);button.Click+=async(_,_)=>{try{if(IsBusy)return;if(blocked() && label is "Stage group" or "Apply restore")throw new InvalidOperationException("End focus/fullscreen before changing workspaces.");await action();}catch(Exception ex){report(ex.Message);}};Buttons[label]=button;panel.Children.Add(button);}
    public void Dispose(){disposed=true;strip?.Close();if(!IsBusy && Stage.IsActive)Stage.Leave();}
}
