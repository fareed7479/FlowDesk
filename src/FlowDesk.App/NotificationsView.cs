using System;
using System.Collections.Generic;
using System.Linq;
using FlowDesk.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace FlowDesk.App;
internal sealed class NotificationsView : UserControl {
    internal int CardCount=>cards.Children.Count;
    private readonly NotificationCenter center;
    private readonly NativeNotifications native;
    private readonly StackPanel cards=new(){Spacing=12};
    private readonly TextBlock status=Design.Caption("");
    private readonly TextBlock empty=Design.Caption("You're all caught up. New FlowDesk events will appear here.");
    private readonly HashSet<Guid> seen=new();
    internal readonly ToggleSwitch Banners=new(){Header="Windows banners",OffContent="Off",OnContent="On"};
    internal readonly ToggleSwitch Sound=new(){Header="Notification sound",OffContent="Silent",OnContent="Sound"};
    internal readonly ToggleSwitch UnreadOnly=new(){Header="Show unread only"};
    internal readonly Dictionary<string,Button> Buttons=new();
    public NotificationsView(NotificationCenter center,NativeNotifications native) {
        this.center=center;this.native=native;
        var panel=new StackPanel{Spacing=20};panel.Children.Add(Design.Heading("Notifications"));panel.Children.Add(Design.Caption(""));
        var controls=new StackPanel{Spacing=14};controls.Children.Add(UnreadOnly);
        var actions=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
        Add(actions,"Mark all read",()=>center.ReadAll());
        Add(actions,"Clear notifications",async()=>{try{await native.Clear();center.Clear();}catch(Exception ex){status.Text=ex.Message;}});
        controls.Children.Add(actions);controls.Children.Add(status);panel.Children.Add(controls);panel.Children.Add(empty);panel.Children.Add(cards);
        var preferences=new StackPanel{Spacing=14};preferences.Children.Add(Design.Heading("Delivery preferences",20));preferences.Children.Add(Design.Caption("FlowDesk keeps local events here. Native Windows banners are optional and keep their Windows appearance."));
        Banners.IsOn=center.State.Preferences.WindowsBanners;Sound.IsOn=center.State.Preferences.Sound;
        Banners.Toggled+=(_,_)=>SaveSettings();Sound.Toggled+=(_,_)=>SaveSettings();UnreadOnly.Toggled+=(_,_)=>Render();
        preferences.Children.Add(Banners);preferences.Children.Add(Sound);
        var tests=new StackPanel{Orientation=Orientation.Horizontal};Add(tests,"Test notification",()=>center.Publish("FlowDesk test","Your notification center is ready."));preferences.Children.Add(tests);panel.Children.Add(Design.Surface(preferences));
        Content=Design.Page(panel,"Notifications","Quiet updates, right where you need them.","NOTIFICATIONS");
        center.Changed+=Render;Render();
    }
    private void SaveSettings(){try{center.SetPreferences(center.State.Preferences with{WindowsBanners=Banners.IsOn,Sound=Sound.IsOn});}catch(Exception ex){status.Text=ex.Message;}}
    private void Add(StackPanel panel,string label,Action action){var button=new Button{Content=label};button.Click+=(_,_)=>{try{action();}catch(Exception ex){status.Text=ex.Message;}};Buttons[label]=button;panel.Children.Add(button);}
    internal void Render(){
        status.Text=$"{center.Unread} unread · Windows: {native.Status}"+(center.Quiet?" · Quiet session":"")+(center.StorageError is not null?"\nHistory is not being saved: "+center.StorageError:"");
        foreach(var key in Buttons.Keys.Where(k=>k.StartsWith("Read ")||k.StartsWith("Dismiss ")).ToArray())Buttons.Remove(key);
        seen.IntersectWith(center.State.Items.Select(n=>n.Id));cards.Children.Clear();
        var visible=center.State.Items.Where(n=>!UnreadOnly.IsOn||!n.Read).ToArray();empty.Visibility=visible.Length==0?Visibility.Visible:Visibility.Collapsed;
        foreach(var notice in visible){
            bool animate=IsLoaded && !seen.Contains(notice.Id);seen.Add(notice.Id);
            var body=new StackPanel{Spacing=10};
            body.Children.Add(Design.Heading((notice.Read?"":"● ")+notice.Title,17));
            body.Children.Add(Design.Caption($"{notice.Created.ToLocalTime():g} · {notice.Delivery}"+(notice.Read?" · Read":" · Unread")));
            body.Children.Add(new TextBlock{Text=notice.Message,TextWrapping=TextWrapping.Wrap,FontSize=14});
            var actions=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};
            Add(actions,"Read "+notice.Id.ToString("N"),()=>center.Read(notice.Id));var read=(Button)actions.Children[0];read.Content="Mark read";read.IsEnabled=!notice.Read;
            Add(actions,"Dismiss "+notice.Id.ToString("N"),async()=>{try{await native.Remove(notice.Id);center.Dismiss(notice.Id);}catch(Exception ex){status.Text=ex.Message;}});((Button)actions.Children[1]).Content="Dismiss";
            body.Children.Add(actions);var card=Design.Surface(body);
            if(animate)card.Loaded+=(_,_)=>SurfaceMotion.Enter(card);card.Unloaded+=(_,_)=>SurfaceMotion.Stop(card);cards.Children.Add(card);
        }
    }
}
