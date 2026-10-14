# Villa Operasyonu — Hazırlanan sürüm

14 Ekim 2026. **0.2.1 prototipi**, mevcut villanın ayrı bir sahne kopyasında hazırlandı. Bu belge, kod ve derleme durumunu manuel oyun kontrolünden ayırır.

## Açılacak dosyalar

- [Windows uygulaması](/C:/Projects/Learn_to_LD/Builds/Villa_Operation/Villa_Operation.exe)
- [Unity oyun sahnesi](/C:/Projects/Learn_to_LD/Assets/_LevelProject/FPS/Scenes/Villa_Operation.unity)
- [Yarınki sıralı kontrol listesi](/C:/Projects/Learn_to_LD/Documents/Villa_FPS_Yarin_Kontrol_Listesi.md)
- [Devralma ve günlük dosyaları](/C:/Projects/Learn_to_LD/Documents/Villa_FPS_Devralma_Notlari.md)
- [Sanat rehberi](/C:/Projects/Learn_to_LD/Documents/Villa_FPS_Sanat_Rehberi.md)
- [Türkçe dengeleme ve sahne düzenleme rehberi](/C:/Projects/Learn_to_LD/Documents/Villa_FPS_Dengeleme_Rehberi.md)

Windows uygulaması 1280×720 boyutunda, yeniden boyutlandırılabilir pencerede başlar. Uygulamanın yanındaki Data klasörü ve DLL dosyaları aynı klasörde kalmalıdır. Paket yaklaşık 165 MiB'dir; yalnız exe'yi taşımak yeterli değildir.

## Hazırlanan içerik

| Bölüm | Hazırlanan davranış | Manuel durum |
|---|---|---|
| Oyuncu | Yürüme, koşma, zıplama, fare bakışı; 100 can | Bekliyor |
| Karabina | 30+90 başlangıç mühimmatı, nişan, 25 hasar, otomatik ateş, iki saniyelik doldurma; hareketten önce aynı karenin nişan/koşu/doldurma kararının işlenmesi | Bekliyor |
| Muhafızlar | Yedi başlangıç muhafızı ve veri alındığında etkinleşen bir yedek; devriye, görüş, arama, ateş, ölüm | Bekliyor |
| Çatışma | Duvar/cam engellemesi, namlu önü kontrolü, en fazla iki aktif saldırgan, isabet/hasar geri bildirimi | Bekliyor |
| Destek paketleri | İki can paketi, iki mühimmat paketi; doluyken tüketilmeme | Bekliyor |
| Görev | Üst kattaki veriyi E ile alma, bahçedeki çıkışa dönme, başarı ve başarısızlık | Bekliyor |
| Kapılar | Menteşeli kapılar ve yana kayan camlı geçiş; yakından E etkileşimi | Bekliyor |
| Arayüz | Görev özeti, HUD, duraklatma, doğrudan yeniden başlatma, sonuç, başlangıca dönüş; fare ve klavye menü girişi | Bekliyor |
| Ayarlar | Fare hassasiyeti, ana ses, kamera tepkisi, dikey görüş açısı ve nişangâh boyutu; kalıcılık ve varsayılanlara dönüş | Bekliyor |
| Görünüm | Mat krem/petrol/bakır paleti, özel URP shader'ları, geometrik muhafız ve karabina, sade yürüme hareketi, pahlanmış eşya kenarları | Bekliyor |
| Silah geri bildirimi | Namlu parlaması, hareketli şarjör, kısa atış çizgisi, sınırlı havuzdan üretilen ve kaybolan duvar/cam izleri | Bekliyor |
| Yönlendirme | Veri alındıktan sonra ekran dışındaki çıkışı gösteren ok; okunurluk için koyu kenarlı nişangâh | Bekliyor |
| Çevre | Bahçe sınırları, siperler, ağaçlar, banklar, mutfak ayrıntıları, kitaplık, geometrik duvar panosu, sığ havuz ve korkuluk | Bekliyor |
| Ses | Ateş, doldurma başlangıç/bitiş, yüzeye göre adım, kapı, hasar, alarm, paket, veri, isabet ve görev sonucu; rüzgâr/su döngüleri | Bekliyor |

## Düzenleme araçları

