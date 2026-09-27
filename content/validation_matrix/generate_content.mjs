import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const dir=path.dirname(fileURLToPath(import.meta.url)),root=path.resolve(dir,'../..');
const plan=JSON.parse(await fs.readFile(path.join(root,'design-discussion/04-content-validation/artifacts/trait-matrix/plan.json'),'utf8'));
const K='Damage Heal Shield Mana Chill Poison Ember Counter ClearCounter PayHealth ConsumeShield ConsumePoison TransferPoison TimedAttribute NextSkillBoost NextAttackBoost ExtendSummon Summon RegisterReaction Delay DashStrike DeathGift EchoDeathGift Taunt SkillVolley LineDamage ScalePoison ExtraShot Sequence'.split(' ');
const T='Self CurrentEnemy NearestEnemies LowestHealthEnemies MostPoisonEnemies MostEmberEnemies DenseEnemies LowestHealthAllies NearestAllies NearestCaster OwnedSummons TemporaryAllies EventSource EventTarget EventNearbyEnemies EventNearbyAllies LowestManaAllies FrontAlly OtherEnemy EventVictim NearWoundedAllyEnemies NearestSummoner NearEventSourceAllies FrontEnemies NearestWoundedAlly NearestBoostableCaster'.split(' ');
const E='Tick AttackHit SkillHit HealthLost ShieldReceived ShieldBroken Healing ManaCast Death Control PoisonTransferred EmberDetonated SummonHit PoisonApplied'.split(' ');
const R='Any Owner Ally OtherAlly Enemy OwnedSummon'.split(' '),C='None Poisoned Chilled EmberMarked Controlled LowHealth Shielded Unshielded Frozen'.split(' ');
const op=(Kind,Target='Self',more={})=>({Kind,Target,...more});
const dmg=(ratio=1,more={})=>op('Damage','CurrentEnemy',{AttackRatio:ratio,...more});
const shield=(amount,target='Self',more={})=>op('Shield',target,{Amount:amount,DurationTicks:30,...more});
const heal=(amount,target='LowestHealthAllies',more={})=>op('Heal',target,{Amount:amount,...more});
const mana=(amount,target='Self',more={})=>op('Mana',target,{Amount:amount,...more});
const rule=(Event,SourceRelation,TargetRelation,Effects,more={})=>op('RegisterReaction','Self',{Event,SourceRelation,TargetRelation,Effects,...more});
const counter=(key,amount=1,max=5,more={})=>op('Counter','Self',{Key:key,Amount:amount,Maximum:max,...more});
const clear=key=>op('ClearCounter','Self',{Key:key});
const attr=(attribute,amount,duration=30,more={})=>op('TimedAttribute','Self',{Attribute:attribute,Amount:amount,DurationTicks:duration,...more});
const boost=(amount=.2,more={})=>op('NextSkillBoost','Self',{Amount:amount,DurationTicks:300,Key:'next-skill',...more});
const nextAttack=(amount=.3,more={})=>op('NextAttackBoost','Self',{Amount:amount,DurationTicks:100,Key:'next-attack',...more});
const poison=(target='EventTarget',amount=1,more={})=>op('Poison',target,{Amount:amount,...more});
const chill=(target='EventTarget',amount=1,more={})=>op('Chill',target,{Amount:amount,...more});
const ember=(target='EventTarget',amount=1,more={})=>op('Ember',target,{Amount:amount,...more});
const dash=(target='LowestHealthEnemies',ratio=1.5)=>op('DashStrike',target,{AttackRatio:ratio,Range:4,DurationTicks:4});
const volley=(count=3,ratio=.65,max=1)=>op('SkillVolley','CurrentEnemy',{Count:count,AttackRatio:ratio,Amount:.45,MaxTargets:max,Range:7});
const summon=(name,count=1)=>op('Summon','Self',{ContentId:`matrix_spirit_${name}`,Count:count,Maximum:3,DurationTicks:100});
const area=(ratio=1,radius=1.5,more={})=>dmg(ratio,{Target:'DenseEnemies',Range:7,Radius:radius,MaxTargets:8,...more});
const tick=(effects,more={})=>rule('Tick','Any','Any',effects,{IntervalTicks:5,...more});
const deathGift=target=>op('DeathGift','Self',{Effects:[shield(35,target,{DurationTicks:35,ExcludeSelf:true})]});
const units={
  1:{a:[dmg(1.4),shield(45)],p:[rule('AttackHit','Enemy','Owner',[chill('EventSource')],{CooldownTicks:15})]},
  2:{a:[dmg(1.7),dmg(.65,{TargetCondition:'Controlled'})],p:[rule('AttackHit','Owner','Enemy',[shield(24)],{TargetCondition:'Frozen',OncePerTarget:true})]},
  3:{a:[volley(5,.55,3)],p:[rule('AttackHit','Owner','Enemy',[counter('chill-chain',1,3,{ResetOnTargetChange:true}),chill('EventTarget',1,{RequiredCounter:'chill-chain',CounterThreshold:3,ConsumeCounter:true})])]},
  4:{a:[op('Delay','DenseEnemies',{Range:7,Radius:1.6,Count:3,DurationTicks:4,IntervalTicks:10,Effects:[area(.45,1.6),chill('CurrentEnemy',1,{Radius:1.6,MaxTargets:6})]})],p:[rule('SkillHit','Owner','Enemy',[mana(8)],{TargetCondition:'Chilled',OncePerAction:true})]},
  5:{a:[heal(55),shield(25,'LowestHealthAllies')],p:[rule('Control','Ally','Enemy',[heal(18)],{Range:3,CooldownTicks:25,TargetCondition:'Frozen',OncePerTarget:true})]},
  6:{a:[op('Taunt','NearestEnemies',{Range:2.5,MaxTargets:5,DurationTicks:30}),attr(4,10)],p:[rule('AttackHit','Enemy','Owner',[poison('EventSource')],{CooldownTicks:20,PerTargetCooldown:true})]},
  7:{a:[volley(3,.75)],p:[rule('AttackHit','Owner','Enemy',[poison()]),rule('SkillHit','Owner','Enemy',[poison()],{CooldownTicks:0})]},
  8:{a:[{...dash('MostPoisonEnemies',1.1),Effects:[op('ConsumePoison','CurrentEnemy',{Count:3,Key:'poison-spent'}),dmg(0,{Key:'poison-spent',CounterRatio:12,ConsumeCounter:true})]}],p:[rule('Death','Owner','Enemy',[shield(35)],{TargetCondition:'Poisoned',CooldownTicks:10})]},
  9:{a:[op('Sequence','DenseEnemies',{Range:7,Radius:1.5,MaxTargets:1,Key:'cloud-radius',RadiusPerCounter:.15,Effects:[area(.7,1.5,{Key:'cloud-radius',RadiusPerCounter:.15}),poison('DenseEnemies',2,{Range:7,Radius:1.5,MaxTargets:6,Key:'cloud-radius',RadiusPerCounter:.15}),op('ScalePoison','CurrentEnemy',{Amount:.4})]}),clear('cloud-radius')],p:[rule('SkillHit','Owner','Enemy',[counter('cloud-radius',1,4)],{TargetCondition:'Poisoned'})]},
  10:{a:[heal(60),attr(1,-8,25,{Target:'NearWoundedAllyEnemies',TargetCondition:'Poisoned',Range:3,MaxTargets:4})],p:[rule('PoisonApplied','Ally','Enemy',[shield(18,'EventVictim')],{CounterThreshold:3,CooldownTicks:30,PerTargetCooldown:true})]},
  11:{a:[dmg(1.6,{Target:'FrontEnemies',Range:2,MaxTargets:5,Key:'blade-width',RadiusPerCounter:.15}),clear('blade-width')],p:[rule('HealthLost','Enemy','Owner',[counter('blade-width',1,4)],{MinimumEventValue:.001})]},
  12:{a:[op('LineDamage','CurrentEnemy',{AttackRatio:2,Range:7,Radius:.35})],p:[rule('AttackHit','Owner','Enemy',[mana(4)],{TargetCondition:'EmberMarked',CooldownTicks:5})]},
  13:{a:[{...dash('MostEmberEnemies',2),LandingSide:true}],p:[rule('EmberDetonated','Owner','Enemy',[nextAttack(.35)])]},
  14:{a:[op('Delay','DenseEnemies',{Range:7,Radius:2,DurationTicks:8,Effects:[area(2.4,2)]})],p:[rule('SkillHit','Owner','Enemy',[mana(15)],{TargetCondition:'EmberMarked',Every:2,OncePerAction:true})]},
  15:{a:[summon('fire')],p:[rule('Death','Any','OwnedSummon',[ember('EventNearbyEnemies',1,{Range:1.5,MaxTargets:5})])]},
  16:{a:[attr(4,8,40),shield(40,'NearestSummoner',{ExcludeSelf:true})],p:[rule('Death','Any','Ally',[shield(18,'Self',{ShieldCap:65})],{TemporaryOnly:true,Range:3,CooldownTicks:10}),deathGift('NearestAllies')]},
  17:{a:[dmg(1.6,{Target:'NearestEnemies',Range:2,MaxTargets:5,Key:'death-power',CounterRatio:7})],p:[rule('Death','Any','OtherAlly',[counter('death-power',1,5,{TemporaryScale:.25})],{OncePerTarget:true})]},
  18:{a:[dash('LowestHealthEnemies',1.9)],p:[rule('Death','Any','OtherAlly',[boost(.35)],{CooldownTicks:5})]},
  19:{a:[shield(40,'LowestHealthAllies',{MaxTargets:2}),op('EchoDeathGift','LowestHealthAllies',{MaxTargets:2})],p:[deathGift('LowestHealthAllies')]},
  20:{a:[summon('bone',2)],p:[rule('Death','Any','Ally',[dmg(0,{Target:'EventNearbyEnemies',Range:1.3,MaxTargets:5,EventTargetHealthRatio:.2})],{TemporaryOnly:true})]},
  21:{a:[shield(80,'Self',{DurationTicks:45})],p:[rule('ShieldBroken','Enemy','Owner',[attr(4,6,30)],{CooldownTicks:30})]},
  22:{a:[op('ConsumeShield','Self',{Amount:.5,Key:'shield-spent'}),dmg(1.6,{Target:'NearestEnemies',Range:2,MaxTargets:5,Key:'shield-spent',CounterRatio:.8,ConsumeCounter:true})],p:[rule('ShieldReceived','OtherAlly','Owner',[counter('shield-gifts',1,5),attr(1,0,1000,{Key:'shield-gifts',CounterRatio:2})],{MinimumEventValue:.001,CooldownTicks:5})]},
  23:{a:[{...volley(5,.55),EventRatio:.05}],p:[rule('AttackHit','Owner','Enemy',[counter('shield-speed',1,8,{ResetOnTargetChange:true}),attr(3,0,15,{Key:'shield-speed',CounterRatio:.025})],{OwnerCondition:'Shielded'}),tick([counter('shield-speed',-1,8),attr(3,0,15,{Key:'shield-speed',CounterRatio:.025})],{OwnerCondition:'Unshielded',IntervalTicks:10})]},
  24:{a:[op('LineDamage','CurrentEnemy',{AttackRatio:2.2,Range:7,Radius:.3})],p:[rule('SkillHit','Owner','Enemy',[shield(35,'FrontAlly')],{Every:2,OncePerAction:true})]},
  25:{a:[summon('drone')],p:[rule('SummonHit','OwnedSummon','Enemy',[shield(25,'NearEventSourceAllies')],{OncePerSource:true})]},
  26:{a:[op('PayHealth','Self',{Amount:.1}),dash('NearestEnemies',1.3),attr(1,10,40)],p:[tick([attr(3,.25,6)],{OwnerCondition:'LowHealth',HealthThreshold:.6})]},
  27:{a:[op('PayHealth','Self',{Amount:.08}),volley(4,.8)],p:[tick([attr(3,.2,6)],{OwnerCondition:'LowHealth',TargetCondition:'Shielded',HealthThreshold:.6,IntervalTicks:1})]},
  28:{a:[dash('LowestHealthEnemies',1.8)],p:[rule('Death','Owner','Enemy',[heal(0,'Self',{OwnerHealthRatio:.12,Maximum:45})],{CooldownTicks:5})]},
  // Starting budget: release half the stored pain, convert that paid half at 60%, preserve the remainder.
  29:{a:[area(1.4,1.6,{Key:'pain',CounterRatio:.3,ConsumeCounter:true,CounterConsumeRatio:.5})],p:[rule('HealthLost','Enemy','Owner',[counter('pain',0,120,{EventRatio:1,ClearOnDeath:true})],{MinimumEventValue:.001})]},
  30:{a:[op('PayHealth','Self',{Amount:.08}),heal(55,'LowestHealthAllies',{MaxTargets:2})],p:[rule('Healing','Owner','Ally',[shield(20,'EventTarget')],{TargetCondition:'LowHealth',MinimumEventValue:.001,CooldownTicks:1})]},
  31:{a:[shield(60),shield(30,'NearestCaster',{ExcludeSelf:true})],p:[rule('ManaCast','OtherAlly','Any',[shield(18)],{Range:3,CooldownTicks:20})]},
  32:{a:[dmg(2.3),nextAttack(.4),counter('double-shot',1,1)],p:[rule('AttackHit','Owner','Enemy',[op('ExtraShot','EventTarget',{AttackRatio:.55}),clear('double-shot')],{RequiredCounter:'double-shot',CounterThreshold:1})]},
  33:{a:[dmg(2.3)],p:[rule('ManaCast','OtherAlly','Any',[boost(.3)],{Range:3})]},
  34:{a:[heal(55,'LowestHealthAllies',{MaxTargets:2})],p:[rule('Healing','Owner','Ally',[mana(8,'LowestManaAllies',{ExcludeSelf:true})],{MinimumEventValue:.001,OncePerAction:true,ActiveSkillOnly:true})]},
  35:{a:[summon('astral')],p:[rule('ManaCast','OtherAlly','Any',[op('Counter','OwnedSummons',{Key:'astral-growth',Amount:1,Maximum:5,MaxTargets:3}),attr(1,0,100,{Target:'OwnedSummons',MaxTargets:3,Key:'astral-growth',CounterRatio:2,CounterOnTarget:true})],{CooldownTicks:5})]},
  36:{a:[{...dash('MostPoisonEnemies',1.5),Effects:[chill('CurrentEnemy')]}],p:[rule('AttackHit','Owner','Enemy',[poison('EventTarget',2)],{TargetCondition:'Chilled',CooldownTicks:20})]},
  37:{a:[summon('plague')],p:[rule('SummonHit','OwnedSummon','Enemy',[poison()]),rule('Death','Any','OwnedSummon',[ember('EventNearbyEnemies',1,{Range:1.5,MaxTargets:5})])]},
  38:{a:[area(2.1,2),ember('DenseEnemies',1,{Radius:2,MaxTargets:7,Key:'death-ember',CounterRatio:.5,ConsumeCounter:true})],p:[rule('Death','Any','OtherAlly',[counter('death-ember',1,4,{TemporaryScale:.25})],{OncePerTarget:true})]},
  39:{a:[shield(55),shield(30,'TemporaryAllies',{Range:3,MaxTargets:3})],p:[rule('Death','Any','Any',[shield(20,'Self',{DurationTicks:20})],{OncePerTarget:true,CooldownTicks:10,Range:3})]},
  40:{a:[op('ConsumeShield','Self',{Amount:.4,Key:'shield-spent'}),op('PayHealth','Self',{Amount:.08}),dmg(1.8,{Target:'NearestEnemies',Range:2,MaxTargets:4,Key:'shield-spent',CounterRatio:.7,ConsumeCounter:true})],p:[rule('ShieldReceived','OtherAlly','Owner',[nextAttack(.45)],{OwnerCondition:'LowHealth',OncePerBattle:true,MinimumEventValue:.001})]},
  41:{a:[op('PayHealth','Self',{Amount:.06}),mana(15,'LowestManaAllies',{MaxTargets:2,ExcludeSelf:true}),heal(25,'LowestManaAllies',{MaxTargets:2,ExcludeSelf:true})],p:[rule('Healing','OtherAlly','Owner',[boost(.35)],{MinimumEventValue:.001})]},
  42:{a:[summon('aurora')],p:[rule('SummonHit','OwnedSummon','Enemy',[chill()],{CooldownTicks:15,PerTargetCooldown:true}),rule('ManaCast','Owner','Any',[op('ExtendSummon','OwnedSummons',{DurationTicks:20,Count:3})])]},
  43:{a:[shield(35,'NearestAllies',{Range:3,MaxTargets:3})],p:[rule('ShieldBroken','Enemy','Owner',[heal(35,'NearestWoundedAlly')],{CooldownTicks:30})]},
  44:{a:[op('SkillVolley','CurrentEnemy',{Count:4,AttackRatio:.7,Amount:.5,Key:'spell-arrows',CounterRatio:.08,ConsumeCounter:true})],p:[rule('AttackHit','Owner','Enemy',[counter('spell-arrows',1,4)])]},
  45:{a:[{...dash('LowestHealthEnemies',2),Key:'revenge',CounterRatio:8,ConsumeCounter:true}],p:[rule('HealthLost','Enemy','Owner',[counter('revenge',1,4)],{MinimumEventValue:.001}),rule('Death','Any','OtherAlly',[counter('revenge',1,4)],{OncePerTarget:true}),rule('Death','Owner','Enemy',[shield(30)],{CooldownTicks:10,ActiveSkillOnly:true})]},
  46:{a:[op('TransferPoison','OtherEnemy',{Count:3,Range:6})],p:[rule('PoisonTransferred','Owner','Enemy',[shield(25)],{CooldownTicks:25})]},
  47:{a:[boost(.5,{Target:'NearestBoostableCaster',ExcludeSelf:true,DurationTicks:120})],p:[rule('ManaCast','OtherAlly','Any',[mana(6)],{CooldownTicks:15})]},
  48:{a:[attr(1,12,45)],p:[rule('AttackHit','Owner','Enemy',[counter('focus',1,5,{ResetOnTargetChange:true}),nextAttack(0,{Key:'focus',CounterRatio:.06})])]},
  49:{native:true,a:[],p:[]}
};

