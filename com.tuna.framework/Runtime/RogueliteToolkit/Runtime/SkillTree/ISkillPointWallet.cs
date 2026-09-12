namespace RogueliteToolkit.SkillTree
{
    public interface ISkillPointWallet
    {
        bool CanSpend(int amount, SkillTreeResourcesType resourceType = SkillTreeResourcesType.None);
        bool TrySpend(int amount, SkillTreeResourcesType resourceType = SkillTreeResourcesType.None);
    }
}
