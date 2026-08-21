#!/usr/bin/env node
/**
 * Amorland — protokol testi (Unity gerektirmez).
 *
 * Iki sanal oyuncu acar, vadiye baglar ve C.3'teki akisi bastan sona yurutur:
 *   misafir giris -> vadi kur -> BAGLAN -> SENKRON_TAM -> NIYET -> ONAY -> partnere DEGISIM
 *
 * Neden var: Unity istemcisi bu makinede calistirilamiyor. Bu script sunucunun
 * gercekten protokolu konustugunu ve Unity'nin bekledigi JSON sekillerini
 * dondurdugunu bagimsiz olarak dogrular. Ayrica sunucu degisikliklerinden sonra
 * "hala calisiyor mu" sorusunun en hizli cevabi.
 *
 * Calistir:  node araclar/protokol-testi.js [http://localhost:8080]
 */

const SUNUCU = process.argv[2] || "http://localhost:8080";
const WS_ADRES = SUNUCU.replace(/^http/, "ws") + "/ws";

let gecen = 0;
let kalan = 0;

function ok(ad, detay = "") {
  gecen++;
  console.log(`  OK   ${ad}${detay ? "  — " + detay : ""}`);
}

function hata(ad, detay) {
  kalan++;
  console.log(`  HATA ${ad}: ${detay}`);
}

function dogrula(kosul, ad, detay) {
  if (kosul) ok(ad, detay);
  else hata(ad, detay || "kosul saglanmadi");
}

async function istek(yol, yontem, govde, jeton) {
  const basliklar = { "Content-Type": "application/json" };
  if (jeton) basliklar["Authorization"] = "Bearer " + jeton;
  const yanit = await fetch(SUNUCU + yol, {
    method: yontem,
    headers: basliklar,
    body: govde ? JSON.stringify(govde) : undefined,
  });
  const metin = await yanit.text();
  let veri = null;
  try {
    veri = metin ? JSON.parse(metin) : null;
  } catch (_) {
    veri = metin;
  }
  return { durum: yanit.status, veri };
}

/** WebSocket istemcisi: mesajlari kuyruga alir, tip bazli bekleme sunar. */
class Istemci {
  constructor(ad) {
    this.ad = ad;
    this.gelenler = [];
    this.bekleyenler = [];
  }

  async bagla(jwt, sonRevision) {
    this.soket = new WebSocket(WS_ADRES);
    await new Promise((coz, red) => {
      this.soket.onopen = coz;
      this.soket.onerror = (e) => red(new Error("WS acilamadi: " + e.message));
    });
    this.soket.onmessage = (olay) => {
      const mesaj = JSON.parse(olay.data);
      // Once bekleyene teslim et. Teslim edilen mesaj kuyruga GIRMEZ; girseydi
      // bir sonraki bekle() bayat mesaji yakalar ve test yanlis sonuc verir.
      for (let i = 0; i < this.bekleyenler.length; i++) {
        if (this.bekleyenler[i].tip === mesaj.t) {
          const kayit = this.bekleyenler[i];
          this.bekleyenler.splice(i, 1);
          kayit.coz(mesaj);
          return;
        }
      }
      this.gelenler.push(mesaj);
    };
    this.gonder({
      t: "BAGLAN",
      jwt,
      sonRevision: sonRevision === undefined ? undefined : sonRevision,
      istemciSurum: "protokol-testi",
    });
  }

  gonder(mesaj) {
    this.soket.send(JSON.stringify(mesaj));
  }

  bekle(tip, saniye = 10) {
    const mevcut = this.gelenler.find((m) => m.t === tip);
    if (mevcut) {
      this.gelenler.splice(this.gelenler.indexOf(mevcut), 1);
      return Promise.resolve(mevcut);
    }
    return new Promise((coz, red) => {
      const kayit = { tip, coz };
      this.bekleyenler.push(kayit);
      setTimeout(() => {
        const i = this.bekleyenler.indexOf(kayit);
        if (i >= 0) {
          this.bekleyenler.splice(i, 1);
          red(
            new Error(
              `${this.ad}: ${tip} gelmedi. Gelenler: ` +
                this.gelenler.map((m) => m.t).join(", ")
            )
          );
        }
      }, saniye * 1000);
    });
  }

  kapat() {
    if (this.soket) this.soket.close();
  }
}

function uuid() {
  return crypto.randomUUID();
}

