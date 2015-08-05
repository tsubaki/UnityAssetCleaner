using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

[System.Serializable]
public class CollectionData  
{
	public string fileGuid;
	public List<string> referenceGids = new List<string>();
}


public interface IReferenceCollection
{
	void CollectionFiles();
	void Init(List<CollectionData> refs);
}