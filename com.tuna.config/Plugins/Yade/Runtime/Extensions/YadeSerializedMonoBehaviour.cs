namespace Yade.Runtime
{
    using System.Reflection;
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
    using System.IO;
#endif

    public class YadeSerializedMonoBehaviour : MonoBehaviour, ISerializationCallbackReceiver
    {
        private BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public virtual void OnAfterDeserialize() { }

        public virtual void OnBeforeSerialize()
        {
            FindYadesheetFieldsAndRebinding();
        }

        private void FindYadesheetFieldsAndRebinding()
        {
#if UNITY_EDITOR
            var t = GetType();
            var fields = t.GetFields(bindingFlags);
            foreach (var field in fields)
            {
                var value = field.GetValue(this);
                if (value is YadeSheetData)
                {
                    var path = AssetDatabase.GetAssetPath(value as YadeSheetData);
                    if (File.Exists(path))
                    {
                        // Try-catch to prevent Unity Exceptions
                        try
                        {
                            value = AssetDatabase.LoadAssetAtPath<YadeSheetData>(path);
                            if (value != null)
                            {
                                field.SetValue(this, value);
                            }
                        }
                        catch
                        { }
                    }
                }
            }
#endif
        }
    }
}