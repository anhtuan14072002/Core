using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Yade.Runtime;

public class OnlineSheetTest : MonoBehaviour
{
    public InputField urlInput;
    public Button GetDataButton;
    public Text output;

    void Start()
    {
        urlInput.text = "https://docs.google.com/spreadsheets/d/1TOKg3YghXYLyxtaduC0r6q1bqUYoVLfPW8X7zR-WwkY/edit?usp=sharing";
        GetDataButton.onClick.AddListener(OnGetClick);
    }

    private void OnGetClick()
    {
        var url = this.urlInput.text;
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        OnlineSheet onlineSheet;

        if (url.IndexOf("google.com") != -1)
        {
            onlineSheet = new OnlineGoogleSheet(url);
        }
        else
        {
            onlineSheet = new OnlineCSVSheet(url);
        }

        output.text = string.Empty;
        StartCoroutine(GetData(onlineSheet));
    }

    private IEnumerator GetData(OnlineSheet onlineSheet)
    {
        // First step: download data
        yield return onlineSheet.DownloadSheetData();

        // Second step: parse data
        var sheet = onlineSheet.GetYadeSheetData();
        if (sheet == null)
        {
            yield break;
        }

        // Output data of first 20 rows
        var columnCount = sheet.GetColumnCount();
        var rowCount = Mathf.Min(20, sheet.GetRowCount());

        StringBuilder sb = new StringBuilder();
        for (int row = 0; row < rowCount; row++)
        {
            StringBuilder line = new StringBuilder();
            for (int column = 0; column < columnCount; column++)
            {
                var cell = sheet.GetCell(row, column);
                if (cell != null)
                {
                    if (column == columnCount - 1)
                    {
                        line.Append(cell.GetValue());
                    }
                    else
                    {
                        line.Append(cell.GetValue() + ",");
                    }
                }
            }

            sb.AppendLine(line.ToString());
        }

        output.text = sb.ToString();
    }
}
