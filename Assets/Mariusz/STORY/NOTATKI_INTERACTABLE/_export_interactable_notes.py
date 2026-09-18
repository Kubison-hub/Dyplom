import json
import re
from collections import defaultdict
from pathlib import Path


MARIUSZ_DIR = Path(__file__).resolve().parents[2]
ASSETS_DIR = MARIUSZ_DIR.parent
SCENE_PATH = MARIUSZ_DIR / "Levels" / "GameLeveL_DEV.unity"
OUTPUT_DIR = Path(__file__).resolve().parent

INTERACTABLE_GUID = "06082a455667b474fa5c9d90a6ee4fec"
NOTE_DATA_GUID = "8ebbf5064190e4340af5d5b2b99c1ef0"

CATEGORIES = {
    0: "Obserwacje",
    1: "Osoby",
    2: "Obiekty",
    3: "Zadania",
}

PEOPLE = {
    0: "None",
    1: "LadyEdithOgilvy",
    2: "MadameSelma",
    3: "LadyVioletOgilvy",
    4: "SirHenry",
    5: "Arthur",
    6: "ReverendGeorge",
    7: "LittleEthel",
}

OBSERVATIONS = {
    0: "None",
    1: "DuchJakoSprawca",
    2: "TajemniczeDzwieki",
    3: "PierscienZInicjalem",
    4: "PustyPergamin",
    5: "MiejsceZbrodni",
    6: "SekretnePrzejscie",
    7: "UkrytePrzejscie",
    8: "PulapkaWPiwnicy",
}


def yaml_scalar(value):
    value = value.strip()
    if value.startswith("'"):
        value = re.sub(r"\r?\n[ ]+", " ", value)
        return value[1:-1].replace("''", "'") if value.endswith("'") else value.strip("'")
    if not value.startswith('"'):
        return value

    value = re.sub(r"\r?\n[ ]+", " ", value)
    value = re.sub(r"\\x([0-9a-fA-F]{2})", r"\\u00\1", value)
    try:
        return json.loads(value)
    except json.JSONDecodeError:
        return value.strip('"')


def field(document, name, default=""):
    match = re.search(
        rf"(?ms)^  {re.escape(name)}: (.*?)(?=^  [A-Za-z_][A-Za-z0-9_]*:|\Z)",
        document,
    )
    return yaml_scalar(match.group(1)) if match else default


def match_value(document, pattern, default=""):
    match = re.search(pattern, document, re.MULTILINE)
    return match.group(1) if match else default


def bool_text(value):
    if value is None:
        return "nieustalone (obiekt pochodzi z instancji modelu/prefabu)"
    if isinstance(value, bool):
        return "tak" if value else "nie"
    return "tak" if str(value) == "1" else "nie"


def asset_display_path(path):
    return "Assets/" + path.relative_to(ASSETS_DIR).as_posix()


def format_content(value):
    return (value or "").replace("\r\n", "\n").replace("\r", "\n").strip()


def read_guid_index():
    asset_by_guid = {}
    script_by_guid = {}

    for meta_path in ASSETS_DIR.rglob("*.meta"):
        try:
            text = meta_path.read_text(encoding="utf-8-sig", errors="replace")
        except OSError:
            continue

        guid_match = re.search(r"(?m)^guid: ([0-9a-f]{32})$", text)
        if not guid_match:
            continue

        asset_path = Path(str(meta_path)[:-5])
        guid = guid_match.group(1)
        asset_by_guid[guid] = asset_path
        if asset_path.suffix.lower() == ".cs":
            script_by_guid[guid] = asset_path.stem

    return asset_by_guid, script_by_guid


def read_note(asset_path, guid):
    document = asset_path.read_text(encoding="utf-8-sig", errors="replace")
    if NOTE_DATA_GUID not in document:
        return {
            "guid": guid,
            "path": asset_path,
            "name": asset_path.stem,
            "title": "[Asset nie jest NoteData]",
            "category": "Nieznana",
            "person": "None",
            "observation": "None",
            "content": "",
        }

    category_value = int(field(document, "category", "-1"))
    person_value = int(field(document, "person", "0"))
    observation_value = int(field(document, "observation", "0"))
    return {
        "guid": guid,
        "path": asset_path,
        "name": field(document, "m_Name", asset_path.stem),
        "title": field(document, "noteTitle", ""),
        "category": CATEGORIES.get(category_value, f"Nieznana ({category_value})"),
        "person": PEOPLE.get(person_value, f"Nieznana ({person_value})"),
        "observation": OBSERVATIONS.get(observation_value, f"Nieznana ({observation_value})"),
        "content": format_content(field(document, "content", "")),
    }


