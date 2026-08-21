using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Amorland.Ag;
using Amorland.Durum;
using Amorland.Gorsel;

namespace Amorland.Arayuz
{
    /// <summary>
    /// Kalici HUD (B.20): seviye+XP · altin (Sv 5+) · canta · tohum secici (4 slot) ·
    /// harita · sohbet · partner gostergesi.
    ///
    /// ⚠ Ekrandaki 4 slotluk cubuk bir <b>ALET cubugu degil</b>, tohum/sarf secicisidir (B.6).
    /// Aletler envanterde durur ve baglama gore otomatik secilir.
    ///
    /// ⚠ Altin Sv 5'e kadar HUD'da <b>hic gorunmez</b> (B.15): Perde 1'de para yoktur ve
    /// gostergesinin bile olmamasi tren kapanisinin anlamini korur.
    ///
    /// ⚠ Kabuk B.22 arayuz stil sozlesmesine uyar: HUD ogeleri KOYU KAPSUL + ACIK KREM
    /// CERCEVE, tohum cubugu PARSOMEN + KALIN KAHVE CERCEVE, secili slot ALTIN cerceve.
    /// Cercevesiz koyu panel zengin dunyanin uzerinde kaybolur — okunurlugu saglayan sey
    /// kapsulun kendisi degil, etrafindaki acik hattir.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        private DunyaDurumu _durum;
        private DengeVerisi _denge;

        private Text _seviyeYazi;
        private Image _xpDolgu;
        private Text _xpYazi;
        private Text _altinYazi;
        private RectTransform _altinKutu;
        private Text _cantaYazi;
        private Text _kovaYazi;
        private Text _partnerYazi;
        private Image _partnerRozet;
        private Text _depoYazi;
        private Image _avatar;

        private readonly List<TohumSlotu> _tohumSlotlari = new List<TohumSlotu>();
        private RectTransform _tohumCubugu;

        public event Action<string> TohumSecildi;
        public event Action CantaAcildi;
        public event Action HaritaAcildi;
        public event Action DefterAcildi;
        public event Action AyarlarAcildi;
        public event Action SohbetAcildi;
        public event Action PartnerTiklandi;

        private class TohumSlotu
        {
            public GameObject Kok;

            /// <summary>Dis cerceve — secili slotta altina doner.</summary>
            public Image Cerceve;

            /// <summary>Ic yuzey — bos slotta cukurlasir.</summary>
            public Image Yuzey;

            public Image Ikon;
            public GameObject AdetRozeti;
            public Text Ad;
            public Text Adet;
            public string EsyaId;
        }

        private const float XpGenislik = 186f;
        private const int TohumSlotSayisi = 4;
        private int _sonXp = -1;

        public string SeciliTohum { get; private set; }

        public void Kur(Transform kanvas, DunyaDurumu durum, DengeVerisi denge)
        {
            _durum = durum;
            _denge = denge;

            var kok = UiKit.Panel(kanvas, "HUD", Color.clear);
            UiKit.Doldur(kok);

            OyuncuKapsuluKur(kok);
            AltinKapsuluKur(kok);
            DepoKapsuluKur(kok);
            TohumCubuguKur(kok);
            MenuKur(kok);

            _durum.DurumDegisti += _ => Tazele();
            Tazele();
        }

        // ------------------------------------------------------------------ bolumler

