using System;
using System.Collections.Immutable;
using TowerAutobattler.Content;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;

namespace TowerAutobattler.Composition;

public static class RunProfileIds
{
    public const string EightTraitExpedition = "eight_trait_expedition_v1";
    public const string GrowthExpedition = "growth_expedition_v2";
    public const string GrowthExpeditionLegacy = "growth_expedition_v1";
    public const string GrowthTowerLegacy = "growth_tower_legacy_v10";
    public const string AlphaTowerLegacy = "alpha_tower_legacy_v10";

    internal static string SingleProject(string projectId) => projectId == "project_my_team_alpha"
        ? AlphaTowerLegacy : projectId;
}

public sealed record CompiledRunProfile(
    string ProfileId,
    int CompatibilityVersion,
    string ContentFingerprint,
    CompiledGameProject Project,
    CompiledGrowthRules? GrowthRules,
    int ProgressionProtocolVersion,
    int RewardProtocolVersion,
    int IdentityProtocolVersion);

public sealed record CompiledGamePackage
{
    public CompiledGamePackage(ContentRegistry content,
        ImmutableDictionary<string, CompiledRunProfile> profilesById,
        string defaultNewRunProfileId, int publicationVersion)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(profilesById);
        if (!profilesById.ContainsKey(defaultNewRunProfileId))
            throw new ArgumentException("Default run profile is not registered.", nameof(defaultNewRunProfileId));
        foreach (var (id, profile) in profilesById)
            if (!StringComparer.Ordinal.Equals(id, profile.ProfileId) ||
                !ReferenceEquals(profile.Project.Content, content.Catalog))
                throw new ArgumentException("Run profiles must share the published catalog and match their keys.", nameof(profilesById));
        Content = content;
        ProfilesById = profilesById.WithComparers(StringComparer.Ordinal);
        DefaultNewRunProfileId = defaultNewRunProfileId;
        PublicationVersion = publicationVersion;
    }

    // Compatibility for already-published single-project consumers and fixtures.
    // This does not create a registry or bypass the caller's content publication.
    public CompiledGamePackage(ContentRegistry content, CompiledGameProject project, int publicationVersion)
        : this(content, SingleProfile(content, project), RunProfileIds.SingleProject(project.StableId), publicationVersion) { }

    public ContentRegistry Content { get; }
    public ImmutableDictionary<string, CompiledRunProfile> ProfilesById { get; }
    public string DefaultNewRunProfileId { get; }
    public int PublicationVersion { get; }
    public CompiledRunProfile DefaultProfile => ProfilesById[DefaultNewRunProfileId];
    public CompiledGameProject Project => DefaultProfile.Project;
    public CompiledGrowthRules? GrowthRules => DefaultProfile.GrowthRules;
    internal object? AuthoringOwner { get; init; }

    // Explicit fixture default override; all profiles and the registry stay intact.
    // Continue selects ProfilesById without changing the production new-run default.
    public CompiledGamePackage ForProfile(string profileId) =>
        new(Content, ProfilesById, profileId, PublicationVersion) { AuthoringOwner = AuthoringOwner };

    private static ImmutableDictionary<string, CompiledRunProfile> SingleProfile(
        ContentRegistry content, CompiledGameProject project)
    {
        var id = RunProfileIds.SingleProject(project.StableId);
        var profile = new CompiledRunProfile(id, 1,
            RunProfileContentFingerprint.Compute(id, 1, 1, 1, 1, project, null, content.Graph), project, null, 1, 1, 1);
        return ImmutableDictionary.Create<string, CompiledRunProfile>(StringComparer.Ordinal).Add(id, profile);
    }
}
