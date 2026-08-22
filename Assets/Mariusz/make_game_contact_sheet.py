from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


SOURCE = Path(r"C:\Users\mkles\Pictures\Screenshots")
OUTPUT = Path("tmp/game_contact_sheet.png")
NAMES = [
    "Zrzut ekranu 2026-08-05 023851.png",
    "Zrzut ekranu 2026-07-30 194724.png",
    "Zrzut ekranu 2026-07-30 194141.png",
    "Zrzut ekranu 2026-07-26 201725.png",
    "Zrzut ekranu 2026-07-26 200244.png",
    "Zrzut ekranu 2026-07-26 193420.png",
    "Zrzut ekranu 2026-07-26 182316.png",
    "Zrzut ekranu 2026-07-26 180901.png",
    "Zrzut ekranu 2026-07-26 175722.png",
    "Zrzut ekranu 2026-07-26 141715.png",
    "Zrzut ekranu 2026-07-26 135528.png",
    "Zrzut ekranu 2026-07-26 131739.png",
]


def main():
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    thumb_width, thumb_height = 480, 300
    columns, padding, label_height = 3, 24, 42
    rows = (len(NAMES) + columns - 1) // columns
    canvas = Image.new(
        "RGB",
        (columns * thumb_width + (columns + 1) * padding, rows * (thumb_height + label_height) + (rows + 1) * padding),
        "#101416",
    )
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()

    for index, name in enumerate(NAMES):
        image = Image.open(SOURCE / name).convert("RGB")
        image.thumbnail((thumb_width, thumb_height))
        x = padding + (index % columns) * (thumb_width + padding)
        y = padding + (index // columns) * (thumb_height + label_height + padding)
        canvas.paste(image, (x + (thumb_width - image.width) // 2, y + (thumb_height - image.height) // 2))
        draw.text((x, y + thumb_height + 10), name.replace("Zrzut ekranu ", ""), fill="#d5ddd7", font=font)

    canvas.save(OUTPUT)


if __name__ == "__main__":
    main()
