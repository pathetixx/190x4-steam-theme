# ============================================================================
# 190x4 - LIVE DOM probe. Walks the rendered DOM of every Steam CEF target and
# reports, per target: body className, the visible elements that actually paint
# a background (class + color + size), and the green Play/Install button's class
# chain (detected by GREEN color, language-independent).
# Needs Steam running, library open, port 8080 up (Millennium opens it).
# Output: x4-dom.json on the Desktop. Send it back.
# Run: powershell -ExecutionPolicy Bypass -File "$env:USERPROFILE\Desktop\dom-probe.ps1"
# ============================================================================
$ErrorActionPreference = 'Stop'
$log = Join-Path ([Environment]::GetFolderPath('Desktop')) 'x4-dom-log.txt'
try { Start-Transcript -Path $log -Force | Out-Null } catch {}

# --- DOM-walk payload (ASCII only) ------------------------------------------
$expr = @'
(()=>{try{
 const out=[],seen=new Set();
 for(const el of document.querySelectorAll('*')){
  let r; try{r=el.getBoundingClientRect();}catch(e){continue;}
  if(r.width*r.height<60000)continue;
  const cs=getComputedStyle(el),bg=cs.backgroundColor,bi=cs.backgroundImage;
  const has=(bg&&bg!=='rgba(0, 0, 0, 0)'&&bg!=='transparent')||(bi&&bi!=='none');
  if(!has)continue;
  let c=el.className; if(c&&c.baseVal!==undefined)c=c.baseVal; c=String(c||'');
  const k=el.tagName+'|'+c; if(seen.has(k))continue; seen.add(k);
  out.push({t:el.tagName,c:c.slice(0,180),bg:bg,bi:bi.slice(0,46),w:Math.round(r.width),h:Math.round(r.height)});
  if(out.length>=220)break;
 }
 let play=null;
 for(const el of document.querySelectorAll('button,[role="button"],div,a')){
  let r; try{r=el.getBoundingClientRect();}catch(e){continue;}
  if(r.width<70||r.width>440||r.height<22||r.height>130)continue;
  const cs=getComputedStyle(el);
  const m=(cs.backgroundColor||'').match(/(\d+),\s*(\d+),\s*(\d+)/);
  const gi=cs.backgroundImage||'';
  const gm=gi.match(/(\d+),\s*(\d+),\s*(\d+)/);
  const green=(x)=>x&&(+x[2])>120&&(+x[2])>(+x[1])+25&&(+x[2])>(+x[3])+25;
  if(green(m)||(/. gradient|gradient/.test(gi)&&green(gm))){
   let pc=el.className; if(pc&&pc.baseVal!==undefined)pc=pc.baseVal;
   const pe=el.parentElement,ppe=pe&&pe.parentElement;
   play={c:String(pc||'').slice(0,200),bg:cs.backgroundColor,bi:gi.slice(0,90),
         p:String((pe&&pe.className)||'').slice(0,200),
         pp:String((ppe&&ppe.className)||'').slice(0,200)};
   break;
  }
 }
 return JSON.stringify({title:document.title,body:String((document.body&&document.body.className)||''),n:out.length,panels:out,play:play});
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
        if ($d.body -or $d.n -gt 0 -or $d.play) {
          $results += [pscustomobject]@{ target = $t.title; url = $t.url; data = $d }
          Write-Host ("   body='{0}'  panels={1}  play={2}" -f $d.body, $d.n, [bool]$d.play)
        }
      }
    } catch { Write-Host ("   skip: {0}" -f $_.Exception.Message) }
  }
  $out = Join-Path ([Environment]::GetFolderPath('Desktop')) 'x4-dom.json'
  ($results | ConvertTo-Json -Depth 12) | Out-File -Encoding utf8 $out
  Write-Host ""
  Write-Host "Saved: $out"
  Write-Host ">>> Send x4-dom.json back."
}
catch {
  Write-Host ""
  Write-Host ("ERROR: {0}" -f $_.Exception.Message) -ForegroundColor Red
}
finally {
  try { Stop-Transcript | Out-Null } catch {}
  Read-Host "Press Enter to close"
}
