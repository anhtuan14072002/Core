using UnityEngine;
using UnityEngine.UI;
using Yade.Runtime;
using System.Linq;

public class DataSheetTest : MonoBehaviour
{
    public YadeSheetData sheetData;
    public RectTransform root;
    public GameObject textPrefab;

    static DataSheetTest()
    {
        // First method that can make types show in dropdown menu of type header in Column Header Settings 
        // Window of YadeEditor.
        DataTypeMapper.RegisterType<DeviceType>(10001);
    }

    private void Start()
    {
        AccessDataSimple();
        AccessDataUsingAlphaBasedCellIndex();
        ConvertDataSheetToList();
        AccesDataUsingYadeDB();
        ConvertDataSheetAsDictionary();
    }

    /// <summary>
    /// Custom data type implement ICellParser which can support by AsList() method of YadeSheetData.
    /// If it also has TypeKey attribute, it will show in dropdown menu of type header in Column Header
    /// Settings Window of YadeEditor
    /// </summary>
    [TypeKey(10002)]
    public class NumberData : ICellParser
    {
        public int index;
        public int year;

        public void ParseFrom(string s)
        {
            var temp = s.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (temp.Length == 2)
            {
                index = int.Parse(temp[0]);
                year = int.Parse(temp[1]);
            }
        }
    }

#pragma warning disable
    /// <summary>
    /// This data class can be generated with following column headers settings by the
    /// builtin code generator of Yade Editor
    /// </summary>
    class Data
    {
        // This one will mapping to Column A in data sheet
        [DataField("A")]
        public string EN;

        // Also works for property
        [DataField("B")]
        public string CN { get; set; }

        // DataField works for integer index of column in data sheet
        [DataField(2)]
        public string JA;

        // Also works for custom type that implement the interface ICellParser
        [DataField(3)]
        public NumberData Numbers;

        // Works for Vectors
        [DataField(4)]
        public Vector2 VEC;

        // Works for enmurations
        [DataField(5)]
        public DeviceType DEVICE;
    }
#pragma warning restore

    private void ConvertDataSheetToList()
    {
        // We use AsList<T>() method here
        var list = sheetData.AsList<Data>();

        list.ForEach(item =>
        {
            CreateText(item.EN);
            CreateText(item.JA);
            CreateText(item.CN);

            if (item.Numbers != null)
            {
                CreateText(item.Numbers.year.ToString());
            }

            CreateText(item.VEC.x.ToString());
            CreateText(item.DEVICE.ToString());
        });
    }

    private void ConvertDataSheetAsDictionary()
    {
        // Create a dictionary with EN column as keys
        var dictionary = sheetData.AsDictionary<string, Data>(item => item.EN);

        Debug.Log(dictionary["Hello"].JA);
        Debug.Log(dictionary["Bye"].CN);
    }

    private void AccessDataSimple()
    {
        // Get row count
        var rows = sheetData.GetRowCount();

        // Get column count
        var columns = sheetData.GetColumnCount();

        // Visit each cell and create text element for cells which have values.
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                var cell = sheetData.GetCell(i, j);
                if (cell != null)
                {
                    CreateText(cell.GetValue());
                }
                else
                {
                    CreateText(string.Empty);
                }
            }
        }
    }

    private void AccessDataUsingAlphaBasedCellIndex()
    {
        var cellsIndex = new string[] { "F1", "E1", "D1", "C1", "B1", "A1", "F2", "E2", "D2", "C2", "B2", "A2" };
        foreach (var index in cellsIndex)
        {
            var cell = sheetData.GetCell(index);
            if (cell != null)
            {
                CreateText(cell.GetValue());
            }
            else
            {
                CreateText(string.Empty);
            }
        }
    }
    
    /// <summary>
    /// We also can use YadeDB to access more efficiently. We need binplace YadeDatabase asset under
    /// Resources folder.
    /// </summary>
    private void AccesDataUsingYadeDB()
    {
        var sheetName = "TestData";
        var databaseName = "YadeSampleDB";

        Debug.Log(YadeDB.Q(sheetName, "A1", databaseName).GetValue());
        Debug.Log(YadeDB.Q<Data>(sheetName, null, databaseName).Count());
    }

    private void CreateText(string text)
    {
        var go = GameObject.Instantiate(textPrefab);
        go.SetActive(true);
        go.transform.SetParent(root);
        go.GetComponent<Text>().text = text;
    }
}
