# Villa Operasyonu — Dengeleme rehberi

**Durum: NOT RUN.** Bu rehber kodda tanımlı ayarları açıklar; aşağıdaki örnekler oyunda denenmiş veya başarılı bulunmuş ayarlar değildir. İlk turda mevcut profille manuel kontrol yap, ardından küçük değişikliklere geç.

## Profili aç

Unity'de Play'i durdur. **Tools > Villa FPS > Open balance settings** menüsünü kullan veya Project panelinde `Assets/_LevelProject/FPS/Settings/Villa Balance.asset` dosyasını seç.

FPS sahnesindeki **Villa Mission** nesnesinin Inspector'ında **Denge ayarlarını seç** düğmesi de aynı profili açar. `Prepare game scene`, mevcut profili bulduğunda içindeki değerleri korur. Sadece denge değiştirmek için sahneyi yeniden üretmen gerekmez. Profil Inspector'ındaki alanlar aşağıdaki Türkçe etiketlerle gösterilir.

Aşağıdaki tablolar `VillaBalance.cs` dosyasındaki başlangıç değerleridir. Profil daha önce düzenlendiyse Inspector'da farklı sayılar görmen normaldir.

## Başlangıç değerleri ve etkileri

| Oyuncu ayarı | Varsayılan | Etkisi |
|---|---:|---|
| Oyuncu canı | 100 | Başlangıç ve azami can |
| Yürüme hızı (m/s) | 3,5 | Normal hareket hızı |
| Koşma hızı (m/s) | 6 | Koşu hızı |
| Zıplama yüksekliği (m) | 0,9 | Zıplama yüksekliği |
| Yerçekimi (m/s²) | −22 | Düşüş ivmesi; mutlak değeri büyüdükçe düşüş hızlanır |

| Karabina ayarı | Varsayılan | Etkisi |
|---|---:|---|
| Şarjör kapasitesi | 30 | Şarjör kapasitesi |
| Başlangıç yedek mühimmatı | 90 | Başlangıçtaki yedek mühimmat |
| Yedek mühimmat sınırı | 150 | Yedek mühimmat sınırı |
| Mermi başına hasar | 25 | Başarılı bir isabetin hasarı |
| Saniyedeki atış | 6 | Tetik basılıyken saniyedeki atış sayısı |
| Doldurma süresi (s) | 2 | Şarjör doldurma süresi |
| Mermi menzili (m) | 50 | Mermi sorgusunun azami mesafesi |
| Normal geri tepme (°) | 0,8 | Normal atışın bakış tepkisi |
| Nişan alırken geri tepme (°) | 0,45 | Nişanlı atışın bakış tepkisi |

| Muhafız ayarı | Varsayılan | Etkisi |
|---|---:|---|
| Muhafız canı | 100 | Muhafız canı |
| Muhafız hızı (m/s) | 2,2 | Hareket hızı |
| Görüş mesafesi (m) | 12 | Görüş için uzaklık sınırı |
| Ateş mesafesi (m) | 7 | Ateş edebileceği mesafe sınırı |
| Görüş açısı (°) | 100 | Bakış yönü çevresindeki görüş açısı |
| İlk tepki süresi (s) | 0,7 | Tepki süresi; artırmak oyuncuya daha fazla zaman verir |
| Arama süresi (s) | 4 | Son bilinen konumdaki arama süresi |
| Atış aralığı (s) | 1 | Atışlar arasındaki süre; artırmak atışları yavaşlatır |
| Muhafız mermi hasarı | 10 | Muhafızın başarılı isabet hasarı |

| Destek paketi ayarı | Varsayılan | Etkisi |
|---|---:|---|
| Can paketi miktarı | 35 | Bir can paketinin sağlayabileceği can |
| Mühimmat paketi miktarı | 30 | Bir mühimmat paketinin sağlayabileceği yedek mermi |

Can ve mühimmat kazancı üst sınırlarda kesilir. Kod ayrıca koşu hızını yürüme hızının altına, saldırı mesafesini görüş mesafesinin üstüne ve başlangıç yedeğini azami yedeğin üstüne çıkarmamaya çalışır. Bu yüzden ilişkili bir değeri değiştirdiğinde Inspector'daki diğer sayılara da bak.

## Tek değişiklikle dene

1. **Play kapalıyken** yalnız bir değeri değiştir; önceki değeri not al ve projeyi kaydet.
2. FPS sahnesini yeniden başlatıp aynı kısa rotayı ve benzer koşulları kullan.
3. Farkın ne olduğunu yaz: daha çok zaman mı kaldı, daha az mermi mi harcandı, hareket mi hızlandı?
4. Sonuç istediğin yönde değilse önceki sayıya dön. İkinci bir ayarı değiştirmeden önce ilk değişikliği değerlendir.

