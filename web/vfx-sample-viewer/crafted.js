const video=document.querySelector('#show');
const after=document.querySelector('#after'),before=document.querySelector('#before');
const labels={fire:'喷火',punch:'蓄怒重拳',rush:'冲锋',final:'三个动作总览',before:'原版 · 同视角、同尺寸总览对照'};
const actions=[...document.querySelectorAll('[data-action]')];
actions.forEach(button=>button.addEventListener('click',()=>choose(button.dataset.action)));
const pause=document.querySelector('#pause'),speed=document.querySelector('#speed');
const revision=document.querySelector('#revision');
let selected='fire';
function play(){video.play().catch(()=>{pause.textContent='播放';});}
function choose(name){
  selected=name;
  revision.disabled=name==='before';
  const prefix=name==='before'?'':revision.value==='animation'?(name==='fire'?'polish-':'animation-'):revision.value==='polish'?'polish-':'iteration1-';
  const suffix=revision.value==='polish' && (name==='rush' || name==='final')?'-wide':'';
  video.src=`crafted/${prefix}${name}${suffix}.mp4`;
  video.playbackRate=Number(speed.value);
  after.setAttribute('aria-pressed',String(name==='final'));
  before.setAttribute('aria-pressed',String(name==='before'));
  actions.forEach(button=>button.setAttribute('aria-pressed',String(button.dataset.action===name)));
  document.querySelector('#version').textContent=name==='before'?labels.before:({animation:'角色动作同步 · ',polish:'动作调整前 · ',iteration1:'初版 · '}[revision.value])+labels[name];
  play();
}
after.addEventListener('click',()=>choose('final'));
before.addEventListener('click',()=>choose('before'));
pause.addEventListener('click',()=>video.paused?play():video.pause());
document.querySelector('#replay').addEventListener('click',()=>{video.currentTime=0;play();});
speed.addEventListener('change',()=>{video.playbackRate=Number(speed.value);});
revision.addEventListener('change',()=>choose(selected));
video.addEventListener('play',()=>{pause.textContent='暂停';});
video.addEventListener('pause',()=>{pause.textContent='播放';});
video.addEventListener('error',()=>{document.querySelector('#version').textContent='录制加载失败，请刷新重试';});
const reduced=matchMedia('(prefers-reduced-motion: reduce)');
if(!reduced.matches)play();else pause.textContent='播放';
reduced.addEventListener('change',event=>{if(event.matches)video.pause();});
