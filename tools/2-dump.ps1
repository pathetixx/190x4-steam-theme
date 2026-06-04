# ============================================================================
# 190x4 - dump Steam class registry via CDP (websocket) and save to file.
# Needs: Steam running with CEF debugging (.cef-enable-remote-debugging),
#        library fully loaded, port 8080 open.
# Output: classmap.json on the Desktop.
# Run:  powershell -ExecutionPolicy Bypass -File "$env:USERPROFILE\Desktop\2-dump.ps1"
# ============================================================================
$ErrorActionPreference = 'Stop'

$log = Join-Path ([Environment]::GetFolderPath('Desktop')) 'classmap-log.txt'
try { Start-Transcript -Path $log -Force | Out-Null } catch {}

try {

# JS: collect {ClassName: hash} from Steam webpack, return as JSON string
$expr = @'
(() => {
  const wp = window.webpackChunksteamui;
  if (!wp) return JSON.stringify({ __error: "no webpack" });
  let req; wp.push([[Symbol("x4")], {}, (r) => { req = r; }]);
  const seen = new Set(), list = [];
  for (const id of Object.keys(req.m)) {
    let mod; try { mod = req(id); } catch (e) { continue; }
    if (!mod) continue;
    for (const m of [mod.default, mod]) {
      if (typeof m !== "object" || m === null || m.__esModule || seen.has(m)) continue;
      const k = Object.keys(m);
      if (!k.length) continue;
      if (k.length === 1 && m.version) continue;
      if (k.length > 1000 && m.AboutSettings) continue;
      if (k.every(x => typeof m[x] === "string" && !Object.getOwnPropertyDescriptor(m, x)?.get)) {
        seen.add(m); list.push(m);
      }
    }
  }
  return JSON.stringify(Object.assign({}, ...list));
})()
'@

# 1. find SharedJSContext target (webpackChunksteamui lives there)
try {
  $targets = Invoke-RestMethod 'http://localhost:8080/json'
} catch {
  throw "localhost:8080 not responding. Steam running with debugging and library loaded?"
}
$t = $targets | Where-Object { $_.title -eq 'SharedJSContext' } | Select-Object -First 1
if (-not $t) { $t = $targets | Where-Object { $_.url -match 'SharedJSContext' } | Select-Object -First 1 }
if (-not $t) {
  Write-Host "SharedJSContext not found. Available targets:"
  $targets | ForEach-Object { Write-Host " - [$($_.title)] $($_.url)" }
  throw "target not found"
}
Write-Host "Target: $($t.title)"

# 2. websocket connect
$ws = [System.Net.WebSockets.ClientWebSocket]::new()
$ws.ConnectAsync([Uri]$t.webSocketDebuggerUrl, [Threading.CancellationToken]::None).Wait()

# 3. Runtime.evaluate
$payload = @{
  id     = 1
  method = 'Runtime.evaluate'
  params = @{ expression = $expr; awaitPromise = $true; returnByValue = $true }
} | ConvertTo-Json -Depth 8 -Compress
$buf = [Text.Encoding]::UTF8.GetBytes($payload)
$ws.SendAsync([ArraySegment[byte]]::new($buf), 'Text', $true, [Threading.CancellationToken]::None).Wait()

# 4. read frames until reply with id:1
$ct   = [Threading.CancellationToken]::None
$recv = New-Object byte[] 131072
$json = $null
for ($i = 0; $i -lt 50 -and -not $json; $i++) {
  $sb = New-Object Text.StringBuilder
  do {
    $seg = [ArraySegment[byte]]::new($recv)
    $r = $ws.ReceiveAsync($seg, $ct); $r.Wait()
    [void]$sb.Append([Text.Encoding]::UTF8.GetString($recv, 0, $r.Result.Count))
  } while (-not $r.Result.EndOfMessage)
  $o = $sb.ToString() | ConvertFrom-Json
  if ($o.id -eq 1) {
    if ($o.error) { throw "CDP error: $($o.error.message)" }
    $json = $o.result.result.value
  }
}
$ws.Dispose()
if (-not $json) { throw "no reply from Steam" }

# 5. save to Desktop
$out = Join-Path ([Environment]::GetFolderPath('Desktop')) 'classmap.json'
$json | Out-File -Encoding utf8 $out
$count = ([regex]::Matches($json, '":"')).Count
Write-Host ""
Write-Host "OK - classes: $count"
Write-Host "Saved: $out"
Write-Host ">>> Send classmap.json back."

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
