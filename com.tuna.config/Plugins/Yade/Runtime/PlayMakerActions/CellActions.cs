//  Copyright (c) 2020-present amlovey
//  
#if PLAYMAKER
using HutongGames.PlayMaker;

namespace Yade.Runtime.PlayMaker
{
    [ActionCategory("Yade Sheet")]
    [Tooltip("Get value of a cell")]
    public class GetCellValue : FsmStateAction
    {
        [UIHint(UIHint.Variable)]
        public FsmObject cell;
        public FsmString value;

        public override void OnEnter()
        {
            value.Value = (cell.Value as FsmCell).Value.GetValue();
            Finish();
        }
    }

    [ActionCategory("Yade Sheet")]
    [Tooltip("Get raw value of a cell")]
    public class GetCellRawValue : FsmStateAction
    {
        [UIHint(UIHint.Variable)]
        public FsmObject cell;
        public FsmString value;

        public override void OnEnter()
        {
            value.Value = (cell.Value as FsmCell).Value.GetRawValue();
            Finish();
        }
    }

    [ActionCategory("Yade Sheet")]
    [Tooltip("Get unity object value of a cell")]
    public class GetCellUnityObject : FsmStateAction
    {
        [UIHint(UIHint.Variable)]
        public FsmObject cell;

        [UIHint(UIHint.Variable)]
        public FsmObject value;

        public override void OnEnter()
        {
            value.Value = (cell.Value as FsmCell).Value.GetUnityObject();
            Finish();
        }
    }

    [ActionCategory("Yade Sheet")]
    [Tooltip("Get Texture2d type cell value and convert it to sprite")]
    public class GetCellValueAsSprite : FsmStateAction
    {
        [UIHint(UIHint.Variable)]
        public FsmObject cell;

        [UIHint(UIHint.Variable)]
        public FsmObject value;

        public override void OnEnter()
        {
            var obj = (cell.Value as FsmCell).Value.GetUnityObject();
            if (obj == null)
            {
                return;
            }

            if (obj is UnityEngine.Texture2D)
            {
                var tex2d = obj as UnityEngine.Texture2D;
                value.Value = UnityEngine.Sprite.Create(tex2d, new UnityEngine.Rect(0, 0, tex2d.width, tex2d.height), new UnityEngine.Vector2(0.5f, 0.5f));
            }

            Finish();
        }
    }
}
#endif