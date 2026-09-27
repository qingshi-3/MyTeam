import fs from 'node:fs';
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
const source = fs.readFileSync(new URL('./plan.json', import.meta.url));
const p = JSON.parse(source);
const ids = new Set(p.units.map(u => u.id));
assert.equal(ids.size, p.units.length);
const choose = (n,k) => { let r=1; for(let i=1;i<=k;i++)r=r*(n-i+1)/i; return Math.round(r); };
const axes = [];
for (const [field, items] of [['classes', p.professions], ['systems', p.systems]]) {
  const legal = new Set(items.map(a=>a.id));
  for(const u of p.units){ assert.equal(new Set(u[field]).size,u[field].length); assert.ok(u[field].every(t=>legal.has(t))); }
  for(const a of items){
    const units=p.units.filter(u=>u[field].includes(a.id));
    const low=units.filter(u=>u.tier<=2);
    const max=Math.max(...a.tiers.map(t=>t.count));
    assert.ok(units.length>max, a.id+' max alternatives');
    assert.ok(low.length>=a.tiers[0].count,a.id+' low entry');
    assert.ok(a.tiers.every(t=>t.effect.length>0));
    axes.push({name:a.name,members:units.length,lowTierMembers:low.length,maxThreshold:max,rawMaxTierSubsets:choose(units.length,max)});
  }
}
let appearances=0,empty=0;
for(const c of [...p.professions.map(a=>a.id),null])for(const s of [...p.systems.map(a=>a.id),null]){
  const members=p.units.filter(u=>(c?u.classes.includes(c):u.classes.length===0)&&(s?u.systems.includes(s):u.systems.length===0));
  appearances+=members.length;
  if(!members.length)empty++;
}
assert.equal(p.units.length,49); assert.equal(appearances,59);
assert.ok(p.units.some(u=>!u.classes.length)); assert.ok(p.units.some(u=>!u.systems.length));
assert.ok(p.units.every(u=>u.classes.length+u.systems.length<=3));
assert.ok(p.units.every(u=>u.mapping && ['候选改造','需补角色'].includes(u.mappingStatus)));
const primary=p.units.filter(u=>u.mappingStatus==='候选改造').map(u=>u.mapping.match(/^HC\d+/)?.[0]);
assert.ok(primary.every(Boolean)); assert.equal(new Set(primary).size,primary.length);
const builds=[
  {name:'七人霜毒',ids:[1,3,5,36,6,7,10],expect:{frost:4,poison:4,guard:2,ranger:2,support:2}},
  {name:'六人混合射手',ids:[3,23,32,48,1,21],expect:{ranger:4,frost:2,construct:2,guard:2}},
  {name:'六人星辉术士',ids:[4,9,14,33,31,34],expect:{mage:4,astral:3}},
  {name:'六人亡契召唤',ids:[20,16,19,39,25,15],expect:{death:4,summoner:3,guard:2,construct:2}},
  {name:'六人血誓构装',ids:[26,30,40,21,22,43],expect:{blood:3,construct:4}},
  {name:'七人血誓四斗',ids:[26,30,40,21,22,43,49],expect:{blood:3,construct:4,fighter:4}},
  {name:'八人无桥霜毒',ids:[1,2,3,5,6,7,9,10],expect:{frost:4,poison:4}}
].map(b=>{
  const members=b.ids.map(n=>p.units.find(u=>u.id==='MX'+String(n).padStart(2,'0')));
  assert.equal(new Set(members).size,members.length);
  const counts={}; for(const u of members)for(const t of [...u.classes,...u.systems])counts[t]=(counts[t]??0)+1;
  for(const [tag,n] of Object.entries(b.expect))assert.ok(counts[tag]>=n,b.name+' '+tag);
  return {name:b.name,population:members.length,units:members.map(u=>u.id),counts};
});
const result={sourceSha256:crypto.createHash('sha256').update(source).digest('hex'),uniqueUnits:ids.size,appearances,emptyCells:empty,tiers:[...p.professions,...p.systems].reduce((s,a)=>s+a.tiers.length,0),candidateRedesigns:primary.length,newRoles:p.units.length-primary.length,axes,builds,scope:'静态结构校验；组合数量不代表同强度、不代表正式供给可达或模拟通过。'};
fs.writeFileSync(new URL('./structure-audit.json',import.meta.url),JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify(result));
