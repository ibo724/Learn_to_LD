# Villa Operasyonu — Yarın yapılacak manuel kontrol

**Süre hedefi: 15–20 dakika.** Bu liste, hazırlanan FPS sürümünü Unity'de senin doğrulaman içindir. Kutular boş bırakılmıştır; burada yazan beklentiler test sonucu veya başarı iddiası değildir. Kontroller sırasında kaynak sahneyi düzenleme, değişiklikleri kaydetme.

**Manuel kontrol durumu: NOT RUN — henüz uygulanmadı.** Oyun başlatma, menü tıklamaları, klavye, hassasiyet/ses ayarları, odak değişimi ve aşağıdaki mekanikler senin kontrolünü bekliyor. Windows derlemesinin oluşturulmuş olması bu kontrollerin yapıldığı anlamına gelmez.

Liste 0.2.1 prototipinin kontrollerini içerir. Sayısal beklentiler varsayılan **Villa Balance** profiline göredir; profili değiştirirsen yeni değerleri kontrol notuna yaz. Eklenen FOV, nişangâh, efekt ve ortam sesi maddelerinin durumu da **NOT RUN**. İlk 15–20 dakikalık turda yetişmeyen kutuları boş bırakabilirsin.

## Başlatmak için iki seçenek

**Windows paketi:** `C:\Projects\Learn_to_LD\Builds\Villa_Operation\Villa_Operation.exe` dosyasını aç. Yanındaki `Villa_Operation_Data` klasörünü ve Unity dosyalarını birlikte tut; yalnız exe'yi başka yere taşıma. Kontrolü bu paketle yapıyorsan aşağıdaki Play başlat/durdur adımları yerine oyunu aç/kapat.

Paket 1280×720 pencereli ve pencere boyutu değiştirilebilir biçimde hazırlanıyor. Bu build seçimi kaynak projenin Player Settings ayarlarını değiştirmiyor. Boyut/oran kontrolünü Windows penceresini küçültüp büyüterek de yapabilirsin.

**Unity:** Projeyi Unity Hub'dan aç, import bitince `Assets/_LevelProject/FPS/Scenes/Villa_Operation.unity` sahnesine çift tıkla ve Play'e bas. Kod hatalarını Console'da görmek için bu seçenek daha kullanışlıdır. Yeni sahneyi oluşturma veya tekrar build alma işlemi ilk kontrol için gerekli değildir.

İlk turda bir seçeneği kullanman yeterli. Bir sorun yalnız Windows paketinde oluyorsa ayrıca bunu belirt.

**Sahneyi yeniden üretme hakkında:** `Tools > Villa FPS > Prepare game scene` FPS kopyasını yeniden oluşturur; ilk denemede bunu çalıştırma. Bu kopyada sonradan elle yaptığın düzenlemeler yeniden üretimde değişebilir. İşlemden önce önceki FPS sahnesinin yedeği `Logs/VillaFPS/SceneBackups` klasörüne alınır; aynı içerik için gereksiz tekrar yedek oluşturulmaz. Kaynak `Industrial_Level_01.unity` sahnesi bu işlemde düzenlenmez.

## 1. Sahneyi aç ve hazırlan — 0–2 dakika

- [ ] Unity'nin import ve script derlemesini tamamlamasını bekle.
- [ ] Project panelinden `Assets/_LevelProject/FPS/Scenes/Villa_Operation.unity` sahnesini aç. Eski villa sahnesinde Play başlatmadığından emin ol.
- [ ] Console'u aç. Kırmızı hata varsa ilk hatayı ve tam ayrıntısını kaydet; hataları temizleyip gizlemek yerine önce aşağıdaki rapor şablonunu doldur.
- [ ] Play'e bas. İlk ekranda **Villa Operasyonu**, görev özeti ve kontroller görünmeli. Menü açıkken karakter, muhafızlar ve görev süresi ilerlememeli.
- [ ] **Operasyonu Başlat** düğmesine bas. İmleç yakalanmalı; can `100`, şarjör/yedek mühimmat `30 / 090`, muhafız sayacı `0/8` ile başlamalı.

## 2. Hareket ve silah — 2–5 dakika

