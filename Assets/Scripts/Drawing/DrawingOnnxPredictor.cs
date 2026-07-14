using System;
using System.IO;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Runs the drawing classifier directly in the game with Sentis and dispatches summon logic by label.
/// </summary>
public sealed class DrawingOnnxPredictor : MonoBehaviour
{
    [Serializable]
    private sealed class ClassNamesFile
    {
        public string[] class_names;
    }

    [Header("Model")]
    [SerializeField] private ModelAsset modelAsset;
    [SerializeField] private TextAsset classNamesJson;
    [SerializeField] private BackendType backendType = BackendType.CPU;
    [SerializeField, Min(8)] private int inputSize = 64;

    [Header("Preprocess")]
    [SerializeField, Range(0f, 255f)] private float cropThreshold = 240f;
    [SerializeField, Min(0)] private int cropMargin = 4;
    [SerializeField, Range(0.1f, 1f)] private float normalizedMaxSize = 0.85f;

    [Header("Decision")]
    [SerializeField, Range(0f, 1f)] private float minimumConfidence = 0.6f;
    [SerializeField] private bool logResult = true;
    [SerializeField] private bool logScores = true;

    [Header("Debug")]
    [SerializeField] private bool savePreprocessedInput;
    [SerializeField] private string debugFolderName = "DrawingOnnxDebug";

    [Header("Events")]
    [SerializeField] private UnityEvent onCircle;
    [SerializeField] private UnityEvent onTriangle;
    [SerializeField] private UnityEvent onUnknown;

