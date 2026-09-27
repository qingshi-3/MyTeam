$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '..\content\growth'
New-Item -ItemType Directory -Force $root, "$root\abilities", "$root\loadouts", "$root\definitions", "$root\entries", "$root\units", "$root\portraits", "$root\project", "$root\encounters", "$root\regions" | Out-Null
function Write-Utf8($path, $text) { [IO.File]::WriteAllText($path, $text, [Text.UTF8Encoding]::new($false)) }
function Copy-GrowthEncounter($name, [float]$health, [float]$damage) {
  $source = Join-Path $PSScriptRoot "..\content\project\encounters\$name.tres"
  $text = [IO.File]::ReadAllText($source)
  $culture = [Globalization.CultureInfo]::InvariantCulture
  $text = [Text.RegularExpressions.Regex]::Replace($text, 'EnemyHealthMultiplier = ([0-9.]+)', {
    param($match) 'EnemyHealthMultiplier = ' + (([float]::Parse($match.Groups[1].Value, $culture) * $health).ToString('0.######', $culture))
  })
  $text = [Text.RegularExpressions.Regex]::Replace($text, 'EnemyDamageMultiplier = ([0-9.]+)', {
    param($match) 'EnemyDamageMultiplier = ' + (([float]::Parse($match.Groups[1].Value, $culture) * $damage).ToString('0.######', $culture))
  })
  Write-Utf8 "$root\encounters\$name.tres" $text
}
function Copy-GrowthRegion($name) {
  $source = Join-Path $PSScriptRoot "..\content\tower\$name.tres"
  $text = [IO.File]::ReadAllText($source).Replace('res://content/project/encounters/', 'res://content/growth/encounters/')
  Write-Utf8 "$root\regions\$name.tres" $text
}

