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
    public static async Task<string> Translate(string text,string source,string target,Action<string> progress,CancellationToken ct) {
        ct.ThrowIfCancellationRequested();
        if(source==target) return text;
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
            progress("正在加载本地翻译模型 …");
            process.Start();
            Task<string> errorTask=process.StandardError.ReadToEndAsync();
            using(ct.Register(()=> { try { if(!process.HasExited) process.Kill(); } catch(InvalidOperationException) { } })) {
                try {
                    string request=new JavaScriptSerializer().Serialize(new { text=text, source=source, target=target });
                    byte[] bytes=Encoding.UTF8.GetBytes(request+"\n");
                    await process.StandardInput.BaseStream.WriteAsync(bytes,0,bytes.Length,ct);
                    process.StandardInput.Close();
                    string result=null, failure=null, line;
                    while((line=await process.StandardOutput.ReadLineAsync())!=null) {
                        ct.ThrowIfCancellationRequested();
                        var message=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(line);
                        string type=Convert.ToString(message["type"]);
                        if(type=="progress") progress(Convert.ToString(message["message"]));
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
