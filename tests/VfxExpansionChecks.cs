using System;
using System.Linq;
using Godot;
using TowerAutobattler.Vfx;

// Focused behavior checks for the new support effects, using real authored
// scenes and a skewed stage rather than matching names or source text.
public static class VfxExpansionChecks
{
    private sealed class Stage : IVfxStage
    {
        public float UnitScale => 1.5f;
        public float RadiusPixels(float radius) => radius * 80;
        public Vector2 Project(Vector2 point, bool ground) =>
            new(130 + point.X * 80 + point.Y * 25, 100 + point.X * 12 + point.Y * 44 + (ground ? 20 : 0));
    }
    public static void Run(VfxPlayer player)
    {
        var stage = new Stage();
        CheckHealing(player, stage);
        CheckFrozen(player, stage);
        CheckDrain(player, stage);
    }
    private static VfxInstance Create(VfxPlayer player, string id, VfxContext context)
    {
        var definition = player.Catalog.Find(id);
        var instance = definition.Scene.Instantiate<VfxInstance>();
        instance.Bind(definition, context);
        player.GetParent().AddChild(instance);
        return instance;
    }
    private static Sprite2D[] Pluses(VfxHealingFieldTrack field) =>
        field.GetChildren().OfType<Sprite2D>().Where(sprite => sprite.Material is null).ToArray();

    private static void CheckHealing(VfxPlayer player, IVfxStage stage)
    {
        var context = new VfxContext(new(-2, 0), new(1, .5f), 1.5f);
        var normal = Create(player, "healing_field", context);
        var wide = Create(player, "healing_field", context with { Radius = 3 });
        try
        {
            normal.Advance(.55f, stage, false);
            wide.Advance(.55f, stage, false);
            var field = normal.GetChildren().OfType<VfxHealingFieldTrack>().Single();
            var wideField = wide.GetChildren().OfType<VfxHealingFieldTrack>().Single();
            var sign = Pluses(field)[0];
            var wideSign = Pluses(wideField)[0];
            var pose = normal.Transform * field.Transform * sign.Transform;
            var widePose = wide.Transform * wideField.Transform * wideSign.Transform;
            if (!Mathf.IsEqualApprox(pose.X.Length(), widePose.X.Length()) ||
                !Mathf.IsEqualApprox(pose.X.Length(), pose.Y.Length()) || !Mathf.IsZeroApprox(pose.X.Y))
                throw new Exception("Healing range scaling resized or flattened its upright plus signs.");
            var before = sign.Transform;
            normal.Advance(0, stage, false);
            if (sign.Transform != before) throw new Exception("Paused healing sign moved.");
            normal.Advance(0, stage, true);
            wide.Advance(0, stage, true);
            var center = stage.Project(context.Target, true);
            float a = (normal.Transform * field.Transform * sign.Position).DistanceTo(center);
            float b = (wide.Transform * wideField.Transform * wideSign.Position).DistanceTo(center);
            if (!Mathf.IsEqualApprox(b, a * 2)) throw new Exception("Healing range did not expand its logical spawn distribution.");
            normal.Advance(7.55f, stage, false);
            if (!Pluses(field).Any(sprite => sprite.Visible && sprite.Modulate.A > .1f))
                throw new Exception("Healing field stopped producing visible signs after eight seconds.");
        }
        finally { normal.Free(); wide.Free(); }

        // Observe one real particle's first wrap, then cancel just before the
        // next wrap. The old cycle may fade, but a replacement must not appear.
        var cancelled = Create(player, "healing_field", context);
        try
        {
            cancelled.Advance(0, stage, false);
            var field = cancelled.GetChildren().OfType<VfxHealingFieldTrack>().Single();
            var sign = Pluses(field)[0];
            float wrap = 0;
            for (int i = 1; i <= 210; i++)
            {
                float y = sign.Position.Y;
                cancelled.Advance(.01f, stage, false);
                if (sign.Position.Y > y + 1) { wrap = i * .01f; break; }
            }
            if (wrap == 0) throw new Exception("Healing sign never completed an upward cycle.");
            cancelled.Advance(wrap - .04f, stage, false);
            cancelled.End(VfxEndReason.Completed);
            cancelled.Advance(.08f, stage, false);
            if (sign.Visible) throw new Exception("Healing field birthed a new particle cycle after cancellation.");
        }
        finally { cancelled.Free(); }
    }

