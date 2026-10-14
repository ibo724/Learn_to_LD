# Villa Operasyonu — Hazırlanan sürüm

14 Ekim 2026. GDD kapsamındaki ilk FPS sürümü, mevcut villanın ayrı bir sahne kopyasında hazırlandı. Bu belge, kod ve derleme durumunu manuel oyun kontrolünden ayırır.

## Açılacak dosyalar

- [Windows uygulaması](/C:/Projects/Learn_to_LD/Builds/Villa_Operation/Villa_Operation.exe)
- [Unity oyun sahnesi](/C:/Projects/Learn_to_LD/Assets/_LevelProject/FPS/Scenes/Villa_Operation.unity)
- [Yarınki sıralı kontrol listesi](/C:/Projects/Learn_to_LD/Documents/Villa_FPS_Yarin_Kontrol_Listesi.md)
- [Devralma ve günlük dosyaları](/C:/Projects/Learn_to_LD/Documents/Villa_FPS_Devralma_Notlari.md)
- [Sanat rehberi](/C:/Projects/Learn_to_LD/Documents/Villa_FPS_Sanat_Rehberi.md)

Windows uygulaması 1280×720 boyutunda, yeniden boyutlandırılabilir pencerede başlar. Uygulamanın yanındaki Data klasörü ve DLL dosyaları aynı klasörde kalmalıdır. Paket yaklaşık 164 MiB'dir; yalnız exe'yi taşımak yeterli değildir.

## Hazırlanan içerik

| Bölüm | Hazırlanan davranış | Manuel durum |
|---|---|---|
| Oyuncu | Yürüme, koşma, zıplama, fare bakışı; 100 can | Bekliyor |
| Karabina | 30+90 başlangıç mühimmatı, nişan, 25 hasar, otomatik ateş, iki saniyelik doldurma | Bekliyor |
| Muhafızlar | Yedi başlangıç muhafızı ve veri alındığında etkinleşen bir yedek; devriye, görüş, arama, ateş, ölüm | Bekliyor |
| Çatışma | Duvar/cam engellemesi, namlu önü kontrolü, en fazla iki aktif saldırgan, isabet/hasar geri bildirimi | Bekliyor |
| Destek paketleri | İki can paketi, iki mühimmat paketi; doluyken tüketilmeme | Bekliyor |
| Görev | Üst kattaki veriyi E ile alma, bahçedeki çıkışa dönme, başarı ve başarısızlık | Bekliyor |
| Kapılar | Menteşeli kapılar ve yana kayan camlı geçiş; yakından E etkileşimi | Bekliyor |
| Arayüz | Görev özeti, HUD, duraklatma, sonuç, yeniden deneme, başlangıca dönüş; fare ve klavye menü girişi | Bekliyor |
| Ayarlar | Fare hassasiyeti, ana ses, kamera tepkisi; uygulama yeniden açıldığında tercihlerin yüklenmesi | Bekliyor |
| Görünüm | Mat krem/petrol/bakır paleti, özel URP shader'ları, geometrik muhafız ve karabina, sade yürüme hareketi | Bekliyor |
| Çevre | Bahçe sınırları, siperler, ağaçlar, banklar, iç eşya, sığ havuz ve güvenlik korkuluğu | Bekliyor |
| Ses | Ateş, doldurma, adım, hasar, alarm, paket, veri, isabet ve görev sonucu sesleri | Bekliyor |

## Yapılan teknik doğrulama

- Son Windows derlemesi: **Succeeded, 0 hata, 1 uyarı**.
- Kalan uyarı: Unity Cloud oturum belirteci olmadığı için native hata ayıklama sembolleri buluta yüklenmedi. Yerel Windows paketi oluşturuldu.
- Özel shader derleyici hata listesi boş.
- Hazırlanan NavMesh 583 üçgen içeriyor; sekiz muhafızın yerleştirildiği konumlarda navigasyon yüzeyi bulunuyor. Bu, gerçek devriye veya görev rotası oynanarak doğrulandı anlamına gelmez.
- Kaynak villa sahnesinin hazırlama öncesi ve sonrası SHA-256 değeri aynı: `03D6774646FCF7A47ECBA8275B75FF2AE8017CB7FB7DAE14A239E90DC6032BAD`.
- Derlemenin değiştirdiği kaynak grafik/kalite ve genel proje ayarları geri alındı. FPS'ye ait Forward pipeline ve materyaller ayrı varlıklarda tutuluyor.

[Derleme özeti](/C:/Projects/Learn_to_LD/Logs/VillaFPS/windows-build.txt), [uyarı ayrıntısı](/C:/Projects/Learn_to_LD/Logs/VillaFPS/build-messages.txt) ve [sahne hazırlama kaydı](/C:/Projects/Learn_to_LD/Logs/VillaFPS/preparation.json) projede bırakıldı.

## Henüz yapılmayanlar

**Oyun başlatılmadı, Unity Play kullanılmadı, Computer Use uygulanmadı.** Oynanış, görüntü, ses dengesi, menü tıklamaları, performans ve baştan sona görev kontrolü kullanıcı isteğiyle ertelendi. Yukarıdaki derleme sonucu bu kontrollerin yerine geçmez.

Ayakların merdivene oturuşu için IK yok; sade parçalı animasyon kullanılıyor. Havuzda yüzme yok. Görev ilerlemesi kaydedilmiyor; yalnız kullanıcı ayarları saklanıyor. Bu sürüm tek haritalı ve tek oyunculu prototip kapsamındadır.

FPS sahnesini yeniden üretmek, o kopyaya sonradan elle eklenmiş düzenlemeleri değiştirebilir. `Prepare game scene` önce mevcut kopyayı `Logs/VillaFPS/SceneBackups` altında yedekler. İlk manuel deneme için yeniden üretim veya yeniden build gerekmez.
