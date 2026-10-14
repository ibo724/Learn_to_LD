# Villa Operasyonu — Sanat Rehberi

Görsel yön: **Akdeniz villası biçiminde bir operasyon maketi.** Büyük sıva yüzeyleri, belirgin çerçeveler, sekiz yüzlü karakter başları ve az sayıda tekrarlanan geometrik parça sahnenin dilini oluşturur. Hafif gölge bantları hacmi anlatır; oyuncu kapıyı, siperi ve muhafızı ilk bakışta ayırt edebilmelidir. Silah ve karakterler çevreyle aynı renk ailesini kullanır.

**Paletin görevleri**

| Renk | HEX | Mevcut kullanım ve devam kuralı |
|---|---|---|
| Sıcak sıva | `#E8DFC9` | Villanın büyük duvarları ve bahçe sınırları. Sahnenin sakin ana rengi. |
| Fildişi | `#F3EBDD` | Tezgâhlar, açık ayrıntılar, havuz gövdesi ve sağlık çantası. Büyük sıvadan daha aydınlık bir vurgu. |
| Kireç taşı | `#BDBBA5` | Zeminler, merdivenler, yol, oturma ve havuz kenarları. Yürünecek yüzeyleri birleştirir. |
| Petrol mavisi | `#294E58` | Kapı kanatları, kornişler, dolaplar, sandıklar ve muhafız üniforması. Açık mimari üzerinde güçlü siluet sağlar. |
| Bakır | `#B9784B` | Kapı/pencere kasaları, korkuluklar, banklar ve karabina ayrıntıları. Tekrarlanan sıcak bağlantı rengi; yüzeyi mat tutulur. |
| Terracotta | `#BC6952` | Saksılar, muhafız zırhı ve kaskı, sağlık işareti. Küçük ve kolay seçilen sıcak yüzeyler. |
| Sakin yeşil | `#819361` | Köşeli ağaç taçları ve çalılar. Terrain çiminde buna yakın `#89986B`, toprakta `#A28B68` kullanılır. |
| Turkuaz | `#63C6BE` | Havuzun sığ suyu, camın hafif tonu ve arayüz vurgusu. Derin su `#287B87` ile koyulaşır. |
| Koyu mürekkep | `#263B42` | Karabina gövdesi, eldivenler, botlar ve veri cihazı. En koyu rengin küçük parçalarda kalması okunaklılığı korur. |
| Sinyal sarısı | `#D7B35A` | Muhafız siperi, veri cihazının yüzü, mühimmat ve çıkış ayrıntıları. Dikkat çeken işaretlerde sınırlı kullanılır. |

**Yüzey dili**

Duvar, taş, bitki, silah ve zırh için `Villa/Matte` kullanılır. Ana renk ve gölge rengi arasındaki kısa geçiş, köşeli modeli okunur tutar. Materyallerin başlangıç gölge eşiği `0.45`, geçişi `0.08`, iç mekân parlaklık tabanı `0.7` değerindedir. Önce ana rengi, ardından gölge rengini değiştir; tek bir nesneyi düzeltmek için bütün sahnenin ışığını değiştirme. Bakır adlı materyal de bu mat yüzey diline dahildir.

Karabina gövdesi, dipçiği, eldivenler ve muhafızın büyük zırh parçalarında üç eksende küçük bevel yüzeyleri bulunur. Aynı kırık kenar dili seçili sandık, dolap, tezgâh, masa ve banklarda tekrarlanır. Ortak `BevelBox3D` mesh’i 44 üçgenle bu geçişleri kurar. Büyük mimari yüzeyler sade kalır; her duvarı yuvarlatmak veya küçük parçaları çoğaltmak yerine ışığı yakalayan bu dar kenarlar kullanılır.

Cam, URP/Lit üzerinde `0.16` alpha ve hafif turkuaz kullanır; çerçeve silueti camdan daha belirgin kalır. Havuz `Villa/Water` kullanır: `0.72` başlangıç opaklığı, derinliğe göre renk ve küçük hareketli çizgiler. Suyun kenarı ile havuz tabanı seçilebilmelidir. Gökyüzü `Villa/Sky` ile petrol tonundan krem ufka geçer.

**Ayrıntı ve hareket ölçüsü**

