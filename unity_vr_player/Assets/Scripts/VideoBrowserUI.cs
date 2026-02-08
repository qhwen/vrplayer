using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime local-video browser used by mobile builds.
/// </summary>
public class VideoBrowserUI : MonoBehaviour
{
    private const int MaxVisibleItems = 20;

    private VRVideoPlayer videoPlayer;
    private LocalFileManager localFileManager;

    private Canvas canvas;
    private RectTransform listContent;
    private Text statusText;

    private readonly List<Button> generatedButtons = new List<Button>();

    private void Start()
    {
        videoPlayer = FindObjectOfType<VRVideoPlayer>();
        localFileManager = FindObjectOfType<LocalFileManager>();

        if (videoPlayer == null || localFileManager == null)
        {
            Debug.LogError("VideoBrowserUI initialization failed: missing dependencies.");
            enabled = false;
            return;
        }

        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("UICanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        BuildPanel();
        RefreshVideoList();
    }

    private void BuildPanel()
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject panelObject = new GameObject("VideoBrowserPanel");
        panelObject.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-20f, 20f);
        panelRect.sizeDelta = new Vector2(420f, 520f);

        Image background = panelObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.75f);

        CreateLabel(panelObject.transform, "Title", "Local Videos", new Vector2(0f, 460f), new Vector2(360f, 34f), font, 24, TextAnchor.MiddleCenter);

        Button refreshButton = CreateButton(panelObject.transform, "RefreshButton", "Refresh", new Vector2(0f, 412f), new Vector2(160f, 40f), font);
        refreshButton.onClick.AddListener(RefreshVideoList);

        statusText = CreateLabel(panelObject.transform, "StatusText", "Loading...", new Vector2(0f, 374f), new Vector2(390f, 30f), font, 16, TextAnchor.MiddleCenter);

        GameObject scrollObject = new GameObject("VideoScroll");
        scrollObject.transform.SetParent(panelObject.transform, false);

        RectTransform scrollRect = scrollObject.AddComponent<RectTransform>();
        scrollRect.anchoredPosition = new Vector2(0f, 168f);
        scrollRect.sizeDelta = new Vector2(390f, 320f);

        Image scrollBackground = scrollObject.AddComponent<Image>();
        scrollBackground.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

        ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        GameObject viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        Mask mask = viewportObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);
        listContent = contentObject.AddComponent<RectTransform>();
        listContent.anchorMin = new Vector2(0f, 1f);
        listContent.anchorMax = new Vector2(1f, 1f);
        listContent.pivot = new Vector2(0.5f, 1f);
        listContent.anchoredPosition = Vector2.zero;
        listContent.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewportRect;
        scroll.content = listContent;
    }

    private void RefreshVideoList()
    {
        if (localFileManager == null)
        {
            return;
        }

        List<VideoFile> videos = localFileManager.GetLocalVideos();
        RegenerateItems(videos);

        if (videos.Count == 0)
        {
            string cachePath = localFileManager.GetCacheDirectory();
            statusText.text = "No video found. Put MP4 files in:\n" + cachePath + "\nor /storage/emulated/0/Movies";
            return;
        }

        statusText.text = "Found " + videos.Count + " videos. Tap one to play.";
    }

    private void RegenerateItems(List<VideoFile> videos)
    {
        for (int i = 0; i < generatedButtons.Count; i++)
        {
            if (generatedButtons[i] != null)
            {
                Destroy(generatedButtons[i].gameObject);
            }
        }

        generatedButtons.Clear();

        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        int count = Mathf.Min(videos.Count, MaxVisibleItems);
        for (int i = 0; i < count; i++)
        {
            VideoFile file = videos[i];
            VideoFile selectedFile = file;
            Button itemButton = CreateButton(listContent, "Video_" + i, BuildButtonText(selectedFile), Vector2.zero, new Vector2(360f, 44f), font);
            itemButton.onClick.AddListener(() => PlayVideo(selectedFile));

            LayoutElement layoutElement = itemButton.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 44f;
            layoutElement.preferredHeight = 44f;

            generatedButtons.Add(itemButton);
        }
    }

    private static string BuildButtonText(VideoFile file)
    {
        if (file == null)
        {
            return "Unknown File";
        }

        string name = string.IsNullOrWhiteSpace(file.name) ? "Unnamed Video" : file.name;
        if (name.Length > 36)
        {
            name = name.Substring(0, 33) + "...";
        }

        string size = FormatSize(file.size);
        return name + "    " + size;
    }

    private void PlayVideo(VideoFile file)
    {
        if (file == null || videoPlayer == null)
        {
            return;
        }

        string path = file.localPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = file.path;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            statusText.text = "Play failed: invalid path";
            return;
        }

        videoPlayer.PlayVideo(path);
        statusText.text = "Preparing: " + file.name;
    }

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 B";
        }

        double value = bytes;
        string[] units = { "B", "KB", "MB", "GB" };
        int unitIndex = 0;

        while (value >= 1024d && unitIndex < units.Length - 1)
        {
            value /= 1024d;
            unitIndex++;
        }

        return value.ToString("F1") + " " + units[unitIndex];
    }

    private static Text CreateLabel(Transform parent, string name, string textValue, Vector2 anchoredPosition, Vector2 size, Font font, int fontSize, TextAnchor anchor)
    {
        GameObject labelObject = new GameObject(name);
        labelObject.transform.SetParent(parent, false);

        Text text = labelObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = Color.white;
        text.text = textValue;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, Font font)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.16f, 0.16f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);

        Text text = labelObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = 15;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = label;

        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(6f, 3f);
        textRect.offsetMax = new Vector2(-6f, -3f);

        return button;
    }
}


