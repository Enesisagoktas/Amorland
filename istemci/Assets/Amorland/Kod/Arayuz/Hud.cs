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
    /// </summary>
    public class Hud : MonoBehaviour
    {
        private DunyaDurumu _durum;
        private DengeVerisi _denge;

        private Text _seviyeYazi;
        private Image _xpDolgu;
        private RectTransform _xpArka;
        private Text _altinYazi;
        private RectTransform _altinKutu;
        private Text _cantaYazi;
        private Text _kovaYazi;
        private Text _partnerYazi;
        private Image _partnerRozet;
        private Text _depoYazi;

        private readonly List<TohumSlotu> _tohumSlotlari = new List<TohumSlotu>();
        private RectTransform _tohumCubugu;

        public event Action<string> TohumSecildi;
        public event Action CantaAcildi;
        public event Action HaritaAcildi;
        public event Action DefterAcildi;
        public event Action AyarlarAcildi;
        public event Action SohbetAcildi;

        private class TohumSlotu
        {
            public GameObject Kok;
            public Image Cerceve;
            public Text Ad;
            public Text Adet;
            public string EsyaId;
        }

        private const float XpGenislik = 180f;
        private const int TohumSlotSayisi = 4;

        public string SeciliTohum { get; private set; }

        public void Kur(Transform kanvas, DunyaDurumu durum, DengeVerisi denge)
        {
            _durum = durum;
            _denge = denge;

            var kok = UiKit.Panel(kanvas, "HUD", Color.clear);
            UiKit.Doldur(kok);

            SolUstKur(kok);
            SagUstKur(kok);
            AltCubukKur(kok);
            SagAltKur(kok);

            _durum.DurumDegisti += _ => Tazele();
            Tazele();
        }

        // ------------------------------------------------------------------ bolumler

        private void SolUstKur(RectTransform kok)
        {
            var panel = UiKit.Panel(kok, "SolUst", Palet.UiZemin);
            UiKit.Sabitle(panel, 0f, 1f, 12f, -12f, 260f, 62f);

            _seviyeYazi = UiKit.Yazi(panel, "Seviye", "Sv 1", 20, Palet.UiYazi, TextAnchor.UpperLeft);
            UiKit.Sabitle(_seviyeYazi.rectTransform, 0f, 1f, 10f, -8f, 100f, 24f);

            var arka = UiKit.Cubuk(panel, "XpCubuk", new Color(1f, 1f, 1f, 0.12f), Palet.UiVurgu, out _xpDolgu);
            _xpArka = arka.rectTransform;
            UiKit.Sabitle(_xpArka, 0f, 1f, 10f, -36f, XpGenislik, 10f);

            // Altin kutusu Sv 5'e kadar GIZLI kalir (B.15).
            _altinKutu = UiKit.Panel(panel, "Altin", Color.clear);
            UiKit.Sabitle(_altinKutu, 1f, 1f, -10f, -8f, 90f, 24f);
            _altinYazi = UiKit.Yazi(_altinKutu, "AltinYazi", "0", 18, Palet.UiVurgu, TextAnchor.UpperRight);
            UiKit.Doldur(_altinYazi.rectTransform);
            _altinKutu.gameObject.SetActive(false);
        }

        private void SagUstKur(RectTransform kok)
        {
            var panel = UiKit.Panel(kok, "SagUst", Palet.UiZemin);
            UiKit.Sabitle(panel, 1f, 1f, -12f, -12f, 250f, 62f);

            _partnerRozet = UiKit.Rozet(panel, Palet.UiYaziSolgun, 16f);
            UiKit.Sabitle(_partnerRozet.rectTransform, 0f, 1f, 10f, -10f, 16f, 16f);

            _partnerYazi = UiKit.Yazi(panel, "Partner", "Tek imza", 16, Palet.UiYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Sabitle(_partnerYazi.rectTransform, 0f, 1f, 34f, -8f, 200f, 22f);

            _depoYazi = UiKit.Yazi(panel, "Depo", "", 14, Palet.UiYaziSolgun, TextAnchor.UpperLeft);
            UiKit.Sabitle(_depoYazi.rectTransform, 0f, 1f, 10f, -34f, 220f, 20f);
        }

        private void AltCubukKur(RectTransform kok)
        {
            _tohumCubugu = UiKit.Panel(kok, "TohumCubugu", Palet.UiZemin);
            UiKit.Sabitle(_tohumCubugu, 0.5f, 0f, 0f, 12f, 4 * 92f + 20f, 88f);

            for (int i = 0; i < TohumSlotSayisi; i++)
            {
                int indeks = i;
                var slotRt = UiKit.Panel(_tohumCubugu, "Slot" + i, Palet.UiPanel);
                UiKit.Sabitle(slotRt, 0f, 0.5f, 10f + i * 92f, 0f, 84f, 76f);
                slotRt.pivot = new Vector2(0f, 0.5f);

                var buton = slotRt.gameObject.AddComponent<Button>();
                buton.targetGraphic = slotRt.gameObject.GetComponent<Image>();
                buton.onClick.AddListener(() => SlotSec(indeks));

                var ad = UiKit.Yazi(slotRt, "Ad", "", 14, Palet.UiYazi, TextAnchor.UpperCenter);
                UiKit.Sabitle(ad.rectTransform, 0.5f, 1f, 0f, -6f, 78f, 34f);

                var adet = UiKit.Yazi(slotRt, "Adet", "", 16, Palet.UiVurgu, TextAnchor.LowerRight);
                UiKit.Sabitle(adet.rectTransform, 1f, 0f, -6f, 4f, 40f, 20f);

                var cerceveRt = UiKit.Panel(slotRt, "Cerceve", Color.clear);
                UiKit.Doldur(cerceveRt);
                var cerceve = cerceveRt.gameObject.GetComponent<Image>();
                cerceve.raycastTarget = false;

                _tohumSlotlari.Add(
                    new TohumSlotu { Kok = slotRt.gameObject, Cerceve = cerceve, Ad = ad, Adet = adet });
            }
        }

        private void SagAltKur(RectTransform kok)
        {
            var panel = UiKit.Panel(kok, "SagAlt", Color.clear);
            UiKit.Sabitle(panel, 1f, 0f, -12f, 12f, 320f, 168f);
            UiKit.YatayDizi(panel, 8f, 0);

            void Ekle(string ad, string metin, Action tik)
            {
                var b = UiKit.Buton(panel, ad, metin, tik);
                UiKit.Boyut(b.gameObject, 72f, 56f);
            }

            Ekle("Canta", "Canta", () => CantaAcildi?.Invoke());
            Ekle("Harita", "Harita", () => HaritaAcildi?.Invoke());
            Ekle("Defter", "Defter", () => DefterAcildi?.Invoke());
            Ekle("Ayarlar", "Ayar", () => AyarlarAcildi?.Invoke());

            _cantaYazi = UiKit.Yazi(kok, "CantaSayaci", "", 14, Palet.UiYaziSolgun, TextAnchor.LowerRight);
            UiKit.Sabitle(_cantaYazi.rectTransform, 1f, 0f, -12f, 186f, 200f, 20f);

            _kovaYazi = UiKit.Yazi(kok, "KovaSayaci", "", 14, Palet.Su, TextAnchor.LowerRight);
            UiKit.Sabitle(_kovaYazi.rectTransform, 1f, 0f, -12f, 208f, 200f, 20f);
        }

        // ------------------------------------------------------------------ tazeleme

        public void Tazele()
        {
            if (_durum == null) return;
            var ben = _durum.Ben;
            if (ben == null) return;

            _seviyeYazi.text = "Sv " + ben.Seviye;
            float oran = ben.SonrakiSeviyeXp > 0 ? ben.Xp / (float)ben.SonrakiSeviyeXp : 1f;
            UiKit.CubukAyarla(_xpDolgu, oran, XpGenislik);

            // Altin: -1 ise sunucu "henuz gorunmez" diyor (B.15).
            bool altinVar = ben.AltinGorunur;
            _altinKutu.gameObject.SetActive(altinVar);
            if (altinVar) _altinYazi.text = ben.Altin.ToString();

            _cantaYazi.text = "Canta " + BenimSlotSayisi() + "/" + ben.CantaSlot;
            _kovaYazi.text = ben.KovaKapasite > 0
                ? "Kova " + ben.KovaKalan + "/" + ben.KovaKapasite
                : "Kova yok";

            PartnerTazele();
            DepoTazele();
            TohumCubugunuTazele();
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
            var partner = _durum.Partner;
            if (partner == null)
            {
                _partnerYazi.text = "Tek imza";
                _partnerYazi.color = Palet.UiYaziSolgun;
                _partnerRozet.color = Palet.UiYaziSolgun;
                return;
            }

            _partnerRozet.color = _denge.OyuncuRengi(partner.Renk);
            bool kus = _durum.Vadi != null && _durum.Vadi.KuslukAktif;

            // ⚠ Kusluk yalnizca bir GOSTERGEDIR (B.8). Ekonomik etkisi yoktur ve
            // bildirim gondermez. Rol olmaktan cikip yaptirim olursa kirmizi cizgi asilir.
            string durumMetni = kus ? " (kus)" : (partner.Cevrimici ? " · cevrimici" : " · cevrimdisi");
            _partnerYazi.text = partner.Ad + durumMetni;
            _partnerYazi.color = partner.Cevrimici ? Palet.UiYazi : Palet.UiYaziSolgun;
        }

        private void DepoTazele()
        {
            var vadi = _durum.Vadi;
            if (vadi == null || vadi.DepoKapasitesi <= 0)
            {
                _depoYazi.text = "";
                return;
            }
            _depoYazi.text = "Depo " + vadi.DepoDolu + "/" + vadi.DepoKapasitesi;
            // ⚠ Depo dolulugu SESSIZ olamaz: hasat engellenmesinin gorsel karsiligi budur.
            float doluluk = vadi.DepoDolu / (float)vadi.DepoKapasitesi;
            _depoYazi.color = doluluk >= 1f ? Palet.UiUyari
                : doluluk > 0.85f ? Palet.UiVurgu
                : Palet.UiYaziSolgun;
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
                if (i < tohumlar.Count)
                {
                    slot.EsyaId = tohumlar[i].EsyaId;
                    var urun = _denge.TohumdanUrun(slot.EsyaId);
                    slot.Ad.text = urun != null ? urun.Ad : _denge.EsyaAdi(slot.EsyaId);
                    slot.Adet.text = tohumlar[i].Adet.ToString();
                    slot.Kok.SetActive(true);
                }
                else
                {
                    slot.EsyaId = null;
                    slot.Ad.text = "";
                    slot.Adet.text = "";
                    slot.Kok.SetActive(true);
                }
                bool secili = slot.EsyaId != null && slot.EsyaId == SeciliTohum;
                slot.Cerceve.color = secili ? new Color(Palet.UiVurgu.r, Palet.UiVurgu.g, Palet.UiVurgu.b, 0.28f)
                                            : Color.clear;
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
