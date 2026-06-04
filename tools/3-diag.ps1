# ============================================================================
# 190x4 - deep diagnostic: probe EVERY Steam CDP target and report whether our
# theme (CSS vars, font, data-x4, webpack registry, logo) is actually present.
# Needs CEF debugging on (port 8080). Theme v0.4.0 enabled in Millennium.
# Output: diag.json on Desktop + readable summary in the window.
# Run: powershell -ExecutionPolicy Bypass -File "$env:USERPROFILE\Desktop\3-diag.ps1"
# ============================================================================
$ErrorActionPreference = 'Stop'
$log = Join-Path ([Environment]::GetFolderPath('Desktop')) 'diag-log.txt'
try { Start-Transcript -Path $log -Force | Out-Null } catch {}
try {

# expression evaluated inside each target
$expr = @'
(() => {
  const r = {};
  try { r.title = document.title; } catch(e){}
  r.hasWebpack = typeof window.webpackChunksteamui;
  r.x4 = typeof window.x4;
  try { r.x4classes = window.x4 ? Object.keys(window.x4.classMap).length : 0; } catch(e){ r.x4classes = "err"; }
  try { r.x4targets = window.x4 ? window.x4.targets() : null; } catch(e){ r.x4targets = "err"; }
  try {
    const cs = getComputedStyle(document.body);
    r.bodyFont = cs.fontFamily;
    r.bodyBg = cs.backgroundColor;
  } catch(e){}
  try {
    const root = getComputedStyle(document.documentElement);
    r.varBg0 = root.getPropertyValue('--x4-bg-0').trim();
    r.varAccent = root.getPropertyValue('--x4-accent').trim();
    r.varSysAccent = root.getPropertyValue('--SystemAccentColor').trim();
  } catch(e){}
  try { r.dataX4 = document.querySelectorAll('[data-x4]').length; } catch(e){}
  try { r.logo = !!document.getElementById('x4-logo'); } catch(e){}
  try {
    r.ourStyleTags = [...document.querySelectorAll('style')]
      .filter(s => /--x4-|data-x4|190x4/.test(s.textContent || '')).length;
  } catch(e){}
  try {
    r.sheets = [...document.styleSheets].map(s => { try { return s.href; } catch(e){ return null; } })
      .filter(h => h && /millennium|190x4|client|colors|webkit/i.test(h));
  } catch(e){}
  return JSON.stringify(r);
})()
'@

function Eval-Target($wsUrl) {
  $ws = [System.Net.WebSockets.ClientWebSocket]::new()
  $ws.ConnectAsync([Uri]$wsUrl, [Threading.CancellationToken]::None).Wait()
  $payload = @{ id=1; method='Runtime.evaluate'; params=@{ expression=$expr; awaitPromise=$true; returnByValue=$true } } |
    ConvertTo-Json -Depth 8 -Compress
  $buf = [Text.Encoding]::UTF8.GetBytes($payload)
  $ws.SendAsync([ArraySegment[byte]]::new($buf), 'Text', $true, [Threading.CancellationToken]::None).Wait()
  $ct = [Threading.CancellationToken]::None
  $recv = New-Object byte[] 131072
  $val = $null
  for ($i=0; $i -lt 40 -and -not $val; $i++) {
    $sb = New-Object Text.StringBuilder
    do {
      $seg = [ArraySegment[byte]]::new($recv)
      $rr = $ws.ReceiveAsync($seg, $ct); $rr.Wait()
      [void]$sb.Append([Text.Encoding]::UTF8.GetString($recv, 0, $rr.Result.Count))
    } while (-not $rr.Result.EndOfMessage)
    $o = $sb.ToString() | ConvertFrom-Json
    if ($o.id -eq 1) { $val = $o.result.result.value }
  }
  $ws.Dispose()
  return $val
}

$targets = Invoke-RestMethod 'http://localhost:8080/json'
$report = @()
foreach ($t in $targets) {
  if (-not $t.webSocketDebuggerUrl) { continue }
  Write-Host "--- probing: [$($t.title)]"
  $v = $null
  try { $v = Eval-Target $t.webSocketDebuggerUrl } catch { Write-Host "   (eval failed: $($_.Exception.Message))" }
  if ($v) {
    $d = $v | ConvertFrom-Json
    $report += [pscustomobject]@{ target=$t.title; data=$d }
    Write-Host ("   webpack={0} x4={1} classes={2} dataX4={3} font={4} bg={5} varBg0={6} sysAccent={7} ourStyle={8} logo={9}" -f `
      $d.hasWebpack, $d.x4, $d.x4classes, $d.dataX4, ($d.bodyFont -replace '"',''), $d.bodyBg, $d.varBg0, $d.varSysAccent, $d.ourStyleTags, $d.logo)
  }
}
$out = Join-Path ([Environment]::GetFolderPath('Desktop')) 'diag.json'
$report | ConvertTo-Json -Depth 8 | Out-File -Encoding utf8 $out
Write-Host ""
Write-Host "Saved: $out  (send this file back)"

}
catch {
  Write-Host ""
  Write-Host "ERROR:" -ForegroundColor Red
  Write-Host $_.Exception.Message -ForegroundColor Red
}
finally {
  try { Stop-Transcript | Out-Null } catch {}
  Write-Host ""
  Write-Host "Log: $log"
  Read-Host "Press Enter to close"
}
