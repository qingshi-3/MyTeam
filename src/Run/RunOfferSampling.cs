using System;
using System.Buffers.Binary;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TowerAutobattler.Expedition;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

internal static class RunOfferSampling
{
    public static CompiledRunOffer? SiteRecipe(CompiledExpeditionSite definition, ExpeditionSiteDto site, int generationVersion) =>
        definition.OfferRecipes.IsDefaultOrEmpty ? definition.Offer : definition.OfferRecipes
            .OrderBy(recipe => BinaryPrimitives.ReadUInt32LittleEndian(SHA256.HashData(Encoding.UTF8.GetBytes(
                $"exp:{generationVersion}:{site.ContentSeed:x16}:event-recipe:{recipe.StableId}"))))
            .ThenBy(recipe => recipe.StableId, StringComparer.Ordinal).First();
}
