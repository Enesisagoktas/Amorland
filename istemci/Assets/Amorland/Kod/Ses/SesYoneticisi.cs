using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Amorland.Ses
{
    /// <summary>
    /// Ses altyapisi (B.23).
    ///
    /// ⚠ B.26 yasak 19: <b>ses altyapisi dikey dilimden sonraya birakilamaz</b> — sonra
    /// birakilirsa 92 sesin hepsi yeniden elden gecer. Bu yuzden havuz, varyasyon ve
    /// kanal siniri Perde 1'den ONCE, burada calisiyor.
    ///
    /// ⚠ B.26 yasak 18: <b>surukle-boyama sesi katleder.</b> 24 karo kuyruga alininca
    /// ayni ses 24 kez calar ve makineli tufege doner. Uc onlem birden zorunlu:
    /// (1) her tekrarda ±%8 perde varyasyonu, (2) her sik tekrar eden eylemin en az
    /// 3 kaydi, (3) etiket basina ayni anda en fazla 3 kanal.
    /// Kuyruk eylemlerinde ses ayrica %75'e alcalir.
    ///
    /// ⚠ B.26 yasak 16: <b>oyuncunun muzigini kesmek kozy oyunun 1 numarali kaldirilma
    /// sebebi.</b> iOS'ta "Mute Other Audio Sources" KAPALI olmali; bu bir Player Setting'dir
    /// ve <c>AmorlandKurulum</c> editor script'i ayarlar.
    /// </summary>
    public class SesYoneticisi : MonoBehaviour
    {
        private const int HavuzBoyutu = 16;          // B.23: ses basina Instantiate YOK
        private const int EtiketBasinaMaksKanal = 3; // B.23 esZamanlilik siniri
        private const float PerdeVaryasyonu = 0.08f; // ±%8
        private const int VaryasyonSayisi = 3;       // her sesin en az 3 kaydi
        private const float KuyrukSesSeviyesi = 0.75f;

        /// <summary>Editorde bir AudioMixer atanirsa kanallar ona baglanir; yoksa
        /// kanal seviyeleri dogrudan AudioSource.volume uzerinden uygulanir.</summary>
        public AudioMixer Mixer;

        private readonly List<AudioSource> _havuz = new List<AudioSource>();
        private readonly Dictionary<SesEtiketi, AudioClip[]> _klipler =
            new Dictionary<SesEtiketi, AudioClip[]>();
        private readonly Dictionary<SesEtiketi, List<float>> _aktifBitis =
            new Dictionary<SesEtiketi, List<float>>();
        private readonly Dictionary<SesKanali, float> _kanalSeviyeleri =
            new Dictionary<SesKanali, float>
            {
                { SesKanali.Muzik, 0.6f },
                { SesKanali.Ortam, 0.5f },
                { SesKanali.Eylem, 1.0f },
                { SesKanali.Ui, 0.8f }
            };

        private AudioSource _ortamKaynagi;
        private int _sonraki;
        private bool _sessiz;

        public bool Sessiz
        {
            get => _sessiz;
            set
            {
                _sessiz = value;
                AudioListener.volume = value ? 0f : 1f;
            }
        }

        public void Kur()
        {
            for (int i = 0; i < HavuzBoyutu; i++)
            {
                var go = new GameObject("Ses_" + i);
                go.transform.SetParent(transform, false);
                var kaynak = go.AddComponent<AudioSource>();
                kaynak.playOnAwake = false;
                kaynak.spatialBlend = 0f; // 2D: ortografik kamerada mesafe sesi anlamsiz
                _havuz.Add(kaynak);
            }

            var ortamGo = new GameObject("OrtamDongusu");
            ortamGo.transform.SetParent(transform, false);
            _ortamKaynagi = ortamGo.AddComponent<AudioSource>();
            _ortamKaynagi.playOnAwake = false;
            _ortamKaynagi.loop = true;
            _ortamKaynagi.spatialBlend = 0f;

            KlipleriHazirla();
        }

        private void KlipleriHazirla()
        {
            foreach (SesEtiketi etiket in System.Enum.GetValues(typeof(SesEtiketi)))
            {
                int adet = SikTekrarEder(etiket) ? VaryasyonSayisi : 1;
                var dizi = new AudioClip[adet];
                for (int v = 0; v < adet; v++) dizi[v] = YerTutucuSes.Uret(etiket, v);
                _klipler[etiket] = dizi;
                _aktifBitis[etiket] = new List<float>();
            }
        }

        /// <summary>
        /// Sik tekrar eden sesler: bunlarin en az 3 varyasyonu olmak ZORUNDA (B.23).
        /// Tek kayitla surukle-boyama makineli tufek sesi verir.
        /// </summary>
        private static bool SikTekrarEder(SesEtiketi etiket)
        {
            switch (etiket)
            {
                case SesEtiketi.Ekme:
                case SesEtiketi.Sulama:
                case SesEtiketi.Hasat:
                case SesEtiketi.Toplama:
                case SesEtiketi.Kazma:
                case SesEtiketi.KazmaTas:
                case SesEtiketi.Balta:
                case SesEtiketi.DugumKirilma:
                case SesEtiketi.Kova:
                case SesEtiketi.AyakToprak:
                case SesEtiketi.AyakCim:
                case SesEtiketi.AyakTahta:
                case SesEtiketi.AyakTas:
                case SesEtiketi.UiDokunma:
                    return true;
                default:
                    return false;
            }
        }

        // ------------------------------------------------------------------ calma

        public void Cal(SesEtiketi etiket, bool kuyruktaMi = false, float seviyeCarpani = 1f)
        {
            if (_sessiz) return;
            if (!_klipler.TryGetValue(etiket, out AudioClip[] varyasyonlar) || varyasyonlar.Length == 0)
                return;

            if (!KanalAcikMi(etiket)) return;

            AudioClip klip = varyasyonlar[Random.Range(0, varyasyonlar.Length)];
            AudioSource kaynak = SonrakiKaynak();

            kaynak.clip = klip;
            kaynak.pitch = 1f + Random.Range(-PerdeVaryasyonu, PerdeVaryasyonu);
            float kanal = _kanalSeviyeleri[KanalBul(etiket)];
            kaynak.volume = kanal * seviyeCarpani * (kuyruktaMi ? KuyrukSesSeviyesi : 1f);
            kaynak.Play();

            float bitis = Time.unscaledTime + (klip.length / Mathf.Max(0.01f, kaynak.pitch));
            _aktifBitis[etiket].Add(bitis);
        }

        /// <summary>Etiket basina en fazla 3 kanal (B.23 esZamanlilik siniri).</summary>
        private bool KanalAcikMi(SesEtiketi etiket)
        {
            List<float> liste = _aktifBitis[etiket];
            for (int i = liste.Count - 1; i >= 0; i--)
            {
                if (liste[i] <= Time.unscaledTime) liste.RemoveAt(i);
            }
            return liste.Count < EtiketBasinaMaksKanal;
        }

        private AudioSource SonrakiKaynak()
        {
            for (int i = 0; i < _havuz.Count; i++)
            {
                int indeks = (_sonraki + i) % _havuz.Count;
                if (!_havuz[indeks].isPlaying)
                {
                    _sonraki = (indeks + 1) % _havuz.Count;
                    return _havuz[indeks];
                }
            }
            // Hepsi mesgulse en eskisini kes: 16 kanal doluysa 17. ses zaten duyulmaz.
            AudioSource kaynak = _havuz[_sonraki];
            _sonraki = (_sonraki + 1) % _havuz.Count;
            return kaynak;
        }

        private static SesKanali KanalBul(SesEtiketi etiket)
        {
            switch (etiket)
            {
                case SesEtiketi.UiDokunma:
                case SesEtiketi.UiAcilis:
                case SesEtiketi.UiKapanis:
                case SesEtiketi.UiHata:
                case SesEtiketi.UiOnay:
                    return SesKanali.Ui;
                case SesEtiketi.OrtamTarla:
                case SesEtiketi.OrtamEv:
                    return SesKanali.Ortam;
                default:
                    return SesKanali.Eylem;
            }
        }

        public void OrtamBaslat(SesEtiketi etiket)
        {
            if (!_klipler.TryGetValue(etiket, out AudioClip[] k) || k.Length == 0) return;
            if (_ortamKaynagi.clip == k[0] && _ortamKaynagi.isPlaying) return;
            _ortamKaynagi.clip = k[0];
            _ortamKaynagi.volume = _kanalSeviyeleri[SesKanali.Ortam];
            _ortamKaynagi.Play();
        }

        public void OrtamDurdur() => _ortamKaynagi.Stop();

        public float KanalSeviyesi(SesKanali kanal) => _kanalSeviyeleri[kanal];

        public void KanalSeviyesiAyarla(SesKanali kanal, float seviye)
        {
            _kanalSeviyeleri[kanal] = Mathf.Clamp01(seviye);
            if (kanal == SesKanali.Ortam) _ortamKaynagi.volume = _kanalSeviyeleri[kanal];
            if (Mixer != null) Mixer.SetFloat(kanal.ToString(), DesibelCevir(_kanalSeviyeleri[kanal]));
            PlayerPrefs.SetFloat("amorland.ses." + kanal, _kanalSeviyeleri[kanal]);
        }

        private static float DesibelCevir(float dogrusal) =>
            dogrusal <= 0.0001f ? -80f : 20f * Mathf.Log10(dogrusal);

        public void SeviyeleriYukle()
        {
            foreach (SesKanali kanal in System.Enum.GetValues(typeof(SesKanali)))
            {
                float kayitli = PlayerPrefs.GetFloat("amorland.ses." + kanal, _kanalSeviyeleri[kanal]);
                KanalSeviyesiAyarla(kanal, kayitli);
            }
        }

        /// <summary>
        /// Uygulama arka plana atilinca tum ses durur (B.23 Unity teknik tablosu).
        /// Arka planda calan oyun sesi, oyuncunun podcast'inin ustune biner.
        /// </summary>
        private void OnApplicationPause(bool duraklatildi)
        {
            AudioListener.pause = duraklatildi;
        }

        private void OnApplicationFocus(bool odakta)
        {
            AudioListener.pause = !odakta;
        }
    }
}
