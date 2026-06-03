using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Tuna.PackageInstaller.Editor
{
    public sealed class TunaPackageInstallerWindow : EditorWindow
    {
        private const string TunaGitUrl = "https://github.com/anhtuan14072002/Core.git";

        private readonly List<PackageInfo> _tunaOnlyPackages = new()
        {
            new("Tuna / Common",       "com.tuna.common"),
            new("Tuna / Config",       "com.tuna.config"),
            new("Tuna / Core",         "com.tuna.core"),
            new("Tuna / Data",         "com.tuna.data"),
            new("Tuna / UI",           "com.tuna.ui"),
            new("Tuna / V",           "com.tuna.v"),
            new("Tuna / Zenject",           "com.tuna.zenject"),
            new("Tuna / Custom",           "com.tuna.custom"),
        };

        private readonly List<PackageInfo> _thirdPartyOnlyPackages = new()
        {
            new(
                "UniTask",
                "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
                PackageSource.GitUrl),

            new(
                "UniState",
                "https://github.com/bazyleu/UniState.git?path=Assets/UniState",
                PackageSource.GitUrl),

            new(
                "NuGetForUnity",
                "https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity",
                PackageSource.GitUrl),

            new(
                "PrimeTween",
                "https://github.com/KyryloKuzyk/PrimeTween.git",
                PackageSource.GitUrl),

            new(
                "Alchemy",
                "https://github.com/yn01-dev/Alchemy.git?path=/Alchemy/Assets/Alchemy",
                PackageSource.GitUrl),
        };

        private Vector2 _scroll;

        [MenuItem("TunaCore/Package Installer")]
        private static void Open()
        {
            GetWindow<TunaPackageInstallerWindow>("Tuna Installer");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            EditorGUILayout.LabelField(
                "Tuna Package Installer",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Tick package cần cài rồi bấm Install Selected.",
                MessageType.Info);

            GUILayout.Space(6);

            DrawToolbar();

            GUILayout.Space(8);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            float columnWidth = (position.width - 26f) * 0.5f;

            EditorGUILayout.BeginHorizontal();

            DrawPackageTable("Tuna Only", _tunaOnlyPackages, columnWidth);

            GUILayout.Space(8);

            DrawPackageTable("Third Party Only", _thirdPartyOnlyPackages, columnWidth);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);

            if (GUILayout.Button("Install Selected", GUILayout.Height(36)))
            {
                InstallSelected();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select All", GUILayout.Height(28)))
            {
                SetAllSelected(true);
            }

            if (GUILayout.Button("Clear", GUILayout.Height(28)))
            {
                SetAllSelected(false);
            }

            if (GUILayout.Button("Tuna Only", GUILayout.Height(28)))
            {
                SelectBySource(PackageSource.TunaRepo);
            }

            if (GUILayout.Button("Third Party Only", GUILayout.Height(28)))
            {
                SelectBySource(PackageSource.GitUrl);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPackageTable(string title, List<PackageInfo> packages, float width)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(width));

            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            for (int i = 0; i < packages.Count; i++)
            {
                DrawPackage(packages[i]);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPackage(PackageInfo package)
        {
            EditorGUILayout.BeginVertical("box");

            package.Selected = EditorGUILayout.ToggleLeft(
                package.DisplayName,
                package.Selected,
                EditorStyles.boldLabel);

            // EditorGUILayout.LabelField("Source", package.Source.ToString());
            //
            // EditorGUILayout.SelectableLabel(
            //     GetPackageUrl(package),
            //     EditorStyles.textField,
            //     GUILayout.Height(18));

            EditorGUILayout.EndVertical();
        }

        private void InstallSelected()
        {
            InstallSelected(_tunaOnlyPackages);
            InstallSelected(_thirdPartyOnlyPackages);
        }

        private void InstallSelected(List<PackageInfo> packages)
        {
            for (int i = 0; i < packages.Count; i++)
            {
                PackageInfo package = packages[i];

                if (!package.Selected)
                    continue;

                string url = GetPackageUrl(package);

                Debug.Log($"[Tuna Installer] Installing: {package.DisplayName}");
                Debug.Log($"[Tuna Installer] URL: {url}");

                Client.Add(url);
            }
        }

        private string GetPackageUrl(PackageInfo package)
        {
            switch (package.Source)
            {
                case PackageSource.TunaRepo:
                    return $"{TunaGitUrl}?path=/{package.Value}";

                case PackageSource.GitUrl:
                    return package.Value;

                default:
                    return package.Value;
            }
        }

        private void SetAllSelected(bool selected)
        {
            SetSelected(_tunaOnlyPackages, selected);
            SetSelected(_thirdPartyOnlyPackages, selected);
        }

        private void SetSelected(List<PackageInfo> packages, bool selected)
        {
            for (int i = 0; i < packages.Count; i++)
            {
                packages[i].Selected = selected;
            }
        }

        private void SelectBySource(PackageSource source)
        {
            SetSelected(_tunaOnlyPackages, source == PackageSource.TunaRepo);
            SetSelected(_thirdPartyOnlyPackages, source == PackageSource.GitUrl);
        }

        private enum PackageSource
        {
            TunaRepo,
            GitUrl
        }

        private sealed class PackageInfo
        {
            public string DisplayName;
            public string Value;
            public PackageSource Source;
            public bool Selected;

            public PackageInfo(
                string displayName,
                string value,
                PackageSource source = PackageSource.TunaRepo,
                bool selected = true)
            {
                DisplayName = displayName;
                Value = value;
                Source = source;
                Selected = selected;
            }
        }
    }
}

