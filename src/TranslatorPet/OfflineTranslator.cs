using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace TranslatorPet {
// Protocol: one JSON request on stdin; JSON progress/result messages on stdout.
// No HTTP service, listening port, account or remote translation fallback.
public static class Translator {
    public static bool ModelReady {
        get {
            try {
                string root=AppDomain.CurrentDomain.BaseDirectory;
                var model=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(root,"model.json"),Encoding.UTF8));
                string name=Convert.ToString(model["name"]);
                if(Path.GetFileName(name)!=name) return false;
                var file=new FileInfo(Path.Combine(root,"models",name));
                return file.Exists && file.Length==Convert.ToInt64(model["size"]);
            } catch { return false; }
        }
    }
    public static Task<string> Translate(string text,string source,string target,Action<string> progress,CancellationToken ct,Action<string> partial=null) {
        ct.ThrowIfCancellationRequested();
        if(source==target) return Task.FromResult(text);
        return Run(new { action="translate", text=text, source=source, target=target },progress,ct,partial);
    }
    public static Task<string> Prepare(Action<string> progress,CancellationToken ct) {
        return Run(new { action="prepare" },progress,ct,null);
    }
    static async Task<string> Run(object payload,Action<string> progress,CancellationToken ct,Action<string> partial) {
        ct.ThrowIfCancellationRequested();
        string root=AppDomain.CurrentDomain.BaseDirectory;
        string python=Path.Combine(root,"runtime","python.exe");
        string worker=Path.Combine(root,"engine","worker.py");
        if(!File.Exists(python) || !File.Exists(worker)) throw new Exception("本地引擎不完整，请解压完整离线包，或按 README 安装模型和运行环境。");
        var start=new ProcessStartInfo {
            FileName=python, Arguments="-I -X utf8 \""+worker+"\"", WorkingDirectory=root,
            UseShellExecute=false, CreateNoWindow=true, RedirectStandardInput=true,
            RedirectStandardOutput=true, RedirectStandardError=true,
            StandardOutputEncoding=Encoding.UTF8, StandardErrorEncoding=Encoding.UTF8
        };
        // Keep input out of command lines, environment variables, and disk files.
        using(var process=new Process { StartInfo=start }) {
            progress("正在启动本地引擎 …");
            process.Start();
            Task<string> errorTask=process.StandardError.ReadToEndAsync();
            using(ct.Register(()=> { try { if(!process.HasExited) process.Kill(); } catch(InvalidOperationException) { } })) {
                try {
                    string request=new JavaScriptSerializer().Serialize(payload);
                    byte[] bytes=Encoding.UTF8.GetBytes(request+"\n");
                    await process.StandardInput.BaseStream.WriteAsync(bytes,0,bytes.Length,ct);
                    process.StandardInput.Close();
                    string result=null, failure=null, line;
                    while((line=await process.StandardOutput.ReadLineAsync())!=null) {
                        ct.ThrowIfCancellationRequested();
                        var message=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(line);
                        string type=Convert.ToString(message["type"]);
                        if(type=="progress") progress(Convert.ToString(message["message"]));
                        else if(type=="partial" && partial!=null) partial(Convert.ToString(message["text"]));
                        else if(type=="result") result=Convert.ToString(message["text"]);
                        else if(type=="error") failure=Convert.ToString(message["message"]);
                    }
                    await Task.Run(()=>process.WaitForExit());
                    await errorTask; // Drain native diagnostics without storing user text.
                    ct.ThrowIfCancellationRequested();
                    if(failure!=null) throw new Exception(failure);
                    if(process.ExitCode!=0 || result==null) throw new Exception("本地引擎启动失败。请确认使用 Windows 10/11 x64，并安装 Microsoft Visual C++ 2015–2022 x64 运行库。详见 README。");
                    return result;
                } catch {
                    try { if(!process.HasExited) process.Kill(); } catch(InvalidOperationException) { }
                    ct.ThrowIfCancellationRequested(); throw;
                }
            }
        }
    }
}
}
