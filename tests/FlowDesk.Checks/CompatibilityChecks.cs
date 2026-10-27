using System.Diagnostics;
using FlowDesk.Windows;
internal static class CompatibilityChecks {
    internal static void Run(Action<bool,string> check) {
        var service=new WindowService();var owned=new List<Process>();
        string prefix="FlowDesk compatibility "+Guid.NewGuid().ToString("N");
        Process Start(string suffix,string option){var p=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false};p.ArgumentList.Add("--fixture");p.ArgumentList.Add(prefix+suffix);p.ArgumentList.Add(option);var child=Process.Start(p)!;owned.Add(child);return child;}
        try {
            Start(" borderless","--borderless");
            FlowDesk.Core.DesktopWindow? borderless=null;
            for(int i=0;i<60 && borderless is null;i++){borderless=service.Enumerate().SingleOrDefault(w=>w.Title==prefix+" borderless");Thread.Sleep(100);}
            check(borderless is not null,"inventory detects a controlled borderless application");
            var before=service.CaptureState(borderless!);service.Move(borderless!,new(150,150,480,360));Thread.Sleep(200);
            check(service.Resolve(borderless!)?.Bounds.Width==480,"native positioning supports a controlled borderless application");
            service.RestoreState(service.Resolve(borderless!)!,before);
            Start(" tool","--tool");Start(" hidden","--hidden");Thread.Sleep(1000);
            check(!service.Enumerate().Any(w=>w.Title==prefix+" tool"),"inventory excludes controlled tool windows");
            check(!service.Enumerate().Any(w=>w.Title==prefix+" hidden"),"inventory excludes controlled hidden windows");
            var hungProcess=Start(" unresponsive","--unresponsive");
            FlowDesk.Core.DesktopWindow? hung=null;
            for(int i=0;i<150;i++){hung=service.Enumerate().SingleOrDefault(w=>w.Title==prefix+" unresponsive");if(hung is not null && !service.IsResponsive(hung))break;Thread.Sleep(100);}
            check(hung is not null && !service.IsResponsive(hung),"controlled unresponsive fixture is detected by Windows");
            var timer=Stopwatch.StartNew();bool rejected=false;
            try{service.Move(hung!,new(100,100,400,300));}catch(InvalidOperationException){rejected=true;}
            check(rejected && timer.Elapsed<TimeSpan.FromSeconds(2),"unresponsive window mutation is rejected without blocking for its message loop");
        }finally {
            foreach(var process in owned){if(!process.HasExited){process.CloseMainWindow();if(!process.WaitForExit(1000))process.Kill();}process.Dispose();}
        }
    }
}
