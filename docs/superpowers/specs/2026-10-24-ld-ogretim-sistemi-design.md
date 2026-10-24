# Level Design öğretim sistemi — tasarım

Tarih: 2026-10-24 · Durum: onay bekliyor

## Amaç

Kullanıcı level design'ı sıfırdan öğreniyor. Hedef, FEAR tarzı düşman yapay zekâsına (siper, flank, kuşatma) uygun FPS arenaları kurmak. Level'lar ProBuilder ile greybox olarak kuruluyor. Kullanıcının zamanı kısıtlı ve terimlere yabancı.

Sistem Unity Editor'ün içinde üç iş yapar:
1. Dersi adım adım, Türkçe anlatır.
2. Kullanıcının kurduğu sahneyi otomatik denetler ve sorunları gösterir.
3. Ölçüleri ve ilişkileri Scene görünümünde çizer.

**Başarı ölçütü:** Kullanıcı Ders 1 arenasını yalnızca bu pencereye bakarak kurabilmeli. Her adımda neyin eksik ya da yanlış olduğunu, nedenini ve sorunun sahnedeki yerini görebilmeli.

**Kullanıcının söyledikleri:** Hepsi bir arada bir sistem (ders + denetim + Scene çizimi) istiyor. Yaklaşım A seçildi: dersler kodla yazılır, sahne parçaları rol bileşeniyle işaretlenir.
**Varsayımlar:** Sistem yalnız Editor'de çalışır; `LDRole` dışında build'e bir şey girmez. Mevcut Villa sahnelerine ve kodlarına dokunulmaz.

## Kapsam dışı

Hayalet yerleştirme rehberleri, hazır parça kiti, yapay zekâ kodu, Ders 2 ve sonrası. Sistem bunları sonradan eklemeye uygun kurulur.

## Dosya düzeni

```
Assets/_LevelProject/LevelDesign/
  LevelDesign.asmdef                 runtime; Assembly-CSharp otomatik görür
  LDRole.cs                          rol bileşeni + LDRoleType enum
  Editor/
    LevelDesign.Editor.asmdef        yalnız Editor
    LDPiece.cs                       denetlenen parçanın verisi
    LDFinding.cs                     bulgu (önem, kural no, mesaj, nesne, konum)
    LDRules.cs                       saf kural motoru (kural 1–8, 11)
    LDSceneScanner.cs                sahneden parça toplama + kural 9–10
    LDLesson.cs                      ders ve adım tanımı
    Lessons/Ders01Arena.cs           Ders 1 içeriği
    LDProgress.cs                    EditorPrefs ile ilerleme
    LDLessonWindow.cs                Tools → Level Design → Ders Penceresi
    LDSceneOverlay.cs                Scene görünümü katmanı
  Tests/Editor/
    LevelDesign.Tests.asmdef
    LDRulesTests.cs
```

Ders sahnesi ve malzemeler düğmeyle `LevelDesign/Scenes` ve `LevelDesign/Materials` altında oluşturulur.

## Bileşenler

### LDRole (runtime)

`MonoBehaviour`. Tek alanı `LDRoleType role` olan bir bileşen. Roller: `Zemin`, `Duvar`, `AlcakSiper`, `YuksekSiper`, `Blok`, `Kapi`, `OyuncuGirisi`, `DusmanGirisi`. Inspector'da Türkçe adlarla görünür. Kodu çalışmaz; yalnız veri taşır. Ders 2'de yapay zekâ siperleri bu işaretlerden okuyacak.

Kurallar:
- `Kapi` rolü lentoya verilir.
- `OyuncuGirisi` rolü `ScaleRef` kapsülüne verilir. Projedeki `LevelPlaytestBootstrap`, Play'de oyuncuyu bu kapsülün yerine koyar.
- `DusmanGirisi` boş bir GameObject'e verilebilir.

### LDPiece ve ölçüm

Sahneden her `LDRole` için bir `LDPiece` oluşur: ad, rol, nesne kimliği, dünya sınır kutusu. Kutu sırasıyla `Renderer.bounds`, `Collider.bounds` ya da (ikisi de yoksa) konumdaki bir noktadan alınır.

- **Yükseklik** = kutunun üst kenarı − altındaki zeminin üst yüzeyi. Zemin, XZ'de çakışan `Zemin` parçalarının en yükseğidir; çakışan zemin yoksa 0 alınır. Böylece zemine gömülmüş parçalar da doğru ölçülür.
- **Ölçüm sınırı:** 90° döndürülmüş parçalar doğru ölçülür; eğik açılardaki parçalar sınır kutusu büyüdüğü için yaklaşık ölçülür. Ders penceresi bunu not olarak gösterir.

### LDRules (saf mantık)

Girdi: `IReadOnlyList<LDPiece>`. Çıktı: `List<LDFinding>`. Önem dereceleri `Hata`, `Uyari`, `Bilgi`.

