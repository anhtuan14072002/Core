using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class AutoDotNumberText : MonoBehaviour
{
    [SerializeField] private bool formatOnEnable = true;
    [SerializeField] private bool formatEveryFrame = false;

    private TMP_Text _text;

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly Regex NumberRegex = new Regex(@"^\d+$"); 

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (formatOnEnable)
            FormatIfNumber();
    }

    private void Update()
    {
        if (formatEveryFrame)
            FormatIfNumber();
    }

    public void FormatIfNumber()
    {
        if (_text == null) return;

        string raw = _text.text.Replace(".", "");

        if (NumberRegex.IsMatch(raw))
        {
            if (long.TryParse(raw, out long value))
            {
                _text.text = value
                    .ToString("N0", Invariant)
                    .Replace(",", ".");
            }
        }
    }
}