- [ ] **WASD** ile hareket et, fareyle bak, **Shift** ile koş, **Space** ile zıpla. Merdivenlerde ve kapı eşiklerinde takılma veya zeminden düşme var mı?
- [ ] **NOT RUN:** Bir duvara doğru yürüyüp gerçekten durduğunda adım sesi kesilmeli; yalnız W basılı diye devam etmemeli. Çim/toprakta yumuşak, sert iç zeminde daha tok adımlar beklenir. Hareket eden muhafızların adımlarında da yüzey farkını dinle.
- [ ] Silah modeli ekranda görünmeli; duvara yaklaşınca görüntü tamamen kaybolmamalı veya ekranı kaplamamalı.
- [ ] Sol tıkla beş atış yap: şarjör `25`, yedek `90` olmalı. Tetik basılı tutulunca ateş devam etmeli.
- [ ] Sağ tıkla nişan al: görüş yakınlaşmalı, nişangâh sadeleşmeli. Bırakınca normal görüşe dönmeli.
- [ ] **NOT RUN — 0.2.1:** Yürürken sağ tıkla nişana gir/çık, nişandayken Shift ile koşuya geç ve R ile doldur. Hareket hızı o karenin nişan/koşu/doldurma durumuna göre değişmeli; eski nişan durumundan kalan bir karelik hız kullanılmamalı. Dolu şarjörde R nişanı kesmemeli, doldurma tamamlanınca mühimmat ve bitiş sesi yalnız bir kez işlenmeli.
- [ ] **R** ile doldur: yaklaşık iki saniyede şarjör `30`, yedek `85` olmalı. Doldurma sırasında ateş etmemeli.
- [ ] **NOT RUN:** Doldururken silahın şarjör parçası hareket edip yerine dönmeli; namlu parlaması yalnız atış anında kısa görünmeli. Duraklatma/sonuç ekranında parıltı takılı kalmamalı.
- [ ] **NOT RUN:** Doldurma gerçekten tamamlanıp mühimmat şarjöre aktarıldığında kısa bir hazır klik sesi duyulmalı. Baştaki mekanizma sesiyle bu bitiş sesini ayır; duraklatılmış doldurma kendi kendine bitiş sesi üretmemeli.
- [ ] Koşarken ateş ve nişan alma engellenmeli. Duvara çok yakın ateş edildiğinde duvarın arkasındaki muhafız hasar almamalı.
- [ ] Şarjörü boşalt: boş tetik sesi gelmeli, mermi sayısı eksiye düşmemeli. Yeniden doldurduğunda yedekten yalnız eksik miktar alınmalı.

## 3. Duraklatma ve ayarlar — 5–7 dakika

- [ ] **Esc** ile duraklat. Fare serbest kalmalı; muhafız hareketi, ateş ve görev süresi durmalı.
- [ ] **NOT RUN:** Duraklatma menüsünde **Devam Et**, **Yeniden Başlat**, **Ayarlar**, **Ana Menü** ve **Oyundan Çık** görünmeli. Fare ve klavye gezintisi beş seçeneği doğru eyleme bağlamalı; Ayarlar'dan Geri/Esc ile dönünce seçim yeniden Ayarlar üzerinde olmalı.
- [ ] **Ayarlar** içinde fare hassasiyetini değiştir, sesi azalt ve kamera tepkisini kapat. Fareyle düğmelerin tamamı çalışmalı.
- [ ] **NOT RUN:** **Görüş açısı (dikey)** değerini `60°` ve `90°` arasında değiştir. Oyuna dönünce görüşün genişliği değişmeli; sağ tıkla nişan alıp bırakınca seçtiğin açıya geri gelmeli.
- [ ] **NOT RUN:** **Nişangâh boyutu** değerini `%100–%175` arasında değiştir. Merkez işareti ve isabet X'i büyümeli; açık renkli duvarda koyu kenar okunaklı kalmalı. Bu ayar atış hasarını değiştirmemeli.
- [ ] **NOT RUN:** **Varsayılanlar** düğmesini dene: ses `%70`, FOV `70°`, nişangâh `%100`, kamera tepkisi **Açık** olmalı. Fare hassasiyeti başlangıç `0,1` değerine döner; mevcut yüzde ölçeğinde yaklaşık `26/100` görünür. Sonra kalıcılık kontrolü için tekrar kendi tercihlerini seç.
- [ ] Ayarlardayken bir kez **Esc**: ayarlar kapanmalı, oyun hâlâ duraklatılmış kalmalı. İkinci Esc veya **Devam Et** ile oyun sürmeli.
- [ ] Oyuna dönünce hassasiyet ve ses değişimini kontrol et. Kamera tepkisi kapalıyken ateşin bakışı itmesi kalkmalı; hasar/isabet göstergeleri kalmalı.
- [ ] İstersen menüde **yukarı/aşağı**, **Tab**, **Enter**; ayarlarda **sol/sağ** tuşlarını dene. Bir tuşa basınca iki farklı işlem tetiklenmemeli.
- [ ] Slider'ın farklı noktalarına tıkla, tutup sağa/sola sürükle; düğmeyi basılı tutup dışarıda bırakınca düğme çalışmamalı. Fare tıklamaları ve klavye menü seçimi aynı ayarı iki kez değiştirmemeli.
- [ ] Oyundayken başka pencereye geçip geri dön: oyun duraklatılmış olmalı, istemeden ateş etmemeli. Devam ettikten sonra bakış ve hareket geri gelmeli.
- [ ] Doldurmanın ortasında duraklat, birkaç saniye bekleyip devam et: kalan doldurma süresi oyuna dönünce işlemeli.

