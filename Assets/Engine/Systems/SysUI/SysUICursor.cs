// SysUICursor.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SysUICursor : MonoBehaviour
{
    // ----- ----- References ----- ----- //

    public static SysUICursor Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private Image cursorImage;
    private RectTransform cursorRect => cursorImage.rectTransform;

    // ----- ----- Configuration ----- ----- //

    [Header("Cursor Definitions")]
    [SerializeField] private CursorDefinition[] cursorDefinitions;

    [Header("Animation")]
    [Tooltip("How fast to interpolate cursor size (units/sec)")]
    [SerializeField] private float resizeSpeed = 500f;

    // ----- ----- Data ----- ----- //

    // lookup by name
    private readonly Dictionary<string, CursorDefinition> defsByName = new Dictionary<string, CursorDefinition>();
    // track last time each cursor was requested
    private readonly Dictionary<string, float> lastRequestTime = new Dictionary<string, float>();

    private CursorDefinition currentDef;
    private float defaultSize, holdSize, currentSize;

    private string defaultName;

    // ------ ----- Lifecycle ----- ----- //

    public void Init()
    {
        InitSingleton();
        ValidateReferences();
        BuildDefinitionLookup();

        if (cursorDefinitions.Length > 0)
            defaultName = cursorDefinitions[0].name;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;

        // start with default
        ApplyCursorDefinition(cursorDefinitions[0]);
    }

    private void InitSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void ValidateReferences()
    {
        if (cursorDefinitions == null || cursorDefinitions.Length == 0)
            Debug.LogError("[SysUICursor] No cursorDefinitions configured!");
        if (uiCanvas == null)
            Debug.LogError("[SysUICursor] uiCanvas missing!");
        if (cursorImage == null)
            Debug.LogError("[SysUICursor] cursorImage missing!");
        else
            cursorImage.raycastTarget = false;
    }

    private void BuildDefinitionLookup()
    {
        defsByName.Clear();
        lastRequestTime.Clear();
        for (int i = 0; i < cursorDefinitions.Length; i++)
        {
            var def = cursorDefinitions[i];
            if (string.IsNullOrEmpty(def.name))
            {
                Debug.LogWarning($"[SysUICursor] cursorDefinitions[{i}] has no name!");
                continue;
            }
            if (defsByName.ContainsKey(def.name))
            {
                Debug.LogWarning($"[SysUICursor] duplicate cursor name '{def.name}' at index {i}");
                continue;
            }
            defsByName.Add(def.name, def);
            lastRequestTime[def.name] = -Mathf.Infinity;
        }
    }

    /// <summary>
    /// Called by any system that wants this named cursor.
    /// Must be called again within bufferTime to keep it active.
    /// </summary>
    public void RequestCursor(string name)
    {
        if (!defsByName.ContainsKey(name))
        {
            Debug.LogError($"[SysUICursor] RequestCursor: no cursor named '{name}'");
            return;
        }
        lastRequestTime[name] = Time.unscaledTime;
    }

    /// <summary>
    /// Clears all requests immediately — you’ll fall back to default.
    /// </summary>
    public void ClearAllRequests()
    {
        var keys = new List<string>(lastRequestTime.Keys);
        foreach (var k in keys) lastRequestTime[k] = -Mathf.Infinity;
    }

    /// <summary>
    /// Call every frame to follow the mouse & pick the highest-priority active cursor.
    /// </summary>
    public void Tick()
    {
        // 1) follow the pointer
        cursorRect.anchoredPosition = Input.mousePosition;

        // 2) choose which definition is still “active”
        float now = Time.unscaledTime;
        CursorDefinition best = defsByName[defaultName];
        foreach (var kv in defsByName)
        {
            var def = kv.Value;
            // skip default in this pass
            if (def.name == defaultName) continue;

            // if requested recently enough…
            if (now - lastRequestTime[def.name] <= def.bufferTime)
            {
                // prefer higher priority
                if (def.priority > best.priority)
                    best = def;
            }
        }

        // 3) if it changed, swap sprites / sizes
        if (currentDef.name != best.name)
            ApplyCursorDefinition(best);

        // 4) handle pressed/held resizing exactly as before:
        // pick target size
        float targetSize = (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2)) ? holdSize : defaultSize;
        currentSize = Mathf.MoveTowards(
            currentSize,
            targetSize,
            resizeSpeed * Time.unscaledDeltaTime
        );
        float scale = currentSize / 100f;
        cursorRect.sizeDelta = new Vector2(
            currentDef.texture.width * scale,
            currentDef.texture.height * scale
        );
    }

    private void ApplyCursorDefinition(CursorDefinition def)
    {
        currentDef  = def;
        defaultSize = def.size;
        holdSize    = def.pressedSize;
        currentSize = defaultSize;

        if (def.texture != null)
        {
            var sprite = Sprite.Create(
                def.texture,
                new Rect(0, 0, def.texture.width, def.texture.height),
                new Vector2(0, 1),
                100f
            );
            cursorImage.sprite = sprite;
        }
        cursorImage.color = def.color;

        float scale = currentSize / 100f;
        cursorRect.sizeDelta = new Vector2(
            def.texture.width * scale,
            def.texture.height * scale
        );
    }
}

[Serializable]
public struct CursorDefinition
{
    [Tooltip("Unique identifier for RequestCursor(name)")]
    public string name;

    [Tooltip("Sprite source")]
    public Texture2D texture;

    [Tooltip("Default size in pixels")]
    public float size;

    [Tooltip("Size when mouse-down")]
    public float pressedSize;

    [Tooltip("Higher = will override lower priority requests")]
    public int priority;

    [Tooltip("Seconds to keep this request active after last RequestCursor()")]
    public float bufferTime;

    [Tooltip("Tint")]
    public Color color;
}
