using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
namespace FlowDesk.App;
internal static class Design {
    private const string Namespace="http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    internal static Border Surface(UIElement child,bool backdropWindow=false) {
        var card=(Border)XamlReader.Load($"<Border xmlns='{Namespace}' Background='{{ThemeResource {(backdropWindow?"GlassTintBrush":"GlassFillBrush")}}}' BorderBrush='{{ThemeResource GlassStrokeBrush}}' BorderThickness='1' CornerRadius='24' Padding='24'/>");
        card.Child=child;return card;
    }
    internal static Grid Canvas()=>(Grid)XamlReader.Load($"<Grid xmlns='{Namespace}' Background='{{ThemeResource GlassCanvasBrush}}'/>");
    internal static TextBlock Caption(string text) {var label=(TextBlock)XamlReader.Load($"<TextBlock xmlns='{Namespace}' Foreground='{{ThemeResource GlassSecondaryTextBrush}}' TextWrapping='Wrap' FontSize='13'/>");label.Text=text;return label;}
    internal static TextBlock Heading(string text,double size=24) {var label=(TextBlock)XamlReader.Load($"<TextBlock xmlns='{Namespace}' Foreground='{{ThemeResource GlassTextBrush}}' TextWrapping='Wrap' FontWeight='SemiBold'/>");label.Text=text;label.FontSize=size;return label;}
    internal static ScrollViewer Page(StackPanel body,string title,string subtitle,string category="WORKSPACE",int remove=2) {
        for(int i=0;i<remove && body.Children.Count>0;i++)body.Children.RemoveAt(0);
        body.Padding=new Thickness(0);body.Spacing=18;
        var hero=new StackPanel{Spacing=8};hero.Children.Add(Heading(title,32));hero.Children.Add(Caption(subtitle));
        var page=new StackPanel{Spacing=28,Padding=new Thickness(28,28,28,32),MaxWidth=1180,HorizontalAlignment=HorizontalAlignment.Stretch};
        page.Children.Add(hero);page.Children.Add(body);
        page.Loaded+=(_,_)=>SurfaceMotion.Enter(page);page.Unloaded+=(_,_)=>SurfaceMotion.Stop(page);
        return new ScrollViewer{Content=page,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalScrollMode=ScrollMode.Disabled};
    }
    internal static void Primary(Button button)=>button.Style=(Style)Application.Current.Resources["GlassPrimaryButtonStyle"];
    internal static void Name(DependencyObject element,string name)=>AutomationProperties.SetName(element,name);
}
internal sealed class PageSelection {
    private int index;
    internal int SelectedIndex {get=>index;set{index=value;Changed?.Invoke(value);}}
    internal event Action<int>? Changed;
}
