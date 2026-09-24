namespace RogueliteToolkit.SkillTree
{
    public readonly struct SkillChange
    {
        public SkillNodeDefinition Node { get; }
        public int PreviousLevel { get; }
        public int CurrentLevel { get; }

        public SkillChange(
            SkillNodeDefinition node, int previousLevel, int currentLevel)
        {
            Node = node;
            PreviousLevel = previousLevel;
            CurrentLevel = currentLevel;
        }
    }
}
