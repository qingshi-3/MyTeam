const video=document.querySelector('#show');
const revision=document.querySelector('#revision'),speed=document.querySelector('#speed'),pause=document.querySelector('#pause');
const facing=document.querySelector('#facing');
const scale=document.querySelector('#effect-scale');
const actions=[...document.querySelectorAll('[data-action]')];
const info={
  returning_blade:['返刃','四刃金属刀体、自旋残影与短尾迹；保留原来的匀速去回。','EE08'],
  mechanical_hook:['飞钩','出钩、回拖速度提高一倍，链头和被钩单位同步；前摇保持原样。','HC39'],
  ground_fissure:['地裂','放大时，单座岩峰的宽高、地缝和烟尘一起等比例放大，保留山体之间的疏密与出土节奏。可切换尺寸和修订前 2× 对照。','EB01'],
  rock_raise:['岩垒','统一角色与地面的比例，墙体在身前留出空隙，两翼减小向后内收。可切换左右、斜向及间距修订前的版本。','EB01'],
  position_swap:['换位','准备标记成对出现，交换同刻两端光缝展开，余光独立收净。','EE09'],
  acid_spit:['酸液','命中后飞溅，并在地面铺开更大的不规则液斑，约 0.7 秒收净；不是持续伤害区域。','EB02']
};
let selected='rock_raise';
const changed=new Set(['mechanical_hook','ground_fissure','rock_raise','acid_spit']);
const reduced=matchMedia('(prefers-reduced-motion: reduce)');
function play(){video.play().catch(()=>pause.textContent='播放');}
function choose(id){
  selected=id;
  const latest=revision.value==='clearance',recent=latest || revision.value==='perspective';
  facing.parentElement.hidden=!(recent && id==='rock_raise');
  scale.parentElement.hidden=!(recent && id==='ground_fissure');
  const file=revision.value==='battle' ? `${info[id][2]==='EB01'?'clearance-':changed.has(id)?'feedback-':''}battle-${info[id][2]}`
    : recent && id==='ground_fissure' ? `scale-${scale.value}-ground_fissure`
    : recent && id==='rock_raise' ? `${latest?'clearance':'perspective'}-${facing.value==='right'?'':facing.value+'-'}rock_raise`
    : `${(recent || revision.value==='feedback')?(changed.has(id)?'feedback':'after'):revision.value}-${id}`;
  video.src=`refined/${file}.mp4`;
  video.playbackRate=Number(speed.value);
  actions.forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.action===id)));
  document.querySelector('#version').textContent=`${revision.selectedOptions[0].textContent} · ${info[id][0]}${recent && id==='ground_fissure'?' · '+scale.selectedOptions[0].textContent:''}`;
  document.querySelector('#note').textContent=revision.value==='after'||revision.value==='before'
    ? '历史版本。岩垒早期仅展示单块升起。':revision.value==='perspective' && id==='rock_raise'
    ? '间距修订前：角色与地面缩放比例不一致，岩壁两翼过度内收，遮住角色脸部。':recent && id==='ground_fissure' && scale.value==='before-large'
    ? '修订前 2×：范围扩大，但单座山体和烟尘仍是原尺寸。与当前 2× 使用相同范围、视角和播放速度。':revision.value==='feedback' && id==='ground_fissure'
    ? '历史版本：已改不规则地缝，尚未修正整体缩放。':revision.value==='feedback' && id==='rock_raise'
    ? '材质与朝向修订前：上一版的弧形岩柱组合，保留供对照。':revision.value==='battle' && id==='rock_raise'
    ? '正式战斗：岩壁采用同一哑光石材与俯侧视投影，起墙后持续驻留。':info[id][1];
  if(!reduced.matches)play();
}
actions.forEach(b=>b.addEventListener('click',()=>choose(b.dataset.action)));
revision.addEventListener('change',()=>choose(selected));
facing.addEventListener('change',()=>choose(selected));
scale.addEventListener('change',()=>choose(selected));
speed.addEventListener('change',()=>video.playbackRate=Number(speed.value));
pause.addEventListener('click',()=>video.paused?play():video.pause());
document.querySelector('#replay').addEventListener('click',()=>{video.currentTime=0;play();});
video.addEventListener('play',()=>pause.textContent='暂停');
video.addEventListener('pause',()=>pause.textContent='播放');
video.addEventListener('error',()=>document.querySelector('#version').textContent='录制加载失败，请刷新重试');
reduced.addEventListener('change',e=>{if(e.matches)video.pause();});
choose(selected);
