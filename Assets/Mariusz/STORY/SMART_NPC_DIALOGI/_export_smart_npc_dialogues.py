import json
import re
from pathlib import Path


SCENE_PATH = Path(__file__).resolve().parents[2] / "Levels" / "GameLeveL_DEV.unity"
OUTPUT_DIR = Path(__file__).resolve().parent

SMART_NPC_GUID = "df3c10e2b686c1a459db3eced35b6eb9"
NPC_CONVERSATION_GUID = "83189876d4d389f43b7dd2e925aa112e"


def yaml_quoted_value(value):
    value = value.strip()
    if not value.startswith('"'):
        return value

    value = re.sub(r"\\x([0-9a-fA-F]{2})", r"\\u00\1", value)
    try:
        return json.loads(value)
    except json.JSONDecodeError:
        return value.strip('"')


def yaml_json_value(document):
    match = re.search(r'(?ms)^  json: "(.*?)"\r?\n  saveVersion:', document)
    if not match:
        raise ValueError("Nie znaleziono pola json w NPCConversation.")

    wrapped_value = re.sub(r"\r?\n[ ]+", " ", match.group(1))
    wrapped_value = re.sub(r"\\x([0-9a-fA-F]{2})", r"\\u00\1", wrapped_value)
    return json.loads(json.loads('"' + wrapped_value + '"'))


def match_value(document, pattern, default=""):
    match = re.search(pattern, document, re.MULTILINE)
    return match.group(1) if match else default


def format_text(value):
    text = (value or "").replace("\r\n", "\n").replace("\r", "\n").strip()
    return text.replace("\n", "<br>")


def format_conditions(conditions):
    if not conditions:
        return "bez warunku"

    result = []
    for condition in conditions:
        name = condition.get("ParameterName", "?")
        required = condition.get("RequiredValue")
        check_type = condition.get("CheckType")
        if isinstance(required, bool):
            required = str(required).lower()
        result.append(f"`{name} = {required}` (CheckType: {check_type})")
    return ", ".join(result)


def format_actions(actions):
    if not actions:
        return "-"

    result = []
    for action in actions:
        action_type = action.get("__type", "Action").split(":", 1)[0]
        name = action.get("ParameterName")
        value = action.get("Value")
        details = action_type
        if name is not None:
            details += f" `{name}`"
        if value is not None:
            details += f" = `{str(value).lower() if isinstance(value, bool) else value}`"
        result.append(details)
    return "; ".join(result)


def format_connections(connections, node_types):
    if not connections:
        return "Koniec rozmowy"

    result = []
    for connection in connections:
        target = connection.get("NodeUID")
        target_type = node_types.get(target, "węzeł")
        conditions = format_conditions(connection.get("Conditions", []))
        result.append(f"{target_type} `#{target}` ({conditions})")
    return "; ".join(result)


def safe_filename(index, name):
    replacements = {
        "ą": "a", "ć": "c", "ę": "e", "ł": "l", "ń": "n",
        "ó": "o", "ś": "s", "ź": "z", "ż": "z",
        "Ą": "A", "Ć": "C", "Ę": "E", "Ł": "L", "Ń": "N",
        "Ó": "O", "Ś": "S", "Ź": "Z", "Ż": "Z",
    }
    normalized = "".join(replacements.get(char, char) for char in name)
    normalized = re.sub(r"[^A-Za-z0-9]+", "_", normalized).strip("_")
    return f"{index:02d}_{normalized}.md"


def build_document(conversation):
    data = conversation["data"]
    speech_nodes = data.get("SpeechNodes", [])
    option_nodes = data.get("Options", [])
    node_types = {node.get("ID"): "wypowiedź" for node in speech_nodes}
    node_types.update({node.get("ID"): "opcja" for node in option_nodes})

    usages = ", ".join(
        f"{usage['npc']} / {usage['player']}" for usage in conversation["usages"]
    )
    lines = [
        f"# {conversation['name']}",
        "",
        f"Użycie w scenie: **{usages}**  ",
        f"Źródło: `Assets/Mariusz/Levels/GameLeveL_DEV.unity`, komponent NPCConversation `{conversation['id']}`.",
        "",
        "> Transkrypcja źródłowa. Zachowano pisownię i treść zapisaną w scenie.",
        "",
        "## Parametry rozmowy",
        "",
    ]

    parameters = data.get("Parameters", [])
    if parameters:
        for parameter in parameters:
            parameter_type = parameter.get("__type", "Parameter").split(":", 1)[0]
            name = parameter.get("ParameterName", "?")
            values = [
                value for key, value in parameter.items()
                if key not in {"__type", "ParameterName"}
            ]
            value = values[0] if values else "?"
            if isinstance(value, bool):
                value = str(value).lower()
            lines.append(f"- `{name}`: `{value}` ({parameter_type})")
    else:
        lines.append("Brak parametrów.")

    lines.extend(["", "## Wypowiedzi", ""])
    for node in sorted(speech_nodes, key=lambda item: item.get("ID", -1)):
        node_id = node.get("ID")
        root = " [ROOT]" if node.get("EditorInfo", {}).get("isRoot") else ""
        speaker = node.get("Name") or "Brak nazwy mówcy"
        lines.extend([
            f"### Wypowiedź #{node_id}{root}",
            "",
            f"**{speaker}:** {format_text(node.get('Text'))}",
            "",
            f"Akcje: {format_actions(node.get('ParamActions', []))}  ",
            f"Dalej: {format_connections(node.get('Connections', []), node_types)}",
            "",
        ])

    lines.extend(["## Opcje gracza", ""])
    if not option_nodes:
        lines.extend(["Brak opcji gracza.", ""])
    else:
        for node in sorted(option_nodes, key=lambda item: item.get("ID", -1)):
            node_id = node.get("ID")
            lines.extend([
                f"### Opcja #{node_id}",
                "",
                f"**Tekst:** {format_text(node.get('Text'))}",
                "",
                f"Akcje: {format_actions(node.get('ParamActions', []))}  ",
                f"Dalej: {format_connections(node.get('Connections', []), node_types)}",
                "",
            ])

    return "\n".join(lines).rstrip() + "\n"


