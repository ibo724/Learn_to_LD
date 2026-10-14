# Villa Operasyonu — Devralma notları

## Nereden başlayacaksın?

- Windows paketi: `C:\Projects\Learn_to_LD\Builds\Villa_Operation\Villa_Operation.exe`
- Unity sahnesi: `Assets/_LevelProject/FPS/Scenes/Villa_Operation.unity`
- Sıralı kontrol: `Documents/Villa_FPS_Yarin_Kontrol_Listesi.md`

Paket klasöründeki exe, `Villa_Operation_Data` ve Unity dosyaları birlikte kalmalı. İlk denemede Unity'den yeniden sahne üretmen veya build alman gerekmez.

Windows paketi 1280×720 pencereli ve yeniden boyutlandırılabilir biçimde hazırlanıyor. Bu build ayarları yalnız paket oluşturulurken uygulanır; kaynak projenin Player Settings değerleri sonrasında geri yüklenir.

## Bu sürümün kapsamı

Mevcut villadan ayrı bir FPS sahnesi hazırlandı. Görev, üst kattaki veri cihazını alıp bahçedeki çıkışa dönmek. Karabina, nişan alma, doldurma, hasar, muhafızlar, can/mühimmat paketleri, kapı etkileşimi, duraklatma, ayarlar ve sonuç ekranları bu akışa bağlandı.

Görsel yön krem, petrol mavisi, bakır ve adaçayı renklerinde stilize bir Akdeniz maketi. Bahçe, sığ havuz, sınırlar ve yönlendirme ayrıntıları FPS sahnesinde ele alındı. Kaynak `Assets/_LevelProject/Scenes 1/Industrial_Level_01.unity` sahnesi ayrı tutuldu.

Başlangıçta yedi muhafız etkin; veri alındığında bir yedek daha katılıyor. Sekizini de etkisizleştirmek zorunda değilsin. Can/mühimmat paketleri üzerinden geçerek, veri ve kapılar ise yakından bakıp E'ye basarak kullanılıyor.

Fare hassasiyeti, ses seviyesi ve kamera tepkisi tercihleri ayarlardan Geri/Esc ile çıkınca kaydedilir. Amaç hem görevi yeniden başlattığında hem uygulamayı tamamen kapatıp açtığında bu seçimleri korumak. **Bu kalıcılık kontrolü henüz yapılmadı (NOT RUN).** Görev ilerlemesi kaydedilmiyor; yeni açılış görev özetiyle başlıyor.

## FPS sahnesini yeniden oluşturmak

`Tools > Villa FPS > Prepare game scene` mevcut FPS kopyasını yeniden üretir. İlk oyun kontrolünde buna gerek yok. FPS sahnesine elle ekleme yaptıktan sonra bu komutu kullanırsan o düzenlemeler yeniden üretimde değişebilir.

Yeniden üretimden önce FPS sahnesinin önceki içeriği `Logs/VillaFPS/SceneBackups` altında yedeklenir; aynı içerik için tekrar yedek eklenmez. Yedek klasörü, elle yaptığın düzenlemeleri geri almak gerektiğinde başvurulacak kaynaktır. Orijinal `Industrial_Level_01.unity` sahnesi ayrı kalır ve düzenlenmez.

## Kontrol durumu

**NOT RUN — manuel oyun kontrolü yapılmadı.** Oyun veya Play başlatılarak değerlendirme yapılmadı; menü tıklamaları, fare/klavye, odak kaybı, çatışmanın hissi, ses seviyesi, sahne içinde yürünebilirlik ve baştan sona görev tamamlama senin denemeni bekliyor. Windows derleme çıktısı mevcut; bu, oyunun bütün akışlarının doğrulandığı anlamına gelmez.

İlk kontrol için listede 15–20 dakikalık rota var. Bir yerde sorun çıkarsa madde numarasını, yaptığın işlemi, odanın/nesnenin yerini ve varsa ilk Console hatasını kaydet. Windows paketine özel sorunlarda `Player.log` konumu da kontrol listesinde bulunuyor.

Windows paketinin günlüğü `C:\Users\halil\AppData\LocalLow\DefaultCompany\Villa Operasyonu\Player.log` konumundadır. Kaynak Unity projesi **Learn_to_LD** ürün adını korur; bu adla oluşturulan başka bir player'ın günlüğü `C:\Users\halil\AppData\LocalLow\DefaultCompany\Learn_to_LD\Player.log` yolunda olur. Unity Play sorunlarını Console veya `C:\Users\halil\AppData\Local\Unity\Editor\Editor.log` günlüğünden, teslim edilen Windows paketi sorunlarını **Villa Operasyonu** Player.log dosyasından takip et.

## Sonraki çalışma

Öncelik, senin kontrolünde çıkan hataları düzeltmek. Ardından muhafız zorluğu, ses dengesi, silah hissi ve görsel okunurluk için küçük ayarlar yapılabilir. Sığ havuz bu sürümde çevre ayrıntısı; yüzme sistemi kapsamda değil.
