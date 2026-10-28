using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace NoteApp;

public class CommunityPost
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Author { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Category { get; set; } = "ทั่วไป";
    public string Created { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    public int Likes { get; set; }
    public bool Pinned { get; set; }
    public List<CommunityReply> Replies { get; set; } = new();
}

public class CommunityReply
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Author { get; set; } = "";
    public string Content { get; set; } = "";
    public string Created { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    public int Likes { get; set; }
}

public static class CommunityStore
{
    private static readonly string FilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "community.json");
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static List<CommunityPost> Load()
    {
        if (!File.Exists(FilePath)) return new();
        try { return JsonSerializer.Deserialize<List<CommunityPost>>(File.ReadAllText(FilePath), Opts) ?? new(); }
        catch { return new(); }
    }

    public static void Save(List<CommunityPost> posts)
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(posts, Opts));
    }
}

public class CommunityServer
{
    private HttpListener? _listener;
    private readonly List<CommunityPost> _posts;
    private readonly Action _onUpdate;
    public bool IsRunning { get; private set; }
    public int Port { get; }

    public CommunityServer(int port, List<CommunityPost> posts, Action onUpdate)
    {
        Port = port;
        _posts = posts;
        _onUpdate = onUpdate;
    }

    public void Start()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{Port}/");
        _listener.Start();
        IsRunning = true;
        System.Threading.Tasks.Task.Run(Listen);
    }

    public void Stop()
    {
        IsRunning = false;
        try { _listener?.Stop(); } catch { }
    }

    private async System.Threading.Tasks.Task Listen()
    {
        while (IsRunning && _listener?.IsListening == true)
        {
            try
            {
                var ctx = await _listener.GetContextAsync();
                HandleRequest(ctx);
            }
            catch { if (!IsRunning) break; }
        }
    }

    private void HandleRequest(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var resp = ctx.Response;
        resp.ContentType = "text/html; charset=utf-8";
        resp.Headers.Add("Access-Control-Allow-Origin", "*");

        string body;
        if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/api/post")
        {
            using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
            var json = reader.ReadToEnd();
            try
            {
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var post = new CommunityPost
                {
                    Author = root.GetProperty("author").GetString() ?? "ไม่ระบุชื่อ",
                    Title = root.GetProperty("title").GetString() ?? "",
                    Content = root.GetProperty("content").GetString() ?? "",
                    Category = root.TryGetProperty("category", out var cat) ? cat.GetString() ?? "ทั่วไป" : "ทั่วไป",
                };
                _posts.Insert(0, post);
                CommunityStore.Save(_posts);
                _onUpdate();
                body = "{\"ok\":true}";
                resp.ContentType = "application/json";
            }
            catch { body = "{\"ok\":false}"; resp.ContentType = "application/json"; }
        }
        else if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/api/reply")
        {
            using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
            var json = reader.ReadToEnd();
            try
            {
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var postId = root.GetProperty("postId").GetString();
                var post = _posts.FirstOrDefault(p => p.Id == postId);
                if (post != null)
                {
                    post.Replies.Add(new CommunityReply
                    {
                        Author = root.GetProperty("author").GetString() ?? "ไม่ระบุชื่อ",
                        Content = root.GetProperty("content").GetString() ?? "",
                    });
                    CommunityStore.Save(_posts);
                    _onUpdate();
                }
                body = "{\"ok\":true}";
                resp.ContentType = "application/json";
            }
            catch { body = "{\"ok\":false}"; resp.ContentType = "application/json"; }
        }
        else if (req.Url?.AbsolutePath == "/api/posts")
        {
            body = JsonSerializer.Serialize(_posts.Take(50), new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            resp.ContentType = "application/json";
        }
        else
        {
            body = GenerateWebPage();
        }

        var buf = Encoding.UTF8.GetBytes(body);
        resp.ContentLength64 = buf.Length;
        resp.OutputStream.Write(buf, 0, buf.Length);
        resp.Close();
    }

    private string GenerateWebPage()
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang='th'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width,initial-scale=1'>");
        sb.Append("<title>NoteApp Community</title><style>");
        sb.Append("*{margin:0;padding:0;box-sizing:border-box}");
        sb.Append("body{font-family:system-ui,sans-serif;background:#13151c;color:#d8dae2;max-width:640px;margin:0 auto;padding:16px}");
        sb.Append("h1{font-size:20px;font-weight:700;margin-bottom:4px}");
        sb.Append(".sub{color:#7d8298;font-size:12px;margin-bottom:20px}");
        sb.Append(".card{background:#1c1f2a;border-radius:10px;padding:16px;margin-bottom:10px}");
        sb.Append(".card h3{font-size:15px;margin-bottom:6px}");
        sb.Append(".meta{color:#7d8298;font-size:11px;margin-bottom:8px}");
        sb.Append(".badge{display:inline-block;background:#272b38;padding:2px 8px;border-radius:6px;font-size:10px;color:#7d8298;margin-right:4px}");
        sb.Append(".content{font-size:13px;line-height:1.7;white-space:pre-wrap;color:#b0b3c6}");
        sb.Append(".reply{background:#272b38;border-radius:8px;padding:10px;margin:6px 0 0 20px;font-size:12px}");
        sb.Append(".reply .meta{margin-bottom:4px}");
        sb.Append("input,textarea{width:100%;padding:10px;background:#272b38;border:1px solid #303345;border-radius:8px;color:#d8dae2;font-size:13px;font-family:inherit;margin-bottom:8px}");
        sb.Append("input:focus,textarea:focus{outline:none;border-color:#7b93f7}");
        sb.Append("textarea{min-height:80px;resize:vertical}");
        sb.Append("button{padding:10px 20px;background:#7b93f7;color:#fff;border:none;border-radius:8px;font-size:13px;font-weight:600;cursor:pointer}");
        sb.Append("button:hover{opacity:0.85}");
        sb.Append(".form{background:#1c1f2a;border-radius:10px;padding:16px;margin-bottom:16px}");
        sb.Append(".likes{color:#7b93f7;font-size:12px;cursor:pointer}");
        sb.Append("</style></head><body>");
        sb.Append("<h1>NoteApp Community</h1>");
        sb.Append($"<div class='sub'>{_posts.Count} โพสต์ · เชื่อมผ่าน WiFi เดียวกัน</div>");

        sb.Append("<div class='form'><h3 style='font-size:14px;margin-bottom:10px;color:#7b93f7'>โพสต์ใหม่</h3>");
        sb.Append("<input id='author' placeholder='ชื่อของคุณ'>");
        sb.Append("<input id='title' placeholder='หัวข้อ'>");
        sb.Append("<textarea id='content' placeholder='เนื้อหา...'></textarea>");
        sb.Append("<button onclick='submitPost()'>โพสต์</button></div>");

        foreach (var p in _posts.Take(30))
        {
            sb.Append("<div class='card'>");
            sb.Append($"<h3>{Esc(p.Title)}</h3>");
            sb.Append($"<div class='meta'><span class='badge'>{Esc(p.Category)}</span> {Esc(p.Author)} · {Esc(p.Created)} · {p.Replies.Count} ตอบ</div>");
            sb.Append($"<div class='content'>{Esc(p.Content)}</div>");

            foreach (var r in p.Replies)
            {
                sb.Append($"<div class='reply'><div class='meta'>{Esc(r.Author)} · {Esc(r.Created)}</div>{Esc(r.Content)}</div>");
            }

            sb.Append($"<div style='margin-top:8px'><input id='ra_{p.Id}' placeholder='ตอบกลับ...' style='width:calc(100% - 70px);display:inline-block'>");
            sb.Append($"<button onclick=\"submitReply('{p.Id}')\" style='padding:8px 14px;font-size:12px;margin-left:4px'>ตอบ</button></div>");
            sb.Append("</div>");
        }

        sb.Append("<script>");
        sb.Append("async function submitPost(){");
        sb.Append("const a=document.getElementById('author').value||'ไม่ระบุชื่อ';");
        sb.Append("const t=document.getElementById('title').value;const c=document.getElementById('content').value;");
        sb.Append("if(!t){alert('กรุณาใส่หัวข้อ');return}");
        sb.Append("await fetch('/api/post',{method:'POST',body:JSON.stringify({author:a,title:t,content:c})});location.reload()}");
        sb.Append("async function submitReply(id){");
        sb.Append("const el=document.getElementById('ra_'+id);const c=el.value;if(!c)return;");
        sb.Append("const a=document.getElementById('author').value||'ไม่ระบุชื่อ';");
        sb.Append("await fetch('/api/reply',{method:'POST',body:JSON.stringify({postId:id,author:a,content:c})});location.reload()}");
        sb.Append("</script></body></html>");
        return sb.ToString();
    }

    private static string Esc(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("\n", "<br>");
}