        /// <summary>Sol ust: avatar halkasi + ad + seviye + XP. HUD'un ana kapsulu.</summary>
        private void OyuncuKapsuluKur(RectTransform kok)
        {
            RectTransform ic;
            var kapsul = UiKit.CerceveliKart(
                kok, "Oyuncu", Palet.KapsulZemin, Palet.KapsulCerceve, out ic, 26, 3f);
            UiKit.Sabitle(kapsul, 0f, 1f, 14f, -12f, 296f, 84f);
            UiKit.GolgeEkle(kapsul, 26, 14, 4f);

            // Kapsul tiklanir: partner yokken davet, varken mesaj panelinin kapisi
            // (denetim: davet ekrani hicbir yerden acilamiyordu — olu koddu).
            var kapsulImg = kapsul.gameObject.GetComponent<Image>();
            kapsulImg.raycastTarget = true;
            var kapsulButon = kapsul.gameObject.AddComponent<Button>();
            kapsulButon.targetGraphic = kapsulImg;
            kapsulButon.transition = Selectable.Transition.None;
            kapsulButon.onClick.AddListener(() => PartnerTiklandi?.Invoke());

            _partnerYazi = UiKit.KonturluYazi(
                ic, "Ad", "", UiKit.YaziGovde, Color.white, TextAnchor.UpperLeft, true, 1.4f);
            UiKit.Sabitle(_partnerYazi.rectTransform, 0f, 1f, 96f, -8f, 158f, 24f);

            _partnerRozet = UiKit.Rozet(ic, Palet.UiYaziSolgun, 18f);
            _partnerRozet.sprite = Sekil.Kalp();
            UiKit.Sabitle(_partnerRozet.rectTransform, 0f, 1f, 258f, -9f, 18f, 18f);

            var yildiz = UiKit.Panel(ic, "Yildiz", Palet.KapsulAltin);
            yildiz.gameObject.GetComponent<Image>().sprite = Sekil.Yildiz();
            UiKit.Sabitle(yildiz, 0f, 1f, 96f, -34f, 18f, 18f);

            _seviyeYazi = UiKit.Yazi(
                ic, "Seviye", "Seviye 1", UiKit.YaziKucuk, Palet.KapsulAltin, TextAnchor.UpperLeft, true);
            UiKit.Sabitle(_seviyeYazi.rectTransform, 0f, 1f, 120f, -34f, 140f, 20f);

            RectTransform xpIc;
            var xp = UiKit.CerceveliKart(
                ic, "Xp", new Color(0f, 0f, 0f, 0.55f), Palet.KapsulCerceve, out xpIc, 10, 2f);
            UiKit.Sabitle(xp, 0f, 1f, 96f, -58f, XpGenislik, 18f);

            var dolgu = UiKit.Kart(xpIc, "Dolgu", Palet.UiAltin, 7);
            dolgu.anchorMin = Vector2.zero;
            dolgu.anchorMax = new Vector2(0f, 1f);
            dolgu.pivot = new Vector2(0f, 0.5f);
            dolgu.offsetMin = Vector2.zero;
            dolgu.offsetMax = Vector2.zero;
            _xpDolgu = dolgu.gameObject.GetComponent<Image>();
            _xpDolgu.raycastTarget = false;

            _xpYazi = UiKit.KonturluYazi(
                xpIc, "XpYazi", "", UiKit.YaziMini, Color.white, TextAnchor.MiddleCenter, true, 1.2f);
            UiKit.Doldur(_xpYazi.rectTransform);

            // Avatar halkasi kapsulun disina tasar — referansta da oyle duruyor.
            var halka = UiKit.Panel(kok, "AvatarHalka", Palet.KapsulCerceve);
            halka.gameObject.GetComponent<Image>().sprite = Sekil.Daire();
            UiKit.Sabitle(halka, 0f, 1f, 10f, -8f, 92f, 92f);
            var avatarRt = UiKit.Panel(halka, "Avatar", Palet.UiYaziSolgun);
            avatarRt.gameObject.GetComponent<Image>().sprite = Sekil.Daire();
            UiKit.Doldur(avatarRt, 5f);
            _avatar = avatarRt.gameObject.GetComponent<Image>();
        }

