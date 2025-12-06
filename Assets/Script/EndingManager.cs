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
    [SerializeField] private Image backgroundDisplay;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button restartButton;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private float initialDelay = 5.0f;

    [Header("Data")]
    [SerializeField] private List<EndingData> endings;

    void Start()
    {
        if (endingPanel != null) endingPanel.SetActive(false);
        restartButton.onClick.AddListener(RestartGame);
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
        // 1. 等待几秒 (让玩家看完最后一句对话)
        yield return new WaitForSeconds(initialDelay);

        // 2. 开启面板并设置图片
        endingPanel.SetActive(true);
        backgroundDisplay.sprite = data.endingImage;
        restartButton.image.color = data.themeColor;
        descriptionText.color = data.themeColor;
        descriptionText.text = ""; // 清空文本准备打字
        restartButton.gameObject.SetActive(false); // 先隐藏按钮

        // 3. 执行打字机效果
        foreach (char c in data.description)
        {
            descriptionText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        // 4. 打字完成后，显示重玩按钮
        restartButton.gameObject.SetActive(true);
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
