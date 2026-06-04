# ============================================================================
# 190x4 - CSS VARIABLE probe. Dumps every CSS custom property (--token) and its
# computed value from :root, <body> and a few major painted containers, across
# every Steam CEF target. These are Steam's design tokens; overriding them in
# :root recolors the whole UI at once (durable, survives class-hash churn).
# Needs Steam running, library open, port 8080 up (Millennium opens it).
# Output: x4-vars.json on the Desktop. Send it back.
# Run: powershell -ExecutionPolicy Bypass -File "$env:USERPROFILE\Desktop\var-probe.ps1"
# ============================================================================
$ErrorActionPreference = 'Stop'
$log = Join-Path ([Environment]::GetFolderPath('Desktop')) 'x4-vars-log.txt'
try { Start-Transcript -Path $log -Force | Out-Null } catch {}

# --- payload: collect custom props from styleSheets rules + computed values --
$expr = @'
(()=>{try{
 const props=new Set();
 // 1) harvest --names declared in any same-origin stylesheet
 for(const ss of document.styleSheets){
  let rules; try{rules=ss.cssRules;}catch(e){continue;}
  if(!rules)continue;
  for(const r of rules){
   const st=r.style; if(!st)continue;
   for(let i=0;i<st.length;i++){const p=st[i]; if(p&&p.startsWith('--'))props.add(p);}
  }
 }
 const root=document.documentElement, rs=getComputedStyle(root);
 const bs=document.body?getComputedStyle(document.body):null;
 const collect=(cs)=>{const o={}; if(!cs)return o;
   for(const p of props){const v=(cs.getPropertyValue(p)||'').trim(); if(v)o[p]=v;} return o;};
 // also scan declared names for color-ish values
 const rootVars=collect(rs), bodyVars=collect(bs);
 // keep only entries that look like colors or are blue-ish, plus all background/color named
 const isColor=(v)=>/^#|rgb|hsl/i.test(v);
 const filt=(o)=>{const x={}; for(const k in o){const v=o[k];
   if(isColor(v)||/color|bg|background|accent|highlight|focus|tint|brand|primary/i.test(k))x[k]=v;} return x;};
 return JSON.stringify({title:document.title,total:props.size,
   root:filt(rootVars),body:filt(bodyVars)});
}catch(e){return JSON.stringify({err:String(e)})}})()
'@

function Probe-Target($wsUrl) {
  $ws = [System.Net.WebSockets.ClientWebSocket]::new()
  $cts = New-Object Threading.CancellationTokenSource 10000
  $ct = $cts.Token
  $ws.ConnectAsync([Uri]$wsUrl, $ct).Wait()
  $payload = @{ id = 1; method = 'Runtime.evaluate'; params = @{ expression = $expr; awaitPromise = $true; returnByValue = $true } } | ConvertTo-Json -Depth 8 -Compress
  $buf = [Text.Encoding]::UTF8.GetBytes($payload)
  $ws.SendAsync([ArraySegment[byte]]::new($buf), 'Text', $true, $ct).Wait()
  $recv = New-Object byte[] 262144
  $val = $null
  for ($i = 0; $i -lt 60 -and -not $val; $i++) {
    $sb = New-Object Text.StringBuilder
    do {
      $seg = [ArraySegment[byte]]::new($recv)
      $r = $ws.ReceiveAsync($seg, $ct); $r.Wait()
      [void]$sb.Append([Text.Encoding]::UTF8.GetString($recv, 0, $r.Result.Count))
    } while (-not $r.Result.EndOfMessage)
    $o = $sb.ToString() | ConvertFrom-Json
    if ($o.id -eq 1) { if ($o.error) { throw $o.error.message }; $val = $o.result.result.value }
  }
  $ws.Dispose()
  return $val
}

try {
  $targets = Invoke-RestMethod 'http://localhost:8080/json'
  $results = @()
  foreach ($t in $targets) {
    if (-not $t.webSocketDebuggerUrl) { continue }
    Write-Host ("probing [{0}] {1}" -f $t.title, $t.url)
    try {
      $v = Probe-Target $t.webSocketDebuggerUrl
      if ($v) {
        $d = $v | ConvertFrom-Json
        $rc = if ($d.root) { ($d.root.PSObject.Properties | Measure-Object).Count } else { 0 }
        if ($rc -gt 0 -or ($d.body -and ($d.body.PSObject.Properties | Measure-Object).Count -gt 0)) {
          $results += [pscustomobject]@{ target = $t.title; url = $t.url; data = $d }
          Write-Host ("   total={0}  colorVars={1}" -f $d.total, $rc)
        }
      }
    } catch { Write-Host ("   skip: {0}" -f $_.Exception.Message) }
  }
  $out = Join-Path ([Environment]::GetFolderPath('Desktop')) 'x4-vars.json'
  ($results | ConvertTo-Json -Depth 12) | Out-File -Encoding utf8 $out
  Write-Host ""
  Write-Host "Saved: $out"
  Write-Host ">>> Send x4-vars.json back."
}
catch {
  Write-Host ""
  Write-Host ("ERROR: {0}" -f $_.Exception.Message) -ForegroundColor Red
}
finally {
  try { Stop-Transcript | Out-Null } catch {}
  Read-Host "Press Enter to close"
}
