using System;
using Godot;
using TowerAutobattler.UI;

namespace TowerAutobattler.Presentation;

// Authored scenery only. No terrain, movement, targeting or combat state lives here.
public partial class BattleEnvironment : Control, IUiMotionHost
{
    [Export] public bool AutoAnimate { get; set; } = true;
    [Export] public bool ReducedMotion { get; set; }
    [Export] public int AtmosphereZIndex { get; set; }
    [Export] public WeatherPresentationStyle? WeatherStyle { get; set; }
    public Func<bool> ReduceUiMotion { get; set; } = () => false;
    private TextureRect _ground = null!;
    private ShaderMaterial _air = null!;
    private ShaderMaterial _surface = null!;
    private ShaderMaterial _weather = null!;
    private ColorRect _precipitation = null!;
    public float ElapsedSeconds { get; private set; }
    public Texture2D GroundTexture => _ground.Texture;

    public override void _Ready()
    {
        _ground = GetNode<TextureRect>("Ground");
        _surface = (ShaderMaterial)_ground.Material.Duplicate();
        _ground.Material = _surface;
        var shafts = GetNode<ColorRect>("SunShafts");
        _air = (ShaderMaterial)shafts.Material.Duplicate();
        shafts.Material = _air;
        shafts.ZIndex = AtmosphereZIndex;
        _precipitation = GetNode<ColorRect>("Weather");
        _weather = (ShaderMaterial)_precipitation.Material.Duplicate();
        _precipitation.Material = _weather;
        _precipitation.ZIndex = AtmosphereZIndex;
        SetWeatherStyle(WeatherStyle);
        RefreshLayout();
        ResetClock();
        SetProcess(AutoAnimate);
    }

    // Screens can reserve a header band; the scenery still covers the viewport.
    public void RefreshLayout()
    {
        var parent = GetParent() as Control;
        var position = parent is null ? Vector2.Zero : -parent.GlobalPosition;
        var size = GetViewportRect().Size;
        if (Position != position) Position = position;
        if (Size != size) Size = size;
        _air?.SetShaderParameter("canvas_size", size);
        _weather?.SetShaderParameter("canvas_size", size);
        if (_ground is not null && _air is not null)
        {
            var start = CanvasToTexture(GlobalPosition);
            var end = CanvasToTexture(GlobalPosition + Size);
            _air.SetShaderParameter("texture_rect", new Vector4(start.X, start.Y, end.X - start.X, end.Y - start.Y));
            _weather?.SetShaderParameter("texture_rect", new Vector4(start.X, start.Y, end.X - start.X, end.Y - start.Y));
        }
    }

    // Explicit presentation command; recipes are never modified or saved here.
    public void SetWeatherStyle(WeatherPresentationStyle? style)
    {
        WeatherStyle = style;
        if (_weather is null) return;
        foreach (var material in new[] { _surface, _air, _weather })
        {
            material.SetShaderParameter("weather_overcast", style?.Overcast ?? 0f);
            material.SetShaderParameter("weather_sky", style?.SkyTint ?? Colors.White);
            material.SetShaderParameter("weather_rain", style?.Rain ?? 0f);
            material.SetShaderParameter("weather_snow", style?.Snow ?? 0f);
            material.SetShaderParameter("weather_wetness", style?.Wetness ?? 0f);
            material.SetShaderParameter("weather_snow_cover", style?.SnowCover ?? 0f);
            material.SetShaderParameter("weather_mist", style?.Mist ?? 0f);
            material.SetShaderParameter("weather_wind", style?.Wind ?? .16f);
            material.SetShaderParameter("weather_lightning", style?.Lightning ?? 0f);
        }
        _precipitation.Visible = style is not null &&
            (style.Rain > 0 || style.Snow > 0 || style.Mist > 0 || style.Lightning > 0);
    }

    public Vector2 CanvasToTexture(Vector2 foot)
    {
        var textureSize = GroundTexture.GetSize();
        var scale = Mathf.Max(Size.X / textureSize.X, Size.Y / textureSize.Y);
        var drawnSize = textureSize * scale;
        var localFoot = GetGlobalTransform().AffineInverse() * foot;
        return (localFoot - (Size - drawnSize) * .5f) / drawnSize;
    }

    public void Advance(float seconds)
    {
        RefreshLayout();
        var reduced = ReducedMotion || ReduceUiMotion();
        if (!reduced) ElapsedSeconds += Mathf.Max(0, seconds);
        _surface.SetShaderParameter("environment_elapsed", ElapsedSeconds);
        _air.SetShaderParameter("environment_elapsed", ElapsedSeconds);
        _surface.SetShaderParameter("reduced_motion", reduced);
        _air.SetShaderParameter("reduced_motion", reduced);
        _weather.SetShaderParameter("environment_elapsed", ElapsedSeconds);
        _weather.SetShaderParameter("reduced_motion", reduced);
    }

    public void ResetClock()
    {
        ElapsedSeconds = 0;
        _surface?.SetShaderParameter("environment_elapsed", 0f);
        _air?.SetShaderParameter("environment_elapsed", 0f);
        _surface?.SetShaderParameter("reduced_motion", ReducedMotion || ReduceUiMotion());
        _air?.SetShaderParameter("reduced_motion", ReducedMotion || ReduceUiMotion());
        _weather?.SetShaderParameter("environment_elapsed", 0f);
        _weather?.SetShaderParameter("reduced_motion", ReducedMotion || ReduceUiMotion());
    }

    public override void _Process(double delta)
    {
        if (IsVisibleInTree()) Advance((float)delta);
    }

    public override void _ExitTree()
    {
        // Only this instance owns the duplicate; the authored material stays shared.
        ElapsedSeconds = 0;
        _surface?.Dispose();
        _surface = null!;
        _air?.Dispose();
        _air = null!;
        _weather?.Dispose();
        _weather = null!;
    }
}
