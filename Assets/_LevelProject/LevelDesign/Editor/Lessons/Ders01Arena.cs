using System;
using System.Linq;
using UnityEngine;

/// <summary>Lesson 1: a combat arena that gives F.E.A.R.-style enemies cover, a loop and a flank route.</summary>
public static class Ders01Arena
{
    public const string Id = "ders01-arena";

    public static readonly string[] VillaScenes =
    {
        "Assets/_LevelProject/FPS/Scenes/Villa_Operation.unity",
        "Assets/_LevelProject/Scenes 1/Industrial_Level_01.unity"
    };

    /// <summary>The arena from the lesson's grid-paper plan; (0, 0) is the middle of the central block.</summary>
    public static readonly LDTableRow[] Table =
    {
        R("Zemin", LDRoleType.Zemin, 1.5f, -0.1f, 0, 25, 0.2f, 26),
        R("Duvar_K_Sol", LDRoleType.Duvar, -5.5f, 2, 12, 9, 4, 0.5f),
        R("Duvar_K_Sag", LDRoleType.Duvar, 5.5f, 2, 12, 9, 4, 0.5f),
        R("Lento_K", LDRoleType.Kapi, 0, 3.25f, 12, 2, 1.5f, 0.5f),
        R("Duvar_G_Sol", LDRoleType.Duvar, -8.5f, 2, -12, 3, 4, 0.5f),
        R("Duvar_G_Sag", LDRoleType.Duvar, 2.5f, 2, -12, 15, 4, 0.5f),
        R("Lento_G", LDRoleType.Kapi, -6, 3.25f, -12, 2, 1.5f, 0.5f),
        R("Duvar_B", LDRoleType.Duvar, -10, 2, 0, 0.5f, 4, 24),
        R("Duvar_D_1", LDRoleType.Duvar, 10, 2, -10.5f, 0.5f, 4, 3),
        R("Duvar_D_2", LDRoleType.Duvar, 10, 2, 0, 0.5f, 4, 14),
        R("Duvar_D_3", LDRoleType.Duvar, 10, 2, 10.5f, 0.5f, 4, 3),
        R("Koridor_Dis", LDRoleType.Duvar, 13, 2, 0, 0.5f, 4, 24),
        R("Koridor_Kapak_G", LDRoleType.Duvar, 11.5f, 2, -12, 3, 4, 0.5f),
        R("Orta_Blok", LDRoleType.Blok, 0, 2, 0, 5, 4, 6),
        R("Siper_A", LDRoleType.AlcakSiper, -6.5f, 0.5f, 7.5f, 3, 1, 0.5f),
        R("Siper_B", LDRoleType.AlcakSiper, 6.5f, 0.5f, 7.5f, 3, 1, 0.5f),
        R("Siper_C", LDRoleType.AlcakSiper, -8, 0.5f, -5, 0.5f, 1, 3),
        R("Siper_D", LDRoleType.AlcakSiper, 7.5f, 0.5f, -4, 0.5f, 1, 3),
        R("Siper_E", LDRoleType.AlcakSiper, 0, 0.5f, -8, 3, 1, 0.5f),
        R("ScaleRef", LDRoleType.OyuncuGirisi, -6, 1, -10.5f, 1, 1, 1),
        R("Dusman_Kapi", LDRoleType.DusmanGirisi, 0, 0, 12.8f, 0, 0, 0),
        R("Dusman_Koridor", LDRoleType.DusmanGirisi, 11.5f, 0, 12.5f, 0, 0, 0)
    };

