using UnityEngine;

public class ForceResolution : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 推荐使用 FullScreenWindow 模式。
        // 它会创建一个与 1920x1080 像素尺寸严格匹配的无边框窗口，
        // 并且更有效地防止 OS 或驱动程序将你的内容拉伸到原生分辨率。
        Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
        
        // 如果你希望在运行时也能保持全屏状态
        Screen.fullScreen = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
