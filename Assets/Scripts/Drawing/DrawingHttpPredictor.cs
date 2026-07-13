using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

/// <summary>
/// Sends a submitted drawing PNG to a local ML prediction service and dispatches game logic by returned label.
/// </summary>
public sealed class DrawingHttpPredictor : MonoBehaviour
{
    private const int BaseSummonEnergyCost = 100;
    private const float MinimumSummonMultiplier = 0.5f;
    private const float MaximumSummonMultiplier = 2f;

    [Serializable]
    private sealed class PredictResponse
    {
        public string label;
        public float confidence;
    }

    [Header("HTTP")]
    [SerializeField] private string predictUrl = "http://127.0.0.1:5000/predict";
    [SerializeField] private string fileFieldName = "file";
    [SerializeField, Min(1f)] private float timeoutSeconds = 10f;

    [Header("Decision")]
    [SerializeField, Range(0f, 1f)] private float minimumConfidence = 0.6f;
    [SerializeField] private bool logResult = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onCircle;
    [SerializeField] private UnityEvent onTriangle;
    [SerializeField] private UnityEvent onUnknown;

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

        byte[] pngBytes = submittedTexture.EncodeToPNG();
        StartCoroutine(PredictRoutine(pngBytes, energyCost));
    }

    private IEnumerator PredictRoutine(byte[] pngBytes, int energyCost)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection(fileFieldName, pngBytes, "drawing.png", "image/png")
        };

        using (UnityWebRequest request = UnityWebRequest.Post(predictUrl, formData))
        {
            request.timeout = Mathf.CeilToInt(timeoutSeconds);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Drawing prediction failed: {request.error}");
                HandleLabel("unknown", 0f, energyCost);
                yield break;
            }

            PredictResponse response = JsonUtility.FromJson<PredictResponse>(request.downloadHandler.text);
            if (response == null || string.IsNullOrWhiteSpace(response.label))
            {
                Debug.LogWarning($"Drawing prediction returned invalid JSON: {request.downloadHandler.text}");
                HandleLabel("unknown", 0f, energyCost);
                yield break;
            }

            string label = response.confidence >= minimumConfidence ? response.label : "unknown";
            HandleLabel(label, response.confidence, energyCost);
        }
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
                Debug.Log("圆的逻辑");
                if (HasValidPersistentTarget(onCircle))
                {
                    onCircle.Invoke();
                }
                else
                {
                    TryInvokeSummon(false, energyCost);
                }
                break;
            case "triangle":
                Debug.Log("三角的逻辑");
                if (HasValidPersistentTarget(onTriangle))
                {
                    onTriangle.Invoke();
                }
                else
                {
                    TryInvokeSummon(true, energyCost);
                }
                break;
            default:
                Debug.Log("无效/无法识别的逻辑");
                onUnknown?.Invoke();
                break;
        }
    }

private void TryInvokeSummon(bool ranged, int energyCost)
    {
        if (!TryCalculateSummonMultiplier(energyCost, out float multiplier, out int effectiveEnergyCost))
        {
            return;
        }

        GameObject player = GameObject.Find("Player");
        PlayerSummonSpawner spawner = player != null ? player.GetComponent<PlayerSummonSpawner>() : FindObjectOfType<PlayerSummonSpawner>(true);
        if (spawner == null)
        {
            Debug.LogWarning("Drawing prediction found no PlayerSummonSpawner in the current level.");
            return;
        }

        if (!spawner.CanSpawnSummon(ranged))
        {
            Debug.Log("Summon failed: summon limit reached or prefab missing.");
            return;
        }

        PlayerEnergy playerEnergy = player != null ? player.GetComponent<PlayerEnergy>() : FindObjectOfType<PlayerEnergy>(true);
        if (playerEnergy == null)
        {
            Debug.LogWarning("Summon failed: PlayerEnergy not found.");
            return;
        }

        if (!playerEnergy.TrySpend(effectiveEnergyCost))
        {
            Debug.Log("Summon failed: not enough energy.");
            return;
        }

        bool spawned = ranged ? spawner.TrySpawnRangedSummon(multiplier) : spawner.TrySpawnSummon(multiplier);
        if (!spawned)
        {
            playerEnergy.Restore(effectiveEnergyCost);
            Debug.Log("Summon failed: energy refunded.");
            return;
        }

        Debug.Log($"Summon created with energy {effectiveEnergyCost}, multiplier {multiplier:0.00}.");
    }

private static bool TryCalculateSummonMultiplier(int energyCost, out float multiplier, out int effectiveEnergyCost)
    {
        effectiveEnergyCost = Mathf.Clamp(energyCost, 0, BaseSummonEnergyCost * 2);
        multiplier = effectiveEnergyCost / (float)BaseSummonEnergyCost;
        if (multiplier < MinimumSummonMultiplier)
        {
            Debug.Log("Summon failed: drawing is too small.");
            return false;
        }

        multiplier = Mathf.Min(multiplier, MaximumSummonMultiplier);
        return true;
    }



private static bool HasValidPersistentTarget(UnityEvent unityEvent)
    {
        if (unityEvent == null)
        {
            return false;
        }

        for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
        {
            if (unityEvent.GetPersistentTarget(i) != null)
            {
                return true;
            }
        }

        return false;
    }
}
