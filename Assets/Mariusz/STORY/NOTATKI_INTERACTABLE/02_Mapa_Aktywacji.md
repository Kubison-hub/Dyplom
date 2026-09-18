# Mapa aktywacji notatek przez Interactable

Źródło: `Assets/Mariusz/Levels/GameLeveL_DEV.unity` oraz skrypty wywołujące `AddNote`, `AddAllDatabaseNotes` lub `AddAndOpenNote`.

Liczba Interactable z co najmniej jedną przypisaną notatką: **29**

## Zasada bazowa

Gdy `Add Database Notes Automatically` jest włączone, `Interactable.PerformInteraction()` dodaje wszystkie elementy `databaseNotes` po obsłużeniu interakcji. `PlayerController.RotateAndPerform()` obecnie ponawia to samo wywołanie po powrocie z `PerformInteraction`; `NotebookManager` chroni listę przed dodaniem tego samego assetu drugi raz.

Gdy opcja automatyczna jest wyłączona albo skrypt zmienia ją w `Start()`, moment dodania określa skrypt konkretnej mechaniki.

## 1. BrickPillar_01 (65)

- Object Description: `Drzwi na zewnątrz`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **nie**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_lv3_ExitDoor`

**Przypisane notatki:**

- Indeks `0`: **Ethel to...** (`398c501f03e178c4ab22c139ec40f21f`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 2. Chest

- Object Description: `Drewniane pudełko`
- Stan początkowy: GameObject Active Self: **nieustalone (obiekt pochodzi z instancji modelu/prefabu)**, Active In Hierarchy: **nieustalone (obiekt pochodzi z instancji modelu/prefabu)**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_lv3_SmallBox`

**Przypisane notatki:**

