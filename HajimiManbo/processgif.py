
from pathlib import Path
from PIL import Image

# ======== �������� ========
SOURCE_DIR = Path(r"D:\csProject\HajimiManbo\HajimiManbo\HajimiManbo\Content\img\Character\Doro")
SHEET_MODE = "RGBA"
PADDING = 0
# ==========================

def export_frames(gif_path: Path, out_dir: Path) -> list[Path]:

    with Image.open(gif_path) as im:
        frame_paths = []
        for idx in range(im.n_frames):
            im.seek(idx)
            frame = im.convert("RGBA")     # ����͸��
            fname = f"{gif_path.stem}_f{idx:03d}.png"
            out_path = out_dir / fname
            frame.save(out_path)
            frame_paths.append(out_path)
        return frame_paths

def build_spritesheet(frame_paths: list[Path], out_sheet: Path):

    frames = [Image.open(p).convert("RGBA") for p in frame_paths]
    w, h = frames[0].size
    sheet_w = w * len(frames) + PADDING * (len(frames) - 1)
    sheet_h = h
    sheet = Image.new(SHEET_MODE, (sheet_w, sheet_h), (0, 0, 0, 0))

    x = 0
    for frame in frames:
        sheet.paste(frame, (x, 0))
        x += w + PADDING
    sheet.save(out_sheet)
    for f in frames:
        f.close()

def process_all_gifs(src_dir: Path):
    out_root = src_dir
    out_root.mkdir(exist_ok=True)

    for gif_path in src_dir.glob("*.gif"):
        print(f"{gif_path.name}")
        gif_out = out_root / gif_path.stem
        gif_out.mkdir(exist_ok=True)

        frame_files = export_frames(gif_path, gif_out)
        sheet_file = gif_out / f"{gif_path.stem}_sheet.png"
        build_spritesheet(frame_files, sheet_file)

        print(f"   {len(frame_files)} ֡ + spritesheet{sheet_file.name}")

if __name__ == "__main__":
    process_all_gifs(SOURCE_DIR)
