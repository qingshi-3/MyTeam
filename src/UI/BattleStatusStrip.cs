using Godot;

namespace TowerAutobattler.UI;

public partial class BattleStatusStrip : HBoxContainer
{
    private SemanticChip _time = null!;
    private SemanticChip _allies = null!;
    private SemanticChip _enemies = null!;
    private SemanticChip _gold = null!;
    private Label _feedback = null!;

    public int DisplayedGold { get; private set; }
    public string FeedbackText => _feedback?.Text ?? string.Empty;

    public override void _Ready()
    {
        _time = GetNode<SemanticChip>("%Time");
        _allies = GetNode<SemanticChip>("%Allies");
        _enemies = GetNode<SemanticChip>("%Enemies");
        _gold = GetNode<SemanticChip>("%Gold");
        _feedback = GetNode<Label>("%Feedback");
    }

    public void Bind(float seconds, int allies, int enemies, bool showGold, int gold, string feedback, bool feedbackError)
    {
        _time.Bind(SemanticIconKeys.Time, $"{seconds:0.0}s", "SecondaryLabel");
        _allies.Bind(SemanticIconKeys.Melee, allies.ToString(), "PlayerLabel");
        _enemies.Bind(SemanticIconKeys.Deaths, enemies.ToString(), "EnemyLabel");
        DisplayedGold = gold;
        _gold.Visible = showGold;
        if (showGold) _gold.Bind(SemanticIconKeys.Gold, gold.ToString(), "GoldValue");
        _feedback.Text = feedback;
        _feedback.Visible = !string.IsNullOrWhiteSpace(feedback);
        _feedback.ThemeTypeVariation = feedbackError ? "FeedbackFailure" : "FeedbackSuccess";
    }
}