        /// <summary>Ust orta: altin. Sv 5'e kadar GIZLI (B.15).</summary>
        private void AltinKapsuluKur(RectTransform kok)
        {
            // ⚠ Kapsul kendi GORUNMEZ kabugunun icinde durur ve gizlenen sey kabuktur.
            // GolgeEkle golgeyi KARDES olarak birakir (cocuk olsaydi kapsulun ustune
            // cizilirdi); kapsulu tek basina kapatmak golgeyi ekranda birakiyordu —
            // Sv 5'e kadar ust ortada asili duran kara leke tam olarak buydu.
            // Bildirimler'deki "her serit kendi kabugunda" cozumunun aynisi.
            var kabuk = UiKit.Panel(kok, "AltinKabuk", Color.clear);
            UiKit.Sabitle(kabuk, 0.5f, 1f, 0f, -14f, 150f, 46f);
            _altinKutu = kabuk;

            RectTransform ic;
            var kapsul = UiKit.CerceveliKart(
                kabuk, "Altin", Palet.KapsulZemin, Palet.KapsulCerceve, out ic, 20, 3f);
            UiKit.Doldur(kapsul);
            UiKit.GolgeEkle(kapsul, 20, 10, 3f);

            var sikke = UiKit.Panel(ic, "Sikke", Palet.UiAltin);
            sikke.gameObject.GetComponent<Image>().sprite = Sekil.Daire();
            UiKit.Sabitle(sikke, 0f, 0.5f, 8f, 0f, 26f, 26f);

            _altinYazi = UiKit.KonturluYazi(
                ic, "Deger", "0", UiKit.YaziGovde, Color.white, TextAnchor.MiddleLeft, true, 1.3f);
            UiKit.Sabitle(_altinYazi.rectTransform, 0f, 0.5f, 42f, 0f, 94f, 26f);

            _altinKutu.gameObject.SetActive(false);
        }

        /// <summary>Sag ust: depo dolulugu.</summary>
        private void DepoKapsuluKur(RectTransform kok)
        {
            RectTransform ic;
            var kapsul = UiKit.CerceveliKart(
                kok, "Depo", Palet.KapsulZemin, Palet.KapsulCerceve, out ic, 20, 3f);
            UiKit.Sabitle(kapsul, 1f, 1f, -14f, -14f, 196f, 46f);
            UiKit.GolgeEkle(kapsul, 20, 10, 3f);

            var ikon = UiKit.Panel(ic, "Ikon", Palet.KapsulCerceve);
            ikon.gameObject.GetComponent<Image>().sprite = Sekil.Elmas();
            UiKit.Sabitle(ikon, 0f, 0.5f, 8f, 0f, 24f, 24f);

            _depoYazi = UiKit.KonturluYazi(
                ic, "Deger", "", UiKit.YaziKucuk, Color.white, TextAnchor.MiddleLeft, true, 1.2f);
            UiKit.Sabitle(_depoYazi.rectTransform, 0f, 0.5f, 38f, 0f, 146f, 24f);
        }

        /// <summary>Alt orta: parsomen tohum cubugu. Secili slot ALTIN cerceve alir.</summary>
        private void TohumCubuguKur(RectTransform kok)
        {
            RectTransform ic;
            var cubuk = UiKit.CerceveliKart(
                kok, "TohumCubugu", Palet.Parsomen, Palet.UiCerceve, out ic, 20, 4f);
            UiKit.Sabitle(cubuk, 0.5f, 0f, 0f, 14f, 4 * 90f + 12f, 104f);
            UiKit.GolgeEkle(cubuk, 20, 16, 5f);
            _tohumCubugu = cubuk;

            for (int i = 0; i < TohumSlotSayisi; i++)
            {
                int indeks = i;
                RectTransform slotIc;
                var slot = UiKit.CerceveliKart(
                    ic, "Slot" + i, Palet.ParsomenKoyu, Palet.UiCerceve, out slotIc, 14, 3f);
                UiKit.Sabitle(slot, 0f, 1f, 6f + i * 90f, -6f, 84f, 62f);

                var disImg = slot.gameObject.GetComponent<Image>();
                disImg.raycastTarget = true;
                var buton = slot.gameObject.AddComponent<Button>();
                buton.targetGraphic = disImg;
                buton.onClick.AddListener(() => SlotSec(indeks));

                var ikonRt = UiKit.Panel(slotIc, "Ikon", Palet.Yaprak);
                ikonRt.gameObject.GetComponent<Image>().sprite = Sekil.Damla();
                UiKit.Sabitle(ikonRt, 0.5f, 0.5f, 0f, 0f, 32f, 32f);

                // Adet rozeti: koyu daire — referanstaki sayac rozetiyle ayni dil.
                var rozet = UiKit.Panel(slotIc, "AdetRozet", Palet.KapsulZemin);
                rozet.gameObject.GetComponent<Image>().sprite = Sekil.Daire();
                // Ic yuzeyin kosesinde, cerceveye binmeden (UI denetimi: tasiyordu).
                UiKit.Sabitle(rozet, 1f, 0f, -4f, 4f, 24f, 24f);
                var adet = UiKit.Yazi(
                    rozet, "Adet", "", UiKit.YaziMini, Color.white, TextAnchor.MiddleCenter, true);
                UiKit.Doldur(adet.rectTransform);

                // Ad slotun DISINDA, cubugun alt seridinde durur: 84 px'lik slota hem
                // ikon hem ad hem sayac sigmiyor ve yazi ikonun uzerine biniyordu.
                var ad = UiKit.Yazi(
                    ic, "Ad" + i, "", UiKit.YaziMini, Palet.UiKoyuYazi, TextAnchor.UpperCenter, true);
                UiKit.Sabitle(ad.rectTransform, 0f, 1f, 6f + i * 90f, -70f, 84f, 20f);

                _tohumSlotlari.Add(new TohumSlotu
                {
                    Kok = slot.gameObject,
                    Cerceve = disImg,
                    Yuzey = slotIc.gameObject.GetComponent<Image>(),
                    Ikon = ikonRt.gameObject.GetComponent<Image>(),
                    AdetRozeti = rozet.gameObject,
                    Ad = ad,
                    Adet = adet,
                });
            }
        }

