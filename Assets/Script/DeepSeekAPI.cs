using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class DeepSeekAPI : MonoBehaviour
{
    private string apiKey = "sk-f5dafb6122974678bca3de410c34b397";
    private string apiUrl = "https://api.deepseek.com/v1/chat/completions";

    [SerializeField]
    private string modelName = "deepseek-chat";

    [Header("Dialogue Settings")] // 参数
    [Range(0, 2)] public float temperature = 0.5f; // 越高越随机
    [Range(1, 1000)] public int maxTokens = 200;

    [System.Serializable] // serializable class (serialize to json)
    public class Character
    {
        public string name = "Unity-助手";

        [TextArea(3, 20)]
        public string personalityPrompt = "你是unity助手 擅长Unity和c#编程知识。";
    }

    [SerializeField] public Character character;

    public delegate void DialogueCallback(string content, bool isSuccess);

    // ===== 新增：对话历史 =====
    private List<ApiMessage> conversationHistory = new List<ApiMessage>();

    void Start()
    {
        
    }

    public void SendMessageToDeepSeek(string message, DialogueCallback callback)
    {
        StartCoroutine(PostRequest(message, callback));
    }

    IEnumerator PostRequest(string message, DialogueCallback callback)
    {
        // ===== 新增：初始化 system（只加一次）=====
        if (conversationHistory.Count == 0)
        {
            conversationHistory.Add(new ApiMessage
            {
                role = "system",
                content = character.personalityPrompt
            });
        }

        // ===== 新增：加入 user =====
        conversationHistory.Add(new ApiMessage
        {
            role = "user",
            content = message
        });

        // ===== 新增：裁剪历史（保留 system）=====
        TrimConversationHistory();

        ApiRequest requestBody = new ApiRequest
        {
            model = modelName,
            messages = conversationHistory,
            temperature = temperature,
            max_tokens = maxTokens
        };

        string jsonBody = JsonConvert.SerializeObject(requestBody);
        Debug.Log(jsonBody);

        UnityWebRequest request = CreateWebRequest(jsonBody);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || 
            request.result == UnityWebRequest.Result.ProtocolError || 
            request.result == UnityWebRequest.Result.DataProcessingError)
        {
            if (request.responseCode == 429) // 速率限制 too many requests
            {
                Debug.LogWarning("Rate limit reached, retrying");
                yield return new WaitForSeconds(5);
                StartCoroutine(PostRequest(message, callback));
                yield break;
            } 
            else
            {
                Debug.LogError("Error: " + request.error);
                Debug.LogError("Response code: " + request.responseCode);

                // ===== 新增：失败回滚刚刚加入的 user =====
                if (conversationHistory.Count > 0 && conversationHistory[conversationHistory.Count - 1].role == "user")
                    conversationHistory.RemoveAt(conversationHistory.Count - 1);

                callback?.Invoke("API request failed: " + request.downloadHandler.text, false);
                yield break;
            }  
        }
        
        ApiResponse response = ParseResponse(request.downloadHandler.text);

        if (response.choices != null && response.choices.Length > 0)
        {
            string reply = response.choices[0].message.content;

            // ===== 新增：把 assistant 写入历史 =====
            conversationHistory.Add(new ApiMessage
            {
                role = "assistant",
                content = reply
            });

            callback?.Invoke(reply, true);
        }
        else
        {
            callback?.Invoke(name + "is silent", false);
        }

        request.Dispose(); // clean used up request
    }

    private UnityWebRequest CreateWebRequest(string jsonBody)
    {
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");

        // http 只能传字节
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("Accept", "application/json");

        return request;
    }

    private ApiResponse ParseResponse(string jsonResponse)
    {   
        Debug.Log("Response: " + jsonResponse);
        try
        {
            ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(jsonResponse);

            if (response == null || response.choices == null || response.choices.Length == 0)
            {
                Debug.LogError("API 响应结构异常，未包含 choices");
                return null;
            }

            return response;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON解析失败: {e.Message}\n原始内容: {jsonResponse}");
            return null;
        }
    }

    // ===== 新增：裁剪规则（永远保留 system）=====
    private void TrimConversationHistory()
    {
        // 想保留的“轮数”
    int maxRounds = 6;

        // 每轮 = user + assistant = 2 条
        int maxMessages = maxRounds * 2;

        // system 永远保留在 index 0
        int systemOffset = 1;

        // 当 非system 的消息数 > maxMessages 时，删除最旧的一条
        while (conversationHistory.Count - systemOffset > maxMessages)
        {
            conversationHistory.RemoveAt(systemOffset);
        }
    }

    [System.Serializable]
    public class ApiMessage
    {
        public string role; // system/user/assistant
        public string content;
    }

    [System.Serializable]
    public class ApiRequest
    {
        public string model;
        public List<ApiMessage> messages;
        public float temperature;
        public int max_tokens;
    }

    [System.Serializable]
    public class ApiChoice
    {
        public ApiMessage message;
    }

    [System.Serializable]
    public class ApiResponse
    {
        public ApiChoice[] choices;
    }
}
