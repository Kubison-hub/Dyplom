from pathlib import Path

from pypdf import PdfReader, PdfWriter


ROOT = Path(__file__).parent
OUTPUT = ROOT / "output" / "pdf" / "Sherlock_Holmes_Combined_Presentation.pdf"
SOURCES = [
    ROOT / "output" / "pdf" / "Sherlock_Holmes_English_Presentation.pdf",
    ROOT / "output" / "pdf" / "Sherlock_Holmes_My_Developer_Role.pdf",
]


def merge_presentations():
    writer = PdfWriter()
    writer.add_metadata({
        "/Title": "Sherlock Holmes - Game Project and My Developer Role",
        "/Author": "Warsaw Film School",
        "/Subject": "Diploma game project presentation",
    })

    page_offset = 0
    sections = ["The Game Project", "My Developer Role"]
    for section, source in zip(sections, SOURCES):
        reader = PdfReader(str(source))
        writer.add_outline_item(section, page_offset)
        for page in reader.pages:
            writer.add_page(page)
        page_offset += len(reader.pages)

    with OUTPUT.open("wb") as file:
        writer.write(file)


if __name__ == "__main__":
    merge_presentations()
    print(OUTPUT)
