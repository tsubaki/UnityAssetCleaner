using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

[System.Serializable]
public class CollectionData  
{
	public string fileGuid;
	public string fileName;
	public List<string> referenceGids = new List<string>();
	public DateTime timeStamp;
}

[System.Serializable]
public class TypeDate
{
	public string guid;
	public string fileName;
	public DateTime timeStamp;
	public List<string> typeFullName = new List<string>();
	public string assemblly;

	public void Add(Type addtype){
		assemblly = addtype.Assembly.FullName;
		var typeName = addtype.FullName;
		if( typeFullName.Contains(typeName) == false){
			typeFullName.Add(typeName);
		}
	}

	public Type[] types{
		get{
			return typeFullName.Select(c=>Type.GetType(c)).ToArray();
		}
	}
}

public interface IReferenceCollection
{
	void CollectionFiles();
	void Init(List<CollectionData> refs);
}

