using System.Collections.Generic;
using UnityEngine;

namespace Amorland.Girdi
{
    /// <summary>
    /// Izgara A* yol bulma (B.6).
    ///
    /// Vadi karolari kucuk (Sv 1'de 4, Sv 18'de 24 + kenar seridi), o yuzden basit
    /// bir ikili yigin yeterli. Karmasik navmesh gerekmez ve mobilde bedava degil.
    ///
    /// Engel modeli kasitli olarak ZAYIF: yalnizca "gecilemez" isaretli hucreler engeldir.
    /// Ekili karo engel DEGILDIR — oyuncu urunun uzerinden gecebilir. Aksi halde 24 karolu
    /// bir parselde oyuncu kendi tarlasinda kapana kisilirdi.
    /// </summary>
    public class Izgara
    {
        private readonly HashSet<long> _engeller = new HashSet<long>();

        /// <summary>
        /// Dis engel sorgusu: vadi haritasinin gecilmezligi (su, bina, agac govdesi)
        /// buradan gelir. 163 bin hucreyi kumeye taramak yerine geri cagri —
        /// A* zaten yalnizca dokundugu hucreleri sorar.
        /// </summary>
        public System.Func<int, int, bool> DisEngel;

        public int MinX = -12, MaksX = 24, MinY = -8, MaksY = 16;

        private static long Anahtar(int x, int y) => ((long)x << 32) ^ (uint)y;

        public void EngelKoy(int x, int y) => _engeller.Add(Anahtar(x, y));

        public void EngelKaldir(int x, int y) => _engeller.Remove(Anahtar(x, y));

        public void Temizle() => _engeller.Clear();

        public bool Gecilebilir(int x, int y) =>
            x >= MinX && x <= MaksX && y >= MinY && y <= MaksY
            && !_engeller.Contains(Anahtar(x, y))
            && (DisEngel == null || !DisEngel(x, y));

        private static readonly int[] KomsuX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] KomsuY = { 0, 0, 1, -1, 1, -1, 1, -1 };

        /// <summary>
        /// Yol bulur. Hedef gecilemezse hedefin YANINA gidilir — oyuncunun dokundugu
        /// karoya ulasamamasi "oyun beni dinlemiyor" hissi verir.
        /// </summary>
        public List<Vector2Int> Yol(Vector2Int baslangic, Vector2Int hedef, int maksAdim = 4096)
        {
            if (baslangic == hedef) return new List<Vector2Int>();

            Vector2Int gercekHedef = hedef;
            if (!Gecilebilir(hedef.x, hedef.y))
            {
                gercekHedef = EnYakinGecilebilir(hedef, baslangic);
                if (gercekHedef == baslangic) return new List<Vector2Int>();
            }

            var acik = new List<Vector2Int> { baslangic };
            var gelinen = new Dictionary<long, Vector2Int>();
            var gSkor = new Dictionary<long, float> { [Anahtar(baslangic.x, baslangic.y)] = 0f };
            var fSkor = new Dictionary<long, float>
            {
                [Anahtar(baslangic.x, baslangic.y)] = Sezgi(baslangic, gercekHedef)
            };
            var kapali = new HashSet<long>();
            int adim = 0;

            // Hedefe ULASILAMAZSA (kapali cit alani gibi) bos donmek karakteri
            // cakili birakiyordu; gidilebilen EN YAKIN dugum izlenir ve oraya
            // yurunur — "oyun beni dinlemiyor" hissi biter.
            Vector2Int enYakinDugum = baslangic;
            float enYakinSezgi = Sezgi(baslangic, gercekHedef);

            while (acik.Count > 0 && adim++ < maksAdim)
            {
                int enIyi = 0;
                for (int i = 1; i < acik.Count; i++)
                {
                    if (fSkor[Anahtar(acik[i].x, acik[i].y)] < fSkor[Anahtar(acik[enIyi].x, acik[enIyi].y)])
                        enIyi = i;
                }
                Vector2Int simdiki = acik[enIyi];
                acik.RemoveAt(enIyi);
                long simdikiAnahtar = Anahtar(simdiki.x, simdiki.y);
                kapali.Add(simdikiAnahtar);

                if (simdiki == gercekHedef) return YoluTopla(gelinen, simdiki);

                float sezgi = Sezgi(simdiki, gercekHedef);
                if (sezgi < enYakinSezgi)
                {
                    enYakinSezgi = sezgi;
                    enYakinDugum = simdiki;
                }

                for (int k = 0; k < KomsuX.Length; k++)
                {
                    int nx = simdiki.x + KomsuX[k];
                    int ny = simdiki.y + KomsuY[k];
                    if (!Gecilebilir(nx, ny)) continue;

                    // Caprazda kose kesme yok: karakter duvarin icinden gecmis gorunmesin.
                    bool capraz = KomsuX[k] != 0 && KomsuY[k] != 0;
                    if (capraz &&
                        (!Gecilebilir(simdiki.x + KomsuX[k], simdiki.y) ||
                         !Gecilebilir(simdiki.x, simdiki.y + KomsuY[k])))
                    {
                        continue;
                    }

                    long komsuAnahtar = Anahtar(nx, ny);
                    if (kapali.Contains(komsuAnahtar)) continue;

                    float maliyet = capraz ? 1.41421f : 1f;
                    float yeniG = gSkor[simdikiAnahtar] + maliyet;
                    if (gSkor.TryGetValue(komsuAnahtar, out float eskiG) && yeniG >= eskiG) continue;

                    gelinen[komsuAnahtar] = simdiki;
                    gSkor[komsuAnahtar] = yeniG;
                    fSkor[komsuAnahtar] = yeniG + Sezgi(new Vector2Int(nx, ny), gercekHedef);
                    var komsu = new Vector2Int(nx, ny);
                    if (!acik.Contains(komsu)) acik.Add(komsu);
                }
            }
            return enYakinDugum == baslangic
                ? new List<Vector2Int>()
                : YoluTopla(gelinen, enYakinDugum);
        }

