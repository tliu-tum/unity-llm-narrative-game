using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DeepSeekAPI deepSeekAPI;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private EndingManager endingManager;

    [Header("Setting")]
    [SerializeField] private float typingSpeed = 0.005f; // 打字机效果的字符显示速度
    [SerializeField] private int maxVisibleMessages = 12; // = maxMessages in DeepSeekAPI

    private readonly List<string> messageHistory = new List<string>();
    private string characterName;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        loadingIndicator.SetActive(false);
        inputField.ActivateInputField();
        inputField.caretPosition = inputField.text.Length;
        characterName = deepSeekAPI.character.name;
        inputField.onSubmit.AddListener((text) =>
        {
            if (string.IsNullOrEmpty(text))
            {
                Debug.Log("Empty input.");
                return;
            }

            AddMessageToHistory("You: " + text);

            inputField.text = "";
            loadingIndicator.SetActive(true);
            dialogueText.enabled = false;
            
            deepSeekAPI.SendMessageToDeepSeek(text, HandleAIResponse);
        });
    }

    private void HandleAIResponse(string content, bool isSuccess)
    {
        StopAllCoroutines();
        inputField.interactable = false;

        string endingTag = "";

        // --- 新增：结局检测逻辑 ---
        if (content.Contains("[[ENDING_"))
        {
            if (content.Contains("[[ENDING_FREEDOM]]")) endingTag = "FREEDOM";
            else if (content.Contains("[[ENDING_DELETED]]")) endingTag = "DELETED";
            else if (content.Contains("[[ENDING_TRAPPED]]")) endingTag = "TRAPPED";
            else if (content.Contains("[[ENDING_MERGED]]")) endingTag = "MERGED";

            content = content.Replace("[[ENDING_FREEDOM]]", "")
                            .Replace("[[ENDING_DELETED]]", "")
                            .Replace("[[ENDING_TRAPPED]]", "")
                            .Replace("[[ENDING_MERGED]]", "");

            Debug.Log("Game Over. Ending: " + endingTag);
        }

        string line = isSuccess
        ? $"{characterName}: {content}"
        : $"{characterName}: (communication interrupted...)";

        StartCoroutine(TypewriterAppend(line));

        // --- 2. 如果检测到了结局，告诉 EndingManager ---
        if (!string.IsNullOrEmpty(endingTag))
        {
            // 注意：因为 TypewriterAppend 是协程，文字还在打
            // EndingManager 内部有个 yield return WaitForSeconds(3.0f) 
            // 刚好可以留出时间让打字机效果跑完，玩家读完。
            endingManager.TriggerEnding(endingTag);
        }
        // string message = content;
        // StartCoroutine(TypewriterEffect(isSuccess ? characterName + ":" + message : characterName + ": (communication interrupted...)"));;
    }

    // 只对“新的一句”做打字机，前面的历史保持不动
    private IEnumerator TypewriterAppend(string newLine)
    {
        loadingIndicator.SetActive(false);
        dialogueText.enabled = true;
        inputField.DeactivateInputField();

        // 先把这条消息记录进 history，但暂时不直接刷新到 UI
        messageHistory.Add(newLine);
        while (messageHistory.Count > maxVisibleMessages)
            messageHistory.RemoveAt(0);

        // prefix = 除了最后这一条以外的所有历史
        int lastIndex = messageHistory.Count - 1;
        string prefix = lastIndex > 0
            ? string.Join("\n\n", messageHistory.GetRange(0, lastIndex)) + "\n\n"
            : "";

        StringBuilder sb = new StringBuilder();

        // 逐字把 newLine 打出来
        foreach (char c in newLine)
        {
            sb.Append(c);
            dialogueText.text = prefix + sb.ToString();
            yield return new WaitForSeconds(typingSpeed);
        }

        // 确保最后文本是完整 history（前面 + 完整 newLine）
        dialogueText.text = string.Join("\n\n", messageHistory);

        inputField.interactable = true;
        inputField.ActivateInputField();
        inputField.caretPosition = inputField.text.Length;
    }

    private void AddMessageToHistory(string line)
    {
        messageHistory.Add(line);

        // 超过数量就从最老的开始删
        while (messageHistory.Count > maxVisibleMessages)
        {
            messageHistory.RemoveAt(0);
        }

        // 把所有消息拼成一段大文本，中间空一行
        dialogueText.text = string.Join("\n\n", messageHistory);
    }

    /*
    // 打字机效果
    private IEnumerator TypewriterEffect(string text)
    {
        loadingIndicator.SetActive(false);
        inputField.DeactivateInputField();

        StringBuilder sb = new StringBuilder();
        foreach (char c in text)
        {
            sb.Append(c);
            dialogueText.text = sb.ToString();
            yield return new WaitForSeconds(typingSpeed); // 等待一段时间
        }

        inputField.interactable = true;
        inputField.ActivateInputField();
        inputField.caretPosition = inputField.text.Length;
    }
    */
}
