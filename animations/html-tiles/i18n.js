// ============================================================================
//  ConsoleSplash —— 内置文本库 / built-in localization database
//
//  语言集合对齐 SCP:SL 客户端 Translations 目录（NW 语言列表）：
//  ca, cs, de, en, es, fr, gl, it, ko, pl, pt_BR, ru, sk, sr, tr, uk, vi,
//  zh_Hans, zh_Hant。
//  选择顺序：系统时区 -> 浏览器语言 -> 英语。
//  Picked from the system time zone first, then the browser language, then English.
// ============================================================================

(function () {
  "use strict";

  var DB = {
    en: {
      subtitle: "OPENING SOON",
      head: "CONSOLE BOOT SEQUENCE",
      game: "SCP:SL DEDICATED",
      log1: "initializing overlay",
      log2: "loading animation pack",
      log3: "mounting console window",
      log4: "starting server",
      log5: "secure. contain. protect.",
      log6: "console handoff complete",
      log7: "waiting for operator input",
      log8: "press any key to skip",
      ok: "OK",
      ready: "READY",
      pending: "PENDING",
      progress: "BOOT PROGRESS",
      footer: "SERVER WINDOW WILL OPEN AT SCREEN CENTER",
      hint: "PRESS ANY KEY TO SKIP"
    },

    "zh-CN": {
      subtitle: "即将打开",
      head: "控制台启动序列",
      game: "SCP:SL 专用服务器",
      log1: "正在初始化覆盖层",
      log2: "正在加载动画包",
      log3: "正在挂载控制台窗口",
      log4: "正在启动服务器",
      log5: "控制. 收容. 保护.",
      log6: "控制台交接完成",
      log7: "等待操作员输入",
      log8: "按任意键跳过",
      ok: "成功",
      ready: "就绪",
      pending: "等待中",
      progress: "启动进度",
      footer: "服务器窗口将在屏幕正中心打开",
      hint: "按任意键跳过"
    },

    "zh-TW": {
      subtitle: "即將開啟",
      head: "主控台啟動序列",
      game: "SCP:SL 專用伺服器",
      log1: "正在初始化覆蓋層",
      log2: "正在載入動畫包",
      log3: "正在掛載主控台視窗",
      log4: "正在啟動伺服器",
      log5: "控制. 收容. 保護.",
      log6: "主控台交接完成",
      log7: "等待操作員輸入",
      log8: "按任意鍵跳過",
      ok: "成功",
      ready: "就緒",
      pending: "等待中",
      progress: "啟動進度",
      footer: "伺服器視窗將在螢幕正中心開啟",
      hint: "按任意鍵跳過"
    },

    ko: {
      subtitle: "곧 실행됩니다",
      head: "콘솔 부팅 시퀀스",
      game: "SCP:SL 전용 서버",
      log1: "오버레이 초기화 중",
      log2: "애니메이션 팩 불러오는 중",
      log3: "콘솔 창 연결 중",
      log4: "서버 시작 중",
      log5: "확보. 격리. 보호.",
      log6: "콘솔 인계 완료",
      log7: "운영자 입력 대기 중",
      log8: "아무 키나 눌러 건너뛰기",
      ok: "성공",
      ready: "준비 완료",
      pending: "대기 중",
      progress: "부팅 진행",
      footer: "서버 창이 화면 중앙에 열립니다",
      hint: "아무 키나 눌러 건너뛰기"
    },

    ru: {
      subtitle: "СКОРО ОТКРОЕТСЯ",
      head: "ПОСЛЕДОВАТЕЛЬНОСТЬ ЗАГРУЗКИ КОНСОЛИ",
      game: "SCP:SL ВЫДЕЛЕННЫЙ СЕРВЕР",
      log1: "инициализация оверлея",
      log2: "загрузка пакета анимации",
      log3: "подключение окна консоли",
      log4: "запуск сервера",
      log5: "Обезопасить. Удержать. Сохранить.",
      log6: "передача консоли завершена",
      log7: "ожидание ввода оператора",
      log8: "нажмите любую клавишу для пропуска",
      ok: "ОК",
      ready: "ГОТОВО",
      pending: "ОЖИДАНИЕ",
      progress: "ХОД ЗАГРУЗКИ",
      footer: "ОКНО СЕРВЕРА ОТКРОЕТСЯ В ЦЕНТРЕ ЭКРАНА",
      hint: "НАЖМИТЕ ЛЮБУЮ КЛАВИШУ"
    },

    uk: {
      subtitle: "НЕЗАБАРОМ ВІДКРИЄТЬСЯ",
      head: "ПОСЛІДОВНІСТЬ ЗАВАНТАЖЕННЯ КОНСОЛІ",
      game: "SCP:SL ВИДІЛЕНИЙ СЕРВЕР",
      log1: "ініціалізація оверлею",
      log2: "завантаження пакета анімації",
      log3: "підключення вікна консолі",
      log4: "запуск сервера",
      log5: "Захистити. Утримати. Зберегти.",
      log6: "передавання консолі завершено",
      log7: "очікування введення оператора",
      log8: "натисніть будь-яку клавішу для пропуску",
      ok: "ОК",
      ready: "ГОТОВО",
      pending: "ОЧІКУВАННЯ",
      progress: "ПРОГРЕС ЗАВАНТАЖЕННЯ",
      footer: "ВІКНО СЕРВЕРА ВІДКРИЄТЬСЯ В ЦЕНТРІ ЕКРАНА",
      hint: "НАТИСНІТЬ БУДЬ-ЯКУ КЛАВІШУ"
    },

    pl: {
      subtitle: "WKRÓTCE OTWARCIE",
      head: "SEKWENCJA ROZRUCHU KONSOLI",
      game: "SCP:SL SERWER DEDYKOWANY",
      log1: "inicjalizacja nakładki",
      log2: "ładowanie pakietu animacji",
      log3: "montowanie okna konsoli",
      log4: "uruchamianie serwera",
      log5: "Zabezpieczyć. Przechować. Ochronić.",
      log6: "przekazanie konsoli zakończone",
      log7: "oczekiwanie na dane operatora",
      log8: "naciśnij dowolny klawisz, aby pominąć",
      ok: "OK",
      ready: "GOTOWE",
      pending: "OCZEKUJE",
      progress: "POSTĘP ROZRUCHU",
      footer: "OKNO SERWERA OTWORZY SIĘ NA ŚRODKU EKRANU",
      hint: "NACIŚNIJ DOWOLNY KLAWISZ"
    },

    de: {
      subtitle: "ÖFFNET GLEICH",
      head: "KONSOLEN-STARTSEQUENZ",
      game: "SCP:SL DEDIZIERTER SERVER",
      log1: "Overlay wird initialisiert",
      log2: "Animationspaket wird geladen",
      log3: "Konsolenfenster wird eingebunden",
      log4: "Server wird gestartet",
      log5: "Sichern. Eindämmen. Schützen.",
      log6: "Konsolenübergabe abgeschlossen",
      log7: "warte auf Bedienereingabe",
      log8: "beliebige Taste zum Überspringen",
      ok: "OK",
      ready: "BEREIT",
      pending: "WARTET",
      progress: "STARTFORTSCHRITT",
      footer: "DAS SERVERFENSTER ÖFFNET SICH IN DER BILDSCHIRMMITTE",
      hint: "BELIEBIGE TASTE ZUM ÜBERSPRINGEN"
    },

    fr: {
      subtitle: "OUVERTURE IMMINENTE",
      head: "SÉQUENCE DE DÉMARRAGE DE LA CONSOLE",
      game: "SCP:SL SERVEUR DÉDIÉ",
      log1: "initialisation de la surcouche",
      log2: "chargement du pack d'animation",
      log3: "montage de la fenêtre console",
      log4: "démarrage du serveur",
      log5: "Sécuriser. Confiner. Protéger.",
      log6: "transfert de la console terminé",
      log7: "en attente de l'opérateur",
      log8: "appuyez sur une touche pour passer",
      ok: "OK",
      ready: "PRÊT",
      pending: "EN ATTENTE",
      progress: "PROGRESSION DU DÉMARRAGE",
      footer: "LA FENÊTRE DU SERVEUR S'OUVRIRA AU CENTRE DE L'ÉCRAN",
      hint: "APPUYEZ SUR UNE TOUCHE"
    },

    es: {
      subtitle: "A PUNTO DE ABRIR",
      head: "SECUENCIA DE INICIO DE CONSOLA",
      game: "SCP:SL SERVIDOR DEDICADO",
      log1: "inicializando superposición",
      log2: "cargando paquete de animación",
      log3: "montando ventana de consola",
      log4: "iniciando servidor",
      log5: "Asegurar. Contener. Proteger.",
      log6: "traspaso de consola completado",
      log7: "esperando entrada del operador",
      log8: "pulsa cualquier tecla para omitir",
      ok: "OK",
      ready: "LISTO",
      pending: "PENDIENTE",
      progress: "PROGRESO DE INICIO",
      footer: "LA VENTANA DEL SERVIDOR SE ABRIRÁ EN EL CENTRO DE LA PANTALLA",
      hint: "PULSA CUALQUIER TECLA"
    },

    it: {
      subtitle: "APERTURA IMMINENTE",
      head: "SEQUENZA DI AVVIO DELLA CONSOLE",
      game: "SCP:SL SERVER DEDICATO",
      log1: "inizializzazione overlay",
      log2: "caricamento pacchetto animazione",
      log3: "montaggio finestra console",
      log4: "avvio del server",
      log5: "Proteggere. Contenere. Salvaguardare.",
      log6: "passaggio della console completato",
      log7: "in attesa dell'operatore",
      log8: "premi un tasto per saltare",
      ok: "OK",
      ready: "PRONTO",
      pending: "IN ATTESA",
      progress: "AVANZAMENTO AVVIO",
      footer: "LA FINESTRA DEL SERVER SI APRIRÀ AL CENTRO DELLO SCHERMO",
      hint: "PREMI UN TASTO"
    },

    "pt-BR": {
      subtitle: "ABRINDO EM BREVE",
      head: "SEQUÊNCIA DE INICIALIZAÇÃO DO CONSOLE",
      game: "SCP:SL SERVIDOR DEDICADO",
      log1: "inicializando sobreposição",
      log2: "carregando pacote de animação",
      log3: "montando janela do console",
      log4: "iniciando servidor",
      log5: "Proteger. Conter. Preservar.",
      log6: "transferência do console concluída",
      log7: "aguardando entrada do operador",
      log8: "pressione qualquer tecla para pular",
      ok: "OK",
      ready: "PRONTO",
      pending: "AGUARDANDO",
      progress: "PROGRESSO DE INICIALIZAÇÃO",
      footer: "A JANELA DO SERVIDOR ABRIRÁ NO CENTRO DA TELA",
      hint: "PRESSIONE QUALQUER TECLA"
    },

    tr: {
      subtitle: "AÇILMAK ÜZERE",
      head: "KONSOL ÖNYÜKLEME SIRASI",
      game: "SCP:SL ÖZEL SUNUCU",
      log1: "kaplama başlatılıyor",
      log2: "animasyon paketi yükleniyor",
      log3: "konsol penceresi bağlanıyor",
      log4: "sunucu başlatılıyor",
      log5: "Güvenli. Muhafaza. Koru.",
      log6: "konsol devri tamamlandı",
      log7: "operatör girişi bekleniyor",
      log8: "atlamak için herhangi bir tuşa basın",
      ok: "TAMAM",
      ready: "HAZIR",
      pending: "BEKLİYOR",
      progress: "ÖNYÜKLEME İLERLEMESİ",
      footer: "SUNUCU PENCERESİ EKRANIN ORTASINDA AÇILACAK",
      hint: "ATLAMAK İÇİN BİR TUŞA BASIN"
    },

    cs: {
      subtitle: "BRZY SE OTEVŘE",
      head: "POSLOUPNOST SPUŠTĚNÍ KONZOLE",
      game: "SCP:SL VYHRAZENÝ SERVER",
      log1: "inicializace překrytí",
      log2: "načítání animačního balíčku",
      log3: "připojování okna konzole",
      log4: "spouštění serveru",
      log5: "Zabezpečit. Zadržet. Chránit.",
      log6: "předání konzole dokončeno",
      log7: "čekání na vstup operátora",
      log8: "pro přeskočení stiskněte libovolnou klávesu",
      ok: "OK",
      ready: "PŘIPRAVENO",
      pending: "ČEKÁ",
      progress: "PRŮBĚH SPUŠTĚNÍ",
      footer: "OKNO SERVERU SE OTEVŘE UPROSTŘED OBRAZOVKY",
      hint: "STISKNĚTE LIBOVOLNOU KLÁVESU"
    },

    sk: {
      subtitle: "ČOSKORO SA OTVORÍ",
      head: "POSTUPNOSŤ ŠTARTU KONZOLY",
      game: "SCP:SL VYHRADENÝ SERVER",
      log1: "inicializácia prekrytia",
      log2: "načítavanie animačného balíka",
      log3: "pripájanie okna konzoly",
      log4: "spúšťanie servera",
      log5: "Zabezpečiť. Zadržať. Chrániť.",
      log6: "odovzdanie konzoly dokončené",
      log7: "čakanie na vstup operátora",
      log8: "stlačením ľubovoľnej klávesy preskočiť",
      ok: "OK",
      ready: "PRIPRAVENÉ",
      pending: "ČAKÁ",
      progress: "PRIEBEH ŠTARTU",
      footer: "OKNO SERVERA SA OTVORÍ V STREDE OBRAZOVKY",
      hint: "STLAČTE ĽUBOVOĽNÚ KLÁVESU"
    },

    ca: {
      subtitle: "A PUNT D'OBRIR",
      head: "SEQÜÈNCIA D'ARRENCADA DE LA CONSOLA",
      game: "SCP:SL SERVIDOR DEDICAT",
      log1: "inicialitzant la superposició",
      log2: "carregant el paquet d'animació",
      log3: "muntant la finestra de la consola",
      log4: "iniciant el servidor",
      log5: "Assegurar. Contenir. Protegir.",
      log6: "traspàs de la consola completat",
      log7: "esperant l'entrada de l'operador",
      log8: "prem qualsevol tecla per ometre",
      ok: "OK",
      ready: "LLEST",
      pending: "PENDENT",
      progress: "PROGRÉS D'ARRENCADA",
      footer: "LA FINESTRA DEL SERVIDOR S'OBRIRÀ AL CENTRE DE LA PANTALLA",
      hint: "PREM QUALSEVOL TECLA"
    },

    gl: {
      subtitle: "A PUNTO DE ABRIR",
      head: "SECUENCIA DE ARRANQUE DA CONSOLA",
      game: "SCP:SL SERVIDOR DEDICADO",
      log1: "inicializando a superposición",
      log2: "cargando o paquete de animación",
      log3: "montando a xanela da consola",
      log4: "iniciando o servidor",
      log5: "Asegurar. Conter. Protexer.",
      log6: "traspaso da consola completado",
      log7: "esperando a entrada do operador",
      log8: "preme calquera tecla para omitir",
      ok: "OK",
      ready: "LISTO",
      pending: "PENDENTE",
      progress: "PROGRESO DE ARRANQUE",
      footer: "A XANELA DO SERVIDOR ABRIRÁ NO CENTRO DA PANTALLA",
      hint: "PREME CALQUERA TECLA"
    },

    vi: {
      subtitle: "SẮP MỞ",
      head: "TRÌNH TỰ KHỞI ĐỘNG BẢNG ĐIỀU KHIỂN",
      game: "SCP:SL MÁY CHỦ RIÊNG",
      log1: "đang khởi tạo lớp phủ",
      log2: "đang tải gói hoạt ảnh",
      log3: "đang gắn cửa sổ bảng điều khiển",
      log4: "đang khởi động máy chủ",
      log5: "Bảo vệ. Giam giữ. Duy trì.",
      log6: "bàn giao bảng điều khiển hoàn tất",
      log7: "đang chờ người vận hành nhập",
      log8: "nhấn phím bất kỳ để bỏ qua",
      ok: "OK",
      ready: "SẴN SÀNG",
      pending: "CHỜ",
      progress: "TIẾN TRÌNH KHỞI ĐỘNG",
      footer: "CỬA SỔ MÁY CHỦ SẼ MỞ Ở GIỮA MÀN HÌNH",
      hint: "NHẤN PHÍM BẤT KỲ"
    },

    sr: {
      subtitle: "УСКОРО СЕ ОТВАРА",
      head: "РЕДОСЛЕД ПОДИЗАЊА КОНЗОЛЕ",
      game: "SCP:SL ДЕДИКОВАНИ СЕРВЕР",
      log1: "иницијализација преклопа",
      log2: "учитавање пакета анимације",
      log3: "повезивање прозора конзоле",
      log4: "покретање сервера",
      log5: "Обезбеди. Задржи. Заштити.",
      log6: "предаја конзоле завршена",
      log7: "чекање на унос оператера",
      log8: "притисните било коју типку да прескочите",
      ok: "У РЕДУ",
      ready: "СПРЕМАН",
      pending: "ЧЕКА",
      progress: "НАПРЕДАК ПОДИЗАЊА",
      footer: "ПРОЗОР СЕРВЕРА ЋЕ СЕ ОТВОРИТИ У СРЕДИНИ ЕКРАНА",
      hint: "ПРИТИСНИТЕ БИЛО КОЈУ ТИПКУ"
    },

    "sr-Latn": {
      subtitle: "USKORO SE OTVARA",
      head: "REDOSLED PODIZANJA KONZOLE",
      game: "SCP:SL DEDIKOVANI SERVER",
      log1: "inicijalizacija preklopa",
      log2: "učitavanje paketa animacije",
      log3: "povezivanje prozora konzole",
      log4: "pokretanje servera",
      log5: "Obezbedi. Zadrži. Zaštiti.",
      log6: "predaja konzole završena",
      log7: "čekanje na unos operatera",
      log8: "pritisnite bilo koju tipku da preskočite",
      ok: "U REDU",
      ready: "SPREMAN",
      pending: "ČEKA",
      progress: "NAPREDAK PODIZANJA",
      footer: "PROZOR SERVERA ĆE SE OTVORITI U SREDINI EKRANA",
      hint: "PRITISNITE BILO KOJU TIPKU"
    }
  };

  // 时区 -> 语言。覆盖 NW 语言列表里有独立时区的所有语言。
  // Time zone -> language, covering every NW language with a distinct region.
  var TZ = {
    "Asia/Shanghai": "zh-CN", "Asia/Chongqing": "zh-CN", "Asia/Harbin": "zh-CN",
    "Asia/Urumqi": "zh-CN", "Asia/Kashgar": "zh-CN",
    "Asia/Taipei": "zh-TW", "Asia/Hong_Kong": "zh-TW", "Asia/Macau": "zh-TW",

    "Asia/Seoul": "ko", "Asia/Pyongyang": "ko",

    "Europe/Moscow": "ru", "Europe/Kaliningrad": "ru", "Europe/Samara": "ru",
    "Europe/Volgograd": "ru", "Europe/Astrakhan": "ru", "Europe/Ulyanovsk": "ru",
    "Asia/Yekaterinburg": "ru", "Asia/Novosibirsk": "ru", "Asia/Krasnoyarsk": "ru",
    "Asia/Irkutsk": "ru", "Asia/Yakutsk": "ru", "Asia/Vladivostok": "ru",
    "Asia/Magadan": "ru", "Asia/Kamchatka": "ru", "Asia/Sakhalin": "ru",
    "Asia/Omsk": "ru", "Asia/Barnaul": "ru", "Asia/Tomsk": "ru", "Asia/Novokuznetsk": "ru",

    "Europe/Kyiv": "uk", "Europe/Kiev": "uk", "Europe/Simferopol": "uk",
    "Europe/Uzhgorod": "uk", "Europe/Zaporozhye": "uk",

    "Europe/Warsaw": "pl",
    "Europe/Prague": "cs",
    "Europe/Bratislava": "sk",

    "Europe/Berlin": "de", "Europe/Vienna": "de", "Europe/Zurich": "de",
    "Europe/Luxembourg": "de", "Europe/Liechtenstein": "de",

    "Europe/Paris": "fr", "Europe/Monaco": "fr",
    "Europe/Madrid": "es", "Atlantic/Canary": "es",

    "Europe/Rome": "it", "Europe/Vatican": "it", "Europe/San_Marino": "it",

    "America/Sao_Paulo": "pt-BR", "America/Bahia": "pt-BR", "America/Fortaleza": "pt-BR",
    "America/Recife": "pt-BR", "America/Belem": "pt-BR", "America/Manaus": "pt-BR",
    "America/Cuiaba": "pt-BR", "America/Porto_Velho": "pt-BR",
    "America/Rio_Branco": "pt-BR", "America/Noronha": "pt-BR",

    "Europe/Istanbul": "tr", "Asia/Istanbul": "tr",

    "Europe/Belgrade": "sr", "Europe/Podgorica": "sr", "Europe/Sarajevo": "sr",
    "Europe/Skopje": "sr",

    "Asia/Ho_Chi_Minh": "vi", "Asia/Saigon": "vi",

    "America/Mexico_City": "es", "America/Monterrey": "es", "America/Tijuana": "es",
    "America/Chihuahua": "es", "America/Hermosillo": "es", "America/Mazatlan": "es",
    "America/Bogota": "es", "America/Lima": "es", "America/Santiago": "es",
    "America/Argentina/Buenos_Aires": "es", "America/Argentina/Cordoba": "es",
    "America/Argentina/Mendoza": "es", "America/Caracas": "es", "America/Guayaquil": "es",
    "America/La_Paz": "es", "America/Asuncion": "es", "America/Montevideo": "es",
    "America/Panama": "es", "America/Costa_Rica": "es", "America/El_Salvador": "es",
    "America/Guatemala": "es", "America/Tegucigalpa": "es", "America/Managua": "es",
    "America/Havana": "es", "America/Puerto_Rico": "es", "America/Dominica": "es",

    "Europe/Lisbon": "pt-BR", "Atlantic/Azores": "pt-BR", "Atlantic/Madeira": "pt-BR",

    "Africa/Casablanca": "fr", "Africa/El_Aaiun": "fr",
    "Africa/Dakar": "fr", "Africa/Abidjan": "fr", "Africa/Bamako": "fr",
    "Africa/Ouagadougou": "fr", "Africa/Niamey": "fr", "Africa/Conakry": "fr",
    "Africa/Lome": "fr", "Africa/Porto-Novo": "fr", "Africa/Libreville": "fr",
    "Africa/Brazzaville": "fr", "Africa/Kinshasa": "fr", "Africa/Douala": "fr",
    "Africa/Ndjamena": "fr", "Africa/Bangui": "fr", "Africa/Antananarivo": "fr"
  };

  // 时区无法区分（加泰罗尼亚/加利西亚与西班牙同一时区）时，用浏览器语言兜底。
  // Catalonia/Galicia share Spain's time zone; fall back to the browser language.
  var NAV = {
    ca: "ca", gl: "gl", cs: "cs", sk: "sk", sr: "sr", "sr-latn": "sr-Latn",
    zh: "zh-CN", "zh-cn": "zh-CN", "zh-hans": "zh-CN", "zh-sg": "zh-CN",
    "zh-tw": "zh-TW", "zh-hk": "zh-TW", "zh-mo": "zh-TW", "zh-hant": "zh-TW",
    pt: "pt-BR", "pt-br": "pt-BR", "pt-pt": "pt-BR",
    en: "en", ko: "ko", ru: "ru", uk: "uk", pl: "pl", de: "de", fr: "fr",
    es: "es", it: "it", tr: "tr", vi: "vi"
  };

  function normalize(code) {
    if (!code) return "en";
    var lower = String(code).toLowerCase();
    if (NAV[lower]) return NAV[lower];
    var base = lower.split(/[-_]/)[0];
    if (NAV[base]) return NAV[base];
    return "en";
  }

  function detect() {
    try {
      var tz = "";
      if (typeof Intl !== "undefined" && Intl.DateTimeFormat) {
        tz = Intl.DateTimeFormat().resolvedOptions().timeZone || "";
      }
      if (tz && TZ[tz]) return TZ[tz];

      if (tz) {
        var region = tz.split("/")[0];
        if (region === "Asia" && tz.indexOf("Taipei") >= 0) return "zh-TW";
      }
    } catch (e) {
    }

    try {
      var language = navigator.language || navigator.userLanguage || "";
      if (language) return normalize(language);
    } catch (e) {
    }

    return "en";
  }

  function apply() {
    var lang = detect();
    var dict = DB[lang] || DB.en;

    document.documentElement.lang = lang;

    var nodes = document.querySelectorAll("[data-i18n]");
    for (var i = 0; i < nodes.length; i++) {
      var key = nodes[i].getAttribute("data-i18n");
      if (key && dict[key] != null) nodes[i].textContent = dict[key];
    }

    // 方便调试：window.CS_I18N.lang 可看到实际选中的语言。
    window.CS_I18N = { lang: lang, dict: dict };
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", apply);
  } else {
    apply();
  }
})();
