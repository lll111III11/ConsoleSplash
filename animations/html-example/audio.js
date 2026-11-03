/* ==========================================================================
   ConsoleSplash —— 过场动画音效系统 / procedural soundtrack
   全部用 WebAudio 现场合成，不依赖任何音频文件。

   结构 / structure:
     · 100 BPM，4 小节一个循环，循环不停、层次只增不减（渐进）
     · 钢琴分解和弦 + 低音铺底 + 定音鼓/军鼓/踩镲
     · 鼓点走向参考 SCP:SL 轻收容封锁那段的进行感（低鼓 + 军鼓滚奏）

   时间线 / timeline (seconds):
     0.25 网格  0.85 服务器名  2.28 光幕  2.45 白闪  2.68 终端
     6.25 收尾光幕  6.28 收尾和弦  6.55-6.92 淡出
   ========================================================================== */
(function () {
  'use strict';

  // 音效默认关闭，由 index.html 里的 CONSOLESPLASH_SOUND 控制。
  // Sound is off by default; flip CONSOLESPLASH_SOUND in index.html to enable.
  if (window.CONSOLESPLASH_SOUND !== true) return;

  var AC = window.AudioContext || window.webkitAudioContext;
  if (!AC) return;

  var ctx;
  try { ctx = new AC(); } catch (e) { return; }
  if (ctx.state === 'suspended') { try { ctx.resume(); } catch (e) {} }

  // ---------- 总线 / buses ----------

  var master = ctx.createGain();
  master.gain.value = 0.55;

  var comp = ctx.createDynamicsCompressor();
  comp.threshold.value = -16;
  comp.knee.value = 24;
  comp.ratio.value = 6;
  comp.attack.value = 0.004;
  comp.release.value = 0.22;

  master.connect(comp);
  comp.connect(ctx.destination);

  // 简易混响：自制衰减噪声当脉冲响应，给钢琴和鼓一点空间
  function makeImpulse(seconds, decay) {
    var rate = ctx.sampleRate;
    var length = Math.floor(rate * seconds);
    var buffer = ctx.createBuffer(2, length, rate);
    for (var ch = 0; ch < 2; ch++) {
      var chan = buffer.getChannelData(ch);
      for (var i = 0; i < length; i++) {
        chan[i] = (Math.random() * 2 - 1) * Math.pow(1 - i / length, decay);
      }
    }
    return buffer;
  }

  var reverbIn = ctx.createGain();
  var convolver = ctx.createConvolver();
  convolver.buffer = makeImpulse(2.2, 2.4);
  var wet = ctx.createGain();
  wet.gain.value = 0.3;
  reverbIn.connect(convolver);
  convolver.connect(wet);
  wet.connect(master);

  function send(node, amount) {
    var g = ctx.createGain();
    g.gain.value = amount;
    node.connect(g);
    g.connect(reverbIn);
  }

  // ---------- 基础工具 / helpers ----------

  var noiseBuffer = ctx.createBuffer(1, Math.floor(ctx.sampleRate * 1.5), ctx.sampleRate);
  (function () {
    var data = noiseBuffer.getChannelData(0);
    for (var i = 0; i < data.length; i++) data[i] = Math.random() * 2 - 1;
  })();

  function gainNode(value) {
    var g = ctx.createGain();
    g.gain.value = value;
    return g;
  }

  function noiseSource(t, duration) {
    var src = ctx.createBufferSource();
    src.buffer = noiseBuffer;
    src.loop = true;
    src.start(t);
    src.stop(t + duration);
    return src;
  }

  function envelope(param, t, peak, attack, decay) {
    param.setValueAtTime(0.0001, t);
    param.exponentialRampToValueAtTime(Math.max(peak, 0.0002), t + attack);
    param.exponentialRampToValueAtTime(0.0001, t + attack + decay);
  }

  function midiToFreq(note) {
    return 440 * Math.pow(2, (note - 69) / 12);
  }

  // ---------- 乐器 / instruments ----------

  // 钢琴：三角波基频 + 二三次谐波，快速起音后指数衰减，带一点击弦噪声
  function piano(t, note, velocity, duration) {
    var freq = midiToFreq(note);
    var out = gainNode(0);
    var lowpass = ctx.createBiquadFilter();
    lowpass.type = 'lowpass';
    lowpass.frequency.setValueAtTime(Math.min(freq * 9, 9000), t);
    lowpass.frequency.exponentialRampToValueAtTime(Math.max(freq * 2.2, 420), t + duration);

    var o1 = ctx.createOscillator();
    o1.type = 'triangle';
    o1.frequency.value = freq;

    var o2 = ctx.createOscillator();
    o2.type = 'sine';
    o2.frequency.value = freq * 2.003;
    var g2 = gainNode(0.38);

    var o3 = ctx.createOscillator();
    o3.type = 'sine';
    o3.frequency.value = freq * 3.001;
    var g3 = gainNode(0.12);

    o1.connect(lowpass);
    o2.connect(g2); g2.connect(lowpass);
    o3.connect(g3); g3.connect(lowpass);
    lowpass.connect(out);
    out.connect(master);
    send(out, 0.42);

    out.gain.setValueAtTime(0.0001, t);
    out.gain.exponentialRampToValueAtTime(velocity, t + 0.008);
    out.gain.exponentialRampToValueAtTime(0.0001, t + duration);

    // 击弦噪声，让音头不至于太软
    var hammer = noiseSource(t, 0.05);
    var bp = ctx.createBiquadFilter();
    bp.type = 'bandpass';
    bp.frequency.value = Math.min(freq * 4.5, 6000);
    bp.Q.value = 1.4;
    var hg = gainNode(0);
    envelope(hg.gain, t, velocity * 0.22, 0.002, 0.035);
    hammer.connect(bp); bp.connect(hg); hg.connect(master);

    o1.start(t); o1.stop(t + duration + 0.05);
    o2.start(t); o2.stop(t + duration + 0.05);
    o3.start(t); o3.stop(t + duration + 0.05);
  }

  // 弦垫：两个微失谐锯齿，慢起慢落
  function pad(t, note, duration, level) {
    var freq = midiToFreq(note);
    var out = gainNode(0);
    var lowpass = ctx.createBiquadFilter();
    lowpass.type = 'lowpass';
    lowpass.frequency.value = 900;
    lowpass.Q.value = 0.7;

    var a = ctx.createOscillator();
    a.type = 'sawtooth';
    a.frequency.value = freq;
    a.detune.value = -7;
    var b = ctx.createOscillator();
    b.type = 'sawtooth';
    b.frequency.value = freq;
    b.detune.value = 7;

    a.connect(lowpass); b.connect(lowpass);
    lowpass.connect(out);
    out.connect(master);
    send(out, 0.5);

    out.gain.setValueAtTime(0.0001, t);
    out.gain.exponentialRampToValueAtTime(level, t + 0.8);
    out.gain.setValueAtTime(level, t + duration - 0.4);
    out.gain.exponentialRampToValueAtTime(0.0001, t + duration);

    a.start(t); a.stop(t + duration + 0.05);
    b.start(t); b.stop(t + duration + 0.05);
  }

  function bass(t, note, level, duration) {
    var freq = midiToFreq(note);
    var osc = ctx.createOscillator();
    var osc2 = ctx.createOscillator();
    var lowpass = ctx.createBiquadFilter();
    var out = gainNode(0);

    osc.type = 'sawtooth';
    osc.frequency.value = freq;
    osc2.type = 'sine';
    osc2.frequency.value = freq;

    lowpass.type = 'lowpass';
    lowpass.frequency.setValueAtTime(420, t);
    lowpass.frequency.exponentialRampToValueAtTime(140, t + duration);
    lowpass.Q.value = 4;

    envelope(out.gain, t, level, 0.01, duration);
    osc.connect(lowpass);
    osc2.connect(lowpass);
    lowpass.connect(out);
    out.connect(master);

    osc.start(t); osc.stop(t + duration + 0.05);
    osc2.start(t); osc2.stop(t + duration + 0.05);
  }

  // 定音鼓：低鼓 + 噪声击打，参考封锁警报的低频推进
  function timpani(t, level) {
    var osc = ctx.createOscillator();
    var out = gainNode(0);
    osc.type = 'sine';
    osc.frequency.setValueAtTime(118, t);
    osc.frequency.exponentialRampToValueAtTime(58, t + 0.3);
    envelope(out.gain, t, level, 0.006, 0.85);
    osc.connect(out); out.connect(master);
    send(out, 0.35);
    osc.start(t); osc.stop(t + 1.0);

    var hit = noiseSource(t, 0.12);
    var lp = ctx.createBiquadFilter();
    lp.type = 'lowpass';
    lp.frequency.value = 380;
    var hg = gainNode(0);
    envelope(hg.gain, t, level * 0.5, 0.003, 0.1);
    hit.connect(lp); lp.connect(hg); hg.connect(master);
  }

  function kick(t, level) {
    var osc = ctx.createOscillator();
    var out = gainNode(0);
    osc.type = 'sine';
    osc.frequency.setValueAtTime(120, t);
    osc.frequency.exponentialRampToValueAtTime(46, t + 0.14);
    envelope(out.gain, t, level, 0.004, 0.3);
    osc.connect(out); out.connect(master);
    osc.start(t); osc.stop(t + 0.36);
  }

  function snare(t, level) {
    var src = noiseSource(t, 0.22);
    var bp = ctx.createBiquadFilter();
    var out = gainNode(0);
    bp.type = 'bandpass';
    bp.frequency.value = 1850;
    bp.Q.value = 0.9;
    envelope(out.gain, t, level, 0.003, 0.17);
    src.connect(bp); bp.connect(out); out.connect(master);
    send(out, 0.22);

    var body = ctx.createOscillator();
    var bg = gainNode(0);
    body.type = 'triangle';
    body.frequency.setValueAtTime(195, t);
    body.frequency.exponentialRampToValueAtTime(120, t + 0.11);
    envelope(bg.gain, t, level * 0.5, 0.003, 0.11);
    body.connect(bg); bg.connect(master);
    body.start(t); body.stop(t + 0.16);
  }

  function hat(t, level) {
    var src = noiseSource(t, 0.07);
    var hp = ctx.createBiquadFilter();
    var out = gainNode(0);
    hp.type = 'highpass';
    hp.frequency.value = 7600;
    envelope(out.gain, t, level, 0.002, 0.05);
    src.connect(hp); hp.connect(out); out.connect(master);
  }

  function crash(t, level) {
    var src = noiseSource(t, 1.1);
    var hp = ctx.createBiquadFilter();
    var out = gainNode(0);
    hp.type = 'highpass';
    hp.frequency.value = 2600;
    envelope(out.gain, t, level, 0.006, 1.0);
    src.connect(hp); hp.connect(out); out.connect(master);
    send(out, 0.4);
  }

  function tick(t, freq, level) {
    var osc = ctx.createOscillator();
    var out = gainNode(0);
    osc.type = 'square';
    osc.frequency.value = freq;
    envelope(out.gain, t, level, 0.002, 0.045);
    osc.connect(out); out.connect(master);
    osc.start(t); osc.stop(t + 0.07);
  }

  // 噪声横扫：入场/出场光幕
  function whoosh(t, duration, from, to, level, q) {
    var src = noiseSource(t, duration + 0.1);
    var bp = ctx.createBiquadFilter();
    var out = gainNode(0);
    bp.type = 'bandpass';
    bp.Q.value = q || 1.2;
    bp.frequency.setValueAtTime(from, t);
    bp.frequency.exponentialRampToValueAtTime(to, t + duration);
    out.gain.setValueAtTime(0.0001, t);
    out.gain.exponentialRampToValueAtTime(level, t + duration * 0.35);
    out.gain.exponentialRampToValueAtTime(0.0001, t + duration);
    src.connect(bp); bp.connect(out); out.connect(master);
    send(out, 0.25);
  }

  function riser(t, duration, level) {
    var src = noiseSource(t, duration + 0.1);
    var bp = ctx.createBiquadFilter();
    var out = gainNode(0);
    bp.type = 'bandpass';
    bp.Q.value = 2.2;
    bp.frequency.setValueAtTime(300, t);
    bp.frequency.exponentialRampToValueAtTime(6800, t + duration);
    out.gain.setValueAtTime(0.0001, t);
    out.gain.exponentialRampToValueAtTime(level, t + duration * 0.9);
    out.gain.exponentialRampToValueAtTime(0.0001, t + duration + 0.05);
    src.connect(bp); bp.connect(out); out.connect(master);
    send(out, 0.25);
  }

  // ---------- 循环段落 / looping arrangement ----------

  var BPM = 100;
  var BEAT = 60 / BPM;        // 0.6s
  var BAR = BEAT * 4;         // 2.4s

  // Dm - Bb - F - A，四小节一个循环
  var CHORDS = [
    { bass: 38, triad: [62, 65, 69], melody: 74 }, // Dm
    { bass: 34, triad: [58, 62, 65], melody: 70 }, // Bb
    { bass: 41, triad: [60, 65, 69], melody: 72 }, // F
    { bass: 45, triad: [61, 64, 69], melody: 73 }  // A
  ];

  function scheduleBar(index, t) {
    var chord = CHORDS[index % 4];
    var level = Math.min(index, 3); // 0..3，只往上叠，不回退

    // 钢琴分解和弦：八分音符，每两拍升八度
    for (var i = 0; i < 8; i++) {
      var note = chord.triad[i % 3] + (i >= 4 ? 12 : 0);
      var velocity = 0.13 + (i === 0 ? 0.04 : 0) + Math.random() * 0.02;
      piano(t + i * BEAT * 0.5, note, velocity, 0.9 + Math.random() * 0.3);
    }

    // 低音弦垫，撑住整个小节
    for (var p = 0; p < chord.triad.length; p++) {
      pad(t, chord.triad[p] - 12, BAR + 0.5, 0.028);
    }

    // 第一层：心跳般的低鼓
    kick(t, 0.18);
    kick(t + BEAT * 2, 0.15);

    if (level >= 1) {
      // 旋律长音 + 贝斯八分
      piano(t, chord.melody, 0.2, 1.7);
      bass(t, chord.bass, 0.18, 0.5);
      bass(t + BEAT, chord.bass, 0.14, 0.4);
      bass(t + BEAT * 2, chord.bass + 7, 0.15, 0.45);
      bass(t + BEAT * 3, chord.bass, 0.14, 0.4);

      // 军鼓落在二、四拍
      snare(t + BEAT, 0.1);
      snare(t + BEAT * 3, 0.12);
      kick(t + BEAT * 1.5, 0.2);
      kick(t + BEAT * 3.5, 0.16);
    }

    if (level >= 2) {
      // 踩镲八分 + 高八度钢琴
      for (var h = 0; h < 8; h++) {
        hat(t + h * BEAT * 0.5, h % 2 === 0 ? 0.03 : 0.018);
      }
      piano(t + BEAT * 2, chord.triad[2] + 12, 0.1, 1.2);
      timpani(t, 0.26);
    }

    if (level >= 3) {
      // 最后一拍军鼓滚奏，进入满编
      for (var s = 0; s < 4; s++) {
        snare(t + BEAT * 3 + s * BEAT * 0.25, 0.07 + s * 0.012);
      }
      crash(t, 0.07);
      piano(t, chord.triad[0] + 24, 0.08, 1.4);
    }

    // 白闪那一小节（第 2 小节起拍正好 2.45s）加一记重音
    if (index === 1) {
      timpani(t, 0.55);
      crash(t, 0.16);
      bass(t, chord.bass - 12, 0.2, 0.9);
    }
  }

  var startAt = ctx.currentTime + 0.05;
  var nextBar = startAt;
  var barIndex = 0;

  function pump() {
    var limit = startAt + 6.4; // 收尾和弦之后不再叠新小节
    while (nextBar < ctx.currentTime + 0.9 && nextBar < limit) {
      scheduleBar(barIndex, nextBar);
      nextBar += BAR;
      barIndex++;
    }
  }

  pump();
  var timer = setInterval(pump, 150);

  // 循环一直跑；页面关闭时定时器自然消失
  // ---------- 与画面同步的点缀 ----------

  var at = function (s) { return startAt + s; };

  // 0.25 网格地面：一记很轻的低频落点
  kick(at(0.25), 0.12);
  (function air() {
    var src = noiseSource(at(0), 2.4);
    var lp = ctx.createBiquadFilter();
    var g = gainNode(0);
    lp.type = 'lowpass';
    lp.frequency.value = 420;
    g.gain.setValueAtTime(0.0001, at(0));
    g.gain.exponentialRampToValueAtTime(0.015, at(0.9));
    g.gain.exponentialRampToValueAtTime(0.0001, at(2.4));
    src.connect(lp); lp.connect(g); g.connect(master);
  })();

  // 2.28 光幕入场
  riser(at(2.2), 0.42, 0.12);
  whoosh(at(2.26), 0.58, 5200, 480, 0.18, 1.1);

  // 2.68 终端亮起
  tick(at(2.68), 720, 0.03);
  tick(at(2.72), 1080, 0.022);

  // 2.95 - 5.32 日志逐行滴答
  var lineTimes = [2.95, 3.25, 3.55, 3.85, 4.15, 4.45, 4.75, 5.05, 5.32];
  for (var i = 0; i < lineTimes.length; i++) {
    tick(at(lineTimes[i]), 1250 + i * 55, 0.016);
  }

  // 5.95 - 6.25 送进收尾光幕
  riser(at(5.88), 0.5, 0.13);
  whoosh(at(6.22), 0.55, 6800, 380, 0.24, 1.0);
  timpani(at(6.3), 0.45);
  crash(at(6.3), 0.12);

  // 6.28 收尾和弦：Dm 落地，钢琴 + 低音留一条尾巴
  var endChord = [50, 57, 62, 65]; // D3 A3 D4 F4
  for (var c = 0; c < endChord.length; c++) {
    piano(at(6.28) + c * 0.012, endChord[c], c === 0 ? 0.18 : 0.11, 2.0);
  }
  bass(at(6.28), 38, 0.16, 1.4);

  // 6.55 - 6.92 平滑淡出，窗口关闭前已经归零
  master.gain.setValueAtTime(master.gain.value, at(6.55));
  master.gain.exponentialRampToValueAtTime(0.0001, at(6.92));

  if (window.addEventListener) {
    // 页面被关闭时浏览器会回收，这里只做防御性清理
    window.addEventListener('pagehide', function () {
      try { clearInterval(timer); } catch (e) {}
    });
  }
})();
