using System;
using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.UI;

// View-only handoff. The router has already committed the new page; this node
// retains the old artwork briefly without retaining its input or run authority.
public partial class UiJourneyTransition : Control, IUiMotionHost
{
    public Func<bool> ReduceUiMotion { get; set; } = () => false;
    public Action<string>? PlaySound { get; set; }
    public bool IsPlaying { get; private set; }
    private readonly Dictionary<Control, State> _states = [];
    private sealed record State(Color Modulate, Vector2 Position, Vector2 Scale,
        bool Enabled, bool VisualOnly, Vector2 Pivot, int Z,
        ProcessModeEnum Process, MouseBehaviorRecursiveEnum Mouse);
    private Tween? _tween;
    private Control? _outgoing;
    private ExpeditionScreenController? _map;
    private Action? _completed;
    private Action? _pendingAnimation;
    private int _layoutFrames;
    private int _generation;
    private ColorRect _placeEdge = null!;
    private ShaderMaterial _edgeMaterial = null!;
    private bool _returningToPlace;

    public override void _Ready()
    {
        _placeEdge = GetNode<ColorRect>("PlaceEdgeLight");
        _edgeMaterial = (ShaderMaterial)_placeEdge.Material;
        Hide();
        SetProcessInput(false);
    }
    public override void _Input(InputEvent input)
    {
        if (IsPlaying && input is InputEventKey or InputEventMouseButton or InputEventJoypadButton)
            GetViewport().SetInputAsHandled();
    }
    public override void _Process(double delta)
    {
        if (!IsPlaying) return;
        if (ReduceUiMotion()) { Finish(); return; }
        if (_pendingAnimation is not null && --_layoutFrames <= 0)
        {
            var animate = _pendingAnimation;
            _pendingAnimation = null;
            animate();
        }
    }

    public void Begin(Control? from, Control to, ExpeditionScreenController map, Control header,
        bool departure, Action completed)
    {
        Cancel();
        _outgoing = from;
        _map = map;
        _completed = completed;
        if (ReduceUiMotion()) { Finish(); return; }
        IsPlaying = true;
        Show();
        SetProcessInput(true);
        map.SetPresentationInputBlocked(true);
        GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
        Remember(to);
        to.Modulate = new Color(1, 1, 1, 0);
        if (departure)
        {
            Remember(header);
            header.Modulate = new Color(1, 1, 1, 0);
        }
        to.MouseBehaviorRecursive = MouseBehaviorRecursiveEnum.Disabled;
        if (from is not null)
        {
            Remember(from);
            from.ProcessMode = ProcessModeEnum.Disabled;
            from.MouseBehaviorRecursive = MouseBehaviorRecursiveEnum.Disabled;
            from.ZIndex = 30;
        }
        // Wait for the real containers and the map's initial centering. No
        // guessed screen coordinates are used for the portrait destination.
        var generation = ++_generation;
        _layoutFrames = 3;
        _pendingAnimation = () =>
        {
            if (!IsInsideTree() || generation != _generation || !IsPlaying) return;
            to.Modulate = _states[to].Modulate;
            Animate(from, to, map, header, departure);
        };
    }

