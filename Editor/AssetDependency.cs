using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityAssetDependency
{
	public class AssetResultsWindow : EditorWindow
	{
		Dictionary<string, HashSet<string>> allResults = new Dictionary<string, HashSet<string>>();

		Vector2 scroll;

		public static void ShowWindow(Dictionary<string, HashSet<string>> allResults)
		{
			var window = EditorWindow.GetWindow<AssetResultsWindow>();
			window.titleContent = new GUIContent("Dependency");
			window.allResults = allResults;
			window.Show();
		}

		void OnGUI()
		{
			string clicked = null;

			scroll = EditorGUILayout.BeginScrollView(scroll);
			EditorGUILayout.BeginVertical();

			int index = 0;
			foreach (var kvp in allResults)
			{
				index++;

				EditorGUILayout.LabelField(kvp.Key, EditorStyles.boldLabel);

				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				EditorGUILayout.LabelField("Found in:");
				foreach (var path in kvp.Value)
					if (GUILayout.Button(path))
						clicked = path;
				EditorGUILayout.EndVertical();

				EditorGUILayout.Space();
				EditorGUILayout.Space();
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space();

			if (GUILayout.Button("Close"))
				Close();

			if (clicked != null)
			{
				var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(clicked);
				if (obj != null)
					EditorGUIUtility.PingObject(obj);
			}
		}
	}

	public class AssetDependency : AssetPostprocessor
	{
		static string[] allGuids = null;
		static Dictionary<string, List<string>> database = null;

		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			allGuids = null;
			database = null;
		}

		[MenuItem("Assets/Find Where Used In Project", false, 30)]
		static void CheckUsage()
		{
			//float start = Time.realtimeSinceStartup;

			BuildDatabase();

			Dictionary<string, HashSet<string>> allResults = new Dictionary<string, HashSet<string>>();

			bool foundAnything = false;

			UnityEngine.Object[] objs = Selection.objects;
			foreach (var obj in objs)
			{
				if (obj == null)
					continue;

				HashSet<string> results = new HashSet<string>();

				string path = AssetDatabase.GetAssetPath(obj);
				string guid = AssetDatabase.AssetPathToGUID(path);

				if (AssetDatabase.IsValidFolder(path))
					continue;

				List<string> users = null;
				if (database.TryGetValue(guid, out users))
					foreach (var u in users)
						results.Add(AssetDatabase.GUIDToAssetPath(u));

				foundAnything |= results.Count > 0;

				allResults.Add(path, results);
			}

			/*
			Log.D("The following assets are not used:");
			foreach (var guid in allGuids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var type = AssetDatabase.GetMainAssetTypeAtPath(path);

				if (type == typeof(UnityEditor.DefaultAsset))
					continue;

				if (!database.ContainsKey(guid))
					Log.D(path);
			}
			*/

			//float stop = Time.realtimeSinceStartup;

			if (foundAnything)
				AssetResultsWindow.ShowWindow(allResults);
			else
				EditorUtility.DisplayDialog("AssetDependency", "Did not find anything that uses selected asset.", "OK");
		}

		static void BuildDatabase()
		{
			//Already built database.
			if (allGuids != null && database != null)
				return;

			EditorUtility.DisplayProgressBar("Building database", "Caching dependencies..", 0f);

			allGuids = AssetDatabase.FindAssets("t:object");

			database = new Dictionary<string, List<string>>();

			float step = 1f / (float)allGuids.Length;
			float progress = 0f;

			for (int i = 0; i < allGuids.Length; i++)
			{
				var guid = allGuids[i];
				var dependencies = AssetDatabase.GetDependencies(AssetDatabase.GUIDToAssetPath(guid), false);

				foreach (var dep in dependencies)
				{
					var g = AssetDatabase.AssetPathToGUID(dep);

					List<string> refs = null;
					if (!database.TryGetValue(g, out refs))
					{
						refs = new List<string>();
						database.Add(g, refs);
					}

					refs.Add(guid);
				}

				progress += step;

				EditorUtility.DisplayProgressBar("Building database", "Caching dependencies..", progress);
			}

			EditorUtility.ClearProgressBar();
		}
	}
}
