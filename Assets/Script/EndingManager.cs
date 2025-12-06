using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class EndingManager : MonoBehaviour
{
    [System.Serializable]
    public struct EndingData
    {
        public string tag;
        public Sprite endingImage;
        [TextArea(3, 10)] 
        public string description;
        public Color themeColor;
    }

    [Header("UI References")]
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private CanvasGroup endingCanvasGroup;
    [SerializeField] private Image backgroundDisplay;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private float initialDelay = 3.0f;
    [SerializeField] private float fadeDuration = 1.5f; // 【新增】渐显持续时间

    [Header("Data")]
    [SerializeField] private List<EndingData> endings;

    void Start()
    {
        if (endingPanel != null) 
        {
            endingPanel.SetActive(false);
            // 确保一开始透明度也是0，防止意外显示
            if(endingCanvasGroup != null) endingCanvasGroup.alpha = 0; 
        }

        if (restartButton != null) 
            restartButton.onClick.AddListener(RestartGame);

        if (exitButton != null) 
            exitButton.onClick.AddListener(QuitGame);
    }

    public void TriggerEnding(string tag)
    {
        EndingData data = endings.Find(e => e.tag == tag);
        
        if (string.IsNullOrEmpty(data.tag))
        {
            Debug.LogWarning($"Ending tag '{tag}' not found!");
            return;
        }

        StartCoroutine(PlayEndingSequence(data));
    }

    private IEnumerator PlayEndingSequence(EndingData data)
    {
        // 1. 等待对话完全结束
        yield return new WaitForSeconds(initialDelay);

        // 2. 准备 UI 状态 (先开启物体，但设为全透明)
        endingPanel.SetActive(true);
        endingCanvasGroup.alpha = 0; // 【关键】设为透明
        
        backgroundDisplay.sprite = data.endingImage;
        restartButton.image.color = data.themeColor;
        exitButton.image.color = data.themeColor;
        descriptionText.color = data.themeColor;
        descriptionText.text = ""; 
        restartButton.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(false);

        // 3. 【新增】执行 Fade In 渐显效果
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // Lerp 从 0 变到 1
            endingCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }
        endingCanvasGroup.alpha = 1f; // 确保完全不透明

        // 4. 图片完全显示后，开始打字
        foreach (char c in data.description)
        {
            descriptionText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        // 5. 显示按钮
        restartButton.gameObject.SetActive(true);
        exitButton.gameObject.SetActive(true);
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void QuitGame()
    {
        Debug.Log("Game Quit triggered.");
        
        Application.Quit();

        // 这段代码让在 Unity 编辑器里点退出也能停止播放
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
