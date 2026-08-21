#!/bin/sh
# Amorland — Unity C# kodunu derleyerek dogrular.
#
# IKI KATMAN, ama ESIT DEGIL:
#   GercekUnity/  — GERCEK Unity DLL'lerine karsi derler. Lisans gerektirmez, editor
#                   acilmaz. Unity kuruluysa TEK OTORITE budur.
#   UnityStub/    — Unity KURULU DEGILKEN calisan yedek yol. Imzalari elle taklit eder.
#
# ⚠ Unity kuruluyken stub CALISTIRILMAZ. Sebebi tecrubeyle sabit:
#   (a) Stub eksik. Su an RectInt, Tilemap/Tile/Grid, RenderTexture, Animator,
#       AnimationClip, AssetPostprocessor, UnityEditor.Build ve UnityEditor.Animations
#       yok. Bu tiplere dokunan her dosya stub'da kirmizi yanar; gercek Unity'de temiz.
#   (b) Daha kotusu, stub bir imzayi YANLIS taklit ederse yesil yanar ve kod Unity'de
#       kirilir. Bu tam olarak yasandi: csproj'a AnimationModule eklenince derleme
#       yesile dondu ama gercek sebep manifest.json'daki eksik paketti.
#   Iki katmani birlikte kosturmak, otorite olmayan katmanin gurultusunu otoritenin
#   uzerine biniyor. Otorite varken otoriteye bakilir.
#
# YAKALAR:    sozdizimi, tip, imza, eksik using, yanlis ad alani
# YAKALAMAZ:  calisma zamani davranisi, sahne kurulumu, shader/materyal sorunlari
#             (onlar icin araclar/... altindaki bassiz render ve denetim araclari var)
#
# Kullanim:  DOTNET=/yol/dotnet.exe ./dogrula.sh
#            UNITY_DIZIN="C:\Program Files\Unity\Hub\Editor\6000.3.22f1" ./dogrula.sh
set -e
DIZIN="$(cd "$(dirname "$0")" && pwd)"
DOTNET="${DOTNET:-dotnet}"
UNITY_DIZIN="${UNITY_DIZIN:-C:\Program Files\Unity\Hub\Editor\6000.3.22f1}"

# Her varyant ayri cikti klasorune derlenir: MSBuild onbellegi bayat "0 hata" gostermesin.
if [ -d "$UNITY_DIZIN" ]; then
  echo "== Gercek Unity: eski girdi kolu =="
  "$DOTNET" build "$DIZIN/GercekUnity/GercekUnity.csproj" -v quiet --nologo \
    -p:UnityDizin="$UNITY_DIZIN"

  echo "== Gercek Unity: yeni Input System kolu =="
  "$DOTNET" build "$DIZIN/GercekUnity/GercekUnity.csproj" -v quiet --nologo \
    -p:UnityDizin="$UNITY_DIZIN" -p:DefineConstants="ENABLE_INPUT_SYSTEM" \
    -p:BaseOutputPath="bin/yeni-girdi/"

  echo "Istemci C# kodu GERCEK Unity assembly'lerine karsi, iki girdi kolunda da derleniyor."
else
  echo "Unity bulunamadi ($UNITY_DIZIN) — YEDEK YOL: stub dogrulamasi."
  echo "⚠ Stub eksiktir ve yanlis imza taklit ederse yesil gorunup Unity'de kirilir."
  echo "⚠ Bu cikti KANIT DEGILDIR. Birlestirmeden once Unity kurulu bir makinede kos."

  echo "== Stub: eski girdi kolu =="
  "$DOTNET" build "$DIZIN/Dogrulama/Dogrulama.csproj" -v quiet --nologo

  echo "== Stub: yeni Input System kolu =="
  "$DOTNET" build "$DIZIN/Dogrulama/Dogrulama.csproj" -v quiet --nologo \
    -p:DefineConstants="ENABLE_INPUT_SYSTEM" -p:BaseOutputPath="bin/yeni-girdi/"
fi
