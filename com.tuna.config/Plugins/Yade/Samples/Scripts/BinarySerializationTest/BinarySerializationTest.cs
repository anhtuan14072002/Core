using UnityEngine;
using Yade.Runtime;
using UnityEngine.UI;
using System.IO;
using System;

public class BinarySerializationTest : MonoBehaviour
{
    public Text nameText;
    public RectTransform content;
    public GameObject itemPrefab;
    public InputField firstNameText;
    public InputField lastNameText;
    public Button addButton;

    private string dbName = "YadeSampleDB";
    private string sheetName = "Roles";

    public bool SerializationEnable = true;

#pragma warning disable
    private class Role
    {
        [DataField(0)] public string FirstName;
        [DataField(1)] public string LastName;
    }
#pragma warning restore

    void Start()
    {
        YadeDB.SetBinarySerializerEnabled(SerializationEnable, dbName);

#if UNITY_WEBGL
        if (SerializationEnable)
        {
            string data = PlayerPrefs.GetString(dbName);
            if (!string.IsNullOrEmpty(data))
            {
                var bytes = Convert.FromBase64String(data);
                YadeDB.Deserialize(bytes, dbName);
            }
        }
#else
        RestoreDataFromFileIfNeeds();
#endif
        LoadList();

        addButton.onClick.AddListener(AddOne);
    }

    private void AddOne()
    {
        if (string.IsNullOrEmpty(firstNameText.text) || string.IsNullOrEmpty(lastNameText.text))
        {
            Debug.LogError("First Name and Last Name cannot be empty");
            return;
        }

        var rowCount = YadeDB.GetDatabase(dbName).Sheets[sheetName].GetRowCount();
        YadeDB.SetRawValue(sheetName, rowCount, 0, firstNameText.text, dbName);
        YadeDB.SetRawValue(sheetName, rowCount, 1, lastNameText.text, dbName);

        AddRoleToList(firstNameText.text, lastNameText.text);
        firstNameText.text = string.Empty;
        lastNameText.text = string.Empty;

        var scroll = content.GetComponentInParent<ScrollRect>();
        scroll.normalizedPosition = Vector2.up;

#if UNITY_WEBGL
        var bytes = YadeDB.Serialize(dbName);
        PlayerPrefs.SetString(dbName, Convert.ToBase64String(bytes));
        PlayerPrefs.Save();
#endif
    }

    private void OnDestroy()
    {
#if !UNITY_WEBGL
        SaveToFileIfNeeds();
#endif
    }

    private void LoadList()
    {
        var roles = YadeDB.Q<Role>(sheetName, null, dbName);
        foreach (var role in roles)
        {
            AddRoleToList(role.FirstName, role.LastName);
        }
    }

    private void AddRoleToList(string firstName, string lastName)
    {
        var go = GameObject.Instantiate(itemPrefab);
        go.transform.SetParent(content);
        go.transform.SetAsFirstSibling();
        go.transform.localScale = Vector3.one;
        go.SetActive(true);

        var roleName = string.Format("{0}  {1}", firstName, lastName);
        var textComponent = go.transform.Find("Text").GetComponent<Text>();
        if (textComponent)
        {
            textComponent.text = roleName;
        }

        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            OnItemClick(roleName);
        });
    }

    private void OnItemClick(string roleName)
    {
        nameText.text = roleName;
    }

    private void SaveToFileIfNeeds()
    {
        if (!SerializationEnable)
        {
            return;
        }

        var bytes = YadeDB.Serialize(dbName);
        var dataPath = GetDataFilePath();
        File.WriteAllBytes(dataPath, bytes);
    }

    private void RestoreDataFromFileIfNeeds()
    {
        if (!SerializationEnable)
        {
            return;
        }

        var dataPath = GetDataFilePath();
        if (!File.Exists(dataPath))
        {
            return;
        }
        var bytes = File.ReadAllBytes(dataPath);
        YadeDB.Deserialize(bytes, dbName);
    }

    private string GetDataFilePath()
    {
        return Path.Combine(Application.persistentDataPath, "yade.db");
    }
}
