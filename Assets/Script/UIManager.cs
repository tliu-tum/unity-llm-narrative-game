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
    [SerializeField] private float typingSpeed = 0.005f; // seconds per character for the typewriter effect
    [SerializeField] private int maxVisibleMessages = 12; // = maxMessages in DeepSeekAPI

    [Header("Terminal Style Settings")]
    [SerializeField] private int maxCharsPerLine = 50;

    [Header("Popup")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private string playerColorHex = "#F9F1A5";
    [SerializeField] private float codePopupDelay = 1.5f;
    [SerializeField] private float closePopupDelay = 10f;

    private readonly List<string> messageHistory = new List<string>();
    private string characterName;
    private int currentTurn = 0;

    // Do not accept input while the popup modal is active
    //被弹窗锁定的时候不要响应输入
    private bool lockedByPopup = false;

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

            AddMessageToHistory(">_ <color=#F9F1A5>You:</color> \n>_ " + text);

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

        // Regex: loose NAME tag match — tolerates malformed brackets, extra spaces, mixed case
        // --- 修改后 (Robust / Loose) ---
        // 解释:
        // \[+          -> 匹配 1个或多个 '['
        // NAME         -> 匹配单词 NAME (后面加了 IgnoreCase 忽略大小写)
        // \s*:\s*      -> 匹配冒号，且允许冒号前后有任意个空格
        // (.*?)        -> 捕获组：非贪婪匹配名字内容
        // \]+          -> 匹配 1个或多个 ']'
        Match nameMatch = Regex.Match(content, @"\[+NAME\s*:\s*(.*?)\]+", RegexOptions.IgnoreCase);

        if (nameMatch.Success)
        {
            // 1. Extract and trim the name value
            // 1. 提取名字 & 去除首尾多余空格 (Trim)
            string newName = nameMatch.Groups[1].Value.Trim();

            // Extra safety: strip any stray closing brackets from the extracted name
            // 额外保险：防止名字里意外夹带了右括号（虽然正则已经处理了大部分）
            newName = newName.Replace("]", "");

            // 2. Update the active character name
            // 2. 更新名字
            characterName = newName;
            Debug.Log($"Character renamed to: {characterName}");

            // 3. Remove the full tag from content before display
            // 3. 从原文中移除整个标签 (nameMatch.Value 是指 [[NAME: ... ]] 这一整串)
            content = content.Replace(nameMatch.Value, "");
        }

        // v1 implementation — kept for reference
        /* Strict ver.
        // 匹配 [[NAME:任意内容]]
        Match nameMatch = Regex.Match(content, @"\[\[NAME:(.*?)\]\]");
        if (nameMatch.Success)
        {
            // 1. 提取名字 (Group[1] 是括号里的内容)
            string newName = nameMatch.Groups[1].Value;

            // 2. 更新当前的角色名
            characterName = newName;

            Debug.Log($"Character renamed to: {characterName}");

            // 3. 从显示的文本中移除这个标签，保持沉浸感
            content = content.Replace(nameMatch.Value, "");
        }
        */

        // Silently strip SYNC/TRUST score tags — present in prompts but must not appear in dialogue
        // 3. 【新增】隐形移除分数标签 (Robust Version)
        // Regex breakdown: require a digit to avoid false-matching plain words like "sync"
        // 原理：
        // \[+       -> 匹配 1个或多个 '['
        // \s*       -> 允许前面有空格
        // SYNC      -> 关键词 (配合 IgnoreCase 忽略大小写)
        // .*?       -> 中间可以夹杂冒号、等号或空格 (非贪婪匹配)
        // \d+       -> **必须包含数字** (防止误删普通对话里的单词 "Sync")
        // .*?       -> 允许数字后面有 % 或空格
        // \]+       -> 匹配 1个或多个 ']'

        // v1 implementation — kept for reference
        // content = Regex.Replace(content, @"\[+\s*SYNC.*?\d+.*?\]+", "", RegexOptions.IgnoreCase);
        // content = Regex.Replace(content, @"\[+\s*TRUST.*?\d+.*?\]+", "", RegexOptions.IgnoreCase);

        // v1 implementation — kept for reference
        /*
        //检测[[CODE:...]]标签，触发弹窗
        var codeMatch = Regex.Match(content, @"\[+CODE\s*:\s*(.*?)\]+", RegexOptions.IgnoreCase);
        if (codeMatch.Success)
        {
            content = Regex.Replace(content, @"\[+CODE\s*:\s*(.*?)\]+", "");

            Debug.Log("CODE tag detected. Triggering code input popup.");
            if(inputManager != null)
            {
                //延时弹窗，确保AI回复先显示出来
                StartCoroutine(ShowPopupDelayed());
                //inputManager.ShowInput();
            }
            else
            {
                Debug.LogWarning("CODE tag detexted but CodeInputPopup reference is missing.");
            }
        }
        */

        // Regex: loose CODE tag match with zero-width lookahead — handles malformed delimiters
        // 终极提取正则：
        // \[+CODE      -> 找 [[CODE
        // \s*[:=\s]\s* -> 容错分隔符：允许冒号(:)、等号(=) 或者 纯空格( )
        // (.*?)        -> 提取内容
        // (?=\]|\[|$)  -> 【零宽断言】预测结尾：要么是 ]，要么是下一个 [，要么是字符串结束
        var codeMatch = Regex.Match(content, @"\[+CODE\s*[:=\s]\s*(.*?)(?=\]|\[|$)", RegexOptions.IgnoreCase);

        if (codeMatch.Success)
        {
            // Strip any trailing brackets left from the regex match
            // 拿到内容后，去掉可能残留的右括号 (Trim)
            string codeContent = codeMatch.Groups[1].Value.Replace("]", "").Trim();
            Debug.Log($"CODE detected: {codeContent}");

            if (inputManager != null)
            {
                StartCoroutine(ShowPopupDelayed());
            }
        }

        // Unified tag cleanup: strip all remaining control tags (SYNC, TRUST, CODE, NAME) before display
        // =================================================================================
        // 2. 隐形移除所有标签 (SYNC, TRUST, CODE, NAME) - 统一清洗
        // =================================================================================

        // 清洗正则解释：
        // \[+\s*             -> 匹配 [[ 和可能的空格
        // (SYNC|TRUST|CODE|NAME) -> 匹配所有关键字
        // [^\[\]]*           -> 匹配中间内容，但遇到 [ 或 ] 就停（防止吃掉下一个标签）
        // (\]+|$)            -> 结尾必须是 ] 或者 字符串结束(针对截断)

        string cleanPattern = @"\[+\s*(SYNC|TRUST|CODE|NAME)[^\[\]]*(\]+|$)";

        // Execute cleanup
        content = Regex.Replace(content, cleanPattern, "", RegexOptions.IgnoreCase);

        // --- Handle ending tags ---
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

        // v1 implementation — kept for reference
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

        // --- Fail-safe: force a defined ending if the turn limit is reached ---
        if (string.IsNullOrEmpty(endingTag) && currentTurn >= maxVisibleMessages)
        {
            Debug.LogWarning("Turn limit reached. Forcing Ending.");
            endingTag = "DELETED";

            // Append an in-world error message to keep the narrative coherent on forced ending
            // 可以在这里强行追加一句台词，让剧情显得合理
            content += "\n\n(System Error: Connection timeout. Protocol purged.)";
        }

        string formattedContent = FormatToTerminalStyle(content);

        string line = isSuccess
        ? $">_ <color=#85E0A6>{characterName}:</color> \n>_ {formattedContent}"
        : $">_ <color=#85E0A6>{characterName}:</color> \n>_ (communication interrupted...)";

        StartCoroutine(TypewriterAppend(line));

        if (!string.IsNullOrEmpty(endingTag))
        {
            // TypewriterAppend is still running here; EndingManager's 3s initial delay gives it time to finish
            // 注意：因为 TypewriterAppend 是协程，文字还在打
            // EndingManager 内部有个 yield return WaitForSeconds(3.0f)
            // 刚好可以留出时间让打字机效果跑完，玩家读完。
            endingManager.TriggerEnding(endingTag);
            ClosePopUp();
        }
    }

    // Only animate the newest line; all prior history is displayed statically
    // 只对"新的一句"做打字机，前面的历史保持不动
    private IEnumerator TypewriterAppend(string newLine)
    {
        loadingIndicator.SetActive(false);
        dialogueText.enabled = true;
        inputField.DeactivateInputField();

        // Add to history now; the animation loop below handles incremental UI updates
        // 先把这条消息记录进 history，但暂时不直接刷新到 UI
        messageHistory.Add(newLine);
        while (messageHistory.Count > maxVisibleMessages)
            messageHistory.RemoveAt(0);

        // prefix = all history entries except the currently animating (last) line
        // prefix = 除了最后这一条以外的所有历史
        int lastIndex = messageHistory.Count - 1;
        string prefix = lastIndex > 0
            ? string.Join("\n\n", messageHistory.GetRange(0, lastIndex)) + "\n\n"
            : "";

        StringBuilder sb = new StringBuilder();

        // Reveal the new line one character at a time
        // 逐字把 newLine 打出来
        foreach (char c in newLine)
        {
            sb.Append(c);
            dialogueText.text = prefix + sb.ToString();
            yield return new WaitForSeconds(typingSpeed);
        }

        // Finalise: set the full joined history as the display text
        // 确保最后文本是完整 history（前面 + 完整 newLine）
        dialogueText.text = string.Join("\n\n", messageHistory);

        // Only re-enable input once the popup is confirmed closed
        //确定弹窗关闭的时候才响应输入
        if (!lockedByPopup)
        {
            inputField.interactable = true;
            inputField.ActivateInputField();
            inputField.caretPosition = inputField.text.Length;
        }
    }

    private IEnumerator ShowPopupDelayed()
    {
        yield return new WaitForSeconds(codePopupDelay);
        if (inputManager != null)
        {
            inputManager.ShowInput();
        }
    }

    private IEnumerator ClosePopUp()
    {
        yield return new WaitForSeconds(closePopupDelay);
        if (inputManager != null)
        {
            inputManager.Close();
        }
    }

    private void AddMessageToHistory(string line)
    {
        messageHistory.Add(line);

        // If over the limit, drop oldest entries first
        // 超过数量就从最老的开始删
        while (messageHistory.Count > maxVisibleMessages)
        {
            messageHistory.RemoveAt(0);
        }

        // Rebuild display text from the full history buffer, blank line between messages
        // 把所有消息拼成一段大文本，中间空一行
        dialogueText.text = string.Join("\n\n", messageHistory);
    }

    private string FormatToTerminalStyle(string originalText)
    {
        StringBuilder finalBuilder = new StringBuilder();

        // 1. Split on the AI's own line breaks first, to preserve intended paragraph structure
        // 1. 先按"AI输出的原有换行"切分段落
        // (比如AI自己分段了，我们要保留这个分段结构)
        string[] paragraphs = originalText.Split('\n');

        for (int i = 0; i < paragraphs.Length; i++)
        {
            string paragraph = paragraphs[i];

            // Skip blank paragraphs produced by consecutive newlines in the AI response
            // 如果是空行（AI输出了连续换行），我们可以选择跳过或者加个空行
            if (string.IsNullOrWhiteSpace(paragraph)) continue;

            // 2. Tokenise the paragraph into words for word-wrap
            // 2. 将段落拆分成单词 (用空格拆分)
            string[] words = paragraph.Split(' ');

            StringBuilder currentLine = new StringBuilder();

            foreach (string word in words)
            {
                // Skip empty tokens from consecutive spaces
                // 如果是空单词（比如连续空格），跳过
                if (string.IsNullOrEmpty(word)) continue;

                // 3. Predict: would appending this word exceed the character limit?
                // 3. 预测：如果加上这个单词，长度会不会爆？
                // 现有长度 + 空格(1) + 新单词长度
                int potentialLength = currentLine.Length + word.Length + (currentLine.Length > 0 ? 1 : 0);

                if (potentialLength > maxCharsPerLine)
                {
                    // Line overflow: flush current line to output and start a new one
                    // --- 爆了：把当前行存入 finalBuilder，并换行 ---

                    // 如果 finalBuilder 已经有内容了，说明这是第2、3...行，需要加前缀
                    // (如果是整个回复的第一行，HandleAIResponse 外面已经加了前缀，所以这里不加)
                    if (finalBuilder.Length > 0)
                    {
                        finalBuilder.Append("\n>_ ");
                    }

                    finalBuilder.Append(currentLine.ToString());

                    // Reset line buffer; the overflowing word starts the next line
                    // 重置当前行，并将这个"导致溢出"的单词作为新一行的开头
                    currentLine.Clear();
                    currentLine.Append(word);
                }
                else
                {
                    // No overflow: append word to current line
                    // --- 没爆：追加到当前行 ---
                    if (currentLine.Length > 0)
                    {
                        currentLine.Append(" "); // add space between words
                    }
                    currentLine.Append(word);
                }
            }

            // 4. Flush the last partial line of the paragraph
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

    // Called by InputManager to lock or unlock the main input field
    //供弹窗调用，锁定输入
    public void SetInputEnable(bool enables)
    {
        lockedByPopup = !enables;
        inputField.interactable = enables;

        if (enables)
        {
            inputField.ActivateInputField();
            inputField.caretPosition = inputField.text.Length;
        }
        else
        {
            inputField.DeactivateInputField();
        }
    }

    // Called by InputManager when the player submits code input via Enter
    //回车之后调用的方法
    public void OnCodeSubmitted(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.Log("Empty code input.");
            text = "(empty)";
        }

        AddMessageToHistory(">_ <color=" + playerColorHex + ">You (code input):</color> \n>_ " + text);

        loadingIndicator.SetActive(true);
        dialogueText.enabled = false;

        deepSeekAPI.SendMessageToDeepSeek(text, HandleAIResponse);
    }
}
