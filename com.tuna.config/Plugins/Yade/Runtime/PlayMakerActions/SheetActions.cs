//  Copyright (c) 2020-present amlovey
//  
#if PLAYMAKER
using System;
using HutongGames.PlayMaker;

namespace Yade.Runtime.PlayMaker
{
    [ActionCategory("Yade Sheet")]
    [Tooltip("Get cell by index")]
    public class GetCellByIndex : FsmStateAction
    {
        public YadeSheetData yadeSheet;
        public FsmInt rowIndex;
        public FsmInt columnIndex;

        [UIHint(UIHint.Variable)]
        public FsmObject cell;

        public override void OnEnter()
        {
            if (cell.Value == null)
            {
                cell.Value = new FsmCell();
            }

            (cell.Value as FsmCell).Value = yadeSheet.GetCell(rowIndex.Value, columnIndex.Value);
            Finish();
        }
    }

    [ActionCategory("Yade Sheet")]
    [Tooltip("Get cell by alpha based index")]
    public class GetCellByAlphaIndex : FsmStateAction
    {
        public YadeSheetData yadeSheet;

        public FsmString alphaBasedCellIndex;

        [UIHint(UIHint.Variable)]
        public FsmObject cell;

        public override void OnEnter()
        {
            if (cell.Value == null)
            {
                cell.Value = new FsmCell();
            }

            (cell.Value as FsmCell).Value = yadeSheet.GetCell(alphaBasedCellIndex.Value);
            Finish();
        }
    }

    
    [ActionCategory("Yade Sheet")]
    [Tooltip("Set raw value of cell by alpha index")]
    public class SetCellRawValueByAlphaIndex : FsmStateAction
    {
        public YadeSheetData yadeSheet;
        public FsmString alphaIndex;
        public FsmString rawValue;

        public override void OnEnter()
        {
            if (yadeSheet == null)
            {
                throw new Exception("sheet data cannot be null");
            }

            if (string.IsNullOrEmpty(alphaIndex.Value))
            {
                throw new Exception("alpha based index cannot be null or empty");
            }

            var index = IndexHelper.AlphaBasedToCellIndex(alphaIndex.Value);
            yadeSheet.SetRawValue(index.row, index.column, rawValue.Value);
        }
    }

    [ActionCategory("Yade Sheet")]
    [Tooltip("Set raw value of cell by index")]
    public class SetCellRawValue : FsmStateAction
    {
        public YadeSheetData yadeSheet;
        public FsmInt row;
        public FsmInt column;
        public FsmString rawValue;

        public override void OnEnter()
        {
            if (yadeSheet == null)
            {
                throw new Exception("sheet data cannot be null");
            }

            if (row.Value < 0 || column.Value < 0)
            {
                throw new Exception("row or column should be larger or equal to 0");
            }

            yadeSheet.SetRawValue(row.Value, column.Value, rawValue.Value);
        }
    }
}

#endif