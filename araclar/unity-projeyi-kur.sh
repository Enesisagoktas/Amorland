#!/bin/sh
# Amorland — Unity projesini kurar ve derler (lisans aktif OLDUKTAN sonra calistir).
#
# Yaptiklari:
#   1. Unity'yi batchmode'da acar (paketleri cozer, scriptleri derler)
#   2. Amorland > Kurulumu Calistir menusunu koddan tetikler
#   3. Derleme hatasi varsa loglardan cikarir
#
# Lisans yoksa Unity acilmaz. Once Unity Hub'da hesabinla giris yap.
#
# Kullanim:  ./araclar/unity-projeyi-kur.sh
set -e
KOK="$(cd "$(dirname "$0")/.." && pwd)"
UNITY="${UNITY_EXE:-/c/Program Files/Unity/Hub/Editor/6000.3.22f1/Editor/Unity.exe}"
PROJE="${PROJE:-C:/App/Amorland/istemci}"
LOG="/tmp/amorland-unity.log"

if [ ! -f "$UNITY" ]; then
  echo "Unity bulunamadi: $UNITY"
  exit 1
fi

echo "Unity aciliyor (ilk acilis paketleri indirir, 2-5 dakika surebilir)..."
"$UNITY" -batchmode -quit -nographics \
  -logFile "$LOG" \
  -projectPath "$PROJE" \
  -executeMethod Amorland.EditorAraclari.AmorlandKurulum.Calistir || true

echo
if grep -qiE "Found 0 entitlement|No valid Unity Editor license|LICENSE SYSTEM" "$LOG" 2>/dev/null; then
  echo "LISANS YOK. Unity Hub'i ac, hesabinla giris yap, sonra bu betigi tekrar calistir."
  exit 2
fi

echo "=== Derleme hatalari ==="
if grep -E "error CS[0-9]+" "$LOG" 2>/dev/null | sort -u | head -20; then
  :
fi
grep -cE "error CS[0-9]+" "$LOG" 2>/dev/null | xargs -I{} echo "toplam: {} hata"

echo
echo "=== Kurulum ciktisi ==="
grep -A8 "\[Amorland\] Kurulum" "$LOG" 2>/dev/null | head -12 || echo "(kurulum mesaji bulunamadi, tam log: $LOG)"
