using System;
using System.Collections.Generic;
using System.Linq;
using FlowDesk.Core;
using FlowDesk.Windows;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
namespace FlowDesk.App;
internal sealed class StageStrip : Window {
    private readonly WorkspaceView owner;
    private readonly StackPanel tiles=new(){Spacing=12};
    private readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromSeconds(2)};
    private readonly List<LivePreview> previews=new();
    private string signature="";
    private bool closed;
    private string? lastError;
    internal readonly Dictionary<Guid,Button> StageButtons=new();
    internal int RegisteredPreviews=>previews.Count(p=>p.Registered);
    internal StageStrip(WorkspaceView owner) {
        this.owner=owner;Title="FlowDesk · Stages";
        SystemBackdrop=new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
        var presenter=OverlappedPresenter.Create();presenter.IsAlwaysOnTop=true;presenter.IsResizable=false;presenter.IsMaximizable=false;presenter.IsMinimizable=false;presenter.SetBorderAndTitleBar(false,false);AppWindow.SetPresenter(presenter);
        var body=new StackPanel{Spacing=12,Padding=new Thickness(12)};
        body.Children.Add(Design.Heading("Stages",18));
        var scroll=new ScrollViewer{Content=tiles,MaxHeight=490,VerticalScrollMode=ScrollMode.Enabled,HorizontalScrollMode=ScrollMode.Disabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
        body.Children.Add(scroll);
        var leave=new Button{Content="Leave Stage",HorizontalAlignment=HorizontalAlignment.Stretch};leave.Click+=async(_,_)=>await owner.LeaveStage();body.Children.Add(leave);
        var hide=new Button{Content="Hide strip",HorizontalAlignment=HorizontalAlignment.Stretch};hide.Click+=(_,_)=>Close();body.Children.Add(hide);
        var root=Design.Surface(body,true);root.Padding=new Thickness(0);root.RequestedTheme=AppData.LoadTheme();Content=root;
        timer.Tick+=(_,_)=>Refresh();timer.Start();Closed+=(_,_)=>{closed=true;timer.Stop();foreach(var preview in previews)preview.Dispose();};
        Refresh(true);
    }
    internal void ShowStrip() {Position();AppWindow.Show(false);}
    private void Position() {
        var area=owner.StageWorkArea();var scale=owner.StageScale;
        int width=(int)(232*scale),height=Math.Min(area.Height-24,(int)(720*scale));
        AppWindow.MoveAndResize(new(area.X+12,area.Y+Math.Max(12,(area.Height-height)/2),width,Math.Max(200,height)));
    }
    internal void Refresh(bool force=false){try{RefreshCore(force);}catch(Exception ex)when(ex is System.ComponentModel.Win32Exception or InvalidOperationException or ArgumentException){signature="";if(lastError!=ex.Message){lastError=ex.Message;owner.StageHint("Stage previews are temporarily unavailable. "+ex.Message);}}}
    private void RefreshCore(bool force) {
        if(closed || owner.IsBusy)return;
        if(Content is FrameworkElement root)root.RequestedTheme=AppData.LoadTheme();var inventory=owner.Inventory();
        var states=owner.Groups.Select(g=>{var p=owner.Stage.Preview(g,null,inventory);return (Group:g,Plan:p);}).ToArray();
        string next=string.Join("|",states.Select(s=>$"{s.Group.Id}:{s.Group.Name}:{s.Plan.Moves.Count}:{owner.Stage.ActiveGroupId}:"+string.Join(",",s.Plan.Moves.Select(m=>m.Window.Handle))));
        if(!force && next==signature)return;signature=next;
        foreach(var preview in previews)preview.Dispose();previews.Clear();tiles.Children.Clear();StageButtons.Clear();
        foreach(var state in states) {
            bool active=owner.Stage.ActiveGroupId==state.Group.Id;
            var stack=new StackPanel{Spacing=6};
            if(state.Plan.Moves.Count>0){var preview=new LivePreview(state.Plan.Moves[0].Window.Handle,86,WinRT.Interop.WindowNative.GetWindowHandle(this));previews.Add(preview);stack.Children.Add(preview);}
            stack.Children.Add(Design.Heading(state.Group.Name,15));
            stack.Children.Add(Design.Caption(active?"● Active stage":state.Plan.Moves.Count==0?"No matching windows":$"{state.Plan.Moves.Count} window(s) available"));
            var button=new Button{Content=stack,HorizontalAlignment=HorizontalAlignment.Stretch,HorizontalContentAlignment=HorizontalAlignment.Stretch,Padding=new Thickness(12),IsEnabled=state.Plan.Moves.Count>0};
            button.CornerRadius=new CornerRadius(20);if(active){button.Style=(Style)Application.Current.Resources["GlassStageActiveStyle"];stack.Loaded+=(_,_)=>SurfaceMotion.Enter(stack,SurfaceMotion.StageDuration);}
            Design.Name(button,$"{state.Group.Name}, "+(active?"active stage":state.Plan.Moves.Count==0?"unavailable":$"{state.Plan.Moves.Count} windows. Switch stage"));
            var group=state.Group;button.Click+=async(_,_)=>await owner.SwitchStage(group);tiles.Children.Add(button);StageButtons[group.Id]=button;
        }
        Position();lastError=null;
    }
    internal void Busy(bool busy) {foreach(var button in StageButtons.Values)button.IsEnabled=!busy;}
}
