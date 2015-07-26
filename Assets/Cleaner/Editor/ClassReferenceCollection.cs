/**
	asset cleaner
	Copyright (c) 2015 Tatsuhiko Yamamura

    This software is released under the MIT License.
    http://opensource.org/licenses/mit-license.php
*/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Linq;

namespace AssetClean
{
	public class ClassReferenceCollection
	{
		// type : guid
		public Dictionary<System.Type, List<string>> codeFileList = new Dictionary<System.Type, List<string>> ();
		// guid : types
		public Dictionary<string, List<System.Type>> references = new Dictionary<string, List<System.Type>> ();

		public void Collection ()
		{
			references.Clear ();
			EditorUtility.DisplayProgressBar ("checking", "collection all type", 0);

			// Connect the files and class.
			var codes = Directory.GetFiles ("Assets", "*.cs", SearchOption.AllDirectories);
			// connect each classes.
			var firstPassList = new List<string>();
			if( Directory.Exists ("Assets/Plugins") )
				firstPassList.AddRange( Directory.GetFiles ("Assets/Plugins", "*.cs", SearchOption.AllDirectories));
			if( Directory.Exists ("Assets/Standard Assets") )
				firstPassList.AddRange( Directory.GetFiles ("Assets/Standard Assets", "*.cs", SearchOption.AllDirectories));

			var allFirstpassTypes = collectionAllFastspassClasses ();
			CollectionCodeFileDictionary (allFirstpassTypes, firstPassList.ToArray());


			var alltypes = CollectionAllClasses ();
			CollectionCodeFileDictionary (alltypes, codes.ToArray());
			alltypes.AddRange (allFirstpassTypes);

			int count = 0;
			foreach (var codepath in firstPassList) {
				CollectionReferenceClasses (AssetDatabase.AssetPathToGUID (codepath), allFirstpassTypes);
				EditorUtility.DisplayProgressBar ("checking", "analytics codes", ((float)++count / codes.Length) * 0.5f + 0.5f);
			}
			count = 0;
			foreach (var codepath in codes) {
				CollectionReferenceClasses (AssetDatabase.AssetPathToGUID (codepath), alltypes);
				EditorUtility.DisplayProgressBar ("checking", "analytics codes", ((float)++count / codes.Length) * 0.5f);
			}
		}

		void CollectionCodeFileDictionary (List<System.Type> alltypes, string[] codes)
		{
			float count = 1;
			foreach (var codePath in codes) {
				EditorUtility.DisplayProgressBar ("checking", "search files", count++ / codes.Length);

				// connect file and classes.
				var code = System.IO.File.ReadAllText (codePath);
				code = Regex.Replace(code, "//.*[\\n\\r]", "");
				code = Regex.Replace(code, "/\\*.*[\\n\\r]\\*/", "");

				foreach (var type in alltypes) {

					if( codeFileList.ContainsKey(type ) == false ){
						codeFileList.Add(type, new List<string>());
					}
					var list = codeFileList[type];
				
					if (string.IsNullOrEmpty (type.Namespace) == false) {
						var namespacepattern = string.Format ("namespace[\\s.]{0}[{{\\s\\n]", type.Namespace);
						if (Regex.IsMatch (code, namespacepattern) == false) {
							continue;
						}
					}

					string typeName = type.IsGenericTypeDefinition ? type.GetGenericTypeDefinition ().Name.Split ('`') [0] : type.Name;
					if (Regex.IsMatch (code, string.Format ("class\\s*{0}?[\\s:<{{]", typeName))) {
						list.Add( AssetDatabase.AssetPathToGUID(codePath) );
						continue;
					}

					if (Regex.IsMatch (code, string.Format ("struct\\s*{0}[\\s:<{{]", typeName))) {
						list.Add( AssetDatabase.AssetPathToGUID(codePath) );
						continue;
					}
				
					if (Regex.IsMatch (code, string.Format ("enum\\s*{0}[\\s{{]", type.Name))) {
						list.Add( AssetDatabase.AssetPathToGUID(codePath) );
						continue;
					}
				
					if (Regex.IsMatch (code, string.Format ("delegate\\s*{0}\\s\\(", type.Name))) {
						list.Add( AssetDatabase.AssetPathToGUID(codePath) );
						continue;
					}
				}
			}
		}

		List<System.Type> CollectionAllClasses ()
		{
			List<System.Type> alltypes = new List<System.Type> ();
		
			if (File.Exists ("Library/ScriptAssemblies/Assembly-CSharp.dll"))
				alltypes.AddRange (Assembly.LoadFile ("Library/ScriptAssemblies/Assembly-CSharp.dll").GetTypes ());
			if (File.Exists ("Library/ScriptAssemblies/Assembly-CSharp-Editor.dll"))
				alltypes.AddRange (Assembly.LoadFile ("Library/ScriptAssemblies/Assembly-CSharp-Editor.dll").GetTypes ());

			return alltypes	.ToList ();
		}

		List<System.Type> collectionAllFastspassClasses()
		{
			List<System.Type> alltypes = new List<System.Type> ();
			if (File.Exists ("Library/ScriptAssemblies/Assembly-CSharp-firstpass.dll"))
				alltypes.AddRange (Assembly.LoadFile ("Library/ScriptAssemblies/Assembly-CSharp-firstpass.dll").GetTypes ());
			if (File.Exists ("Library/ScriptAssemblies/Assembly-CSharp-Editor-firstpass.dll"))
				alltypes.AddRange (Assembly.LoadFile ("Library/ScriptAssemblies/Assembly-CSharp-Editor-firstpass.dll").GetTypes ());
			return alltypes;
		}
		
		void CollectionReferenceClasses (string guid, List<System.Type> types)
		{
			var codePath = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty (codePath) || references.ContainsKey (guid) || File.Exists(codePath)==false) {
				return;
			}

			var code = System.IO.File.ReadAllText (codePath);
			code = Regex.Replace(code, "//.*[\\n\\r]", "");
			code = Regex.Replace(code, "/\\*.*[\\n\\r]\\*/", "");

			var list = new List<System.Type> ();
			references [guid] = list;

			foreach (var type in types) {
	
				if (string.IsNullOrEmpty (type.Namespace) == false) {
					var namespacepattern = string.Format ("[namespace|using][\\s\\.]{0}[{{\\s\\r\\n\\r;]", type.Namespace);
					if (Regex.IsMatch (code, namespacepattern) == false) {
						continue;
					}
				}

				if (codeFileList.ContainsKey (type) == false) {
					continue;
				}

				string match = string.Empty;
				if (type.IsGenericTypeDefinition) {
					string typeName = type.GetGenericTypeDefinition ().Name.Split ('`') [0];
					match = string.Format ("[\\]\\[\\.\\s<(]{0}[\\.\\s\\n\\r>,<(){{]", typeName);
				} else {
					match = string.Format ("[\\]\\[\\.\\s<(]{0}[\\.\\s\\n\\r>,<(){{\\]]", type.Name.Replace("Attribute", ""));
				}
				if (Regex.IsMatch (code, match)) {
					list.Add (type);
					var typeGuid =  codeFileList[type];
					foreach( var referenceGuid in typeGuid){
						CollectionReferenceClasses (referenceGuid, types);
					}
				}
			}
		}
	}
}