- `Villa Balance.asset` üzerinden oyuncu, silah, muhafız ve destek paketi değerleri tek profilden değiştirilebilir. Inspector alanları Türkçe etiketlidir; mevcut profil, sahne yeniden hazırlanırken korunur.
- Görev Inspector'ından başlangıç, veri ve çıkış nesnelerine odaklanılabilir. Muhafız görüş kılavuzları tasarım yardımcısıdır; duvar engelini hesaplamaz.
- Muhafız devriye noktaları Scene görünümünde taşınabilir, iki kısa nokta oluşturulabilir ve noktalar yakın NavMesh'e oturtulabilir. Bunlar editör araçlarıdır; rotanın oynanabilirliği ayrıca kontrol edilmelidir.
- `Tools/Build-Villa.ps1` kaydedilmiş sahneyi Windows için paketler. `-Prepare` seçeneği FPS kopyasını yeniden üretir. Yeni paket önce ayrı klasöre derlenir; başarıda önceki paket `Builds/History` altında korunur.

## Yapılan teknik doğrulama

- Son Windows derlemesi: **Succeeded, 0 hata, 1 uyarı**.
- Kalan uyarı: Unity Cloud oturum belirteci olmadığı için native hata ayıklama sembolleri buluta yüklenmedi. Yerel Windows paketi oluşturuldu.
- Özel shader derleyici hata listesi boş.
- Hazırlanan muhafız prefabında Renderer sayısı **52 → 12**, silahta **33 → 5** oldu; birlikte hareket eden parçalar materyale göre birleştirildi. Şarjör ve bacak hareket kökleri ayrı tutuldu. Bu sayılar ölçülmüş FPS kazancı değildir.
- Hazırlanan NavMesh 583 üçgen içeriyor; sekiz muhafızın yerleştirildiği konumlarda navigasyon yüzeyi bulunuyor. Bu, gerçek devriye veya görev rotası oynanarak doğrulandı anlamına gelmez.
- Kaynak villa sahnesinin hazırlama öncesi ve sonrası SHA-256 değeri aynı: `03D6774646FCF7A47ECBA8275B75FF2AE8017CB7FB7DAE14A239E90DC6032BAD`.
- Derlemenin değiştirdiği kaynak grafik/kalite ve genel proje ayarları geri alındı. FPS'ye ait Forward pipeline ve materyaller ayrı varlıklarda tutuluyor.
- Son kod incelemesi statik olarak yapıldı. Derleme kaydı `BUILD_INFO.json`, paket sürümünü, sahne ve kullanılan denge profili kimliklerini, sonucu ve `manualVerification: NOT RUN` durumunu içerir.

[Derleme özeti](/C:/Projects/Learn_to_LD/Logs/VillaFPS/windows-build.txt), [uyarı ayrıntısı](/C:/Projects/Learn_to_LD/Logs/VillaFPS/build-messages.txt), [paket kimliği](/C:/Projects/Learn_to_LD/Builds/Villa_Operation/BUILD_INFO.json) ve [sahne hazırlama kaydı](/C:/Projects/Learn_to_LD/Logs/VillaFPS/preparation.json) projede bırakıldı.

## Henüz yapılmayanlar

**Oyun başlatılmadı, Unity Play kullanılmadı, Computer Use uygulanmadı.** Oynanış, görüntü, ses dengesi, menü tıklamaları, performans ve baştan sona görev kontrolü kullanıcı isteğiyle ertelendi. Yukarıdaki derleme sonucu bu kontrollerin yerine geçmez.

Ayakların merdivene oturuşu için IK yok; sade parçalı animasyon kullanılıyor. Havuzda yüzme yok. Görev ilerlemesi kaydedilmiyor; yalnız kullanıcı ayarları saklanıyor. Bu sürüm tek haritalı ve tek oyunculu prototip kapsamındadır.

Oyun için 120, menüler için 60 kare/sn hedef sınırı ayarlı. Bunlar donanımda doğrulanmış performans veya garanti edilen kare hızı değildir. Çatışma zorluğu, silah hissi ve ses dengesi manuel turdan sonra ayarlanmalıdır.

FPS sahnesini yeniden üretmek, o kopyaya sonradan elle eklenmiş düzenlemeleri değiştirebilir. `Prepare game scene` önce mevcut kopyayı `Logs/VillaFPS/SceneBackups` altında yedekler. İlk manuel deneme için yeniden üretim veya yeniden build gerekmez.