    private void Animate(Control? from, Control to, ExpeditionScreenController map, Control header, bool departure)
    {
        _tween = CreateTween().SetParallel().SetIgnoreTimeScale(true)
            .SetPauseMode(Tween.TweenPauseMode.Process).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        if (departure && from is HeroSelectScreen heroes)
        {
            heroes.PrepareDeparture();
            var finalPartyCenter = map.PartyScreenCenter;
            Prepare(to, new Vector2(0, 12), 1.025f, 0);
            Move(to, Vector2.Zero, 1, .46, .14);
            Fade(to, 1, .34, .16);
            foreach (var surface in heroes.DepartureSurfaces)
            {
                Remember(surface);
                Move(surface, new Vector2(0, -32), .98f, .28);
                Fade(surface, 0, .22);
            }
            var index = 0;
            foreach (var portrait in heroes.DeparturePortraits)
            {
                Remember(portrait);
                portrait.ZIndex = 80;
                var origin = portrait.GetGlobalRect().GetCenter();
                var finish = finalPartyCenter;
                var displacement = portrait.GetGlobalTransform().BasisXformInv(finish - origin);
                var control = displacement * .45f + new Vector2(index++ == 0 ? -70 : 70, -100);
                var moving = portrait;
                _tween.TweenMethod(Callable.From<float>(t =>
                {
                    moving.OffsetTransformPosition = (2 * (1 - t) * t * control) + (t * t * displacement);
                    moving.OffsetTransformScale = Vector2.One * Mathf.Lerp(1, .38f, t);
                }), 0f, 1f, .56).SetDelay(.18).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
                Fade(portrait, 0, .12, .68);
            }
            Remember(header);
            header.Modulate = new Color(1, 1, 1, 0);
            Fade(header, 1, .25, .45);
            PlaySound?.Invoke("journey_depart");
            _tween.TweenCallback(Callable.From(() => PlaySound?.Invoke("journey_arrive"))).SetDelay(.7);
        }
        else if (from is MainMenuScreenController menu)
        {
            var surface = menu.JourneySurface;
            Remember(surface);
            Move(surface, new Vector2(-160, 0), .97f, .27);
            Fade(surface, 0, .22);
            Prepare(to, new Vector2(38, 0), 1, 0);
            Move(to, Vector2.Zero, 1, .36, .06);
            Fade(to, 1, .22, .08);
            PlaySound?.Invoke("journey_open");
        }
        else if (to is MainMenuScreenController returning)
        {
            if (from is not null) { Move(from, new Vector2(55, 0), 1, .2); Fade(from, 0, .2); }
            Prepare(returning.JourneySurface, new Vector2(-100, 0), .97f, 0);
            Move(returning.JourneySurface, Vector2.Zero, 1, .34);
            Fade(returning.JourneySurface, 1, .18);
            PlaySound?.Invoke("journey_close");
        }
        else if (from == map)
        {
            map.PulseSelectedPlace();
            var offset = (map.SelectedPlaceScreenCenter - to.GetGlobalRect().GetCenter()) * .1f;
            Prepare(to, offset, .965f, 0);
            Move(to, Vector2.Zero, 1, .32);
            Fade(to, 1, .2);
            Fade(map, 0, .2);
            LightPlaceEdge(to, map.SelectedPlaceScreenCenter, .32);
            PlaySound?.Invoke("journey_open");
        }
        else if (to == map)
        {
            _returningToPlace = true;
            if (from is not null)
            {
                var offset = (map.SelectedPlaceScreenCenter - from.GetGlobalRect().GetCenter()) * .08f;
                Move(from, offset, .975f, .22);
                Fade(from, 0, .2);
                LightPlaceEdge(from, map.SelectedPlaceScreenCenter, .22);
            }
            Prepare(to, Vector2.Zero, 1, 0);
            Fade(to, 1, .24);
            PlaySound?.Invoke("journey_close");
        }
        else
        {
            // Initial main menu: one soft settle, distinct from travel.
            var surface = to is MainMenuScreenController first ? first.JourneySurface : to;
            Prepare(surface, new Vector2(0, 14), .975f, 0);
            Move(surface, Vector2.Zero, 1, .32);
            Fade(surface, 1, .2);
        }
        _tween.Chain().TweenInterval(.01);
        // The explicit interval also covers the travel sound marker.
        _tween.TweenCallback(Callable.From(Finish));
    }

