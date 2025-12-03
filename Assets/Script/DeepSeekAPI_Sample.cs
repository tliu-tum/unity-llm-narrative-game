using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class DeepSeekAPI_Sample : MonoBehaviour
{
    private string apiKey = "sk-f5dafb6122974678bca3de410c34b397";
    private string apiUrl = "https://api.deepseek.com/v1/chat/completions";

    void Start()
    {
        SendMessageToDeepSeek("Hella", null);
    }

    public void SendMessageToDeepSeek(string message, UnityAction<string> callback)
    {
        StartCoroutine(PostRequest(message, callback));
    }

    IEnumerator PostRequest(string message, UnityAction<string> callback)
    {
        var requestBody = new
        {
            model = "deepseek-chat",
            messages = new[]
            {
                new { role = "user", content = message }
            }
        };

        string jsonBody = JsonConvert.SerializeObject(requestBody);
        Debug.Log(jsonBody);
        // yield return null;

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");

        // http 只能传字节
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
        else
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log("Response: " + responseJson);
        }
    }
}
