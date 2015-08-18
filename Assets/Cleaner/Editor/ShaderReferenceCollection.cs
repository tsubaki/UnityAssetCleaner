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
	public class ShaderReferenceCollection : IReferenceCollection
	{
		// shader name / shader file guid
		public Dictionary<string, string> shaderFileList = new Dictionary<string, string> ();
		private List<CollectionData> references = new List<CollectionData>();

		public void Init(List<CollectionData> refs){
			references = refs;
		}

		public void CollectionFiles ()
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
				if( shaderFileList.ContainsKey(file) == false ){
					shaderFileList.Add (file, cgincPath);
				}
			}
		}

		void CheckReference ()
		{
			foreach (var shader in shaderFileList) {
				var shaderFilePath = AssetDatabase.GUIDToAssetPath(shader.Value);
				if( File.Exists(shaderFilePath) == false){
					continue;
				}

				var guid = shader.Value;

				List<string> referenceList = null;
				CollectionData reference =  null;
				
				if( references.Exists(c=>c.fileGuid == guid) == false ) {
					referenceList = new List<string>();
					reference = new CollectionData() {
						fileGuid = guid,
						referenceGids = referenceList,
					};
					references.Add(reference);
				}else{
					reference = references.Find(c=>c.fileGuid == guid);
					referenceList = reference.referenceGids;
				}

				reference.timeStamp = File.GetLastWriteTime(AssetDatabase.GUIDToAssetPath(guid));

				var code = ClassReferenceCollection.StripComment( File.ReadAllText (shaderFilePath));
			
				foreach (var checkingShaderName in shaderFileList.Keys) {
					if( checkingShaderName == shader.Key ){
						continue;
					}

					if (code.IndexOf(checkingShaderName) != -1 && shaderFileList.ContainsKey(checkingShaderName))  {
						var fileGuid = shaderFileList [checkingShaderName];
						if( referenceList.Contains(fileGuid) == false ){
							referenceList.Add (fileGuid);
						}
					}
				}
			}
		}
	}
}