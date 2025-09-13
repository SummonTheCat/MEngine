using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UIUtilFitChildren : MonoBehaviour
{
    [SerializeField] private bool resizeWidth = true;
    [SerializeField] private bool resizeHeight = true;
    [SerializeField] private float padding = 0f; // optional extra space

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        UpdateFit();
    }

    private void Update()
    {
        // Runtime refresh (like Unity’s ContentSizeFitter)
        UpdateFit();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UpdateFit();
    }

    [InitializeOnLoadMethod]
    private static void HookEditorUpdate()
    {
        EditorApplication.update += () =>
        {
            if (Application.isPlaying) return;
            foreach (var comp in Object.FindObjectsByType<UIUtilFitChildren>(FindObjectsSortMode.None))
            {
                if (comp != null) comp.UpdateFit();
            }
        };
    }
#endif

    public void UpdateFit()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (transform.childCount == 0) return;

        // Initialize bounds
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, 0f);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, 0f);

        // Include each child’s rect in local space
        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf) continue;

            Vector3[] corners = new Vector3[4];
            child.GetLocalCorners(corners);
            Vector3 childPos = child.localPosition;

            for (int c = 0; c < 4; c++)
            {
                Vector3 world = childPos + corners[c];
                min = Vector3.Min(min, world);
                max = Vector3.Max(max, world);
            }
        }

        // Current size and pivot
        Vector2 sizeDelta = rectTransform.sizeDelta;
        Vector2 currentSize = rectTransform.rect.size;
        Vector2 pivot = rectTransform.pivot;

        if (resizeWidth)
        {
            float neededWidth = (max.x - min.x) + padding * 2f;
            sizeDelta.x += neededWidth - currentSize.x;
        }

        if (resizeHeight)
        {
            float neededHeight = (max.y - min.y) + padding * 2f;
            sizeDelta.y += neededHeight - currentSize.y;
        }

        rectTransform.sizeDelta = sizeDelta;
    }
}
