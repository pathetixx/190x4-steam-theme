/* 190x4 icon set — stroke icons, 24px grid. Usage: <i data-ic="search"></i> (+ optional data-fill)
   Replaced on DOMContentLoaded. Keep simple geometry only (per brand rules). */
(function(){
  const P = (d, extra) => `<path d="${d}" ${extra||''}/>`;
  const I = {
    search:'<circle cx="11" cy="11" r="7"/><path d="M21 21l-4.3-4.3"/>',
    filter:'<path d="M3 5h18M6 12h12M10 19h4"/>',
    grid:'<rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/>',
    list:'<path d="M8 6h13M8 12h13M8 18h13M3.5 6h.01M3.5 12h.01M3.5 18h.01"/>',
    clock:'<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
    play:'<path d="M7 5l12 7-12 7z" fill="currentColor" stroke="none"/>',
    gear:'<circle cx="12" cy="12" r="3"/><path d="M19.4 13a7.8 7.8 0 0 0 0-2l2-1.5-2-3.5-2.4 1a7.6 7.6 0 0 0-1.7-1L14 0h-4l-.3 2.5a7.6 7.6 0 0 0-1.7 1l-2.4-1-2 3.5L3.6 11a7.8 7.8 0 0 0 0 2l-2 1.5 2 3.5 2.4-1c.5.4 1.1.7 1.7 1L10 21h4l.3-2.5c.6-.3 1.2-.6 1.7-1l2.4 1 2-3.5z" transform="translate(0 1.5)"/>',
    info:'<circle cx="12" cy="12" r="9"/><path d="M12 11v5M12 8h.01"/>',
    heart:'<path d="M12 20s-7-4.6-9.3-9C1 7.5 3 4.5 6.2 4.5c2 0 3.2 1.2 3.8 2.3.6-1.1 1.8-2.3 3.8-2.3 3.2 0 5.2 3 3.5 6.5C19 15.4 12 20 12 20z"/>',
    cloud:'<path d="M7 18a4 4 0 0 1-.4-8 5.5 5.5 0 0 1 10.6 1.2A3.5 3.5 0 0 1 17 18z"/>',
    trophy:'<path d="M7 4h10v4a5 5 0 0 1-10 0zM7 6H4v2a3 3 0 0 0 3 3M17 6h3v2a3 3 0 0 1-3 3M9 18h6M10 14v4M14 14v4M8 21h8"/>',
    chevL:'<path d="M15 6l-6 6 6 6"/>',
    chevR:'<path d="M9 6l6 6-6 6"/>',
    chevD:'<path d="M6 9l6 6 6-6"/>',
    bell:'<path d="M6 9a6 6 0 0 1 12 0c0 5 2 6 2 6H4s2-1 2-6zM10 20a2 2 0 0 0 4 0"/>',
    broadcast:'<circle cx="12" cy="12" r="2.5"/><path d="M7 7a7 7 0 0 0 0 10M17 7a7 7 0 0 1 0 10M4 4a11 11 0 0 0 0 16M20 4a11 11 0 0 1 0 16"/>',
    monitor:'<rect x="3" y="4" width="18" height="12" rx="1"/><path d="M8 20h8M12 16v4"/>',
    min:'<path d="M5 12h14"/>',
    max:'<rect x="5" y="5" width="14" height="14"/>',
    close:'<path d="M6 6l12 12M18 6L6 18"/>',
    people:'<circle cx="9" cy="8" r="3.2"/><path d="M3.5 19a5.5 5.5 0 0 1 11 0M16 6a3 3 0 0 1 0 6M17 14.2a5.5 5.5 0 0 1 3.5 4.8"/>',
    addperson:'<circle cx="9" cy="8" r="3.2"/><path d="M3.5 19a5.5 5.5 0 0 1 11 0M18 7v6M21 10h-6"/>',
    send:'<path d="M4 12l16-7-7 16-2.5-6.5z"/>',
    emoji:'<circle cx="12" cy="12" r="9"/><path d="M8.5 10h.01M15.5 10h.01M8.5 14.5a5 5 0 0 0 7 0"/>',
    attach:'<path d="M20 11l-8 8a5 5 0 0 1-7-7l8-8a3.4 3.4 0 0 1 5 5l-8 8a1.8 1.8 0 0 1-2.5-2.5l7-7"/>',
    download:'<path d="M12 3v11M7 10l5 5 5-5M5 20h14"/>',
    pause:'<path d="M8 5v14M16 5v14"/>',
    refresh:'<path d="M20 11a8 8 0 1 0-1 5M20 5v6h-6"/>',
    net:'<path d="M4 20V10M10 20V5M16 20v-8M22 20V4"/>',
    disk:'<rect x="3" y="3" width="18" height="18" rx="2"/><path d="M7 3v6h10V3M12 16h.01"/>',
    plus:'<path d="M12 5v14M5 12h14"/>',
    lock:'<rect x="5" y="11" width="14" height="9" rx="1"/><path d="M8 11V8a4 4 0 0 1 8 0v3"/>',
    shield:'<path d="M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6z"/>',
    user:'<circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0 1 16 0"/>',
    store:'<path d="M4 9l1.5-5h13L20 9M4 9v10h16V9M4 9h16M9 19v-6h6v6"/>',
    library:'<rect x="4" y="3" width="5" height="18"/><rect x="11" y="3" width="5" height="18"/><path d="M18 4l3 16"/>',
    community:'<circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3c3 3 3 15 0 18M12 3c-3 3-3 15 0 18"/>',
    eyeoff:'<path d="M3 3l18 18M10.6 10.6a2 2 0 0 0 2.8 2.8M9.4 5.2A9 9 0 0 1 21 12a14 14 0 0 1-2.2 3M6.2 6.2A14 14 0 0 0 3 12a9 9 0 0 0 12 5.7"/>',
    edit:'<path d="M4 20h4l10-10-4-4L4 16zM13.5 6.5l4 4"/>',
    card:'<rect x="3" y="5" width="18" height="14" rx="2"/><path d="M3 10h18M7 15h4"/>',
    dots:'<circle cx="6" cy="12" r="1.4" fill="currentColor" stroke="none"/><circle cx="12" cy="12" r="1.4" fill="currentColor" stroke="none"/><circle cx="18" cy="12" r="1.4" fill="currentColor" stroke="none"/>',
    back:'<path d="M19 12H5M11 6l-6 6 6 6"/>',
    fwd:'<path d="M5 12h14M13 6l6 6-6 6"/>',
    globe:'<circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3c2.6 2.5 2.6 15 0 18M12 3c-2.6 2.5-2.6 15 0 18"/>',
    star:'<path d="M12 3l2.7 5.5 6 .9-4.3 4.2 1 6-5.4-2.8L6.6 19.6l1-6L3.3 9.4l6-.9z"/>',
    flame:'<path d="M12 3c1 3 4 4 4 8a4 4 0 0 1-8 0c0-1.5.7-2.3 1.3-3 .2 1 .9 1.6 1.7 1.6C12 9.6 11 7 12 3z"/>',
    workshop:'<path d="M12 3l2 4 4 .6-3 3 .8 4.2L12 13l-3.8 1.8.8-4.2-3-3 4-.6zM6 20h12"/>',
    achiev:'<circle cx="12" cy="9" r="5"/><path d="M9 13l-1 8 4-2 4 2-1-8"/>',
    map:'<path d="M9 4L3 6v14l6-2 6 2 6-2V4l-6 2-6-2zM9 4v14M15 6v14"/>',
    controller:'<path d="M7 8h10a5 5 0 0 1 5 5l-1 5a2.5 2.5 0 0 1-4.3 1L15 16H9l-1.7 3a2.5 2.5 0 0 1-4.3-1l-1-5a5 5 0 0 1 5-5zM7 11v3M5.5 12.5h3M15.5 11.5h.01M18 13h.01"/>',
    family:'<circle cx="8" cy="8" r="2.5"/><circle cx="16" cy="8" r="2.5"/><path d="M3 20a5 5 0 0 1 10 0M11 20a5 5 0 0 1 10 0"/>',
    box:'<path d="M3 7l9-4 9 4-9 4-9-4zM3 7v10l9 4 9-4V7M12 11v10"/>',
    mic:'<rect x="9" y="3" width="6" height="11" rx="3"/><path d="M5 11a7 7 0 0 0 14 0M12 18v3"/>',
    rec:'<circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="3.5" fill="currentColor" stroke="none"/>',
    accessib:'<circle cx="12" cy="4.5" r="1.8"/><path d="M4 8h16M12 8v6M12 14l-3 6M12 14l3 6"/>',
    check:'<path d="M5 12l4 4 10-10"/>',
    sliders:'<path d="M4 6h10M18 6h2M4 12h2M10 12h10M4 18h12M20 18h0"/><circle cx="15" cy="6" r="2"/><circle cx="8" cy="12" r="2"/><circle cx="17" cy="18" r="2"/>',
  };
  function render(){
    document.querySelectorAll('i[data-ic]').forEach(el=>{
      const name = el.getAttribute('data-ic'); if(!I[name]) return;
      const sw = el.getAttribute('data-sw') || '1.7';
      const svg = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="${sw}" stroke-linecap="round" stroke-linejoin="round" width="100%" height="100%">${I[name]}</svg>`;
      el.innerHTML = svg; el.style.display='inline-flex'; el.style.lineHeight='0';
    });
  }
  if(document.readyState==='loading') document.addEventListener('DOMContentLoaded', render); else render();
  window.x4icons = I;
  window.__x4renderIcons = render;
})();
