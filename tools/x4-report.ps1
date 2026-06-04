# ============================================================================
# 190x4 - ALL-IN-ONE state report. Run with Millennium INSTALLED, theme
# selected, Steam running. Does NOT need the .cef file (Millennium opens 8080).
# Output: x4-report.txt on Desktop (send it back).
# Run: powershell -ExecutionPolicy Bypass -File "$env:USERPROFILE\Desktop\x4-report.ps1"
# ============================================================================
$ErrorActionPreference = 'Continue'
$out = Join-Path ([Environment]::GetFolderPath('Desktop')) 'x4-report.txt'
$lines = @()
function L($s){ $script:lines += $s; Write-Host $s }

$steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -EA SilentlyContinue).SteamPath
if (-not $steam) { $steam = 'C:\Program Files (x86)\Steam' }
$steam = $steam -replace '/','\'
L "Steam: $steam"

# 0. the startup-killer file
$cef = Join-Path $steam '.cef-enable-remote-debugging'
L ("[.cef-enable-remote-debugging] present: {0}" -f (Test-Path $cef))

# steam running?
$proc = Get-Process steam -EA SilentlyContinue
L ("[steam.exe running]: {0}" -f [bool]$proc)
L ""

$roots = @($env:USERPROFILE,$env:APPDATA,$env:LOCALAPPDATA,$env:PROGRAMDATA,$steam) |
  Select-Object -Unique | Where-Object { $_ -and (Test-Path $_) }

# 1. Millennium install + version
L "=== Millennium folders / version ==="
$mfound = $false
foreach ($r in $roots) {
  Get-ChildItem -Path $r -Recurse -Directory -EA SilentlyContinue -Depth 4 |
    Where-Object { $_.Name -match 'millennium' } | ForEach-Object { $mfound=$true; L (" {0}" -f $_.FullName) }
}
Get-ChildItem -Path $steam -File -EA SilentlyContinue | Where-Object { $_.Name -match 'millennium' } |
  ForEach-Object { $mfound=$true; L (" {0}  ({1} bytes)" -f $_.FullName, $_.Length) }
if (-not $mfound) { L " (no millennium folder/file found -> Millennium NOT installed)" }
L ""

# 2. all skin.json (where the theme really is)
L "=== ALL skin.json ==="
$any=$false
foreach ($r in $roots) {
  Get-ChildItem -Path $r -Recurse -Filter 'skin.json' -File -EA SilentlyContinue -Depth 6 | ForEach-Object {
    $any=$true; $n='?';$v='?'
    try { $j=Get-Content $_.FullName -Raw -EA SilentlyContinue|ConvertFrom-Json; $n=$j.name;$v=$j.version } catch {}
    L (" {0}  [name={1} ver={2}]" -f $_.FullName,$n,$v)
  }
}
if (-not $any) { L " (none found)" }
L ""

# 3. active theme config + millennium log
L "=== Millennium config (active theme) ==="
foreach ($r in $roots) {
  Get-ChildItem -Path $r -Recurse -File -EA SilentlyContinue -Depth 5 |
    Where-Object { $_.FullName -match 'millennium' -and $_.Name -match '\.json$' } |
    Select-Object -First 8 | ForEach-Object {
      $t = Get-Content $_.FullName -Raw -EA SilentlyContinue
      if ($t -match 'theme|active|190x4|skin') { L " --- $($_.FullName) ---"; L ($t.Substring(0,[Math]::Min(600,$t.Length))); L "" }
    }
}
L "=== Millennium log tail ==="
foreach ($r in $roots) {
  Get-ChildItem -Path $r -Recurse -File -EA SilentlyContinue -Depth 5 |
    Where-Object { $_.FullName -match 'millennium' -and $_.Extension -match '\.(log|txt)$' } |
    Sort-Object LastWriteTime -Desc | Select-Object -First 2 | ForEach-Object {
      L " --- $($_.FullName) ---"; Get-Content $_.FullName -Tail 30 -EA SilentlyContinue | ForEach-Object { L "   $_" }; L ""
    }
}

# 4. live injection probe (only if 8080 up)
L "=== injection probe (localhost:8080) ==="
$targets = $null
try { $targets = Invoke-RestMethod 'http://localhost:8080/json' -TimeoutSec 5 } catch { L " 8080 NOT responding: $($_.Exception.Message)" }
if ($targets) {
  $expr = @'
(() => { const r={}; try{r.t=document.title}catch(e){} r.wp=typeof window.webpackChunksteamui; r.x4=typeof window.x4;
    try{const c=getComputedStyle(document.body);r.font=c.fontFamily;r.bg=c.backgroundColor;}catch(e){}
    try{r.bg0=getComputedStyle(document.documentElement).getPropertyValue('--x4-bg-0').trim();}catch(e){}
    try{r.dx=document.querySelectorAll('[data-x4]').length;}catch(e){}
    try{r.st=[...document.querySelectorAll('style')].filter(s=>/--x4-|data-x4|190x4/.test(s.textContent||'')).length;}catch(e){}
    return JSON.stringify(r); })()
'@
  foreach ($t in $targets) {
    if (-not $t.webSocketDebuggerUrl) { continue }
    $val=$null
    try {
      $cts=New-Object Threading.CancellationTokenSource 8000   # 8s hard timeout per target
      $ct=$cts.Token
      $ws=[System.Net.WebSockets.ClientWebSocket]::new()
      $ws.ConnectAsync([Uri]$t.webSocketDebuggerUrl,$ct).Wait()
      $pl=@{id=1;method='Runtime.evaluate';params=@{expression=$expr;awaitPromise=$true;returnByValue=$true}}|ConvertTo-Json -Depth 8 -Compress
      $b=[Text.Encoding]::UTF8.GetBytes($pl)
      $ws.SendAsync([ArraySegment[byte]]::new($b),'Text',$true,$ct).Wait()
      $rb=New-Object byte[] 131072
      for($i=0;$i -lt 30 -and -not $val;$i++){ $sb=New-Object Text.StringBuilder
        do{$seg=[ArraySegment[byte]]::new($rb);$rr=$ws.ReceiveAsync($seg,$ct);$rr.Wait();[void]$sb.Append([Text.Encoding]::UTF8.GetString($rb,0,$rr.Result.Count))}while(-not $rr.Result.EndOfMessage)
        $o=$sb.ToString()|ConvertFrom-Json; if($o.id -eq 1){$val=$o.result.result.value} }
      $ws.Dispose()
    } catch { L (" [{0}] eval err/timeout: {1}" -f $t.title,$_.Exception.Message) }
    if ($val) { $d=$val|ConvertFrom-Json
      L ("  [{0,-22}] wp={1} x4={2} dataX4={3} font={4} bg={5} bg0={6} ourStyle={7}" -f $t.title,$d.wp,$d.x4,$d.dx,($d.font -replace '"',''),$d.bg,$d.bg0,$d.st) }
  }
}

$lines | Out-File -Encoding utf8 $out
Write-Host ""
Write-Host "Saved: $out  (send it back)"
Read-Host "Press Enter to close"