| # | Kural | Eşik | Önem |
|---|---|---|---|
| 1 | Alçak siper yüksekliği | 0,9–1,2 m dışı | Hata |
| 2 | Yüksek siper / blok yüksekliği | < 1,9 m | Hata |
| 3 | Kapı genişliği (lentonun yatay uzun kenarı) | < 1,2 m Hata, < 1,5 m Uyarı | Hata/Uyarı |
| 3 | Kapı yüksekliği (lento alt kenarı − zemin) | < 2,2 m | Hata |
| 4 | Engeller (Duvar, siperler, Blok) arası XZ boşluğu | 0,05 m < boşluk < 1,2 m | Uyarı |
| 5 | Siper zinciri: her siper/blok için en yakın başka siper/bloğa kenar mesafesi | > 6 m Uyarı; < 3 m Bilgi (ikili başına bir kez) | Uyarı/Bilgi |
| 6 | Oyuncu girişi ve düşman girişi sayısı | 0 ise | Hata |
| 7 | Düşman girişi sayısı (flank) | < 2 | Uyarı |
| 8 | Oyuncu girişi ile en yakın düşman girişi arası | < 10 m | Uyarı |
| 11 | Rolsüz ProBuilder parçası | var | Bilgi |

### LDSceneScanner

Aktif sahnedeki etkin `LDRole` nesnelerini toplar, `LDRules`'u çalıştırır ve sahne gerektiren iki kuralı ekler:

| # | Kural | Yöntem | Önem |
|---|---|---|---|
| 9 | Oyuncu girişinden düşman girişi doğrudan görünüyor mu | Zemin + 1,6 m'den düşman girişi + 1,6 m'ye `Physics.Linecast` (tetikleyiciler ve `ScaleRef` kendi collider'ları hariç). Engel yoksa görünüyor sayılır | Uyarı |
| 10 | NavMesh ve yürüme yolu | `NavMesh.CalculateTriangulation` boşsa "bake edilmemiş". Değilse her düşman girişinden oyuncu girişine `NavMesh.SamplePosition` (2 m) + `NavMesh.CalculatePath`; `PathComplete` değilse | Hata |

Rolsüz ProBuilder parçalarını bulmak için `ProBuilderMesh` bileşeni olan ve `LDRole` taşımayan nesneler listelenir (kural 11). Bunun için `LevelDesign.Editor.asmdef`, `Unity.ProBuilder` assembly'sine referans verir. NavMesh sorguları `UnityEngine.AI` çekirdek modülünden gelir.

Sonuç bir `LDScanResult` nesnesidir: parçalar, bulgular ve rol başına sayılar. Pencere ile katman aynı sonucu paylaşır. Tarama sahne değişikliklerinden sonra (hiyerarşi değişimi veya Undo) 0,5 sn gecikmeyle ya da **Kontrol et** düğmesiyle yenilenir.

### LDLesson ve Ders01Arena

Bir `LDLesson` şunları içerir: kimlik, başlık ve adımlar. Her `LDStep` şunları içerir:
- başlık
- **Ne yapıyoruz** metni
- **Neden** metni (FEAR yapay zekâsıyla bağlantısı)
- ölçü tablosu satırları (ad, konum, ölçek)
- terimler (terim → kısa tanım)
- `Func<LDScanResult, LDStepResult>` otomatik kontrol (isteğe bağlı)
- elle işaretlenen kutular (isteğe bağlı)
- isteğe bağlı bir yardımcı eylem düğmesi

Adım şu koşullarda **geçer**: ilgili kurallarda Hata yoksa, gereken parça sayıları tamsa ve elle işaretlenecek kutuların hepsi işaretliyse. Uyarılar adımı engellemez ama listelenir.

Ders 1 adımları:

| Adım | Kullanıcı ne yapar | Otomatik kontrol |
|---|---|---|
| 0 Hazırlık | **Ders sahnesini oluştur**: ışık, kamera, boş `Arena`, Gri/Sarı/Kırmızı URP Lit malzemeler. Sahne zaten varsa üzerine yazmaz, açar | Aktif sahne kayıtlı ve `Villa_Operation` / `Industrial_Level_01` değil |
| 1 Zemin | Zemin, `Zemin` rolü | ≥ 1 zemin; XZ ≥ 20 × 24 m |
| 2 Duvarlar | Dış duvarlar, `Duvar` | ≥ 4 duvar; her biri ≥ 3,5 m |
| 3 Kapılar | Boşluklar + lentolar, `Kapi` | ≥ 2 kapı; kural 3'te Hata yok |
| 4 Orta blok | `Blok` | ≥ 1 blok; kural 2'de Hata yok |
| 5 Siperler | 5 alçak siper | ≥ 5 `AlcakSiper`; kural 1'de Hata yok; kural 5 listelenir |
| 6 Koridor ve girişler | Yan koridor; `ScaleRef` (-6, 1, -10.5) → `OyuncuGirisi`; boş nesneler kapı ağzına (0, 0, 12.8) ve koridor ucuna (11.5, 0, 12.5) → `DusmanGirisi` | Kural 6'da Hata yok; kural 7–9 listelenir |
| 7 NavMesh | NavMesh Surface + Bake | Kural 10'da Hata yok; kural 4 listelenir |
| 8 Oyun testi | Play'de gezme | Elle: "Orta bloğun etrafında tam tur attım", "Oyuncu girişinden arenaya baktım", "Düşmanın koridordan geleceği yolu yürüdüm" |

Ölçü tabloları 2026-10-24 tarihli Ders 1 sohbetindeki koordinat tablosunun aynısıdır.

Bu tabloda oyuncu girişinden kuzey kapısına uzanan görüş hattı orta bloğun köşesini birkaç santimle ıskalar, bu yüzden kural 9 büyük ihtimalle uyarı verir. Bu bilinçli bırakılır: 6. adım metni bunu kullanıcının ilk tasarım kararı olarak sunar (bloğu, kapıyı ya da girişi kaydırarak hattı kesmek).

### LDProgress

Seçili ders, seçili adım ve elle işaretlenen kutular `EditorPrefs`'te tutulur. Anahtarlar proje yoluna göre ayrılır (`LD.<projeHash>.<dersId>...`). Otomatik kontroller kaydedilmez, her açılışta sahneden yeniden hesaplanır.

### LDLessonWindow

`EditorWindow`, IMGUI (mevcut editör kodlarıyla aynı tarz). Menü: **Tools → Level Design → Ders Penceresi**.

İki sekmesi var:
- **Ders:** Ders seçimi, adım listesi (✓/✗/•), seçili adımın metinleri, ölçü tablosu, terimler, yardımcı düğme, elle işaretlenecek kutular ve o adımın bulguları.
- **Denetim:** Ders bağımsız, bütün bulgular.

Her bulgu satırında **Göster** düğmesi nesneyi seçer ve Scene kamerasını ona çevirir (`SceneView.lastActiveSceneView.Frame`).

Rol atama alanında her rol için bir düğme bulunur ("Seçilenler → Alçak siper"). Seçili nesnelere `LDRole` ekler ya da rolünü günceller; Undo desteklidir. Play modunda pencere salt okunur bilgi gösterir.

### LDSceneOverlay

Scene görünümünde **LD Görünümü** adlı bir aç/kapa Overlay. Açıkken `SceneView.duringSceneGui` ile `Handles` kullanarak şunları çizer:
- rol renginde tel kafes kutu ve rol etiketi
- alçak siperlerin üstünde 1 m çizgisi
- siper zinciri çizgileri (3–6 m yeşil, diğerleri sarı)
- dar geçit işaretleri (kırmızı)
- oyuncu girişinden düşman girişlerine görüş hatları (engelliyse yeşil, açıksa sarı)

Son tarama sonucunu kullanır; çizim sırasında tarama yapmaz.

## Hata durumları

- Sahnede hiç rol yok: pencere "Önce parçalara rol ver" der ve rol atama alanını öne çıkarır.
- Play modu: rol atama ve yardımcı düğmeler kapalı, denetim çalışır.
- URP Lit shader bulunamazsa: malzemeler etkin render pipeline'ın varsayılan shader'ıyla oluşturulur ve uyarı gösterilir.
- Ders sahnesi zaten varsa: üzerine yazılmaz, açılır.
- Kaydedilmemiş değişiklik varken sahne değişecekse: Unity'nin kaydet sorusu gösterilir (`SaveCurrentModifiedScenesIfUserWantsTo`).

## Test

- **EditMode testleri** (`LevelDesign.Tests`): kural 1–8 ve 11 için yapay `LDPiece` listeleriyle sınır değerler test edilir. Örnekler: 0,89 / 0,9 / 1,2 / 1,21 m siper; 1,19 / 1,2 / 1,5 m kapı; 0,04 / 1,19 / 1,2 m boşluk; 6,01 m siper mesafesi; zemine gömülü siperin yüksekliği.
- **Ders 1 uyumu:** sohbetteki koordinat tablosundan kurulan parça listesi kural 1–8'den Hata çıkarmamalı. Beklenen bulgular test olarak sabitlenir.
- **Elle kontrol (NOT RUN olarak raporlanır):** kural 9–10, pencere ve katmanın görünüşü, rol atama, Undo, ilerlemenin yeniden açılışta korunması.
- **Çalıştırma:** Unity kapalıysa testler komut satırından batch modda çalıştırılır. Açıksa kullanıcı Window → General → Test Runner'dan çalıştırır.