        /// <summary>Sag alt: menu butonlari + canta/kova sayaclari.</summary>
        private void MenuKur(RectTransform kok)
        {
            var dugmeler = new[]
            {
                new object[] { "Sohbet", "Mesaj", (Action)(() => SohbetAcildi?.Invoke()) },
                new object[] { "Canta", "Çanta", (Action)(() => CantaAcildi?.Invoke()) },
                new object[] { "Harita", "Harita", (Action)(() => HaritaAcildi?.Invoke()) },
                new object[] { "Defter", "Defter", (Action)(() => DefterAcildi?.Invoke()) },
                new object[] { "Ayarlar", "Ayar", (Action)(() => AyarlarAcildi?.Invoke()) },
            };

            for (int i = 0; i < dugmeler.Length; i++)
            {
                var b = UiKit.AhsapButon(
                    kok, (string)dugmeler[i][0], (string)dugmeler[i][1], (Action)dugmeler[i][2]);
                var rt = b.GetComponent<RectTransform>();
                // 80 px buton + 86 px adim: 5. dugme (Mesaj) gelince genislik
                // daraldi — tohum cubuguyla cakismadan 16 px pay kalir.
                UiKit.Sabitle(rt, 1f, 0f, -14f - (dugmeler.Length - 1 - i) * 86f, 14f, 80f, 62f);
                b.GetComponentInChildren<Text>().fontSize = UiKit.YaziKucuk;
            }

            // ⚠ Sayaclar kendi kapsulunde: ciplak yazi cim uzerinde okunmuyordu.
            RectTransform sayacIc;
            var sayac = UiKit.CerceveliKart(
                kok, "Sayaclar", Palet.KapsulZemin, Palet.KapsulCerceve, out sayacIc, 16, 3f);
            UiKit.Sabitle(sayac, 1f, 0f, -14f, 84f, 246f, 40f);

            _cantaYazi = UiKit.KonturluYazi(
                sayacIc, "Canta", "", UiKit.YaziMini, Color.white, TextAnchor.MiddleLeft, true, 1.2f);
            UiKit.Sabitle(_cantaYazi.rectTransform, 0f, 0.5f, 10f, 0f, 112f, 20f);

            _kovaYazi = UiKit.KonturluYazi(
                sayacIc, "Kova", "", UiKit.YaziMini, Palet.Gokyuzu, TextAnchor.MiddleRight, true, 1.2f);
            UiKit.Sabitle(_kovaYazi.rectTransform, 1f, 0.5f, -10f, 0f, 112f, 20f);
        }

        // ------------------------------------------------------------------ tazeleme

