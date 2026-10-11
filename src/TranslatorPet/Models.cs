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
public class Preferences {
    public string Source = "en", Target = "zh-CN", Skin = "orb";
    public double X = -1, Y = -1;
    public int Size = 48;
}
public class Language {
    public string Code, Name;
    public Language(string code, string name) { Code=code; Name=name; }
    public override string ToString() { return Name; }
}
}
