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
	public class ClassReferenceCollection : IReferenceCollection
	{
		// guid : types
		private List<CollectionData> references = null;

		// type : guid
		private Dictionary<System.Type, List<string>> codeFileList = new Dictionary<System.Type, List<string>> ();

		private bool isSaveEditorCode = false;

		public ClassReferenceCollection(bool saveStiroCode = false )
		{
			isSaveEditorCode = saveStiroCode;
		}

		public void Init(List<CollectionData> refs){
			references = refs;
		}

		public void CollectionFiles ()
		{
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

			foreach (var codepath in firstPassList) {
				CollectionReferenceClasses (AssetDatabase.AssetPathToGUID (codepath), allFirstpassTypes);
			}
			foreach (var codepath in codes) {
				CollectionReferenceClasses (AssetDatabase.AssetPathToGUID (codepath), alltypes);
			}

			if( isSaveEditorCode ){
				CollectionCustomEditorClasses(alltypes);
			}
		}

		void CollectionCodeFileDictionary (List<System.Type> alltypes, string[] codes)
		{
			float count = 1;
			foreach (var codePath in codes) {
				EditorUtility.DisplayProgressBar ("checking", "search files", count++ / codes.Length);

				// connect file and classes.
				var code = StripComment(System.IO.File.ReadAllText (codePath));

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
			if (isSaveEditorCode && File.Exists ("Library/ScriptAssemblies/Assembly-CSharp-Editor.dll"))
				alltypes.AddRange (Assembly.LoadFile ("Library/ScriptAssemblies/Assembly-CSharp-Editor.dll").GetTypes ());

			return alltypes	.ToList ();
		}

		List<System.Type> collectionAllFastspassClasses()
		{
			List<System.Type> alltypes = new List<System.Type> ();
			if (File.Exists ("Library/ScriptAssemblies/Assembly-CSharp-firstpass.dll"))
				alltypes.AddRange (Assembly.LoadFile ("Library/ScriptAssemblies/Assembly-CSharp-firstpass.dll").GetTypes ());
			if (isSaveEditorCode && File.Exists ("Library/ScriptAssemblies/Assembly-CSharp-Editor-firstpass.dll"))
				alltypes.AddRange (Assembly.LoadFile ("Library/ScriptAssemblies/Assembly-CSharp-Editor-firstpass.dll").GetTypes ());
			return alltypes;
		}

		public static string StripComment(string code)
		{
			code = Regex.Replace(code, "//.*[\\n\\r]", "");
			code = Regex.Replace(code, "/\\*.*[\\n\\r]\\*/", "");
			return code;
		}
		
		void CollectionReferenceClasses (string guid, List<System.Type> types)
		{
			var codePath = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty (codePath) || File.Exists(codePath)==false) {
				return;
			}

			var code = StripComment( System.IO.File.ReadAllText (codePath));

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

			foreach (var type in types) {

				if (codeFileList.ContainsKey (type) == false || codeFileList[type].Contains(guid)) {
					continue;
				}

				if (string.IsNullOrEmpty (type.Namespace) == false) {
					var namespacepattern = string.Format ("([namespace|using][\\s]{0}[{{\\s\\r\\n\\r;]|{0}\\.)", type.Namespace);
					if (Regex.IsMatch (code, namespacepattern) == false) {
						continue;
					}
				}

				string match = string.Empty;

				if (type.IsGenericTypeDefinition) {
					string typeName = type.GetGenericTypeDefinition ().Name.Split ('`') [0];
					match = string.Format ("[\\]\\[\\.\\s<(]{0}[\\.\\s\\n\\r>,<(){{]", typeName);

				} else {
					string typeName = type.Name.Replace("Attribute", "");
					match = string.Format ("[\\]\\[\\.\\s<(]{0}[\\.\\s\\n\\r>,<(){{\\]]", typeName);

					if( Regex.IsMatch(code, string.Format("this\\s{0}\\s", typeName ) )){
						foreach( var file in codeFileList[type] ){
							foreach( var baseReference in references.Where(c=>c.fileGuid == file)){
								baseReference.referenceGids.Add(guid);
							}
						}
					}
				}
				if (Regex.IsMatch (code, match)) {
					var typeGuids =  codeFileList[type];
					foreach( var typeGuid in typeGuids ){

						if( referenceList.Contains(typeGuid ) == false ){
							referenceList.Add(typeGuid);
						}
					}
				}
			}
		}

		void CollectionCustomEditorClasses(IEnumerable<System.Type> types)
		{
			foreach( var type in types )
			{
				
				if( codeFileList.ContainsKey(type ) == false ){
					continue;
				}
				var filePaths = codeFileList[type];

                 var attributes = type.GetCustomAttributes(typeof(CustomEditor), true);
				foreach( var attribute in attributes ){
					if( attribute is CustomEditor  == false){
						continue;
					}
					var customEditor = attribute as CustomEditor;
					var customEditorReferenceTypeField = typeof(CustomEditor).GetField("m_InspectedType", BindingFlags.Instance | BindingFlags.NonPublic);
					var customEditorReferenceType = (System.Type)customEditorReferenceTypeField.GetValue(customEditor);

					if( codeFileList.ContainsKey(customEditorReferenceType ) == false ){
						continue;
					}



					foreach( var filePath in codeFileList[customEditorReferenceType]){
						if( references.Exists(c=>c.fileGuid == filePath) == false ){
							continue;
						}
						var list = references.First(c=>c.fileGuid == filePath).referenceGids;
						list.AddRange(codeFileList[type]);
					}
				}
			}
		}
	}
}
