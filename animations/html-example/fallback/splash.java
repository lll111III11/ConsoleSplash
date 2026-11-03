// ConsoleSplash fallback manifest (declarative, not executed)
// WebView2 不可用时播放的预渲染帧，8 FPS，28 帧 = 3.5 秒。
public class Splash {
    String  name       = "Console Boot Fallback";
    String  animation  = "frames";
    int     fps        = 8;
    double  duration   = 0.0;
    boolean skipOnKey  = true;
    String  background = "#05080B";
    String  accent     = "#7FDBFF";
    boolean hint       = true;
}
