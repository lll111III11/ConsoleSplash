// ConsoleSplash animation manifest (declarative, not executed)
// 第二种默认覆盖：斜滑块滑入控制台大小 -> 斜边扫过合上缝隙 -> 结束后从底部斜着逐块划走。
public class Splash {
    String  name       = "Tile Reveal";
    String  uiid       = "tiles";
    String  animation  = "index.html";
    double  duration   = 7.0;
    double  fade       = 0.35;
    boolean skipOnKey  = true;
    boolean smooth     = false;   // 只覆盖控制台，不铺满屏幕 / console-only
    String  backdrop   = "console"; // 缝隙里露出控制台截图 / console capture behind gaps
    String  background = "#05080B";
    String  accent     = "#7FDBFF";
    boolean hint       = true;
}
