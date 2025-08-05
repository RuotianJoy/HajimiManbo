# generate_autotile_47_full.py
# pip install pillow numpy
from PIL import Image
import numpy as np
from pathlib import Path

TILE   = 48               # ← 一处改成 48
COLS   = 8                # 8×6 = 48 (>47)；图集宽 = 8*48 = 384
ROWS   = 6
OUT_P  = Path("tileset_47_48px.png")

# === 1. 底砖 ===
base = Image.open("Content/Tiles/Dirt_Block_(placed).png").convert("RGBA")    # 请准备 48×48
if base.size != (TILE, TILE):
    base = base.resize((TILE, TILE), Image.NEAREST)

# === 2. 掩码表（示例只列前 13 帧；缺的用实心占位） ===
def full():  return np.ones((TILE, TILE), np.uint8)
def rect(l,t,r,b):
    m = np.zeros((TILE, TILE), np.uint8); m[t:b, l:r] = 1; return m
def tri(br):  # br∈{"BR","BL","TR","TL"}
    m = np.zeros((TILE,TILE), np.uint8); y,x = np.ogrid[:TILE,:TILE]
    if br=="BR": m[y+x < TILE] = 1          # ↘
    if br=="BL": m[y < x] = 1               # ↙
    if br=="TR": m[y >= x] = 1              # ↗
    if br=="TL": m[y+x >= TILE] = 1         # ↖
    return m

MASKS = [
    full(),                    # 0 实心
    tri("BR"), tri("BL"), tri("TR"), tri("TL"),  # 1-4 外角
    rect(0,0,TILE,TILE//2),    # 5 上边
    rect(TILE//2,0,TILE,TILE), # 6 右边
    rect(0,TILE//2,TILE,TILE), # 7 下边
    rect(0,0,TILE//2,TILE),    # 8 左边
    tri("BR"), tri("BL"), tri("TR"), tri("TL"),  # 9-12 对角斜坡
]
while len(MASKS) < 47:
    MASKS.append(full())       # 未完成的帧暂时用实心占位

# === 3. 合成图集 ===
atlas = Image.new("RGBA", (COLS*TILE, ROWS*TILE))
for idx, mask in enumerate(MASKS):
    dst = np.zeros((TILE, TILE, 4), np.uint8)          # 全透明
    base_arr = np.asarray(base)
    dst[mask == 1] = base_arr[mask == 1]               # 写可见像素
    tile_img = Image.fromarray(dst, mode="RGBA")
    atlas.paste(tile_img, ((idx % COLS)*TILE, (idx // COLS)*TILE))

atlas.save(OUT_P)
print("✅ 已生成", OUT_P, f"({atlas.width}×{atlas.height})")
