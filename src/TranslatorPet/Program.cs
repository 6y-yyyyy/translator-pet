using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;

namespace TranslatorPet {
public static class Profile {
    public static string Directory {
        get {
            string root=AppDomain.CurrentDomain.BaseDirectory;
            string path=File.Exists(Path.Combine(root,"portable.marker")) ? Path.Combine(root,"data") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TranslatorPet");
            System.IO.Directory.CreateDirectory(path); return path;
        }
    }
}
public static class Program {
    [STAThread] public static void Main(string[] args) {
        bool preview=args.Length>0 && args[0]=="--preview";
        try {
            bool owned;
            using(var mutex=new Mutex(true,"Local\\TranslatorPet-Offline-v02",out owned)) {
                if(!owned && !preview) { MessageBox.Show("随身翻译已在运行，请点击桌面悬浮球或系统托盘图标。","随身翻译"); return; }
                var app=new PetApplication();
                app.DispatcherUnhandledException+=(s,e)=> {
                    if(preview) { File.WriteAllText(Path.Combine(Profile.Directory,"diagnostics.txt"),e.Exception.ToString(),Encoding.UTF8); Environment.Exit(1); }
                    e.Handled=true; MessageBox.Show("发生错误："+e.Exception.Message,"随身翻译");
                };
                app.Initialize(preview);
                if(preview) app.Dispatcher.BeginInvoke(new Action(()=>app.Preview(args.Length>1 ? args[1] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview"))),System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                app.Run();
            }
        } catch(Exception e) {
            try { File.WriteAllText(Path.Combine(Profile.Directory,"diagnostics.txt"),e.ToString(),Encoding.UTF8); } catch { }
            Environment.ExitCode=1;
            if(!preview) MessageBox.Show("启动失败："+e.Message,"随身翻译");
        }
    }
}
}