def custom_activation_descriptions(component_names, component_documents):
    descriptions = []

    for script_name, document in component_documents:
        if script_name == "Int_Edith_BulletHole":
            descriptions.append(
                "`Int_Edith_BulletHole.CompleteSuccessfulExamination()` dodaje notatkę "
                f"o indeksie `{match_value(document, r'successfulExaminationNoteIndex: (-?\d+)', '0')}` "
                "dopiero po udanej examinacji."
            )
        elif script_name == "Int_lv1_HidenWallMask":
            descriptions.append(
                "`Int_lv1_HidenWallMask.CompleteMask()` dodaje wszystkie notatki po odkryciu całego wzoru przez LineRenderery."
            )
        elif script_name == "Int_lv2_WoodBrickWall":
            descriptions.append(
                "`Int_lv2_WoodBrickWall.AddOpenedDoorNotebookNote()` dodaje notatkę "
                f"o indeksie `{match_value(document, r'openedDoorNoteIndex: (-?\d+)', '-1')}` po poprawnym otwarciu drzwi."
            )
        elif script_name == "lvl2_Int_Letter":
            descriptions.append(
                "`lvl2_Int_Letter` dodaje i natychmiast otwiera notatkę "
                f"o indeksie `{match_value(document, r'notebookNoteIndex: (-?\d+)', '0')}` przy podniesieniu listu."
            )
        elif script_name == "int_lv3_easyTable":
            pre = match_value(document, r"prePuzzleNoteIndex: (-?\d+)", "-1")
            completed = match_value(document, r"completedPuzzleNoteIndex: (-?\d+)", "-1")
            additional = match_value(document, r"completedPuzzleAdditionalNoteIndex: (-?\d+)", "-1")
            descriptions.append(
                "`int_lv3_easyTable` używa indeksów: "
                f"pierwsza interakcja `{pre}`, ukończenie `{completed}`, dodatkowa po ukończeniu `{additional}`."
            )
        elif script_name == "Int_lv3_Ethel":
            descriptions.append(
                "`Int_lv3_Ethel` dodaje i natychmiast otwiera notatkę "
                f"o indeksie `{match_value(document, r'notebookNoteIndex: (-?\d+)', '0')}` po zakończeniu pierwszej rozmowy z Ethel."
            )

    return descriptions


def runtime_disables_automatic_notes(component_documents):
    for script_name, document in component_documents:
        if script_name == "int_lv3_easyTable":
            return True
        if script_name == "Int_lv2_WoodBrickWall":
            return True
        if script_name == "lvl2_Int_Letter":
            return match_value(document, r"openNotebookNoteAfterPickup: (\d+)", "1") == "1"
        if script_name == "Int_lv3_Ethel":
            return match_value(document, r"openNotebookNoteAfterConversation: (\d+)", "1") == "1"
    return False