$baseIds = @('hero_mx01','hero_mx02','hero_mx03','hero_mx04','hero_mx05','hero_mx10','hero_mx21','hero_mx22','hero_mx23','hero_mx24','hero_mx25','hero_mx30','hero_mx36','hero_mx39','hero_mx40','hero_mx42','hero_mx43','hero_mx44','hero_mx45','hero_mx46','hero_mx47','hero_mx48')
$important = @{
 hero_mx01=@('寒阵接力','首次施加控制后为附近友军提供短盾',9,8,35,0)
 hero_mx03=@('追猎寒羽','换目标后保留连续射击计数，并在开战获得短时攻速',19,1,1,1)
 hero_mx05=@('雪灯回护','友军施加控制时为最低生命友军提供短盾',9,7,28,2)
 hero_mx21=@('预铆护阵','开战为自身与邻近友军提供短盾',1,8,36,0)
 hero_mx22=@('余盾回收','施法消费盾后返还短盾',11,0,24,0)
 hero_mx23=@('稳压供弹','护盾被击破后短时维持攻击速度',19,0,0.18,3)
 hero_mx25=@('协同工单','开战工蜂与相邻受益者构成一次保护链',1,8,30,0)
}
foreach($id in $baseIds) {
  $abilityPath = "$root\abilities\${id}_ascend.tres"
  if($important.ContainsKey($id)) {
    $v=$important[$id]; $trigger=if($v[2] -eq 1){1}else{1}; $event=$v[2]; $target=$v[3]; $amount=$v[4]; $attribute=$v[5]
    if($id -eq 'hero_mx22') {
      $ops=@"
[sub_resource type="Resource" id="shield"]
script = ExtResource("operation")
Kind = 2
Target = 0
Amount = 24
DurationTicks = 20
[sub_resource type="Resource" id="reaction"]
script = ExtResource("operation")
Kind = 18
Target = 0
Event = 7
SourceRelation = 1
TargetRelation = 1
CooldownTicks = 1
Effects = Array[ExtResource("operation")]([SubResource("shield")])
"@
    } elseif($id -eq 'hero_mx23') {
      $ops=@"
[sub_resource type="Resource" id="effect"]
script = ExtResource("operation")
Kind = 13
Target = 0
Attribute = 3
Amount = 0.18
DurationTicks = 15
[sub_resource type="Resource" id="reaction"]
script = ExtResource("operation")
Kind = 18
Target = 0
Event = 5
SourceRelation = 4
TargetRelation = 1
OncePerBattle = true
Effects = Array[ExtResource("operation")]([SubResource("effect")])
"@
    } elseif($id -eq 'hero_mx03') {
      $ops=@"
[sub_resource type="Resource" id="reaction"]
script = ExtResource("operation")
Kind = 13
Target = 0
Attribute = 3
Amount = 0.15
DurationTicks = 35
"@
      Write-Utf8 "$root\abilities\hero_mx03_ascended_p.tres" @'
[gd_resource type="Resource" script_class="AbilityDefinition" format=3]

[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityDefinition.cs" id="ability"]
[ext_resource type="Script" path="res://src/Abilities/Authoring/MatrixAbilityOperationSpec.cs" id="operation"]

[sub_resource type="Resource" id="op_2"]
script = ExtResource("operation")
Kind = 7
Target = 0
Key = "chill-chain"
Amount = 1
Maximum = 3
ResetOnTargetChange = false

[sub_resource type="Resource" id="op_3"]
script = ExtResource("operation")
Kind = 4
Target = 13
Amount = 1
RequiredCounter = "chill-chain"
CounterThreshold = 3
ConsumeCounter = true

[sub_resource type="Resource" id="op_1"]
script = ExtResource("operation")
Kind = 18
Target = 0
Event = 1
SourceRelation = 1
TargetRelation = 4
Effects = Array[ExtResource("operation")]([SubResource("op_2"), SubResource("op_3")])

[resource]
script = ExtResource("ability")
StableId = "hero_mx03_growth_ascended_p"
DisplayName = "寒羽游侠·升阶被动"
Description = "连续命中计数在切换目标后保留；累计三次命中后施加1层寒意，遵守羁绊冻结冷却。"
ActivationKind = 1
Trigger = 1
ManaCost = 0
CooldownTicks = 1
MaxUses = 1
Operations = [SubResource("op_1")]
'@
    } else {
      $eventLine=if($event -eq 1){''}else{"Event = $event`nSourceRelation = 1`nTargetRelation = 4"}
      $targetKind=if($id -in @('hero_mx01','hero_mx21','hero_mx25')){8}elseif($id -eq 'hero_mx05'){7}else{0}
      $ops=@"
[sub_resource type="Resource" id="shield"]
script = ExtResource("operation")
Kind = 2
Target = $targetKind
Range = 2.5
MaxTargets = 2
Amount = $amount
DurationTicks = 30
[sub_resource type="Resource" id="reaction"]
script = ExtResource("operation")
Kind = 18
Target = 0
$eventLine
OncePerBattle = true
Effects = Array[ExtResource("operation")]([SubResource("shield")])
"@
    }
    $opRef='SubResource("reaction")'
    Write-Utf8 $abilityPath @"
[gd_resource type="Resource" script_class="AbilityDefinition" format=3]
[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityDefinition.cs" id="ability"]
[ext_resource type="Script" path="res://src/Abilities/Authoring/MatrixAbilityOperationSpec.cs" id="operation"]
$ops
[resource]
script = ExtResource("ability")
StableId = "${id}_growth_ascend"
DisplayName = "$($v[0])"
Description = "$($v[1])"
ActivationKind = 1
Trigger = $trigger
ManaCost = 0
CooldownTicks = 1
MaxUses = 1
Operations = [$opRef]
"@
  } else {
    Write-Utf8 $abilityPath @"
[gd_resource type="Resource" script_class="AbilityDefinition" format=3]
[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityDefinition.cs" id="ability"]
[ext_resource type="Script" path="res://src/Abilities/Authoring/MatrixAbilityOperationSpec.cs" id="operation"]
[sub_resource type="Resource" id="shield"]
script = ExtResource("operation")
Kind = 2
Target = 0
Amount = 20
DurationTicks = 25
[resource]
script = ExtResource("ability")
StableId = "${id}_growth_ascend"
DisplayName = "升阶战备"
Description = "开战获得一层短时防护，确保晚来功能位立即参与战斗。"
ActivationKind = 1
Trigger = 1
MaxUses = 1
Operations = [SubResource("shield")]
"@
  }
  $passivePath = if($id -eq 'hero_mx03'){'res://content/growth/abilities/hero_mx03_ascended_p.tres'}else{"res://content/validation_matrix/abilities/${id}_p.tres"}
  Write-Utf8 "$root\loadouts\${id}_ascended.tres" @"
[gd_resource type="Resource" script_class="AbilityLoadoutDefinition" format=3]
[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityLoadoutDefinition.cs" id="script"]
[ext_resource type="Resource" path="res://content/validation_matrix/abilities/${id}_a.tres" id="a"]
[ext_resource type="Resource" path="$passivePath" id="p"]
[ext_resource type="Resource" path="res://content/growth/abilities/${id}_ascend.tres" id="growth"]
[resource]
script = ExtResource("script")
Abilities = [ExtResource("a"), ExtResource("p"), ExtResource("growth")]
"@
}

$gx=@(
 @('gx01_vine_plague','GX01 蔓疫织者','棘毒高阶术士；主动向高毒层敌群扩散原生毒。',3,2,'f3_dunecaster','mx_mage','mx_poison',5,4,6),
 @('gx02_venom_walker','GX02 负毒行者','棘毒高阶统御者；开战放出有限寿命拦截体，接敌施毒。',5,2,'f4_crawler','mx_summoner','mx_poison',17,0,1),
 @('gx03_array_gunner','GX03 阵列炮师','构装高阶射手；消耗自身部分护盾，对前方敌人发射贯穿射线。',2,6,'f1_ranged','mx_ranger','mx_construct',10,0,0.35),
 @('gx04_phase_anchor','GX04 稳相锚卫','构装高阶护卫；开战消耗自身部分护盾，为附近友军补充短盾。',0,6,'f1_tank','mx_guard','mx_construct',10,0,0.3)
)
foreach($g in $gx){
 $id=$g[0]; $opKind=$g[8]; $target=$g[9]; $amount=$g[10]
 $extraOps=''; $opRefs='SubResource("op")'
 if($id -eq 'gx03_array_gunner') {
  $extraOps=@'
[sub_resource type="Resource" id="line"]
script = ExtResource("operation")
Kind = 25
Target = 1
AttackRatio = 1.6
Range = 8
MaxTargets = 5
Key = "gx03_array_gunner-shield-spent"
CounterRatio = 0.6
ConsumeCounter = true
'@
  $opRefs='SubResource("op"), SubResource("line")'
 } elseif($id -eq 'gx04_phase_anchor') {
  $extraOps=@'
[sub_resource type="Resource" id="near"]
script = ExtResource("operation")
Kind = 2
Target = 8
Range = 2.5
MaxTargets = 3
Amount = 36
DurationTicks = 30
'@
  $opRefs='SubResource("op"), SubResource("near")'
 }
 Write-Utf8 "$root\abilities\${id}_a.tres" @"
[gd_resource type="Resource" script_class="AbilityDefinition" format=3]
[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityDefinition.cs" id="ability"]
[ext_resource type="Script" path="res://src/Abilities/Authoring/MatrixAbilityOperationSpec.cs" id="operation"]
[sub_resource type="Resource" id="op"]
script = ExtResource("operation")
Kind = $opKind
Target = $target
Amount = $amount
Key = "${id}-shield-spent"
AttackRatio = 1.4
Range = 7
Radius = 2
MaxTargets = 5
ContentId = "matrix_spirit_plague"
DurationTicks = 60
$extraOps
[resource]
script = ExtResource("ability")
StableId = "${id}_a"
DisplayName = "$($g[1])·战术"
Description = "$($g[2])"
ActivationKind = 1
Trigger = 5
ManaCost = 70
CooldownTicks = 1
Operations = [$opRefs]
"@
 Write-Utf8 "$root\abilities\${id}_ascend.tres" @"
[gd_resource type="Resource" script_class="AbilityDefinition" format=3]
[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityDefinition.cs" id="ability"]
[ext_resource type="Script" path="res://src/Abilities/Authoring/MatrixAbilityOperationSpec.cs" id="operation"]
[sub_resource type="Resource" id="op"]
script = ExtResource("operation")
Kind = 2
Target = 0
Amount = 32
DurationTicks = 30
[resource]
script = ExtResource("ability")
StableId = "${id}_ascend"
DisplayName = "$($g[1])·强化"
Description = "升阶后获得额外短盾，使高阶功能可立即兑现。"
ActivationKind = 1
Trigger = 1
MaxUses = 1
Operations = [SubResource("op")]
"@
 foreach($suffix in @('base','ascended')) {
  $extra=if($suffix -eq 'ascended'){', ExtResource("asc")'}else{''}
  $asc=if($suffix -eq 'ascended'){"[ext_resource type=`"Resource`" path=`"res://content/growth/abilities/${id}_ascend.tres`" id=`"asc`"]"}else{''}
  Write-Utf8 "$root\loadouts\${id}_${suffix}.tres" @"
[gd_resource type="Resource" script_class="AbilityLoadoutDefinition" format=3]
[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityLoadoutDefinition.cs" id="script"]
[ext_resource type="Resource" path="res://content/growth/abilities/${id}_a.tres" id="a"]
$asc
[resource]
script = ExtResource("script")
Abilities = [ExtResource("a")$extra]
"@
 }
 $portraitNumber=44 + [array]::IndexOf($gx,$g)
 $portraitSource=Join-Path $PSScriptRoot "..\content\validation_matrix\portraits\hero_mx$('{0:d2}' -f $portraitNumber).tres"
 $portraitText=(Get-Content -Raw $portraitSource) -replace "StableId = `"hero_mx$('{0:d2}' -f $portraitNumber)`"", "StableId = `"$id`""
 Write-Utf8 "$root\portraits\${id}.tres" $portraitText
 $stats=switch($id){
  'gx01_vine_plague' {@(185,12,1,1.6,5.2,1)}
  'gx02_venom_walker' {@(210,10,2,1.5,5.2,1)}
  'gx03_array_gunner' {@(175,22,1,1.2,5.2,1)}
  'gx04_phase_anchor' {@(400,10,8,1.5,0.25,0)}
 }
 Write-Utf8 "$root\definitions\${id}.tres" @"
[gd_resource type="Resource" script_class="UnitDefinition" format=3]
[ext_resource type="Script" path="res://src/Content/UnitDefinition.cs" id="script"]
[ext_resource type="Resource" path="res://content/growth/portraits/${id}.tres" id="portrait"]
[ext_resource type="Script" path="res://src/Traits/Authoring/TraitContributionSpec.cs" id="trait"]
[sub_resource type="Resource" id="role"]
script = ExtResource("trait")
TraitId = "$($g[6])"
Value = 1
[sub_resource type="Resource" id="theme"]
script = ExtResource("trait")
TraitId = "$($g[7])"
Value = 1
[resource]
script = ExtResource("script")
Id = "$id"
DisplayName = "$($g[1])"
Description = "$($g[2])"
Portrait = ExtResource("portrait")
IsHero = true
Role = $($g[3])
Faction = $($g[4])
MaxHealth = $($stats[0])
AttackDamage = $($stats[1])
Armor = $($stats[2])
AttackCooldown = $($stats[3])
AttackRange = $($stats[4])
AttackDelivery = $($stats[5])
ProjectileWindupSeconds = 0.35
MaxMana = 70
StartingMana = 20
ManaPerSecond = 5
ManaPerAttack = 10
Tags = Array[StringName]([&"hero", &"growth", &"tier4"])
TraitContributions = [SubResource("role"), SubResource("theme")]
"@
 Write-Utf8 "$root\units\${id}.tscn" @"
[gd_scene format=3]
[ext_resource type="Script" path="res://src/Content/UnitContentRoot.cs" id="root"]
[ext_resource type="Resource" path="res://content/growth/definitions/${id}.tres" id="definition"]
[ext_resource type="SpriteFrames" path="res://assets/donor-units/$($g[5])/frames.tres" id="frames"]
[ext_resource type="PackedScene" path="res://scenes/components/UnitMotionPresentationComponent.tscn" id="motion"]
[ext_resource type="PackedScene" path="res://scenes/components/UnitAnimationComponent.tscn" id="animation"]
[ext_resource type="PackedScene" path="res://scenes/components/HealthViewComponent.tscn" id="health"]
[ext_resource type="PackedScene" path="res://scenes/components/UnitBehaviorComponent.tscn" id="behavior"]
[ext_resource type="PackedScene" path="res://scenes/components/HeroRuleComponent.tscn" id="rule"]
[ext_resource type="PackedScene" path="res://scenes/components/UnitAbilityLoadoutComponent.tscn" id="ability"]
[ext_resource type="Resource" path="res://content/growth/loadouts/${id}_base.tres" id="loadout"]
[node name="$id" type="Node2D"]
script = ExtResource("root")
Definition = ExtResource("definition")
[node name="UnitMotionPresentationComponent" parent="." instance=ExtResource("motion")]
[node name="VisualRoot" type="Node2D" parent="."]
[node name="UnitAnimationComponent" parent="VisualRoot" instance=ExtResource("animation")]
Frames = ExtResource("frames")
[node name="HealthViewComponent" parent="." instance=ExtResource("health")]
[node name="UnitBehaviorComponent" parent="." instance=ExtResource("behavior")]
[node name="HeroRuleComponent" parent="." instance=ExtResource("rule")]
RuleTitle = "成长高阶候选"
RuleDescription = "$($g[2])"
[node name="UnitAbilityLoadoutComponent" parent="." instance=ExtResource("ability")]
Loadout = ExtResource("loadout")
"@
 Write-Utf8 "$root\entries\${id}.tres" @"
[gd_resource type="Resource" script_class="CatalogEntry" format=3]
[ext_resource type="Script" path="res://src/Content/CatalogEntry.cs" id="script"]
[ext_resource type="PackedScene" path="res://content/growth/units/${id}.tscn" id="scene"]
[ext_resource type="Resource" path="res://content/growth/definitions/${id}.tres" id="definition"]
[resource]
script = ExtResource("script")
Scene = ExtResource("scene")
Definition = ExtResource("definition")
"@
}

# Growth battle spells are ordinary once-per-battle loadouts attached to the selected hero.
Write-Utf8 "$root\abilities\spell_guard_formation.tres" @'
[gd_resource type="Resource" script_class="AbilityDefinition" format=3]
[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityDefinition.cs" id="ability"]
[ext_resource type="Script" path="res://src/Abilities/Authoring/MatrixAbilityOperationSpec.cs" id="operation"]
[sub_resource type="Resource" id="self"]
script = ExtResource("operation")
Kind = 2
Target = 0
OwnerHealthRatio = 0.15
DurationTicks = 180
[sub_resource type="Resource" id="near"]
script = ExtResource("operation")
Kind = 2
Target = 8
Range = 2.5
MaxTargets = 2
OwnerHealthRatio = 0.15
DurationTicks = 180
[resource]
script = ExtResource("ability")
StableId = "growth_spell_guard_formation"
DisplayName = "护阵"
Description = "指定英雄及其2.5格内至多2名友军，各获得相当于指定英雄本场生命上限15%的护盾，持续18秒。"
ActivationKind = 1
Trigger = 1
MaxUses = 1
Operations = [SubResource("self"), SubResource("near")]
'@
Write-Utf8 "$root\abilities\spell_gather_momentum.tres" @'
[gd_resource type="Resource" script_class="AbilityDefinition" format=3]
[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityDefinition.cs" id="ability"]
[ext_resource type="Script" path="res://src/Abilities/Authoring/MatrixAbilityOperationSpec.cs" id="operation"]
[sub_resource type="Resource" id="mana"]
script = ExtResource("operation")
Kind = 3
Target = 0
Amount = 25
[resource]
script = ExtResource("ability")
StableId = "growth_spell_gather_momentum"
DisplayName = "蓄势"
Description = "指定英雄开战时获得25点启动法力。"
ActivationKind = 1
Trigger = 1
MaxUses = 1
Operations = [SubResource("mana")]
'@
foreach($spell in @('guard_formation','gather_momentum')) { Write-Utf8 "$root\loadouts\spell_${spell}.tres" @"
[gd_resource type="Resource" script_class="AbilityLoadoutDefinition" format=3]
[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityLoadoutDefinition.cs" id="script"]
[ext_resource type="Resource" path="res://content/growth/abilities/spell_${spell}.tres" id="a"]
[resource]
script = ExtResource("script")
Abilities = [ExtResource("a")]
"@ }

# Reuse the complete authored matrix dependency closure without editing it.
$matrixManifest = Get-Content -Raw (Join-Path $PSScriptRoot '..\content\validation_matrix\manifest.json') | ConvertFrom-Json
$manifest=[ordered]@{}
foreach($p in $matrixManifest.psobject.Properties){$manifest[$p.Name]=@($p.Value)}
$manifest.Loadouts += Get-ChildItem "$root\loadouts\*.tres" | ForEach-Object { 'res://content/growth/loadouts/'+$_.Name }
$manifest.Abilities += Get-ChildItem "$root\abilities\*.tres" | ForEach-Object { 'res://content/growth/abilities/'+$_.Name }
Write-Utf8 "$root\manifest.json" ($manifest | ConvertTo-Json -Depth 6)

$catalog=Get-Content -Raw (Join-Path $PSScriptRoot '..\content\validation_matrix\matrix_catalog.tres')
$refs=''; $heroRefs=''
for($i=0;$i -lt $gx.Count;$i++){
 $id=$gx[$i][0]
 $refs += "`n[ext_resource type=`"Resource`" path=`"res://content/growth/entries/${id}.tres`" id=`"growth_gx$i`"]"
 $heroRefs += ", ExtResource(`"growth_gx$i`")"
}
$catalog=$catalog -replace '\[resource\]', ($refs+"`n`n[resource]")
$catalog=$catalog -replace '(Heroes = Array\[CatalogEntry\]\(\[[^\r\n]+)(\]\))', ('$1'+$heroRefs+'$2')
Write-Utf8 "$root\growth_catalog.tres" $catalog

$poolIds=$baseIds + ($gx | ForEach-Object {$_[0]})
Write-Utf8 "$root\project\growth_hero_pool.tres" @"
[gd_resource type="Resource" script_class="ContentPoolDefinition" format=3]
[ext_resource type="Script" path="res://src/Project/ContentPoolDefinition.cs" id="1"]
[resource]
script = ExtResource("1")
StableId = "pool_growth_heroes"
Kind = 0
ContentIds = PackedStringArray($(($poolIds | ForEach-Object {'"'+$_+'"'}) -join ', '))
"@
Write-Utf8 "$root\project\growth_recruitment_supply.tres" @'
[gd_resource type="Resource" script_class="RecruitmentSupplyDefinition" format=3]
[ext_resource type="Script" path="res://src/Project/RecruitmentSupplyDefinition.cs" id="1"]
[ext_resource type="Script" path="res://src/Project/RecruitmentTierDefinition.cs" id="2"]
[ext_resource type="Script" path="res://src/Project/RecruitmentStageDefinition.cs" id="3"]
[sub_resource type="Resource" id="T1"]
script = ExtResource("2")
HeroIds = PackedStringArray("hero_mx01", "hero_mx03", "hero_mx21", "hero_mx23")
[sub_resource type="Resource" id="T2"]
script = ExtResource("2")
HeroIds = PackedStringArray("hero_mx02", "hero_mx05", "hero_mx10", "hero_mx22", "hero_mx25", "hero_mx30")
[sub_resource type="Resource" id="T3"]
script = ExtResource("2")
HeroIds = PackedStringArray("hero_mx04", "hero_mx24", "hero_mx36", "hero_mx39", "hero_mx40", "hero_mx42", "hero_mx43")
[sub_resource type="Resource" id="T4"]
script = ExtResource("2")
HeroIds = PackedStringArray("hero_mx44", "hero_mx45", "hero_mx46", "hero_mx47", "hero_mx48", "gx01_vine_plague", "gx02_venom_walker", "gx03_array_gunner", "gx04_phase_anchor")
[sub_resource type="Resource" id="S1"]
script = ExtResource("3")
StartFloorIndex = 0
TierWeights = PackedInt32Array(60, 40, 0, 0)
[sub_resource type="Resource" id="S2"]
script = ExtResource("3")
StartFloorIndex = 4
TierWeights = PackedInt32Array(30, 40, 30, 0)
[sub_resource type="Resource" id="S3"]
script = ExtResource("3")
StartFloorIndex = 9
TierWeights = PackedInt32Array(0, 20, 45, 35)
[resource]
script = ExtResource("1")
Tiers = Array[Resource]([SubResource("T1"), SubResource("T2"), SubResource("T3"), SubResource("T4")])
OpeningTierWeights = PackedInt32Array(60, 40, 0, 0)
Stages = Array[Resource]([SubResource("S1"), SubResource("S2"), SubResource("S3")])
'@
# Growth owns encounter resources so its balance can evolve without changing alpha.
foreach($kind in @('combat','elite','boss')) { Copy-GrowthEncounter "encounter_ember_$kind" 1 1 }
foreach($kind in @('combat','elite')) { Copy-GrowthEncounter "encounter_gloam_$kind" 1.25 1.15 }
Copy-GrowthEncounter 'encounter_gloam_boss' 1.5 1.25
foreach($kind in @('combat','elite')) { Copy-GrowthEncounter "encounter_crown_$kind" 1.25 1.25 }
Copy-GrowthEncounter 'encounter_crown_boss' 1.1 1.4
foreach($region in @('region_ember_foundry','region_gloam_crypt','region_crown_engine')) { Copy-GrowthRegion $region }

Write-Utf8 "$root\growth_campaign.tres" @'
[gd_resource type="Resource" script_class="CampaignDefinition" load_steps=9 format=3]
[ext_resource type="Script" path="res://src/Project/CampaignDefinition.cs" id="1"]
[ext_resource type="Resource" path="res://content/growth/regions/region_ember_foundry.tres" id="2"]
[ext_resource type="Resource" path="res://content/growth/regions/region_gloam_crypt.tres" id="3"]
[ext_resource type="Resource" path="res://content/growth/regions/region_crown_engine.tres" id="4"]
[ext_resource type="Resource" path="res://content/project/tower_node_table.tres" id="5"]
[ext_resource type="Resource" path="res://content/growth/project/growth_hero_pool.tres" id="6"]
[ext_resource type="Resource" path="res://content/project/pools/pool_all_items.tres" id="7"]
[ext_resource type="Resource" path="res://content/growth/project/growth_recruitment_supply.tres" id="8"]
[resource]
script = ExtResource("1")
StableId = "campaign_growth_tower"
FloorsPerRegion = 5
Regions = Array[TowerRegionDefinition]([ExtResource("2"), ExtResource("3"), ExtResource("4")])
NodeTable = ExtResource("5")
StarterPool = ExtResource("6")
RecruitmentPool = ExtResource("6")
ItemRewardPool = ExtResource("7")
ShopPool = ExtResource("7")
RecruitmentSupply = ExtResource("8")
'@
Copy-Item (Join-Path $PSScriptRoot '..\content\validation_matrix\matrix_run_rules.tres') "$root\growth_run_rules.tres" -Force
Write-Utf8 "$root\growth_project.tres" @'
[gd_resource type="Resource" script_class="GameProjectDefinition" load_steps=6 format=3]
[ext_resource type="Script" path="res://src/Project/GameProjectDefinition.cs" id="1"]
[ext_resource type="Resource" path="res://content/growth/growth_catalog.tres" id="2"]
[ext_resource type="Resource" path="res://content/growth/growth_campaign.tres" id="3"]
[ext_resource type="Resource" path="res://content/growth/growth_run_rules.tres" id="4"]
[ext_resource type="Resource" path="res://content/project/alpha_presentation.tres" id="5"]
[resource]
script = ExtResource("1")
StableId = "project_growth"
Content = ExtResource("2")
Campaign = ExtResource("3")
RunRules = ExtResource("4")
Presentation = ExtResource("5")
'@

# MX25's growth slice replaces the repeatable mana skill with one battle-start drone.
Write-Utf8 "$root\abilities\hero_mx25_growth_drone.tres" @'
[gd_resource type="Resource" script_class="AbilityDefinition" format=3]
[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityDefinition.cs" id="ability"]
[ext_resource type="Script" path="res://src/Abilities/Authoring/MatrixAbilityOperationSpec.cs" id="operation"]
[sub_resource type="Resource" id="summon"]
script = ExtResource("operation")
Kind = 17
Target = 0
ContentId = "matrix_spirit_drone"
Count = 1
Maximum = 1
DurationTicks = 100
[resource]
script = ExtResource("ability")
StableId = "hero_mx25_growth_drone"
DisplayName = "开战工蜂"
Description = "每场开战仅部署一只短寿命工蜂；不再循环召唤。"
ActivationKind = 1
Trigger = 1
MaxUses = 1
Operations = [SubResource("summon")]
'@
Write-Utf8 "$root\loadouts\hero_mx25_growth_base.tres" @'
[gd_resource type="Resource" script_class="AbilityLoadoutDefinition" format=3]
[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityLoadoutDefinition.cs" id="script"]
[ext_resource type="Resource" path="res://content/growth/abilities/hero_mx25_growth_drone.tres" id="a"]
[ext_resource type="Resource" path="res://content/validation_matrix/abilities/hero_mx25_p.tres" id="p"]
[resource]
script = ExtResource("script")
Abilities = [ExtResource("a"), ExtResource("p")]
'@
Write-Utf8 "$root\loadouts\hero_mx25_growth_ascended.tres" @'
[gd_resource type="Resource" script_class="AbilityLoadoutDefinition" format=3]
[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityLoadoutDefinition.cs" id="script"]
[ext_resource type="Resource" path="res://content/growth/abilities/hero_mx25_growth_drone.tres" id="a"]
[ext_resource type="Resource" path="res://content/validation_matrix/abilities/hero_mx25_p.tres" id="p"]
[ext_resource type="Resource" path="res://content/growth/abilities/hero_mx25_ascend.tres" id="growth"]
[resource]
script = ExtResource("script")
Abilities = [ExtResource("a"), ExtResource("p"), ExtResource("growth")]
'@

# MX01 battle-growth comparison: 4% of authored base 400 MaxHealth = fixed 16.
Write-Utf8 "$root\abilities\hero_mx01_permanent_frost_growth.tres" @'
[gd_resource type="Resource" script_class="AbilityDefinition" format=3]
[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityDefinition.cs" id="ability"]
[ext_resource type="Script" path="res://src/Abilities/Authoring/MatrixAbilityOperationSpec.cs" id="operation"]
[sub_resource type="Resource" id="gain"]
script = ExtResource("operation")
Kind = 29
Target = 0
Amount = 16
Attribute = 0
[sub_resource type="Resource" id="reaction"]
script = ExtResource("operation")
Kind = 18
Target = 0
Event = 9
SourceRelation = 1
TargetRelation = 4
TargetCondition = 8
OncePerBattle = true
Effects = Array[ExtResource("operation")]([SubResource("gain")])
[resource]
script = ExtResource("ability")
StableId = "hero_mx01_permanent_frost_growth"
DisplayName = "冻土积层"
Description = "本战首次成功施加冻结后，自身永久获得16点最大生命；当场及本局以后有效。"
ActivationKind = 1
Trigger = 1
MaxUses = 1
Operations = [SubResource("reaction")]
'@
foreach($suffix in @('base','ascended')) {
 $ascRef=if($suffix -eq 'ascended'){'[ext_resource type="Resource" path="res://content/growth/abilities/hero_mx01_ascend.tres" id="asc"]'}else{''}
 $ascItem=if($suffix -eq 'ascended'){', ExtResource("asc")'}else{''}
 Write-Utf8 "$root\loadouts\hero_mx01_growth_${suffix}.tres" @"
[gd_resource type="Resource" script_class="AbilityLoadoutDefinition" format=3]
[ext_resource type="Script" path="res://src/Abilities/Authoring/AbilityLoadoutDefinition.cs" id="script"]
[ext_resource type="Resource" path="res://content/validation_matrix/abilities/hero_mx01_a.tres" id="a"]
[ext_resource type="Resource" path="res://content/validation_matrix/abilities/hero_mx01_p.tres" id="p"]
[ext_resource type="Resource" path="res://content/growth/abilities/hero_mx01_permanent_frost_growth.tres" id="permanent"]
$ascRef
[resource]
script = ExtResource("script")
Abilities = [ExtResource("a"), ExtResource("p"), ExtResource("permanent")$ascItem]
"@
}

$rulesRefs=@(
 '[ext_resource type="Script" path="res://src/Growth/GrowthRulesDefinition.cs" id="rules"]',
 '[ext_resource type="Script" path="res://src/Growth/GrowthHeroDefinition.cs" id="hero_script"]',
 '[ext_resource type="Script" path="res://src/Growth/GrowthSpellDefinition.cs" id="spell_script"]')
$heroBlocks=@(); $heroResourceRefs=@()
$allGrowthIds=$baseIds + ($gx | ForEach-Object {$_[0]})
for($i=0;$i -lt $allGrowthIds.Count;$i++){
 $id=$allGrowthIds[$i]; $ascPath=if($id -like 'gx*'){"res://content/growth/loadouts/${id}_ascended.tres"}elseif($id -eq 'hero_mx25'){"res://content/growth/loadouts/hero_mx25_growth_ascended.tres"}elseif($id -eq 'hero_mx01'){"res://content/growth/loadouts/hero_mx01_growth_ascended.tres"}else{"res://content/growth/loadouts/${id}_ascended.tres"}
 $basePath=if($id -like 'gx*'){"res://content/growth/loadouts/${id}_base.tres"}elseif($id -eq 'hero_mx25'){"res://content/growth/loadouts/hero_mx25_growth_base.tres"}elseif($id -eq 'hero_mx01'){"res://content/growth/loadouts/hero_mx01_growth_base.tres"}else{"res://content/validation_matrix/loadouts/${id}.tres"}
 $rulesRefs += "[ext_resource type=`"Resource`" path=`"$ascPath`" id=`"asc$i`"]"
 $rulesRefs += "[ext_resource type=`"Resource`" path=`"$basePath`" id=`"base$i`"]"
 $modes=if($id -eq 'hero_mx25'){"ProductionModes = PackedInt32Array(0, 1, 2)`nResearchYield = 1"}elseif($id -eq 'hero_mx10'){'ProductionModes = PackedInt32Array(1)'}elseif($id -eq 'hero_mx30'){'ProductionModes = PackedInt32Array(0, 1)'}else{'ProductionModes = PackedInt32Array()'}
 $desc=switch($id){
  'hero_mx01' {'首次有效控制后给近邻输出者短盾。'}
  'hero_mx03' {'连续命中进度换目标不清零，并获得短时启动攻速。'}
  'hero_mx05' {'控制发生后为低生命友军补短盾。'}
  'hero_mx21' {'开战为自身与近邻友军预铆短盾。'}
  'hero_mx22' {'主动结束后返还一层短盾。'}
  'hero_mx23' {'护盾击破后短时维持攻速。'}
  'hero_mx25' {'协同工单：开战工蜂与近邻形成一次额外保护。'}
  default {'获得开战短盾，使该功能位晚来后立即参与战斗。'}
 }
 $heroBlocks += @"
[sub_resource type="Resource" id="hero$i"]
script = ExtResource("hero_script")
ContentId = "$id"
$modes
GrowthRate = 0.04
AscensionId = "${id}_growth_ascension"
AscensionName = "一次升阶"
AscensionDescription = "$desc"
AscendedLoadout = ExtResource("asc$i")
BaseLoadout = ExtResource("base$i")
MaterialCategory = "general"
"@
 $heroResourceRefs += "SubResource(`"hero$i`")"
}
$rulesRefs += '[ext_resource type="Resource" path="res://content/growth/loadouts/spell_guard_formation.tres" id="spell_guard"]'
$rulesRefs += '[ext_resource type="Resource" path="res://content/growth/loadouts/spell_gather_momentum.tres" id="spell_mana"]'
$first=@('hero_mx04','hero_mx24','hero_mx36','hero_mx39','hero_mx40','hero_mx42','hero_mx43')
$advanced=@('hero_mx44','hero_mx45','hero_mx46','hero_mx47','hero_mx48','gx01_vine_plague','gx02_venom_walker','gx03_array_gunner','gx04_phase_anchor')
Write-Utf8 "$root\growth_rules.tres" @"
[gd_resource type="Resource" script_class="GrowthRulesDefinition" format=3]
$(($rulesRefs -join "`n"))
$(($heroBlocks -join "`n"))
[sub_resource type="Resource" id="guard"]
script = ExtResource("spell_script")
StableId = "guard_formation"
DisplayName = "护阵"
Description = "指定英雄及其2.5格内至多2名友军，各获得相当于指定英雄本场生命上限15%的护盾，持续18秒。"
ResearchCost = 4
BattleLoadout = ExtResource("spell_guard")
[sub_resource type="Resource" id="mana"]
script = ExtResource("spell_script")
StableId = "gather_momentum"
DisplayName = "蓄势"
Description = "指定英雄开战时获得25点启动法力。"
ResearchCost = 4
BattleLoadout = ExtResource("spell_mana")
[resource]
script = ExtResource("rules")
StableId = "growth_rules_v1"
Heroes = Array[Resource]([$(($heroResourceRefs -join ', '))])
FirstDiscoveryPool = PackedStringArray($(($first | ForEach-Object {'"'+$_+'"'}) -join ', '))
AdvancedDiscoveryPool = PackedStringArray($(($advanced | ForEach-Object {'"'+$_+'"'}) -join ', '))
Spells = Array[Resource]([SubResource("guard"), SubResource("mana")])
MaterialsPerNode = 1
AscensionCost = 4
"@

# Refresh manifest after all generated loadouts/abilities exist.
$manifest.Loadouts = @($matrixManifest.Loadouts) + @(Get-ChildItem "$root\loadouts\*.tres" |
    Where-Object { $_.Name -notin @('hero_mx01_ascended.tres','hero_mx25_ascended.tres') } |
    ForEach-Object { 'res://content/growth/loadouts/'+$_.Name })
$manifest.Abilities = @($matrixManifest.Abilities) + @(Get-ChildItem "$root\abilities\*.tres" | ForEach-Object { 'res://content/growth/abilities/'+$_.Name })
Write-Utf8 "$root\manifest.json" ($manifest | ConvertTo-Json -Depth 6)

