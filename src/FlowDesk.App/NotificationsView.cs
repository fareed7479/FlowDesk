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
    private readonly StackPanel cards=new() {Spacing=10};
    private readonly TextBlock status=new() {TextWrapping=TextWrapping.Wrap};
    internal readonly ToggleSwitch Banners=new() {Header="Windows banners",OffContent="Off",OnContent="On"};
    internal readonly ToggleSwitch Sound=new() {Header="Notification sound",OffContent="Silent",OnContent="Sound"};
    internal readonly ToggleSwitch UnreadOnly=new() {Header="Show unread only"};
    internal readonly Dictionary<string,Button> Buttons=new();
    public NotificationsView(NotificationCenter center,NativeNotifications native) {
        this.center=center;this.native=native;
        var panel=new StackPanel {Spacing=12,Padding=new Thickness(20)};
        panel.Children.Add(new TextBlock {Text="FlowDesk notifications",FontSize=24});
        panel.Children.Add(new TextBlock {Text="Workspace and focus events stay in this local center. Windows banners are optional. Quiet sessions keep new events here without banners.",TextWrapping=TextWrapping.Wrap});
        Banners.IsOn=center.State.Preferences.WindowsBanners;Sound.IsOn=center.State.Preferences.Sound;
        Banners.Toggled+=(_,_)=>SaveSettings();Sound.Toggled+=(_,_)=>SaveSettings();UnreadOnly.Toggled+=(_,_)=>Render();
        panel.Children.Add(Banners);panel.Children.Add(Sound);panel.Children.Add(UnreadOnly);
        var actions=new StackPanel {Orientation=Orientation.Horizontal,Spacing=8};
        Add(actions,"Test notification",()=>center.Publish("FlowDesk test","Your notification center is ready."));
        Add(actions,"Mark all read",()=>center.ReadAll());
        Add(actions,"Clear notifications",async ()=>{try{await native.Clear();center.Clear();}catch(Exception ex){status.Text=ex.Message;}});
        panel.Children.Add(actions);panel.Children.Add(status);panel.Children.Add(cards);
        Content=new ScrollViewer {Content=panel};center.Changed+=Render;Render();
    }
    private void SaveSettings() {center.SetPreferences(center.State.Preferences with {WindowsBanners=Banners.IsOn,Sound=Sound.IsOn});}
    private void Add(StackPanel panel,string label,Action action) {var b=new Button {Content=label};b.Click+=(_,_)=>{try{action();}catch(Exception ex){status.Text=ex.Message;}};Buttons[label]=b;panel.Children.Add(b);}
    internal void Render() {
        status.Text=$"{center.Unread} unread · Windows: {native.Status}"+(center.Quiet?" · Quiet session":"")+(center.StorageError is not null?"\nHistory is not being saved: "+center.StorageError:"");
        cards.Children.Clear();
        foreach(var key in Buttons.Keys.Where(k=>k.StartsWith("Read ") || k.StartsWith("Dismiss ")).ToArray())Buttons.Remove(key);
        foreach(var notice in center.State.Items.Where(n=>!UnreadOnly.IsOn || !n.Read)) {
            var body=new StackPanel {Spacing=6};
            body.Children.Add(new TextBlock {Text=$"{notice.Created.ToLocalTime():g} · {notice.Delivery}",TextWrapping=TextWrapping.Wrap});
            body.Children.Add(new TextBlock {Text=notice.Message,TextWrapping=TextWrapping.Wrap});
            var actions=new StackPanel {Orientation=Orientation.Horizontal,Spacing=8};
            Add(actions,"Read "+notice.Id.ToString("N"),()=>center.Read(notice.Id));((Button)actions.Children[0]).Content="Mark read";
            Add(actions,"Dismiss "+notice.Id.ToString("N"),async ()=>{try{await native.Remove(notice.Id);center.Dismiss(notice.Id);}catch(Exception ex){status.Text=ex.Message;}});((Button)actions.Children[1]).Content="Dismiss";
            body.Children.Add(actions);
            cards.Children.Add(new InfoBar {IsOpen=true,IsClosable=false,Title=(notice.Read?"":"● ")+notice.Title,Content=body,Severity=InfoBarSeverity.Informational});
        }
    }
}
