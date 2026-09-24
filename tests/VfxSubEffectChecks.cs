using System;
using System.Linq;
using Godot;
using TowerAutobattler.Vfx;

// Called by the isolated VFX harness. These checks use real leaf scenes and
// a skewed stage projection; they do not construct a battle or touch a save.
public static class VfxSubEffectChecks
{
    private sealed class SkewedStage : IVfxStage
    {
        public float UnitScale => 1.25f;
        public float RadiusPixels(float radius) => radius * 90;
        public Vector2 Project(Vector2 point, bool ground) =>
            new(51 + 70 * point.X + 19 * point.Y, 27 - 24 * point.X + 45 * point.Y + (ground ? 14 : 0));
    }

    public static void Run(VfxPlayer player)
    {
        var stage = new SkewedStage();
        var host = player.GetParent();
        var context = new VfxContext(new(-2, -.5f), new(1, .7f), 3,
            new(CastSpeed: 1, MotionSpeed: 1.4f, FlightDuration: 4, ParticleScale: 1.2f, Density: .7f, WidthScale: 1.1f),
            TargetDisplayPosition: new(8000, 8000));

        VfxInstance Create(string id, VfxContext requested)
        {
            var definition = player.Catalog.Find(id);
            var instance = definition.Scene.Instantiate<VfxInstance>();
            instance.Bind(definition, requested);
            host.AddChild(instance);
            return instance;
        }

        var pattern = Create("meteor_rain", context);
        try
        {
            pattern.Advance(.1f, stage, false);
            var first = pattern.GetNode<VfxSubEffectTrack>("First");
            var second = pattern.GetNode<VfxSubEffectTrack>("Second");
            var final = pattern.GetNode<VfxSubEffectTrack>("Final");
            if (first.GetChildCount() != 1 || second.GetChildCount() != 0 || final.GetChildCount() != 0)
                throw new Exception("Sub-effects lost their independent start cues.");
            var child = first.GetChild<VfxInstance>(0);
            var target = context.Target + first.TargetOffset * (context.Radius / first.ReferenceRadius);
            var stageTransform = pattern.Transform * first.Transform * child.Transform;
            if (!child.Context.Target.IsEqualApprox(target) || child.Context.TargetDisplayPosition is not null ||
                !stageTransform.Origin.IsEqualApprox(stage.Project(target, true)))
                throw new Exception("Sub-effect offsets bypassed stage projection or inherited a display-space anchor.");
            if (!Mathf.IsEqualApprox(child.Context.Radius, context.Radius * first.RadiusRatio) ||
                child.Playback.Parameters.CastSpeed != 1 || child.Playback.Parameters.MotionSpeed != 1.4f ||
                child.Playback.Parameters.ParticleScale != 1.2f || child.Playback.Parameters.Density != .7f ||
                child.Playback.Parameters.WidthScale != 1.1f ||
                !Mathf.IsEqualApprox(child.Playback.FlightDuration, first.Definition.FlightDuration))
                throw new Exception("Sub-effects lost their parent parameters or fixed authored flight contract.");

            float age = child.Playback.Age;
            pattern.Advance(0, stage, false);
            if (child.Playback.Age != age) throw new Exception("Paused sub-effect clock advanced.");
            pattern.Context = context with { Target = new(5, 6) };
            pattern.Advance(.02f, stage, false);
            if (child.Context.Target != target)
                throw new Exception("An already-started child followed a moving pattern center.");

            pattern.End(VfxEndReason.Completed);
            pattern.Advance(.1f, stage, false);
            if (!child.Playback.Cancelled || second.GetChildCount() != 0 || final.GetChildCount() != 0)
                throw new Exception("Pattern cancellation started a pending child or failed to release an active child.");
            pattern.Advance(.2f, stage, false);
            if (first.GetChildCount() != 0 || GodotObject.IsInstanceValid(child))
                throw new Exception("Released sub-effect was not cleaned up.");
        }
        finally { pattern.Free(); }

        foreach (var id in new[] { "meteor_rain", "thunderstorm" })
        foreach (float speed in new[] { .5f, 2f })
        {
            var direct = Create(id, context with { Playback = new(CastSpeed: speed) });
            var stepped = Create(id, context with { Playback = new(CastSpeed: speed) });
            try
            {
                float sample = .72f / speed;
                direct.Advance(sample, stage, false);
                stepped.Advance(sample * .3f, stage, false);
                stepped.Advance(sample * .7f, stage, false);
                foreach (var track in direct.GetChildren().OfType<VfxSubEffectTrack>())
                {
                    var other = stepped.GetNode<VfxSubEffectTrack>(track.Name.ToString());
                    if (track.GetChildCount() != other.GetChildCount())
                        throw new Exception("Child start depends on parent frame subdivision.");
                    if (track.GetChildCount() == 0) continue;
                    var a = track.GetChild<VfxInstance>(0);
                    var b = other.GetChild<VfxInstance>(0);
                    if (!Mathf.IsEqualApprox(a.Playback.Age, b.Playback.Age) || a.Context.Target != b.Context.Target)
                        throw new Exception("Child timing or position depends on parent frame subdivision.");
                }

                float expected = direct.Playback.ExpectedDuration;
                direct.Advance(expected - sample - .12f, stage, false);
                var final = direct.GetNode<VfxSubEffectTrack>("Final");
                if (final.GetChildCount() != 1)
                    throw new Exception($"{id}: slow/fast playback cut off the final child tail.");
                var last = final.GetChild<VfxInstance>(0);
                float childEnd = final.Delay / speed + last.Playback.ExpectedDuration;
                if (!Mathf.IsEqualApprox(expected, childEnd))
                    throw new Exception($"{id}: parent lifetime disagrees with the final child.");
                direct.Free();
                if (GodotObject.IsInstanceValid(last))
                    throw new Exception("Destroying a pattern retained its child instance.");
            }
            finally
            {
                if (GodotObject.IsInstanceValid(direct)) direct.Free();
                stepped.Free();
            }
        }

        CheckNestedRejection(host, player.Catalog, stage);
    }

    private static void CheckNestedRejection(Node host, VfxCatalog catalog, IVfxStage stage)
    {
        var leafContainer = new VfxInstance();
        var nested = new VfxSubEffectTrack { Definition = catalog.Find("lightning") };
        leafContainer.AddChild(nested);
        nested.Owner = leafContainer;
        var packed = new PackedScene();
        if (packed.Pack(leafContainer) != Error.Ok) throw new Exception("Could not prepare nested-pattern check.");
        leafContainer.Free();
        var pattern = new VfxInstance();
        var outer = new VfxSubEffectTrack
        {
            Definition = new VfxDefinition { StableId = "nested_check", Scene = packed, Duration = 1 },
        };
        pattern.AddChild(outer);
        pattern.Bind(catalog.Find("thunderstorm"), new(Vector2.Zero, Vector2.One));
        host.AddChild(pattern);
        try
        {
            bool rejected = false;
            try { pattern.Advance(.1f, stage, false); }
            catch (InvalidOperationException error) when (error.Message.StartsWith("Nested sub-effect")) { rejected = true; }
            if (!rejected || outer.GetChildCount() != 0)
                throw new Exception("A nested/cyclic pattern was instantiated instead of rejected before binding.");
        }
        finally { pattern.Free(); }
    }
}
