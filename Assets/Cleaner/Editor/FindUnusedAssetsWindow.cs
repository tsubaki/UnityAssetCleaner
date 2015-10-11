/**
	asset cleaner
	Copyright (c) 2015 Tatsuhiko Yamamura

    This software is released under the MIT License.
    http://opensource.org/licenses/mit-license.php
*/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Linq;

namespace AssetClean
{
	public class FindUnusedAssetsWindow : EditorWindow
	{
		AssetCollector collection = new AssetCollector ();
		List<DeleteAsset> deleteAssets = new List<DeleteAsset> ();
		Vector2 scroll;

		[MenuItem("Window/Delete Unused Assets/only resource", false, 50)]
		static void InitWithoutCode ()
		{
			var window = FindUnusedAssetsWindow.CreateInstance<FindUnusedAssetsWindow> ();
			window.collection.useCodeStrip = false;
			window.collection.Collection (new string[]{"Assets"});
			window.CopyDeleteFileList (window.collection.deleteFileList);
			
			window.Show ();
		}

		[MenuItem("Window/Delete Unused Assets/unused by editor", false, 51)]
		static void InitWithout ()
		{
			var window = FindUnusedAssetsWindow.CreateInstance<FindUnusedAssetsWindow> ();
			window.collection.Collection (new string[]{"Assets"});
			window.CopyDeleteFileList (window.collection.deleteFileList);
			
			window.Show ();
		}

		[MenuItem("Window/Delete Unused Assets/unused by game", false, 52)]
		static void Init ()
		{
			var window = FindUnusedAssetsWindow.CreateInstance<FindUnusedAssetsWindow> ();
			window.collection.saveEditorExtensions = false;
			window.collection.Collection (new string[]{"Assets"});
			window.CopyDeleteFileList (window.collection.deleteFileList);
			
			window.Show ();
		}

//		[MenuItem("Assets/Delete Unused Assets/unused by editor", false, 52)]
//		static void InitAssets ()
//		{
//			var paths = Selection.objects
//				.Select(c=>AssetDatabase.GetAssetPath(c))
//				.Where(c=>Directory.Exists(c));
//			if( paths.Any(c=> string.IsNullOrEmpty(c) ) ){
//				return;
//			}
//
//			var window = FindUnusedAssetsWindow.CreateInstance<FindUnusedAssetsWindow> ();
//			window.collection.Collection (paths.ToArray());
//			window.CopyDeleteFileList (window.collection.deleteFileList);
//			
//			window.Show ();
//		}
//
//		[MenuItem("Assets/Delete Unused Assets/unused by editor", true)]
//		static bool InitAssetsA ()
//		{
//			var paths = Selection.objects
//				.Select(c=>AssetDatabase.GetAssetPath(c))
//				.Where(c=>Directory.Exists(c));
//			return ! paths.Any(c=> string.IsNullOrEmpty(c) );
//		}



		[MenuItem("Assets/Delete Unused Assets/unused only resources", false, 52)]
		static void InitAssetsOnlyResources ()
		{
			var paths = Selection.objects
				.Select(c=>AssetDatabase.GetAssetPath(c))
					.Where(c=>Directory.Exists(c));
			if( paths.Any(c=> string.IsNullOrEmpty(c) ) ){
				return;
			}
			
			var window = FindUnusedAssetsWindow.CreateInstance<FindUnusedAssetsWindow> ();
			window.collection.useCodeStrip = false;
			window.collection.Collection (paths.ToArray());
			window.CopyDeleteFileList (window.collection.deleteFileList);
			
			window.Show ();
		}
		[MenuItem("Assets/Delete Unused Assets/unused only resources", true)]
		static bool InitAssetsOnlyResourcesA ()
		{
			var paths = Selection.objects
				.Select(c=>AssetDatabase.GetAssetPath(c))
					.Where(c=>Directory.Exists(c));
			return ! paths.Any(c=> string.IsNullOrEmpty(c) );
		}

		[MenuItem("Window/Delete Unused Assets/Clear cache")]
		static void ClearCache()
		{
			File.Delete(AssetClean.AssetCollector.exportXMLPath);
			File.Delete(AssetClean.ClassReferenceCollection.xmlPath);

			EditorUtility.DisplayDialog("clear file", "clear file", "OK");
		}


