using UnityEngine;
using UnityEngine.UI;

public class Loader : MonoBehaviour
{
    [SerializeField] bool loadNormalMap = false;
    [SerializeField] RawImage rawImage;

    Texture2D texture;
    float loadTime;

    void Load()
    {
        if (texture != null)
        {
            Destroy(texture);
        }

        loadTime = Time.realtimeSinceStartup;

        var filename = loadNormalMap ? "heifloader-normal.heic" : "heifloader.heic";
        var path = System.IO.Path.Combine(Application.streamingAssetsPath, filename);
        Debug.Log(path);
        texture = HeifLoader.LoadFromFile(path, true, loadNormalMap);

        loadTime = Time.realtimeSinceStartup - loadTime;
        Debug.Log("Load time: " + loadTime);

        rawImage.texture = texture;
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 100, 30), "Load"))
        {
            Load();
        }

        GUI.Label(new Rect(10, 40, 200, 30), $"Load time: {loadTime * 1000f:F2} ms");
    }
}
