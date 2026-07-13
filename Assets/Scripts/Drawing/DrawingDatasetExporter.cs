using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Saves submitted drawings into a configurable dataset label folder for later ML training.
/// </summary>
public sealed class DrawingDatasetExporter : MonoBehaviour
{
    [SerializeField] private bool exportOnSubmit = true;
    [SerializeField] private string datasetRoot = @"D:\小学期\图像识别模型\dataset";
    [SerializeField] private string targetLabel = "circle";
    [SerializeField] private string filePrefix = "drawing";
    [SerializeField] private bool logSavedPath = true;

    public string TargetLabel
    {
        get => targetLabel;
        set => targetLabel = SanitizeFolderName(value);
    }

    public void Export(Texture2D sourceTexture, int inkUsage)
    {
        if (!exportOnSubmit || sourceTexture == null || inkUsage <= 0)
        {
            return;
        }

        string label = SanitizeFolderName(targetLabel);
        if (string.IsNullOrWhiteSpace(label))
        {
            return;
        }

        string labelFolder = Path.Combine(datasetRoot, label);
        Directory.CreateDirectory(labelFolder);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string fileName = $"{SanitizeFileName(filePrefix)}_{label}_{timestamp}.png";
        string filePath = Path.Combine(labelFolder, fileName);

        byte[] pngBytes = sourceTexture.EncodeToPNG();
        File.WriteAllBytes(filePath, pngBytes);

        if (logSavedPath)
        {
            Debug.Log($"Drawing sample saved: {filePath}");
        }
    }

    private string SanitizeFolderName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string result = value.Trim();
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            result = result.Replace(invalid.ToString(), string.Empty);
        }

        return result;
    }

    private string SanitizeFileName(string value)
    {
        string result = SanitizeFolderName(value);
        return string.IsNullOrWhiteSpace(result) ? "drawing" : result;
    }
}
