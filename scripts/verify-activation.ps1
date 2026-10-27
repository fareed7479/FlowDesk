$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$exe=Join-Path $root 'src/FlowDesk.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/FlowDesk.App.exe'
if(Get-Process FlowDesk.App -ErrorAction SilentlyContinue){throw 'Close FlowDesk before the activation check.'}
$probeProcess=Start-Process -FilePath $exe -ArgumentList '--toast-activation-probe' -PassThru
if(!$probeProcess.WaitForExit(10000) -or $probeProcess.ExitCode -ne 0){throw 'Activation probe could not be prepared.'}
$probe=Get-Content -Raw (Join-Path $root '.cache/activation-probe.json') | ConvertFrom-Json
$success=$false
@{Complete=$false;Success=$false} | ConvertTo-Json | Set-Content (Join-Path $root '.cache/activation-results.json')
try {
    if($probe.Delivery -ne 'Submitted to Windows'){throw 'Windows did not accept the activation probe.'}
    $stateFile=Join-Path $probe.DataDirectory 'notifications.json'
    function Invoke-Notice($id) {
        $uri='flowdesk-control://notification/'+([Guid]$id).ToString('N')
        $start=[Diagnostics.ProcessStartInfo]::new($uri);$start.UseShellExecute=$true
        [Diagnostics.Process]::Start($start) | Out-Null
        $read=$false
        for($attempt=0;$attempt -lt 100;$attempt++) {
            Start-Sleep -Milliseconds 100
            $state=Get-Content -Raw $stateFile | ConvertFrom-Json
            $notice=$state.Items | Where-Object {$_.Id -eq $id}
            if($notice.Read){$read=$true;break}
        }
        if(!$read){throw 'Notification URI did not open its local card and mark it read.'}
        for($settle=0;$settle -lt 50 -and @(Get-Process FlowDesk.App -ErrorAction SilentlyContinue).Count -ne 1;$settle++){Start-Sleep -Milliseconds 100}
        if(@(Get-Process FlowDesk.App -ErrorAction SilentlyContinue).Count -ne 1){throw 'Notification activation did not settle to exactly one instance.'}
    }
    Invoke-Notice $probe.NoticeId
    Invoke-Notice $probe.WarmNoticeId
    $duplicate=Start-Process -FilePath $exe -PassThru
    if(!$duplicate.WaitForExit(10000) -or $duplicate.ExitCode -ne 0){throw 'Second launch did not redirect cleanly.'}
    if(@(Get-Process FlowDesk.App -ErrorAction SilentlyContinue).Count -ne 1){throw 'Single-instance redirect failed.'}
    $bad=[Diagnostics.ProcessStartInfo]::new($exe);$bad.UseShellExecute=$false
    $bad.ArgumentList.Add('--notification-uri');$bad.ArgumentList.Add('flowdesk-control://notification/'+([Guid]$probe.NoticeId).ToString('N'));$bad.ArgumentList.Add('--desktop-checks')
    $rejected=[Diagnostics.Process]::Start($bad)
    if(!$rejected.WaitForExit(10000) -or $rejected.ExitCode -ne 2){throw 'Injected command-line flags were not rejected.'}
    $success=$true
} finally {
    Get-Process FlowDesk.App -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $exe} | ForEach-Object {$_.CloseMainWindow() | Out-Null;if(!$_.WaitForExit(10000)){throw 'Activation check could not close FlowDesk.'}}
    $cleanup=Start-Process -FilePath $exe -ArgumentList '--cleanup-activation-probe' -PassThru
    if(!$cleanup.WaitForExit(10000) -or $cleanup.ExitCode -ne 0){throw 'Activation probe cleanup failed.'}
}
@{Complete=$true;Success=$success;ColdActivation=$true;WarmActivation=$true;SingleInstance=$true;InjectedFlagsRejected=$true;ProbeCleanedUp=$true;Method='Windows ShellExecute of the notification protocol; direct banner click is not automated'} | ConvertTo-Json | Set-Content (Join-Path $root '.cache/activation-results.json')
Get-Content -Raw (Join-Path $root '.cache/activation-results.json')
