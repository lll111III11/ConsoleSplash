// ConsoleSplash animation manifest (declarative, not executed)
// 16 张 PNG（frames/frame_001.png ...），12 FPS，播完自动循环。
public class Splash {
    String  name       = "PNG Frame Sequence";
    String  animation  = "frames";
    double  duration   = 0.0;   // 0 = 使用素材自身长度 / use the natural length
    int     fps        = 12;
    boolean skipOnKey  = true;
    String  background = "#000000";
    String  accent     = "#7FDBFF";
    boolean hint       = true;
}
