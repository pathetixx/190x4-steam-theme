# 190x4 Bloodmoon for Steam: applies the theme to every Steam window over the local debug port.
# No hooks and no files changed in Steam. Close this script and restart Steam to get the stock look.
#   powershell -File x4-steam-bloodmoon.ps1            keep running
#   powershell -File x4-steam-bloodmoon.ps1 -Once      one pass and exit
#   powershell -File x4-steam-bloodmoon.ps1 -Remove    remove the theme from open windows
param([switch]$Once, [switch]$Remove, [int]$Port = 8080, [string]$LogFile = "")
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Web.Extensions
$json = New-Object System.Web.Script.Serialization.JavaScriptSerializer
$json.MaxJsonLength = [int]::MaxValue
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$utf8 = New-Object System.Text.UTF8Encoding($false)
function Read-Text($name) { [IO.File]::ReadAllText((Join-Path $root $name), $utf8) }
function Log($m) { if ($LogFile) { Add-Content -Path $LogFile -Value ((Get-Date -Format "HH:mm:ss") + " " + $m) } }

$mutex = New-Object System.Threading.Mutex($false, "x4-steam-bloodmoon")
if (-not $Remove -and -not $mutex.WaitOne(0)) { exit 0 }

function Read-Theme {
  $script:clientRaw = Read-Text "client.css"
  $script:webCss = Read-Text "web.css"
  $script:resolver = Read-Text "resolver.js"
  $script:recolor = Read-Text "recolor.js"
  $script:stamp = (Get-ChildItem -Path $root -Include *.css, *.js -Recurse | Measure-Object -Property LastWriteTimeUtc -Maximum).Maximum
}
Read-Theme
$placeJs = '(function(c){var s=document.getElementById("x4bloodmoon");if(!s){s=document.createElement("style");s.id="x4bloodmoon";(document.head||document.documentElement).appendChild(s);}if(s.textContent!==c)s.textContent=c;window.__x4bloodmoon=1;return 1;})'
$clearJs = '(function(){var s=document.getElementById("x4bloodmoon");if(s)s.remove();if(window.__x4recolor)window.__x4recolor.revert();delete window.__x4bloodmoon;return 1;})()'

$popupHook = @'
(function(){
  function apply(w){
    try{
      if(!w||!w.document)return;
      var d=w.document;
      if(!d.getElementById("x4bloodmoon")){var s=d.createElement("style");s.id="x4bloodmoon";s.textContent=window.__x4bm.css;(d.head||d.documentElement).appendChild(s);w.__x4bloodmoon=1;}
      w.eval(window.__x4bm.code);
    }catch(e){}
  }
  if(window.__x4bmhooked)return;
  window.__x4bmhooked=1;
  g_PopupManager.AddPopupCreatedCallback(function(p){apply(p.window||p.m_popup);});
})();
'@

function Invoke-Page($wsUrl, $expression) {
  $ws = New-Object System.Net.WebSockets.ClientWebSocket
  $cts = New-Object System.Threading.CancellationTokenSource(20000)
  try {
    $ws.ConnectAsync([Uri]$wsUrl, $cts.Token).Wait()
    $msg = $json.Serialize(@{ id = 1; method = "Runtime.evaluate"; params = @{ expression = $expression; returnByValue = $true; awaitPromise = $true } })
    $bytes = $utf8.GetBytes($msg)
    $ws.SendAsync((New-Object ArraySegment[byte] -ArgumentList @(,$bytes)), [System.Net.WebSockets.WebSocketMessageType]::Text, $true, $cts.Token).Wait()
    $buf = New-Object byte[] 65536
    while ($true) {
      $ms = New-Object IO.MemoryStream
      do {
        $task = $ws.ReceiveAsync((New-Object ArraySegment[byte] -ArgumentList @(,$buf)), $cts.Token)
        $task.Wait()
        $ms.Write($buf, 0, $task.Result.Count)
      } while (-not $task.Result.EndOfMessage)
      $m = $json.DeserializeObject($utf8.GetString($ms.ToArray()))
      if ($m["id"] -eq 1) {
        if ($m["result"] -and $m["result"]["result"]) { return $m["result"]["result"]["value"] }
        return $null
      }
    }
  } finally { $ws.Dispose(); $cts.Dispose() }
}

