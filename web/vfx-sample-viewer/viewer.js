let groups = [
 {title:'斩击', samples:[
  {name:'短斩 · PNG',kind:'png',id:'01_Slash',duration:.5,note:'短促斜斩与接触闪光。适合命中反馈，不能直接代表以施放者为中心的180°横扫。'},
  {name:'双刃 · 粒子',kind:'efk',url:'slash/VFX/effects/EVFX02_02_TwinEdge.efkefc',rotation:[0,-30,0],duration:1.8,note:'Twin Edge 免费样例。观察双刃轨迹与接触点；朝向沿作者演示设置。'},
  {name:'快刃 · 粒子',kind:'efk',url:'slash/VFX/effects/EVFX02_11_QuickBlade.efkefc',fit:.6,duration:1.5,note:'短促刺入接点并留下细长斩痕。适合优先试用；仍需匹配攻击方向。'},
  {name:'裂爪 · 粒子',kind:'efk',url:'slash/VFX/effects/EVFX02_09_EruptionClaw.efkefc',fit:.24,rotation:[15,0,0],duration:4.5,note:'爪痕之后接柱状喷发与环形冲击。更适合大技能，不直接沿用为普通命中。'}]},
 {title:'治疗', samples:[
  {name:'环绕治疗 · PNG',kind:'png',id:'07_Cure',duration:1.25,note:'弧形光带与星点环绕后散开。适合目标端短治疗；浅底下留意暗色边缘。'},
  {name:'医师徽记 · 粒子',kind:'efk',url:'medic/VFX/EVFXParagon10_01_MedicEmblem.efkefc',rotation:[0,180,0],duration:4,note:'注射器图案与散开的碎光。动画完整，但现代医疗符号不适合直接套用奇幻医师。'},
  {name:'诊断之眼 · 粒子',kind:'efk',url:'medic/VFX/EVFXParagon10_03_DiagnosticEye.efkefc',duration:4,note:'圆形扫描盘与网格，科技感较强。保留作对照，暂不推荐作为通用治疗。'}]},
 {title:'爆发',samples:[
  {name:'聚能爆发 · PNG',kind:'png',id:'10_Burst',duration:1.667,note:'先向中心聚能，再爆成球形碎光。适合魔法爆发；不是火药或烟尘爆炸。'},
  {name:'火焰唤起 · 粒子',kind:'efk',url:'blazeforge/VFX/EVFXForge11_02_BlazeConjuration.efkefc',duration:4,note:'火焰节点围绕中心形成圆阵，随后释放。适合施法或范围技能，需要核对地面投影。'}]}
];
if(new URLSearchParams(location.search).get('set')==='actions'){
 groups=await fetch('action-samples.json').then(r=>r.json());
 document.querySelector('h1').textContent='按实际技能筛选';
 document.querySelector('header p').textContent='喷火、重拳与冲锋的分层候选。素材动画保持原样，尚未接入技能。';
 document.querySelector('.badge').textContent='免费样例 · 动作核对';
}
const $ = (id) => document.getElementById(id);
let paused = matchMedia('(prefers-reduced-motion: reduce)').matches;
let speed = 1, scale = 1, last = performance.now();
const runtime = new Promise((resolve,reject) => effekseer.initRuntime('/samples/runtime/effekseer.wasm',resolve,()=>reject(new Error('粒子播放器加载失败'))));
const cells = await fetch('/samples/png-animations.json').then(r=>r.json());
const players=[];
for (const [i,group] of groups.entries()) {
 const card=document.createElement('article');
 card.innerHTML=`<h2>${group.title}</h2><div class="stage"><div class="ground"></div><img class="actor" alt="项目角色尺寸参照" src="/samples/actor.png"><canvas class="png"></canvas><canvas class="efk" hidden></canvas><span class="loading">加载中</span></div><select aria-label="${group.title}样例" class="choice">${group.samples.map((s,n)=>`<option value="${n}">${s.name}</option>`).join('')}</select><div class="meta"><span class="format"></span><button class="individual">重播${group.title}</button></div><div class="timeline"><input type="range" min="0" max="1000" value="0" aria-label="${group.title}进度"><span class="time">0.00秒</span></div><p class="note"></p>`;
 $('cards').append(card);
 const p={card,group,index:i,elapsed:0,ready:false,sample:null,handle:null,effect:null,context:null,gl:null,seq:null,image:null,token:0};
 p.png=card.querySelector('.png');p.efk=card.querySelector('.efk');p.status=card.querySelector('.loading');p.progress=card.querySelector('input');
 players.push(p);
 card.querySelector('select').addEventListener('change',e=>load(p,group.samples[+e.target.value]));
 card.querySelector('button').addEventListener('click',()=>{restart(p);setPaused(false)});
 p.progress.addEventListener('input',()=>{setPaused(true); const frame=+p.progress.value/1000*p.sample.duration; restart(p);p.elapsed=frame;if(p.context&&p.sample.kind==='efk')for(let n=0;n<Math.round(frame*60);n++)p.context.update(1);draw(p)});
 await load(p,group.samples[0]);
}
function setPaused(value){paused=value;$('pause').textContent=value?'继续播放':'暂停'}
function resize(p){const r=p.card.querySelector('.stage').getBoundingClientRect();for(const c of [p.png,p.efk]){if(c.width!==Math.round(r.width)||c.height!==Math.round(r.height)){c.width=Math.round(r.width);c.height=Math.round(r.height)}}}
function restart(p){if(!p.ready)return;p.elapsed=0;if(p.handle)p.handle.stop();if(p.sample.kind==='efk'){p.handle=p.context.play(p.effect,0,0,0);p.handle.setRandomSeed(214);const rot=p.sample.rotation||[0,0,0];p.handle.setRotation(...rot.map(x=>x*Math.PI/180))}draw(p)}
async function load(p,s){
 const token=++p.token;p.ready=false;p.sample=s;p.elapsed=0;p.status.textContent='加载中';p.status.classList.remove('error');p.card.querySelector('.note').textContent=s.note;
 if(p.handle){p.handle.stop();p.handle=null}if(p.effect){p.context.releaseEffect(p.effect);p.effect=null}
 p.png.hidden=s.kind!=='png';p.efk.hidden=s.kind!=='efk';resize(p);
 try {
  if(s.kind==='png'){
   p.image=new Image();p.image.src=`/samples/fa01/60FPS/60FPS_FA01_${s.id}.png`;await p.image.decode();if(token!==p.token)return;p.seq=cells[s.id];s.duration=p.seq.frames.length/60;p.card.querySelector('.format').textContent=`PNG · 60帧/秒 · ${s.duration.toFixed(2)}秒`;
  }else{
   await runtime;if(token!==p.token)return;
   if(!p.context){p.gl=p.efk.getContext('webgl2',{alpha:true,premultipliedAlpha:true,preserveDrawingBuffer:true})||p.efk.getContext('webgl',{alpha:true,premultipliedAlpha:true,preserveDrawingBuffer:true});if(!p.gl)throw new Error('浏览器不支持WebGL');p.context=effekseer.createContext();p.context.init(p.gl,{instanceMaxCount:4000});}
   const effect=await new Promise((resolve,reject)=>{const e=p.context.loadEffect('/samples/'+s.url,1,()=>resolve(e),(reason,path)=>reject(new Error(reason+' '+path)))});if(token!==p.token){p.context.releaseEffect(effect);return}p.effect=effect;
   // Measure the actual sample lifetime; a guessed timer could truncate its tail.
   const probe=p.context.play(p.effect,0,0,0);probe.setRandomSeed(214);let frames=0;
   while(probe.exists && frames<1800){p.context.update(1);frames++}
   const capped=probe.exists;probe.stop();s.duration=frames/60;
   p.card.querySelector('.format').textContent=`粒子动画 · ${s.duration.toFixed(2)}秒${capped?'（30秒预览上限）':''}`;
  }
  p.ready=true;p.status.textContent='';restart(p);
 }catch(e){p.status.textContent='加载失败：'+e.message;p.status.classList.add('error');console.error(e)}
}
function draw(p){
 if(!p.ready)return;resize(p);const s=p.sample;
 if(s.kind==='png'){
  const ctx=p.png.getContext('2d');ctx.clearRect(0,0,p.png.width,p.png.height);const index=Math.floor(p.elapsed*60);const frame=p.seq.frames[index];
  if(frame)for(const cell of frame){if(cell[0]<0)continue;const size=192*cell[3]/100*scale*.67;ctx.save();ctx.translate(p.png.width/2+cell[1]*scale*.67,p.png.height/2+cell[2]*scale*.67);ctx.rotate(cell[4]*Math.PI/180);ctx.scale(cell[5]?-1:1,1);ctx.globalAlpha=cell[6]/255;ctx.globalCompositeOperation=['source-over','lighter','multiply','screen'][cell[7]]||'source-over';ctx.drawImage(p.image,cell[0]%5*192,Math.floor(cell[0]/5)*192,192,192,-size/2,-size/2,size,size);ctx.restore()}
 }else{
  const gl=p.gl,w=p.efk.width,h=p.efk.height;gl.viewport(0,0,w,h);gl.clearColor(0,0,0,0);gl.clear(gl.COLOR_BUFFER_BIT|gl.DEPTH_BUFFER_BIT);
  // Front view, y-down as in the source demo. This is an audition camera,
  // not the game's battle projection; original per-effect rotations are retained.
  // Zoom the camera rather than changing root scale: authored particles may
  // intentionally opt out of inherited scale, which would distort the effect.
  const zoom=40*scale*(s.fit||1);
  p.context.setProjectionMatrix(new Float32Array([zoom/w,0,0,0,0,-zoom/h,0,0,0,0,-.01,0,0,0,0,1]));
  p.context.setCameraMatrix(new Float32Array([1,0,0,0,0,1,0,0,0,0,1,0,0,0,-10,1]));p.context.draw();
 }
 p.progress.value=Math.min(1000,p.elapsed/s.duration*1000);p.card.querySelector('.time').textContent=Math.min(p.elapsed,s.duration).toFixed(2)+'秒';
}
function loop(now){const dt=Math.min((now-last)/1000,.06);last=now;for(const p of players){if(!p.ready)continue;if(!paused){p.elapsed+=dt*speed;if(p.sample.kind==='efk')p.context.update(dt*speed*60);if(p.elapsed>p.sample.duration+.9&&$('loop').checked)restart(p)}draw(p)}requestAnimationFrame(loop)}
$('replay').onclick=()=>{players.forEach(restart);setPaused(false)};$('pause').onclick=()=>setPaused(!paused);
$('speed').onchange=e=>speed=+e.target.value;$('scale').onchange=e=>scale=+e.target.value;
$('background').onchange=e=>document.querySelectorAll('.stage').forEach(el=>el.dataset.bg=e.target.value);
$('actor').onchange=e=>document.querySelectorAll('.actor').forEach(el=>el.hidden=!e.target.checked);
document.addEventListener('visibilitychange',()=>{last=performance.now()});
setPaused(paused);requestAnimationFrame(loop);
