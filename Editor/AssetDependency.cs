using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;

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
		float start = Time.realtimeSinceStartup;

		BuildDatabase();

		StringBuilder sb = new StringBuilder();
		sb.AppendLine("Search Report");
		sb.AppendLine("");

		UnityEngine.Object[] objs = Selection.objects;
		foreach (var obj in objs)
		{
			if (obj == null)
				continue;
			
			string path = AssetDatabase.GetAssetPath(obj);
			string guid = AssetDatabase.AssetPathToGUID(path);

			sb.AppendFormat("Searching for {0}...\n", obj.name);

			List<string> users = null;
			if (database.TryGetValue(guid, out users))
				foreach (var u in users)
					sb.AppendFormat("Found in: {0}\n", AssetDatabase.GUIDToAssetPath(u));

			sb.AppendLine();
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

		float stop = Time.realtimeSinceStartup;

		sb.AppendFormat("Elapsed time: {0}\n", (stop - start));

		Debug.Log(sb.ToString());
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
