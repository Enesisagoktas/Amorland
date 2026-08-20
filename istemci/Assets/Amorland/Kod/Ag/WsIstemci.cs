using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Amorland.Ag
{
    /// <summary>
    /// WebSocket istemcisi. <see cref="ClientWebSocket"/> kullanir — ek paket yok.
    ///
    /// Gelen mesajlar arka plan is parcaciginda okunur ama <b>ana is parcaciginda</b>
    /// islenir: Unity API'leri yalnizca ana is parcacigindan cagrilabilir.
    /// Bu yuzden mesajlar kuyruga alinir, <see cref="Isle"/> her karede bosaltir.
    /// </summary>
    public class WsIstemci : IDisposable
    {
        public enum Durum { Kapali, Baglaniyor, Acik, Kapaniyor }

        private ClientWebSocket _soket;
        private CancellationTokenSource _iptal;
        private readonly ConcurrentQueue<string> _gelenKuyrugu = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _gidenKuyrugu = new ConcurrentQueue<string>();

        public Durum AnlikDurum { get; private set; } = Durum.Kapali;
        public string SonHata { get; private set; }

        /// <summary>Ana is parcaciginda cagrilir.</summary>
        public event Action<WsGelen> MesajGeldi;

        public event Action Acildi;
        public event Action<string> Kapandi;

        public bool Bagli => AnlikDurum == Durum.Acik;

        public async void Bagla(string url)
        {
            if (AnlikDurum == Durum.Baglaniyor || AnlikDurum == Durum.Acik) return;
            Kapat("yeniden baglaniyor");

            AnlikDurum = Durum.Baglaniyor;
            SonHata = null;
            _iptal = new CancellationTokenSource();
            _soket = new ClientWebSocket();

            try
            {
                await _soket.ConnectAsync(new Uri(url), _iptal.Token);
                AnlikDurum = Durum.Acik;
                _ = OkumaDongusu(_iptal.Token);
                _ = YazmaDongusu(_iptal.Token);
            }
            catch (Exception e)
            {
                SonHata = e.Message;
                AnlikDurum = Durum.Kapali;
                _gelenKuyrugu.Enqueue(null); // Isle() icinde Kapandi olayini tetikler
            }
        }

        private async Task OkumaDongusu(CancellationToken iptal)
        {
            var tampon = new byte[16 * 1024];
            var govde = new StringBuilder();
            try
            {
                while (_soket != null && _soket.State == WebSocketState.Open && !iptal.IsCancellationRequested)
                {
                    WebSocketReceiveResult sonuc =
                        await _soket.ReceiveAsync(new ArraySegment<byte>(tampon), iptal);

                    if (sonuc.MessageType == WebSocketMessageType.Close)
                    {
                        SonHata = sonuc.CloseStatusDescription;
                        break;
                    }
                    govde.Append(Encoding.UTF8.GetString(tampon, 0, sonuc.Count));
                    if (!sonuc.EndOfMessage) continue;

                    _gelenKuyrugu.Enqueue(govde.ToString());
                    govde.Clear();
                }
            }
            catch (OperationCanceledException)
            {
                // normal kapanis
            }
            catch (Exception e)
            {
                SonHata = e.Message;
            }
            finally
            {
                AnlikDurum = Durum.Kapali;
                _gelenKuyrugu.Enqueue(null);
            }
        }

        private async Task YazmaDongusu(CancellationToken iptal)
        {
            try
            {
                while (_soket != null && _soket.State == WebSocketState.Open && !iptal.IsCancellationRequested)
                {
                    if (_gidenKuyrugu.TryDequeue(out string mesaj))
                    {
                        byte[] veri = Encoding.UTF8.GetBytes(mesaj);
                        await _soket.SendAsync(
                            new ArraySegment<byte>(veri), WebSocketMessageType.Text, true, iptal);
                    }
                    else
                    {
                        await Task.Delay(10, iptal);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // normal kapanis
            }
            catch (Exception e)
            {
                SonHata = e.Message;
            }
        }

        public void Gonder(WsGiden mesaj)
        {
            _gidenKuyrugu.Enqueue(JsonConvert.SerializeObject(mesaj));
        }

        /// <summary>Her karede ana is parcaciginda cagrilir.</summary>
        public void Isle()
        {
            while (_gelenKuyrugu.TryDequeue(out string ham))
            {
                if (ham == null)
                {
                    Kapandi?.Invoke(SonHata ?? "baglanti kapandi");
                    continue;
                }
                WsGelen mesaj;
                try
                {
                    mesaj = JsonConvert.DeserializeObject<WsGelen>(ham);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Amorland] WS mesaji cozulemedi: " + e.Message);
                    continue;
                }
                if (mesaj == null) continue;
                MesajGeldi?.Invoke(mesaj);
            }

            if (AnlikDurum == Durum.Acik && !_acildiBildirildi)
            {
                _acildiBildirildi = true;
                Acildi?.Invoke();
            }
            if (AnlikDurum != Durum.Acik) _acildiBildirildi = false;
        }

        private bool _acildiBildirildi;

        public void Kapat(string sebep)
        {
            try
            {
                _iptal?.Cancel();
                if (_soket != null && _soket.State == WebSocketState.Open)
                {
                    AnlikDurum = Durum.Kapaniyor;
                    _ = _soket.CloseAsync(WebSocketCloseStatus.NormalClosure, sebep, CancellationToken.None);
                }
            }
            catch (Exception)
            {
                // kapanista onemli degil
            }
            finally
            {
                _soket?.Dispose();
                _soket = null;
                _iptal?.Dispose();
                _iptal = null;
                AnlikDurum = Durum.Kapali;
            }
        }

        public void Dispose() => Kapat("dispose");
    }
}