## 4. Muhafızlar, cam ve kapılar — 7–10 dakika

- [ ] Planlanan dağılım toplam **8 muhafız: başlangıçta 7 etkin, veri alınınca 1 yedek**. Sekizincisi başlangıçta çatışmaya katılmamalı. Gerekirse duraklatıp Hierarchy'den etkinlik durumlarını incele.
- [ ] Görüşe girince muhafız tepki vermeli; sağlam duvarın arkasındayken doğrudan seni görmemeli. Duvarın içinden ateş/hasar geliyor mu?
- [ ] Camın iki tarafında görünürlük korunmalı. Muhafız seni camın arkasından görebilmeli, fakat sağlam cam mermiyi durdurmalı. Pencere kasası ve duvar da mermiyi kesmeli.
- [ ] **NOT RUN:** Duvar ve cam isabetlerinde küçük yüzey izlerini kontrol et. Camdaki görsel iz kırılma mekaniği değildir; cam sağlam kalmalı ve sonraki mermileri de durdurmalı. İzler zamanla kaybolmalı.
- [ ] Kapıya yaklaş, ortasına bak ve **E** ile aç/kapat. Uzakta veya arada duvar varken etkileşim olmamalı; hedef mesafesi **2,5 metre**.
- [ ] Normal kapılar menteşeden dönmeli. Camlı geçişin kanatları yana kaymalı; kasa sabit kalmalı, parçalar kopup havada kalmamalı.
- [ ] **NOT RUN:** Kabul edilen kapı aç/kapat etkileşiminde kısa, düşük seviyeli mekanizma sesi duyulmalı. Uzakta E'ye basmak kapı sesi üretmemeli.
- [ ] Muhafıza isabet edince merkezde geri bildirim, sen hasar alınca can azalması ve saldırı yönü göstergesi görünmeli. Ölen muhafız ateşe devam etmemeli.
- [ ] **NOT RUN:** Yatay yön farkı olmayan yukarı/aşağı hasarla karşılaşırsan gösterge ön/arka yön uydurmadan nötr bir çerçeve göstermeli. Normal turda bu durum oluşmazsa bu kutuyu boş bırak; sonucu varsayma.

## 5. Can ve mühimmat paketleri — 10–12 dakika

- [ ] Haritada **2 can paketi** ve **2 mühimmat paketi** bulunmalı. Paketler yaklaşıp üzerinden geçince alınmalı; E gerekmemeli.
- [ ] Canın eksikken bir can paketi al: **en fazla +35**, toplam **en fazla 100** can. Canın doluyken paket harcanmamalı.
- [ ] Mühimmat paketi al: yedeğe **en fazla +30**, yedek toplamı **en fazla 150**. Artış bildirimi görünmeli ve alınan paket kaybolmalı.
- [ ] Aynı paketin üzerinde durunca tekrar tekrar kazanım oluşmamalı. Yedek doluyken mühimmat paketi harcanmamalı.

## 6. Veri cihazı ve görev sonu — 12–16 dakika