Yeni dekorları sade bir dış çizgi ve bir ana renk üzerinden tasarla. Büyük yüzeylerde sakin renkleri koru; yazı, parlak vurgu ve küçük parçaları hedefi anlatmak için kullan. Mutfakta birkaç çekmece ve kulp, bir tepsi ve kupa; arşivde açık raflar ve az sayıda renkli kitap bulunur. Sade geometrik duvar panosu aynı paleti tekrarlar. Kapıların, merdivenin ve veri cihazının önündeki alan açık kalmalıdır.

Muhafızın terracotta kaskı, koyu çerçeveli sarı siperi, dar baldır plakaları ve petrol mavisi üniforması uzaktan tek bir karakter olarak okunmalı. Bacak yürüyüşü küçük tutulur; eller ve tüfek nişan pozunu korur. Baldır plakaları mevcut diz rig’ini takip eder ve ölümde bacaklar nötr poza döner. Karabinanın koyu gövdesi ve bakır bölümleri aynı sadeliği sürdürür; küçük başparmak/manşet biçimleri tutuşu anlatır. Fildişi nişangâh işaretlerinin yeri korunur; bunlar süs veya parlak ışık kaynağı olarak büyütülmez.

Atış geri bildirimi de geometriktir: kısa namlu parlaması, küçük sekizgen yüzey izi, camda açık bir halka ve birkaç kısa ömürlü parça. Aynı hazır efekt geometrileri yeniden kullanılır. Muhafıza isabet edildiğinde kısa bir sıcak vurgu görünür. Bu efektler hedefi ve isabeti okumaya yardım eder; yoğun parçacık, kalıcı parlama veya yüzeyi kaplayan büyük izler eklenmez. Cam darbe halkası, camın kırıldığı anlamına gelmez.

İlk görsel kontrolde giriş, merdiven ve veri cihazının bulunduğu oda üzerinden renkleri değerlendir. Camların arkasındaki muhafızlar, çim üzerindeki sağlık çantası ve çıkış işareti seçilebilmeli. FPS çalışma anında kendi Forward pipeline’ını kullanır; editörün kaynak PC görünümündeki temas gölgeleri farklı olabilir. Asıl renk kararını oyunun Game görünümünde ver. Görsel kontrol ve ayakların merdivene oturuşu henüz kullanıcı doğrulaması bekliyor.

**Projede karşılıkları**

- [Palet ve materyal üretimi](/C:/Projects/Learn_to_LD/Assets/_LevelProject/Editor/VillaArt.cs), [sıva materyali](/C:/Projects/Learn_to_LD/Assets/_LevelProject/FPS/Materials/Plaster.mat), [bakır materyali](/C:/Projects/Learn_to_LD/Assets/_LevelProject/FPS/Materials/Copper.mat).
- [Mat yüzey shader’ı](/C:/Projects/Learn_to_LD/Assets/_LevelProject/FPS/Shaders/VillaMatte.shader), [su shader’ı](/C:/Projects/Learn_to_LD/Assets/_LevelProject/FPS/Shaders/VillaWater.shader), [gökyüzü shader’ı](/C:/Projects/Learn_to_LD/Assets/_LevelProject/FPS/Shaders/VillaSky.shader).
- [Karabina prefabı](/C:/Projects/Learn_to_LD/Assets/_LevelProject/FPS/Prefabs/Carbine.prefab), [muhafız prefabı](/C:/Projects/Learn_to_LD/Assets/_LevelProject/FPS/Prefabs/Guard.prefab), [geometrik model üretimi](/C:/Projects/Learn_to_LD/Assets/_LevelProject/Editor/VillaModelFactory.cs).
- [Ortak bevel mesh’i](/C:/Projects/Learn_to_LD/Assets/_LevelProject/FPS/Meshes/BevelBox3D.asset), [geometrik darbe ve namlu efektleri](/C:/Projects/Learn_to_LD/Assets/_LevelProject/FPS/Scripts/VillaImpactEffects.cs).
- [FPS sahnesi](/C:/Projects/Learn_to_LD/Assets/_LevelProject/FPS/Scenes/Villa_Operation.unity), [Forward pipeline](/C:/Projects/Learn_to_LD/Assets/_LevelProject/FPS/Rendering/VillaForwardPipeline.asset), [yarınki kontrol listesi](/C:/Projects/Learn_to_LD/Documents/Villa_FPS_Yarin_Kontrol_Listesi.md).

Bu görünüm Unity URP ve projedeki özel shader’larla hazırlanmıştır; Flat Kit veya başka ücretli shader paketi gerektirmez.
