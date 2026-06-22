using Steamworks;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Mail;
using UnityEngine;
using UnityEngine.Networking;

public class DebugLogger : MonoBehaviour
{
    [Header("Telegram Settings")]
    [SerializeField] private string botToken = "8822747807:AAEEbQ-_YIZKverP7mfxJHOoKl76qZOjFlI";
    [SerializeField] private string chatId = "5195493417";

    [Header("Email Settings")]
    [SerializeField] private string smtpServer = "smtp.mail.ru";
    [SerializeField] private int smtpPort = 587;
    [SerializeField] private string senderEmail = "forgotlogsender@mail.ru";
    [SerializeField] private string senderPassword = "jXBei0cDItAPXfFf7aHO";
    [SerializeField] private string recipientEmail = "forgotloger@mail.ru";

    [Header("Behaviour")]
    [SerializeField] private bool sendOnError = true;
    [SerializeField] private bool sendOnQuit = true;
    [SerializeField] private float minIntervalBetweenSends = 60f; // секунды

    private string _logFilePath;
    private string _sessionId;
    private float _lastSendTime = -60f;
    private bool _hasErrorSinceLastSend = false;

    // Имя игрока (если Steam инициализирован)
    private string PlayerName => SteamInitializer.SteamInitialized ? SteamFriends.GetPersonaName() : "Unknown";
    private string PlayerSteamID => SteamInitializer.SteamInitialized ? SteamUser.GetSteamID().ToString() : "N/A";

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _sessionId = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string logDir = Path.Combine(Application.persistentDataPath, "Logs");
        Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, $"log_{_sessionId}.txt");

        File.WriteAllText(_logFilePath, GetSessionHeader());
        Debug.Log($"Лог-файл создан: {_logFilePath}");
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void OnApplicationQuit()
    {
        if (sendOnQuit && File.Exists(_logFilePath) && new FileInfo(_logFilePath).Length > 0)
        {
            SendFullReport("Завершение игры");
        }
    }

    // Вызывается из UI для ручной отправки
    public void SendManualReport()
    {
        if (File.Exists(_logFilePath) && new FileInfo(_logFilePath).Length > 0)
        {
            SendFullReport("Ручной отчёт игрока");
        }
        else
        {
            Debug.LogWarning("Нет логов для отправки.");
        }
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Записываем всё в файл
        string entry = $"[{System.DateTime.Now:HH:mm:ss.fff}] [{type}] {logString}\n{stackTrace}\n";
        File.AppendAllText(_logFilePath, entry);

        // Если ошибка и включена отправка
        if (sendOnError && (type == LogType.Error || type == LogType.Exception))
        {
            _hasErrorSinceLastSend = true;
            if (Time.time - _lastSendTime >= minIntervalBetweenSends)
            {
                SendFullReport("Произошла ошибка: " + logString);
                _lastSendTime = Time.time;
                _hasErrorSinceLastSend = false;
            }
        }
    }

    private void SendFullReport(string title)
    {
        StartCoroutine(SendFullReportCoroutine(title));
    }

    private IEnumerator SendFullReportCoroutine(string title)
    {
        // 1. Отправляем краткое сообщение в Telegram
        yield return StartCoroutine(SendTelegramMessage(GetShortReport(title)));

        // 2. Отправляем файл в Telegram
        yield return StartCoroutine(SendTelegramFile());

        // 3. Отправляем файл на почту
        SendEmailLog();

        // 4. Опционально очищаем файл после отправки? Нет, оставляем для истории.
    }

    private string GetSessionHeader()
    {
        string header = "=== ЛОГ СЕССИИ ===\n";
        header += $"Время начала: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
        header += $"Версия игры: {Application.version}\n";
        header += $"Unity версия: {Application.unityVersion}\n";
        header += $"Платформа: {Application.platform}\n";
        header += $"Игрок: {PlayerName} (SteamID: {PlayerSteamID})\n";
        header += "------------------------\n";
        return header;
    }

    private string GetShortReport(string title)
    {
        string report = $"⚠️ {title}\n\n";
        report += $"Время: {System.DateTime.Now:HH:mm:ss}\n";
        report += $"Игрок: {PlayerName} (ID: {PlayerSteamID})\n";
        report += $"Версия: {Application.version}\n";
        report += $"Файл с полным логом приложен к сообщению.";
        return report;
    }

    // ======================= TELEGRAM =======================

    private IEnumerator SendTelegramMessage(string message)
    {
        string url = $"https://api.telegram.org/bot{botToken}/sendMessage";
        WWWForm form = new WWWForm();
        form.AddField("chat_id", chatId);
        form.AddField("text", message);
        form.AddField("parse_mode", "HTML");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"Telegram message send error: {request.error}");
            else
                Debug.Log("Telegram: краткое сообщение отправлено.");
        }
    }

    private IEnumerator SendTelegramFile()
    {
        if (!File.Exists(_logFilePath)) yield break;

        string url = $"https://api.telegram.org/bot{botToken}/sendDocument";
        byte[] fileData = File.ReadAllBytes(_logFilePath);

        WWWForm form = new WWWForm();
        form.AddField("chat_id", chatId);
        form.AddBinaryData("document", fileData, $"log_{_sessionId}.txt", "text/plain");
        form.AddField("caption", $"Лог сессии {_sessionId}");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"Telegram file send error: {request.error}");
            else
                Debug.Log("Telegram: файл с логом отправлен.");
        }
    }

    // ======================= EMAIL =======================

    private void SendEmailLog()
    {
        if (!File.Exists(_logFilePath))
        {
            Debug.LogWarning("Файл лога не найден для отправки по почте.");
            return;
        }

        try
        {
            using (SmtpClient client = new SmtpClient(smtpServer, smtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(senderEmail, senderPassword);

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(senderEmail);
                    mail.To.Add(recipientEmail);
                    mail.Subject = $"Логи игры {Application.productName} - {_sessionId}";
                    mail.Body = $"Логи от игрока {PlayerName} (SteamID: {PlayerSteamID})\n" +
                                $"Время: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                                $"Версия: {Application.version}";

                    Attachment attachment = new Attachment(_logFilePath);
                    mail.Attachments.Add(attachment);

                    client.Send(mail);
                    Debug.Log("Email: письмо с логом отправлено.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Email send error: {e.Message}");
        }
    }
}