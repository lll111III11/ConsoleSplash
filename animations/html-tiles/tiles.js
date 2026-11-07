/* ============================================================================
   ConsoleSplash —— 斜滑块覆盖 / slanted slat reveal

   1. 斜滑块一块块滑入控制台大小，彼此留缝；
   2. 斜边光带扫过，缝隙逐块合上，拼成完整画面；
   3. 结束前一块接一块从底部斜着划走，露出控制台。
   ========================================================================== */
(function () {
  'use strict';

  var COUNT = 9;      // 滑块数量 / number of slats
  var GAP = 14;       // 初始缝隙（像素）/ initial gap in px
  var STEP = 100 / COUNT;

  var layer = document.getElementById('strips');
  var backdrop = document.getElementById('backdrop');
  if (!layer) return;

  // 缝隙里露出的控制台截图（渲染器在显示窗口前截的）。
  if (backdrop && window.CSPLASH_BACKDROP) {
    backdrop.style.backgroundImage = 'url("' + window.CSPLASH_BACKDROP + '")';
  }

  var strips = [];
  for (var i = 0; i < COUNT; i++) {
    var strip = document.createElement('div');
    strip.className = 'strip';
    strip.style.left = 'calc(' + (i * STEP) + '% + ' + (GAP / 2) + 'px)';
    strip.style.width = 'calc(' + STEP + '% - ' + GAP + 'px)';
    strip.style.setProperty('--from', i % 2 === 0 ? '-130%' : '130%');
    strip.style.animationDelay = (0.08 + i * 0.06).toFixed(2) + 's';
    layer.appendChild(strip);
    strips.push(strip);
  }

  // 斜边扫过后，按顺序把缝隙合上。
  setTimeout(function () {
    for (var i = 0; i < strips.length; i++) {
      (function (index, node) {
        setTimeout(function () {
          node.style.left = (index * STEP) + '%';
          node.style.width = (STEP + 0.25) + '%';
        }, index * 45);
      })(i, strips[i]);
    }
  }, 1400);

  function shuffled(list) {
    var copy = list.slice();
    for (var i = copy.length - 1; i > 0; i--) {
      var j = Math.floor(Math.random() * (i + 1));
      var swap = copy[i];
      copy[i] = copy[j];
      copy[j] = swap;
    }
    return copy;
  }

  // 结尾：一块接一块从底部斜着划走。
  setTimeout(function () {
    var order = shuffled(strips);
    for (var i = 0; i < order.length; i++) {
      (function (index, node) {
        setTimeout(function () {
          node.style.setProperty('--drift', index % 2 === 0 ? '10%' : '-10%');
          node.style.animationDelay = '0s';
          node.classList.add('out');
        }, index * 65);
      })(i, order[i]);
    }
  }, 5820);

  // 跳过：立刻按顺序划走，和渲染器的关闭同步。
  if (window.MutationObserver) {
    var observer = new MutationObserver(function () {
      if (!document.body.classList.contains('skipping')) return;
      observer.disconnect();
      for (var i = 0; i < strips.length; i++) {
        (function (index, node) {
          setTimeout(function () {
            node.style.setProperty('--drift', index % 2 === 0 ? '10%' : '-10%');
            node.style.animationDelay = '0s';
            node.classList.add('out');
          }, index * 10);
        })(i, strips[i]);
      }
    });
    observer.observe(document.body, { attributes: true, attributeFilter: ['class'] });
  }
})();
