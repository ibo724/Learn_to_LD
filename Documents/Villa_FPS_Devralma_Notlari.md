# Villa Operasyonu — Devralma notları

## Nereden başlayacaksın?

- Windows paketi: `C:\Projects\Learn_to_LD\Builds\Villa_Operation\Villa_Operation.exe`
- Unity sahnesi: `Assets/_LevelProject/FPS/Scenes/Villa_Operation.unity`
- Sıralı kontrol: `Documents/Villa_FPS_Yarin_Kontrol_Listesi.md`
- Dengeleme rehberi: `Documents/Villa_FPS_Dengeleme_Rehberi.md`

Paket klasöründeki exe, `Villa_Operation_Data` ve Unity dosyaları birlikte kalmalı. İlk denemede Unity'den yeniden sahne üretmen veya build alman gerekmez.

Unity'de FPS sürümü için **Tools > Villa FPS > Open game scene** ile `Assets/_LevelProject/FPS/Scenes/Villa_Operation.unity` sahnesini aç. Kaynak `Industrial_Level_01.unity`, önceki level çalışman olarak ayrı tutulur. Alt kat koridoruna girişten okunması amaçlanan **ARŞİV / ÜST KAT** askılı levhası eklendi; levhanın görünürlüğü, yazı boyutu ve mimariye oturuşu için manuel görsel kontrol **NOT RUN**.

Windows paketi 1280×720 pencereli ve yeniden boyutlandırılabilir biçimde hazırlanıyor. Bu build ayarları yalnız paket oluşturulurken uygulanır; kaynak projenin Player Settings değerleri sonrasında geri yüklenir.

## Bu sürümün kapsamı

Mevcut villadan ayrı bir FPS sahnesi hazırlandı. Görev, üst kattaki veri cihazını alıp bahçedeki çıkışa dönmek. Karabina, nişan alma, doldurma, hasar, muhafızlar, can/mühimmat paketleri, kapı etkileşimi, duraklatma, ayarlar ve sonuç ekranları bu akışa bağlandı.

Görsel yön krem, petrol mavisi, bakır ve adaçayı renklerinde stilize bir Akdeniz maketi. Bahçe, sığ havuz, sınırlar ve yönlendirme ayrıntıları FPS sahnesinde ele alındı. Kaynak `Assets/_LevelProject/Scenes 1/Industrial_Level_01.unity` sahnesi ayrı tutuldu.

Başlangıçta yedi muhafız etkin; veri alındığında bir yedek daha katılıyor. Sekizini de etkisizleştirmek zorunda değilsin. Can/mühimmat paketleri üzerinden geçerek, veri ve kapılar ise yakından bakıp E'ye basarak kullanılıyor.

Fare hassasiyeti, ses seviyesi, kamera tepkisi, dikey FOV (`60–90°`) ve nişangâh boyutu (`%100–%175`) ayarlardan Geri/Esc ile çıkınca kaydedilir. Varsayılanlar düğmesi başlangıç tercihlerine döndürür. Amaç hem görevi yeniden başlattığında hem uygulamayı tamamen kapatıp açtığında seçimlerini korumak. **Bu kalıcılık kontrolü henüz yapılmadı (NOT RUN).** Görev ilerlemesi kaydedilmiyor; yeni açılış görev özetiyle başlıyor.

Duraklatma menüsünde **Devam Et**, **Yeniden Başlat**, **Ayarlar**, **Ana Menü** ve **Oyundan Çık** bulunur. **Yeniden Başlat** mevcut görevi başlangıç durumuyla doğrudan açar; **Ana Menü** görev özetine döner. Ayarlardan geri dönüş ilgili Ayarlar seçimini korur. Bu menü eylemlerinin, sıfırlamanın ve tercihleri korumanın manuel kontrolü **NOT RUN**.

Nişangâhta koyu kontrast kenarı, veri alındıktan sonra ekran dışındaki çıkışa küçük yön oku, kısa namlu parlaması, hareketli şarjör ve yüzey isabet izleri eklendi. Cam üzerindeki iz görsel geri bildirimdir; cam kırılmadan mermiyi durdurur. Yatay yönü olmayan hasar için nötr gösterge kullanılır. Bu görsel davranışların tamamı manuel kontrol bekliyor.

Ortam sesi düşük seviyeli bahçe rüzgârı ve havuza yakın üç boyutlu su döngüsünden oluşur. İçeride rüzgâr azaltılır; görev dışındaki ekranlarda döngüler duraklar. Kapı etkileşiminde kısa mekanizma sesi, doldurma tamamlanınca ayrı hazır klik sesi vardır. Adımlar gerçek hareket hızına bağlanır; duvara dayanıp durunca kesilmeleri, çim/toprak ile sert zeminde farklı tınlamaları amaçlanır. Sesler prosedürel üretildi; dinleme, yüzey seçimi, seviye dengesi ve döngü sınırı kontrolü **NOT RUN**.

