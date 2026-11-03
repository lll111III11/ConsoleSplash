// ConsoleSplash animation manifest (declarative, not executed)
// 默认主界面：终端风格开机动画。HTML 由插件内嵌 WebView2 渲染；
// WebView2 不可用时回退到同款预渲染帧。
public class Splash {
    String  name       = "Console Boot";
    String  animation  = "index.html";
    String  fallback   = "fallback";
    double  duration   = 7.0;
    double  fade       = 0.35;
    boolean skipOnKey  = true;
    String  background = "#05080B";
    String  accent     = "#7FDBFF";
    boolean hint       = true;
}