Örnek başlangıç denemeleri; hepsi **NOT RUN**:

- Muhafız çok çabuk ateş ediyorsa yalnız **İlk tepki süresi: 0,7 → 0,9** dene.
- Muhafız fazla mermi istiyorsa yalnız **Muhafız canı: 100 → 75** dene. Hasar 25 kaldığında gerekli başarılı isabet sayısı dört yerine üç olur.
- Doldurma fazla kısa geliyorsa yalnız **Doldurma süresi: 2 → 2,4** dene.

Bir profil değişikliği, daha önce oluşturulmuş exe'yi değiştirmez. Unity denemesinden sonra Windows paketinde görmek için yeni build gerekir. Yeniden üretmeden paketlemek için, Unity kapalıyken proje kökünde `powershell -NoProfile -File .\Tools\Build-Villa.ps1` kullanabilirsin. `-Prepare` eklemek FPS sahnesini yeniden üretir; bu amaçla gerekli değildir.

## Başlangıç değerlerine dönmek

Profil Inspector'ındaki **GDD başlangıç değerlerine dön (Undo ile geri alınabilir)** düğmesi bu tablodaki denge değerlerine döner. Gerekirse Unity Undo ile geri alabilirsin.

Oyun menüsündeki **Varsayılanlar** düğmesi ise fare/ses/FOV/nişangâh/kamera tercihleri içindir; denge profilini sıfırlamaz. FOV ve nişangâh boyutu hasarı değiştirmez. Kamera tepkisini kapatmak bakışın geri tepme hareketini kaldırır.

## Sahne tasarımına yardımcı araçlar

FPS sahnesinde **Villa Mission** nesnesini seç. Inspector'daki **Başlangıç**, **Veri cihazı** ve **Çıkış** düğmeleri ilgili işaretçiyi seçip Scene görünümünde odaklar. Veri çevresindeki 2,5 m ve çıkış çevresindeki 1,8 m halkalar yerleşim rehberidir.

**Muhafız görüş yayları** seçeneği mesafe ve açıyı gösterir. Bu yaylar duvarları kesmez ve duvar/cam engelini hesaplamaz; gerçek görünürlüğün kanıtı değildir. Gerçek görüş ve atış engelleri oyun kontrolünde ayrıca denenmeli. Bu çizimler ve tasarım düğmeleri oyuncu HUD'sine eklenmez.

**Muhafız devriyesini düzenlemek:** Play kapalıyken sahnedeki bir `Guard` nesnesini seç. Scene görünümündeki **DEVRİYE 1/2** tutamaçlarını sürükleyebilirsin. Noktalar dünya koordinatındadır; muhafızı başka yere taşırsan noktalarını da gözden geçir. Inspector'daki **İki kısa devriye noktası oluştur** ve **Devriye noktalarını yakındaki NavMesh'e oturt** düğmeleri başlangıç düzeni için kullanılabilir. Oturtma yalnız yakındaki NavMesh'i arar; çok uzaktaki noktayı önce elle yaklaştır. Değişiklikleri kaydet, gerekirse Undo ile geri al. Bu tasarım araçlarının ve yeni rotanın manuel doğrulaması **NOT RUN**.

Devriye düzenlemeleri FPS sahnesinde tutulur. Kaynak sahneden yeniden `Prepare` yapmak bu el düzenlemelerini yeniden üretebilir; sahne yedeği alınsa da denge profilini korumakla devriye konumlarını korumak aynı işlem değildir.

## Ses ve akıcılık notu

Adımlar hareket isteğinden değil gerçek hızdan tetiklenir; duvara yaslanıp durduğunda susmaları beklenir. Sert zemin ile Terrain üzerindeki çim/toprak için farklı adım sesleri, doldurma sonunda ayrı hazır sesi vardır. Bu ses davranışları ve dengeleri **NOT RUN**.

Oyun için hedef sınır 120, menüler için 60 kare/sn. Bunlar performans ölçümü değildir; denge değişikliğini değerlendirirken takılma varsa ayrıca not al. Build sürümü ve `BUILD_INFO.json` kayıt biçimi bu eklerle **0.2.0** olarak kalır.

## Deneme kaydı

```text
Profil / build sürümü:
Değişen tek alan:
Önceki → yeni değer:
Denediğim kısa rota:
Gözlediğim fark:
Karar: koru / geri al / tekrar dene
Kontrol durumu: NOT RUN / kullanıcı tarafından denendi
```

Ses seviyesi, ortam döngüleri ve yeni görsel geri bildirimler için ayrıca `Villa_FPS_Yarin_Kontrol_Listesi.md` dosyasındaki kutuları kullan.
