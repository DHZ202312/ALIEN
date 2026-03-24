using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SearchEngine : MonoBehaviour
{
    [Header("Config")]
    public SearchDatabase database;
    public SearchAppType appType;

    [Header("StickyNotes")]
    public Button toSearchEngine;
    public Button toPoliceDatabase;

    [Header("Search UI")]
    public TMP_InputField queryInput;
    public Button searchButton;
    public RectTransform SearchElementPos;
    public GameObject SearchEngineLogo;
    public RectTransform DefaultPos;
    public RectTransform UpperPos;

    [Header("Results UI")]
    public GameObject resultsPanel;
    public Transform resultsContainer;
    public SearchResultItem resultItemPrefab;
    public GameObject NoResult;

    [Header("Article UI")]
    public GameObject articlePanel;
    public Button backButton;
    public TMP_Text articleTitle;
    public TMP_InputField articleBody;

    private readonly List<SearchResultItem> spawnedItems = new List<SearchResultItem>();

    private void Start()
    {
        searchButton.onClick.AddListener(DoSearch);
        backButton.onClick.AddListener(BackToResults);
        toSearchEngine.onClick.AddListener(BackToSearchEngine);

        resultsPanel.SetActive(true);
        articlePanel.SetActive(false);

        // 正文只读但可选中复制
        articleBody.readOnly = true;
    }

    public void DoSearch()
    {
        string query = Normalize(queryInput.text);

        ClearResults();

        if (string.IsNullOrWhiteSpace(query))
            return;

        SearchElementPos.anchoredPosition = UpperPos.anchoredPosition;
        SearchEngineLogo.SetActive(false);

        List<SearchDocument> matches = FindMatches(query);

        if (matches.Count == 0)
        {
            NoResult.SetActive(true);
            resultsPanel.SetActive(true);
            articlePanel.SetActive(false);
            return;
        }

        for (int i = 0; i < matches.Count; i++)
        {
            SearchResultItem item = Instantiate(resultItemPrefab, resultsContainer);
            item.Setup(matches[i], OpenDocument);
            spawnedItems.Add(item);
        }

        resultsPanel.SetActive(true);
        articlePanel.SetActive(false);
    }

    private List<SearchDocument> FindMatches(string query)
    {
        List<SearchDocument> results = new List<SearchDocument>();

        for (int i = 0; i < database.documents.Count; i++)
        {
            SearchDocument doc = database.documents[i];

            if (doc.appType != appType)
                continue;

            for (int j = 0; j < doc.keywords.Count; j++)
            {
                if (Normalize(doc.keywords[j]) == query)
                {
                    results.Add(doc);
                    break;
                }
            }
        }

        return results;
    }

    private void OpenDocument(SearchDocument doc)
    {
        SearchElementPos.gameObject.SetActive(false);
        articleTitle.text = doc.title;
        articleBody.SetTextWithoutNotify(doc.body);
        articleBody.caretPosition = 0;
        articleBody.selectionAnchorPosition = 0;
        articleBody.selectionFocusPosition = 0;
        articleBody.ForceLabelUpdate();

        resultsPanel.SetActive(false);
        articlePanel.SetActive(true);
    }

    private void BackToResults()
    {
        SearchElementPos.gameObject.SetActive(true);
        articlePanel.SetActive(false);
        resultsPanel.SetActive(true);
    }

    private void ClearResults()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }

        NoResult.SetActive(false);
        spawnedItems.Clear();
    }

    private string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
    private void BackToSearchEngine()
    {
        SearchElementPos.gameObject.SetActive(true);
        articlePanel.SetActive(false);
        resultsPanel.SetActive(false);
        SearchElementPos.anchoredPosition = DefaultPos.anchoredPosition;
        SearchEngineLogo.SetActive(true);
    }
}