**Kısa rota:** Ön bahçe → ana giriş/koridor → villanın iç merdivenleri → üst kat → üzerinde **VERİ / E** işareti bulunan masa → aynı merdivenlerden bahçe → işaretli çıkış. Tüm muhafızları temizlemek görev koşulu değildir. Cihazı bulamazsan duraklatıp Scene görünümünde yaklaşık **X: 141,6 / Y: 4,84 / Z: −10,85** konumundaki `Data Device` nesnesine bakabilirsin; bu yardım kontrol listesine özeldir.

- [ ] Veriyi almadan bahçedeki çıkış alanına git: görev tamamlanmamalı.
- [ ] Üst kattaki veri cihazını bul. **2,5 metre içinde**, doğrudan cihaza bakınca **[E] Veri Cihazını Al** görünmeli.
- [ ] E'ye bas: cihaz kaybolmalı, veri alındı bildirimi gelmeli, görev **Çıkışa Ulaş** olarak değişmeli. Yedek muhafız bu aşamada etkinleşmeli.
- [ ] Bahçedeki işaretli çıkışa dön. Konumu bulmak gerekirse dünya koordinatları yaklaşık **X: 127,6 / Z: −16,4**. HUD'deki çıkış mesafesi yaklaşırken azalmalı.
- [ ] **NOT RUN:** Veri sende olduğunda çıkışı bakışın dışına al ve arkanı dön. Ekranın kenarında küçük **ÇIKIŞ** yön oku görünmeli; hedefe bakınca dünya üzerindeki işarete geçmeli. Veriyi almadan bu ok görünmemeli.
- [ ] Çıkışa yaklaşık **1,8 metre** yaklaşınca başarı ekranı görünmeli. Veri yanında olmadan bu ekran açılmamalı.
- [ ] Süre, muhafız sayısı ve isabet yüzdesini kontrol et. Sonuç ekranında oyun ve sayılar ilerlememeli.

## 7. Yeniden dene ve kaybetme — 16–18 dakika

- [ ] **NOT RUN:** Görev sürerken Esc → **Yeniden Başlat** seç. Başarı/başarısızlık ekranına uğramadan yeni görev başlamalı; can, mühimmat, süre, veri cihazı, paketler ve muhafızlar başlangıç durumuna dönmeli. Oyuncu tercihlerin korunmalı. Esc → **Ana Menü** ise görev özeti ekranına götürmeli.
- [ ] Başarı ekranından **Yeniden Dene**: doğrudan yeni görev başlamalı. Can `100`, mühimmat `30 / 090`, süre ve muhafız sayacı sıfır; veri cihazı ve paketler yeniden yerlerinde olmalı. Yedek muhafız tekrar pasif başlamalı.
- [ ] Bu yeni denemede muhafızdan hasar alarak canı sıfıra indir: başarısızlık ekranı açılmalı, ateş ve hareket durmalı.
- [ ] Başarısızlıktan **Yeniden Dene** aynı başlangıç sıfırlamasını yapmalı. **Başlangıca Dön** ise görev özeti ekranına götürmeli.
- [ ] Seçtiğin **ses, fare, kamera tepkisi, FOV ve nişangâh boyutu** ayarları **Yeniden Dene** ve **Başlangıca Dön** sonrasında korunmalı. Bunlar görev ilerlemesiyle birlikte sıfırlanmamalı. **Durum: NOT RUN.**
- [ ] Ayarları değiştirip **Geri/Esc** ile ayarlardan çık, oyunu tamamen kapat ve exe'yi yeniden aç. Beş ayar da önceki seçimini korumalı; görev ise başlangıç özetinden açılmalı. Windows'ta uygulamayı tam kapatıp yeniden açma kontrolü, yalnız Unity'de Play'i durdurup başlatmaktan ayrıdır. **Durum: NOT RUN.**

## 8. Bahçe ve son görsel tur — 18–20 dakika

