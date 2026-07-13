using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles pointer drawing inside a fixed UI painting range and exposes a submit entry for later recognition.
/// </summary>
public class DrawingCanvasPainter : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private const int MaximumEffectiveEnergyCost = 200;

    [Header("Drawing Area")]
    [SerializeField] private RectTransform paintingRange;
    [SerializeField] private RawImage drawingImage;
    private int textureWidth;
    private int textureHeight;

    [Header("Brush")]
    [SerializeField] private Color brushColor = Color.black;
    [SerializeField, Min(1)] private int brushRadius = 4;

    [Header("Ink Usage")]
    [SerializeField] private Text usageText;
    [SerializeField] private TMP_Text usageTmpText;
    [SerializeField] private DrawingTemplateRecognizer templateRecognizer;
    [SerializeField] private DrawingDatasetExporter datasetExporter;
    [SerializeField] private DrawingHttpPredictor httpPredictor;

    private Texture2D drawingTexture;
    private Color clearColor;
    private bool isDrawing;
    private bool hasLastPixel;
    private Vector2Int lastPixel;
    private int usedPixelCount;
    private int lastSubmittedInkUsage;
    private int lastSubmittedEnergyCost;
    private Texture2D lastSubmittedTexture;

    public int UsedPixelCount => usedPixelCount;
    public int LastSubmittedInkUsage => lastSubmittedInkUsage;
    public int LastSubmittedEnergyCost => lastSubmittedEnergyCost;
    public Texture2D DrawingTexture => drawingTexture;
    public Texture2D LastSubmittedTexture => lastSubmittedTexture;

    private void Awake()
    {
        EnsureTexture();
        UpdateUsageText();
    }

    private void OnEnable()
    {
        EnsureTexture();
        isDrawing = false;
        hasLastPixel = false;
        UpdateUsageText();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (!TryGetPixel(eventData, out Vector2Int pixel))
        {
            return;
        }

        isDrawing = true;
        hasLastPixel = true;
        lastPixel = pixel;
        PaintAt(pixel);
        ApplyTexture();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawing)
        {
            return;
        }

        if (!TryGetPixel(eventData, out Vector2Int pixel))
        {
            hasLastPixel = false;
            return;
        }

        if (hasLastPixel)
        {
            DrawLine(lastPixel, pixel);
        }
        else
        {
            PaintAt(pixel);
        }

        lastPixel = pixel;
        hasLastPixel = true;
        ApplyTexture();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        isDrawing = false;
        hasLastPixel = false;
    }

    public void ClearDrawing()
    {
        EnsureTexture();
        FillTexture(clearColor);
        usedPixelCount = 0;
        ApplyTexture();
    }

public void SubmitDrawing()
    {
        EnsureTexture();

        CacheSubmittedDrawing();

        if (datasetExporter != null)
        {
            datasetExporter.Export(lastSubmittedTexture, lastSubmittedInkUsage);
        }

        if (httpPredictor != null)
        {
            httpPredictor.Predict(lastSubmittedTexture, lastSubmittedInkUsage, lastSubmittedEnergyCost);
        }
        else if (templateRecognizer != null)
        {
            templateRecognizer.Recognize(lastSubmittedTexture, lastSubmittedInkUsage);
        }
        else
        {
            Debug.Log("识别失败");
        }

        ClearDrawing();
    }

private void CacheSubmittedDrawing()
    {
        lastSubmittedInkUsage = usedPixelCount;
        lastSubmittedEnergyCost = CalculateEffectiveEnergyCost(usedPixelCount);

        if (lastSubmittedTexture == null || lastSubmittedTexture.width != textureWidth || lastSubmittedTexture.height != textureHeight)
        {
            lastSubmittedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            lastSubmittedTexture.wrapMode = TextureWrapMode.Clamp;
            lastSubmittedTexture.filterMode = FilterMode.Point;
        }

        lastSubmittedTexture.SetPixels(drawingTexture.GetPixels());
        lastSubmittedTexture.Apply();
    }