# Sent over a connection that stays open: the browser keeps the script only while we are attached.
function Send-Keep($ws, [int]$id, [string]$source) { Send-Raw $ws (@{ id = $id; method = "Page.addScriptToEvaluateOnNewDocument"; params = @{ source = $source } }) }

function Send-Raw($ws, $obj) {
  $bytes = $utf8.GetBytes($json.Serialize($obj))
  $ws.SendAsync((New-Object ArraySegment[byte] -ArgumentList @(,$bytes)), [System.Net.WebSockets.WebSocketMessageType]::Text, $true, [Threading.CancellationToken]::None).Wait()
}

function Get-Targets {
  $req = [Net.WebRequest]::Create("http://127.0.0.1:$Port/json")
  $req.Timeout = 3000
  $req.Proxy = $null
  $res = $req.GetResponse()
  try {
    $text = (New-Object IO.StreamReader($res.GetResponseStream(), $utf8)).ReadToEnd()
    return $json.DeserializeObject($text)
  } finally { $res.Close() }
}

function Resolve-Client($shared) {
  [string[]]$tokens = @([regex]::Matches($clientRaw, "\{\{([^}]+)\}\}") | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
  [void](Invoke-Page $shared $resolver)
  $map = $json.DeserializeObject((Invoke-Page $shared ("__x4resolve(" + $json.Serialize($tokens) + ")")))
  return [regex]::Replace($clientRaw, "\{\{([^}]+)\}\}", {
    param($m)
    $h = @($map[$m.Groups[1].Value])
    if ($h.Count -eq 0) { ".x4-missing" }
    elseif ($h.Count -eq 1) { "." + $h[0] }
    else { ":is(" + (($h | ForEach-Object { "." + $_ }) -join ",") + ")" }
  })
}

$seen = @{}        # target id -> url it was themed at
$webCache = @{}    # stylesheet url -> recolored text
$client = $null
$sharedId = ""
$keep = @{}        # web target id -> open connection
$drain = @{}       # web target id -> pending read

function Get-WebBoot {
  return "(function(){if(window!==top)return;var c=" + $json.Serialize($webCss) + ";function put(){var s=document.createElement('style');s.id='x4bloodmoon';s.textContent=c;(document.head||document.documentElement).appendChild(s);window.__x4bloodmoon=1;}if(document.documentElement){put();}else{var o=new MutationObserver(function(){if(document.documentElement){o.disconnect();put();}});o.observe(document,{childList:true});}})();" + $recolor
}

function Keep-Web($id, $wsUrl) {
  if ($keep.ContainsKey($id)) { return }
  $ws = New-Object System.Net.WebSockets.ClientWebSocket
  $ws.ConnectAsync([Uri]$wsUrl, [Threading.CancellationToken]::None).Wait()
  $keep[$id] = $ws
  # new-document scripts only run while the Page domain is enabled
  Send-Raw $ws (@{ id = 900000; method = "Page.enable"; params = @{} })
  Send-Keep $ws 1 (Get-WebBoot)
  $n = 2
  foreach ($h in @($webCache.Keys)) { Send-Keep $ws $n (Get-CacheScript $h); $n++ }
}

# Page events keep coming over those connections; read them away so the socket never fills up.
function Drain-Keep {
  foreach ($k in @($keep.Keys)) {
    try {
      $st = $drain[$k]
      while ($true) {
        if ($st -and -not $st.Task.IsCompleted) { break }
        if (-not $st) { $st = @{ Buf = New-Object byte[] 65536 } }
        $st.Task = $keep[$k].ReceiveAsync((New-Object ArraySegment[byte] -ArgumentList @(,$st.Buf)), [Threading.CancellationToken]::None)
        $drain[$k] = $st
        if (-not $st.Task.IsCompleted) { break }
      }
    } catch { $drain.Remove($k) }
  }
}

function Get-CacheScript($href) {
  return "if(window===top){(window.__x4c=window.__x4c||{})[" + $json.Serialize($href) + "]=" + $json.Serialize($webCache[$href]) + ";}"
}

function Sync-Web($wsUrl) {
  $hrefs = $json.DeserializeObject((Invoke-Page $wsUrl "window.__x4recolor?JSON.stringify(window.__x4recolor.pending()):'[]'"))
  foreach ($href in @($hrefs)) {
    if (-not $href) { continue }
    if ($webCache.ContainsKey($href)) {
      [void](Invoke-Page $wsUrl ("window.__x4recolor.addRaw(" + $json.Serialize($href) + "," + $json.Serialize($webCache[$href]) + ")"))
    } else {
      $wc = New-Object Net.WebClient
      $wc.Encoding = $utf8
      $text = $wc.DownloadString($href)
      $wc.Dispose()
      $webCache[$href] = Invoke-Page $wsUrl ("window.__x4recolor.addText(" + $json.Serialize($href) + "," + $json.Serialize($text) + ")")
      $src = Get-CacheScript $href
      foreach ($k in @($keep.Keys)) { try { Send-Keep $keep[$k] (Get-Random -Minimum 100 -Maximum 100000) $src } catch { Log "keep send failed" } }
    }
  }
}

while ($true) {
  try {
    # the theme files changed: take them off the windows and apply the new ones
    $now = (Get-ChildItem -Path $root -Include *.css, *.js -Recurse | Measure-Object -Property LastWriteTimeUtc -Maximum).Maximum
    if ($now -ne $stamp) {
      foreach ($t in @(Get-Targets)) { if ($t["type"] -eq "page" -and $t["title"] -ne "SharedJSContext" -and $seen.ContainsKey($t["id"])) { try { [void](Invoke-Page $t["webSocketDebuggerUrl"] $clearJs) } catch {} } }
      Read-Theme
      $client = $null; $seen = @{}; $webCache = @{}
      foreach ($k in @($keep.Keys)) { try { $keep[$k].Dispose() } catch {} }
      $keep = @{}; $drain = @{}
      Log "theme files reloaded"
    }
    $targets = @(Get-Targets)
    $shared = $targets | Where-Object { $_["title"] -eq "SharedJSContext" } | Select-Object -First 1
    if ($shared) {
      if ($shared["id"] -ne $sharedId) { $sharedId = $shared["id"]; $client = $null; $seen = @{} }
      if (-not $Remove -and -not $client) {
        $client = Resolve-Client $shared["webSocketDebuggerUrl"]
        Log "resolved classes"
        # Steam reports every new window here before showing it: the theme gets in ahead of the first paint.
        [void](Invoke-Page $shared["webSocketDebuggerUrl"] ("window.__x4bm={css:" + $json.Serialize($client) + ",code:" + $json.Serialize($recolor) + "};" + $popupHook))
      }
      $alive = @{}
      foreach ($t in $targets) { $alive[$t["id"]] = 1 }
      foreach ($k in @($keep.Keys)) { if (-not $alive[$k]) { try { $keep[$k].Dispose() } catch {} ; $keep.Remove($k); $drain.Remove($k) } }
      Drain-Keep
      foreach ($t in $targets) {
        if ($t["type"] -ne "page" -or $t["id"] -eq $sharedId) { continue }
        $url = [string]$t["url"]
        $isClient = $url.StartsWith("about:blank")
        $isWeb = $url -match "^https://([a-z0-9-]+\.)?(steampowered|steamcommunity)\.com"
        if (-not $isClient -and -not $isWeb) { continue }
        $ws = $t["webSocketDebuggerUrl"]
        try {
          if ($Remove) { [void](Invoke-Page $ws $clearJs); continue }
          $fresh = $seen[$t["id"]] -ne $url
          # a web page can reload at the same address and lose the theme
          if (-not $fresh -and $isWeb) { $fresh = -not (Invoke-Page $ws "!!window.__x4bloodmoon") }
          if ($fresh) {
            $css = $(if ($isClient) { $client } else { $webCss })
            [void](Invoke-Page $ws ($placeJs + "(" + $json.Serialize($css) + ")"))
            [void](Invoke-Page $ws $recolor)
            $seen[$t["id"]] = $url
            Log ("themed " + $t["id"])
          }
          # stylesheets of a web page keep arriving after it opens
          if ($isWeb) { Keep-Web $t["id"] $ws; Sync-Web $ws }
        } catch { Log ("target failed: " + $_.Exception.Message) }
      }
    }
  } catch {
    $client = $null
    $sharedId = ""
    Log ("waiting for Steam: " + $_.Exception.Message)
    if (-not $Once -and -not $Remove) { Start-Sleep -Seconds 5; continue }
  }
  if ($Once -or $Remove) { break }
  Start-Sleep -Milliseconds 800
}
