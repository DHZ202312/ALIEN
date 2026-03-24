using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Search Database")]
public class SearchDatabase : ScriptableObject
{
    public List<SearchDocument> documents = new List<SearchDocument>();
}
