using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

// Capture during real input without PNG compression stalling the motion itself.
internal sealed class UiDragMotionRecorder
{
    private readonly List<Image> _frames = [];
    private readonly List<object> _samples = [];
    private ulong _last;
    public void Sample(Viewport viewport, string phase)
    {
        var now = Time.GetTicksMsec();
        if (now - _last < 32 || _frames.Count >= 220) return;
        _last = now;
        var image = viewport.GetTexture().GetImage();
        image.Resize(960, 540, Image.Interpolation.Lanczos);
        _frames.Add(image);
        _samples.Add(new { phase, ticks = now });
    }
    public void Save(string name)
    {
        var path = ProjectSettings.GlobalizePath("res://.godot/ui-review/" + name);
        Directory.CreateDirectory(path);
        for (var i = 0; i < _frames.Count; i++)
        {
            _frames[i].SavePng(Path.Combine(path, $"frame-{i:D3}.png"));
            _frames[i].Dispose();
        }
        File.WriteAllText(Path.Combine(path, "samples.json"), JsonSerializer.Serialize(_samples));
    }
}
