namespace RogueliteToolkit.SkillTree
{
    public interface ISkillPointWallet
    {
        bool CanSpend(double amount, SkillTreeResourcesType resourceType = SkillTreeResourcesType.None);
        bool TrySpend(double amount, SkillTreeResourcesType resourceType = SkillTreeResourcesType.None);
        bool TrySpend(SkillNodeDefinition node, int level);
    }
}
