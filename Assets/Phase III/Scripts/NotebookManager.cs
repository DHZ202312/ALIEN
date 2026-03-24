using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NotebookManager : MonoBehaviour
{
    [Header("Refs")]
    public TMP_InputField contentInput;

    public Button pageButton1;
    public Button pageButton2;
    public Button pageButton3;
    public Button pageButton4;
    public Button pageButton5;

    [Header("Page Data")]
    public string[] pageContents = new string[5];

    [Header("Visual")]
    public Image[] pageButtonImages;

    public float normalBrightness = 1f;
    public float selectedBrightness = 1.25f;

    Color[] originalColors;

    int currentPageIndex = 0;

    void Start()
    {
        if (pageContents.Length != 5)
            pageContents = new string[5];

        originalColors = new Color[pageButtonImages.Length];

        for (int i = 0; i < pageButtonImages.Length; i++)
        {
            originalColors[i] = pageButtonImages[i].color;
        }

        pageButton1.onClick.AddListener(() => SwitchPage(0));
        pageButton2.onClick.AddListener(() => SwitchPage(1));
        pageButton3.onClick.AddListener(() => SwitchPage(2));
        pageButton4.onClick.AddListener(() => SwitchPage(3));
        pageButton5.onClick.AddListener(() => SwitchPage(4));

        contentInput.onValueChanged.AddListener(OnContentChanged);

        LoadPage(0);
    }

    void SwitchPage(int newPage)
    {
        if (newPage == currentPageIndex) return;

        SavePage();
        LoadPage(newPage);
    }

    void SavePage()
    {
        pageContents[currentPageIndex] = contentInput.text;
    }

    void LoadPage(int index)
    {
        currentPageIndex = index;

        contentInput.SetTextWithoutNotify(pageContents[index]);
        contentInput.ForceLabelUpdate();

        RefreshTabVisuals();
    }

    void OnContentChanged(string value)
    {
        pageContents[currentPageIndex] = value;
    }

    void RefreshTabVisuals()
    {
        for (int i = 0; i < pageButtonImages.Length; i++)
        {
            float brightness = (i == currentPageIndex) ? selectedBrightness : normalBrightness;

            Color baseColor = originalColors[i];

            pageButtonImages[i].color = new Color(
                baseColor.r * brightness,
                baseColor.g * brightness,
                baseColor.b * brightness,
                baseColor.a
            );
        }
    }
}