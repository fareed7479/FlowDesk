using System;
using System.IO;
using Microsoft.Win32;
namespace FlowDesk.App;
internal static class NotificationProtocol {
    internal static void Register() {
        const string name=@"Software\Classes\flowdesk-control";
        string exe=Path.Combine(AppContext.BaseDirectory,"FlowDesk.App.exe");
        using var existing=Registry.CurrentUser.OpenSubKey(name);
        if(existing is not null && existing.GetValue("FlowDeskOwner") as string!="fareed7479.FlowDesk")throw new InvalidOperationException("The flowdesk-control protocol is already owned by another application.");
        using var key=Registry.CurrentUser.CreateSubKey(name);key.SetValue("","URL:FlowDesk notification");key.SetValue("URL Protocol","");key.SetValue("FlowDeskOwner","fareed7479.FlowDesk");
        using var command=key.CreateSubKey(@"shell\open\command");command.SetValue("","\""+exe+"\" --notification-uri \"%1\"");
    }
}
