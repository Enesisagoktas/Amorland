#!/bin/sh
# Amorland — Unity C# kodunu Unity KURULU OLMADAN derleyerek dogrular.
#
# Nasil calisir: UnityStub/ Unity ve UnityEditor API'lerinin bos govdeli kopyalarini
# icerir. Istemci kodu bunlara karsi derlenir.
#
# YAKALAR:    sozdizimi, tip, imza, eksik using, yanlis alan adi
# YAKALAMAZ:  calisma zamani davranisi, sahne kurulumu, shader/materyal sorunlari
#             (bunlar icin Unity gerekir)
#
# Iki kol da derlenir:
#   1) eski girdi sistemi (varsayilan)
#   2) yeni Input System (ENABLE_INPUT_SYSTEM)
# Ikisi de yesil olmadan "Unity'de derlenir" denmez.
#
# Kullanim:  DOTNET=/yol/dotnet.exe ./dogrula.sh
set -e
DIZIN="$(cd "$(dirname "$0")" && pwd)"
DOTNET="${DOTNET:-dotnet}"

echo "== 1/2: eski girdi kolu =="
"$DOTNET" build "$DIZIN/Dogrulama/Dogrulama.csproj" -v quiet --nologo

echo "== 2/2: yeni Input System kolu =="
"$DOTNET" build "$DIZIN/Dogrulama/Dogrulama.csproj" -v quiet --nologo \
  -p:DefineConstants="ENABLE_INPUT_SYSTEM"

echo "Istemci C# kodu iki girdi kolunda da derleniyor."