- Indeks `0`: **Drewniane pudełko** (`9843c42be0d82864093d2786273e506f`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 3. Ethel_NPC

- Object Description: `Ethel`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **nie**
- Skrypty na obiekcie: `SmartNPC`, `Int_lv3_Ethel`, `b2d8418b0b9634b1892b0268dd9c2743`, `fff0960ef4ea6e04eac66b4a7fd2189d`

**Przypisane notatki:**

- Indeks `0`: **List** (`9e96fbc0bacf8fb48998915060f91d46`)

**Sposób aktywacji:**

- Skrypt mechaniki wyłącza automatyczne dodawanie podczas `Start()`.
- `Int_lv3_Ethel` dodaje i natychmiast otwiera notatkę o indeksie `0` po zakończeniu pierwszej rozmowy z Ethel.

## 4. Globus_Button

- Object Description: `Przycisk`
- Stan początkowy: GameObject Active Self: **nieustalone (obiekt pochodzi z instancji modelu/prefabu)**, Active In Hierarchy: **nieustalone (obiekt pochodzi z instancji modelu/prefabu)**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `LockPickAudioController`, `int_Globus_Button`

**Przypisane notatki:**

- Indeks `0`: **Portret królowej Wiktorii** (`00a7c424532d6ec47832ea75592bca71`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 5. Int_Edith_BulletHole

- Object Description: `Slad po kuli`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **nie**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **nie**
- Skrypty na obiekcie: `Int_Edith_BulletHole`

**Przypisane notatki:**

- Indeks `0`: **Jak zginęła Lady Edith?** (`6487a92cc2b7b31419e0505a8539c6c1`)

**Sposób aktywacji:**

- Automatyczne dodawanie jest wyłączone w danych sceny.
- `Int_Edith_BulletHole.CompleteSuccessfulExamination()` dodaje notatkę o indeksie `0` dopiero po udanej examinacji.

## 6. Int_Edith_Paper

- Object Description: `Biała Kartka`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_Edith_Paper`

**Przypisane notatki:**

- Indeks `0`: **Pusty Pergamin** (`7ec57c0f8f8d2b34b8eea891afd92e9f`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 7. Int_Edith_Ring

- Object Description: `Pierścień`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_Edith_Ring`

**Przypisane notatki:**

- Indeks `0`: **Pierścień z inicjałem.** (`88a57585aafb5db47a893b4a9d3af789`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 8. Int_FootPath_LibraryPainting (1)

- Object Description: `Obraz na ścianie`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `int_LibraryPainting`

**Przypisane notatki:**

- Indeks `0`: **Portret królowej Wiktorii** (`d5af257b0f671ae45ba003d3ffcacd4c`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 9. Int_lv1_CircleTable

- Object Description: `Stół`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_lv1_CircleTable`

**Przypisane notatki:**

- Indeks `0`: **okrągły stół** (`4a3426f22a6e0c54080d63778b280d19`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 10. Int_lv1_Fireplace

- Object Description: `Kominek`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_lv1_Fireplace`

**Przypisane notatki:**

- Indeks `0`: **Światło z kominka** (`20614c6d08b5278458b516597cbdd6b3`) **[DUPLIKAT W LIŚCIE]**
- Indeks `1`: **Światło z kominka** (`20614c6d08b5278458b516597cbdd6b3`) **[DUPLIKAT W LIŚCIE]**

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 11. Int_lv1_HiddenWallMask

- Object Description: `Ściana`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_lv1_HidenWallMask`

**Przypisane notatki:**

- Indeks `0`: **Podejrzana ściana.** (`fcac1155960896d4f83d94d1363f4698`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.
- `Int_lv1_HidenWallMask.CompleteMask()` dodaje wszystkie notatki po odkryciu całego wzoru przez LineRenderery.

## 12. Int_lv1_lamp

- Object Description: `Światło`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_lv1_lamp`, `DetectiveIdeaPoint`

**Przypisane notatki:**

- Indeks `0`: **Zgaszone światło.** (`1be9d511693f9d24ebfe1dfaf717fd11`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 13. Int_lv1_LibraryBooks

- Object Description: `Biblioteka`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_lv1_LibraryBooks`

**Przypisane notatki:**

- Indeks `0`: **Biblioteka** (`1a243b6867b97ad45aaf6401ed8706da`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 14. Int_lv1_WoodBlockButton

- Object Description: `Drewniany klocek`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **nie**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_lv1_WoodBlockButton`

**Przypisane notatki:**

- Indeks `0`: **wytapetowane drewnienko** (`c311fad2204b084498c4f1d59de92ec0`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 15. Int_lv2_EthelHiddenWallMask

- Object Description: `Ściana`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_lv1_HidenWallMask`

**Przypisane notatki:**

- Indeks `0`: **ukryte przejscie** (`10f5fb9968b82f74a9185a258619751c`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.
- `Int_lv1_HidenWallMask.CompleteMask()` dodaje wszystkie notatki po odkryciu całego wzoru przez LineRenderery.

## 16. Int_lv2_WoodBlockButton

- Object Description: `Drewniany klocek`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_lv2_WoodBlockButton`

**Przypisane notatki:**

- Indeks `0`: **wytapetowane drewnienko** (`da3a3c21334d9f24eaa6c9be4b63c3ab`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 17. Int_lv2_WoodBrickWall

- Object Description: `Wnęka w ścianie`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **nie**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **nie**
- Skrypty na obiekcie: `Int_lv2_WoodBrickWall`

**Przypisane notatki:**

- Indeks `0`: **Przycisk na ścianie (po przekładni)** (`bad59528efc0aaf4693ef6c9df426654`)

**Sposób aktywacji:**

- Skrypt mechaniki wyłącza automatyczne dodawanie podczas `Start()`.
- `Int_lv2_WoodBrickWall.AddOpenedDoorNotebookNote()` dodaje notatkę o indeksie `0` po poprawnym otwarciu drzwi.

## 18. Int_lv3_ClockClue

- Object Description: `Stary zegar`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **nie**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_lv3_ClockClue`

**Przypisane notatki:**

- Indeks `0`: **Stary Zegar** (`70f059af09eff4646ad090c4783552ca`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 19. Int_lv3_Control Unit

- Object Description: `Mechanizm dźwigni`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **nie**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_lv3_ControlUnit`

**Przypisane notatki:**

- Indeks `0`: **Dźwignie** (`37c47095cf3529f408fedd9c8f510559`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 20. Int_lv3_Manequine

- Object Description: `Manekin`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **nie**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_lv3_Manequine`

**Przypisane notatki:**

- Indeks `0`: **Manekin** (`60e1c4549f4cf0041b9280ce07ee7ae7`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 21. Int_StairsUp (BLOKADA)

- Object Description: `Schody na górę`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_StairsUp`

**Przypisane notatki:**

- Indeks `0`: **Schody ** (`4fc72b43519a0234583f231fec0b048c`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 22. IntLibraryKey vis

- Object Description: `Kluczyk`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_LibraryKey`, `LockPickAudioController`

**Przypisane notatki:**

- Indeks `0`: **kluczyk w bibliotece** (`a87c1a807b1d2ea4895cbea11b6019e3`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 23. lvl2_Int_EthelRoom

- Object Description: ``
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **nie**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `lvl2_Int_EthelRoom`

**Przypisane notatki:**

- Indeks `0`: **Gdzie jest Ethel?** (`c1eb8eab7d263fe43b1d9d11b608b9d5`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 24. lvl2_Int_Letter_1

- Object Description: `List`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **nie**
- Skrypty na obiekcie: `lvl2_Int_Letter`

**Przypisane notatki:**

- Indeks `0`: **List na biurku** (`337f4dd9278e30146bbf0f05c6dd12fd`)

**Sposób aktywacji:**

- Skrypt mechaniki wyłącza automatyczne dodawanie podczas `Start()`.
- `lvl2_Int_Letter` dodaje i natychmiast otwiera notatkę o indeksie `0` przy podniesieniu listu.

## 25. lvl2_Int_Letter_2

- Object Description: `List`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **nie**
- Skrypty na obiekcie: `lvl2_Int_Letter`

**Przypisane notatki:**

- Indeks `0`: **Notatki na toaletce** (`a8c01ebbb690ec049ba9eb512720a2de`)

**Sposób aktywacji:**

- Skrypt mechaniki wyłącza automatyczne dodawanie podczas `Start()`.
- `lvl2_Int_Letter` dodaje i natychmiast otwiera notatkę o indeksie `0` przy podniesieniu listu.

## 26. lvl2_Int_Letter_3

- Object Description: `List`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **nie**
- Skrypty na obiekcie: `lvl2_Int_Letter`

**Przypisane notatki:**

- Indeks `0`: **Nadpalony list w kominku** (`284923c8524e88b4c9c9ae0a873ca560`)

**Sposób aktywacji:**

- Skrypt mechaniki wyłącza automatyczne dodawanie podczas `Start()`.
- `lvl2_Int_Letter` dodaje i natychmiast otwiera notatkę o indeksie `0` przy podniesieniu listu.

## 27. lvl2_Int_Letter_book

- Object Description: `Notatnik`
- Stan początkowy: GameObject Active Self: **tak**, Active In Hierarchy: **tak**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **nie**
- Skrypty na obiekcie: `lvl2_Int_Letter`

**Przypisane notatki:**

- Indeks `0`: **Notatnik** (`a0a0bc7bccb01e54c9162a9b5f67a8b5`)

**Sposób aktywacji:**

- Skrypt mechaniki wyłącza automatyczne dodawanie podczas `Start()`.
- `lvl2_Int_Letter` dodaje i natychmiast otwiera notatkę o indeksie `0` przy podniesieniu listu.

## 28. Otwarta książka

- Object Description: `Pozostawiona książka`
- Stan początkowy: GameObject Active Self: **nieustalone (obiekt pochodzi z instancji modelu/prefabu)**, Active In Hierarchy: **nieustalone (obiekt pochodzi z instancji modelu/prefabu)**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **tak**
- Skrypty na obiekcie: `Int_LibraryBook`

**Przypisane notatki:**

- Indeks `0`: **Historia Wielkiej Brytanii** (`b94e77a4c3024724984e9d41a47d2fd1`)

**Sposób aktywacji:**

- Wszystkie po zakończeniu standardowego `PerformInteraction()`.

## 29. TableLoupeSymbols (off)

- Object Description: `Stół`
- Stan początkowy: GameObject Active Self: **nie**, Active In Hierarchy: **nie**, komponent Interactable włączony: **tak**
- `Add Database Notes Automatically` zapisane w scenie: **tak**
- Automatyczne dodawanie po uwzględnieniu `Start()` skryptów: **nie**
- Skrypty na obiekcie: `int_lv3_easyTable`

**Przypisane notatki:**

- Indeks `0`: **Mechanizm w stole** (`7c0d8ee6f8ba19c43818e007d198ded1`)
- Indeks `1`: **Mechanizm w stole** (`b1b560e6dfbfc6f469aac55222405b9b`)

**Sposób aktywacji:**

- Skrypt mechaniki wyłącza automatyczne dodawanie podczas `Start()`.
- `int_lv3_easyTable` używa indeksów: pierwsza interakcja `0`, ukończenie `1`, dodatkowa po ukończeniu `5`.
- `DetectiveSequencePuzzle.AddSolvedNotebookNote()` dodaje indeks `2` po rozwiązaniu puzzla IdeaPoint.
