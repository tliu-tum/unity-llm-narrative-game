using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DeepSeekAPI deepSeekAPI;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text dialogueText;
    private string characterName;

    [Header("Setting")]
    [SerializeField] private float typingSpeed = 0.005f; // 打字机效果的字符显示速度
    [SerializeField] private GameObject loadingIndicator;

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
            inputField.text = "";
            dialogueText.text = "";
            loadingIndicator.SetActive(true);
            deepSeekAPI.SendMessageToDeepSeek(text, HandleAIResposne);
        });
    }

    private void HandleAIResposne(string content, bool isSuccess)
    {
        StopAllCoroutines();
        inputField.interactable = false;
        string message = content;
        StartCoroutine(TypewriterEffect(isSuccess ? characterName + ":" + message : characterName + ": (communication interrupted...)"));;
    }

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
}
