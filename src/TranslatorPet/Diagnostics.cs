using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

namespace TranslatorPet { public partial class PetApplication {
    public void Preview(string folder) {
        Directory.CreateDirectory(folder); OpenPopup();
        var checks=new List<string>();
        string expectedAction=Translator.ModelReady ? "翻译  ↵" : "下载模型";
        if(Convert.ToString(TranslateButton.Content)!=expectedAction) throw new Exception("模型状态与按钮不一致");
        checks.Add("PASS: model readiness controls download/translate action");
        string oldSkin=Pref.Skin;
        foreach(string skin in new[]{"orb","cat","robot"}) { ChangeSkin(skin); if(Pet.Content==null) throw new Exception("皮肤加载失败"); }
        ChangeSkin(oldSkin); checks.Add("PASS: all three built-in skins render");
        VerifyCustomSkinReload(folder,checks);
        Select(Source,"zh-CN"); Select(Target,"en");
        Find<Button>(Popup,"SwapButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(((Language)Source.SelectedItem).Code!="en" || ((Language)Target.SelectedItem).Code!="zh-CN") throw new Exception("语言交换失败");
        checks.Add("PASS: language selections and swap");
        Input.Text="before"; Output.Text="after";
        Find<Button>(Popup,"SwapButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(Input.Text!="after" || Output.Text!="before") throw new Exception("文字交换失败");
        Find<Button>(Popup,"ClearButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(Input.Text.Length>0 || Output.Text.Length>0) throw new Exception("清空失败");
        checks.Add("PASS: text swap and clear");
        Find<Button>(Popup,"CloseButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        if(Popup.IsVisible || !Pet.IsVisible) throw new Exception("收起窗口失败");
        OpenPopup(); if(!Popup.IsVisible) throw new Exception("重新打开失败");
        checks.Add("PASS: close hides popup, keeps pet, reopen works");
        Select(Source,"en"); Select(Target,"zh-CN");
        Input.Text="This project is open source."; Output.Text="这个项目是开源的。"; Status.Text="界面预览 · 示例文字";
        Popup.UpdateLayout(); Pet.UpdateLayout();
        Render(Popup,Path.Combine(folder,"translator-window.png")); Render(Pet,Path.Combine(folder,"floating-orb.png"));
        File.WriteAllLines(Path.Combine(folder,"ui-verification.txt"),checks,Encoding.UTF8); Quit();
    }
    static void Render(Window window,string path) {
        var bmp=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32); bmp.Render(window);
        var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bmp)); using(var f=File.Create(path)) encoder.Save(f);
    }
    void VerifyCustomSkinReload(string folder,List<string> checks) {
        string originalSkin=Pref.Skin;
        string scratch=Path.Combine(folder,"skin-reload-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        try {
            foreach(string extension in new[]{".png",".jpg",".bmp",".gif"}) {
                string path=Path.Combine(scratch,"custom-skin"+extension);
                BitmapSource previous=null;
                foreach(Color color in new[]{Colors.Red,Colors.Blue,Colors.Lime}) {
                    BitmapEncoder encoder=extension==".jpg" ? (BitmapEncoder)new JpegBitmapEncoder() : extension==".bmp" ? new BmpBitmapEncoder() : extension==".gif" ? new GifBitmapEncoder() : (BitmapEncoder)new PngBitmapEncoder();
                    var pixels=new byte[16*16*4];
                    for(int i=0;i<pixels.Length;i+=4) { pixels[i]=color.B; pixels[i+1]=color.G; pixels[i+2]=color.R; pixels[i+3]=255; }
                    encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(16,16,96,96,PixelFormats.Bgra32,null,pixels,16*4)));
                    using(var file=File.Create(path)) encoder.Save(file);
                    // Keep the old image alive, as it is while an import overwrites its path.
                    ChangeSkin(path);
                    var bitmap=(BitmapSource)((Image)((Border)Pet.Content).Child).Source;
                    var converted=new FormatConvertedBitmap(bitmap,PixelFormats.Bgra32,null,0);
                    var actual=new byte[4]; converted.CopyPixels(new Int32Rect(0,0,1,1),actual,4,0);
                    if(Math.Abs(actual[0]-color.B)>10 || Math.Abs(actual[1]-color.G)>10 || Math.Abs(actual[2]-color.R)>10) throw new Exception("Custom skin displays stale pixels: "+extension);
                    using(var file=File.Open(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None)) { }
                    GC.KeepAlive(previous); previous=bitmap;
                }
                ChangeSkin("orb"); ChangeSkin(path);
                if(!(((Border)Pet.Content).Child is Image)) throw new Exception("Custom skin reload failed");
                File.Delete(path);
            }
            checks.Add("PASS: PNG/JPG/BMP/GIF skins update on three consecutive same-path replacements without locking files");
        } finally { ChangeSkin(originalSkin); Directory.Delete(scratch,true); }
    }
}}
