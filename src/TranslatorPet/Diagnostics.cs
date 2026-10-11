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
}}
