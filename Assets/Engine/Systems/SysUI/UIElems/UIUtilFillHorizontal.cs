using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UIUtilFillHorizontal : MonoBehaviour
{
    [SerializeField] private UIUtilFillOptional[] elements = new UIUtilFillOptional[0];
    [SerializeField] private float spacing = 0f;
    [SerializeField] private bool controlHeight = false; // ✅ new option

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        UpdateFill();
    }

    private void Update()
    {
        UpdateFill();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UpdateFill();
    }

    [InitializeOnLoadMethod]
    private static void HookEditorUpdate()
    {
        EditorApplication.update += () =>
        {
            if (Application.isPlaying) return;
            foreach (var comp in Object.FindObjectsByType<UIUtilFillHorizontal>(FindObjectsSortMode.None))
            {
                if (comp != null) comp.UpdateFill();
            }
        };
    }
#endif

    public void UpdateFill()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (elements == null || elements.Length == 0)
            return;

        // Parent dimensions & pivot
        float parentWidth  = rectTransform.rect.width;
        float parentHeight = rectTransform.rect.height;
        float parentPivotX = rectTransform.pivot.x;
        float parentPivotY = rectTransform.pivot.y;

        // Locked vs flexible
        float lockedTotal = 0f;
        int flexCount = 0;
        int activeCount = 0;

        foreach (var e in elements)
        {
            if (e == null || e.element == null) continue;
            activeCount++;
            if (e.lockedWidth) lockedTotal += e.widthSize;
            else flexCount++;
        }

        float totalSpacing = Mathf.Max(0, activeCount - 1) * spacing;
        float available = Mathf.Max(0, parentWidth - lockedTotal - totalSpacing);
        float flexWidth = flexCount > 0 ? available / flexCount : 0f;

        // Left edge of parent local rect
        float startX = -parentWidth * parentPivotX;
        // Vertical center relative to parent pivot
        float yPos = -parentHeight * parentPivotY + parentHeight * 0.5f;

        float cursorX = startX;

        foreach (var e in elements)
        {
            if (e == null || e.element == null) continue;
            RectTransform rt = e.element as RectTransform;
            if (rt == null) continue;

            float width = e.lockedWidth ? e.widthSize : flexWidth;

            // Force horizontal anchors only
            rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
            rt.anchorMax = new Vector2(0f, rt.anchorMax.y);
            rt.pivot     = new Vector2(0f, rt.pivot.y);

            // Width always controlled
            Vector2 size = rt.sizeDelta;
            size.x = width;
            if (controlHeight)
                size.y = parentHeight;
            rt.sizeDelta = size;

            // Position
            Vector3 pos = rt.localPosition;
            pos.x = cursorX;
            if (controlHeight)
                pos.y = yPos; // keep centered vertically if controlling height
            rt.localPosition = pos;

            cursorX += width + spacing;
        }
    }
}

[System.Serializable]
public class UIUtilFillOptional
{
    public Transform element;
    public bool lockedWidth;
    public int widthSize;
}
