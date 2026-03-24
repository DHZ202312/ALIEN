using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SearchResultItem : MonoBehaviour
{
    public Button button;
    public TMP_Text titleText;

    private SearchDocument boundDocument;
    private Action<SearchDocument> onClick;

    public void Setup(SearchDocument document, Action<SearchDocument> clickCallback)
    {
        boundDocument = document;
        onClick = clickCallback;

        titleText.text = document.title;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            onClick?.Invoke(boundDocument);
        });
    }
}