        public void Tazele()
        {
            if (_durum == null) return;
            var ben = _durum.Ben;
            if (ben == null)
            {
                // ⚠ Iskelet doldurma (UI denetimi): sunucu durumu gelmeden bos kapsul
                // "kirik/yuklenememis" okunuyordu. Veri yokken de her gosterge makul
                // bir varsayilanla dolu durur; ilk gercek Tazele hepsini ezer.
                IskeletDoldur();
                return;
            }

            _seviyeYazi.text = "Seviye " + ben.Seviye;
            // XP kazanci ucan "+N" ile kutlanir: bar sessizce dolunca odul hissi
            // kayboluyordu (B.22 ailesi: her kazancin gorunur ani olur).
            if (_sonXp >= 0 && ben.Xp > _sonXp) XpUcur(ben.Xp - _sonXp);
            _sonXp = ben.Xp;
            float oran = ben.SonrakiSeviyeXp > 0 ? ben.Xp / (float)ben.SonrakiSeviyeXp : 1f;
            UiKit.CubukAyarla(_xpDolgu, oran, XpGenislik - 4f);
            _xpYazi.text = ben.SonrakiSeviyeXp > 0 ? ben.Xp + " / " + ben.SonrakiSeviyeXp : "";
            _avatar.color = _denge.OyuncuRengi(ben.Renk);

            // Altin: -1 ise sunucu "henuz gorunmez" diyor (B.15).
            bool altinVar = ben.AltinGorunur;
            _altinKutu.gameObject.SetActive(altinVar);
            if (altinVar) _altinYazi.text = ben.Altin.ToString();

            _cantaYazi.text = "Çanta " + BenimSlotSayisi() + "/" + ben.CantaSlot;
            _kovaYazi.text = ben.KovaKapasite > 0
                ? "Kova " + ben.KovaKalan + "/" + ben.KovaKapasite
                : "Kova yok";

            PartnerTazele();
            DepoTazele();
            TohumCubugunuTazele();
        }

        private void IskeletDoldur()
        {
            _seviyeYazi.text = "Seviye 1";
            UiKit.CubukAyarla(_xpDolgu, 0f, XpGenislik - 4f);
            _xpYazi.text = "0 / 100";
            _avatar.color = new Color(0.93f, 0.72f, 0.52f);
            _partnerYazi.text = "Vadi çifti";
            _partnerRozet.color = new Color(
                Palet.UiKirmizi.r, Palet.UiKirmizi.g, Palet.UiKirmizi.b, 0.45f);
            _cantaYazi.text = "Çanta 0/8";
            _kovaYazi.text = "Kova 0/2";
            _depoYazi.text = "Depo 0/3";
            _depoYazi.color = Color.white;
            TohumCubugunuTazele();
        }

        private void XpUcur(int kazanc)
        {
            var yazi = UiKit.KonturluYazi(
                _xpYazi.transform.parent, "XpKazanc", "+" + kazanc + " XP",
                UiKit.YaziKucuk, Palet.UiAltin, TextAnchor.MiddleCenter, true, 1.4f);
            var rt = yazi.rectTransform;
            UiKit.Sabitle(rt, 0.5f, 0.5f, 0f, 0f, 120f, 24f);
            StartCoroutine(XpYaziUcur(yazi));
        }

        private System.Collections.IEnumerator XpYaziUcur(UnityEngine.UI.Text yazi)
        {
            float t = 0f;
            var rt = yazi.rectTransform;
            Color c = yazi.color;
            while (t < 1f && yazi != null)
            {
                t += Time.deltaTime;
                rt.anchoredPosition = new Vector2(0f, -6f - 30f * t);
                c.a = 1f - t * t;
                yazi.color = c;
                yield return null;
            }
            if (yazi != null) Destroy(yazi.gameObject);
        }

        private int BenimSlotSayisi()
        {
            int yigin = _denge.YiginBoyutu;
            int toplam = 0;
            foreach (var e in _durum.BenimEnvanterim)
            {
                toplam += Mathf.CeilToInt(e.Adet / (float)yigin);
            }
            return toplam;
        }

