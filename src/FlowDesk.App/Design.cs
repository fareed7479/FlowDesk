using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
namespace FlowDesk.App;
internal static class Design {
    internal static Border Surface(UIElement child) {
        var card=(Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource CardBackgroundFillColorDefaultBrush}' BorderBrush='{ThemeResource CardStrokeColorDefaultBrush}' BorderThickness='1' CornerRadius='16' Padding='20'/>");
        card.Child=child;return card;
    }
    internal static TextBlock Caption(string text)=>new(){Text=text,FontSize=13,Opacity=.72,TextWrapping=TextWrapping.Wrap};
    internal static TextBlock Heading(string text,double size=24)=>new(){Text=text,FontSize=size,FontWeight=Microsoft.UI.Text.FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap};
    internal static ScrollViewer Page(StackPanel body,string title,string subtitle,string category="WORKSPACE",int remove=2) {
        for(int i=0;i<remove && body.Children.Count>0;i++)body.Children.RemoveAt(0);
        body.Padding=new Thickness(0);body.Spacing=16;
        var hero=new StackPanel{Spacing=8};
        hero.Children.Add(new TextBlock{Text=category,FontSize=11,CharacterSpacing=180,Foreground=new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255,71,177,166))});
        hero.Children.Add(Heading(title,32));hero.Children.Add(Caption(subtitle));
        var page=new StackPanel{Spacing=24,Padding=new Thickness(28,24,28,28),MaxWidth=1180,HorizontalAlignment=HorizontalAlignment.Stretch};
        page.ChildrenTransitions=new Microsoft.UI.Xaml.Media.Animation.TransitionCollection{new Microsoft.UI.Xaml.Media.Animation.EntranceThemeTransition{IsStaggeringEnabled=true}};
        page.Children.Add(hero);page.Children.Add(Surface(body));
        return new ScrollViewer{Content=page,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalScrollMode=ScrollMode.Disabled};
    }
    internal static void Name(DependencyObject element,string name)=>AutomationProperties.SetName(element,name);
}
internal sealed class PageSelection {
    private int index;
    internal int SelectedIndex {get=>index;set{index=value;Changed?.Invoke(value);}}
    internal event Action<int>? Changed;
}