private void EnsureTexture()
    {
        ResolveTextureSize();

        if (drawingTexture != null && drawingTexture.width == textureWidth && drawingTexture.height == textureHeight)
        {
            if (drawingImage != null && drawingImage.texture != drawingTexture)
            {
                drawingImage.texture = drawingTexture;
            }

            return;
        }

        clearColor = new Color(1f, 1f, 1f, 0f);
        drawingTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        drawingTexture.wrapMode = TextureWrapMode.Clamp;
        drawingTexture.filterMode = FilterMode.Point;
        usedPixelCount = 0;
        FillTexture(clearColor);

        if (drawingImage != null)
        {
            drawingImage.texture = drawingTexture;
            drawingImage.color = Color.white;
            drawingImage.raycastTarget = true;
        }
    }

private void ResolveTextureSize()
    {
        Rect sourceRect = paintingRange != null ? paintingRange.rect : Rect.zero;
        textureWidth = Mathf.Max(1, Mathf.RoundToInt(sourceRect.width));
        textureHeight = Mathf.Max(1, Mathf.RoundToInt(sourceRect.height));
    }


    private void FillTexture(Color color)
    {
        Color[] colors = new Color[textureWidth * textureHeight];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = color;
        }

        drawingTexture.SetPixels(colors);
        drawingTexture.Apply();
    }

    private bool TryGetPixel(PointerEventData eventData, out Vector2Int pixel)
    {
        pixel = default;

        if (paintingRange == null)
        {
            return false;
        }

        Camera eventCamera = eventData.pressEventCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(paintingRange, eventData.position, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = paintingRange.rect;
        if (!rect.Contains(localPoint))
        {
            return false;
        }

        float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float normalizedY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
        int x = Mathf.Clamp(Mathf.RoundToInt(normalizedX * (textureWidth - 1)), 0, textureWidth - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(normalizedY * (textureHeight - 1)), 0, textureHeight - 1);
        pixel = new Vector2Int(x, y);
        return true;
    }

    private void DrawLine(Vector2Int from, Vector2Int to)
    {
        int steps = Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.y - from.y));
        if (steps == 0)
        {
            PaintAt(to);
            return;
        }

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(from.y, to.y, t));
            PaintAt(new Vector2Int(x, y));
        }
    }

    private void PaintAt(Vector2Int center)
    {
        int radiusSqr = brushRadius * brushRadius;
        for (int y = -brushRadius; y <= brushRadius; y++)
        {
            for (int x = -brushRadius; x <= brushRadius; x++)
            {
                if (x * x + y * y > radiusSqr)
                {
                    continue;
                }

                int pixelX = center.x + x;
                int pixelY = center.y + y;
                if (pixelX < 0 || pixelX >= textureWidth || pixelY < 0 || pixelY >= textureHeight)
                {
                    continue;
                }

                Color existing = drawingTexture.GetPixel(pixelX, pixelY);
                if (existing.a <= 0.01f)
                {
                    usedPixelCount++;
                }

                drawingTexture.SetPixel(pixelX, pixelY, brushColor);
            }
        }
    }

    private void ApplyTexture()
    {
        drawingTexture.Apply();
        UpdateUsageText();
    }

private void UpdateUsageText()
    {
        string text = CalculateEffectiveEnergyCost(usedPixelCount).ToString();
        if (usageText != null)
        {
            usageText.text = text;
        }

        if (usageTmpText != null)
        {
            usageTmpText.text = text;
        }
    }

    private static int CalculateEnergyCost(int inkUsage)
    {
        return Mathf.FloorToInt(Mathf.Max(0, inkUsage) * 2f / 100f);
    }

    private static int CalculateEffectiveEnergyCost(int inkUsage)
    {
        return Mathf.Min(CalculateEnergyCost(inkUsage), MaximumEffectiveEnergyCost);
    }
}