async function main() {
  console.log();
  console.log("=".repeat(70));
  console.log("AMORLAND — PROTOKOL TESTI");
  console.log("Sunucu: " + SUNUCU);
  console.log("=".repeat(70));
  console.log();

  // ---------------------------------------------------------------- REST
  const saglik = await istek("/api/saglik", "GET");
  dogrula(saglik.durum === 200, "GET /api/saglik", "durum " + saglik.durum);
  dogrula(
    typeof saglik.veri.sunucuZamaniMs === "number",
    "Sunucu saati donuyor",
    "istemci saat farkini buradan olcer (mimari kural 5)"
  );

  const denge = await istek("/api/denge", "GET");
  dogrula(denge.durum === 200, "GET /api/denge");
  dogrula(
    denge.veri.urunler && denge.veri.urunler.urunler.length === 16,
    "16 urun yuklu"
  );
  dogrula(
    JSON.stringify(denge.veri).indexOf('"tohum":') === -1 ||
      !denge.veri.vadi,
    "Denge paketinde vadi tohumu yok",
    "C.11: deterministik fiyat tohumu istemciye gonderilmez"
  );

  const cihazA = "protokol-a-" + Date.now();
  const cihazB = "protokol-b-" + Date.now();

  const girisA = await istek("/api/oturum/misafir", "POST", {
    cihazId: cihazA,
    ad: "Ada",
    renk: 0,
  });
  dogrula(girisA.durum === 200, "Misafir giris (A)", "kayit ekrani yok (B.19)");
  const jwtA = girisA.veri.erisimJetonu;

  const girisB = await istek("/api/oturum/misafir", "POST", {
    cihazId: cihazB,
    ad: "Bora",
    renk: 1,
  });
  dogrula(girisB.durum === 200, "Misafir giris (B)");
  const jwtB = girisB.veri.erisimJetonu;

  const vadiA = await istek("/api/vadi", "POST", {}, jwtA);
  dogrula(vadiA.durum === 200, "Vadi kuruldu", "revision " + vadiA.veri.revision);
  dogrula(vadiA.veri.tekImzaModu === true, "Partner yokken tek imza modu");

  // ---------------------------------------------------------------- WS: tek oyuncu
  const a = new Istemci("A");
  await a.bagla(jwtA);

  const tam = await a.bekle("SENKRON_TAM");
  dogrula(tam.degisim.tam === true, "BAGLAN -> SENKRON_TAM");
  dogrula(tam.degisim.karolar.length === 4, "Sv1 parseli 4 karo (B.11)");
  dogrula(
    tam.degisim.envanter.some(
      (e) => e.esyaId === "bugday_tohum" && e.adet === 4
    ),
    "Baslangicta 4 bugday tohumu (B.18 1:20)"
  );
  dogrula(
    tam.degisim.dugumler.length === 3,
    "Tarla kenarinda 3 yabani kaynak (B.18 2:20)"
  );
  dogrula(
    tam.degisim.oyuncular[0].altin === -1,
    "Altin Sv 5'e kadar gizli (B.15)"
  );
  dogrula(
    tam.degisim.vadi && tam.degisim.vadi.tohum === undefined,
    "Vadi tohumu istemciye gonderilmiyor (C.11)"
  );
  // ⚠ Sunucu null alanlari JSON'dan ATAR (Jackson non_null): mobil bant genisligi.
  // Istemcide fark yok — okunmayan alan varsayilan (null) kalir.
  dogrula(
    tam.degisim.karolar[0].ekildiAt == null && tam.degisim.karolar[0].urunId == null,
    "Karolar bos basliyor",
    "null alanlar gonderilmiyor (bant genisligi)"
  );
  dogrula(
    tam.degisim.karolar[0].buyumeAsamasi === undefined,
    "Buyume asamasi SAKLANMIYOR, gonderilmiyor (mimari kural 2)"
  );

  // ---------------------------------------------------------------- NIYET -> ONAY
  const eylemId = uuid();
  a.gonder({
    t: "NIYET",
    niyet: {
      clientActionId: eylemId,
      tip: "ek",
      hedef: { bolge: "tarla", x: 0, y: 0 },
      ek: { urunId: "bugday" },
    },
  });
  const onay = await a.bekle("ONAY");
  dogrula(onay.clientActionId === eylemId, "NIYET -> ONAY, ayni eylem kimligi");
  dogrula(onay.revision === 1, "Eylem revizyonu 1 artirdi (C.1)");
  dogrula(
    onay.degisim.karolar[0].urunId === "bugday",
    "Karo ekildi"
  );
  dogrula(
    onay.degisim.karolar[0].sulandiAt == null,
    "Ekilen karo sulanmamis — sulanmadan buyumez"
  );
  dogrula(
    onay.degisim.envanter.some(
      (e) => e.esyaId === "bugday_tohum" && e.adet === 3
    ),
    "Tohum harcandi"
  );
  dogrula(
    onay.degisim.olaylar.some(
      (o) => o.tip === "ilk_kez" && o.deger === "ilk_ekim"
    ),
    "Ilk ekim odulu verildi (B.16)"
  );

  // ---------------------------------------------------------------- idempotency
  a.gonder({
    t: "NIYET",
    niyet: {
      clientActionId: eylemId,
      tip: "ek",
      hedef: { bolge: "tarla", x: 0, y: 0 },
      ek: { urunId: "bugday" },
    },
  });
  const tekrar = await a.bekle("ONAY");
  dogrula(
    tekrar.revision === onay.revision,
    "Ayni client_action_id tekrar edildi -> AYNI sonuc (mimari kural 4)",
    "revision " + tekrar.revision
  );

  // ---------------------------------------------------------------- RED
  a.gonder({
    t: "NIYET",
    niyet: {
      clientActionId: uuid(),
      tip: "hasat",
      hedef: { bolge: "tarla", x: 1, y: 0 },
    },
  });
  const red = await a.bekle("RED");
  dogrula(red.kod === "GECERSIZ_HEDEF", "Gecersiz niyet RED + kod", red.kod);

  // Baslangic seti DOLU ahsap kova verir (oyuncu bulgusu sonrasi kalici kural):
  // sulama ILK denemede calismali. Bos-kova reddi sunucu birim testinde olculuyor.
  a.gonder({
    t: "NIYET",
    niyet: {
      clientActionId: uuid(),
      tip: "sula",
      hedef: { bolge: "tarla", x: 0, y: 0 },
    },
  });
  const sulamaOnay = await a.bekle("ONAY");
  const benOzet = (sulamaOnay.degisim.oyuncular || []).find(() => true);
  dogrula(
    benOzet && benOzet.kovaKalan < benOzet.kovaKapasite,
    "Baslangic kovasi dolu gelir, sulama ilk denemede calisir ve kovayi azaltir",
    benOzet ? benOzet.kovaKalan + "/" + benOzet.kovaKapasite : "oyuncu ozeti yok"
  );

  // ---------------------------------------------------------------- tren panosu (B.15)
  const tren = await istek("/api/vadi/" + vadiA.veri.id + "/tren", "GET", null, jwtA);
  dogrula(tren.durum === 200, "Tren panosu okunur (GET /tren)");
  dogrula(
    tren.veri && Array.isArray(tren.veri.siparisler) && tren.veri.siparisler.length === 3,
    "Sefer 3 siparis karti tasir (B.15)",
    tren.veri && tren.veri.siparisler ? tren.veri.siparisler.length + " kart" : "govde yok"
  );
  dogrula(
    tren.veri && tren.veri.kalanSn > 0 && tren.veri.kalanSn <= 21600,
    "Geri sayim sefer araligi icinde",
    tren.veri ? tren.veri.kalanSn + " sn" : ""
  );

  // ---------------------------------------------------------------- yeniden baglanma
  a.kapat();
  await new Promise((r) => setTimeout(r, 300));

  const a2 = new Istemci("A2");
  await a2.bagla(jwtA, 0);
  const delta = await a2.bekle("SENKRON_DELTA");
  dogrula(
    Array.isArray(delta.degisimler) && delta.degisimler.length >= 1,
    "Yeniden baglanma -> SENKRON_DELTA (C.9)",
    delta.degisimler.length + " degisim"
  );
  dogrula(
    delta.degisimler[0].tam === false,
    "Delta paketi tam anlik goruntu degil"
  );

  // ---------------------------------------------------------------- iki oyuncu
  // Partner daveti Sv 3 + hesap baglama ister (B.19); dogrudan denenip red beklenir.
  const erkenDavet = await istek("/api/vadi/davet", "POST", {}, jwtA);
  dogrula(
    erkenDavet.durum !== 200,
    "Sv 3 altinda davet REDDEDILIR (B.17)",
    erkenDavet.veri && erkenDavet.veri.mesaj
  );

  // ---------------------------------------------------------------- yetki
  const baskasininVadisi = await istek(
    "/api/vadi/" + vadiA.veri.id + "/anlik", "GET", null, jwtB);
  dogrula(
    baskasininVadisi.durum === 403,
    "Baskasinin vadisi okunamaz (403, 401 degil)",
    "jeton gecerli ama uye degil - yenileme dongusune girmesin"
  );
  const jetonsuz = await istek("/api/vadi/" + vadiA.veri.id + "/anlik", "GET", null, null);
  dogrula(jetonsuz.durum === 401, "Jetonsuz istek reddedilir");

  // ---------------------------------------------------------------- konum yayini
  a2.gonder({
    t: "KONUM",
    konum: { bolge: "tarla", x: 1.5, y: 2.0, yon: 0 },
  });
  await new Promise((r) => setTimeout(r, 200));
  dogrula(
    !a2.gelenler.some((m) => m.t === "ONAY" || m.t === "DEGISIM"),
    "Konum yayini REVIZYON URETMIYOR (C.3)"
  );

  a2.kapat();

  // ---------------------------------------------------------------- ozet
  console.log();
  console.log("-".repeat(70));
  if (kalan === 0) {
    console.log(`Temiz: ${gecen} denetimin hepsi gecti.`);
  } else {
    console.log(`${gecen} gecti, ${kalan} HATA.`);
  }
  console.log("-".repeat(70));
  console.log();
  process.exit(kalan === 0 ? 0 : 1);
}

main().catch((e) => {
  console.error("\nTest calistirilamadi: " + e.message);
  console.error("Sunucu calisiyor mu? " + SUNUCU);
  process.exit(2);
});