    private Worker worker;
    private string[] classNames;

    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        worker?.Dispose();
        worker = null;
    }

    public void Predict(Texture2D submittedTexture, int inkUsage)
    {
        Predict(submittedTexture, inkUsage, Mathf.FloorToInt(Mathf.Max(0, inkUsage) * 2f / 100f));
    }

    public void Predict(Texture2D submittedTexture, int inkUsage, int energyCost)
    {
        if (submittedTexture == null || inkUsage <= 0)
        {
            HandleLabel("unknown", 0f, energyCost);
            return;
        }

        if (!Initialize())
        {
            HandleLabel("unknown", 0f, energyCost);
            return;
        }

        float[] input = BuildInput(submittedTexture);
        SaveDebugInputIfNeeded(input);
        using (Tensor<float> inputTensor = new Tensor<float>(new TensorShape(1, 1, inputSize, inputSize), input))
        {
            worker.Schedule(inputTensor);
            Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;
            if (outputTensor == null)
            {
                Debug.LogWarning("Drawing ONNX prediction failed: output tensor is not float.");
                HandleLabel("unknown", 0f, energyCost);
                return;
            }

            using (Tensor<float> readableOutput = outputTensor.ReadbackAndClone())
            {
                ReadOnlySpan<float> logits = readableOutput.AsReadOnlySpan();
                HandleScores(logits, energyCost);
            }
        }
    }

    private bool Initialize()
    {
        if (worker != null && classNames != null && classNames.Length > 0)
        {
            return true;
        }

        if (modelAsset == null)
        {
            Debug.LogWarning("Drawing ONNX prediction failed: model asset is missing.");
            return false;
        }

        classNames = LoadClassNames();
        if (classNames == null || classNames.Length == 0)
        {
            Debug.LogWarning("Drawing ONNX prediction failed: class names are missing.");
            return false;
        }

        Model model = ModelLoader.Load(modelAsset);
        worker = new Worker(model, backendType);
        return true;
    }

    private string[] LoadClassNames()
    {
        if (classNamesJson == null || string.IsNullOrWhiteSpace(classNamesJson.text))
        {
            return null;
        }

        ClassNamesFile parsed = JsonUtility.FromJson<ClassNamesFile>(classNamesJson.text);
        return parsed != null ? parsed.class_names : null;
    }

    private void HandleScores(ReadOnlySpan<float> logits, int energyCost)
    {
        int classCount = Mathf.Min(logits.Length, classNames.Length);
        if (classCount <= 0)
        {
            HandleLabel("unknown", 0f, energyCost);
            return;
        }

        float maxLogit = logits[0];
        for (int i = 1; i < classCount; i++)
        {
            maxLogit = Mathf.Max(maxLogit, logits[i]);
        }

        float sum = 0f;
        float bestRawProbability = 0f;
        int bestIndex = 0;
        float[] probabilities = new float[classCount];
        for (int i = 0; i < classCount; i++)
        {
            float probability = Mathf.Exp(logits[i] - maxLogit);
            probabilities[i] = probability;
            sum += probability;
            if (probability > bestRawProbability)
            {
                bestRawProbability = probability;
                bestIndex = i;
            }
        }

        float bestConfidence = 0f;
        if (sum > 0f)
        {
            for (int i = 0; i < classCount; i++)
            {
                probabilities[i] /= sum;
            }

            bestConfidence = probabilities[bestIndex];
        }

        if (logScores)
        {
            string scoreText = string.Empty;
            for (int i = 0; i < classCount; i++)
            {
                scoreText += $"{classNames[i]}={probabilities[i]:0.000}";
                if (i + 1 < classCount)
                {
                    scoreText += ", ";
                }
            }

            Debug.Log($"Drawing ONNX scores: {scoreText}");
        }

        string label = bestConfidence >= minimumConfidence ? classNames[bestIndex] : "unknown";
        HandleLabel(label, bestConfidence, energyCost);
    }

    private float[] BuildInput(Texture2D source)
    {
        Color32[] pixels = source.GetPixels32();
        int sourceWidth = source.width;
        int sourceHeight = source.height;

        FindInkBounds(pixels, sourceWidth, sourceHeight, out int xMin, out int xMax, out int yMin, out int yMax);

        int cropWidth = Mathf.Max(1, xMax - xMin + 1);
        int cropHeight = Mathf.Max(1, yMax - yMin + 1);
        float scale = inputSize * normalizedMaxSize / Mathf.Max(cropWidth, cropHeight);
        int resizedWidth = scale >= 1f ? cropWidth : Mathf.Max(1, Mathf.FloorToInt(cropWidth * scale));
        int resizedHeight = scale >= 1f ? cropHeight : Mathf.Max(1, Mathf.FloorToInt(cropHeight * scale));

        float[] output = new float[inputSize * inputSize];
        int xOffset = (inputSize - resizedWidth) / 2;
        int yOffset = (inputSize - resizedHeight) / 2;

        for (int y = 0; y < resizedHeight; y++)
        {
            for (int x = 0; x < resizedWidth; x++)
            {
                float sourceX = xMin + (x + 0.5f) * cropWidth / resizedWidth - 0.5f;
                // Python/PIL uses row 0 as the top; Unity textures use y=0 as the bottom.
                float sourceY = yMax - (y + 0.5f) * cropHeight / resizedHeight + 0.5f;
                float gray = SampleGray(pixels, sourceWidth, sourceHeight, sourceX, sourceY);
                output[(yOffset + y) * inputSize + xOffset + x] = 1f - gray / 255f;
            }
        }

        return output;
    }

    private void SaveDebugInputIfNeeded(float[] input)
    {
        if (!savePreprocessedInput || input == null || input.Length != inputSize * inputSize)
        {
            return;
        }

        Texture2D debugTexture = new Texture2D(inputSize, inputSize, TextureFormat.RGBA32, false);
        Color32[] colors = new Color32[input.Length];
        for (int row = 0; row < inputSize; row++)
        {
            for (int x = 0; x < inputSize; x++)
            {
                int inputIndex = row * inputSize + x;
                byte value = (byte)Mathf.Clamp(Mathf.RoundToInt((1f - input[inputIndex]) * 255f), 0, 255);
                int textureY = inputSize - 1 - row;
                colors[textureY * inputSize + x] = new Color32(value, value, value, 255);
            }
        }

        debugTexture.SetPixels32(colors);
        debugTexture.Apply();

        string folder = Path.Combine(Application.persistentDataPath, debugFolderName);
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, $"onnx_input_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        File.WriteAllBytes(path, debugTexture.EncodeToPNG());
        Destroy(debugTexture);
        Debug.Log($"Drawing ONNX debug input saved: {path}");
    }

    private void FindInkBounds(Color32[] pixels, int width, int height, out int xMin, out int xMax, out int yMin, out int yMax)
    {
        xMin = width - 1;
        xMax = 0;
        yMin = height - 1;
        yMax = 0;
        bool hasInk = false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color32 pixel = pixels[y * width + x];
                float gray = CompositeOnWhiteGray(pixel);
                if (gray >= cropThreshold)
                {
                    continue;
                }

                hasInk = true;
                xMin = Mathf.Min(xMin, x);
                xMax = Mathf.Max(xMax, x);
                yMin = Mathf.Min(yMin, y);
                yMax = Mathf.Max(yMax, y);
            }
        }

        if (!hasInk)
        {
            xMin = 0;
            xMax = width - 1;
            yMin = 0;
            yMax = height - 1;
            return;
        }

        xMin = Mathf.Max(0, xMin - cropMargin);
        xMax = Mathf.Min(width - 1, xMax + cropMargin);
        yMin = Mathf.Max(0, yMin - cropMargin);
        yMax = Mathf.Min(height - 1, yMax + cropMargin);
    }

    private static float SampleGray(Color32[] pixels, int width, int height, float x, float y)
    {
        int x0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, width - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(y), 0, height - 1);
        int x1 = Mathf.Clamp(x0 + 1, 0, width - 1);
        int y1 = Mathf.Clamp(y0 + 1, 0, height - 1);
        float tx = Mathf.Clamp01(x - x0);
        float ty = Mathf.Clamp01(y - y0);

        float a = CompositeOnWhiteGray(pixels[y0 * width + x0]);
        float b = CompositeOnWhiteGray(pixels[y0 * width + x1]);
        float c = CompositeOnWhiteGray(pixels[y1 * width + x0]);
        float d = CompositeOnWhiteGray(pixels[y1 * width + x1]);
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
    }

    private static float CompositeOnWhiteGray(Color32 pixel)
    {
        float alpha = pixel.a / 255f;
        float r = pixel.r * alpha + 255f * (1f - alpha);
        float g = pixel.g * alpha + 255f * (1f - alpha);
        float b = pixel.b * alpha + 255f * (1f - alpha);
        return 0.299f * r + 0.587f * g + 0.114f * b;
    }

    private void HandleLabel(string label, float confidence, int energyCost)
    {
        string normalizedLabel = string.IsNullOrWhiteSpace(label) ? "unknown" : label.Trim().ToLowerInvariant();

        if (logResult)
        {
            Debug.Log($"Drawing prediction: {normalizedLabel} ({confidence:0.000})");
        }

        switch (normalizedLabel)
        {
            case "circle":
                TryInvokeSummon(PlayerSummonSpawner.SummonKind.Melee, energyCost);
                onCircle?.Invoke();
                break;
            case "triangle":
                TryInvokeSummon(PlayerSummonSpawner.SummonKind.Ranged, energyCost);
                onTriangle?.Invoke();
                break;
            case "firetriangle":
                TryInvokeSummon(PlayerSummonSpawner.SummonKind.Firemage, energyCost);
                break;
            case "strongcircle":
                TryInvokeSummon(PlayerSummonSpawner.SummonKind.Berserker, energyCost);
                break;
            default:
                Debug.Log("Invalid or unrecognized drawing.");
                onUnknown?.Invoke();
                break;
        }
    }

    private void TryInvokeSummon(PlayerSummonSpawner.SummonKind kind, int energyCost)
    {
        GameObject player = GameObject.Find("Player");
        PlayerSummonSpawner spawner = player != null ? player.GetComponent<PlayerSummonSpawner>() : FindObjectOfType<PlayerSummonSpawner>(true);
        if (spawner == null)
        {
            Debug.LogWarning("Drawing prediction found no PlayerSummonSpawner in the current level.");
            return;
        }

        if (!spawner.TrySpawnSummonWithEnergy(kind, energyCost, true, out float multiplier, out int effectiveEnergyCost))
        {
            return;
        }

        Debug.Log($"Summon created with energy {effectiveEnergyCost}, multiplier {multiplier:0.00}.");
    }
}