        private static float Sezgi(Vector2Int a, Vector2Int b)
        {
            // Octile: 8 yonlu izgarada Manhattan'dan dogru, Oklid'den hizli.
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return (dx + dy) + (1.41421f - 2f) * Mathf.Min(dx, dy);
        }

        private List<Vector2Int> YoluTopla(Dictionary<long, Vector2Int> gelinen, Vector2Int son)
        {
            var yol = new List<Vector2Int> { son };
            long anahtar = Anahtar(son.x, son.y);
            while (gelinen.TryGetValue(anahtar, out Vector2Int onceki))
            {
                yol.Add(onceki);
                anahtar = Anahtar(onceki.x, onceki.y);
            }
            yol.Reverse();
            if (yol.Count > 0) yol.RemoveAt(0); // baslangic karosu yola dahil degil
            return yol;
        }

        /// <summary>
        /// Engelli hedefi en yakin gecilebilir hucreye yansitir. Halkadaki
        /// adaylar arasindan OYUNCUYA en yakin olan secilir: sabit tarama
        /// sirasi (eski hali) hep guneybatiyi seciyordu ve kuzeyden dokunulan
        /// citte karakter citin OBUR yuzune dolaniyordu (oyuncu bulgusu).
        /// </summary>
        private Vector2Int EnYakinGecilebilir(Vector2Int hedef, Vector2Int baslangic)
        {
            for (int r = 1; r <= 4; r++)
            {
                Vector2Int enIyi = hedef;
                float enIyiSkor = float.MaxValue;
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                        int nx = hedef.x + dx;
                        int ny = hedef.y + dy;
                        if (!Gecilebilir(nx, ny)) continue;
                        var aday = new Vector2Int(nx, ny);
                        // Once oyuncuya yakinlik (kendi yuzundeki hucre),
                        // esitlikte hedefe yakinlik.
                        float skor = Sezgi(aday, baslangic) * 16f + Sezgi(aday, hedef);
                        if (skor < enIyiSkor)
                        {
                            enIyiSkor = skor;
                            enIyi = aday;
                        }
                    }
                }
                if (enIyiSkor < float.MaxValue) return enIyi;
            }
            return hedef;
        }
    }
}
