#!/usr/bin/env bash
# Downloads the MediaPipe Unity plugin package and the hand landmarker model, checks their SHA-256,
# and puts them where the Unity project expects them. Fails (non-zero exit) on any mismatch.
# Run from anywhere: scripts/fetch-mediapipe.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

PLUGIN_VERSION="0.16.3"
PLUGIN_URL="https://github.com/homuler/MediaPipeUnityPlugin/releases/download/v${PLUGIN_VERSION}/com.github.homuler.mediapipe-${PLUGIN_VERSION}.tgz"
PLUGIN_SHA256="cc3e77a219e0b99618ae3be64c31a566197deedc69c1e136acf52d65d7cf2e79"
PLUGIN_DEST="$ROOT/app/Packages/com.github.homuler.mediapipe-${PLUGIN_VERSION}.tgz"

MODEL_URL="https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task"
MODEL_SHA256="fbc2a30080c3c557093b5ddfc334698132eb341044ccee322ccf8bcf3607cde1"
MODEL_DEST="$ROOT/app/Assets/StreamingAssets/hand_landmarker.task"

# fetch URL DEST SHA256: skips the download when DEST already has the right hash.
fetch() {
  local url="$1" dest="$2" want="$3" got tmp
  if [ -f "$dest" ]; then
    got="$(sha256sum "$dest" | cut -d' ' -f1)"
    if [ "$got" = "$want" ]; then
      echo "OK (already present): $dest"
      return 0
    fi
    echo "Existing file has the wrong hash, downloading again: $dest"
  fi
  mkdir -p "$(dirname "$dest")"
  tmp="$dest.part"
  rm -f "$tmp"
  echo "Downloading $url"
  curl -fL --retry 3 --retry-delay 5 -o "$tmp" "$url"
  got="$(sha256sum "$tmp" | cut -d' ' -f1)"
  if [ "$got" != "$want" ]; then
    rm -f "$tmp"
    echo "ERROR: SHA-256 mismatch for $url" >&2
    echo "  expected $want" >&2
    echo "  got      $got" >&2
    exit 1
  fi
  mv "$tmp" "$dest"
  echo "OK: $dest"
}

fetch "$PLUGIN_URL" "$PLUGIN_DEST" "$PLUGIN_SHA256"
fetch "$MODEL_URL" "$MODEL_DEST" "$MODEL_SHA256"
