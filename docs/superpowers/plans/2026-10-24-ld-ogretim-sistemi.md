# Level Design Öğretim Sistemi Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Unity Editor içinde Ders 1'i adım adım anlatan, sahneyi rollere göre denetleyen ve ölçüleri Scene görünümünde çizen bir öğretim sistemi kurmak.

**Architecture:** Parçalar runtime `LDRole` bileşeniyle işaretlenir. Saf bir kural motoru (`LDRules`) parça listesinden bulgu üretir ve sahnesiz test edilir. `LDSceneScanner` sahneden parça toplar, sahne gerektiren kural 9–10'u ekler. `LDScanService` sonucu önbelleğe alıp değişikliklerde yeniler. Ders penceresi ve Scene çizicisi aynı sonucu kullanır.

**Tech Stack:** Unity 6000.3.11f1, C# (global namespace), IMGUI `EditorWindow`, `UnityEditor.Overlays`, `UnityEngine.AI.NavMesh`, ProBuilder 6.0.9 (`Unity.ProBuilder`), Unity Test Framework 1.6.0 (NUnit, EditMode).

**Spec:** `docs/superpowers/specs/2026-10-24-ld-ogretim-sistemi-design.md`

## Global Constraints

- Bütün yeni dosyalar `Assets/_LevelProject/LevelDesign/` altında. `Villa*` scriptlerine, `Villa_Operation.unity` ve `Industrial_Level_01.unity` sahnelerine dokunulmaz.
- Assembly'ler: `LevelDesign` (runtime, yalnız `LDRole.cs`), `LevelDesign.Editor` (yalnız Editor platformu; `LevelDesign` ve `Unity.ProBuilder` referansları), `LevelDesign.Tests` (Editor platformu, `UNITY_INCLUDE_TESTS` define constraint, `nunit.framework.dll` precompiled referansı, `UnityEngine.TestRunner` + `UnityEditor.TestRunner` + `LevelDesign` + `LevelDesign.Editor`, `autoReferenced: false`).
- Kod tarzı mevcut projeyle aynı: global namespace, `sealed` sınıflar, IMGUI. Sınıf adları `LD` ön ekli.
- Kullanıcıya görünen bütün metinler Türkçe ve Türkçe karakterli ("Alçak siper", "Düşman girişi"). Enum üyeleri ASCII (`AlcakSiper`), Inspector adları `[InspectorName("Alçak siper")]` ile verilir.
- Eşikler (spec'ten): alçak siper 0,9–1,2 m; yüksek siper/blok ≥ 1,9 m; kapı genişliği < 1,2 m Hata, < 1,5 m Uyarı; kapı yüksekliği < 2,2 m Hata; geçit 0,05 m < boşluk < 1,2 m Uyarı; siper zinciri > 6 m Uyarı, < 3 m Bilgi; giriş mesafesi < 10 m Uyarı; göz yüksekliği 1,6 m; NavMesh örnekleme yarıçapı 2 m.
- Sınır karşılaştırmaları ± `LDRules.Epsilon = 0.005f` toleranslı. Sınır değerin kendisi geçer (0,9 ve 1,2 m siper geçerli).
- Unity `.meta` dosyalarını kendisi üretir. Her commit'ten önce Unity'nin dosyaları import etmiş olması gerekir; yeni `.cs`/`.asmdef` dosyalarının `.meta`'ları da commit'e girer.
- **Test çalıştırma.** Unity açıksa (kullanıcı şu an açık tutuyor) kullanıcıdan Unity'ye geçmesi ve Window → General → Test Runner → EditMode → `LevelDesign.Tests` → Run All yapması istenir. Derleme hataları `"$LOCALAPPDATA/Unity/Editor/Editor.log"` içinde `error CS` aranarak okunur. Unity kapalıysa:
  `"/c/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor/Unity.exe" -batchmode -projectPath "C:/Projects/Learn_to_LD" -runTests -testPlatform EditMode -assemblyNames LevelDesign.Tests -testResults "<scratchpad>/ld-tests.xml" -logFile "<scratchpad>/ld-tests.log"`
  Çıkış kodu 0 hepsi geçti, 2 başarısız test var, diğer kodlar derleme/başlatma hatası (log'da `error CS` ara).
- Commit'ler `ld-ogretim-sistemi` dalına; mesaj sonunda `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **Tam eşikte float hatası:** Scale 0,9 ile kurulan siperin bounds yüksekliği 0,90000004 çıkabilir. 0,9 ve 1,2 m geçmeli (Task 2 testi `Kural1_SinirDegerleriGecer`).
2. **Zemin yok ya da parça zemin dışında:** Yükseklik 0'dan ölçülmeli, istisna atılmamalı (Task 1 testi `Yukseklik_ZeminYoksaSifirdanOlculur`).
3. **Üst üste zeminler (platform):** Alt kattaki siperin yüksekliği üstteki platforma göre değil, altındaki zemine göre ölçülmeli (Task 1 testi `Yukseklik_UstPlatformaGoreDegilAltZemineGore`).
4. **Boş nesne işaretleri (renderer'sız Düşman girişi):** Sıfır boyutlu bounds; engel sayılmamalı ve kural 8 mesafesi merkezden doğru ölçülmeli (Task 2 testi `Kural8_NoktaIsaretleriyleMesafe`).
5. **Kaydedilmemiş sahne:** `ScenePath` boşsa 0. adım geçmemeli ve "Sahneyi kaydet" demeli (Task 3 testi `Adim0_KaydedilmemisSahneGecmez`).

---

### Task 1: Assembly'ler, LDRole ve ölçüm yardımcıları

**Files:**
- Create: `Assets/_LevelProject/LevelDesign/LevelDesign.asmdef`
- Create: `Assets/_LevelProject/LevelDesign/LDRole.cs`
- Create: `Assets/_LevelProject/LevelDesign/Editor/LevelDesign.Editor.asmdef`
- Create: `Assets/_LevelProject/LevelDesign/Editor/LDPiece.cs`
- Create: `Assets/_LevelProject/LevelDesign/Editor/LDFinding.cs`
- Create: `Assets/_LevelProject/LevelDesign/Editor/LDRules.cs` (bu task'ta yalnız ölçüm yardımcıları ve sabitler)
- Create: `Assets/_LevelProject/LevelDesign/Tests/Editor/LevelDesign.Tests.asmdef`
- Test: `Assets/_LevelProject/LevelDesign/Tests/Editor/LDMeasureTests.cs`

**Interfaces:**
- Produces:
  - `public enum LDRoleType { Zemin, Duvar, AlcakSiper, YuksekSiper, Blok, Kapi, OyuncuGirisi, DusmanGirisi }` (her üyede `[InspectorName]`: "Zemin", "Duvar", "Alçak siper", "Yüksek siper", "Blok / kolon", "Kapı (lento)", "Oyuncu girişi", "Düşman girişi")
  - `[DisallowMultipleComponent][AddComponentMenu("Level Design/LD Rol")] public sealed class LDRole : MonoBehaviour { public LDRoleType role; }`
  - `public readonly struct LDPiece { public readonly string Name; public readonly LDRoleType Role; public readonly Bounds Bounds; public readonly UnityEngine.Object Source; public LDPiece(string name, LDRoleType role, Bounds bounds, UnityEngine.Object source = null); }`
  - `public enum LDSeverity { Hata, Uyari, Bilgi }`
  - `public readonly struct LDFinding { public readonly LDSeverity Severity; public readonly int Rule; public readonly string Message; public readonly Vector3 Position; public readonly UnityEngine.Object Target; public LDFinding(LDSeverity severity, int rule, string message, Vector3 position, UnityEngine.Object target = null); }`
  - `public static class LDRules` içinde: eşik sabitleri (`Epsilon`, `LowCoverMin`, `LowCoverMax`, `TallMin`, `DoorWidthError`, `DoorWidthWarn`, `DoorHeightMin`, `GapTouch`, `GapMin`, `ChainMin`, `ChainMax`, `EntranceMinDistance`, `EyeHeight`, `NavSampleRadius`; değerler Global Constraints'te), `public static float HorizontalGap(Bounds a, Bounds b)`, `public static float FloorTopUnder(IReadOnlyList<LDPiece> pieces, Bounds b)`, `public static float HeightAboveFloor(IReadOnlyList<LDPiece> pieces, Bounds b)`.

- [ ] **Step 1: Asmdef dosyalarını ve `LDRole.cs`'yi yaz** (Global Constraints'teki referanslarla). Tests asmdef `"includePlatforms": ["Editor"]`, `"overrideReferences": true`, `"precompiledReferences": ["nunit.framework.dll"]`, `"defineConstraints": ["UNITY_INCLUDE_TESTS"]`.

- [ ] **Step 2: Failing testleri yaz** — `LDMeasureTests.cs`. Yardımcı: `static LDPiece P(LDRoleType r, Vector3 center, Vector3 size) => new LDPiece(r.ToString(), r, new Bounds(center, size));`

```csharp
[Test] public void Bosluk_DegenKutulardaSifir()
    => Assert.AreEqual(0f, LDRules.HorizontalGap(new Bounds(new Vector3(0,1,0), new Vector3(2,2,2)), new Bounds(new Vector3(2,1,0), new Vector3(2,2,2))), 1e-4f);
[Test] public void Bosluk_KosegendeOklidMesafesi()   // dx=3, dz=4
    => Assert.AreEqual(5f, LDRules.HorizontalGap(new Bounds(Vector3.zero, Vector3.one*2), new Bounds(new Vector3(5,0,6), Vector3.one*2)), 1e-4f);
[Test] public void Bosluk_YukseklikFarkiniYoksayar()
    => Assert.AreEqual(1f, LDRules.HorizontalGap(new Bounds(Vector3.zero, Vector3.one), new Bounds(new Vector3(2,10,0), Vector3.one)), 1e-4f);
[Test] public void Yukseklik_ZemineGomuluSiper() {   // zemin üstü y=0, siper -0.2..1.0
    var list = new List<LDPiece>{ P(LDRoleType.Zemin, new Vector3(0,-0.1f,0), new Vector3(20,0.2f,20)) };
    Assert.AreEqual(1f, LDRules.HeightAboveFloor(list, new Bounds(new Vector3(0,0.4f,0), new Vector3(3,1.2f,0.5f))), 1e-4f); }
[Test] public void Yukseklik_ZeminYoksaSifirdanOlculur()
    => Assert.AreEqual(1f, LDRules.HeightAboveFloor(new List<LDPiece>(), new Bounds(new Vector3(0,0.5f,0), new Vector3(1,1,1))), 1e-4f);
[Test] public void Yukseklik_UstPlatformaGoreDegilAltZemineGore() {
    var list = new List<LDPiece>{ P(LDRoleType.Zemin, new Vector3(0,-0.1f,0), new Vector3(20,0.2f,20)),
                                  P(LDRoleType.Zemin, new Vector3(0,3.9f,0), new Vector3(20,0.2f,20)) };
    Assert.AreEqual(1f, LDRules.HeightAboveFloor(list, new Bounds(new Vector3(0,0.5f,0), new Vector3(3,1,0.5f))), 1e-4f); }
```

- [ ] **Step 3: Testlerin derlenmediğini/başarısız olduğunu doğrula** (Global Constraints'teki komut). Beklenen: `LDRules` tanımsız olduğu için derleme hatası.

- [ ] **Step 4: `LDPiece`, `LDFinding` ve `LDRules` ölçüm yardımcılarını yaz.**
  - `HorizontalGap`: XZ dikdörtgenleri arası Öklid mesafesi; `dx = max(0, max(a.min.x - b.max.x, b.min.x - a.max.x))`, `dz` aynı şekilde; `sqrt(dx²+dz²)`.
  - `FloorTopUnder`: `Role == Zemin` olan, XZ'de `b` ile çakışan (`HorizontalGap == 0`) ve üst yüzeyi `b.min.y + 0.1f` değerini aşmayan zeminlerin en yüksek `max.y` değeri; hiç yoksa `0f`.
  - `HeightAboveFloor = b.max.y - FloorTopUnder(pieces, b)`.

- [ ] **Step 5: Testleri çalıştır.** Beklenen: `LDMeasureTests` 6/6 PASS.

- [ ] **Step 6: Commit** (`.meta` dosyalarıyla) — `git add Assets/_LevelProject/LevelDesign && git commit -m "LD: assembly'ler, LDRole ve ölçüm yardımcıları"`

---

### Task 2: Kural motoru (kural 1–8, 11) ve siper zinciri bağlantıları

**Files:**
- Modify: `Assets/_LevelProject/LevelDesign/Editor/LDRules.cs`
- Test: `Assets/_LevelProject/LevelDesign/Tests/Editor/LDRulesTests.cs`

**Interfaces:**
- Consumes: Task 1'deki tipler ve yardımcılar.
- Produces:
  - `public static List<LDFinding> LDRules.Evaluate(IReadOnlyList<LDPiece> pieces, IReadOnlyList<LDPiece> unassigned)`. `unassigned`, rolü olmayan ProBuilder parçalarıdır; `Role` alanı yok sayılır, kural 11 içindir.
  - `public readonly struct LDChainLink { public readonly LDPiece From; public readonly LDPiece To; public readonly float Distance; }` ve `public static List<LDChainLink> LDRules.ChainLinks(IReadOnlyList<LDPiece> pieces)`: her siper/blok için en yakın başka siper/bloğa tek bağlantı.
  - Yardımcı kümeler: `static bool IsObstacle(LDRoleType r)` → Duvar, AlcakSiper, YuksekSiper, Blok. `static bool IsCover(LDRoleType r)` → AlcakSiper, YuksekSiper, Blok.

Kural davranışı (mesajlar Türkçe; bulgunun `Position` alanı parçanın merkezi, ikili kurallarda iki merkezin ortası; `Target` = `Source`):
- **1:** `AlcakSiper` yüksekliği `[0.9-ε, 1.2+ε]` dışında → Hata, ör. "Alçak siper 0,75 m: 0,9–1,2 m olmalı".
- **2:** `YuksekSiper`/`Blok` yüksekliği `< 1.9-ε` → Hata.
- **3:** `Kapi` için genişlik = `max(size.x, size.z)`. `< 1.2-ε` → Hata, `< 1.5-ε` → Uyarı. Kapı yüksekliği `= min.y - FloorTopUnder` değeri `< 2.2-ε` → Hata.
- **4:** Engel ikilileri için `GapTouch < gap < GapMin-ε` → Uyarı (ikili başına bir kez).
- **5:** `ChainLinks` üzerinden: `Distance > 6+ε` → Uyarı (kaynak parçada). Tek siper/blok varsa o parça için Uyarı. Bir ikili `< 3-ε` ise Bilgi, ikili başına bir kez (A→B ve B→A tekrar etmez).
- **6:** `OyuncuGirisi` sayısı 0 → Hata. `DusmanGirisi` sayısı 0 → Hata.
- **7:** `DusmanGirisi` sayısı tam 1 → Uyarı.
- **8:** Her oyuncu girişi için en yakın düşman girişine XZ merkez mesafesi `< 10-ε` → Uyarı.
- **11:** `unassigned` içindeki her parça → Bilgi "Rolü yok: <ad>".

- [ ] **Step 1: Failing testleri yaz** — `LDRulesTests.cs`. Yardımcılar: `P(...)` (Task 1'deki gibi), `Zemin()` = merkez (0,-0.1,0), boyut (40,0.2,40); `Siper(x,z,h)` = `AlcakSiper` merkez (x,h/2,z), boyut (3,h,0.5); `Has(list, rule, sev)` ve `Count(list, rule, sev)` LINQ yardımcıları.

```csharp
[TestCase(0.9f, true)] [TestCase(1.2f, true)] [TestCase(0.89f, false)] [TestCase(1.21f, false)]
public void Kural1_SinirDegerleriGecer(float h, bool ok)
    => Assert.AreEqual(!ok, Has(LDRules.Evaluate(L(Zemin(), Siper(0,0,h)), None), 1, LDSeverity.Hata));
[TestCase(1.89f, true)] [TestCase(1.9f, false)]
public void Kural2_YuksekSiper(float h, bool hata) { /* YuksekSiper (0,h/2,0) boyut (1,h,1) */ }
[TestCase(1.19f, LDSeverity.Hata)] [TestCase(1.2f, LDSeverity.Uyari)] [TestCase(1.49f, LDSeverity.Uyari)]
public void Kural3_KapiGenisligi(float w, LDSeverity sev) { /* Kapi merkez (0,3.25,0) boyut (w,1.5,0.5); Count(...,3,sev)==1 */ }
[Test] public void Kural3_KapiGenisligi15Temiz() { /* w=1.5 → kural 3 bulgusu yok */ }
[Test] public void Kural3_AlcakLentoHata() { /* lento alt kenarı 2.1 m → Hata */ }
[TestCase(0.04f, 0)] [TestCase(1.19f, 1)] [TestCase(1.2f, 0)]
public void Kural4_DarGecit(float gap, int beklenen) { /* iki Duvar, aradaki X boşluğu = gap → Count(...,4,Uyari) */ }
[Test] public void Kural4_KapiVeGirisEngelSayilmaz() { /* Duvar'a 0.5 m uzaklıkta Kapi ve DusmanGirisi → kural 4 yok */ }
[Test] public void Kural5_YalnizSiper() { /* iki siper arası kenar mesafesi 6.01 → her iki siper için Uyarı (Count==2) */ }
[Test] public void Kural5_YakinIkiliTekBilgi() { /* kenar mesafesi 2.0 → Count(...,5,Bilgi)==1 */ }
[Test] public void Kural5_TekSiperUyari() { /* tek AlcakSiper → Count(...,5,Uyari)==1 */ }
[Test] public void Kural6_GirisYoksaHata() { /* giriş yok → Count(...,6,Hata)==2 */ }
[Test] public void Kural7_TekDusmanGirisi() { /* 1 oyuncu + 1 düşman girişi → Count(...,7,Uyari)==1; 2 düşman → 0 */ }
[Test] public void Kural8_NoktaIsaretleriyleMesafe() {
    // OyuncuGirisi (0,1,0) boyut (1,2,1); DusmanGirisi boyutu Vector3.zero, (9.99,0,0) → Uyarı; (10,0,0) → yok
}
[Test] public void Kural11_RolsuzParca() { /* unassigned 2 parça → Count(...,11,Bilgi)==2 */ }
[Test] public void ZincirBaglantilari_EnYakiniSecer() { /* 3 siper x=0, x=5, x=20 → x=20'nin bağlantısı x=5'e, Distance==12 (3 m genişlik çıkarılmış) */ }
```

- [ ] **Step 2: Testleri çalıştır.** Beklenen: `Evaluate`/`ChainLinks` tanımsız, derleme hatası.
- [ ] **Step 3: `Evaluate`, `ChainLinks`, `LDChainLink` yaz** (yukarıdaki kural davranışı; ikililerde `i < j` döngüsü).
- [ ] **Step 4: Testleri çalıştır.** Beklenen: `LDMeasureTests` + `LDRulesTests` hepsi PASS.
- [ ] **Step 5: Commit** — `"LD: kural motoru (1–8, 11) ve siper zinciri"`

---

### Task 3: Tarama sonucu, ders modeli ve Ders 1 içeriği

**Files:**
- Create: `Assets/_LevelProject/LevelDesign/Editor/LDScanResult.cs`
- Create: `Assets/_LevelProject/LevelDesign/Editor/LDLesson.cs` (`LDLesson`, `LDStep`, `LDStepResult`, `LDTableRow`)
- Create: `Assets/_LevelProject/LevelDesign/Editor/Lessons/Ders01Arena.cs`
- Create: `Assets/_LevelProject/LevelDesign/Editor/LDLessons.cs` (ders kayıt listesi)
- Test: `Assets/_LevelProject/LevelDesign/Tests/Editor/Ders01ArenaTests.cs`

**Interfaces:**
- Consumes: `LDRules.Evaluate`, `LDRules.HeightAboveFloor`, `LDRules.ChainLinks`.
- Produces:
  - `public readonly struct LDSightLine { public readonly Vector3 From, To; public readonly bool Blocked; }`
  - `public sealed class LDScanResult { public string ScenePath; public List<LDPiece> Pieces; public List<LDFinding> Findings; public List<LDChainLink> Links; public List<LDSightLine> SightLines; public int Count(LDRoleType r); public IEnumerable<LDPiece> OfRole(LDRoleType r); public IEnumerable<LDFinding> ForRules(int[] rules); public bool HasError(params int[] rules); public static LDScanResult FromPieces(string scenePath, List<LDPiece> pieces, List<LDPiece> unassigned); public static readonly LDScanResult Empty; }`. `FromPieces`, `Findings` ve `Links`'i `LDRules`'tan doldurur; `SightLines` boş başlar.
  - `public sealed class LDTableRow { public string Name; public LDRoleType Role; public Vector3 Position; public Vector3 Scale; }`
  - `public sealed class LDStepResult { public bool Passed => Problems.Count == 0; public readonly List<string> Problems = new List<string>(); public LDStepResult Require(bool condition, string problem); }`
  - `public sealed class LDStep { public string Title, What, Why; public LDTableRow[] Table; public (string Term, string Meaning)[] Terms; public int[] Rules; public Func<LDScanResult, LDStepResult> Check; public string[] ManualChecks; public string ActionLabel; public Action Action; public bool IsPassed(LDScanResult scan, Func<int, bool> manualChecked); }`. `IsPassed`: `Check` null ya da geçti VE bütün `ManualChecks` işaretli.
  - `public sealed class LDLesson { public string Id, Title; public LDStep[] Steps; }`
  - `public static class Ders01Arena { public const string Id = "ders01-arena"; public static readonly LDTableRow[] Table; public static readonly string[] VillaScenes; public static LDLesson Create(Action createLessonScene); }`
  - `public static class LDLessons { public static IReadOnlyList<LDLesson> All { get; } }` (Task 5 sahne kurulumunu bağlayana kadar `Ders01Arena.Create(null)`)

`Ders01Arena.Table` spec'teki 19 satır + `ScaleRef` (`OyuncuGirisi`, (-6,1,-10.5), (1,1,1); kapsül bounds boyutu (1,2,1)) + `Dusman_Kapi` ((0,0,12.8), Scale (0,0,0)) + `Dusman_Koridor` ((11.5,0,12.5), Scale (0,0,0)). Roller: Zemin → `Zemin`, `Duvar_*`/`Koridor_*` → `Duvar`, `Lento_*` → `Kapi`, `Orta_Blok` → `Blok`, `Siper_*` → `AlcakSiper`. Her adımın `Table` alanı bu dizinin ilgili satırlarıdır.

Adımlar (Ne/Neden metinleri Türkçe, kısa ve sohbetteki Ders 1 anlatımıyla tutarlı; `Terms` sohbetteki sözlükten ilgili terimler):

| # | Check | Rules |
|---|---|---|
| 0 Hazırlık | `ScenePath` boş değil ("Sahneyi kaydet"); `VillaScenes` içinde değil ("Villa sahnesinde değil, ders sahnesinde çalış") | — |
| 1 Zemin | `Count(Zemin) ≥ 1`; en büyük zeminin XZ kenarları sıralanınca ≥ 20 ve ≥ 24 | — |
| 2 Duvarlar | `Count(Duvar) ≥ 4`; her duvar `HeightAboveFloor ≥ 3.5-ε` | — |
| 3 Kapılar | `Count(Kapi) ≥ 2`; `!HasError(3)` | 3 |
| 4 Orta blok | `Count(Blok) ≥ 1`; `!HasError(2)` | 2 |
| 5 Siperler | `Count(AlcakSiper) ≥ 5`; `!HasError(1)` | 1, 5 |
| 6 Koridor ve girişler | `!HasError(6)`. `Why` metni kural 9 uyarısını ilk tasarım kararı olarak anlatır | 6, 7, 8, 9 |
| 7 NavMesh | `!HasError(10)` | 10, 4 |
| 8 Oyun testi | — ; `ManualChecks`: "Orta bloğun etrafında tam tur attım", "Oyuncu girişinden arenaya baktım", "Düşmanın koridordan geleceği yolu yürüdüm" | — |

0. adımın `ActionLabel`'ı "Ders sahnesini oluştur", `Action`'ı `createLessonScene`.

- [ ] **Step 1: Failing testleri yaz** — `Ders01ArenaTests.cs`. Yardımcı `TablePieces()`: `Ders01Arena.Table`'dan `new Bounds(Position, Scale)` ile parça listesi (ScaleRef için boyut (1,2,1)). `Scan(pieces) => LDScanResult.FromPieces("Assets/_LevelProject/LevelDesign/Scenes/LD_Ders01_Arena.unity", pieces, new List<LDPiece>())`.

```csharp
[Test] public void Tablo_Kural1den8eHataVeUyariUretmez()
    => CollectionAssert.IsEmpty(Scan(TablePieces()).Findings.Where(f => f.Severity != LDSeverity.Bilgi));
[Test] public void Tablo_Adim1den7yeKadarGecer() {
    var lesson = Ders01Arena.Create(null); var scan = Scan(TablePieces());
    for (int i = 0; i <= 7; i++) Assert.IsTrue(lesson.Steps[i].IsPassed(scan, _ => true), lesson.Steps[i].Title);
}
[Test] public void Adim0_KaydedilmemisSahneGecmez() { /* ScenePath "" → Steps[0].Check(...).Problems "Sahneyi kaydet" içerir */ }
[Test] public void Adim0_VillaSahnesindeGecmez() { /* "Assets/_LevelProject/FPS/Scenes/Villa_Operation.unity" → geçmez */ }
[Test] public void Adim5_DortSiperleGecmez() { /* Siper_E çıkarılınca Steps[5] geçmez */ }
[Test] public void Adim2_AlcakDuvarlaGecmez() { /* bir duvarın Scale.y'si 3 → Steps[2] geçmez */ }
[Test] public void Adim8_ElleIsaretlenmedenGecmez() { /* manualChecked => false → Steps[8].IsPassed false; true → true */ }
[Test] public void DersKaydi_Ders01Icerir() => Assert.AreEqual(Ders01Arena.Id, LDLessons.All[0].Id);
```

- [ ] **Step 2: Testleri çalıştır.** Beklenen: tipler tanımsız, derleme hatası.
- [ ] **Step 3: `LDScanResult`, `LDLesson.cs` tiplerini, `Ders01Arena` ve `LDLessons`'ı yaz.** `VillaScenes` = `"Assets/_LevelProject/FPS/Scenes/Villa_Operation.unity"`, `"Assets/_LevelProject/Scenes 1/Industrial_Level_01.unity"`.
- [ ] **Step 4: Testleri çalıştır.** Beklenen: üç test dosyası da PASS. `Tablo_Kural1den8eHataVeUyariUretmez` kırılırsa tabloyu değil kuralı düzelt: tablo sohbette elle doğrulandı (spec'teki kural 9 notu sahne gerektirir, bu testte yoktur).
- [ ] **Step 5: Commit** — `"LD: tarama sonucu, ders modeli ve Ders 1"`

---

### Task 4: Sahne tarayıcı, tarama servisi ve rol atama

**Files:**
- Create: `Assets/_LevelProject/LevelDesign/Editor/LDSceneScanner.cs`
- Create: `Assets/_LevelProject/LevelDesign/Editor/LDScanService.cs`
- Create: `Assets/_LevelProject/LevelDesign/Editor/LDRoleAssigner.cs`
- Test: `Assets/_LevelProject/LevelDesign/Tests/Editor/LDRoleAssignerTests.cs`

**Interfaces:**
- Consumes: `LDScanResult.FromPieces`, `LDRules` sabitleri, `LDPiece`, `LDFinding`, `LDSightLine`.
- Produces:
  - `public static class LDSceneScanner { public static LDScanResult Scan(); }`
  - `[InitializeOnLoad] public static class LDScanService { public static LDScanResult Last { get; } public static event Action Changed; public static void RequestScan(); public static void ScanNow(); }`
  - `public static class LDRoleAssigner { public static int Assign(GameObject[] targets, LDRoleType role); }` (eklenen ya da güncellenen nesne sayısını döndürür; Undo kaydı "LD rol ata")

Davranış:
- `Scan`: aktif sahnedeki `activeInHierarchy` olan bütün `LDRole`'ler (`Object.FindObjectsByType<LDRole>(FindObjectsSortMode.None)`). Bounds = nesne ve çocuklarındaki `Renderer.bounds` birleşimi; renderer yoksa `Collider.bounds` birleşimi; o da yoksa `new Bounds(transform.position, Vector3.zero)`. `unassigned` = rolü olmayan, kendisinde ve üst nesnelerinde `LDRole` bulunmayan `ProBuilderMesh` nesneleri. Play modunda da çalışır.
- Kural 9: her (oyuncu girişi, düşman girişi) ikilisi için `from = oyuncu bounds.min.y + EyeHeight`, `to = düşman merkezinin XZ'si, y = FloorTopUnder + EyeHeight`. `Physics.Linecast` + `QueryTriggerInteraction.Ignore`; oyuncu girişinin kendi collider'larına ve hedef nesnenin collider'larına çarpma engel sayılmaz (`Physics.RaycastAll` ile sıralı vuruşları gez). Engel yoksa Uyarı "Oyuncu girişinden düşman girişi doğrudan görünüyor". Her ikili bir `LDSightLine` olarak eklenir.
- Kural 10: `NavMesh.CalculateTriangulation().vertices.Length == 0` → tek Hata "NavMesh bake edilmemiş". Aksi halde her düşman girişi için `NavMesh.SamplePosition(merkez, out hit, NavSampleRadius, NavMesh.AllAreas)` her iki uçta; örnekleme başarısızsa Hata "<ad> NavMesh üstünde değil"; `NavMesh.CalculatePath` sonucu `PathComplete` değilse Hata "<ad> oyuncu girişine yürüyemiyor".
- `LDScanService`: `EditorApplication.hierarchyChanged`, `Undo.undoRedoPerformed`, `ObjectChangeEvents.changesPublished` ve `EditorSceneManager.activeSceneChangedInEditMode` olaylarında `RequestScan()`. `RequestScan` 0,5 sn sonrasına bir tarama planlar (`EditorApplication.timeSinceStartup` ile `EditorApplication.update` içinde). Tarama bitince `Changed` tetiklenir.

- [ ] **Step 1: Failing testleri yaz** — `LDRoleAssignerTests.cs` (oluşturulan GameObject'ler `TearDown`'da `Object.DestroyImmediate` ile silinir):

```csharp
[Test] public void Assign_BilesenEklerVeRolVerir() { /* 2 yeni GO → Assign(...,AlcakSiper)==2; ikisinde de role==AlcakSiper */ }
[Test] public void Assign_VarolanRoluGunceller() { /* LDRole(Duvar) olan GO → Assign(...,Blok)==1; GetComponents<LDRole>().Length==1, role==Blok */ }
[Test] public void Assign_BosDiziSifir() => Assert.AreEqual(0, LDRoleAssigner.Assign(new GameObject[0], LDRoleType.Zemin));
```

- [ ] **Step 2: Testleri çalıştır.** Beklenen: `LDRoleAssigner` tanımsız.
- [ ] **Step 3: `LDRoleAssigner`'ı yaz** (`Undo.AddComponent` / `Undo.RecordObject`).
- [ ] **Step 4: `LDSceneScanner` ve `LDScanService`'i yaz** (yukarıdaki davranış).
- [ ] **Step 5: Testleri çalıştır.** Beklenen: bütün testler PASS, `Editor.log`'da `error CS` yok.
- [ ] **Step 6: Commit** — `"LD: sahne tarayıcı, tarama servisi, rol atama"`

---

### Task 5: İlerleme, ders sahnesi kurulumu ve ders penceresi

**Files:**
- Create: `Assets/_LevelProject/LevelDesign/Editor/LDProgress.cs`
- Create: `Assets/_LevelProject/LevelDesign/Editor/LDLessonSceneSetup.cs`
- Create: `Assets/_LevelProject/LevelDesign/Editor/LDLessonWindow.cs`
- Modify: `Assets/_LevelProject/LevelDesign/Editor/LDLessons.cs` (`Ders01Arena.Create(LDLessonSceneSetup.CreateOrOpen)`)
- Test: `Assets/_LevelProject/LevelDesign/Tests/Editor/LDProgressTests.cs`

**Interfaces:**
- Consumes: `LDLessons.All`, `LDStep.IsPassed`, `LDScanService.Last/Changed/ScanNow`, `LDRoleAssigner.Assign`, `LDScanResult.ForRules`.
- Produces:
  - `public static class LDProgress { public static string LessonId { get; set; } public static int GetStep(string lessonId); public static void SetStep(string lessonId, int step); public static bool GetManual(string lessonId, int step, int index); public static void SetManual(string lessonId, int step, int index, bool value); internal static string Key(string suffix); }`. Anahtar `"LD." + Hash128.Compute(Application.dataPath) + "." + suffix`, değerler `EditorPrefs`'te.
  - `public static class LDLessonSceneSetup { public const string ScenePath = "Assets/_LevelProject/LevelDesign/Scenes/LD_Ders01_Arena.unity"; public static void CreateOrOpen(); }`
  - `public sealed class LDLessonWindow : EditorWindow` — `[MenuItem("Tools/Level Design/Ders Penceresi")]` ve `[MenuItem("Tools/Level Design/Sahneyi Denetle")]` (ikincisi pencereyi Denetim sekmesinde açar).

Davranış:
- `CreateOrOpen`: önce `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()`; kullanıcı iptal ederse dur. Sahne varsa `OpenScene` ile aç. Yoksa `Scenes` ve `Materials` klasörlerini oluştur, `NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single)` ile sahneyi kur, boş bir `Arena` nesnesi ekle, `SaveScene`. Malzemeler yoksa `Materials/LD_Gri.mat` (0.6,0.6,0.6), `LD_Sari.mat` (0.95,0.75,0.2), `LD_Kirmizi.mat` (0.85,0.25,0.2) oluşturulur. Shader `Shader.Find("Universal Render Pipeline/Lit")`; bulunamazsa `GraphicsSettings.currentRenderPipeline?.defaultShader` ile birlikte `Debug.LogWarning`. Renk `_BaseColor` varsa ona, yoksa `material.color`'a yazılır.
- Pencere düzeni (spec "LDLessonWindow"):
  - Üstte `Ders` / `Denetim` sekmeleri (`GUILayout.Toolbar`).
  - **Ders sekmesi:** ders seçimi (`EditorGUILayout.Popup`); adım listesi (✓ geçti, ✗ geçmedi, • elle kontrolsüz; tıklayınca seçilir); seçili adımın başlık, Ne yapıyoruz, Neden metinleri (`EditorStyles.wordWrappedLabel`); tablo (Ad | Konum | Ölçek); terimler; `ActionLabel` düğmesi; elle kontrol kutuları (`LDProgress`'e yazılır); `Check` sorunları (HelpBox Warning); `Rules`'a ait bulgular.
  - **Denetim sekmesi:** `Last.Findings` önem sırasıyla.
  - Her bulgu satırı: önem ikonu (`EditorGUIUtility.IconContent("console.erroricon.sml" / "console.warnicon.sml" / "console.infoicon.sml")`), mesaj ve `Target` varsa **Göster** düğmesi (`Selection.activeObject = Target; SceneView.lastActiveSceneView?.FrameSelected();`).
  - Alt bölümde "Rol ata" başlığı ve her rol için bir düğme ("Seçilenler → Alçak siper" vb.); seçim boşsa düğmeler kapalı. Hiç rol yoksa üstte HelpBox Info "Önce parçalara rol ver" görünür.
  - En altta "Kontrol et" düğmesi (`LDScanService.ScanNow()`) ve ölçüm sınırı notu ("Eğik açılı parçalar yaklaşık ölçülür").
  - `OnEnable`'da `LDScanService.Changed += Repaint`; `OnDisable`'da çıkar. Play modunda rol düğmeleri ve `Action` kapalı (`EditorGUI.DisabledScope(Application.isPlaying)`).

- [ ] **Step 1: Failing testleri yaz** — `LDProgressTests.cs` (her test kendi `lessonId`'si olarak `"test-" + Guid.NewGuid()` kullanır, `TearDown`'da `EditorPrefs.DeleteKey` ile anahtarları temizler):

```csharp
[Test] public void Adim_KaydedilirVeOkunur() { LDProgress.SetStep(id, 4); Assert.AreEqual(4, LDProgress.GetStep(id)); }
[Test] public void Adim_VarsayilanSifir() => Assert.AreEqual(0, LDProgress.GetStep(id));
[Test] public void ElleKontrol_AdimVeSiraylaAyrilir() { LDProgress.SetManual(id, 8, 1, true); Assert.IsTrue(LDProgress.GetManual(id, 8, 1)); Assert.IsFalse(LDProgress.GetManual(id, 8, 0)); }
[Test] public void Anahtar_ProjeyeOzgu() => StringAssert.StartsWith("LD." + Hash128.Compute(Application.dataPath), LDProgress.Key("x"));
```

- [ ] **Step 2: Testleri çalıştır.** Beklenen: `LDProgress` tanımsız.
- [ ] **Step 3: `LDProgress`'i yaz.** Testleri çalıştır, PASS.
- [ ] **Step 4: `LDLessonSceneSetup` ve `LDLessonWindow`'u yaz, `LDLessons`'ı güncelle.** Testleri tekrar çalıştır, hepsi PASS ve `error CS` yok.
- [ ] **Step 5: Elle doğrulama** (kullanıcıyla; sonuçlar PASS/FAIL/NOT RUN olarak raporlanır):
  - Tools → Level Design → Ders Penceresi açılıyor; Türkçe karakterler doğru görünüyor.
  - "Ders sahnesini oluştur" `LD_Ders01_Arena` sahnesini, `Arena` nesnesini ve 3 malzemeyi oluşturuyor; ikinci basışta üzerine yazmadan açıyor.
  - Bir ProBuilder küpüne "Seçilenler → Zemin" rolü veriliyor, Ctrl+Z geri alıyor.
  - Unity kapatılıp açılınca seçili adım ve elle kontrol kutuları korunuyor.
- [ ] **Step 6: Commit** — `"LD: ders penceresi, ilerleme ve ders sahnesi kurulumu"`

---

### Task 6: Scene görünümü katmanı

**Files:**
- Create: `Assets/_LevelProject/LevelDesign/Editor/LDSceneOverlay.cs` (aç/kapa Overlay)
- Create: `Assets/_LevelProject/LevelDesign/Editor/LDSceneDrawer.cs` (Handles çizimi)

**Interfaces:**
- Consumes: `LDScanService.Last/Changed`, `LDScanResult.Pieces/Links/SightLines/Findings`, `LDRules.ChainMin/ChainMax/HeightAboveFloor/FloorTopUnder`.
- Produces:
  - `[Overlay(typeof(SceneView), "ld-gorunumu", "LD Görünümü")] public sealed class LDSceneOverlay : Overlay`: `CreatePanelContent` içinde `LDSceneDrawer.Enabled`'a bağlı tek bir `Toggle` ("Ölçüleri göster").
  - `[InitializeOnLoad] public static class LDSceneDrawer { public static bool Enabled { get; set; } public static Color RoleColor(LDRoleType r); }`. `Enabled`, `EditorPrefs` anahtarı `LDProgress.Key("overlay")`'de saklanır; değişince `SceneView.RepaintAll()`.

Çizim (yalnız `Enabled` iken, `SceneView.duringSceneGui`):
- Her parça rol renginde tel kafes kutu (`Handles.DrawWireCube`) ve üstünde `Handles.Label` ile Türkçe rol adı. Boyutsuz işaretler için disk çizilir.
- Renkler: Zemin gri (0.5), Duvar koyu gri (0.35), Alçak siper sarı (0.95,0.75,0.2), Yüksek siper turuncu (0.95,0.5,0.15), Blok mavi-gri (0.45,0.55,0.7), Kapı camgöbeği (0.2,0.8,0.9), Oyuncu girişi yeşil (0.2,0.8,0.4), Düşman girişi kırmızı (0.9,0.25,0.2).
- Her alçak siperin üstünde, zemin + 1 m yüksekliğinde yatay çizgi.
- `Links`: iki parçanın en yakın kenar noktaları arasında çizgi ve ortasında "x,x m" etiketi; `ChainMin ≤ d ≤ ChainMax` yeşil, değilse sarı.
- Kural 4 bulguları: `Position`'da kırmızı disk.
- `SightLines`: engelliyse yeşil, açıksa sarı kesikli çizgi (`Handles.DrawDashedLine`).

- [ ] **Step 1: İki dosyayı yaz.**
- [ ] **Step 2: Testleri çalıştır** (regresyon). Beklenen: hepsi PASS, `error CS` yok.
- [ ] **Step 3: Elle doğrulama** (kullanıcıyla): Scene görünümünde Overlays menüsünden "LD Görünümü" açılıyor; tablo ölçülerinde kurulmuş birkaç siperde zincir çizgileri ve mesafe etiketleri doğru renkte; 1 m çizgisi siperin üstüne oturuyor; ayar Unity yeniden açılınca korunuyor.
- [ ] **Step 4: Commit** — `"LD: Scene görünümü katmanı"`

---

### Task 7: Uçtan uca doğrulama

**Files:** yok (yalnız doğrulama; hata çıkarsa ilgili task'ın dosyası düzeltilir ve o task'ın testi eklenir).

- [ ] **Step 1: Bütün testleri çalıştır.** Beklenen: `LevelDesign.Tests` hepsi PASS. Sayıyı raporla.
- [ ] **Step 2: Kullanıcıyla Ders 1 provası.** Kullanıcı pencereyi izleyerek 0–7. adımları kurar. Her adımda ✓ olması, 6. adımda kural 9 uyarısının çıkması ve 7. adımda NavMesh bake edilince kural 10'un temizlenmesi beklenir. Bake'ten önce "NavMesh bake edilmemiş" Hatası görünmeli.
- [ ] **Step 3: Sonuçları raporla.** Her elle kontrolü PASS / FAIL / NOT RUN olarak listele. Spec'teki "Elle kontrol" maddeleri: kural 9–10, pencere ve katman görünüşü, rol atama + Undo, yeniden açılışta ilerleme.
- [ ] **Step 4: Gerekirse son düzeltmeleri commit'le.**
