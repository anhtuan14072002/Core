using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sheet
{
    public enum SheetImportType
    {
        Auto,
        Google,
        Csv,
    }

    [Serializable]
    public sealed class GoogleSheetBulkImportItem
    {
        public string SourceUrl;
        public string SaveTo = "Assets/_Project/Resources/Config/";
        public string SheetName;
        public SheetImportType Type;
    }

    [CreateAssetMenu(
        fileName = "BulkImportConfig",
        menuName = "Wizard/Google Sheet Bulk Import Config")]
    public sealed class GoogleSheetBulkImportConfig : ScriptableObject
    {
        public List<GoogleSheetBulkImportItem> Items = new();
    }
}