def main():
    scene = SCENE_PATH.read_text(encoding="utf-8-sig", errors="replace")
    documents = re.split(r"(?m)^--- ", scene)
    asset_by_guid, script_by_guid = read_guid_index()

    game_objects = {}
    components_by_game_object = defaultdict(list)
    transform_to_game_object = {}
    game_object_parent_transform = {}

    for document in documents:
        game_object_header = re.match(r"!u!1 &(-?\d+)", document)
        if game_object_header:
            game_object_id = game_object_header.group(1)
            active_match = re.search(r"(?m)^  m_IsActive: (\d+)$", document)
            source_guid = match_value(
                document,
                r"m_CorrespondingSourceObject: \{fileID: -?\d+, guid: ([0-9a-f]{32})",
            )
            fallback_name = f"GameObject_{game_object_id}"
            if source_guid in asset_by_guid:
                fallback_name = asset_by_guid[source_guid].stem
            game_objects[game_object_id] = {
                "id": game_object_id,
                "name": field(document, "m_Name", fallback_name),
                "active_self": active_match.group(1) if active_match else None,
            }
            continue

        transform_header = re.match(r"!u!(?:4|224) &(-?\d+)", document)
        if transform_header:
            transform_id = transform_header.group(1)
            game_object_id = match_value(document, r"m_GameObject: \{fileID: (-?\d+)\}")
            transform_to_game_object[transform_id] = game_object_id
            game_object_parent_transform[game_object_id] = match_value(
                document, r"m_Father: \{fileID: (-?\d+)\}", "0"
            )
            continue

        component_header = re.match(r"!u!114 &(-?\d+)", document)
        if component_header:
            game_object_id = match_value(document, r"m_GameObject: \{fileID: (-?\d+)\}")
            script_guid = match_value(document, r"m_Script: \{fileID: \d+, guid: ([0-9a-f]{32})")
            components_by_game_object[game_object_id].append({
                "id": component_header.group(1),
                "script_guid": script_guid,
                "script_name": script_by_guid.get(script_guid, script_guid or "Nieznany skrypt"),
                "enabled": match_value(document, r"m_Enabled: (\d+)", "1"),
                "document": document,
            })

    def active_in_hierarchy(game_object_id, visited=None):
        visited = visited or set()
        if game_object_id in visited:
            return False
        visited.add(game_object_id)

        game_object = game_objects.get(game_object_id)
        if game_object is None:
            return False
        if game_object["active_self"] is None:
            return None
        if game_object["active_self"] != "1":
            return False

        parent_transform = game_object_parent_transform.get(game_object_id, "0")
        if parent_transform == "0":
            return True
        parent_game_object = transform_to_game_object.get(parent_transform)
        return active_in_hierarchy(parent_game_object, visited) if parent_game_object else None

    interactions = []
    referenced_note_guids = set()
    interactable_by_component_id = {}

    for game_object_id, components in components_by_game_object.items():
        for component in components:
            if component["script_guid"] != INTERACTABLE_GUID:
                continue

            note_block = re.search(
                r"(?ms)^  databaseNotes:(.*?)(?=^  addDatabaseNotesAutomatically:)",
                component["document"],
            )
            note_guids = re.findall(r"guid: ([0-9a-f]{32})", note_block.group(1)) if note_block else []
            if not note_guids:
                continue

            referenced_note_guids.update(note_guids)
            sibling_components = [item for item in components if item is not component]
            sibling_component_documents = [
                (item["script_name"], item["document"]) for item in sibling_components
            ]
            serialized_automatic = match_value(
                component["document"], r"addDatabaseNotesAutomatically: (\d+)", "1"
            ) == "1"
            runtime_auto_disabled = runtime_disables_automatic_notes(sibling_component_documents)
            interaction = {
                "component_id": component["id"],
                "game_object_id": game_object_id,
                "name": game_objects.get(game_object_id, {}).get("name", f"GameObject_{game_object_id}"),
                "object_description": field(component["document"], "objectDescription", ""),
                "component_enabled": component["enabled"] == "1",
                "active_self": (
                    None
                    if game_objects.get(game_object_id, {}).get("active_self") is None
                    else game_objects[game_object_id]["active_self"] == "1"
                ),
                "active_hierarchy": active_in_hierarchy(game_object_id),
                "serialized_automatic": serialized_automatic,
                "runtime_auto_disabled": runtime_auto_disabled,
                "effective_automatic": serialized_automatic and not runtime_auto_disabled,
                "note_guids": note_guids,
                "scripts": [item["script_name"] for item in sibling_components],
                "custom": custom_activation_descriptions(
                    [item["script_name"] for item in sibling_components],
                    sibling_component_documents,
                ),
            }
            interactions.append(interaction)
            interactable_by_component_id[component["id"]] = interaction

    external_activations = defaultdict(list)
    for components in components_by_game_object.values():
        for component in components:
            if component["script_name"] != "DetectiveSequencePuzzle":
                continue
            source_id = match_value(component["document"], r"solvedNotebookNoteSource: \{fileID: (-?\d+)\}", "0")
            note_index = match_value(component["document"], r"solvedNotebookNoteIndex: (-?\d+)", "-1")
            if source_id in interactable_by_component_id and int(note_index) >= 0:
                external_activations[source_id].append(
                    f"`DetectiveSequencePuzzle.AddSolvedNotebookNote()` dodaje indeks `{note_index}` po rozwiązaniu puzzla IdeaPoint."
                )

    for component_id, descriptions in external_activations.items():
        interactable_by_component_id[component_id]["custom"].extend(descriptions)

    notes = {}
    missing_assets = []
    for guid in sorted(referenced_note_guids):
        asset_path = asset_by_guid.get(guid)
        if asset_path is None or not asset_path.exists():
            missing_assets.append(guid)
            continue
        notes[guid] = read_note(asset_path, guid)

    usage_by_note = defaultdict(list)
    for interaction in interactions:
        for index, guid in enumerate(interaction["note_guids"]):
            usage_by_note[guid].append((interaction["name"], index))

    note_lines = [
        "# Baza NoteData używanych przez Interactable",
        "",
        "Źródło: pola `databaseNotes` komponentów `Interactable` obecnych w `Assets/Mariusz/Levels/GameLeveL_DEV.unity`.",
        "",
        "> Treści są transkrypcją źródłową. Zachowano pisownię zapisaną w assetach NoteData.",
        "",
        f"Liczba unikalnych assetów NoteData: **{len(notes)}**",
        "",
    ]

    for index, note in enumerate(sorted(notes.values(), key=lambda item: (item["category"], item["title"], item["name"])), start=1):
        usages = ", ".join(f"`{name}` [indeks {note_index}]" for name, note_index in usage_by_note[note["guid"]])
        note_lines.extend([
            f"## {index}. {note['title'] or note['name']}",
            "",
            f"- Nazwa assetu: `{note['name']}`",
            f"- Kategoria: `{note['category']}`",
            f"- Osoba: `{note['person']}`",
            f"- Obserwacja: `{note['observation']}`",
            f"- Użycie: {usages}",
            f"- Asset: `{asset_display_path(note['path'])}`",
            "",
            "**Treść:**",
            "",
            note["content"] or "[Brak treści]",
            "",
        ])

    if missing_assets:
        note_lines.extend(["## Brakujące assety", ""])
        note_lines.extend(f"- `{guid}`" for guid in missing_assets)
        note_lines.append("")

    (OUTPUT_DIR / "01_Baza_NoteData.md").write_text("\n".join(note_lines), encoding="utf-8")

    map_lines = [
        "# Mapa aktywacji notatek przez Interactable",
        "",
        "Źródło: `Assets/Mariusz/Levels/GameLeveL_DEV.unity` oraz skrypty wywołujące `AddNote`, `AddAllDatabaseNotes` lub `AddAndOpenNote`.",
        "",
        f"Liczba Interactable z co najmniej jedną przypisaną notatką: **{len(interactions)}**",
        "",
        "## Zasada bazowa",
        "",
        "Gdy `Add Database Notes Automatically` jest włączone, `Interactable.PerformInteraction()` dodaje wszystkie elementy `databaseNotes` po obsłużeniu interakcji. `PlayerController.RotateAndPerform()` obecnie ponawia to samo wywołanie po powrocie z `PerformInteraction`; `NotebookManager` chroni listę przed dodaniem tego samego assetu drugi raz.",
        "",
        "Gdy opcja automatyczna jest wyłączona albo skrypt zmienia ją w `Start()`, moment dodania określa skrypt konkretnej mechaniki.",
        "",
    ]

    for index, interaction in enumerate(sorted(interactions, key=lambda item: item["name"].lower()), start=1):
        state = (
            f"GameObject Active Self: **{bool_text(interaction['active_self'])}**, "
            f"Active In Hierarchy: **{bool_text(interaction['active_hierarchy'])}**, "
            f"komponent Interactable włączony: **{bool_text(interaction['component_enabled'])}**"
        )
        map_lines.extend([
            f"## {index}. {interaction['name']}",
            "",
            f"- Object Description: `{interaction['object_description']}`",
            f"- Stan początkowy: {state}",
            f"- `Add Database Notes Automatically` zapisane w scenie: **{bool_text(interaction['serialized_automatic'])}**",
            f"- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **{bool_text(interaction['effective_automatic'])}**",
            f"- Skrypty na obiekcie: {', '.join(f'`{name}`' for name in interaction['scripts']) or '[brak dodatkowego MonoBehaviour]'}",
            "",
            "**Przypisane notatki:**",
            "",
        ])

        duplicates = {guid for guid in interaction["note_guids"] if interaction["note_guids"].count(guid) > 1}
        for note_index, guid in enumerate(interaction["note_guids"]):
            note = notes.get(guid)
            title = note["title"] if note else "[Brak assetu]"
            duplicate_marker = " **[DUPLIKAT W LIŚCIE]**" if guid in duplicates else ""
            map_lines.append(f"- Indeks `{note_index}`: **{title}** (`{guid}`){duplicate_marker}")

        map_lines.extend(["", "**Sposób aktywacji:**", ""])
        if interaction["effective_automatic"]:
            map_lines.append("- Wszystkie po zakończeniu standardowego `PerformInteraction()`.")
        else:
            if interaction["runtime_auto_disabled"] and interaction["serialized_automatic"]:
                map_lines.append("- Skrypt mechaniki wyłącza automatyczne dodawanie podczas `Start()`.")
            else:
                map_lines.append("- Automatyczne dodawanie jest wyłączone w danych sceny.")
        if interaction["custom"]:
            map_lines.extend(f"- {description}" for description in interaction["custom"])
        elif not interaction["effective_automatic"]:
            map_lines.append("- Nie znaleziono na tym obiekcie rozpoznanego wywołania niestandardowego; wymaga ręcznej kontroli.")
        map_lines.append("")

    (OUTPUT_DIR / "02_Mapa_Aktywacji.md").write_text("\n".join(map_lines), encoding="utf-8")

    active_count = sum(1 for interaction in interactions if interaction["active_hierarchy"] is True and interaction["component_enabled"])
    inactive_count = sum(1 for interaction in interactions if interaction["active_hierarchy"] is False or not interaction["component_enabled"])
    unknown_count = len(interactions) - active_count - inactive_count
    index_lines = [
        "# Notatki Interactable - indeks",
        "",
        "- [Baza NoteData](01_Baza_NoteData.md) - pełne treści unikalnych notatek.",
        "- [Mapa aktywacji](02_Mapa_Aktywacji.md) - Interactable, indeksy notatek i momenty ich dodawania.",
        "",
        f"Interactable z notatkami: **{len(interactions)}**  ",
        f"Aktywne w hierarchii na początku sceny: **{active_count}**  ",
        f"Nieaktywne w hierarchii lub z wyłączonym komponentem na początku sceny: **{inactive_count}**  ",
        f"Stan hierarchii nierozstrzygalny z YAML sceny (instancje modeli/prefabów): **{unknown_count}**  ",
        f"Unikalne NoteData: **{len(notes)}**",
        "",
        "Zakres nie obejmuje notatek dodawanych wyłącznie z `DialogueNotebookActions` ani notatek startowych z `CluesLog`, ponieważ nie należą do list `databaseNotes` Interactable.",
        "",
        "## Uwagi audytowe",
        "",
        "- Standardowe automatyczne dodawanie jest obecnie wywoływane zarówno na końcu `Interactable.PerformInteraction()`, jak i ponownie przez `PlayerController.RotateAndPerform()`. `NotebookManager` nie dodaje duplikatu do listy, ale wykonuje się druga próba.",
        "- `Int_lv1_HiddenWallMask` i `Int_lv2_EthelHiddenWallMask` mają automatyczne dodawanie włączone, a jednocześnie `CompleteMask()` ponownie dodaje notatki po ukończeniu wzoru. Oznacza to, że notatka może trafić do notatnika już podczas wcześniejszej interakcji.",
        "- `Int_lv1_Fireplace` zawiera ten sam asset NoteData dwukrotnie w `databaseNotes`.",
        "- `TableLoupeSymbols (off)` ma tylko indeksy `0` i `1`, natomiast `completedPuzzleAdditionalNoteIndex` wskazuje `5`, a `DetectiveSequencePuzzle` próbuje dodać indeks `2`. Oba odwołania są poza aktualną listą.",
    ]
    (OUTPUT_DIR / "00_Indeks.md").write_text("\n".join(index_lines) + "\n", encoding="utf-8")

    print(
        f"Wyeksportowano {len(notes)} NoteData z {len(interactions)} Interactable "
        f"({active_count} aktywnych, {inactive_count} nieaktywnych na początku)."
    )


if __name__ == "__main__":
    main()
