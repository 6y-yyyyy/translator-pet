using System;
using System.Threading;
using TranslatorPet;

public static class InteropTests {
    public static int Main() {
        try {
            Console.InputEncoding=new System.Text.UTF8Encoding(true);
            Environment.SetEnvironmentVariable("TRANSLATORPET_DISABLE_NETWORK","1");
            bool receivedPartial=false;
            string en=Translator.Translate("This project is open source.","en","zh-CN",s=>{},CancellationToken.None,s=>{ if(s.Length>0) receivedPartial=true; }).GetAwaiter().GetResult();
            if(!en.Contains("开源")) throw new Exception("English to Chinese failed");
            if(!receivedPartial) throw new Exception("Streaming result missing");
            Translator.Prepare(s=>{},CancellationToken.None).GetAwaiter().GetResult();
            string zh=Translator.Translate("请阅读安装说明。","zh-CN","en",s=>{},CancellationToken.None).GetAwaiter().GetResult();
            if(!zh.ToLowerInvariant().Contains("install")) throw new Exception("Chinese UTF-8 roundtrip failed");
            using(var ct=new CancellationTokenSource(100)) {
                try { Translator.Translate(new string('a',9000),"en","zh-CN",s=>{},ct.Token).GetAwaiter().GetResult(); throw new Exception("Cancellation did not occur"); }
                catch(OperationCanceledException) { }
            }
            using(var ct=new CancellationTokenSource()) {
                try { Translator.Translate("Explain how nested callbacks affect event execution order.","en","zh-CN",s=>{},ct.Token,s=>{ if(s.Length>0) ct.Cancel(); }).GetAwaiter().GetResult(); throw new Exception("Streaming cancellation did not occur"); }
                catch(OperationCanceledException) { }
            }
            Console.WriteLine("PASS: C# -> worker offline translation, Unicode, model verification, streaming, cancellation");
            return 0;
        } catch(Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
