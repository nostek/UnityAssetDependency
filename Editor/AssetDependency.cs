using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityAssetDependency
{
	public class AssetResultsWindow : EditorWindow
	{
		Dictionary<string, List<string>> allResults = new Dictionary<string, List<string>>();

		Vector2 scroll;

		bool showUsed = true;
		bool showUnused = true;

		public static void ShowWindow(Dictionary<string, List<string>> allResults)
		{
			var window = EditorWindow.GetWindow<AssetResultsWindow>();
			window.titleContent = new GUIContent("Dependency");
			window.allResults = allResults;
			window.Show();
		}

		void OnGUI()
		{
			string clicked = null;

			var btnStyle = GUI.skin.button;
			btnStyle.alignment = TextAnchor.MiddleLeft;

			using (new EditorGUILayout.HorizontalScope())
			{
				showUsed = EditorGUILayout.Toggle("Show Used", showUsed);
				showUnused = EditorGUILayout.Toggle("Show Unused", showUnused);
			}

			scroll = EditorGUILayout.BeginScrollView(scroll);
			EditorGUILayout.BeginVertical();

			foreach (var kvp in allResults)
			{
				if (!showUsed && kvp.Value.Count != 0)
					continue;
				if (!showUnused && kvp.Value.Count == 0)
					continue;

				EditorGUILayout.LabelField(kvp.Key, EditorStyles.boldLabel);

				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				EditorGUILayout.LabelField("Found in:");
				foreach (var path in kvp.Value)
					if (GUILayout.Button(path, btnStyle))
						clicked = path;
				EditorGUILayout.EndVertical();

				EditorGUILayout.Space();
				EditorGUILayout.Space();
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Select Used"))
			{
				List<Object> objs = new List<Object>();
				foreach (var kvp in allResults)
					if (kvp.Value.Count > 0)
						objs.Add(AssetDatabase.LoadMainAssetAtPath(kvp.Key));
				foreach (var o in objs)
					EditorGUIUtility.PingObject(o);
				Selection.objects = objs.ToArray();
			}

			if (GUILayout.Button("Select Unused"))
			{
				List<Object> objs = new List<Object>();
				foreach (var kvp in allResults)
					if (kvp.Value.Count == 0)
						objs.Add(AssetDatabase.LoadMainAssetAtPath(kvp.Key));
				foreach (var o in objs)
					EditorGUIUtility.PingObject(o);
				Selection.objects = objs.ToArray();
			}

			if (GUILayout.Button("Select References"))
			{
				List<Object> objs = new List<Object>();
				foreach (var kvp in allResults)
					foreach (var path in kvp.Value)
						objs.Add(AssetDatabase.LoadMainAssetAtPath(path));
				foreach (var o in objs)
					EditorGUIUtility.PingObject(o);
				Selection.objects = objs.ToArray();
			}

			if (GUILayout.Button("Close"))
				Close();

			EditorGUILayout.EndHorizontal();

			if (clicked != null)
			{
				var obj = AssetDatabase.LoadMainAssetAtPath(clicked);
				if (obj != null)
					EditorGUIUtility.PingObject(obj);
			}
		}
	}

	public class AssetDependency : AssetPostprocessor
	{
		static Dictionary<string, List<string>> database = null;

		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			database = null;
		}

		[MenuItem("Assets/Find Where Used In Project", false, 30)]
		static void CheckUsage()
		{
			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			BuildDatabase();

			Dictionary<string, List<string>> allResults = new Dictionary<string, List<string>>();

			bool foundAnything = false;

			UnityEngine.Object[] objs = Selection.objects;
			foreach (var obj in objs)
			{
				if (obj == null)
					continue;

				HashSet<string> results = new HashSet<string>();

				string path = AssetDatabase.GetAssetPath(obj);

				List<string> dependants = null;
				if (database.TryGetValue(path, out dependants))
					foreach (var depPath in dependants)
						results.Add(depPath);

				foundAnything |= results.Count > 0;

				var sorted = new List<string>(results);
				sorted.Sort();

				allResults.Add(path, sorted);
			}

			sw.Stop();
			// Debug.Log("Search Time: " + sw.ElapsedMilliseconds + "ms");

			if (foundAnything)
				AssetResultsWindow.ShowWindow(allResults);
			else
				EditorUtility.DisplayDialog("AssetDependency", "Did not find anything that uses selected asset.", "OK");
		}

		static void BuildDatabase()
		{
			//Already built database.
			if (database != null)
				return;

			EditorUtility.DisplayProgressBar("Building database", "Caching dependencies..", 0f);

			var paths = AssetDatabase.GetAllAssetPaths();

			database = new Dictionary<string, List<string>>();

			float step = 1f / (float)paths.Length;
			float progress = 0f;

			for (int i = 0, imax = paths.Length; i < imax; ++i)
			{
				var path = paths[i];

				if (AssetDatabase.IsValidFolder(path))
					continue;

				if (!path.StartsWith("Assets/"))
					continue;

				var dependencies = AssetDatabase.GetDependencies(path, false);

				for (int x = 0, xmax = dependencies.Length; x < xmax; ++x)
				{
					var otherPath = dependencies[x];

					if (!database.TryGetValue(otherPath, out var refs))
					{
						refs = new List<string>();
						database.Add(otherPath, refs);
					}

					refs.Add(path);
				}

				progress += step;

				EditorUtility.DisplayProgressBar("Building database", "Caching dependencies..", progress);
			}

			EditorUtility.ClearProgressBar();
		}
	}
}
