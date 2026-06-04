/* 190x4 — shared app chrome (titlebar + nav + footer).
   Usage: <div data-chrome data-active="library" data-url="https://store.steampowered.com/"></div>
   Renders into any [data-chrome]; reads data-active (store|library|community|profile) + optional data-url. */
(function(){
  const NAME = 'ты+я = нихуя';
  const BAL  = '122,37₸';

  function avatar(size, cls){
    return `<span class="x4-ava ${cls||''}" style="width:${size}px;height:${size}px">
      <span style="position:absolute;inset:0;background:
        radial-gradient(circle at 30% 25%, #7a3bd6, #2a1145 70%)"></span>
      <span style="position:absolute;inset:0;background:
        linear-gradient(135deg, transparent 40%, rgba(255,37,48,.35))"></span></span>`;
  }

  function titlebar(){
    return `<div class="x4-titlebar">
      <div class="x4-tb-left">
        <span class="x4-logo" data-t="190×4" style="font-size:15px">190<span class="x4-x">×4</span></span>
        <nav class="x4-appmenu">
          <button>Steam</button><button>Вид</button><button>Друзья</button>
          <button>Игры</button><button>Справка</button>
        </nav>
      </div>
      <div class="x4-tb-right">
        <button class="x4-iconbtn" title="Трансляция"><i data-ic="broadcast"></i></button>
        <button class="x4-iconbtn is-active" title="Уведомления"><i data-ic="bell"></i><span class="x4-notif-dot"></span></button>
        <button class="x4-profilechip">
          ${avatar(22,'x4-ava--online')}
          <span class="nm">${NAME}</span><i data-ic="chevD" class="cv"></i>
          <span class="bal">${BAL}</span>
        </button>
        <button class="x4-iconbtn" title="Удалённая игра"><i data-ic="monitor"></i></button>
        <div class="x4-winbtns">
          <span class="x4-winbtn"><i data-ic="min"></i></span>
          <span class="x4-winbtn"><i data-ic="max"></i></span>
          <span class="x4-winbtn x4-winbtn--close"><i data-ic="close"></i></span>
        </div>
      </div>
    </div>`;
  }

  function nav(active, url){
    const tab=(id,label)=>`<button class="x4-navtab ${active===id?'is-active':''}">${label}</button>`;
    return `<div class="x4-nav">
      <div class="x4-nav-arrows">
        <button class="x4-iconbtn"><i data-ic="back"></i></button>
        <button class="x4-iconbtn"><i data-ic="fwd"></i></button>
      </div>
      <div class="x4-nav-tabs">
        ${tab('store','Магазин')}${tab('library','Библиотека')}${tab('community','Сообщество')}
        <button class="x4-navtab x4-navtab--profile ${active==='profile'?'is-active':''}">${NAME.toUpperCase()}</button>
      </div>
    </div>
    ${url?`<div class="x4-urlbar"><i data-ic="globe"></i><span>${url}</span></div>`:''}`;
  }

  function footer(){
    return `<div class="x4-footer">
      <button class="x4-foot-link"><i data-ic="plus"></i> Добавить игру</button>
      <button class="x4-foot-link x4-foot-center"><i data-ic="download"></i> Управление загрузками</button>
      <button class="x4-foot-link"><i data-ic="people"></i> Друзья и чат</button>
    </div>`;
  }

  window.x4Chrome = { titlebar, nav, footer, avatar };

  function mount(){
    document.querySelectorAll('[data-chrome]').forEach(el=>{
      const active = el.getAttribute('data-active')||'';
      const url = el.getAttribute('data-url')||'';
      el.outerHTML = `<header class="x4-chrome">${titlebar()}${nav(active,url)}</header>`;
    });
    document.querySelectorAll('[data-footer]').forEach(el=>{ el.outerHTML = footer(); });
    if(window.x4icons===undefined){ /* icons.js renders on its own */ }
    // re-render icons that we just injected
    if(window.__x4renderIcons) window.__x4renderIcons();
  }
  if(document.readyState==='loading') document.addEventListener('DOMContentLoaded', mount); else mount();
})();