def main():
    scene = SCENE_PATH.read_text(encoding="utf-8-sig")
    documents = re.split(r"(?m)^--- ", scene)

    game_object_names = {}
    dialogue_documents = {}
    smart_npcs = []

    for document in documents:
        header = re.match(r"!u!1 &(-?\d+)", document)
        if not header:
            continue
        name_match = re.search(r"(?m)^  m_Name: (.*)$", document)
        if name_match:
            game_object_names[header.group(1)] = yaml_quoted_value(name_match.group(1))

    for document in documents:
        header = re.match(r"!u!114 &(-?\d+)", document)
        if not header:
            continue

        component_id = header.group(1)
        game_object_id = match_value(document, r"m_GameObject: \{fileID: (-?\d+)\}")
        if NPC_CONVERSATION_GUID in document:
            if not re.search(r'(?m)^  json: "', document):
                continue
            dialogue_documents[component_id] = {
                "id": component_id,
                "name": game_object_names.get(game_object_id, f"NPCConversation_{component_id}"),
                "data": yaml_json_value(document),
            }
        elif SMART_NPC_GUID in document and match_value(document, r"m_Enabled: (\d+)") == "1":
            smart_npcs.append({
                "name": game_object_names.get(game_object_id, f"SmartNPC_{component_id}"),
                "npc_id": match_value(document, r"npcID: (-?\d+)"),
                "sherlock": match_value(document, r"rozmowaDlaPostaciA: \{fileID: (-?\d+)"),
                "watson": match_value(document, r"rozmowaDlaPostaciB: \{fileID: (-?\d+)"),
            })

    used = {}
    for npc in smart_npcs:
        for player_key, player_name in (("sherlock", "Sherlock"), ("watson", "Watson")):
            conversation_id = npc[player_key]
            if not conversation_id or conversation_id == "0":
                continue
            if conversation_id not in dialogue_documents:
                raise ValueError(
                    f"SmartNPC {npc['name']} odwołuje się do brakującej rozmowy {conversation_id}."
                )
            conversation = used.setdefault(conversation_id, dialogue_documents[conversation_id])
            conversation.setdefault("usages", []).append({
                "npc": npc["name"],
                "npc_id": npc["npc_id"],
                "player": player_name,
            })

    conversations = sorted(
        used.values(),
        key=lambda item: (item["usages"][0]["npc"], item["usages"][0]["player"], item["name"]),
    )

    generated_files = []
    for index, conversation in enumerate(conversations, start=1):
        filename = safe_filename(index, conversation["name"])
        (OUTPUT_DIR / filename).write_text(build_document(conversation), encoding="utf-8")
        generated_files.append((filename, conversation))

    index_lines = [
        "# Dialogi Smart NPC - indeks",
        "",
        "Źródło: aktywne komponenty `SmartNPC` w `Assets/Mariusz/Levels/GameLeveL_DEV.unity`.",
        "",
        "Zestawienie obejmuje wyłącznie rozmowy przypisane w polach `rozmowaDlaPostaciA` (Sherlock) i `rozmowaDlaPostaciB` (Watson). Nie obejmuje DialogLine ani rozmów uruchamianych bezpośrednio poza SmartNPC.",
        "",
        f"Liczba aktywnych Smart NPC z przypisaną rozmową: **{len([npc for npc in smart_npcs if npc['sherlock'] != '0' or npc['watson'] != '0'])}**  ",
        f"Liczba unikalnych konwersacji: **{len(conversations)}**",
        "",
        "## Konwersacje",
        "",
    ]

    for filename, conversation in generated_files:
        usages = ", ".join(
            f"{usage['npc']} / {usage['player']}" for usage in conversation["usages"]
        )
        index_lines.append(f"- [{conversation['name']}]({filename}) - {usages}")

    index_lines.extend([
        "",
        "## Smart NPC bez rozmów",
        "",
    ])
    empty_npcs = [npc for npc in smart_npcs if npc["sherlock"] == "0" and npc["watson"] == "0"]
    if empty_npcs:
        for npc in empty_npcs:
            index_lines.append(f"- `{npc['name']}` (npcID: `{npc['npc_id']}`)")
    else:
        index_lines.append("Brak.")

    (OUTPUT_DIR / "00_Indeks.md").write_text("\n".join(index_lines) + "\n", encoding="utf-8")
    print(f"Wyeksportowano {len(conversations)} rozmów do {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
