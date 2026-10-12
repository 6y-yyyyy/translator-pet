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

namespace TranslatorPet {
public partial class PetApplication : Application {
    public Preferences Pref;
    public Window Pet, Popup;
    public TextBox Input, Output;
    public ComboBox Source, Target;
    TextBlock Status, Count;
    Button TranslateButton, PasteButton, SwapButton, CopyButton;
    CancellationTokenSource Pending;
    Forms.NotifyIcon Tray;
    bool Quitting, Dragged, IsPreview;
    Point DragStart;
    readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
    public string SettingsPath { get { return Path.Combine(Profile.Directory,"preferences.json"); } }
    public static readonly Language[] Languages={new Language("en","英语"),new Language("zh-CN","简体中文")};
    public PetApplication() {
        ShutdownMode=ShutdownMode.OnExplicitShutdown;
        Pref=new Preferences();
        try { if(File.Exists(SettingsPath)) Pref=new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(SettingsPath,Encoding.UTF8)) ?? Pref; } catch { }
        if(Pref.Size!=40 && Pref.Size!=48 && Pref.Size!=64) Pref.Size=48;
    }
    static T Find<T>(Window w,string name) where T:class { return (T)w.FindName(name); }
    public void Initialize(bool preview) {
        IsPreview=preview;
        Pet=new Window { Title="随身翻译 · 悬浮球", Width=Pref.Size+12, Height=Pref.Size+12, AllowsTransparency=true, WindowStyle=WindowStyle.None, Background=Brushes.Transparent, ShowInTaskbar=false, Topmost=true, ResizeMode=ResizeMode.NoResize };
        Pet.Left=Pref.X<0 || double.IsNaN(Pref.X) || double.IsInfinity(Pref.X) ? SystemParameters.WorkArea.Right-Pet.Width-24 : Pref.X;
        Pet.Top=Pref.Y<0 || double.IsNaN(Pref.Y) || double.IsInfinity(Pref.Y) ? SystemParameters.WorkArea.Bottom-Pet.Height-24 : Pref.Y;
        SetSkin();
        Pet.MouseLeftButtonDown+=(s,e)=> { DragStart=e.GetPosition(Pet); Dragged=false; Pet.CaptureMouse(); e.Handled=true; };
        Pet.MouseMove+=(s,e)=> {
            if(!Pet.IsMouseCaptured || e.LeftButton!=MouseButtonState.Pressed) return;
            Point p=e.GetPosition(Pet);
            double dx=p.X-DragStart.X, dy=p.Y-DragStart.Y;
            if(!Dragged && Math.Abs(dx)+Math.Abs(dy)<5) return;
            Dragged=true; Pet.Left+=dx; Pet.Top+=dy;
        };
        Pet.MouseLeftButtonUp+=(s,e)=> { if(!Pet.IsMouseCaptured) return; Pet.ReleaseMouseCapture(); if(Dragged) { ClampPet(); Save(); } else Toggle(); };
        Pet.Closing+=(s,e)=> { if(!Quitting) { e.Cancel=true; Quit(); } };
        Pet.ContextMenu=BuildMenu();
        Popup=(Window)XamlReader.Parse(PopupXaml);
        Input=Find<TextBox>(Popup,"InputBox"); Output=Find<TextBox>(Popup,"OutputBox");
        Source=Find<ComboBox>(Popup,"SourceLanguage"); Target=Find<ComboBox>(Popup,"TargetLanguage");
        Status=Find<TextBlock>(Popup,"StatusText"); Count=Find<TextBlock>(Popup,"CountText");
        TranslateButton=Find<Button>(Popup,"TranslateButton"); PasteButton=Find<Button>(Popup,"PasteButton");
        SwapButton=Find<Button>(Popup,"SwapButton"); CopyButton=Find<Button>(Popup,"CopyButton");
        TranslateButton.Content=Translator.ModelReady ? "翻译  ↵" : "下载模型";
        if(!Translator.ModelReady) Status.Text="首次使用请先下载模型（约 2.5 GB）";
        foreach(var lang in Languages) { Source.Items.Add(lang); Target.Items.Add(lang); }
        Select(Source,Pref.Source); Select(Target,Pref.Target);
        Source.SelectionChanged+=(s,e)=>Save(); Target.SelectionChanged+=(s,e)=>Save();
        Input.TextChanged+=(s,e)=> { Count.Text=Input.Text.Length.ToString("N0")+" / 10,000"; };
        Find<Button>(Popup,"CloseButton").Click+=(s,e)=>Popup.Hide();
        Find<Button>(Popup,"ClearButton").Click+=(s,e)=> { Input.Clear(); Output.Clear(); Status.Text="已清空"; Input.Focus(); };
        Find<Button>(Popup,"MoreButton").Click+=(s,e)=> { var menu=BuildMenu(); menu.PlacementTarget=(Button)s; menu.IsOpen=true; };
        Find<Border>(Popup,"TitleBar").MouseLeftButtonDown+=(s,e)=> { if(e.OriginalSource is TextBlock || e.OriginalSource is Border) Popup.DragMove(); };
        Popup.Closing+=(s,e)=> { if(!Quitting) { e.Cancel=true; Popup.Hide(); } };
        PasteButton.Click+=(s,e)=>Paste(); CopyButton.Click+=(s,e)=>Copy();
        SwapButton.Click+=(s,e)=> { int a=Source.SelectedIndex; Source.SelectedIndex=Target.SelectedIndex; Target.SelectedIndex=a; if(Output.Text.Length>0) { string t=Input.Text; Input.Text=Output.Text; Output.Text=t; } Status.Text="已交换语言"; };
        TranslateButton.Click+=async(s,e)=> { if(Pending!=null) Pending.Cancel(); else await RunTranslation(); };
        Popup.PreviewKeyDown+=async(s,e)=> {
            if(e.Key==Key.Escape) { Popup.Hide(); e.Handled=true; }
            if(e.Key==Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control)!=0) { e.Handled=true; if(Pending==null) await RunTranslation(); }
        };
        if(!preview) {
            Tray=new Forms.NotifyIcon { Icon=System.Drawing.SystemIcons.Information, Text="随身翻译 · 点击悬浮球打开", Visible=true };
            var menu=new Forms.ContextMenuStrip();
            menu.Items.Add("打开翻译窗口",null,(s,e)=>Dispatcher.Invoke(new Action(OpenPopup)));
            menu.Items.Add("显示 / 隐藏悬浮球",null,(s,e)=>Dispatcher.Invoke(new Action(()=> { if(Pet.IsVisible) Pet.Hide(); else Pet.Show(); })));
            menu.Items.Add("重置悬浮球位置",null,(s,e)=>Dispatcher.Invoke(new Action(ResetPosition)));
            menu.Items.Add("退出",null,(s,e)=>Dispatcher.Invoke(new Action(Quit)));
            Tray.ContextMenuStrip=menu; Tray.DoubleClick+=(s,e)=>Dispatcher.Invoke(new Action(OpenPopup));
        }
        Pet.Show(); ClampPet();
    }
    public void Select(ComboBox box,string code) { for(int i=0;i<Languages.Length;i++) if(Languages[i].Code==code) { box.SelectedIndex=i; return; } box.SelectedIndex=0; }
    public void Save() {
        if(IsPreview) return;
        if(Source!=null && Source.SelectedItem!=null) Pref.Source=((Language)Source.SelectedItem).Code;
        if(Target!=null && Target.SelectedItem!=null) Pref.Target=((Language)Target.SelectedItem).Code;
        if(Pet!=null) { Pref.X=Pet.Left; Pref.Y=Pet.Top; }
        try { File.WriteAllText(SettingsPath,new JavaScriptSerializer().Serialize(Pref),Encoding.UTF8); } catch { }
    }
    void ClampPet() {
        var point=Pet.PointToScreen(new Point(0,0));
        var screen=Forms.Screen.FromPoint(new System.Drawing.Point((int)point.X,(int)point.Y));
        var area=screen.WorkingArea;
        var dpi=PresentationSource.FromVisual(Pet);
        double scale=dpi==null ? 1 : dpi.CompositionTarget.TransformToDevice.M11;
        double left=area.Left/scale, top=area.Top/scale, right=area.Right/scale, bottom=area.Bottom/scale;
        Pet.Left=Math.Max(left,Math.Min(Pet.Left,right-Pet.Width));
        Pet.Top=Math.Max(top,Math.Min(Pet.Top,bottom-Pet.Height));
    }
    public void ResetPosition() { Pref.X=Pref.Y=-1; Pet.Left=SystemParameters.WorkArea.Right-Pet.Width-24; Pet.Top=SystemParameters.WorkArea.Bottom-Pet.Height-24; Pet.Show(); ClampPet(); Save(); }
    void Toggle() { if(Popup.IsVisible) Popup.Hide(); else OpenPopup(); }
    public void OpenPopup() {
        double left=Pet.Left-Popup.Width-10, top=Pet.Top-Popup.Height+Pet.Height;
        // Popup is placed in the work area of the pet's monitor using WPF logical coordinates.
        var p=Pet.PointToScreen(new Point(0,0));
        var monitor=Forms.Screen.FromPoint(new System.Drawing.Point((int)p.X,(int)p.Y)).WorkingArea;
        var src=PresentationSource.FromVisual(Pet);
        double scale=src==null ? 1 : src.CompositionTarget.TransformToDevice.M11;
        if(left<monitor.Left/scale) left=Pet.Left+Pet.Width+10;
        Popup.Left=Math.Max(monitor.Left/scale,Math.Min(left,monitor.Right/scale-Popup.Width));
        Popup.Top=Math.Max(monitor.Top/scale,Math.Min(top,monitor.Bottom/scale-Popup.Height));
        Popup.Show(); Popup.Activate(); Input.Focus();
    }
    void Paste() {
        try { if(Clipboard.ContainsText()) { Input.Text=Clipboard.GetText(); Input.CaretIndex=Input.Text.Length; Status.Text="已粘贴，点击翻译或按 Ctrl+Enter"; } else Status.Text="剪贴板里没有文字"; }
        catch { Status.Text="剪贴板暂时被占用，请稍后再试"; }
        Input.Focus();
    }
    void Copy() {
        if(string.IsNullOrWhiteSpace(Output.Text)) { Status.Text="还没有可复制的译文"; return; }
        try { Clipboard.SetText(Output.Text); Status.Text="译文已复制"; } catch { Status.Text="剪贴板暂时被占用，请稍后再试"; }
    }
    async Task RunTranslation(bool verifyModel=false) {
        bool preparing=verifyModel || !Translator.ModelReady;
        string text=Input.Text;
        if(!preparing && string.IsNullOrWhiteSpace(text)) { Status.Text="先输入或粘贴需要翻译的文字"; Input.Focus(); return; }
        if(!preparing && text.Length>10000) { Status.Text="单次最多 10,000 字，请缩短文字后重试"; return; }
        var pending=new CancellationTokenSource(preparing ? TimeSpan.FromHours(2) : TimeSpan.FromMinutes(15)); Pending=pending;
        Input.IsReadOnly=true; Source.IsEnabled=Target.IsEnabled=PasteButton.IsEnabled=SwapButton.IsEnabled=false;
        Find<Button>(Popup,"ClearButton").IsEnabled=false; CopyButton.IsEnabled=false;
        TranslateButton.Content="取消"; Output.Clear();
        try {
            if(preparing) {
                await Translator.Prepare(s=>Status.Text=s,pending.Token);
                Status.Text="模型已就绪，点击翻译即可离线使用";
            } else {
                Output.Text=await Translator.Translate(text,((Language)Source.SelectedItem).Code,((Language)Target.SelectedItem).Code,s=>Status.Text=s,pending.Token,s=>Output.Text=s);
                Status.Text="翻译完成 · Qwen 增强离线模型";
            }
        } catch(OperationCanceledException) { Output.Clear(); Status.Text=preparing ? "下载已取消，重试时继续下载" : "翻译已取消或超时，未完成的译文已清除"; }
        catch(Exception e) { Output.Clear(); Status.Text=e is ArgumentException || e is InvalidOperationException ? "本地引擎响应异常，请重试" : e.Message; }
        finally {
            Pending=null; pending.Dispose(); Input.IsReadOnly=false;
            Source.IsEnabled=Target.IsEnabled=PasteButton.IsEnabled=SwapButton.IsEnabled=CopyButton.IsEnabled=true;
            Find<Button>(Popup,"ClearButton").IsEnabled=true; TranslateButton.Content=Translator.ModelReady ? "翻译  ↵" : "下载模型";
        }
    }
    ContextMenu BuildMenu() {
        var menu=new ContextMenu();
        Add(menu,"打开翻译窗口",OpenPopup);
        var skins=new MenuItem { Header="桌宠形象" };
        Add(skins,"悬浮圆球",()=>ChangeSkin("orb")); Add(skins,"小猫",()=>ChangeSkin("cat")); Add(skins,"小机器人",()=>ChangeSkin("robot"));
        Add(skins,"导入图片…",ImportSkin); menu.Items.Add(skins);
        var sizes=new MenuItem { Header="桌宠大小" };
        foreach(int n in new[]{40,48,64}) { int size=n; Add(sizes,n+" 像素"+(n==48 ? "（默认）" : ""),()=> { Pref.Size=size; Pet.Width=Pet.Height=size+12; ClampPet(); Save(); }); }
        menu.Items.Add(sizes); Add(menu,"重置位置",ResetPosition);
        Add(menu,"下载 / 校验离线模型",async()=> { OpenPopup(); if(Pending==null) await RunTranslation(true); });
        menu.Items.Add(new Separator()); Add(menu,"使用说明",()=> { MessageBox.Show("点击圆球：展开 / 收起翻译窗口\n按住拖动：移动圆球\n右键圆球：换形象、调大小、退出\nCtrl+Enter：翻译；Esc：收起窗口\n\n默认英语 → 简体中文，两种语言均可选择。\n中英翻译在本机完成，不上传原文，无每日次数额度。首次使用请点击下载模型（约 2.5 GB）。增强模型更占内存，生成译文需要一些时间。不会自动读取剪贴板。\n\n自定义皮肤支持 PNG、JPG、BMP、GIF；推荐透明 PNG。第一版为静态皮肤，GIF 仅显示一帧。\n关闭翻译窗口不会退出程序，退出请使用右键菜单或系统托盘。", "随身翻译 · 使用说明"); });
        Add(menu,"退出",Quit); return menu;
    }
    static void Add(ItemsControl parent,string title,Action action) { var m=new MenuItem { Header=title }; m.Click+=(s,e)=>action(); parent.Items.Add(m); }
    void ImportSkin() {
        var picker=new Microsoft.Win32.OpenFileDialog { Title="选择桌宠图片（推荐透明 PNG）", Filter="图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|PNG 图片|*.png" };
        if(picker.ShowDialog()!=true) return;
        try {
            if(new FileInfo(picker.FileName).Length>20*1024*1024) throw new Exception("请使用小于 20 MB 的图片。");
            LoadBitmap(picker.FileName);
            string dest=Path.Combine(Profile.Directory,"custom-skin"+Path.GetExtension(picker.FileName).ToLowerInvariant());
            if(!string.Equals(Path.GetFullPath(picker.FileName),Path.GetFullPath(dest),StringComparison.OrdinalIgnoreCase)) File.Copy(picker.FileName,dest,true);
            ChangeSkin(Path.GetFileName(dest));
        } catch(Exception e) { MessageBox.Show("导入失败："+e.Message,"随身翻译"); }
    }
    static BitmapImage LoadBitmap(string path) {
        // Imported skins overwrite the same filename. Read bytes directly so WPF's
        // URI image cache cannot return the previous skin for that filename.
        using(var stream=File.OpenRead(Path.GetFullPath(path))) {
            var b=new BitmapImage(); b.BeginInit(); b.CacheOption=BitmapCacheOption.OnLoad;
            b.StreamSource=stream; b.DecodePixelWidth=512; b.EndInit(); b.Freeze(); return b;
        }
    }
    void ChangeSkin(string skin) { Pref.Skin=skin; SetSkin(); Save(); }
    void SetSkin() {
        FrameworkElement art=null;
        if(Pref.Skin!="orb" && Pref.Skin!="cat" && Pref.Skin!="robot") {
            try { art=new Image { Source=LoadBitmap(Path.IsPathRooted(Pref.Skin) ? Pref.Skin : Path.Combine(Profile.Directory,Pref.Skin)), Stretch=Stretch.Uniform }; } catch { Pref.Skin="orb"; }
        }
        if(art==null) art=(FrameworkElement)XamlReader.Parse(Pref.Skin=="cat" ? CatXaml : Pref.Skin=="robot" ? RobotXaml : OrbXaml);
        var border=new Border { Background=Brushes.Transparent, Padding=new Thickness(6), Child=art, ToolTip="随身翻译\n点击打开 · 拖动移动 · 右键设置", Cursor=Cursors.Hand };
        Pet.Content=border;
    }
    public void Quit() { if(Quitting) return; Quitting=true; if(Pending!=null) Pending.Cancel(); Save(); if(Tray!=null) { Tray.Visible=false; Tray.Dispose(); } Shutdown(); }
    const string OrbXaml=@"<Viewbox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Grid Width='48' Height='48'><Ellipse Margin='2'><Ellipse.Fill><LinearGradientBrush StartPoint='0,0' EndPoint='1,1'><GradientStop Color='#A5AEFF'/><GradientStop Color='#7465E8' Offset='1'/></LinearGradientBrush></Ellipse.Fill><Ellipse.Effect><DropShadowEffect Color='#6655B4' Opacity='0.3' BlurRadius='6' ShadowDepth='2'/></Ellipse.Effect></Ellipse><Ellipse Margin='6,5,20,28' Fill='#35FFFFFF'/><TextBlock Text='文' Foreground='White' FontSize='19' FontWeight='SemiBold' HorizontalAlignment='Center' VerticalAlignment='Center' Margin='0,0,6,2'/><TextBlock Text='A' Foreground='#DDFFFFFF' FontSize='12' FontWeight='Bold' HorizontalAlignment='Right' VerticalAlignment='Bottom' Margin='0,0,9,9'/></Grid></Viewbox>";
    const string CatXaml=@"<Viewbox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Canvas Width='48' Height='48'><Path Data='M 8,21 L 7,5 L 20,13 L 29,13 L 41,5 L 40,22 Z' Fill='#F8E4CF' Stroke='#C6A8AC' StrokeThickness='1.3'/><Ellipse Canvas.Left='6' Canvas.Top='11' Width='36' Height='30' Fill='#FFF1E0' Stroke='#C6A8AC' StrokeThickness='1.3'/><Ellipse Canvas.Left='14' Canvas.Top='23' Width='4' Height='5' Fill='#4B445B'/><Ellipse Canvas.Left='30' Canvas.Top='23' Width='4' Height='5' Fill='#4B445B'/><Path Data='M 22,29 L 26,29 L 24,32 Z' Fill='#DF9CAA'/><Path Data='M 13,40 Q 24,47 36,40' Stroke='#9C8EEB' StrokeThickness='5'/></Canvas></Viewbox>";
    const string RobotXaml=@"<Viewbox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Canvas Width='48' Height='48'><Path Data='M 24,11 L 24,5' Stroke='#9A8EEB' StrokeThickness='3'/><Ellipse Canvas.Left='21' Canvas.Top='1' Width='6' Height='6' Fill='#B0A4F5'/><Rectangle Canvas.Left='4' Canvas.Top='10' Width='40' Height='30' RadiusX='12' RadiusY='12' Fill='#ECEAFF' Stroke='#AD9FE6' StrokeThickness='1.5'/><Rectangle Canvas.Left='10' Canvas.Top='16' Width='28' Height='18' RadiusX='7' RadiusY='7' Fill='#343451'/><Path Data='M 16,27 Q 19,20 22,27 M 27,27 Q 30,20 33,27' Stroke='#9ACBFF' StrokeThickness='2.6' StrokeStartLineCap='Round' StrokeEndLineCap='Round'/><Rectangle Canvas.Left='14' Canvas.Top='40' Width='20' Height='6' RadiusX='3' RadiusY='3' Fill='#B0A4F5'/></Canvas></Viewbox>";
    static string PopupXaml { get { using(var stream=typeof(PetApplication).Assembly.GetManifestResourceStream("TranslatorPet.TranslationWindow.xaml")) using(var reader=new StreamReader(stream,Encoding.UTF8)) return reader.ReadToEnd(); } }
}
}
