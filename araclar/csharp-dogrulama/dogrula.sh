#!/bin/sh
# Amorland — Unity C# kodunu derleyerek dogrular.
#
# IKI KATMAN:
#   1) UnityStub/    — Unity KURULU DEGILKEN calisir. Imzalar elle taklit edilir.
#                      Hizli ama stub yanlissa yalan soyler.
#   2) GercekUnity/  — Unity kuruluysa GERCEK DLL'lere karsi derler. Kesin sonuc.
#                      Lisans GEREKTIRMEZ, editor acilmaz.
#
# Her katman iki girdi kolunda da kosar (eski Input / yeni Input System).
#
# YAKALAR:    sozdizimi, tip, imza, eksik using, yanlis ad alani
# YAKALAMAZ:  calisma zamani davranisi, sahne kurulumu, shader/materyal sorunlari
#
# Kullanim:  DOTNET=/yol/dotnet.exe ./dogrula.sh
#            UNITY_DIZIN="C:\Program Files\Unity\Hub\Editor\6000.3.22f1" ./dogrula.sh
set -e
DIZIN="$(cd "$(dirname "$0")" && pwd)"
DOTNET="${DOTNET:-dotnet}"
# Her varyant ayri cikti klasorune derlenir: MSBuild onbellegi bayat "0 hata" gostermesin.
UNITY_DIZIN="${UNITY_DIZIN:-C:\Program Files\Unity\Hub\Editor\6000.3.22f1}"

echo "== Stub: eski girdi kolu =="
"$DOTNET" build "$DIZIN/Dogrulama/Dogrulama.csproj" -v quiet --nologo

echo "== Stub: yeni Input System kolu =="
"$DOTNET" build "$DIZIN/Dogrulama/Dogrulama.csproj" -v quiet --nologo \
  -p:DefineConstants="ENABLE_INPUT_SYSTEM" -p:BaseOutputPath="bin/yeni-girdi/"

if [ -d "$UNITY_DIZIN" ]; then
  echo "== Gercek Unity: eski girdi kolu =="
  "$DOTNET" build "$DIZIN/GercekUnity/GercekUnity.csproj" -v quiet --nologo \
    -p:UnityDizin="$UNITY_DIZIN"

  echo "== Gercek Unity: yeni Input System kolu =="
  "$DOTNET" build "$DIZIN/GercekUnity/GercekUnity.csproj" -v quiet --nologo \
    -p:UnityDizin="$UNITY_DIZIN" -p:DefineConstants="ENABLE_INPUT_SYSTEM"     -p:BaseOutputPath="bin/yeni-girdi/"

  echo "Istemci C# kodu GERCEK Unity assembly'lerine karsi, iki girdi kolunda da derleniyor."
else
  echo "Unity bulunamadi ($UNITY_DIZIN) — yalnizca stub dogrulamasi yapildi."
  echo "Stub bir imzayi yanlis taklit ederse bu yesil gorunur ama Unity'de kirilir."
fi