    private void Remember(Control control)
    {
        if (_states.ContainsKey(control)) return;
        _states.Add(control, new State(control.Modulate, control.OffsetTransformPosition,
            control.OffsetTransformScale, control.OffsetTransformEnabled, control.OffsetTransformVisualOnly,
            control.OffsetTransformPivotRatio, control.ZIndex, control.ProcessMode, control.MouseBehaviorRecursive));
        control.OffsetTransformEnabled = true;
        control.OffsetTransformVisualOnly = false;
        control.OffsetTransformPivotRatio = new Vector2(.5f, .5f);
    }
    private void Prepare(Control c, Vector2 position, float scale, float alpha)
    {
        Remember(c);
        c.OffsetTransformPosition = position;
        c.OffsetTransformScale = Vector2.One * scale;
        c.Modulate = new Color(1, 1, 1, alpha);
    }
    private void Move(Control c, Vector2 position, float scale, double seconds, double delay = 0)
    {
        Remember(c);
        _tween!.TweenProperty(c, "offset_transform_position", position, seconds).SetDelay(delay);
        _tween.TweenProperty(c, "offset_transform_scale", Vector2.One * scale, seconds).SetDelay(delay);
    }
    private void Fade(Control c, float alpha, double seconds, double delay = 0)
        => _tween!.TweenProperty(c, "modulate:a", alpha, seconds).SetDelay(delay);

    private void LightPlaceEdge(Control page, Vector2 source, double duration)
    {
        if (page is not IUiJourneySurface host) return;
        if (host.ResolveJourneySurface(source) is not { } surface) return;
        var direction = source - surface.GetGlobalRect().GetCenter();
        var horizontal = Mathf.Abs(direction.X) / Mathf.Max(1, surface.Size.X) >=
            Mathf.Abs(direction.Y) / Mathf.Max(1, surface.Size.Y);
        _edgeMaterial.SetShaderParameter("edge", horizontal ? (direction.X < 0 ? 0 : 1) : (direction.Y < 0 ? 2 : 3));
        _edgeMaterial.SetShaderParameter("progress", 0f);
        _placeEdge.Show();
        _tween!.TweenMethod(Callable.From<float>(t =>
        {
            var transform = GetGlobalTransformWithCanvas().AffineInverse() * surface.GetGlobalTransformWithCanvas();
            var topLeft = transform * Vector2.Zero;
            var bottomRight = transform * surface.Size;
            _placeEdge.Position = topLeft - Vector2.One * 14;
            _placeEdge.Size = bottomRight - topLeft + Vector2.One * 28;
            _edgeMaterial.SetShaderParameter("extent", _placeEdge.Size);
            _edgeMaterial.SetShaderParameter("progress", t);
        }), 0f, 1f, duration).SetTrans(Tween.TransitionType.Linear);
    }

    private void Finish()
    {
        var completed = _completed;
        _completed = null;
        _pendingAnimation = null;
        var returnedMap = _returningToPlace ? _map : null;
        Cancel();
        if (GodotObject.IsInstanceValid(returnedMap)) returnedMap!.PulseSelectedPlace();
        completed?.Invoke();
    }
    public void Cancel()
    {
        ++_generation;
        _pendingAnimation = null;
        _tween?.Kill(); _tween = null;
        if (GodotObject.IsInstanceValid(_outgoing)) _outgoing!.Hide();
        foreach (var (control, saved) in _states)
        {
            if (!GodotObject.IsInstanceValid(control)) continue;
            control.OffsetTransformPosition = saved.Position;
            control.OffsetTransformScale = saved.Scale;
            control.OffsetTransformPivotRatio = saved.Pivot;
            control.OffsetTransformEnabled = saved.Enabled;
            control.OffsetTransformVisualOnly = saved.VisualOnly;
            control.Modulate = saved.Modulate;
            control.ZIndex = saved.Z;
            control.ProcessMode = saved.Process;
            control.MouseBehaviorRecursive = saved.Mouse;
        }
        _states.Clear();
        _returningToPlace = false;
        _placeEdge?.Hide();
        _outgoing = null;
        if (GodotObject.IsInstanceValid(_map)) _map!.SetPresentationInputBlocked(false);
        _map = null;
        _completed = null;
        IsPlaying = false;
        SetProcessInput(false);
        Hide();
    }
    public override void _ExitTree() => Cancel();
}
