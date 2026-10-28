using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FlowDesk.Core;
using FlowDesk.Windows;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace FlowDesk.App;
internal sealed class FloatingDock : Window {
    private readonly WindowService service;
    private readonly Func<DockPreferences> preferences;
    private readonly Action<string> report;
    private DesktopDisplay? display;
    private readonly StackPanel apps=new(){Orientation=Orientation.Horizontal,Spacing=6};
    private readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromMilliseconds(300)};
    private readonly Dictionary<string,int> cycles=new(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset away=DateTimeOffset.UtcNow;
    private bool collapsed,closed;
    private int ticks;
    private string topology="";
    private int fullWidth=650,fullHeight=92;
    internal bool IsCollapsed=>collapsed;
    internal FloatingDock(WindowService service,Func<DockPreferences> preferences,Action<string> report,DesktopDisplay? display) {
        this.service=service;this.preferences=preferences;this.report=report;this.display=display;
        Title="FlowDesk · FlowDock";SystemBackdrop=new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
        var presenter=OverlappedPresenter.Create();presenter.IsAlwaysOnTop=true;presenter.IsResizable=false;presenter.IsMaximizable=false;presenter.IsMinimizable=false;presenter.SetBorderAndTitleBar(false,false);AppWindow.SetPresenter(presenter);
        var panel=new StackPanel{Spacing=6,Padding=new Thickness(12,8,12,8)};
        panel.Children.Add(new TextBlock{Text="F L O W D O C K",FontSize=10,Opacity=.65,HorizontalAlignment=HorizontalAlignment.Center});
        panel.Children.Add(new ScrollViewer{Content=apps,HorizontalScrollMode=ScrollMode.Enabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Hidden,VerticalScrollMode=ScrollMode.Disabled});
        var root=Design.Surface(panel,true);root.Padding=new Thickness(0);root.RequestedTheme=AppData.LoadTheme();Content=root;
        timer.Tick+=(_,_)=>Tick();timer.Start();Closed+=(_,_)=>{closed=true;timer.Stop();};Refresh();
    }
    internal void ShowDock(){Position(false);AppWindow.Show(false);}
    internal void Refresh() {
        if(closed)return;apps.Children.Clear();var windows=service.Enumerate();
        foreach(var path in preferences().Pins.Concat(windows.Select(w=>w.ProcessPath)).Distinct(StringComparer.OrdinalIgnoreCase).Take(24)) {
            int count=windows.Count(w=>StringComparer.OrdinalIgnoreCase.Equals(w.ProcessPath,path));
            var name=Path.GetFileNameWithoutExtension(path);var button=new Button{Content=name+(count>0?$" · {count}":""),MinHeight=40,MaxWidth=180};
            Design.Name(button,$"{name}, {count} running windows. Activate or launch.");
            button.Click+=(_,_)=>{try{var current=service.Enumerate().Where(w=>StringComparer.OrdinalIgnoreCase.Equals(w.ProcessPath,path)).OrderBy(w=>w.Handle).ToArray();if(current.Length>0){int i=cycles.GetValueOrDefault(path)%current.Length;cycles[path]=i+1;service.Activate(current[i]);}else if(preferences().Pins.Contains(path,StringComparer.OrdinalIgnoreCase)&&File.Exists(path))Process.Start(new ProcessStartInfo(path){UseShellExecute=true});}catch(Exception ex){report(ex.Message);}};
            apps.Children.Add(button);
        }
        Position(collapsed);
    }
    private void Position(bool hide) {
        var displays=service.Displays();display=displays.FirstOrDefault(d=>d.Device==display?.Device)??displays.FirstOrDefault();if(display is null)return;
        // AppWindow positions use physical pixels; scale the dock chrome for the selected display.
        uint dpi=GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(this));double scale=(dpi==0?96:dpi)/96d;
        var work=display.WorkArea;fullWidth=Math.Min(work.Width-16,(int)(720*scale));fullHeight=(int)(100*scale);
        int height=hide?Math.Max(8,(int)(8*scale)):fullHeight;
        AppWindow.MoveAndResize(new(work.X+(work.Width-fullWidth)/2,work.Y+work.Height-height,fullWidth,height));
        if(Content is FrameworkElement root)root.Opacity=hide?.35:1;
        collapsed=hide;
    }
    private void Tick() {
        if(closed||display is null)return;
        if(++ticks%20==0){var signature=string.Join(";",service.Displays().Select(d=>$"{d.Device}:{d.WorkArea}"));if(signature!=topology){topology=signature;Position(collapsed);}}
        if(!preferences().AutoHide){if(collapsed)Position(false);return;}
        GetCursorPos(out var point);var pos=AppWindow.Position;var size=AppWindow.Size;
        bool inside=point.X>=pos.X && point.X<pos.X+size.Width && point.Y>=pos.Y-3 && point.Y<pos.Y+size.Height;
        bool focused=GetForegroundWindow()==WinRT.Interop.WindowNative.GetWindowHandle(this);
        if(inside||focused){away=DateTimeOffset.UtcNow;if(collapsed)Position(false);}
        else if(!collapsed && DateTimeOffset.UtcNow-away>TimeSpan.FromSeconds(2))Position(true);
    }
    [StructLayout(LayoutKind.Sequential)]private struct Point {public int X,Y;}
    [DllImport("user32.dll")]private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")]private static extern uint GetDpiForWindow(nint window);
}
