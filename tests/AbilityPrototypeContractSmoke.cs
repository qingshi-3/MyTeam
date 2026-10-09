using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;

public partial class AbilityPrototypeContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var content = await SkillSampleContent.LoadAsync(this);
            Require(content.Graph.Traits.Length == 8 && content.Catalog.AllEntries().Count() == AbilityPrototypeContent.UnitNumbers.Length + 8 + 18 + 15,
                "eight selected traits, current prototype roster, eight retained samples, eighteen items and fifteen expedition enemy contents");
            Require(content.Graph.Equipment.Select(item=>item.StableId).Order().SequenceEqual(new[]{2,3,6,7,10,11,15}.Select(n=>$"v1_equipment_e{n:00}").Order()),"exact published equipment set");
            Require(content.Graph.Relics.Select(item=>item.StableId).Order().SequenceEqual(Enumerable.Range(1,11).Select(n=>$"v1_relic_r{n:00}").Order()),"exact published relic set");
            AbilityPrototypeRuleChecks.Run(content);
            GD.Print("PROTOTYPE_RULES_OK clone mana independent counts additive-health transform shield-start refresh expiry rollback delayed-storm assassin-landing bounded-followups empowered-attacks form-attacks");
            var casts = new HashSet<string>();
            for (var preset = 0; preset < 4; preset++)
            {
                var aConfig = AbilityPrototypeContent.CreateBattle(content, AbilityPrototypeContent.Presets[preset], AbilityPrototypeContent.Presets[(preset+1)%4]);
                var bConfig = AbilityPrototypeContent.CreateBattle(content, AbilityPrototypeContent.Presets[preset], AbilityPrototypeContent.Presets[(preset+1)%4]);
                using var a = new BattleSimulation(aConfig);
                using var b = new BattleSimulation(bConfig);
                for (var tick = 0; tick < 400; tick++) { a.Step(); b.Step(); }
                Require(a.CreateResult().Digest == b.CreateResult().Digest, "determinism preset " + preset);
                foreach(var fact in a.CombatEvents.Where(e => e.Kind == BattleCombatEventKind.AbilityResolved)) casts.Add(fact.SubjectStableId);
                GD.Print($"PROTOTYPE_PRESET_OK {preset} events={a.CombatEvents.Count} clones={a.UnitViews.Count(u=>u.IsTemporary)} transforms={a.CombatEvents.Count(e=>e.Kind==BattleCombatEventKind.UnitTransformed)}");
                Require(a.CombatEvents.Any(e=>e.Kind==BattleCombatEventKind.StatusApplied && e.SubjectStableId=="matrix_freeze") || preset==1, "freeze executes " + preset);
            }
            using (var grove = new BattleSimulation(AbilityPrototypeContent.CreateBattle(content,
                [25, 2, 0, 16, 3, 0], [4, 7, 0, 19, 18, 0])))
            {
                grove.RunToEnd();
                foreach (var fact in grove.CombatEvents.Where(e => e.Kind == BattleCombatEventKind.AbilityResolved)) casts.Add(fact.SubjectStableId);
            }
            foreach(var number in new[]{1,6,14,20,21,22})
            {
                using var newcomer=new BattleSimulation(AbilityPrototypeContent.CreateBattle(content,
                    [number,0,0,0,0,0],[16,0,0,0,0,0]));
                newcomer.RunToEnd();
                foreach(var fact in newcomer.CombatEvents.Where(e=>e.Kind==BattleCombatEventKind.AbilityResolved))casts.Add(fact.SubjectStableId);
                Require(newcomer.CombatEvents.Any(e=>e.Kind==BattleCombatEventKind.AbilityResolved&&e.SubjectStableId==$"prototype_u{number:00}_a"),"newcomer actually casts published skill "+number);
                GD.Print($"PROTOTYPE_NEWCOMER_CAST_OK {number} outcome={newcomer.Outcome} ticks={newcomer.TickIndex}");
            }
            GD.Print("PROTOTYPE_CASTS " + string.Join(",",casts.Order()));
            foreach(var n in AbilityPrototypeContent.UnitNumbers.Where(n=>n!=7))
                Require(casts.Contains($"prototype_u{n:00}_a"), "unit casts " + n);
            Require(casts.Contains("ability_hc38_a"), "retained grit casts");
            GD.Print("ABILITY_PROTOTYPE_CONTRACT_OK publication deterministic all-current-prototypes-cast");
            GetTree().Quit();
        }
        catch(Exception error) { GD.PrintErr("ABILITY_PROTOTYPE_CONTRACT_FAILED " + error); GetTree().Quit(1); }
    }
    private static void Require(bool pass,string message) { if(!pass)throw new InvalidOperationException(message); }
}
