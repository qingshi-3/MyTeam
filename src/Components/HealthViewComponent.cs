using Godot;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Statuses;
using TowerAutobattler.Presentation;
using TowerAutobattler.UI;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Components;

[GlobalClass]
public partial class HealthViewComponent : Node2D
{
    [Export] public ProgressBar Bar { get; set; } = null!;
    [Export] public ProgressBar ManaBar { get; set; } = null!;
    [Export] public ProgressBar ShieldBar { get; set; } = null!;
    [Export] public TextureRect StatusIcon { get; set; } = null!;
    [Export] public StyleBox AllyHealthFill { get; set; } = null!;
    [Export] public StyleBox EnemyHealthFill { get; set; } = null!;

    public void SetTeam(int team)
    {
        Bar.AddThemeStyleboxOverride("fill", team == 0 ? AllyHealthFill : EnemyHealthFill);
    }

    public void SetHealth(float current, float maximum)
    {
        if (Bar is null) return;
        Bar.MaxValue = maximum;
        Bar.Value = Mathf.Clamp(current, 0, maximum);
    }

    public void SetCombatResources(float mana, float maximumMana, float shield,
        ImmutableArray<StatusRuntimeSnapshot> statuses, bool alive, BattleSkillProgress? skill = null)
    {
        skill ??= maximumMana > 0 ? new("", "技能", SkillResourceKind.Mana, mana, maximumMana, "法力",
            mana >= maximumMana ? SkillProgressState.Ready : SkillProgressState.Building) : null;
        ManaBar.Visible = alive && skill is not null;
        if (skill is not null)
            SkillProgressPresentation.Bind(ManaBar, skill);
        ShieldBar.Visible = alive && shield > 0;
        ShieldBar.MaxValue = Mathf.Max((float)Bar.MaxValue, shield);
        ShieldBar.Value = shield;
        var visibleStatuses = statuses.IsDefaultOrEmpty ? [] : statuses
            .Where(item => StatusDisplayFacts.DisablesActions(item) || item.Disposition == StatusDisposition.Harmful)
            .OrderByDescending(StatusDisplayFacts.DisablesActions)
            .ThenBy(item => item.ApplicationSequence).ToArray();
        var primary = visibleStatuses.FirstOrDefault();
        StatusIcon.Visible = alive && primary is not null;
        if (primary is not null)
        {
            StatusIcon.Texture = SemanticIcons.Catalog.ResolveIcon(StatusDisplayFacts.IconKey(primary))
                ?? SemanticIcons.Catalog.ResolveIcon("risk");
        }
    }

    // Kept as a presentation compatibility hook. Cast details belong in inspection;
    // the combat board intentionally avoids transient ability-name copy overhead.
    public void PresentAbility(string name)
    {
    }

    public void SetPaused(bool paused)
    {
    }
}
