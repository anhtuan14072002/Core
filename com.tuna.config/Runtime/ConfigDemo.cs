using System.Collections;
using System.Collections.Generic;
using Yade.Runtime;

namespace Core
{
    public class NameConfig
    {
        [DataField(0)] public int Id;
    }

    public partial class GameConfig
    {
        public DictList<int, NameConfig> NameDict;
        public IEnumerator LoadChapter(string nameConfig)
        {
            NameDict = LoadSheet(nameConfig).AsDictList<int, NameConfig>(key => key.Id);
            yield return null;
        }
        public List<NameConfig> LoadNameConfig(int id)
        {
            if (NameDict.TryGetValue(id, out var config))
                return config;
            return new List<NameConfig>();
        }
    }
}