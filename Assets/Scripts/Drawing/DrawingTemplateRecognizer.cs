using System;
using UnityEngine;

/// <summary>
/// Recognizes submitted drawings by comparing them with multiple templates per shape type.
/// </summary>
public sealed class DrawingTemplateRecognizer : MonoBehaviour
{
    [Serializable]
    public sealed class TemplateGroup
    {
        public string shapeName;
        public string successMessage;
        [Range(0f, 1f)] public float threshold = 0.18f;
        public Texture2D[] templates;
    }

    [SerializeField] private TemplateGroup[] templateGroups;
    [SerializeField, Range(16, 128)] private int normalizedSize = 64;
    [SerializeField, Min(0)] private int dilationRadius = 1;
    [SerializeField] private bool logScores;

    public void Recognize(Texture2D submittedTexture, int inkUsage)
    {
        if (submittedTexture == null || inkUsage <= 0 || templateGroups == null || templateGroups.Length == 0)
        {
            Debug.Log("识别失败");
            return;
        }

        bool[] drawingMask = Dilate(BuildNormalizedMask(submittedTexture), dilationRadius);
        if (!HasAnyInk(drawingMask))
        {
            Debug.Log("识别失败");
            return;
        }

        TemplateGroup bestGroup = null;
        float bestScore = 0f;

        for (int i = 0; i < templateGroups.Length; i++)
        {
            TemplateGroup group = templateGroups[i];
            float groupBestScore = GetBestGroupScore(drawingMask, group);

            if (logScores && group != null)
            {
                Debug.Log($"{group.shapeName} score: {groupBestScore:0.000}");
            }

            if (group != null && groupBestScore > bestScore)
            {
                bestScore = groupBestScore;
                bestGroup = group;
            }
        }

        if (bestGroup != null && bestScore >= bestGroup.threshold)
        {
            Debug.Log(string.IsNullOrWhiteSpace(bestGroup.successMessage) ? bestGroup.shapeName : bestGroup.successMessage);
        }
        else
        {
            Debug.Log("识别失败");
        }
    }

    private float GetBestGroupScore(bool[] drawingMask, TemplateGroup group)
    {
        if (group == null || group.templates == null || group.templates.Length == 0)
        {
            return 0f;
        }

        float bestScore = 0f;
        for (int i = 0; i < group.templates.Length; i++)
        {
            Texture2D template = group.templates[i];
            if (template == null)
            {
                continue;
            }

            bool[] templateMask = Dilate(BuildNormalizedMask(template), dilationRadius);
            if (!HasAnyInk(templateMask))
            {
                continue;
            }

            float score = CalculateIntersectionOverUnion(drawingMask, templateMask);
            if (score > bestScore)
            {
                bestScore = score;
            }
        }

        return bestScore;
    }

    private bool[] BuildNormalizedMask(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        int width = texture.width;
        int height = texture.height;

        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!IsInk(pixels[y * width + x]))
                {
                    continue;
                }

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        bool[] mask = new bool[normalizedSize * normalizedSize];
        if (maxX < minX || maxY < minY)
        {
            return mask;
        }

        int sourceWidth = Mathf.Max(1, maxX - minX + 1);
        int sourceHeight = Mathf.Max(1, maxY - minY + 1);
        float scale = Mathf.Min((normalizedSize - 2f) / sourceWidth, (normalizedSize - 2f) / sourceHeight);
        int outputWidth = Mathf.Max(1, Mathf.RoundToInt(sourceWidth * scale));
        int outputHeight = Mathf.Max(1, Mathf.RoundToInt(sourceHeight * scale));
        int offsetX = (normalizedSize - outputWidth) / 2;
        int offsetY = (normalizedSize - outputHeight) / 2;

        for (int oy = 0; oy < outputHeight; oy++)
        {
            for (int ox = 0; ox < outputWidth; ox++)
            {
                float sourceX = minX + ((ox + 0.5f) / outputWidth) * sourceWidth;
                float sourceY = minY + ((oy + 0.5f) / outputHeight) * sourceHeight;
                int sx = Mathf.Clamp(Mathf.FloorToInt(sourceX), 0, width - 1);
                int sy = Mathf.Clamp(Mathf.FloorToInt(sourceY), 0, height - 1);

                if (IsInk(pixels[sy * width + sx]))
                {
                    int mx = offsetX + ox;
                    int my = offsetY + oy;
                    mask[my * normalizedSize + mx] = true;
                }
            }
        }

        return mask;
    }

    private bool IsInk(Color32 color)
    {
        if (color.a <= 20)
        {
            return false;
        }

        int brightness = (color.r + color.g + color.b) / 3;
        return brightness < 220;
    }

    private bool HasAnyInk(bool[] mask)
    {
        for (int i = 0; i < mask.Length; i++)
        {
            if (mask[i])
            {
                return true;
            }
        }

        return false;
    }

    private bool[] Dilate(bool[] source, int radius)
    {
        if (radius <= 0)
        {
            return source;
        }

        bool[] result = new bool[source.Length];
        for (int y = 0; y < normalizedSize; y++)
        {
            for (int x = 0; x < normalizedSize; x++)
            {
                if (!source[y * normalizedSize + x])
                {
                    continue;
                }

                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (dx * dx + dy * dy > radius * radius)
                        {
                            continue;
                        }

                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx >= 0 && nx < normalizedSize && ny >= 0 && ny < normalizedSize)
                        {
                            result[ny * normalizedSize + nx] = true;
                        }
                    }
                }
            }
        }

        return result;
    }

    private float CalculateIntersectionOverUnion(bool[] first, bool[] second)
    {
        int intersection = 0;
        int union = 0;

        for (int i = 0; i < first.Length; i++)
        {
            bool a = first[i];
            bool b = second[i];
            if (a && b)
            {
                intersection++;
            }

            if (a || b)
            {
                union++;
            }
        }

        return union == 0 ? 0f : intersection / (float)union;
    }
}
