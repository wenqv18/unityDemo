using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads the shared runtime UI scene additively for gameplay levels.
/// Keep this on each level scene that should use the global UI stack.
/// </summary>
public sealed class RuntimeUISceneLoader : MonoBehaviour
{
    [SerializeField] private string runtimeUISceneName = "RuntimeUIScene";

    private IEnumerator Start()
    {
        if (!IsSceneLoaded(runtimeUISceneName))
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(runtimeUISceneName, LoadSceneMode.Additive);
            while (loadOperation != null && !loadOperation.isDone)
            {
                yield return null;
            }
        }

        RuntimeUIManager manager = FindObjectOfType<RuntimeUIManager>(true);
        if (manager != null)
        {
            manager.BindToCurrentLevel();
        }
    }

    private static bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.name == sceneName)
            {
                return true;
            }
        }

        return false;
    }
}