Oyun sırasında hedef kare sınırı 120, menülerde 60 kare/sn olarak ayarlı. Bu değerler ölçülmüş FPS sonucu değildir; performans ve takılma kontrolü henüz yapılmadı.

## FPS sahnesini yeniden oluşturmak

`Tools > Villa FPS > Prepare game scene` mevcut FPS kopyasını yeniden üretir. İlk oyun kontrolünde buna gerek yok. FPS sahnesine elle ekleme yaptıktan sonra bu komutu kullanırsan o düzenlemeler yeniden üretimde değişebilir.

Yeniden üretimden önce FPS sahnesinin önceki içeriği `Logs/VillaFPS/SceneBackups` altında yedeklenir; aynı içerik için tekrar yedek eklenmez. Yedek klasörü, elle yaptığın düzenlemeleri geri almak gerektiğinde başvurulacak kaynaktır. Orijinal `Industrial_Level_01.unity` sahnesi ayrı kalır ve düzenlenmez.

`Assets/_LevelProject/FPS/Settings/Villa Balance.asset` mevcutsa Prepare bu denge profilindeki tercihlerini korur. Gameplay sayılarını değiştirmek için sahneyi yeniden üretmek gerekmez; rehberdeki profili düzenleyip yeni bir manuel deneme yapabilirsin.

Denge Inspector'ındaki alanlar Türkçe etiketlenmiştir. Bir muhafızı seçtiğinde Scene görünümünde devriye noktalarını sürükleyebilir, Inspector'dan iki kısa nokta oluşturabilir veya noktaları yakındaki NavMesh'e oturtabilirsin. Bunlar Play kapalıyken kullanılan tasarım araçlarıdır; değişiklikten sonra sahneyi kaydet ve rotayı kendin dene. Adımlar ve sınırlar dengeleme rehberindedir.

## Yeni Windows paketi almak

Unity kapalıyken proje kökünden PowerShell'de:

```powershell
# Kaydedilmiş mevcut FPS sahnesini paketle:
powershell -NoProfile -File .\Tools\Build-Villa.ps1

# FPS sahnesini kaynaktan yeniden üret, ardından paketle:
powershell -NoProfile -File .\Tools\Build-Villa.ps1 -Prepare
```

`-Prepare` olmadan mevcut FPS sahnesi kullanılır; elle yaptığın sahne düzenlemelerini önce kaydet. `-Prepare` FPS kopyasını yeniden oluşturur, önceki sahneyi yedekler ve mevcut denge profilini korur. Script, ilgili proje genelindeki dosyaları işlem sonunda geri yükler; Unity zaten açıksa onu kapatmaz, işlem başlamadan durur. Bu komutlar Play veya oyunu başlatmaz.

Yeni paket önce ayrı bir staging klasöründe derlenir. Derleme başarıyla tamamlanınca `Builds/Villa_Operation` güncellenir; önceki paket tarihli bir klasörle `Builds/History` altında tutulur. Paket içindeki `BUILD_INFO.json`, sürüm `0.2.0`, tarih, sahne/denge kimlikleri ve derleme sonucunu kaydeder; `manualVerification: NOT RUN` oyun kontrolünün ayrıca beklediğini belirtir. Başarılı derleme, mekaniklerin veya seslerin çalıştığına dair manuel test sonucu değildir.

## Kontrol durumu

**NOT RUN — manuel oyun kontrolü yapılmadı.** Oyun veya Play başlatılarak değerlendirme yapılmadı; menü tıklamaları, fare/klavye, odak kaybı, çatışmanın hissi, ses seviyesi, sahne içinde yürünebilirlik ve baştan sona görev tamamlama senin denemeni bekliyor. Windows derleme çıktısı mevcut; bu, oyunun bütün akışlarının doğrulandığı anlamına gelmez.

İlk kontrol için listede 15–20 dakikalık rota var. Bir yerde sorun çıkarsa madde numarasını, yaptığın işlemi, odanın/nesnenin yerini ve varsa ilk Console hatasını kaydet. Windows paketine özel sorunlarda `Player.log` konumu da kontrol listesinde bulunuyor.

Windows paketinin günlüğü `C:\Users\halil\AppData\LocalLow\DefaultCompany\Villa Operasyonu\Player.log` konumundadır. Kaynak Unity projesi **Learn_to_LD** ürün adını korur; bu adla oluşturulan başka bir player'ın günlüğü `C:\Users\halil\AppData\LocalLow\DefaultCompany\Learn_to_LD\Player.log` yolunda olur. Unity Play sorunlarını Console veya `C:\Users\halil\AppData\Local\Unity\Editor\Editor.log` günlüğünden, teslim edilen Windows paketi sorunlarını **Villa Operasyonu** Player.log dosyasından takip et.

## Sonraki çalışma

Öncelik, senin kontrolünde çıkan hataları düzeltmek. Ardından muhafız zorluğu, ses dengesi, silah hissi ve görsel okunurluk için küçük ayarlar yapılabilir. Sığ havuz bu sürümde çevre ayrıntısı; yüzme sistemi kapsamda değil.
