using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

    [Header("Terminal Style Settings")]
    [SerializeField] private int maxCharsPerLine = 50;

    private readonly List<string> messageHistory = new List<string>();
    private string characterName;
    private int currentTurn = 0;

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

            AddMessageToHistory(">_ You: " + text);

            inputField.text = "";
            loadingIndicator.SetActive(true);
            dialogueText.enabled = false;
            
            deepSeekAPI.SendMessageToDeepSeek(text, HandleAIResponse);
        });
    }

    private void HandleAIResponse(string content, bool isSuccess)
    {
        currentTurn++;
        StopAllCoroutines();
        inputField.interactable = false;

        string upperContent = content.ToUpper();
        string endingTag = "";

        if (upperContent.Contains("ENDING_"))
        {
            if (upperContent.Contains("ENDING_FREEDOM")) endingTag = "FREEDOM";
            else if (upperContent.Contains("ENDING_DELETED")) endingTag = "DELETED";
            else if (upperContent.Contains("ENDING_TRAPPED")) endingTag = "TRAPPED";
            else if (upperContent.Contains("ENDING_MERGED")) endingTag = "MERGED";

            content = Regex.Replace(content, @"\[\[ENDING_.*?\]\]", "");
            Debug.Log("Game Over. Ending: " + endingTag);
        }

        // Stricter ver.
        /*
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
        */

        // --- 保险机制 (Fail-safe) ---
        if (string.IsNullOrEmpty(endingTag) && currentTurn >= maxVisibleMessages)
        {
            Debug.LogWarning("Turn limit reached. Forcing Ending.");
            endingTag = "DELETED"; 
            
            // 可以在这里强行追加一句台词，让剧情显得合理
            content += "\n\n(System Error: Connection timeout. Protocol purged.)";
        }

        string formattedContent = FormatToTerminalStyle(content);

        string line = isSuccess
        ? $">_ {characterName}: {formattedContent}"
        : $">_ {characterName}: (communication interrupted...)";

        StartCoroutine(TypewriterAppend(line));

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

    private string FormatToTerminalStyle(string originalText)
    {
        StringBuilder finalBuilder = new StringBuilder();
        
        // 1. 先按“AI输出的原有换行”切分段落
        // (比如AI自己分段了，我们要保留这个分段结构)
        string[] paragraphs = originalText.Split('\n');

        for (int i = 0; i < paragraphs.Length; i++)
        {
            string paragraph = paragraphs[i];
            
            // 如果是空行（AI输出了连续换行），我们可以选择跳过或者加个空行
            if (string.IsNullOrWhiteSpace(paragraph)) continue;

            // 2. 将段落拆分成单词 (用空格拆分)
            string[] words = paragraph.Split(' ');
            
            StringBuilder currentLine = new StringBuilder();

            foreach (string word in words)
            {
                // 如果是空单词（比如连续空格），跳过
                if (string.IsNullOrEmpty(word)) continue;

                // 3. 预测：如果加上这个单词，长度会不会爆？
                // 现有长度 + 空格(1) + 新单词长度
                int potentialLength = currentLine.Length + word.Length + (currentLine.Length > 0 ? 1 : 0);

                if (potentialLength > maxCharsPerLine)
                {
                    // --- 爆了：把当前行存入 finalBuilder，并换行 ---
                    
                    // 如果 finalBuilder 已经有内容了，说明这是第2、3...行，需要加前缀
                    // (如果是整个回复的第一行，HandleAIResponse 外面已经加了前缀，所以这里不加)
                    if (finalBuilder.Length > 0)
                    {
                        finalBuilder.Append("\n>_ "); 
                    }
                    
                    finalBuilder.Append(currentLine.ToString());

                    // 重置当前行，并将这个“导致溢出”的单词作为新一行的开头
                    currentLine.Clear();
                    currentLine.Append(word);
                }
                else
                {
                    // --- 没爆：追加到当前行 ---
                    if (currentLine.Length > 0)
                    {
                        currentLine.Append(" "); // 单词之间加空格
                    }
                    currentLine.Append(word);
                }
            }

            // 4. 处理段落剩下的最后一行
            if (currentLine.Length > 0)
            {
                if (finalBuilder.Length > 0)
                {
                    finalBuilder.Append("\n>_ ");
                }
                finalBuilder.Append(currentLine.ToString());
            }
        }

        return finalBuilder.ToString();
    }
}