        private void PartnerTazele()
        {
            var ben = _durum.Ben;
            var partner = _durum.Partner;
            string benimAdim = ben != null ? ben.Ad : "";

            if (partner == null)
            {
                // Tek imza: kalp soluk kalir. "Ikinci imza icin bir yer her zaman
                // ayrilmistir" (B.21) — gosterge kaybolmaz, sadece sonuk durur.
                _partnerYazi.text = benimAdim;
                // Kalp gri degil KIRMIZI-soluk: gosterge bos degil, "yer ayrilmis" (B.21).
                _partnerRozet.color = new Color(
                    Palet.UiKirmizi.r, Palet.UiKirmizi.g, Palet.UiKirmizi.b, 0.45f);
                return;
            }

            bool kus = _durum.Vadi != null && _durum.Vadi.KuslukAktif;

            // ⚠ Kusluk yalnizca bir GOSTERGEDIR (B.8). Ekonomik etkisi yoktur ve
            // bildirim gondermez. Rol olmaktan cikip yaptirim olursa kirmizi cizgi asilir.
            _partnerYazi.text = benimAdim + " & " + partner.Ad;
            _partnerRozet.color = kus ? new Color(1f, 1f, 1f, 0.35f)
                : partner.Cevrimici ? Palet.UiKirmizi
                : new Color(Palet.UiKirmizi.r, Palet.UiKirmizi.g, Palet.UiKirmizi.b, 0.5f);
        }

        private void DepoTazele()
        {
            var vadi = _durum.Vadi;
            if (vadi == null || vadi.DepoKapasitesi <= 0)
            {
                _depoYazi.text = "Depo kapalı";
                _depoYazi.color = new Color(1f, 1f, 1f, 0.55f);
                return;
            }
            _depoYazi.text = "Depo " + vadi.DepoDolu + "/" + vadi.DepoKapasitesi;
            // ⚠ Depo dolulugu SESSIZ olamaz: hasat engellenmesinin gorsel karsiligi budur.
            float doluluk = vadi.DepoDolu / (float)vadi.DepoKapasitesi;
            _depoYazi.color = doluluk >= 1f ? Palet.UiKirmizi
                : doluluk > 0.85f ? Palet.UiAltin
                : Color.white;
        }

        private void TohumCubugunuTazele()
        {
            var tohumlar = new List<EnvanterSatiri>();
            foreach (var e in _durum.BenimEnvanterim)
            {
                if (_denge.EsyaKategorisi(e.EsyaId) == "tohum") tohumlar.Add(e);
            }

            for (int i = 0; i < _tohumSlotlari.Count; i++)
            {
                var slot = _tohumSlotlari[i];
                bool dolu = i < tohumlar.Count;

                if (dolu)
                {
                    slot.EsyaId = tohumlar[i].EsyaId;
                    var urun = _denge.TohumdanUrun(slot.EsyaId);
                    slot.Ad.text = urun != null ? urun.Ad : _denge.EsyaAdi(slot.EsyaId);
                    slot.Adet.text = tohumlar[i].Adet.ToString();
                }
                else
                {
                    slot.EsyaId = null;
                    slot.Ad.text = "";
                    slot.Adet.text = "";
                }

                slot.Ikon.enabled = dolu;
                slot.AdetRozeti.SetActive(dolu);

                bool secili = slot.EsyaId != null && slot.EsyaId == SeciliTohum;
                // Secim ALTIN cerceveyle isaretlenir (B.22 arayuz sozlesmesi).
                slot.Cerceve.color = secili ? Palet.KapsulAltin : Palet.UiCerceve;
                slot.Yuzey.color = dolu ? Palet.ParsomenKoyu : Palet.UiKagitCukur;
            }

            // Secili tohum bittiyse secim dusurulur; oyuncu bos slota basmis gibi olmasin.
            if (SeciliTohum != null && _durum.EnvanterAdet(SeciliTohum) <= 0)
            {
                SeciliTohum = null;
                TohumSecildi?.Invoke(null);
            }
            // Hicbir sey secili degilse ilk tohumu sec: ogreticide "neden ekmiyor" sorusu cikmasin.
            if (SeciliTohum == null && tohumlar.Count > 0)
            {
                SeciliTohum = tohumlar[0].EsyaId;
                TohumSecildi?.Invoke(SeciliTohum);
            }
        }

        private void SlotSec(int indeks)
        {
            if (indeks < 0 || indeks >= _tohumSlotlari.Count) return;
            string esyaId = _tohumSlotlari[indeks].EsyaId;
            if (esyaId == null) return;
            SeciliTohum = esyaId;
            TohumSecildi?.Invoke(esyaId);
            Tazele();
        }
    }
}
