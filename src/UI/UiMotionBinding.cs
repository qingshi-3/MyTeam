using System;
using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.UI;

// The explicit UI root owns subscriptions and transient transforms. Materials and
// layout resources remain shared and immutable; interrupted input always settles.
public sealed class UiMotionBinding : IDisposable
{
    private readonly Node _root;
    private readonly SceneTree _tree;
    private readonly Func<bool> _reduced;
    private readonly Dictionary<BaseButton, Binding> _bindings = [];

    public UiMotionBinding(Node root, Func<bool> reduced)
    {
        _root = root;
        _tree = root.GetTree();
        _reduced = reduced;
        Visit(root);
        _tree.NodeAdded += Added;
    }

    private void Visit(Node node)
    {
        Added(node);
        foreach (var child in node.GetChildren()) Visit(child);
    }

    private void Added(Node node)
    {
        if (_root.IsAncestorOf(node) && node is IUiMotionHost host) host.ReduceUiMotion = _reduced;
        if (node is not Button button || !_root.IsAncestorOf(button) || _bindings.ContainsKey(button)) return;
        // Board cells, drag owners and card layouts have their own spatial/input
        // contracts. Only authored action roles receive this small press response.
        if (button.ThemeTypeVariation != "PrimaryButton" &&
            button.ThemeTypeVariation != "SecondaryButton" &&
            button.ThemeTypeVariation != "DangerButton" &&
            button.ThemeTypeVariation != "CompactButton" &&
            button is not DetailExplainButton && button is not EquipmentSlotButton) return;
        var binding = new Binding(button, _reduced, () => Remove(button));
        _bindings.Add(button, binding);
    }

    private void Remove(BaseButton button)
    {
        if (_bindings.Remove(button, out var binding)) binding.Dispose();
    }

    public void Dispose()
    {
        _tree.NodeAdded -= Added;
        foreach (var binding in _bindings.Values) binding.Dispose();
        _bindings.Clear();
    }

    private sealed class Binding : IDisposable
    {
        private readonly Button _button;
        private readonly Func<bool> _reduced;
        private readonly Action _exit;
        private Tween? _tween;
        private bool _down;
        private bool _releasing;
        private float _targetScale = 1f;
        private Vector2 _targetPosition;
        private readonly bool _reading;

        public Binding(Button button, Func<bool> reduced, Action exit)
        {
            _button = button;
            _reduced = reduced;
            _exit = exit;
            _reading = button is DetailExplainButton or EquipmentSlotButton;
            button.OffsetTransformEnabled = true;
            button.OffsetTransformVisualOnly = true;
            button.OffsetTransformPivotRatio = new Vector2(.5f, .5f);
            button.MouseEntered += Refresh;
            button.MouseExited += Release;
            button.FocusEntered += Refresh;
            button.FocusExited += Release;
            button.ButtonDown += Press;
            button.ButtonUp += Release;
            button.VisibilityChanged += Visibility;
            button.TreeExiting += exit;
        }

        private void Press() { _down = true; Refresh(); }
        private void Release() { _releasing = _down; _down = false; Refresh(); _releasing = false; }
        private void Visibility()
        {
            if (!_button.IsVisibleInTree()) Reset();
            else Refresh();
        }

        private void Refresh()
        {
            if (_reduced() || _button.Disabled || !_button.IsVisibleInTree())
            {
                Reset();
                return;
            }
            var target = _down ? (_reading ? .988f : .956f) : !_reading && _button.IsHovered() ? 1.018f : 1f;
            var position = new Vector2(0, _down ? 1.5f : !_reading && _button.IsHovered() ? -1f : 0f);
            // Mouse exit, focus exit and button-up can describe the same release.
            // Keep its rebound instead of replacing it with a second plain tween.
            if (_tween is not null && Mathf.IsEqualApprox(_targetScale, target) && _targetPosition == position) return;
            _tween?.Kill();
            _targetScale = target;
            _targetPosition = position;
            _tween = _button.CreateTween().SetIgnoreTimeScale(true).SetPauseMode(Tween.TweenPauseMode.Process)
                .SetParallel().SetTrans(_releasing ? Tween.TransitionType.Back : Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
            _tween.TweenProperty(_button, "offset_transform_scale", Vector2.One * target, _down ? .065 : _releasing ? .24 : .14);
            _tween.TweenProperty(_button, "offset_transform_position",
                position, _down ? .065 : .18);
        }

        private void Reset()
        {
            _down = false;
            _tween?.Kill();
            _tween = null;
            if (GodotObject.IsInstanceValid(_button))
            {
                _button.OffsetTransformScale = Vector2.One;
                _button.OffsetTransformPosition = Vector2.Zero;
            }
        }

        public void Dispose()
        {
            Reset();
            if (!GodotObject.IsInstanceValid(_button)) return;
            _button.MouseEntered -= Refresh;
            _button.MouseExited -= Release;
            _button.FocusEntered -= Refresh;
            _button.FocusExited -= Release;
            _button.ButtonDown -= Press;
            _button.ButtonUp -= Release;
            _button.VisibilityChanged -= Visibility;
            _button.TreeExiting -= _exit;
        }
    }
}
