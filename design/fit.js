/* Fit a fixed-size frame (.x4-fit) into the viewport, letterboxed on black.
   Wrap your 1920×1080 (or window) root in <div class="x4-fit"> ... </div>. */
(function(){
  function fit(){
    document.querySelectorAll('.x4-fit').forEach(stage=>{
      const el = stage.firstElementChild; if(!el) return;
      const w = el.offsetWidth, h = el.offsetHeight;
      if(!w||!h) return;
      const s = Math.min(window.innerWidth/w, window.innerHeight/h);
      el.style.transformOrigin='top left';
      el.style.transform = `scale(${s})`;
      stage.style.width = window.innerWidth+'px';
      stage.style.height = window.innerHeight+'px';
      el.style.position='absolute';
      el.style.left = ((window.innerWidth - w*s)/2)+'px';
      el.style.top  = ((window.innerHeight - h*s)/2)+'px';
    });
  }
  window.addEventListener('resize', fit);
  if(document.readyState==='loading') document.addEventListener('DOMContentLoaded', fit); else fit();
  setTimeout(fit, 60); setTimeout(fit, 250);
})();
