using UnityEngine;
using System.Diagnostics;
using System.Collections;

using Debug = UnityEngine.Debug;

public class Benchmark : MonoBehaviour
{
    [SerializeField] int waitTimeBeforeBenchmark = 3;
    [SerializeField] int loadCount = 10;

    void Start()
    {
        var path = System.IO.Path.Combine(Application.streamingAssetsPath, "heifloader.heic");
        Debug.Log(path);

        StartCoroutine(Run(path));
    }

    IEnumerator Run(string path)
    {
        Debug.Log($"ベンチマークを{waitTimeBeforeBenchmark}秒後に開始します...");
        yield return new WaitForSeconds(waitTimeBeforeBenchmark);

        RunBenchmark(path);
    }

    void RunBenchmark(string path)
    {
        Stopwatch stopwatch = new Stopwatch();
        long totalFlippedTime = 0;
        long totalNonFlippedTime = 0;

        // Benchmark with flipY enabled
        for (int i = 0; i < loadCount; i++)
        {
            stopwatch.Restart();
            var texture = HeifLoader.Load(path, true);
            stopwatch.Stop();
            totalFlippedTime += stopwatch.ElapsedMilliseconds;
        }

        // Benchmark with flipY disabled
        for (int i = 0; i < loadCount; i++)
        {
            stopwatch.Restart();
            var texture = HeifLoader.Load(path, false);
            stopwatch.Stop();
            totalNonFlippedTime += stopwatch.ElapsedMilliseconds;
        }

        float avgFlippedTime = (float)totalFlippedTime / loadCount;
        float avgNonFlippedTime = (float)totalNonFlippedTime / loadCount;

        // Display results
        string result = $"平均読み込み時間 ({loadCount}回):\nFlipY 有効: {avgFlippedTime:F2}ms\nFlipY 無効: {avgNonFlippedTime:F2}ms";
        Debug.Log(result);
    }
}
