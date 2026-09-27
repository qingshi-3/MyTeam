const $=id=>document.getElementById(id);
const rows=await fetch('matches.json').then(r=>r.json());
function render(){
 const query=$('search').value.trim().toLowerCase();
 const shown=rows.filter(r=>($('retained').checked||r.roster==='正式')&&($('filter').value==='all'||r.status===$('filter').value)&&[r.id,r.hero,r.skill,r.asset,r.note].join(' ').toLowerCase().includes(query));
 $('rows').replaceChildren(...shown.map(r=>{
  const tr=document.createElement('tr');
  const hero=document.createElement('td');const name=document.createElement('strong');name.textContent=r.hero;const skill=document.createElement('small');skill.textContent=`${r.id} · ${r.skill}${r.roster==='保留'?' · 保留':''}`;hero.append(name,skill);
  const asset=document.createElement('td');const link=document.createElement(r.url?'a':'span');link.textContent=r.asset;if(r.url){link.href=r.url;link.target='_blank';link.rel='noreferrer'}asset.append(link);const availability=document.createElement('small');availability.textContent=r.availability;asset.append(availability);
  const status=document.createElement('td');status.textContent=r.status;const note=document.createElement('td');note.textContent=r.note;tr.append(hero,asset,status,note);return tr;
 }));
 $('count').textContent=`${shown.length}名英雄`;$('empty').hidden=shown.length!==0;
}
for(const id of ['search','filter','retained'])$(id).addEventListener('input',render);
for(const button of document.querySelectorAll('.demo-toggle'))button.addEventListener('click',()=>{
 const target=$(button.dataset.target);
 if(target.querySelector('img')){target.replaceChildren();const text=document.createElement('p');text.textContent='演示已停止';target.append(text);button.textContent='播放作者演示';return;}
 const img=document.createElement('img');img.alt='作者公开动画演示';img.referrerPolicy='no-referrer';img.src=button.dataset.url;img.onerror=()=>{target.textContent='演示暂时未能加载，请打开上方作者页面查看。';button.textContent='重新加载作者演示';};target.replaceChildren(img);button.textContent='停止演示';
});
render();
