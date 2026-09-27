const $=s=>document.querySelector(s);
const records=await fetch('library.json').then(r=>{if(!r.ok)throw Error('目录加载失败');return r.json();});
const video=$('#show'),reduced=matchMedia('(prefers-reduced-motion: reduce)');
let selected=records.find(r=>r.id===location.hash.slice(1))||records.find(r=>r.id==='claw_swipe');
$('#summary').textContent=`${records.length} 项完整目录 · ${records.filter(r=>r.status==='本轮细化').length} 项本轮细化 · 已认可动作保留为基准`;
for(const group of new Set(records.map(r=>r.group))){const o=new Option(group,group);$('#category').add(o);}
function list(){
 const q=$('#search').value.trim().toLowerCase(),group=$('#category').value,status=$('#status').value;
 const shown=records.filter(r=>(!q||`${r.name} ${r.id}`.toLowerCase().includes(q))&&(!group||group===r.group)&&(!status||status===r.status));
 $('#count').textContent=`显示 ${shown.length} / ${records.length} 项`;
 $('#list').replaceChildren(...shown.map(r=>{const b=document.createElement('button');b.type='button';b.textContent=r.name;b.setAttribute('aria-pressed',String(r.id===selected.id));const tag=document.createElement('span');tag.textContent=`${r.group} · ${r.status}`;b.append(tag);b.addEventListener('click',()=>choose(r));return b;}));
 if(!shown.length){const p=document.createElement('p');p.textContent='没有匹配的特效。';$('#list').append(p);}
}
function play(){video.play().catch(()=>$('#media-state').textContent='点播放开始');}
function choose(r){
 selected=r;history.replaceState(null,'',`#${r.id}`);
 $('#name').textContent=r.name;$('#tag').textContent=`${r.group} / ${r.status}`;
 const old=$('#version').value==='before';
 video.src=old?r.before:r.after;video.playbackRate=Number($('#speed').value);
 $('#note').textContent=r.note;
 $('#comparison').textContent=old?'修改前的历史记录。此轮同时修正了预览中心、默认半径和外部位移，相关条目的构图可能不同。':r.comparison||'当前共享资源的单项展示；可切换修改前记录。';
 $('#detail').hidden=!r.detail;if(r.detail)$('#detail').href=r.detail;
 $('#media-state').textContent='加载中';list();if(!reduced.matches)play();
}
for(const id of ['search','category','status'])$("#"+id).addEventListener(id==='search'?'input':'change',list);
$('#version').addEventListener('change',()=>choose(selected));
$('#speed').addEventListener('change',()=>video.playbackRate=Number($('#speed').value));
$('#replay').addEventListener('click',()=>{video.currentTime=0;play();});
$('#pause').addEventListener('click',()=>video.paused?play():video.pause());
video.addEventListener('play',()=>$('#pause').textContent='暂停');video.addEventListener('pause',()=>$('#pause').textContent='播放');
video.addEventListener('loadeddata',()=>$('#media-state').textContent='引擎实录 · 无音轨');
video.addEventListener('error',()=>$('#media-state').textContent='视频加载失败，请刷新重试');
reduced.addEventListener('change',e=>{if(e.matches)video.pause();});choose(selected);
