using System;
using System.Collections.Generic;
using FlowDesk.Core;
using FlowDesk.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace FlowDesk.App;
internal sealed class LivePreview : UserControl,IDisposable {
    private readonly nint source,destination;
    private LiveThumbnail? thumbnail;
    private bool failed,attached;
    private Rect? previous;
    private readonly List<ScrollViewer> scrollers=new();
    private readonly HashSet<ScrollViewer> moving=new();
    private readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromMilliseconds(80)};
    private DateTimeOffset ready;
    internal bool Registered=>thumbnail?.Registered==true;
    internal LivePreview(nint source,double height=140,nint destination=0) {
        this.source=source;this.destination=destination==0?Program.MainHandle:destination;Height=height;Margin=new Thickness(0,0,0,10);
        Content=new TextBlock{Text="Live preview · unavailable when minimized or protected",TextWrapping=TextWrapping.Wrap,Opacity=.5,Margin=new Thickness(12),VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center,FontSize=12};
        Design.Name(this,"Live window preview");
        Loaded+=(_,_)=>Attach();LayoutUpdated+=(_,_)=>Update();Unloaded+=(_,_)=>Dispose();timer.Tick+=(_,_)=>Update();
    }
    private void Attach() {
        if(attached)return;attached=true;ready=DateTimeOffset.UtcNow;
        DependencyObject? parent=this;
        while((parent=Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent)) is not null)if(parent is ScrollViewer scroll){scrollers.Add(scroll);scroll.ViewChanging+=Changing;scroll.ViewChanged+=Changed;}
        timer.Start();
    }
    private void Changing(object? sender,ScrollViewerViewChangingEventArgs e) {if(sender is ScrollViewer scroll)moving.Add(scroll);Hide();}
    private void Changed(object? sender,ScrollViewerViewChangedEventArgs e) {
        if(sender is not ScrollViewer scroll)return;
        if(e.IsIntermediate){moving.Add(scroll);Hide();}
        else {moving.Remove(scroll);ready=DateTimeOffset.UtcNow.AddMilliseconds(100);}
    }
    private void Hide(){if(previous is null)return;try{thumbnail?.Update(new(0,0,1,1),false);}catch(System.Runtime.InteropServices.COMException){thumbnail?.Dispose();thumbnail=null;failed=true;}previous=null;}
    private void Update() {
        if(!attached || !IsLoaded || failed || XamlRoot is null || ActualWidth<2)return;
        try {
            if(moving.Count>0 || DateTimeOffset.UtcNow<ready || SurfaceMotion.Moving(this)){Hide();return;}
            var point=TransformToVisual(null).TransformPoint(new global::Windows.Foundation.Point(0,0));
            bool visible=true;DependencyObject? parent=this;
            while((parent=Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent)) is not null) {
                if(parent is ScrollViewer scroll){var p=TransformToVisual(scroll).TransformPoint(new global::Windows.Foundation.Point(0,0));if(p.Y<0 || p.Y+ActualHeight>scroll.ActualHeight || p.X<0 || p.X+ActualWidth>scroll.ActualWidth)visible=false;}
                if(parent is UIElement element && element.Visibility!=Visibility.Visible)visible=false;
            }
            var scale=XamlRoot.RasterizationScale;
            var rect=new Rect((int)(point.X*scale),(int)(point.Y*scale),Math.Max(1,(int)(ActualWidth*scale)),Math.Max(1,(int)(ActualHeight*scale)));
            if(!visible){Hide();return;}
            thumbnail??=new LiveThumbnail(destination,source);
            if(previous!=rect){thumbnail.Update(rect,true);previous=rect;}
        }catch(Exception ex)when(ex is System.Runtime.InteropServices.COMException or ArgumentException or InvalidOperationException){failed=true;Dispose();}
    }
    public void Dispose() {
        timer.Stop();foreach(var scroll in scrollers){scroll.ViewChanging-=Changing;scroll.ViewChanged-=Changed;}
        scrollers.Clear();moving.Clear();attached=false;thumbnail?.Dispose();thumbnail=null;previous=null;
    }
}
