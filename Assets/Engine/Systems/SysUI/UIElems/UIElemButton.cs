using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIElemButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Button button;
    public Text label;
    private Image image;

    private ElemButtonConfig config;
    private RectTransform RectTransform => (RectTransform)transform;

    private enum ButtonVisualState { Normal, Hover, Pressed }
    private ButtonVisualState currentState = ButtonVisualState.Normal;

    public static UIElemButton CreateButton(string content, ElemButtonConfig config)
    {
        GameObject buttonObj = new GameObject("UIElemButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Button), typeof(Image));
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));

        textObj.transform.SetParent(buttonObj.transform, false);

        // Setup button RectTransform
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.sizeDelta = config.size;
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);

        // Setup text RectTransform
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textRect.pivot = new Vector2(0.5f, 0.5f);

        // Image
        Image image = buttonObj.GetComponent<Image>();
        image.color = config.backgroundColor;
        if (config.backgroundSprite != null)
        {
            image.sprite = config.backgroundSprite;
            image.type = config.spriteMode == ElemButtonConfig.SpriteMode.Sliced ? Image.Type.Sliced : Image.Type.Simple;
        }

        // Button
        Button btn = buttonObj.GetComponent<Button>();

        // Text
        Text txt = textObj.GetComponent<Text>();
        txt.text = content;
        txt.font = config.font;
        txt.fontSize = config.fontSize;
        txt.color = config.textColor;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.raycastTarget = false;

        // Script
        UIElemButton elem = buttonObj.AddComponent<UIElemButton>();
        elem.button = btn;
        elem.label = txt;
        elem.image = image;
        elem.config = config;

        elem.ApplyVisualState(ButtonVisualState.Normal);

        return elem;
    }

    public void AddClickListener(UnityAction callback)
    {
        button.onClick.AddListener(callback);
    }

    public void SetPosition(Vector2 position)
    {
        RectTransform.anchoredPosition = position;
    }

    public void AlignCenter() => Align(Vector2.one * 0.5f);
    public void AlignTopLeft() => Align(new Vector2(0f, 1f));
    public void AlignBottomLeft() => Align(new Vector2(0f, 0f));
    public void AlignTopCenter() => Align(new Vector2(0.5f, 1f));
    public void AlignBottomCenter() => Align(new Vector2(0.5f, 0f));

    private void Align(Vector2 anchorPivot)
    {
        var rt = RectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchorPivot;
    }

    private void ApplyVisualState(ButtonVisualState state)
    {
        currentState = state;

        switch (state)
        {
            case ButtonVisualState.Normal:
                label.fontSize = config.fontSize;
                label.color = config.textColor;
                image.color = config.backgroundColor;
                if (config.backgroundSprite != null)
                    image.sprite = config.backgroundSprite;
                break;

            case ButtonVisualState.Hover:
                label.fontSize = config.hoverFontSize;
                label.color = config.hoverTextColor;
                image.color = config.hoverBackgroundColor;
                if (config.hoverBackgroundSprite != null)
                    image.sprite = config.hoverBackgroundSprite;
                break;

            case ButtonVisualState.Pressed:
                label.fontSize = config.pressedFontSize;
                label.color = config.pressedTextColor;
                image.color = config.pressedBackgroundColor;
                if (config.pressedBackgroundSprite != null)
                    image.sprite = config.pressedBackgroundSprite;
                break;
        }

        if (image.sprite != null)
            image.type = config.spriteMode == ElemButtonConfig.SpriteMode.Sliced ? Image.Type.Sliced : Image.Type.Simple;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentState != ButtonVisualState.Pressed)
            ApplyVisualState(ButtonVisualState.Hover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentState != ButtonVisualState.Pressed)
            ApplyVisualState(ButtonVisualState.Normal);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ApplyVisualState(ButtonVisualState.Pressed);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ApplyVisualState(ButtonVisualState.Hover);
    }
}

[System.Serializable]
public class ElemButtonConfig
{
    public enum SpriteMode
    {
        Simple,
        Sliced
    }

    public Font font;
    public int fontSize = 18;
    public Color textColor = Color.white;
    public Color backgroundColor = Color.gray;
    public Sprite backgroundSprite = null;
    public Vector2 size = new Vector2(160, 30);

    public int hoverFontSize = 20;
    public Color hoverTextColor = Color.yellow;
    public Color hoverBackgroundColor = Color.white;
    public Sprite hoverBackgroundSprite = null;

    public int pressedFontSize = 18;
    public Color pressedTextColor = Color.red;
    public Color pressedBackgroundColor = Color.black;
    public Sprite pressedBackgroundSprite = null;

    public SpriteMode spriteMode = SpriteMode.Simple;
}