    private static void CheckFrozen(VfxPlayer player, IVfxStage stage)
    {
        var context = new VfxContext(Vector2.Zero, new(1, .5f));
        var instance = Create(player, "frozen", context);
        try
        {
            instance.Advance(8.1f, stage, false);
            var crystals = instance.GetChildren().OfType<VfxFrozenCrystalTrack>().Where(sprite => sprite.Visible).ToArray();
            var bodyCenter = instance.Transform.AffineInverse() * stage.Project(context.Target, false);
            bool covered = false;
            foreach (var crystal in crystals)
            {
                var rect = crystal.GetRect();
                var local = crystal.Transform.AffineInverse() * bodyCenter;
                if (!rect.HasPoint(local) || crystal.Scale.Y * rect.Size.Y < 60) continue;
                var uv = (local - rect.Position) / rect.Size;
                using var pixels = crystal.Texture.GetImage();
                if (pixels.IsCompressed()) pixels.Decompress();
                var texel = pixels.GetPixel(Mathf.Clamp((int)(uv.X * pixels.GetWidth()), 0, pixels.GetWidth() - 1),
                    Mathf.Clamp((int)(uv.Y * pixels.GetHeight()), 0, pixels.GetHeight() - 1));
                covered |= texel.A > .4f && ((ShaderMaterial)crystal.Material).GetShaderParameter("growth").AsSingle() > .95f;
                if (covered) break;
            }
            if (crystals.Length < 3 || !covered) throw new Exception("Frozen status lost its held shell or left the body center empty.");
            var chips = instance.GetChildren().OfType<VfxFrozenReleaseTrack>().Single().GetChildren().OfType<Sprite2D>().ToArray();
            if (chips.Any(sprite => sprite.Visible)) throw new Exception("Frozen break chips appeared before status release.");
            instance.End(VfxEndReason.Completed);
            instance.Advance(.06f, stage, false);
            if (!chips.Any(sprite => sprite.Visible && sprite.Modulate.A > .1f))
                throw new Exception("Releasing the frozen shell did not produce its independent chips.");
            instance.Advance(.22f, stage, false);
            if (chips.Any(sprite => sprite.Visible) || crystals.Any(sprite => sprite.Visible))
                throw new Exception("Frozen shell or chips remained visible after the release window.");
        }
        finally { instance.Free(); }
    }

    private static void CheckDrain(VfxPlayer player, IVfxStage stage)
    {
        var context = new VfxContext(new(-2, -.5f), new(1, .7f),
            TargetDisplayPosition: stage.Project(new(1, .7f), false) + new Vector2(13, -7));
        var instance = Create(player, "life_drain", context);
        try
        {
            instance.Advance(.55f, stage, false);
            var flow = instance.GetChildren().OfType<VfxInwardFlowTrack>().Single();
            var strand = flow.GetChildren().OfType<MeshInstance2D>().First();
            float CheckEndpointsAndWidth(VfxContext expected)
            {
                var vertices = ((ArrayMesh)strand.Mesh).SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector2Array();
                var transform = instance.Transform * flow.Transform * strand.Transform;
                var start = transform * ((vertices[0] + vertices[1]) * .5f);
                var end = transform * ((vertices[^2] + vertices[^1]) * .5f);
                if (!start.IsEqualApprox(expected.TargetDisplayPosition!.Value) || !end.IsEqualApprox(stage.Project(expected.Source, false)))
                    throw new Exception("Life drain did not connect displayed target to real source.");
                int middle = (vertices.Length / 4) * 2;
                return (transform * vertices[middle]).DistanceTo(transform * vertices[middle + 1]);
            }
            float width = CheckEndpointsAndWidth(context);
            var shader = (ShaderMaterial)strand.Material;
            float clock = shader.GetShaderParameter("age").AsSingle();
            instance.Advance(0, stage, false);
            if (shader.GetShaderParameter("age").AsSingle() != clock) throw new Exception("Paused life-drain material advanced.");
            instance.Context = context with { Source = new(-5, 2) };
            instance.Advance(0, stage, false);
            if (!Mathf.IsEqualApprox(width, CheckEndpointsAndWidth(instance.Context)))
                throw new Exception("Changing drain endpoint distance changed ribbon width.");
        }
        finally { instance.Free(); }
    }
}
