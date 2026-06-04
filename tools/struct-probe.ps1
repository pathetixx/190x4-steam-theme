# ============================================================================
# 190x4 - STRUCTURE probe. Dumps a readable DOM outline of the main Steam window
# (top bar + game-details view): per node -> depth, tag, FULL className, the
# element's own short text label, and paint info (bg / text color / size).
# Lets us map signature design elements (section-header ticks, nav tabs, stat
# blocks, progress bar, the top-left logo slot) onto REAL Steam classes in ONE
# pass, instead of per-hash guessing.
# Needs Steam running, a GAME PAGE open in Library (click a game), port 8080 up.
# Output: x4-struct.json on the Desktop. Send it back.
# Run: powershell -ExecutionPolicy Bypass -File C:\Users\Administrator\Desktop\struct-probe.ps1
# ============================================================================
$ErrorActionPreference = 'Stop'
$log = Join-Path ([Environment]::GetFolderPath('Desktop')) 'x4-struct-log.txt'
try { Start-Transcript -Path $log -Force | Out-Null } catch {}

# --- payload: depth-walk, keep nodes that are a label OR paint a surface ------
$expr = @'
(()=>{try{
 const out=[];
 const ownText=(el)=>{let s='';for(const n of el.childNodes){if(n.nodeType===3)s+=n.nodeValue;}return s.trim();};
 const depth=(el)=>{let d=0,p=el;while(p&&p!==document.body){p=p.parentElement;d++;}return d;};
 let i=0;
 for(const el of document.querySelectorAll('body *')){
  if(i>900)break; i++;
  let r; try{r=el.getBoundingClientRect();}catch(e){continue;}
  if(r.width<2||r.height<2)continue;            // invisible
  const cs=getComputedStyle(el);
  if(cs.visibility==='hidden'||cs.display==='none')continue;
  const t=ownText(el).slice(0,42);
  const bg=cs.backgroundColor, bi=cs.backgroundImage;
  const paints=(bg&&bg!=='rgba(0, 0, 0, 0)'&&bg!=='transparent')||(bi&&bi!=='none');
  const border=(cs.borderTopWidth!=='0px'||cs.borderLeftWidth!=='0px');
  // keep: has a text label, or paints a surface, or has a visible border
  if(!t&&!paints&&!border)continue;
  let c=el.className; if(c&&c.baseVal!==undefined)c=c.baseVal; c=String(c||'');
  out.push({
   d:depth(el), tag:el.tagName, c:c.slice(0,170),
   t:t, paints:paints?1:0, bg:bg,
   bi:(bi&&bi!=='none')?bi.slice(0,40):'',
   col:cs.color, fs:cs.fontSize, fw:cs.fontWeight, tt:cs.textTransform,
   w:Math.round(r.width), h:Math.round(r.height), x:Math.round(r.left), y:Math.round(r.top)
  });
 }
 return JSON.stringify({title:document.title,body:String((document.body&&document.body.className)||''),n:out.length,nodes:out});
}catch(e){return JSON.stringify({err:String(e)})}})()
'@

function Probe-Target($wsUrl) {
  $ws = [System.Net.WebSockets.ClientWebSocket]::new()
  $cts = New-Object Threading.CancellationTokenSource 12000
  $ct = $cts.Token
  $ws.ConnectAsync([Uri]$wsUrl, $ct).Wait()
  $payload = @{ id = 1; method = 'Runtime.evaluate'; params = @{ expression = $expr; awaitPromise = $true; returnByValue = $true } } | ConvertTo-Json -Depth 8 -Compress
  $buf = [Text.Encoding]::UTF8.GetBytes($payload)
  $ws.SendAsync([ArraySegment[byte]]::new($buf), 'Text', $true, $ct).Wait()
  $recv = New-Object byte[] 262144
  $val = $null
  for ($i = 0; $i -lt 80 -and -not $val; $i++) {
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
    if ($t.title -ne 'Steam') { continue }   # main window only
    Write-Host ("probing [{0}] {1}" -f $t.title, $t.url)
    try {
      $v = Probe-Target $t.webSocketDebuggerUrl
      if ($v) {
        $d = $v | ConvertFrom-Json
        $results += [pscustomobject]@{ target = $t.title; url = $t.url; data = $d }
        Write-Host ("   body='{0}'  nodes={1}" -f $d.body, $d.n)
      }
    } catch { Write-Host ("   skip: {0}" -f $_.Exception.Message) }
  }
  $out = Join-Path ([Environment]::GetFolderPath('Desktop')) 'x4-struct.json'
  ($results | ConvertTo-Json -Depth 12) | Out-File -Encoding utf8 $out
  Write-Host ""
  Write-Host "Saved: $out"
  Write-Host ">>> Send x4-struct.json back."
}
catch {
  Write-Host ""
  Write-Host ("ERROR: {0}" -f $_.Exception.Message) -ForegroundColor Red
}
finally {
  try { Stop-Transcript | Out-Null } catch {}
  Read-Host "Press Enter to close"
}
