const media=JSON.parse(root.querySelector('#mf-assets').textContent);
for(const [key,target] of Object.entries(media._aliases||{}))media[key]=media[target];
root.style.setProperty('--mf-backdrop','url("'+media.backdrop+'")');
for(const key of ['backdrop','frame','ring','button'])root.style.setProperty('--opening-'+key,'url("'+media['opening-'+key]+'")');
const openingSampleIds=['mx01','mx03','mx23','mx02','mx25','mx05'];
if(s.openingSample!==2){s={...fresh(),page:'opening',openingSample:2,opening:openingSampleIds,selected:[],roster:[],board:Array(18).fill(null),draft:true,resume:'opening',floor:1,gold:0,research:0,materials:0,health:100,inventory:[],relics:[]};}
const icon=name=>el('i',{'data-lucide':name,class:'icon','aria-hidden':'true'});
const art=(id,cls='',alt='')=>el('img',{src:media[id],class:cls,alt,draggable:false});
const icoBtn=(label,name,act,value='',cls='')=>btn(icon(name),act,value,cls,false,{'aria-label':label,'data-tooltip':label});
const traitIcon=t=>({'霜羽':'snowflake','护卫':'shield','射手':'crosshair','召唤':'bug','护盾':'shield-plus','术士':'sparkles','支援':'heart-handshake','棘毒':'leaf','斗士':'swords'}[t]||'sparkles');
function heading(title){return box('heading',box('copy',el('h2',{},title)));}
function chooseHeroCard(id,mode){
 if(mode==='opening')return openingHeroCard(id);
 const x=hero(id),chosen=s.selected.includes(id),limit=mode==='opening'&&s.selected.length>=2&&!chosen;
 const card=box('candidate hero-card'+(chosen?' chosen':''),
  box('portrait'+(id==='mx21'?' sprite':''),art(id)),
  box('card-traits',x.trait.split(' · ').map(t=>el('span',{class:'trait-medal','data-tooltip':t,'aria-label':t},icon(traitIcon(t))))),
  box('card-info',txt(x.job,'job'),el('h3',{},x.name),box('facts',...([['攻击',x.atk,'swords'],['法强',x.power,'sparkles'],['生命',x.hp,'heart']].map(([label,value,name])=>el('span',{'aria-label':label+' '+value,'data-tooltip':label},icon(name),String(value)))))),
  btn('','choose',id,'card-select',limit,{'aria-label':(chosen?'取消':'选择')+x.name,'aria-pressed':String(chosen)}),
  icoBtn('查看'+x.name+'能力','info','hero-detail',id,'card-details'));
 card.dataset.hero=id;return card;
}
function openingHeroCard(id){
 const x=hero(id),chosen=s.selected.includes(id),limit=s.selected.length===2&&!chosen;
 const card=box('candidate hero-card'+(chosen?' chosen':'')+(limit?' unavailable':''),
  box('portrait',art(id)),art('opening-frame','stone-frame'),
  box('card-info',el('h3',{},x.name),box('facts',...([['攻击',x.atk,'attack'],['法强',x.power,'spell-power'],['生命',x.hp,'health']].map(([label,value,name])=>el('span',{'aria-label':label+' '+value,'data-tooltip':label},art('stat-'+name,'stat-object'),String(value)))))),
  box('card-traits',x.trait.split(' · ').map(t=>el('span',{class:'opening-trait'},t))),
  btn('','choose',id,'card-select',limit,{'aria-label':(chosen?'取消':'选择')+x.name,'aria-pressed':String(chosen)}),
  btn('i','hero-detail',id,'card-details',false,{'aria-label':'查看'+x.name+'能力'}));
 card.dataset.hero=id;card.style.setProperty('--hero-wash',({mx01:'#46758b',mx03:'#505789',mx23:'#75504a',mx02:'#547363',mx25:'#80724d',mx05:'#686988'})[id]||'#426078');return card;
}
function itemCard(x,mode){
 const chosen=s.rewardPick===x.id,bought=s.purchased.includes(x.id);
 return box('candidate item-card'+(chosen?' chosen':''),box('itemmark',art(x.id)),box('row between',el('h3',{},x.name),icoBtn('查看'+x.name+'规则','info','item-detail',x.id,'ghost')),el('p',{class:'desc muted'},x.desc),box('actionrow',mode==='shop'?btn(bought?[icon('check'),' 已售出']:[icon('coins'),' '+x.price],'buy',x.id,'primary',bought||s.gold<x.price,{'aria-label':'购买'+x.name+'，'+x.price+'金币'}):btn(chosen?[icon('check'),' 已选']:'选取','reward-pick',x.id,chosen?'selected':'')));
}
function topbar(){
 const show=!['menu','opening','settings','result'].includes(s.page);
 const res=(name,value,label,key)=>el('span',{class:'resource','aria-label':label+' '+value,'data-tooltip':label,'data-resource':key},icon(name),txt(value));
 return box('top',box('brand',icon('castle'),txt('军团登塔')),show?[
  res('heart',s.health+'/100','生命','health'),res('coins',s.gold,'金币','gold'),res('anvil',s.materials,'材料','materials'),res('flask-conical',s.research,'研究','research'),
  btn([icon('gem'),String(s.relics.length)],'relics','','ghost',false,{'aria-label':'遗物 '+s.relics.length}),btn([icon('users'),'军团'],'manage','roster','ghost'),icoBtn('征程菜单','menu','system','','ghost')]:null);
}
function rail(){
 const traits=['霜羽','护卫','射手','召唤'];
 return box('traitrail',traits.map(t=>{const count=deployed().filter(id=>hero(id).trait.includes(t)).length;return btn([icon(traitIcon(t)),txt(t,'trait-name'),txt(count+' / '+(t==='霜羽'?4:2),'trait-count')],'trait',t,'',false,{'aria-label':t+' '+count});}),box('grow'),el('span',{class:'population','aria-label':'上阵 '+deployed().length+' / 10','data-tooltip':'上阵人数'},icon('users'),txt(deployed().length+'/10')));
}
function selectionTray(count){return box('selection-tray',Array.from({length:count},(_,i)=>{const id=s.selected[i];return id?btn(art(id),'choose',id,'seat filled',false,{'aria-label':'取消'+hero(id).name,'data-seat':id}):el('span',{class:'seat','aria-label':'空英雄席位 '+(i+1)},s.page==='opening'?txt(i===0?'Ⅰ':'Ⅱ','seat-rank'):icon('user-round'));}));}
function presetChip(){if(!s.preset)return null;return box('preset-chip',icon('sparkles'),txt(spells.find(x=>x.id===s.preset.spell).name),icon('arrow-right'),art(s.preset.target),txt(hero(s.preset.target).name));}
function footer(){
 let status=null;
 if(s.page==='opening')status=selectionTray(2);
 else if(['recruit','discovery'].includes(s.page))status=s.roster.length>=capacity()?box('warn row',icon('users'),txt('军团已满')):selectionTray(1);
 else if(['prepare','spells'].includes(s.page))status=presetChip();
 else if(s.page==='battle')status=box('row',icon(s.paused?'pause':'swords'),txt(s.paused?'已暂停':'00:18','small muted'));
 else if(s.page==='production')status=box('row',icon('chevrons-right'),txt(s.production.mode==='research'?'研究':s.production.mode==='training'?hero(s.production.target).name:'材料','small gold'));
 return box('foot',footLeft,box('status',status),footRight);
}
function choiceScreen(discovery=false){
 const ids=(s.scenario==='single'?[s.offers[0]]:s.offers).filter(Boolean);
 footLeft=[btn(discovery?'返回升阶':'返回征程','back',discovery?'ascension':'route')];
 if(!discovery)footRight.push(btn('跳过','skip-recruit'));
 footRight.push(btn(discovery?'加入军团':'招募','confirm-recruit',discovery?'discovery':'recruit','primary',s.selected.length!==1||s.roster.length>=capacity()));
 return [heading(discovery?'高阶召集':'招募英雄'),box('grid3 grow',ids.map(id=>chooseHeroCard(id,discovery?'discovery':'recruit')))];
}
renderers.menu=function(){return [box('menu',box('mark',el('h1',{},'军团登塔'),el('i',{'data-lucide':'castle',class:'crest','aria-hidden':true})),box('stack',btn('继续征程','continue','','primary',!s.runExists),s.runExists?box('resume-marker',icon('flag'),txt(s.draft?'启程之前':'第 '+s.floor+' 层')):null,btn('新征程','new'),btn('设置','settings','','ghost'),btn('退出','quit','','ghost')))];};
renderers.opening=function(){footLeft=[btn('返回','menu')];footRight=[btn('启程','confirm-opening','','primary',s.selected.length!==2)];return [heading('同行者'),box('opening grow',s.opening.map(id=>chooseHeroCard(id,'opening')))];};
renderers.route=function(){
 footLeft=[btn('放弃征程','abandon','','ghost danger')];
 const group=s.floor%3;
 const options=s.pendingPage?[[s.pendingPage,'继续当前机会','flag','继续']]:s.lockedOpponent!==null?[['prepare','战前整备','combat','继续']]:s.floor%5===0?[['boss','区域首领','boss','挑战']]:group===0?[['combat','守军','combat','迎战'],['recruit','招募','recruitment','召集'],['shop','行商','shop','入店']]:group===1?[['event','塔内异象','event','探查'],['rest','高塔营火','rest','驻足'],['elite','精英挑战','elite','迎战']]:[['combat','守军','combat','迎战'],['recruit','招募','recruitment','召集'],['elite','精英挑战','elite','迎战']];
 const track=el('div',{class:'route-track','aria-label':'第 '+s.floor+' 层，共 15 层'},Array.from({length:15},(_,i)=>el('span',{class:'step-dot'+(i+1<s.floor?' past':'')+(i+1===s.floor?' now':'')+((i+1)%5===0?' boss':''),'aria-hidden':true})));
 return [box('heading',box('copy',el('h2',{},'余烬铸层'),txt(String(s.floor).padStart(2,'0')+' / 15','small gold')),track,s.discoveryPending?btn('高阶召集','manage','discovery','primary'):null),box('grid3 grow',options.map(([id,title,kind,action])=>box('candidate route-card',box('node-medallion',media['node-'+kind]?art('node-'+kind):icon('flag')),el('h3',{},title),btn([action,' ',icon('arrow-right')],'node',id,'primary'))))];
};
renderers.opponent=function(){
 footLeft=[btn('返回征程','back','route')];footRight=[btn('进入整备','lock-opponent','','primary')];
 const opts=s.scenario==='single'?enemies.slice(0,1):enemies;
 return [heading('选择对手'),box('grid3 grow',opts.map((x,i)=>box('candidate opponent-card'+(s.opponent===i?' chosen':''),box('enemy-preview',icon(['shield','flame','swords'][i])),box('row between',el('h3',{},x.name),icoBtn('查看敌阵','info','enemy-detail',String(i),'ghost')),el('p',{class:'desc muted'},x.pressure),box('enemy-units',x.units.map(u=>txt(u,'small'))),box('enemy-rewards',txt(x.reward,'small gold'),el('span',{class:'small warn','aria-label':'失败生命减少30'},icon('heart-crack'),' −30')),btn(s.opponent===i?[icon('check'),' 已选']:'挑战','opponent',String(i),s.opponent===i?'selected':''))))];
};
renderers.prepare=function(){
 footLeft=[btn('返回征程','back','route')];footRight=[btn('法术','manage','spells'),btn('开战','start-battle','','primary',deployed().length===0)];
 return [box('heading',box('copy',el('h2',{},'战前整备')),btn('敌阵','enemy-detail',String(s.opponent)),btn('装备','manage','roster')),boardView(false),box('bench',el('span',{'aria-label':'后备','data-tooltip':'后备'},icon('users')),bench().map(id=>btn([art(id,'unit-thumb'),hero(id).name],'unit',id,'',false,{draggable:true,'data-drag':'hero:'+id})))];
};
renderers.spells=function(){
 const sp=spells.find(x=>x.id===s.spell);
 footLeft=[btn('返回','return')];footRight=[btn('取消预案','clear-spell','','',!s.preset||!canEdit()),btn('预设','preset','','primary',stock()===0||!deployed().includes(s.target)||!canEdit())];
 return [heading('战前法术'),managerTabs('spells'),box('split grow',box('list',spells.map((x,i)=>btn([icon(['shield-plus','sparkles','swords','hourglass'][i]),x.name,txt('×'+(s.spellStock[x.id]||0),'small')],'spell',x.id,s.spell===x.id?'selected':''))),box('stack',box('well stack',box('row between',el('h3',{},sp.name),txt('×'+stock(),'gold')),el('p',{},sp.desc),box('row between',canEdit()?null:box('readonly-lock',icon('lock-keyhole'),txt('本场已锁定')),btn([icon('flask-conical'),' '+sp.cost+'  兑换'],'craft','','',s.research<sp.cost||!canEdit()))),box('well stack',txt('目标','small muted'),box('spell-targets',deployed().map(id=>btn([art(id,'unit-thumb'),hero(id).name],'target',id,s.target===id?'selected':''))))))];
};
renderers.production=function(){
 footLeft=[btn('返回','return')];footRight=[btn('保存','save-production','','primary',!canEdit()||!s.roster.includes(s.production.producer))];
 return [heading('生产分配'),managerTabs('production'),box('grid3 grow',box('well stack',icon('user-round-cog'),field('生产者','producer',s.roster.filter(id=>id==='mx25').map(id=>[id,hero(id).name]),s.production.producer),s.roster.includes('mx25')?art('mx25','production-portrait'):muted('暂无生产者')),box('well stack',icon('anvil'),field('产出','mode',[['materials','通用材料'],['research','研究'],['training','定向培养']],s.production.mode)),box('well stack',icon('package-check'),s.production.mode==='training'?field('受益英雄','production-target',s.roster.map(id=>[id,hero(id).name]),s.production.target):el('h3',{},s.production.mode==='research'?'研究储备':'材料储备'),box('grow'),btn('累计培养','growth-ledger')))];
};
renderers.ascension=function(){
 const x=hero(s.unit),already=s.ascended.includes(s.unit);footLeft=[btn('返回','return')];footRight=[s.discoveryPending?btn('高阶召集','open-discovery','','primary'):btn(already?[icon('check'),' 已升阶']:[icon('anvil'),' 4  升阶'],'ascend','','primary',already||s.materials<4||!canEdit())];
 return [heading('英雄升阶'),managerTabs('ascension'),box('split grow',box('list',s.roster.map(id=>btn([art(id,'unit-thumb'),hero(id).name,s.ascended.includes(id)?icon('chevrons-up'):null],'unit',id,s.unit===id?'selected':''))),box('well ascension-panel',art(x.id,'ascension-portrait'),box('stack',el('h3',{},x.name),txt(already?'已升阶':'基础阶','gold'),btn('能力','hero-detail',x.id),box('grow'),box('row',icon('chevrons-up'),txt('高阶英雄 ×1','small gold')))))];
};
renderers.battle=function(){footLeft=[btn(s.paused?[icon('play'),'继续']:[icon('pause'),'暂停'],'pause'),btn('×'+s.speed,'speed')];footRight=[btn('战场规则','rules')];return [box('heading',box('copy',el('h2',{},s.kind==='boss'?'首领战':enemies[s.opponent].name)),btn('阵容','manage','roster')),boardView(true)];};
renderers.reward=function(){const items=s.rewardGroup===0?equipment:relics.filter(x=>!s.relics.includes(x.id)).slice(0,3);footLeft=[btn('跳过','skip-reward')];footRight=[btn('领取','claim-reward','','primary',!s.rewardPick)];return [heading(s.rewardGroup===0?'战利品 · 装备':'战利品 · 遗物'),box('step',el('span',{class:s.rewardGroup===0?'active':''},icon(s.claimed.includes('equipment')?'check':'swords'),' 装备'),el('span',{class:s.rewardGroup===1?'active':''},icon(s.claimed.includes('relic')?'check':'gem'),' 遗物')),box('grid3 grow',(s.scenario==='single'?items.slice(0,1):items).map(x=>itemCard(x,'reward')))];};
renderers.result=function(){footRight=[btn('主菜单','menu'),btn('再次登塔','new','','primary')];return [box('menu',box('stack',el('div',{class:'resultmark'},s.outcome==='win'?'登塔成功':s.outcome==='abandon'?'征程已放弃':'征程结束'),muted('第 '+Math.min(s.floor,15)+' 层'),box('row',...s.roster.slice(0,5).map(id=>btn(art(id,'unit-thumb'),'hero-detail',id,'ghost',false,{'aria-label':hero(id).name})))))];};
function boardView(battle){return box('board',box('homegrid',s.board.map((id,i)=>btn(id?[art(id),txt(hero(id).name)]:icon('plus'),id?'unit':'empty',id||String(i),id?'occupied'+(s.unit===id?' selected':''):'',false,{'data-cell':i,draggable:!!id&&!battle,'data-drag':id?'hero:'+id:null,'aria-label':id?hero(id).name+' 部署位 '+(i+1):'空部署位 '+(i+1)}))),box('enemyfield',enemies[s.opponent].units.map((u,i)=>btn(u,'enemy-unit',String(i)))));}
const previousDialogContent=dialogContent;
function cleanDialog(){
 const m=s.modal;if(!m)return [];
 if(m.kind==='hero'){const x=hero(m.data);return [box('dialog-hero',art(x.id),box('stack',el('h2',{},x.name),txt(x.trait,'gold'),el('p',{},x.desc),box('metricrow',...([['攻击',x.atk],['生命',x.hp],['法强',x.power],['法力','20 / 60']].map(([a,b])=>box('',txt(a),txt(b)))))))];}
 if(m.kind==='enemy-unit')return [el('h2',{},enemies[s.opponent].units[Number(m.data)||0]),el('p',{},enemies[s.opponent].pressure),btn('敌阵','enemy-detail',String(s.opponent))];
 if(m.kind==='trait')return [box('row',icon(traitIcon(m.data)),el('h2',{},m.data)),box('row',...deployed().filter(id=>hero(id).trait.includes(m.data)).map(id=>btn([art(id,'unit-thumb'),hero(id).name],'hero-detail',id))),txt(deployed().filter(id=>hero(id).trait.includes(m.data)).length+' / '+(m.data==='霜羽'?4:2),'gold')];
 if(m.kind==='rules')return [el('h2',{},'战场规则'),el('p',{},'歼灭敌军'),box('row',icon('timer'),txt('02:00')),el('p',{},'英雄以满生命入场。')];
 if(m.kind==='quit')return [el('h2',{},'军团登塔'),btn('返回主菜单','menu','','primary')];
 if(m.kind==='report-unit'){const enemy=s.reportSide==='enemy',name=(enemy?enemies[s.opponent].units:s.roster.map(id=>hero(id).name))[Number(m.data)]||'单位';return [el('h2',{},name),box('metricrow',...([['有效输出','480'],['承受伤害','320'],['有效治疗','0'],['护盾','36']].map(([a,b])=>box('',txt(a),txt(b)))))];}
 return previousDialogContent();
}
const presentationOnlyText=[
 '点击单位查看该维度的明细。','基础收益自动入账；装备与遗物按实际奖励组分别领取。','普通／精英败战仍可继续，生命耗尽或首领失利则结束征程。','保留颜色、焦点和操作结果反馈。','本局累计培养：按英雄逐项列出永久攻击、生命等已提交收益。','站位区域示意；以该遭遇的实际配置为准。','失败代价：按普通／精英／首领规则分别显示','审阅跳转会装入适合该页的示例状态；游戏内按钮沿主流程前进。','以下仅为审阅控制，不是游戏里的操作按钮。'
];
let presentationPage='',presentationModal='',previousSelection=[],previousResources={},feedbackOrigin=null,toastTimer=0,toastSeen='';
function animate(node,keyframes,options){if(node&&!s.reduced&&!matchMedia('(prefers-reduced-motion: reduce)').matches)return node.animate(keyframes,options);}
function refreshPresentation(){
 screen.dataset.page=s.page;root.dataset.reduced=String(s.reduced);
 screen.querySelectorAll('p,span').forEach(n=>{if(presentationOnlyText.includes(n.textContent))n.remove();});
 screen.querySelectorAll('.roster-detail>.muted').forEach(n=>n.remove());
 screen.querySelectorAll('[data-drop-hero]').forEach(n=>n.prepend(art(n.dataset.dropHero,'unit-thumb')));
 screen.querySelectorAll('[data-slot]').forEach(n=>{const id=(s.loadout[s.unit]||[])[Number(n.dataset.slot)];if(id)n.prepend(art(id.split('@')[0],'unit-thumb'));else{n.replaceChildren(icon('plus'));n.setAttribute('aria-label','空装备槽 '+(Number(n.dataset.slot)+1));}});
 screen.querySelectorAll('[data-act=inventory]').forEach(n=>n.prepend(art(n.dataset.value.split('@')[0],'unit-thumb')));
 screen.querySelectorAll('.itemmark').forEach(n=>{if(n.children.length)return;const map={'冒险':'door-open','稳妥':'footprints','休养':'flame','补给':'coins'};const name=map[n.textContent];if(name)n.replaceChildren(icon(name));});
 if(globalThis.lucide)lucide.createIcons({root,attrs:{width:17,height:17}});
 const pageChanged=presentationPage!==s.page;
 if(pageChanged){
  if(s.page==='opening')screen.querySelectorAll('.hero-card').forEach((n,i)=>animate(n,[{opacity:0,transform:'translateY(-16px) scale(1.07)'},{opacity:1,transform:'translateY(2px) scale(.993)',offset:.78},{opacity:1,transform:'translateY(0) scale(1)'}],{duration:350,delay:i*42,fill:'backwards',easing:'cubic-bezier(.18,.7,.2,1)'}));
  else animate(screen.querySelector('.content'),[{opacity:0,transform:'translateY(9px)'},{opacity:1,transform:'translateY(0)'}],{duration:230,easing:'cubic-bezier(.2,.8,.2,1)'});
  presentationPage=s.page;
 }
 if(s.modal?.kind&&presentationModal!==s.modal.kind)animate(screen.querySelector('.dialog'),[{opacity:0,transform:'translateY(-12px) scale(1.045)'},{opacity:1,transform:'translateY(2px) scale(.997)',offset:.78},{transform:'translateY(0) scale(1)'}],{duration:250,easing:'cubic-bezier(.18,.74,.2,1)'});
 presentationModal=s.modal?.kind||'';
 for(const id of s.selected){if(!previousSelection.includes(id)&&!pageChanged){const seat=screen.querySelector('[data-seat="'+id+'"]');animate(seat,[{boxShadow:'0 0 0 0 #d5b77588'},{boxShadow:'0 0 0 12px #d5b77500'}],{duration:400});if(feedbackOrigin&&seat&&!s.reduced&&!matchMedia('(prefers-reduced-motion: reduce)').matches){const a=feedbackOrigin,b=seat.getBoundingClientRect(),r=root.getBoundingClientRect();const flight=box('pointer-flight',art(id));Object.assign(flight.style,{left:(a.x-r.left-19)+'px',top:(a.y-r.top-19)+'px',width:'38px',height:'38px'});root.append(flight);const animation=animate(flight,[{transform:'translate(0,0) scale(1.3)',opacity:1},{transform:'translate('+(b.x+b.width/2-a.x)+'px,'+(b.y+b.height/2-a.y)+'px) scale(1)',opacity:0.4}],{duration:260,easing:'cubic-bezier(.2,.8,.25,1)'});animation?.finished.then(()=>flight.remove()).catch(()=>flight.remove());}}}
 if(s.page==='opening'&&s.selected.length===2&&previousSelection.length!==2)animate(screen.querySelector('[data-act=confirm-opening]'),[{transform:'scale(.97)'},{transform:'scale(1.035)',offset:.65},{transform:'scale(1)'}],{duration:300,easing:'ease-out'});
 previousSelection=[...s.selected];feedbackOrigin=null;
 for(const key of ['gold','health','materials','research']){if(previousResources[key]!==undefined&&previousResources[key]!==s[key])animate(screen.querySelector('[data-resource='+key+']'),[{transform:'scale(1.2)',color:'#f3d492'},{transform:'scale(1)',color:''}],{duration:300});previousResources[key]=s[key];}
 if(s.toast){const error=/失败|未成功|不足|已满|不能|变化/.test(s.toast),toast=box('toast'+(error?' error':''),icon(error?'circle-alert':'check'),txt(s.toast));toast.setAttribute('role',error?'alert':'status');screen.append(toast);if(globalThis.lucide)lucide.createIcons({root:toast,attrs:{width:17,height:17}});if(toastSeen!==s.toast)animate(toast,[{opacity:0,translate:'0 5px'},{opacity:1,translate:'0 0'}],{duration:160});toastSeen=s.toast;clearTimeout(toastTimer);if(!error)toastTimer=setTimeout(()=>{screen.querySelector('.toast')?.remove();s.toast='';},1800);}
}
root.addEventListener('click',e=>{const b=e.target.closest('[data-act=choose]');if(b){const r=b.getBoundingClientRect();feedbackOrigin={x:e.clientX||r.x+r.width/2,y:e.clientY||r.y+r.height/2};}},true);
const sampleReview=box('sample-review',txt('六选二'),btn('未选','sample-state','0'),btn('选一位','sample-state','1'),btn('选两位','sample-state','2'),btn('重播入场','sample-state','replay'));
root.querySelector('.review').append(sampleReview);
root.addEventListener('click',e=>{
 const b=e.target.closest('[data-act=sample-state]');if(!b)return;
 e.stopImmediatePropagation();root.querySelectorAll('.pointer-flight').forEach(n=>n.remove());
 s={...fresh(),page:'opening',openingSample:2,opening:openingSampleIds,selected:openingSampleIds.slice(0,b.dataset.value==='replay'?0:Number(b.dataset.value)),roster:[],board:Array(18).fill(null),draft:true,resume:'opening',floor:1};
 previousSelection=[...s.selected];if(b.dataset.value==='replay')presentationPage='';render();persist();
});
