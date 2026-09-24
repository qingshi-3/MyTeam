using System;
using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.Audio;

// The composition root explicitly supplies the home-menu hover scope. Other
// buttons, including later-created offer cards, sound only on accepted activation.
public sealed class UiFeedbackBinding : IDisposable
{
    private readonly Node _root;
    private readonly Node _hoverRoot;
    private readonly FeedbackAudio _audio;
    private readonly SceneTree _tree;
    private readonly Dictionary<BaseButton, (Action Enter, Action Focus, Action Press, Action Exit)> _bindings = [];

    public UiFeedbackBinding(Node root, FeedbackAudio audio, Node hoverRoot)
    {
        _root = root;
        _hoverRoot = hoverRoot;
        _audio = audio;
        _tree = root.GetTree();
        BindSubtree(root);
        _tree.NodeAdded += OnNodeAdded;
    }

    private void BindSubtree(Node node)
    {
        OnNodeAdded(node);
        foreach (var child in node.GetChildren()) BindSubtree(child);
    }

    private void OnNodeAdded(Node node)
    {
        if (node is not BaseButton button || !_root.IsAncestorOf(button) || _bindings.ContainsKey(button)) return;
        var hoverFeedback = _hoverRoot.IsAncestorOf(button);
        Action enter = () =>
        {
            if (hoverFeedback && !button.Disabled && button.IsVisibleInTree() && button.CanProcess())
                _audio.RequestUi("ui_select");
        };
        // Clicking a hovered button also gives it focus; do not sound twice.
        Action focus = () => { if (!button.IsHovered()) enter(); };
        // Pressed is already accepted input. An earlier command listener may
        // hide/disable the button while handling it, so do not recheck visibility.
        Action press = () => { if (!hoverFeedback) _audio.RequestUi("ui_select"); };
        Action exit = () => Unbind(button);
        _bindings.Add(button, (enter, focus, press, exit));
        button.MouseEntered += enter;
        button.FocusEntered += focus;
        button.Pressed += press;
        button.TreeExiting += exit;
    }

    private void Unbind(BaseButton button)
    {
        if (!_bindings.Remove(button, out var handlers)) return;
        if (!GodotObject.IsInstanceValid(button)) return;
        button.MouseEntered -= handlers.Enter;
        button.FocusEntered -= handlers.Focus;
        button.Pressed -= handlers.Press;
        button.TreeExiting -= handlers.Exit;
    }

    public void Dispose()
    {
        _tree.NodeAdded -= OnNodeAdded;
        foreach (var button in new List<BaseButton>(_bindings.Keys)) Unbind(button);
    }
}
