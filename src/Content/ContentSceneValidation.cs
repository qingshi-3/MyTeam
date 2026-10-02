using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Battle;
using TowerAutobattler.Equipment;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Content;

internal static class ContentSceneValidation
{
    internal static async Task ValidateReadyFrameAsync(
        Node treeOwner, ContentCatalog catalog, ValidationReport report,
        IEnumerable<PackedScene>? additionalValidationScenes = null)
    {
        if (!treeOwner.IsInsideTree())
        {
            report.Error("Ready-frame validation requires a node inside the active scene tree.");
            return;
        }

        var host = new Node2D
        {
            Name = "ContentReadyValidationHost",
            Visible = false
        };
        var attached = false;
        try
        {
            try
            {
                foreach (var entry in ContentReferenceCollector.CatalogEntriesForValidation(catalog))
                    TryAddValidationInstance(host, entry.Scene, entry.Scene.ResourcePath, report);
                foreach (var scene in catalog.FloorRules)
                    TryAddValidationInstance(host, scene, scene?.ResourcePath ?? "<empty floor rule>", report);
                if (additionalValidationScenes is not null)
                    foreach (var scene in additionalValidationScenes)
                        TryAddValidationInstance(host, scene, scene?.ResourcePath ?? "<additional validation scene>", report);
            }
            catch (Exception exception)
            {
                report.Error($"Content ready-frame staging failed: {exception.Message}");
            }

            if (report.HasCoreErrors) return;
            try
            {
                treeOwner.AddChild(host);
                attached = true;
            }
            catch (Exception exception)
            {
                report.Error($"Content ready-frame attach failed: {exception.Message}");
            }

            if (!attached) return;
            try
            {
                await treeOwner.ToSignal(treeOwner.GetTree(), SceneTree.SignalName.ProcessFrame);
                // ProcessFrame is emitted before node _Process callbacks. The second signal proves
                // that every enabled validation node completed at least one actual process cycle.
                await treeOwner.ToSignal(treeOwner.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            catch (Exception exception)
            {
                report.Error($"Content ready-frame process failed: {exception.Message}");
            }
        }
        finally
        {
            try
            {
                if (attached && GodotObject.IsInstanceValid(treeOwner) && GodotObject.IsInstanceValid(host))
                    treeOwner.RemoveChild(host);
            }
            catch (Exception exception) { report.Error($"Content ready-frame detach failed: {exception.Message}"); }
            try
            {
                if (GodotObject.IsInstanceValid(host)) host.Free();
            }
            catch (Exception exception) { report.Error($"Content ready-frame free failed: {exception.Message}"); }
        }
    }

    internal static void ValidateStructuralProbe(PackedScene? scene, ValidationReport report)
    {
        if (scene is null)
        {
            report.Error("Structural validation probe scene is empty.");
            return;
        }

        Node? instance = null;
        try { instance = scene.Instantiate(); }
        catch (Exception exception) { report.Error($"Structural validation probe instantiate failed: {exception.Message}"); }
        finally
        {
            try
            {
                if (GodotObject.IsInstanceValid(instance)) instance?.Free();
            }
            catch (Exception exception) { report.Error($"Structural validation probe free failed: {exception.Message}"); }
        }
    }

    internal static void TryAddValidationInstance(Node host, PackedScene? scene, string label, ValidationReport report)
    {
        if (scene is null)
        {
            report.Error($"Ready-frame validation scene is empty: {label}");
            return;
        }
        Node? instance;
        try { instance = scene.Instantiate(); }
        catch (Exception exception)
        {
            report.Error($"Ready-frame instantiate failed for {label}: {exception.Message}");
            return;
        }

        try { host.AddChild(instance); }
        catch (Exception exception)
        {
            report.Error($"Ready-frame validation-host attach failed for {label}: {exception.Message}");
            try
            {
                if (GodotObject.IsInstanceValid(instance)) instance.Free();
            }
            catch (Exception freeException) { report.Error($"Ready-frame failed-instance free failed for {label}: {freeException.Message}"); }
        }
    }

}
