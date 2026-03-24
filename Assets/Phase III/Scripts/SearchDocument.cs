using System.Collections.Generic;
using UnityEngine;

public enum SearchAppType
{
    SearchEngine,
    PoliceDatabase
}

[System.Serializable]
public class SearchDocument
{
    public string id;
    public string title;
    [TextArea(5, 30)] public string body;

    public SearchAppType appType;

    [Tooltip("支持多个关键词命中同一文档")]
    public List<string> keywords = new List<string>();
}