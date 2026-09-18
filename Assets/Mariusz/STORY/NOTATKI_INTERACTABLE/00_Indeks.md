# Notatki Interactable - indeks

- [Baza NoteData](01_Baza_NoteData.md) - pełne treści unikalnych notatek.
- [Mapa aktywacji](02_Mapa_Aktywacji.md) - Interactable, indeksy notatek i momenty ich dodawania.

Interactable z notatkami: **29**  
Aktywne w hierarchii na początku sceny: **18**  
Nieaktywne w hierarchii lub z wyłączonym komponentem na początku sceny: **8**  
Stan hierarchii nierozstrzygalny z YAML sceny (instancje modeli/prefabów): **3**  
Unikalne NoteData: **30**

Zakres nie obejmuje notatek dodawanych wyłącznie z `DialogueNotebookActions` ani notatek startowych z `CluesLog`, ponieważ nie należą do list `databaseNotes` Interactable.

## Uwagi audytowe

- Standardowe automatyczne dodawanie jest obecnie wywoływane zarówno na końcu `Interactable.PerformInteraction()`, jak i ponownie przez `PlayerController.RotateAndPerform()`. `NotebookManager` nie dodaje duplikatu do listy, ale wykonuje się druga próba.
- `Int_lv1_HiddenWallMask` i `Int_lv2_EthelHiddenWallMask` mają automatyczne dodawanie włączone, a jednocześnie `CompleteMask()` ponownie dodaje notatki po ukończeniu wzoru. Oznacza to, że notatka może trafić do notatnika już podczas wcześniejszej interakcji.
- `Int_lv1_Fireplace` zawiera ten sam asset NoteData dwukrotnie w `databaseNotes`.
- `TableLoupeSymbols (off)` ma tylko indeksy `0` i `1`, natomiast `completedPuzzleAdditionalNoteIndex` wskazuje `5`, a `DetectiveSequencePuzzle` próbuje dodać indeks `2`. Oba odwołania są poza aktualną listą.