    public static LDLesson Create(Action createLessonScene) => new LDLesson
    {
        Id = Id,
        Title = "Ders 1: Çatışma arenası",
        Steps = new[]
        {
            new LDStep
            {
                Title = "0. Hazırlık",
                What = "Ayrı bir ders sahnesinde çalışacağız. Aşağıdaki düğme LD_Ders01_Arena sahnesini, ışığı, kamerayı, boş bir Arena nesnesini ve Gri / Sarı / Kırmızı greybox malzemelerini hazırlar. Bütün parçaları Arena nesnesinin altına koy.",
                Why = "Villa sahnesi araçlar tarafından yeniden üretilebilir; orada elle yaptığın düzenlemeler kaybolabilir. Öğrenirken kendi sahnende rahat deneme yaparsın.",
                Terms = new[]
                {
                    ("Greybox", "Level'ı süssüz gri kutularla kurmak. Amaç güzel görünmek değil, oynanışı denemek."),
                    ("Grid / snapping", "Sahnedeki görünmez kareli kağıt ve nesnelerin taşınırken ona yapışması. Ctrl basılı taşırsan değerler yuvarlanır.")
                },
                Check = s => new LDStepResult()
                    .Require(!string.IsNullOrEmpty(s.ScenePath), "Sahneyi kaydet (File → Save) ya da 'Ders sahnesini oluştur' düğmesine bas.")
                    .Require(!VillaScenes.Contains(s.ScenePath), "Villa sahnesinde değil, ders sahnesinde çalış."),
                ActionLabel = "Ders sahnesini oluştur",
                Action = createLessonScene
            },
            new LDStep
            {
                Title = "1. Zemin",
                What = "GameObject → ProBuilder → Cube ile bir küp oluştur, tablodaki Position ve Scale değerlerini yaz, Gri malzemeyi ver. Sonra seçiliyken aşağıdan 'Seçilenler → Zemin' rolünü ata.",
                Why = "Zemin, yapay zekânın yürüyebileceği yüzeyin (NavMesh) temeli. Bütün yükseklikler zeminin üstünden ölçülür.",
                Table = Rows("Zemin"),
                Terms = new[] { ("Pivot", "Nesnenin tutulduğu nokta. Position, küpün tam ortasını gösterir.") },
                Check = s =>
                {
                    var result = new LDStepResult().Require(s.Count(LDRoleType.Zemin) >= 1, "Zemin rolü olan bir parça yok.");
                    if (result.Passed)
                    {
                        Bounds floor = s.OfRole(LDRoleType.Zemin).OrderByDescending(p => p.Bounds.size.x * p.Bounds.size.z).First().Bounds;
                        float shortSide = Mathf.Min(floor.size.x, floor.size.z), longSide = Mathf.Max(floor.size.x, floor.size.z);
                        result.Require(shortSide >= 20f - LDRules.Epsilon && longSide >= 24f - LDRules.Epsilon,
                            $"Zemin {LDRules.M(floor.size.x)} × {LDRules.M(floor.size.z)} m: en az 20 × 24 m olmalı.");
                    }
                    return result;
                }
            },
            new LDStep
            {
                Title = "2. Dış duvarlar",
                What = "Dış duvarları tablodaki gibi kur ve 'Duvar' rolünü ver. Kuzey ve güney duvarları iki parça: aralarında 2 m'lik kapı boşluğu kalıyor. Doğu duvarı üç parça: aralarındaki boşluklar yan koridora açılıyor.",
                Why = "Duvarlar görüş hattını keser ve oyuncunun nereden saldırı geleceğini okumasını sağlar. Boşluklar, düşmanın arenaya hangi yoldan gireceğini belirler.",
                Table = Rows("Duvar_K_Sol", "Duvar_K_Sag", "Duvar_G_Sol", "Duvar_G_Sag", "Duvar_B", "Duvar_D_1", "Duvar_D_2", "Duvar_D_3"),
                Terms = new[] { ("Metrikler", "Hep aynı kalan ölçüler. Duvar 4 m yüksek, 0,5 m kalın; kapı boşluğu 2 m.") },
                Check = s =>
                {
                    var shortWalls = s.OfRole(LDRoleType.Duvar).Where(p => LDRules.HeightAboveFloor(s.Pieces, p.Bounds) < 3.5f - LDRules.Epsilon).Select(p => p.Name).ToList();
                    return new LDStepResult()
                        .Require(s.Count(LDRoleType.Duvar) >= 4, $"En az 4 duvar gerekli, şu an {s.Count(LDRoleType.Duvar)}.")
                        .Require(shortWalls.Count == 0, "Bu duvarlar 3,5 m'den alçak: " + string.Join(", ", shortWalls));
                }
            },
            new LDStep
            {
                Title = "3. Kapılar",
                What = "Kapı boşluklarının üstüne lentoları koy ve lentolara 'Kapı (lento)' rolünü ver. Kapının genişliği lentonun uzunluğundan, yüksekliği lentonun alt kenarından ölçülür.",
                Why = "FPS'te kapılar gerçeğinden biraz büyük olur: 2 m genişlik iki kişinin aynı anda geçmesine, düşmanın kapıda sıkışmamasına izin verir. 2,5 m yükseklik kameranın lentoya takılmasını önler.",
                Table = Rows("Lento_K", "Lento_G"),
                Terms = new[] { ("Lento", "Kapı boşluğunun üstündeki duvar parçası.") },
                Rules = new[] { 3 },
                Check = s => new LDStepResult()
                    .Require(s.Count(LDRoleType.Kapi) >= 2, $"2 kapı (lento) gerekli, şu an {s.Count(LDRoleType.Kapi)}.")
                    .Require(!s.HasError(3), "Kapı ölçülerinde hata var; aşağıdaki bulgulara bak.")
            },
            new LDStep
            {
                Title = "4. Orta blok",
                What = "Arenanın ortasına tavana kadar uzanan bloğu koy ve 'Blok / kolon' rolünü ver.",
                Why = "Bu arenanın kalbi. Etrafında tam tur atılabildiği için düşman bir yandan seni oyalarken öbür yandan dolanabilir (flank). Ayrıca oyuncunun girişten bütün arenayı görmesini engeller.",
                Table = Rows("Orta_Blok"),
                Terms = new[]
                {
                    ("Döngü (loop)", "Bir bloğun etrafında tam tur atılabilen yol. Flank ancak böyle bir yol varsa mümkün olur."),
                    ("Flank", "Düşmanın yandan dolanıp seni kuşatması.")
                },
                Rules = new[] { 2 },
                Check = s => new LDStepResult()
                    .Require(s.Count(LDRoleType.Blok) >= 1, "Blok rolü olan bir parça yok.")
                    .Require(!s.HasError(2), "Blok en az 1,9 m yüksek olmalı.")
            },
            new LDStep
            {
                Title = "5. Siperler",
                What = "Beş alçak siperi yerleştir, Sarı malzemeyi ver ve 'Alçak siper' rolünü ata. Koyarken Scene'de oyuncu gözünden de bak.",
                Why = "Alçak siper bel hizasında (1 m) olur: hem sen hem düşman üstünden ateş edebilir. Siperler 3–6 m arayla zincir gibi dizilirse düşman siperden sipere sıçrayarak ilerler; FEAR hissini veren de budur. Bu planda zinciri orta blok tamamlıyor.",
                Table = Rows("Siper_A", "Siper_B", "Siper_C", "Siper_D", "Siper_E"),
                Terms = new[] { ("Siper (cover)", "Arkasına saklanılan nesne. Alçak siper bel hizasında, yüksek siper insanı tamamen gizler.") },
                Rules = new[] { 1, 5 },
                Check = s => new LDStepResult()
                    .Require(s.Count(LDRoleType.AlcakSiper) >= 5, $"En az 5 alçak siper gerekli, şu an {s.Count(LDRoleType.AlcakSiper)}.")
                    .Require(!s.HasError(1), "Alçak siper yüksekliği 0,9–1,2 m olmalı.")
            },
            new LDStep
            {
                Title = "6. Yan koridor ve girişler",
                What = "Yan koridorun dış duvarını ve güney kapağını kur. GameObject → 3D Object → Capsule ile ScaleRef adında bir kapsül oluştur ve 'Oyuncu girişi' rolünü ver. Kuzey kapının ağzına ve koridorun kuzey ucuna birer boş nesne (Create Empty) koyup 'Düşman girişi' rolünü ver.",
                Why = "Düşmanın iki giriş yolu olması flank'i mümkün kılar: biri kapıdan seni oyalarken öbürü koridordan yanına geçer. Bu tabloyla oyuncu girişinden kuzey kapısı büyük ihtimalle görünecek (görüş hattı uyarısı). Bu senin ilk tasarım kararın: bloğu, kapıyı ya da girişi kaydırarak hattı kes.",
                Table = Rows("Koridor_Dis", "Koridor_Kapak_G", "ScaleRef", "Dusman_Kapi", "Dusman_Koridor"),
                Terms = new[]
                {
                    ("Spawn / giriş", "Oyuncunun ya da düşmanın oyuna girdiği nokta. Play'e basınca oyuncu ScaleRef'in yerinde başlar."),
                    ("Görüş hattı (sightline)", "Bir noktadan neyin görülebildiği.")
                },
                Rules = new[] { 6, 7, 8, 9 },
                Check = s => new LDStepResult().Require(!s.HasError(6), "Oyuncu girişi ve en az bir düşman girişi gerekli.")
            },
            new LDStep
            {
                Title = "7. NavMesh",
                What = "Boş bir nesne oluştur, adını NavMesh koy, Add Component → NavMesh Surface ekle ve Bake'e bas. Zemin maviye boyanır: mavi yerler düşmanın yürüyebildiği yerlerdir.",
                Why = "Yapay zekâ ancak NavMesh'in olduğu yerde yürür. Düşman girişlerinden oyuncu girişine yürüyebilmesi gerekir. Siperle duvar arasında mavi kesiliyorsa geçit çok dardır.",
                Terms = new[]
                {
                    ("NavMesh", "Yapay zekânın 'nereye yürüyebilirim' haritası."),
                    ("Bake", "Unity'nin bu haritayı zeminden hesaplaması. Parçaları değiştirince yeniden bake et.")
                },
                Rules = new[] { 10, 4 },
                Check = s => new LDStepResult().Require(!s.HasError(10), "NavMesh eksik ya da düşman oyuncuya yürüyemiyor.")
            },
            new LDStep
            {
                Title = "8. Oyun testi",
                What = "Play'e bas ve arenayı oyuncu gözüyle gez (WASD, fare, Shift koşu, Esc fareyi bırakır). Aşağıdaki kutuları kendin denedikten sonra işaretle.",
                Why = "Kurallar ölçüleri kontrol eder ama hissi sen kontrol edersin. Döngü gerçekten akıcı mı, giriş gerilim veriyor mu, düşman yolu mantıklı mı?",
                ManualChecks = new[]
                {
                    "Orta bloğun etrafında tam tur attım",
                    "Oyuncu girişinden arenaya baktım",
                    "Düşmanın koridordan geleceği yolu yürüdüm"
                }
            }
        }
    };

    private static LDTableRow R(string name, LDRoleType role, float px, float py, float pz, float sx, float sy, float sz)
        => new LDTableRow { Name = name, Role = role, Position = new Vector3(px, py, pz), Scale = new Vector3(sx, sy, sz) };

    private static LDTableRow[] Rows(params string[] names) => names.Select(n => Table.First(r => r.Name == n)).ToArray();
}
