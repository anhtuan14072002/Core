/*using System.Reflection;
using UnityEngine;
using Yade.Runtime;

namespace Core
{
    public partial class GameConfig
    {
        private const string ConfigPath = "Configs/";

        public GameConfig()
        {
            CallLoadMethods();
        }

        private void CallLoadMethods()
        {
            MethodInfo[] methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var method in methods)
                if (method.GetCustomAttributes(typeof(LoadConfigAttribute), false).Length > 0)
                    method.Invoke(this, null);
        }

        private static YadeSheetData LoadSheet(string sheetName)
        {
#if UNITY_EDITOR
            if (Resources.Load<YadeSheetData>(ConfigPath + sheetName) == null)
            {
                new System.IO.DirectoryInfo("Assets/Resources/" + ConfigPath).Create();
                UnityEditor.AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<YadeSheetData>(),
                    $"Assets/Resources/{ConfigPath + sheetName}.asset");
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
#endif
            return Resources.Load<YadeSheetData>(ConfigPath + sheetName);
        }
    }
}*/