const q=v=>JSON.stringify(v), resourceBase='res://content/validation_matrix';
const enumFields={Kind:K,Target:T,Event:E,SourceRelation:R,TargetRelation:R,TargetCondition:C,OwnerCondition:C};
const authoredCopyPath=/^(?:abilities|definitions)\/[^/]+\.tres$|^units\/[^/]+\.tscn$/;
const authoredCopyLine=/^(DisplayName|Description|RuleDescription|RuleTitle) = "(?:\\.|[^"\\])*"$/;
// Existing authored player copy takes precedence over historical design-draft wording;
// generation templates still initialize these fields for newly created content.
function preserveAuthoredCopy(relative,generated,existing){
 if(!authoredCopyPath.test(relative))return generated;
 const authored=new Map(existing.split(/\r?\n/).filter(line=>authoredCopyLine.test(line)).map(line=>[line.slice(0,line.indexOf(' = ')),line]));
 return generated.split(/(\r?\n)/).map(part=>{
  if(!authoredCopyLine.test(part))return part;
  return authored.get(part.slice(0,part.indexOf(' = ')))??part;
 }).join('');
}
async function write(relative,text){
 const out=path.join(dir,relative);await fs.mkdir(path.dirname(out),{recursive:true});
 if(authoredCopyPath.test(relative)){
  try{text=preserveAuthoredCopy(relative,text,await fs.readFile(out,'utf8'));}
  catch(error){if(error?.code!=='ENOENT')throw error;}
 }
 await fs.writeFile(out,text);
}
function abilityText(id,name,description,operations,trigger=5,manaCost=70){
 let serial=0,blocks=[];
 function node(data){const id=`op_${++serial}`;const children=(data.Effects??[]).map(node);let lines=[`[sub_resource type="Resource" id="${id}"]`,'script = ExtResource("operation")'];
 for(const [key,value]of Object.entries(data)){if(key==='Effects')continue;const val=enumFields[key]?enumFields[key].indexOf(value):value;if(enumFields[key]&&val<0)throw Error(`Unknown ${key}: ${value}`);lines.push(`${key} = ${typeof val==='string'?q(val):val}`);}
 if(children.length)lines.push(`Effects = Array[ExtResource("operation")]([${children.map(c=>`SubResource("${c}")`).join(', ')}])`);blocks.push(lines.join('\n'));return id;}
 const ids=operations.map(node);return `[gd_resource type="Resource" script_class="AbilityDefinition" format=3]\n\n[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityDefinition.cs" id="ability"]\n[ext_resource type="Script" path="res://src/Abilities/Authoring/MatrixAbilityOperationSpec.cs" id="operation"]\n\n${blocks.join('\n\n')}\n\n[resource]\nscript = ExtResource("ability")\nStableId = ${q(id)}\nDisplayName = ${q(name)}\nDescription = ${q(description)}\nActivationKind = 1\nTrigger = ${trigger}\nManaCost = ${manaCost}\nCooldownTicks = 1\nMaxUses = ${trigger===1?1:0}\nOperations = [${ids.map(i=>`SubResource("${i}")`).join(', ')}]\n`;
}
const roleStats={guard:[0,400,10,8,1.5,.25],fighter:[1,300,20,4,1.3,.25],ranger:[2,175,22,1,1.2,5.2],assassin:[4,200,24,1,1.1,.25],mage:[6,185,12,1,1.6,5.2],support:[3,235,12,2,1.5,5.2],summoner:[5,210,10,2,1.5,5.2]};
const fallback={guard:'hero_hc03_iron_guard',fighter:'hero_hc14_granite_brawler',ranger:'hero_hc01_crossbow',assassin:'hero_hc08_venom_hunter',mage:'hero_hc04_frost_mage',support:'hero_hc19_overheal_priest',summoner:'hero_hc20_bone_priest'};
const existingPortraits=(await fs.readdir(path.join(root,'content/portraits/heroes'))).filter(f=>f.endsWith('.tres'));
const findDonor=(unit,role)=>{const hc=unit.mapping?.match(/HC(\d+)/i)?.[1];return existingPortraits.find(f=>hc&&f.startsWith(`hero_hc${hc.padStart(2,'0')}_`))??existingPortraits.find(f=>f===fallback[role]+'.tres')??existingPortraits.find(f=>f.startsWith(role==='ranger'?'hero_hc01_':role==='guard'?'hero_hc03_':'hero_hc19_'));};
const traitMap={frost:'frost',poison:'poison',ember:'ember',death:'death',construct:'construct',blood:'blood',astral:'astral'};
const entries=[],summonEntries=[],manifestRecords=[];
async function writeUnit(id,name,description,role,donorFile,traits,stats,hero=true){
 let portrait=await fs.readFile(path.join(root,'content/portraits/heroes',donorFile),'utf8');portrait=portrait.replace(/StableId = "[^"]+"/,`StableId = "${id}"`).replace(/ uid="[^"]+"/g,'');await write(`portraits/${id}.tres`,portrait);
 const frames=portrait.match(/path="([^"]+frames\.tres)"/)?.[1];if(!frames)throw Error(`No frames ${donorFile}`);
 const [unitRole,hp,attack,armor,cooldown,range]=stats;
 const ext=`[ext_resource type="Script" path="res://src/Content/UnitDefinition.cs" id="script"]\n[ext_resource type="Resource" path="${resourceBase}/portraits/${id}.tres" id="portrait"]\n[ext_resource type="Script" path="res://src/Traits/Authoring/TraitContributionSpec.cs" id="trait"]`;
 const subs=traits.map((t,i)=>`[sub_resource type="Resource" id="t${i}"]\nscript = ExtResource("trait")\nTraitId = "mx_${t}"\nValue = 1`).join('\n\n');
 await write(`definitions/${id}.tres`,`[gd_resource type="Resource" script_class="UnitDefinition" format=3]\n\n${ext}\n\n${subs}\n\n[resource]\nscript = ExtResource("script")\nId = "${id}"\nDisplayName = ${q(name)}\nDescription = ${q(description)}\nPortrait = ExtResource("portrait")\nIsHero = ${hero}\nRole = ${unitRole}\nFaction = 6\nMaxHealth = ${hp}\nAttackDamage = ${attack}\nArmor = ${armor}\nAttackCooldown = ${cooldown}\nAttackRange = ${range}\nAttackDelivery = ${range>1?1:0}\nProjectileWindupSeconds = ${range>1?.35:0}\nMaxMana = ${hero?70:0}\nStartingMana = ${hero?20:0}\nManaPerSecond = 5\nManaPerAttack = 10\nTags = Array[StringName]([&"${hero?'hero':'soldier'}", &"matrix_validation", &"${role}"])\nTraitContributions = [${traits.map((t,i)=>`SubResource("t${i}")`).join(', ')}]\n`);
 await write(`units/${id}.tscn`,`[gd_scene format=3]\n[ext_resource type="Script" path="res://src/Content/UnitContentRoot.cs" id="root"]\n[ext_resource type="Resource" path="${resourceBase}/definitions/${id}.tres" id="definition"]\n[ext_resource type="SpriteFrames" path="${frames}" id="frames"]\n${['UnitMotionPresentationComponent','UnitAnimationComponent','HealthViewComponent','UnitBehaviorComponent','HeroRuleComponent','UnitAbilityLoadoutComponent'].map(c=>`[ext_resource type="PackedScene" path="res://scenes/components/${c}.tscn" id="${c}"]`).join('\n')}\n[ext_resource type="Resource" path="${resourceBase}/loadouts/${id}.tres" id="loadout"]\n\n[node name="${id}" type="Node2D"]\nscript = ExtResource("root")\nDefinition = ExtResource("definition")\n[node name="UnitMotionPresentationComponent" parent="." instance=ExtResource("UnitMotionPresentationComponent")]\n[node name="VisualRoot" type="Node2D" parent="."]\n[node name="UnitAnimationComponent" parent="VisualRoot" instance=ExtResource("UnitAnimationComponent")]\nFrames = ExtResource("frames")\n[node name="HealthViewComponent" parent="." instance=ExtResource("HealthViewComponent")]\n[node name="UnitBehaviorComponent" parent="." instance=ExtResource("UnitBehaviorComponent")]\n${hero?'[node name="HeroRuleComponent" parent="." instance=ExtResource("HeroRuleComponent")]\nRuleTitle = "矩阵验证单位"\nRuleDescription = "仅验证逻辑，复用现有表现。"\n':''}[node name="UnitAbilityLoadoutComponent" parent="." instance=ExtResource("UnitAbilityLoadoutComponent")]\nLoadout = ExtResource("loadout")\n`);
 await write(`entries/${id}.tres`,`[gd_resource type="Resource" script_class="CatalogEntry" format=3]\n[ext_resource type="Script" path="res://src/Content/CatalogEntry.cs" id="script"]\n[ext_resource type="PackedScene" path="${resourceBase}/units/${id}.tscn" id="scene"]\n[ext_resource type="Resource" path="${resourceBase}/definitions/${id}.tres" id="definition"]\n[resource]\nscript = ExtResource("script")\nScene = ExtResource("scene")\nDefinition = ExtResource("definition")\n`);
}
for(const unit of plan.units){
 const n=Number(unit.id.slice(2)),id=`hero_mx${String(n).padStart(2,'0')}`,content=units[n],role=unit.classes[0]??(n===46?'support':'mage');
 if(!content)throw Error(`No implementation ${unit.id}`);
 await writeUnit(id,unit.name,`${unit.role}。主动：${unit.active}\n被动：${unit.passive}\n独立验证内容，暂复用表现。`,role,findDonor(unit,role),[...unit.classes,...unit.systems.map(s=>traitMap[s])],roleStats[role]);
 if(content.native){
  for(const [suffix,spec,trigger,kind,props] of [['a','DuelAbilityOperationSpec',5,1,'DurationTicks = 40\nBreakDistance = 4'],['p','CounterattackAbilityOperationSpec',8,2,'HitsRequired = 3\nAttackRatio = 1.2\nLifestealRatio = 0.25']])
   await write(`abilities/${id}_${suffix}.tres`,`[gd_resource type="Resource" script_class="AbilityDefinition" format=3]\n[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityDefinition.cs" id="ability"]\n[ext_resource type="Script" path="res://src/Abilities/Authoring/${spec}.cs" id="operation"]\n[sub_resource type="Resource" id="op"]\nscript = ExtResource("operation")\n${props}\n[resource]\nscript = ExtResource("ability")\nStableId = "${id}_${suffix}"\nDisplayName = ${q(unit.name+(suffix==='a'?'·决斗':'·反击'))}\nDescription = ${q(suffix==='a'?unit.active:unit.passive)}\nActivationKind = ${kind}\nTrigger = ${trigger}\nManaCost = ${suffix==='a'?70:0}\nCooldownTicks = ${suffix==='a'?1:0}\nOperations = [SubResource("op")]\n`);
 }else{
  await write(`abilities/${id}_a.tres`,abilityText(`${id}_a`,`${unit.name}·主动`,unit.active,content.a));
  await write(`abilities/${id}_p.tres`,abilityText(`${id}_p`,`${unit.name}·被动`,unit.passive,content.p,1,0));
 }
 await write(`loadouts/${id}.tres`,`[gd_resource type="Resource" script_class="AbilityLoadoutDefinition" format=3]\n[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityLoadoutDefinition.cs" id="script"]\n[ext_resource type="Resource" path="${resourceBase}/abilities/${id}_a.tres" id="a"]\n[ext_resource type="Resource" path="${resourceBase}/abilities/${id}_p.tres" id="p"]\n[resource]\nscript = ExtResource("script")\nAbilities = [ExtResource("a"), ExtResource("p")]\n`);
 entries.push(id);manifestRecords.push({id,planId:unit.id,role,stats:roleStats[role],...content});
}
for(const [name,hp,attack,role]of [['bone',65,8,'fighter'],['fire',75,10,'fighter'],['drone',110,7,'guard'],['astral',60,12,'ranger'],['plague',65,8,'fighter'],['aurora',70,8,'ranger']]){
 const id=`matrix_spirit_${name}`,donor=findDonor({},role);await writeUnit(id,{bone:'短寿骷髅',fire:'短寿火灵',drone:'护卫工蜂',astral:'星灵',plague:'疫焰灵',aurora:'极光灵'}[name],'矩阵验证临时单位。不贡献羁绊与人口。',role,donor,[],[roleStats[role][0],hp,attack,0,1.5,role==='ranger'?3.5:.25],false);
 await write(`abilities/${id}_p.tres`,abilityText(`${id}_p`,'临时单位战斗行为','普通攻击依独立单位定义结算。',[tick([attr(1,0,2)],{IntervalTicks:20})],1,0));
 await write(`loadouts/${id}.tres`,`[gd_resource type="Resource" script_class="AbilityLoadoutDefinition" format=3]\n[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityLoadoutDefinition.cs" id="script"]\n[ext_resource type="Resource" path="${resourceBase}/abilities/${id}_p.tres" id="p"]\n[resource]\nscript = ExtResource("script")\nAbilities = [ExtResource("p")]\n`);summonEntries.push(id);
}
let catalog=await fs.readFile(path.join(root,'content/catalogs/alpha_catalog.tres'),'utf8');catalog=catalog.replace(/ uid="[^"]+"/g,'');
catalog=catalog.replace('[resource]',[...entries,...summonEntries].map(id=>`[ext_resource type="Resource" path="${resourceBase}/entries/${id}.tres" id="${id}"]`).join('\n')+'\n\n[resource]');
catalog=catalog.replace(/Heroes = Array\[CatalogEntry\]\(\[(.*?)\]\)/,(_,list)=>`Heroes = Array[CatalogEntry]([${list}, ${entries.map(id=>`ExtResource("${id}")`).join(', ')}])`);
catalog=catalog.replace(/Soldiers = Array\[CatalogEntry\]\(\[(.*?)\]\)/,(_,list)=>`Soldiers = Array[CatalogEntry]([${list}, ${summonEntries.map(id=>`ExtResource("${id}")`).join(', ')}])`);
await write('matrix_catalog.tres',catalog);
let rules=await fs.readFile(path.join(root,'content/project/alpha_run_rules.tres'),'utf8');rules=rules.replace(/ uid="[^"]+"/g,'')+'\nInitialPopulation = 10\nOrdinaryPopulationCap = 10\n';await write('matrix_run_rules.tres',rules);
let project=await fs.readFile(path.join(root,'content/project/alpha_project.tres'),'utf8');project=project.replace('res://content/catalogs/alpha_catalog.tres',`${resourceBase}/matrix_catalog.tres`).replace('res://content/project/alpha_run_rules.tres',`${resourceBase}/matrix_run_rules.tres`).replace('project_my_team_alpha','project_matrix_validation');await write('matrix_project.tres',project);
let scene=await fs.readFile(path.join(root,'scenes/app/GameRoot.tscn'),'utf8');scene=scene.replace(/ uid="[^"]+"/g,'').replace('res://src/App/GameRoot.cs','res://src/ValidationMatrix/ValidationMatrixRoot.cs').replace('res://content/project/alpha_project.tres',`${resourceBase}/matrix_project.tres`).replace('ProjectDefinition = ExtResource("17")','ProjectDefinition = ExtResource("17")\nSaveNamespace = "matrix-validation"');
const quickUnits=[1,3,4,5].map((n,i)=>({InstanceId:`matrix-quick-${i}`,ContentId:`hero_mx${String(n).padStart(2,'0')}`,Side:0,X:i===0?2:1,Y:i,Equipment:[],RetainAttackStacks:false}));
for(let i=0;i<3;i++)quickUnits.push({InstanceId:`matrix-target-${i}`,ContentId:i===2?'soldier_dummy_ranged':'soldier_dummy_melee',Side:1,X:7,Y:i+1,Equipment:[],RetainAttackStacks:false});
const quickPreset={SchemaVersion:1,Mode:0,CurrentPopulation:10,Seed:20260926,FloorRuleId:'rule_clear',PrimaryHeroInstanceId:'matrix-quick-0',Units:quickUnits,Relics:[]};
let presets=await fs.readFile(path.join(root,'content/battle-lab/battle_lab_presets.tres'),'utf8');
presets=presets.replace('[resource]',`[sub_resource type="Resource" id="matrix_quick"]\nscript = ExtResource("2")\nDisplayName = "矩阵·霜寒快速验证"\nDescription = "MX01、MX03、MX04、MX05：观察寒意、冻结、有效治疗与技能回蓝。"\nPresetJson = ${q(JSON.stringify(quickPreset))}\n\n[resource]`).replace('DefaultPresetName = "默认配置"','DefaultPresetName = "矩阵·霜寒快速验证"').replace('Presets = [','Presets = [SubResource("matrix_quick"), ');
await write('matrix_lab_presets.tres',presets);
scene=scene.replace('[node name="GameRoot"',`[ext_resource type="Resource" path="${resourceBase}/matrix_lab_presets.tres" id="matrix_presets"]\n\n[node name="GameRoot"`).replace(/(\[node name="BattleLabScreen"[^\n]*\n)/,'$1PresetCatalog = ExtResource("matrix_presets")\n');
await fs.writeFile(path.join(root,'scenes/app/ValidationMatrix.tscn'),scene);
async function files(relative,extension='.tres'){const full=path.join(root,relative);let list=[];for(const entry of await fs.readdir(full,{withFileTypes:true})){const file=path.join(relative,entry.name);if(entry.isDirectory())list.push(...await files(file,extension));else if(entry.name.endsWith(extension))list.push('res://'+file.replaceAll('\\','/'));}return list;}
const manifests={Loadouts:[...await files('content/abilities/loadouts'),...await files('content/validation_matrix/loadouts')],Abilities:[...await files('content/abilities/automatic'),...await files('content/abilities/commands'),...await files('content/abilities/triggered'),...await files('content/abilities/passive'),...await files('content/validation_matrix/abilities')],Statuses:await files('content/statuses'),Relics:await files('content/relics/definitions'),Equipment:await files('content/equipment/definitions'),Traits:[...await files('content/traits/definitions'),...plan.professions.concat(plan.systems).map(t=>`${resourceBase}/traits/mx_${t.id}.tres`)],TacticalCommands:await files('content/tactical-commands/definitions'),TacticalCommandScenes:await files('content/tactical-commands/commands','.tscn')};
await write('manifest.json',JSON.stringify(manifests,null,2));await write('ability_manifest.json',JSON.stringify(manifestRecords,null,2));
console.log(JSON.stringify({heroes:entries.length,summons:summonEntries.length,abilities:manifestRecords.length*2+summonEntries.length}));
