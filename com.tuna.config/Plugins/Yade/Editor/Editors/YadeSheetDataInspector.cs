//  Copyright (c) 2020-present amlovey
//  
using UnityEditor;
using Yade.Runtime;

namespace Yade.Editor
{
    [CustomEditor(typeof(YadeSheetData))]
    [CanEditMultipleObjects]
    public class YadeSheetDataInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Don't render inspector ui for now
        }
    }
}
