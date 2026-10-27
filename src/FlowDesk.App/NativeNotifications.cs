using System;
using System.Threading.Tasks;
using FlowDesk.Core;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
namespace FlowDesk.App;
internal sealed class NativeNotifications : INoticeDelivery, IDisposable {
    private bool registered;
    public string Status {get;private set;}="Not registered";
    public void Register() {
        try {
            NotificationProtocol.Register();
            if(!AppNotificationManager.IsSupported()) {Status="Windows app notifications are unavailable";return;}
            AppNotificationManager.Default.NotificationInvoked+=Invoked;
            AppNotificationManager.Default.Register();registered=true;Status=AppNotificationManager.Default.Setting.ToString();
        } catch(Exception ex) {Status=ex.Message;}
    }
    private void Invoked(AppNotificationManager sender,AppNotificationActivatedEventArgs args) {var id=NotificationCenter.ActivationId(args.Argument);if(id.HasValue) App.OpenNotice(id.Value);}
    public string Send(Notice notice,bool sound) {
        if(!registered) return "Unavailable: "+Status;
        if(AppNotificationManager.Default.Setting!=AppNotificationSetting.Enabled) return "Windows disabled banners: "+AppNotificationManager.Default.Setting;
        string title=System.Security.SecurityElement.Escape(notice.Title)!;
        string message=System.Security.SecurityElement.Escape(notice.Message)!;
        string audio=sound?"":"<audio silent=\"true\"/>";
        var notification=new AppNotification($"<toast activationType=\"protocol\" launch=\"flowdesk-control://notification/{notice.Id:N}\" duration=\"long\"><visual><binding template=\"ToastGeneric\"><text>{title}</text><text>{message}</text></binding></visual>{audio}</toast>") {Tag=notice.Id.ToString("N"),Group="FlowDesk"};
        AppNotificationManager.Default.Show(notification);
        return notification.Id==0?"Windows did not accept the notification":"Submitted to Windows";
    }
    public async Task Remove(Guid id) {if(registered) await AppNotificationManager.Default.RemoveByTagAndGroupAsync(id.ToString("N"),"FlowDesk");}
    public async Task Clear() {if(registered) await AppNotificationManager.Default.RemoveByGroupAsync("FlowDesk");}
    public async Task<bool> Contains(Guid id) {if(!registered)return false;var all=await AppNotificationManager.Default.GetAllAsync();foreach(var item in all)if(item.Tag==id.ToString("N") && item.Group=="FlowDesk")return true;return false;}
    public async Task<bool> HasProtocolLink(Guid id) {
        if(!registered)return false;var all=await AppNotificationManager.Default.GetAllAsync();
        foreach(var item in all)if(item.Tag==id.ToString("N") && item.Group=="FlowDesk") {
            var root=System.Xml.Linq.XDocument.Parse(item.Payload).Root;
            return root?.Attribute("activationType")?.Value=="protocol" && NotificationCenter.ActivationUri(root.Attribute("launch")?.Value??"")==id;
        }
        return false;
    }
    public void Dispose() {if(registered){AppNotificationManager.Default.NotificationInvoked-=Invoked;AppNotificationManager.Default.Unregister();registered=false;}}
}
