#!/usr/bin/env bash
# Tải 30 ảnh listing từ Unsplash (fallback Picsum) — chạy 1 lần khi setup project.
set -u
DEST="$(dirname "$0")/../wwwroot/img/listings"
mkdir -p "$DEST"

declare -a images=(
  "luxury 1 photo-1502672260266-1c1ef2d93688"
  "luxury 2 photo-1560448204-e02f11c3d0e2"
  "luxury 3 photo-1493809842364-78817add7ffb"
  "luxury 4 photo-1505691938895-1758d7feb511"
  "luxury 5 photo-1522708323590-d24dbb6b0267"
  "mini 1 photo-1554995207-c18c203602cb"
  "mini 2 photo-1567767292278-a4f21aa2d36e"
  "mini 3 photo-1556228720-195a672e8a03"
  "mini 4 photo-1631679706909-1844bbd07221"
  "mini 5 photo-1502672023488-70e25813eb80"
  "house 1 photo-1568605114967-8130f3a36994"
  "house 2 photo-1570129477492-45c003edd2be"
  "house 3 photo-1605276374104-dee2a0ed3cd6"
  "house 4 photo-1582268611958-ebfd161ef9cf"
  "house 5 photo-1592595896616-c37162298647"
  "villa 1 photo-1613490493576-7fde63acd811"
  "villa 2 photo-1600596542815-ffad4c1539a9"
  "villa 3 photo-1600585154340-be6161a56a0c"
  "villa 4 photo-1564013799919-ab600027ffc6"
  "villa 5 photo-1512917774080-9991f1c4c750"
  "penthouse 1 photo-1600210492486-724fe5c67fb0"
  "penthouse 2 photo-1565538810643-b5bdb714032a"
  "penthouse 3 photo-1600585152220-90363fe7e115"
  "penthouse 4 photo-1583847268964-b28dc8f51f92"
  "penthouse 5 photo-1582719508461-905c673771fd"
  "room 1 photo-1631889993959-41b4e9c6e3c5"
  "room 2 photo-1522444195799-478538b28823"
  "room 3 photo-1540518614846-7eded433c457"
  "room 4 photo-1541123437800-1bb1317badc2"
  "room 5 photo-1486946255434-2466348c2166"
)

ok=0; fb=0; fail=0
for entry in "${images[@]}"; do
  read -r cat idx pid <<<"$entry"
  out="$DEST/${cat}-${idx}.jpg"
  if [[ -s "$out" ]]; then ok=$((ok+1)); continue; fi
  url="https://images.unsplash.com/${pid}?w=1200&q=80&auto=format&fit=crop"
  if curl -fsSL --max-time 30 -o "$out" "$url" && [[ $(stat -c%s "$out" 2>/dev/null || stat -f%z "$out") -gt 5000 ]]; then
    ok=$((ok+1))
  else
    rm -f "$out"
    seed="${cat}${idx}"
    if curl -fsSL --max-time 30 -o "$out" "https://picsum.photos/seed/${seed}/1200/800"; then
      fb=$((fb+1))
    else
      echo "FAIL ${cat}-${idx}.jpg"
      fail=$((fail+1))
    fi
  fi
done
echo "Unsplash OK: $ok, Picsum fallback: $fb, Failed: $fail"
ls -1 "$DEST" | wc -l
