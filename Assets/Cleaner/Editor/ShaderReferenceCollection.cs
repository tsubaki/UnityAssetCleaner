/**
	asset cleaner
	Copyright (c) 2015 Tatsuhiko Yamamura

    This software is released under the MIT License.
    http://opensource.org/licenses/mit-license.php
*/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace AssetClean
{
	public class ShaderReferenceCollection
	{
		// shader name / shader file guid
		public Dictionary<string, string> shaderFileList = new Dictionary<string, string> ();
		public Dictionary<string, List<string> > shaderReferenceList = new Dictionary<string, List<string>> ();

		public void Collection ()
		{
			CollectionShaderFiles ();
			CheckReference ();
		}

		void CollectionShaderFiles ()
		{
			var shaderFiles = Directory.GetFiles ("Assets", "*.shader", SearchOption.AllDirectories);
			foreach (var shaderFilePath in shaderFiles) {
				var code = File.ReadAllText (shaderFilePath);
				var match = Regex.Match (code, "Shader \"(?<name>.*)\"");
				if (match.Success) {
					var shaderName = match.Groups ["name"].ToString ();
					if (shaderFileList.ContainsKey (shaderName) == false) {
						shaderFileList.Add (shaderName, AssetDatabase.AssetPathToGUID(shaderFilePath));
					}
				}
			}
		
			var cgFiles = Directory.GetFiles ("Assets", "*.cg", SearchOption.AllDirectories);
			foreach (var cgFilePath in cgFiles) {
				var file = Path.GetFileName (cgFilePath);
				shaderFileList.Add (file, cgFilePath);
			}

			var cgincFiles = Directory.GetFiles ("Assets", "*.cginc", SearchOption.AllDirectories);
			foreach (var cgincPath in cgincFiles) {
				var file = Path.GetFileName (cgincPath);
				shaderFileList.Add (file, cgincPath);
			}
		}

		void CheckReference ()
		{
			foreach (var shader in shaderFileList) {
				var shaderFilePath = AssetDatabase.GUIDToAssetPath(shader.Value);
				var shaderName = shader.Key;
			
				List<string> referenceList = new List<string> ();
				shaderReferenceList.Add (shaderName, referenceList);
			
				var code = File.ReadAllText (shaderFilePath);
			
				foreach (var checkingShaderName in shaderFileList.Keys) {
					if (Regex.IsMatch (code, string.Format ("{0}", checkingShaderName))) {
						var filePath = shaderFileList [checkingShaderName];
						referenceList.Add (filePath);
					}
				}
			}
		}
	}
}