		void OnGUI ()
		{
			using (var horizonal = new EditorGUILayout.HorizontalScope("box")) {
				EditorGUILayout.LabelField ("delete unreference assets from buildsettings and resources");
			}

			using (var scrollScope = new EditorGUILayout.ScrollViewScope(scroll)) {
				scroll = scrollScope.scrollPosition;
				foreach (var asset in deleteAssets) {
					if (string.IsNullOrEmpty (asset.path)) {
						continue;
					}

					using (var horizonal = new EditorGUILayout.HorizontalScope()) {
						asset.isDelete = EditorGUILayout.Toggle (asset.isDelete, GUILayout.Width (20));
						var icon = AssetDatabase.GetCachedIcon (asset.path);
						GUILayout.Label (icon, GUILayout.Width (20), GUILayout.Height (20));
						if (GUILayout.Button (asset.path, EditorStyles.largeLabel)) {
							Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object> (asset.path);
						}
					}
				}


			}
			using (var horizonal = new EditorGUILayout.HorizontalScope("box")) {
				EditorGUILayout.Space();
				if (GUILayout.Button ("Exclude from Project", GUILayout.Width (160)) && deleteAssets.Count != 0) {
					EditorApplication.delayCall += Exclude;
				}
			}

		}

		void Exclude()
		{
			RemoveFiles ();
			Close ();
		}

	     static void CleanDir()
	     {
			RemoveEmptyDirectry ("Assets");
			AssetDatabase.Refresh ();
		}

		void CopyDeleteFileList(IEnumerable<string> deleteFileList)
		{
			foreach (var asset in deleteFileList) {
				var filePath = AssetDatabase.GUIDToAssetPath (asset);
				if (string.IsNullOrEmpty (filePath) == false) {
					deleteAssets.Add (new DeleteAsset (){ path = filePath});
				}
			}
		}

		void RemoveFiles ()
		{
			try {
				string exportDirectry = "BackupUnusedAssets";
				Directory.CreateDirectory (exportDirectry);
				var files = deleteAssets.Where (item => item.isDelete == true).Select (item => item.path).ToArray ();
				string backupPackageName = exportDirectry + "/package" + System.DateTime.Now.ToString ("yyyyMMddHHmmss") + ".unitypackage";
				EditorUtility.DisplayProgressBar ("export package", backupPackageName, 0);

				AssetDatabase.ExportPackage (files, backupPackageName);

				int i = 0;
				int length = deleteAssets.Count;

				foreach (var assetPath in files) {
					i++;
					EditorUtility.DisplayProgressBar ("delete unused assets", assetPath, (float)i / length);
					AssetDatabase.DeleteAsset (assetPath);
					if( File.Exists(assetPath) ){
						File.Delete(assetPath);
					}
				}

				EditorUtility.DisplayProgressBar ("clean directory", "", 1);
				foreach (var dir in Directory.GetDirectories("Assets")) {
					RemoveEmptyDirectry (dir);
				}

				System.Diagnostics.Process.Start (exportDirectry);

				AssetDatabase.Refresh ();
			}
			catch( System.Exception e ){
				Debug.Log(e.Message);
			}finally {
				EditorUtility.ClearProgressBar ();
			}
		}

		static void RemoveEmptyDirectry (string path)
		{
			var dirs = Directory.GetDirectories (path);
			foreach (var dir in dirs) {
				RemoveEmptyDirectry (dir);
			}
		

			var files = Directory.GetFiles (path, "*", SearchOption.TopDirectoryOnly).Where (item => Path.GetExtension (item) != ".meta");
			if (files.Count () == 0 && Directory.GetDirectories (path).Count () == 0) {
				var metaFile = AssetDatabase.GetTextMetaFilePathFromAssetPath(path);
				UnityEditor.FileUtil.DeleteFileOrDirectory (path);
				UnityEditor.FileUtil.DeleteFileOrDirectory (metaFile);
			}
		}

		class DeleteAsset
		{
			public bool isDelete = true;
			public string path;
		}
	}
}
