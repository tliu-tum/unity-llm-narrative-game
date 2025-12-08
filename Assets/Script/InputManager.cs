using UnityEngine;
using TMPro;

public class InputManager : MonoBehaviour
{

    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private UIManager uiManager;

    private bool isShowing;

    private void Awake()
    {
        if(canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        gameObject.SetActive(false);

        if(codeInput != null)
        {
            codeInput.onSubmit.AddListener(OnCodeSubmitted);
        }
    }

    public void ShowInput()
    {
        Debug.Log("ShowInput called");

        isShowing = true;
        gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        //禁用主输入框
        if (uiManager != null)
        {
            uiManager.SetInputEnable(false);
        }

        //清空
        if (codeInput != null)
        {
            codeInput.text = string.Empty;
            codeInput.ActivateInputField();
            codeInput.caretPosition = 0;
            codeInput.selectionStringAnchorPosition = 0;
            codeInput.selectionStringFocusPosition = 0;
        }
    }

    //按回车触发
    private void OnCodeSubmitted(string text)
    {
        if (!isShowing) return;

        //输入交给LLM处理
        if(uiManager != null)
        {
            uiManager.OnCodeSubmitted(text);
        }

        Close();
    }

    public void Close()
    {
        isShowing = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);

        //启用主输入框
        if (uiManager != null)
        {
            uiManager.SetInputEnable(true);
        }
    }
}
