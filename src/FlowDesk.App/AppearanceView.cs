using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace FlowDesk.App;
internal sealed record Appearance(int Schema,string Theme);
internal static class AppData {
    internal static string Resolve() {
        var marker=Path.Combine(AppContext.BaseDirectory,"flowdesk-install.json");
        if(!File.Exists(marker))return Path.Combine(AppContext.BaseDirectory,"data");
        using var document=JsonDocument.Parse(File.ReadAllText(marker));
        var root=document.RootElement;
        if(root.GetProperty("owner").GetString()!="fareed7479.FlowDesk" || root.GetProperty("schema").GetInt32()!=1)throw new InvalidDataException("Unsupported installation marker.");
        var path=root.GetProperty("dataDirectory").GetString()??"";
        if(!Path.IsPathFullyQualified(path))throw new InvalidDataException("Installation data directory must be absolute.");
        return path;
    }
    internal static ElementTheme LoadTheme() {
        var path=Path.Combine(Program.DataDirectory,"appearance.json");
        try{if(File.Exists(path)){var p=JsonSerializer.Deserialize<Appearance>(File.ReadAllText(path));if(p?.Schema==1 && Enum.TryParse<ElementTheme>(p.Theme,out var theme) && Enum.IsDefined(theme))return theme;}}catch(Exception ex)when(ex is IOException or JsonException or UnauthorizedAccessException){}
        return ElementTheme.Default;
    }
    internal static void SaveTheme(ElementTheme theme) {Directory.CreateDirectory(Program.DataDirectory);var path=Path.Combine(Program.DataDirectory,"appearance.json");File.WriteAllText(path+".tmp",JsonSerializer.Serialize(new Appearance(1,theme.ToString())));File.Move(path+".tmp",path,true);}
}
internal sealed class AppearanceView : UserControl {
    internal ComboBox Theme {get;private set;}=null!;
    internal AppearanceView(FrameworkElement shell) {
        var panel=new StackPanel{Spacing=16};panel.Children.Add(Design.Heading("Settings"));panel.Children.Add(Design.Caption(""));
        panel.Children.Add(Design.Heading("Make room for your flow",22));
        var theme=Theme=new ComboBox{Header="Appearance",MinWidth=260};foreach(var label in new[]{"Use Windows setting","Light","Dark"})theme.Items.Add(label);
        theme.SelectedIndex=shell.RequestedTheme==ElementTheme.Light?1:shell.RequestedTheme==ElementTheme.Dark?2:0;
        var status=Design.Caption("");
        theme.SelectionChanged+=(_,_)=>{try{var value=theme.SelectedIndex==1?ElementTheme.Light:theme.SelectedIndex==2?ElementTheme.Dark:ElementTheme.Default;AppData.SaveTheme(value);shell.RequestedTheme=value;}catch(Exception ex){status.Text=ex.Message;}};
        panel.Children.Add(theme);
        panel.Children.Add(Design.Heading("Startup",20));panel.Children.Add(Design.Caption("Start FlowDesk when you sign in. This is optional and off until you enable it."));
        var startup=new ToggleSwitch{Header="Launch at sign-in"};
        const string key=@"Software\Microsoft\Windows\CurrentVersion\Run";string command="\""+Environment.ProcessPath+"\"";
        using(var registry=Registry.CurrentUser.OpenSubKey(key))startup.IsOn=registry?.GetValue("FlowDesk") as string==command;
        startup.Toggled+=(_,_)=>{try{using var registry=Registry.CurrentUser.CreateSubKey(key);var current=registry.GetValue("FlowDesk") as string;if(current is not null && current!=command)throw new InvalidOperationException("An existing FlowDesk startup entry points elsewhere. Use its installer to update or remove it.");if(startup.IsOn)registry.SetValue("FlowDesk",command,RegistryValueKind.String);else if(current==command)registry.DeleteValue("FlowDesk",false);status.Text=startup.IsOn?"FlowDesk will launch at sign-in.":"Launch at sign-in is off.";}catch(Exception ex){status.Text=ex.Message;}};
        panel.Children.Add(startup);panel.Children.Add(status);
        panel.Children.Add(Design.Heading("Your data stays local",20));panel.Children.Add(Design.Caption("Layouts, preferences, recovery information, and explicitly saved browser URLs are stored locally. Back up this folder before uninstalling or cleaning a development build."));
        var folder=new Button{Content="Open data folder"};folder.Click+=(_,_)=>{Directory.CreateDirectory(Program.DataDirectory);System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Program.DataDirectory){UseShellExecute=true});};panel.Children.Add(folder);
        panel.Children.Add(Design.Caption(Program.DataDirectory));
        panel.Children.Add(Design.Heading("About FlowDesk",20));panel.Children.Add(Design.Caption("Version 0.5.0 · Windows 11 x64 · WinUI 3\nAdvanced desktop preview. Installer and compatibility evidence are documented in the repository. Trusted signing and store publication require release credentials."));
        Content=Design.Page(panel,"Built around your workflow","Appearance, launch preferences, and a clear view of your local data.","PREFERENCES");
    }
}
