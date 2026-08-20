#!/usr/bin/env node
/**
 * Amorland — GERCEK sunucudan oynanmis bir dunya durumu dokumu alir.
 *
 * Neden: arayuz ekranlarini (HUD, canta, depo) gorsel olarak denetlemek icin
 * icinde bir sey OLAN bir durum gerekiyor. Elle uydurulmus sahte veri, gercekte
 * olmayan bir ekrani guzel gosterebilir — dokum sunucunun kendi urettigi
 * durumdur, o yuzden yalan soylemez.
 *
 * Akis: misafir giris -> vadi kur -> BAGLAN -> kova doldur -> ek -> sula ->
 *       topla -> yeniden baglan -> SENKRON_TAM anlik goruntusunu yaz.
 *
 * Cikti dosyasi istemcideki Protokol.Degisim sinifina birebir uyar; Unity
 * tarafinda JsonConvert ile okunup DunyaDurumu.Uygula'ya verilir.
 *
 * Kullanim:  node araclar/durum-dokumu.js [cikti.json]
 *            SUNUCU=http://localhost:8080 node araclar/durum-dokumu.js
 */

const SUNUCU = process.env.SUNUCU || "http://localhost:8080";
const WS_ADRES = SUNUCU.replace(/^http/, "ws") + "/ws";
const CIKTI = process.argv[2] || "durum-dokumu.json";

function uuid() {
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    return (c === "x" ? r : (r & 0x3) | 0x8).toString(16);
  });
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

class Istemci {
  constructor() {
    this.gelenler = [];
    this.bekleyenler = [];
  }

  async bagla(jwt) {
    this.soket = new WebSocket(WS_ADRES);
    await new Promise((coz, red) => {
      this.soket.onopen = coz;
      this.soket.onerror = (e) => red(new Error("WS acilamadi: " + (e.message || "")));
    });
    this.soket.onmessage = (olay) => {
      const mesaj = JSON.parse(olay.data);
      for (let i = 0; i < this.bekleyenler.length; i++) {
        if (this.bekleyenler[i].tipler.includes(mesaj.t)) {
          const kayit = this.bekleyenler[i];
          this.bekleyenler.splice(i, 1);
          kayit.coz(mesaj);
          return;
        }
      }
      this.gelenler.push(mesaj);
    };
    this.gonder({ t: "BAGLAN", jwt, istemciSurum: "durum-dokumu" });
  }

  gonder(mesaj) {
    this.soket.send(JSON.stringify(mesaj));
  }

  bekle(tip, saniye = 10) {
    return this.bekleHerhangi([tip], saniye);
  }

  /**
   * Verilen tiplerden ILK geleni dondurur.
   *
   * ⚠ Iki ayri bekle() cagrisini Promise.race ile yarististirma: kaybeden
   * bekleyici kuyrukta KALIR ve bir sonraki mesaji sessizce yutar. Tek
   * bekleyici, birden fazla tip — dogru cozum bu.
   */
  bekleHerhangi(tipler, saniye = 10) {
    const mevcut = this.gelenler.find((m) => tipler.includes(m.t));
    if (mevcut) {
      this.gelenler.splice(this.gelenler.indexOf(mevcut), 1);
      return Promise.resolve(mevcut);
    }
    return new Promise((coz, red) => {
      const kayit = { tipler, coz };
      this.bekleyenler.push(kayit);
      setTimeout(() => {
        const i = this.bekleyenler.indexOf(kayit);
        if (i >= 0) {
          this.bekleyenler.splice(i, 1);
          red(new Error(tipler.join("/") + " gelmedi (" + saniye + " sn)"));
        }
      }, saniye * 1000);
    });
  }

  kapat() {
    if (this.soket) this.soket.close();
  }
}

/**
 * Niyeti gonderir ve ONAY ya da RED bekler.
 * ⚠ RED bir HATA DEGILDIR: kova zaten doluysa, karo doluysa veya seviye
 * yetmiyorsa sunucu hakli olarak reddeder. Dokum yine de alinabilir.
 */
async function niyet(a, tip, hedef, ek) {
  const eylemId = uuid();
  a.gonder({ t: "NIYET", niyet: { clientActionId: eylemId, tip, hedef, ek } });
  const m = await a.bekleHerhangi(["ONAY", "RED"], 8);
  const tamam = m.t === "ONAY";
  console.log((tamam ? "  ok " : "  -  ") + tip + (tamam ? "" : " (" + (m.kod || "?") + ")"));
  return { tamam, m };
}

async function main() {
  const saglik = await istek("/api/saglik", "GET");
  if (saglik.durum !== 200) {
    console.error("Sunucu yanit vermiyor (" + SUNUCU + "). Once sunucuyu baslat.");
    process.exit(1);
  }

  const giris = await istek("/api/oturum/misafir", "POST", {
    cihazId: "dokum-" + Date.now(),
    ad: "Ada",
    renk: 0,
  });
  if (giris.durum !== 200) {
    console.error("Misafir giris basarisiz: " + giris.durum);
    process.exit(1);
  }
  const jwt = giris.veri.erisimJetonu;

  const vadi = await istek("/api/vadi", "POST", {}, jwt);
  if (vadi.durum !== 200) {
    console.error("Vadi kurulamadi: " + vadi.durum);
    process.exit(1);
  }
  console.log("Vadi kuruldu, revision " + vadi.veri.revision);

  const a = new Istemci();
  await a.bagla(jwt);
  const tam = await a.bekle("SENKRON_TAM");
  const d = tam.degisim;
  console.log(
    "Baslangic: " + d.karolar.length + " karo, " +
    d.envanter.length + " envanter satiri, " +
    d.dugumler.length + " dugum"
  );

  // Kovayi doldur, sonra ek + sula. Ekilen ama sulanmamis karo buyumez;
  // ekranda "sulanmis toprak" tonunu gorebilmek icin sulama sart.
  await niyet(a, "kova_doldur", null, null);

  const karolar = d.karolar.slice(0, 3);
  for (const k of karolar) {
    const hedef = { bolge: k.bolge || "tarla", x: k.x, y: k.y };
    await niyet(a, "ek", hedef, { urunId: "bugday" });
    await niyet(a, "sula", hedef, null);
  }

  // Yabani kaynaklar: envantere odun/tas gibi farkli kategoriler girsin ki
  // canta ekraninda tek kategori degil, gercek bir karisim gorunsun.
  for (const g of d.dugumler) {
    await niyet(a, "topla", { bolge: g.bolge || "tarla", x: g.x, y: g.y }, null);
  }

  a.kapat();

  // Yeniden baglanip TAM anlik goruntu al: birikmis deltalari elle birlestirmek
  // yerine sunucunun kendi ozetini kullan.
  const b = new Istemci();
  await b.bagla(jwt);
  const son = await b.bekle("SENKRON_TAM");
  b.kapat();

  const s = son.degisim;
  console.log(
    "Dokum: " + s.karolar.length + " karo, " +
    s.envanter.length + " envanter satiri, " +
    (s.depo ? s.depo.length : 0) + " depo yigini, revision " + s.revision
  );

  require("fs").writeFileSync(CIKTI, JSON.stringify(s, null, 2), "utf8");
  console.log("Yazildi: " + CIKTI);
  process.exit(0);
}

main().catch((e) => {
  console.error("HATA: " + e.message);
  process.exit(1);
});