- [ ] Havuzun sığ su yüzeyini, tabanını ve kenarlarını incele. Titreyen yüzey, büyük boşluk, görünmez duvar veya suya yaklaşınca düşme var mı? Bu sürümde yüzme mekaniği bekleme.
- [ ] **NOT RUN:** Bahçede düşük seviyeli rüzgâr, havuza yaklaşınca hafif su sesi duyulmalı. Su sesi uzaklaşınca azalmalı; villanın içine girince rüzgâr yumuşayarak kısılmalı. Silah ve etkileşim sesleri bu arka planın üzerinde okunaklı kalmalı.
- [ ] **NOT RUN:** Rüzgâr/su döngüleri görev özeti, duraklatma ve başarı/başarısızlık ekranlarında durmalı. Devam edince yumuşakça geri gelmeli; uzun dinlemede döngü sınırında belirgin tık/sıçrama var mı? Sonuç bildirim sesi ortam döngüsünden ayrıdır.
- [ ] Bahçe çitinin ve kapalı sınırların içinden yürüyerek geçilememeli. Haritanın dışına istemeden çıkılmamalı.
- [ ] Krem duvar, petrol mavisi, bakır ve adaçayı renkleri okunaklı mı? Düşmanlar, veri cihazı ve paketler çevreden ayırt edilebiliyor mu?
- [ ] Pembe materyal, eksik model, aşırı parlak cam/su, kayıp ses veya Game penceresinde taşan yazı var mı? Mümkünse 16:9 yanında 4:3 oranında da HUD'ye kısa bak.
- [ ] 1024×768 ve 1280×720 gibi iki farklı Game penceresi boyutunda menü düğmelerinin görünen yerleriyle tıklanan alanları örtüşmeli. Küçük yazıları, sonuç sayacı ve ayar yüzdelerini okuyabiliyor musun? Farklı oranlarda metin veya düğme kesilmesi var mı?
- [ ] **NOT RUN:** Hareket, ateş, duraklatma ve devam sırasında belirgin takılma/sıçrama varsa not et. Oyun için **120**, menüler için **60 kare/sn hedef sınırı** ayarlı; bunlar ölçülmüş performans veya garanti edilen FPS değildir. FPS'ye kendin bakarsan ekran boyutunu ve gördüğün değeri kaydet.
- [ ] Play'i durdur. Kaynak `Assets/_LevelProject/Scenes 1/Industrial_Level_01.unity` ayrı durmalı; bu kontrol için yaptığın denemeleri kaynak sahneye kaydetme. Yeni FPS sahnesi üzerinden çalışıyoruz.

## Bir hata olduğunda gönderilecek kısa kayıt

İlk hatayı ayrı kaydet; aynı sonucu tekrar edebiliyorsan adımlarını yaz. Test edilmeyen kutuları boş bırak.

```text
Sahne: Villa_Operation
Çalıştırma: Unity Play / Windows paketi
Ekran boyutu / oranı:
Kontrol maddesi:
Yaptığım işlem (sırayla):
Beklenen:
Gördüğüm / duyduğum:
Her denemede oluyor mu?:
Yaklaşık yer (oda / bahçe / üst kat / kapı; biliniyorsa koordinat):
Console ilk hata mesajı ve stack trace:
Varsayılanlardan farklı denge/oyuncu ayarları:
```

Ekran görüntüsünde **Game veya Scene'deki sorunlu yeri** ve ayrıca **Console'da seçili ilk hatanın ayrıntısını** göster. Console'da kırmızı hata yoksa bunu da belirt. Böylece görsel yerleşim sorunu ile kod hatasını ayırabiliriz.

Windows paketinde Console bulunmaz. Hata olursa oyunu kapatmadan önce sorunun görüntüsünü al; ardından `C:\Users\halil\AppData\LocalLow\DefaultCompany\Villa Operasyonu\Player.log` dosyasını da paylaşabilirsin. Paket ürün adı **Villa Operasyonu** olduğu için Windows oyun günlüğü bu klasöre yazılır. Dosya oyunu sen çalıştırdıktan sonra oluşur veya güncellenir.

Unity Play kontrolünde Console ve `C:\Users\halil\AppData\Local\Unity\Editor\Editor.log` günlüğünü kullan. Kaynak proje ürün adı **Learn_to_LD** olarak kalır; bu adla oluşturulan bir player'ın günlük yolu `C:\Users\halil\AppData\LocalLow\DefaultCompany\Learn_to_LD\Player.log` olur. Bunu teslim edilen Windows paketinin **Villa Operasyonu** Player.log dosyasıyla karıştırma; Unity Play hataları için esas kayıt Console/Editor günlüğüdür.
