using UnityEngine;

/// <summary>
/// Shared icon loading helper for inventory views.
/// </summary>
public static class InventorySpriteLoader
{
public static Sprite Load(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return null;
        }

        string path = configuredPath.Trim().TrimStart('/').Replace("\\", "/");
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture != null)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

#if UNITY_EDITOR
        string[] candidates =
        {
            path,
            "Assets/" + path,
            "Assets/" + path + ".png",
            "Assets/Resources/" + path,
            "Assets/Resources/" + path + ".png"
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(candidates[i]);
            if (sprite != null)
            {
                return sprite;
            }

            texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(candidates[i]);
            if (texture != null)
            {
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
        }

        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        string[] guids = UnityEditor.AssetDatabase.FindAssets(fileName + " t:Texture2D");
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!assetPath.Contains(fileName))
            {
                continue;
            }

            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture != null)
            {
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
        }
#endif

        return null;
    }
}
