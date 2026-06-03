//  Copyright (c) 2022-present amlovey
//  
#if PLAYMAKER
using HutongGames.PlayMaker;

namespace Yade.Runtime.PlayMaker
{
    [ActionCategory("Yade Sheet")]
    [Tooltip("Query cell by index using YadeDB")]
    public class YadeDBQueryCellByIndex : FsmStateAction
    {
        public FsmInt rowIndex;
        public FsmInt columnIndex;
        public FsmString sheetName;
        public FsmString dbName = YadeDB.DefaultDB;

        [UIHint(UIHint.Variable)]
        public FsmObject cell;

        public override void OnEnter()
        {
            if (cell.Value == null)
            {
                cell.Value = new FsmCell();
            }

            (cell.Value as FsmCell).Value = YadeDB.Q(sheetName.Value, rowIndex.Value, columnIndex.Value, dbName.Value);
            Finish();
        }
    }

    [ActionCategory("Yade Sheet")]
    [Tooltip("Query cell by alpha index using YadeDB")]
    public class YadeDBQueryCellByAlphaIndex : FsmStateAction
    {
        public FsmString alphaIndex;
        public FsmString sheetName;
        public FsmString dbName = YadeDB.DefaultDB;

        [UIHint(UIHint.Variable)]
        public FsmObject cell;

        public override void OnEnter()
        {
            if (cell.Value == null)
            {
                cell.Value = new FsmCell();
            }

            (cell.Value as FsmCell).Value = YadeDB.Q(sheetName.Value, alphaIndex.Value, dbName.Value);
            Finish();
        }
    }

    [ActionCategory("Yade Sheet")]
    [Tooltip("Set cell raw value by index using YadeDB")]
    public class YadeDBSetCellRawValueByIndex : FsmStateAction
    {
        public FsmInt rowIndex;
        public FsmInt columnIndex;
        public FsmString sheetName;
        public FsmString dbName = YadeDB.DefaultDB;
        public FsmString rawText;

        public override void OnEnter()
        {
            YadeDB.SetRawValue(sheetName.Value, rowIndex.Value, columnIndex.Value, rawText.Value, dbName.Value);
        }
    }

    [ActionCategory("Yade Sheet")]
    [Tooltip("Set cell raw value by alpha index using YadeDB")]
    public class YadeDBSetCellRawValueByAlphaIndex : FsmStateAction
    {
        public FsmString alphaIndex;
        public FsmString sheetName;
        public FsmString dbName = YadeDB.DefaultDB;
        public FsmString rawText;

        public override void OnEnter()
        {
            YadeDB.SetRawValue(sheetName.Value, alphaIndex.Value,rawText.Value, dbName.Value);
        }
    }
}
#endif