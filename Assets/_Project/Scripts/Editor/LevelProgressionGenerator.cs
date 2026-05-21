#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using OceanFactory.Data;

namespace OceanFactory.EditorTools
{
    /// <summary>
    /// One-click generator for the default 10-level campaign. Creates one LevelGoalSO per level,
    /// then a LevelProgressionSO that chains them. Re-running the menu overwrites existing assets
    /// at the same paths, so it is safe to iterate.
    /// </summary>
    public static class LevelProgressionGenerator
    {
        private const string LevelsFolder = "Assets/_Project/Configs/Levels";
        private const string ItemsFolder  = "Assets/_Project/Configs/Items";
        private const string ProgressionAssetPath = LevelsFolder + "/LevelProgression.asset";

        // (title shown in HUD [RU], file-name slug [EN, kept stable across renames],
        //  list of (itemAssetName-without-extension, targetCount))
        private static readonly LevelTemplate[] Templates =
        {
            new LevelTemplate("Запуск фабрики", "Bootstrap_the_Factory",
                ("Item_IronOre", 15)),

            new LevelTemplate("Первая плавка", "First_Smelting",
                ("Item_IronIngot", 20)),

            new LevelTemplate("Оптические основы", "Optical_Foundations",
                ("Item_RubyLens", 10)),

            new LevelTemplate("Цветовое разнообразие", "Color_Variety",
                ("Item_SapphireLens", 8),
                ("Item_EmeraldLens", 8)),

            // NB: asset is intentionally Item_OpticCurcuit (typo preserved from initial import).
            new LevelTemplate("Микросхемы", "Circuit_Boards",
                ("Item_OpticCurcuit", 15)),

            new LevelTemplate("Энергетическая фаза", "Energy_Phase",
                ("Item_PowerRegulator", 12)),

            new LevelTemplate("Производство каркасов", "Frame_Production",
                ("Item_ModularFrame", 10)),

            new LevelTemplate("Кристальные технологии", "Crystal_Tech",
                ("Item_CrystalOscillator", 8)),

            new LevelTemplate("Массовое производство линз", "Lens_Mass_Production",
                ("Item_RubyLens", 20),
                ("Item_SapphireLens", 20),
                ("Item_EmeraldLens", 20)),

            new LevelTemplate("Квантовая эра", "Quantum_Era",
                ("Item_QuantumCore", 5)),
        };

        [MenuItem("OceanFactory/Generate Default 10 Levels")]
        public static void Generate()
        {
            EnsureFolder(LevelsFolder);

            var progression = AssetDatabase.LoadAssetAtPath<LevelProgressionSO>(ProgressionAssetPath);
            bool createdProgression = false;
            if (progression == null)
            {
                progression = ScriptableObject.CreateInstance<LevelProgressionSO>();
                AssetDatabase.CreateAsset(progression, ProgressionAssetPath);
                createdProgression = true;
            }
            progression.levels = new List<LevelGoalSO>(Templates.Length);

            int missingItems = 0;
            for (int i = 0; i < Templates.Length; i++)
            {
                var t = Templates[i];
                string levelPath = $"{LevelsFolder}/Level_{(i + 1):00}_{t.FileSlug}.asset";

                var level = AssetDatabase.LoadAssetAtPath<LevelGoalSO>(levelPath);
                if (level == null)
                {
                    level = ScriptableObject.CreateInstance<LevelGoalSO>();
                    AssetDatabase.CreateAsset(level, levelPath);
                }
                level.levelNumber = i + 1;
                level.title = t.Title;
                level.requirements = new List<LevelGoalSO.Requirement>(t.Requirements.Length);
                for (int r = 0; r < t.Requirements.Length; r++)
                {
                    var (itemName, count) = t.Requirements[r];
                    var item = LoadItem(itemName);
                    if (item == null)
                    {
                        Debug.LogWarning($"[LevelGen] Level {i + 1} '{t.Title}': could not find {itemName}.asset under {ItemsFolder}. Slot left empty — wire it manually.");
                        missingItems++;
                    }
                    level.requirements.Add(new LevelGoalSO.Requirement { item = item, targetCount = count });
                }
                EditorUtility.SetDirty(level);
                progression.levels.Add(level);
            }

            EditorUtility.SetDirty(progression);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = progression;
            EditorGUIUtility.PingObject(progression);

            string verb = createdProgression ? "Created" : "Updated";
            Debug.Log($"[LevelGen] {verb} {Templates.Length} levels + progression at {ProgressionAssetPath}. " +
                      (missingItems > 0
                          ? $"WARNING: {missingItems} requirement slot(s) had missing items. See warnings above."
                          : "All requirements wired successfully."));
            Debug.Log("[LevelGen] Next step: drag LevelProgression.asset onto the Hub prefab's 'Progression' slot.");
        }

        private static ItemTypeSO LoadItem(string nameWithoutExt)
        {
            string path = $"{ItemsFolder}/{nameWithoutExt}.asset";
            return AssetDatabase.LoadAssetAtPath<ItemTypeSO>(path);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private readonly struct LevelTemplate
        {
            public readonly string Title;
            public readonly string FileSlug;
            public readonly (string itemName, int count)[] Requirements;
            public LevelTemplate(string title, string fileSlug, params (string itemName, int count)[] reqs)
            {
                Title = title;
                FileSlug = fileSlug;
                Requirements = reqs;
            }
        }
    }
}
#endif
