using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NoteApp;

// ====================================================
// Local HTTP Server — รับ SMS จากมือถือผ่าน WiFi
// ====================================================
public class SMSReceiverServer
{
    private readonly HttpListener _listener = new();
    private readonly Action<BankTransaction> _onReceive;
    private readonly Action<string> _onLog;
    private CancellationTokenSource? _cts;
    public int Port { get; }
    public bool IsRunning { get; private set; }

    public SMSReceiverServer(int port, Action<BankTransaction> onReceive, Action<string> onLog)
    {
        Port = port;
        _onReceive = onReceive;
        _onLog = onLog;
    }

    public void Start()
    {
        if (IsRunning) return;
        _cts = new CancellationTokenSource();
        _listener.Prefixes.Clear();
        _listener.Prefixes.Add($"http://+:{Port}/");
        try
        {
            _listener.Start();
            IsRunning = true;
            _onLog($"✅ เซิร์ฟเวอร์เริ่มทำงานที่พอร์ต {Port}");
            Task.Run(() => ListenLoop(_cts.Token));
        }
        catch (HttpListenerException)
        {
            _listener.Prefixes.Clear();
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Start();
            IsRunning = true;
            _onLog($"✅ เซิร์ฟเวอร์เริ่มทำงาน (localhost:{Port})");
            Task.Run(() => ListenLoop(_cts.Token));
        }
    }

    public void Stop()
    {
        if (!IsRunning) return;
        _cts?.Cancel();
        _listener.Stop();
        IsRunning = false;
        _onLog("⏹️ เซิร์ฟเวอร์หยุดทำงาน");
    }

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context));
            }
            catch (Exception) when (ct.IsCancellationRequested) { break; }
            catch { }
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        var req = context.Request;
        var resp = context.Response;

        try
        {
            if (req.Url?.AbsolutePath == "/api/sms" && req.HttpMethod == "POST")
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                var body = reader.ReadToEnd();
                var tx = ProcessSMS(body);
                var json = tx != null
                    ? JsonSerializer.Serialize(new { success = true, category = tx.Category, type = tx.Type, amount = tx.Amount })
                    : JsonSerializer.Serialize(new { success = false, error = "ไม่สามารถแยกข้อมูลได้" });
                WriteResponse(resp, 200, json);
            }
            else if (req.Url?.AbsolutePath == "/api/status")
            {
                WriteResponse(resp, 200, JsonSerializer.Serialize(new { status = "ok", app = "NoteApp", time = DateTime.Now.ToString("HH:mm:ss") }));
            }
            else if (req.Url?.AbsolutePath == "/" || req.Url?.AbsolutePath == "/setup")
            {
                WriteResponse(resp, 200, GetSetupPage(), "text/html");
            }
            else
            {
                WriteResponse(resp, 404, "{\"error\":\"not found\"}");
            }
        }
        catch (Exception ex)
        {
            WriteResponse(resp, 500, JsonSerializer.Serialize(new { error = ex.Message }));
        }
    }

    private BankTransaction? ProcessSMS(string body)
    {
        try
        {
            var doc = JsonDocument.Parse(body);
            var smsText = doc.RootElement.TryGetProperty("message", out var msg) ? msg.GetString() ?? ""
                : doc.RootElement.TryGetProperty("sms", out var sms) ? sms.GetString() ?? ""
                : doc.RootElement.TryGetProperty("text", out var txt) ? txt.GetString() ?? "" : body;

            var tx = SMSParser.Parse(smsText);
            if (tx != null)
            {
                _onLog($"📨 รับ SMS: {tx.Bank} {tx.Type} ฿{tx.Amount:N0} [{tx.Category}]");
                _onReceive(tx);
            }
            return tx;
        }
        catch
        {
            var tx = SMSParser.Parse(body);
            if (tx != null) { _onLog($"📨 รับ SMS: ฿{tx.Amount:N0} [{tx.Category}]"); _onReceive(tx); }
            return tx;
        }
    }

    private static void WriteResponse(HttpListenerResponse resp, int code, string body, string contentType = "application/json")
    {
        resp.StatusCode = code;
        resp.ContentType = $"{contentType}; charset=utf-8";
        resp.Headers.Add("Access-Control-Allow-Origin", "*");
        var buffer = Encoding.UTF8.GetBytes(body);
        resp.ContentLength64 = buffer.Length;
        resp.OutputStream.Write(buffer, 0, buffer.Length);
        resp.OutputStream.Close();
    }

    private string GetSetupPage()
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width,initial-scale=1'>");
        sb.Append("<title>NoteApp SMS</title><style>*{margin:0;padding:0;box-sizing:border-box}body{font-family:sans-serif;background:#0f0f1a;color:#e0e0e0;padding:20px;max-width:500px;margin:0 auto}");
        sb.Append("h1{font-size:20px;margin-bottom:10px}.card{background:#1a1a2e;border-radius:12px;padding:16px;margin:10px 0}");
        sb.Append(".badge{background:#27ae60;color:white;padding:4px 12px;border-radius:20px;font-size:12px;display:inline-block}");
        sb.Append(".btn{background:#6c63ff;color:white;border:none;padding:12px 24px;border-radius:8px;font-size:14px;margin:5px 0;cursor:pointer;width:100%}");
        sb.Append("textarea{width:100%;height:60px;background:#222240;color:#e0e0e0;border:1px solid #333;border-radius:8px;padding:8px;font-size:13px;margin:8px 0}");
        sb.Append("#result{margin:8px 0;font-size:13px}</style></head><body>");
        sb.Append("<h1>NoteApp SMS Receiver</h1>");
        sb.Append($"<div class='card'><span class='badge'>Connected</span><p style='margin-top:8px'>Port {Port}</p></div>");
        sb.Append("<div class='card'><h3>Send SMS</h3>");
        sb.Append("<textarea id='sms' placeholder='Paste bank SMS here'></textarea>");
        sb.Append("<button class='btn' onclick='send()'>Send</button><div id='result'></div></div>");
        sb.Append("<script>async function send(){const r=document.getElementById('result');try{const res=await fetch('/api/sms',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({message:document.getElementById('sms').value})});const d=await res.json();r.textContent=d.success?'OK: '+d.category+' '+d.amount:'Error: '+d.error;}catch(e){r.textContent='Error: '+e;}}</script>");
        sb.Append("</body></html>");
        return sb.ToString();
    }
}

// ====================================================
// Email SMS Reader (Gmail via IMAP — basic)
// ====================================================
public static class EmailSMSReader
{
    public static string GenerateGmailFilter()
    {
        return @"วิธีตั้งค่า Auto-Forward SMS เข้า Email:

📱 บน Android (ใช้แอป SMS Forwarder):
1. ติดตั้ง 'SMS Forwarder' จาก Play Store
2. สร้าง Rule: Sender contains 'KBank' หรือ 'SCB' ฯลฯ
3. Forward to: Email ของคุณ
4. แอปจะส่ง SMS ธนาคารเข้า Email อัตโนมัติ

📧 บน Gmail:
1. สร้าง Filter: from:(KBank OR SCB OR KTB OR BBL)
2. Apply Label: 'bank-sms'
3. แอปจะดึงจาก Label นี้

🔗 หรือใช้ IFTTT:
1. Trigger: Android SMS → Received from specific number
2. Action: Send Email
3. ตั้งค่าเบอร์ธนาคาร";
    }
}
