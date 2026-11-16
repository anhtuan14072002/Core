using RogueliteToolkit.Resources;

namespace RogueliteToolkit.SkillTree
{
    public interface ISkillPointWallet
    {
        bool CanSpend(double amount, ResourceDefinition resourceType = null);
        bool TrySpend(double amount, ResourceDefinition resourceType = null);
        // Use the same pricing and resource grouping as TrySpend without deducting balances.
        bool CanSpend(SkillNodeDefinition node, int level);
        bool TrySpend(SkillNodeDefinition node, int level);
    }
}
