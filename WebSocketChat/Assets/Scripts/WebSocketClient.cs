using System;
using UnityEngine;
using UnityEngine.UI;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;

public class WebSocketClient : MonoBehaviour
{
    public TMP_InputField messageInput;
    public Button sendButton;
    public TextMeshProUGUI chatDisplay;
    public TextMeshProUGUI statusText;
    
    private ClientWebSocket webSocket;
    private CancellationTokenSource cancellationTokenSource;
    private string receivedMessage = "";
    
    void Start()
    {
        sendButton.onClick.AddListener(SendMessage);
        ConnectToServer();
    }
    
    async void ConnectToServer()
    {
        statusText.text = "Подключение...";
        
        webSocket = new ClientWebSocket();
        cancellationTokenSource = new CancellationTokenSource();
        
        try
        {
            await webSocket.ConnectAsync(new System.Uri("ws://localhost:8080"), cancellationTokenSource.Token);
            statusText.text = "Подключено!";
            Debug.Log("Подключено к WebSocket серверу");
            
            _ = ReceiveMessages();
        }
        catch (System.Exception e)
        {
            statusText.text = "Ошибка подключения";
            Debug.LogError($"Ошибка подключения: {e.Message}");
        }
    }
    
    async Task ReceiveMessages()
    {
        byte[] buffer = new byte[4096];
        
        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource.Token);
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    receivedMessage = message;
                    chatDisplay.text = $"Получено сообщение: {message}";
                    Debug.Log($"Получено: {message}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка получения: {e.Message}");
                break;
            }
        }
    }
    
    async void SendMessage()
    {
        if (webSocket.State != WebSocketState.Open)
        {
            statusText.text = "Не подключено к серверу";
            return;
        }
        
        string message = messageInput.text;
        if (string.IsNullOrEmpty(message))
        {
            statusText.text = "Введите сообщение";
            return;
        }
        
        byte[] bytes = Encoding.UTF8.GetBytes(message);
        
        try
        {
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationTokenSource.Token);
            chatDisplay.text = $"Вы отправили: {message}";
            messageInput.text = "";
            statusText.text = "Сообщение отправлено";
            Debug.Log($"Отправлено: {message}");
        }
        catch (System.Exception e)
        {
            statusText.text = "Ошибка отправки";
            Debug.LogError($"Ошибка отправки: {e.Message}");
        }
    }
    
    async void OnDestroy()
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            webSocket.Dispose();
        }
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
}