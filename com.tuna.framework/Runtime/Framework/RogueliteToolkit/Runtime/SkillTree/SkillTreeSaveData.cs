using System;
using System.Collections.Generic;

namespace RogueliteToolkit.SkillTree
{
    [Serializable]
    public struct SkillNodeLevelData
    {
        public string NodeId;
        public int Level;

        public SkillNodeLevelData(string nodeId, int level)
        {
            NodeId = nodeId;
            Level = level;
        }
    }

    [Serializable]
    public sealed class SkillTreeSaveData
    {
        public string TreeId;
        public List<SkillNodeLevelData> Nodes = new();
    }
}
