# Audyt GameLeveL_DEV

Data: 2026-06-23

## Cel Audytu

Scena `Levels/GameLeveL_DEV.unity` jest traktowana jako glowna scena gry:

- `LEVEL_1_PARTER` - parter, obecnie najwiekszy obszar do dopracowania.
- `LEVEL_2` - podpiety Golden Level jako pietro.
- `Level_2_DEV.unity` pozostaje referencja jakosci i backupem samodzielnego pietra.
- `Level_3_DEV.unity` nie jest na tym etapie analizowany jako gotowy level.

## Obraz Sceny

W scenie znaleziono dwa glowne bloki leveli:

- `LEVEL_1_PARTER`
- `LEVEL_2`

Istotne systemy obecne w scenie:

- `PlayerSwitcher`
- `PayersTopText`
- `EagleVisionVolume`
- `Canvas - Main`
- `Canvas - MagnifierGlass`
- `NavMesh Surface`
- `CinemachineCamera_Sherlock`
- `CinemachineCamera_Watson`
- `ClueManager`

## Glowny Flow Integracji

Obecny most miedzy parterem i pietrem:

1. Selma blokuje wejscie po schodach.
2. Watson moze uzyc skanowania i odciagnac Selme.
3. `Int2_WatsonScan` ustawia `stairsUp.canGoUpStairs = true`.
4. `Int_StairsUp` aktywuje `LEVEL_2`, przenosi gracza na punkt startowy pietra i wylacza `LEVEL_1_PARTER`.
5. `lvl2_Int_StairsExit` aktywuje parter i wylacza pietro przy powrocie.

To jest dobry rdzen, ale wymaga testow i kilku poprawek stabilizacyjnych.

## Interakcje Parteru

### Miejsce Zbrodni I Dowody

| Obiekt | Typ | Clue | Uwagi |
| --- | --- | ---: | --- |
| `Int_LadyEdithBody` | `ClueInteraction` | 0 | Cialo Lady Edith. Brak clue przypietego w `Interactable`, warto sprawdzic czy skrypt dodaje je inaczej. |
| `Int_Edith_BulletHole` | `Int_Edith_BulletHole` | 1 | Slad po kuli. |
| `Int_Edith_Paper` | `Int_Edith_Paper` | 1 | Biala kartka/pergamin. |
| `Int_Edith_Ring` | `Int_Edith_Ring` | 1 | Pierscien. |
| `Int_Bullet` | `Interaction` | 1 | Opis wskazuje "Uszkodzone okno", nazwa sugeruje kule. Do ujednolicenia. |
| `Int_Trajektoria` | `Interaction` | 1 | Opis wskazuje "Uszkodzone okno". |
| `Int_Kominek` | `Interaction` | 1 | Kominek. |

### Tropienie I Eagle Vision

| Obiekt | Typ | Clue | Uwagi |
| --- | --- | ---: | --- |
| `Int_FootPrints` | `Footsteps` | 1 | Slady na podlodze. |
| `Int_FootPath_Window` | `PrintPath` | 1 | Trop do okna/domu. |
| `Int_FootPath_Bonus` | `PrintPath` | 1 | "Zabawa w chowanego". |
| `Int_FootPath_LibraryRoom` | `PrintPath` | 1 | Biblioteka. |
| `Int_FootPath_FirePlaceRoom` | `PrintPath` | 1 | Pokoj kominkowy. |
| `Int_FootPath_EthelClue` | `PrintPath` | 1 | Slady/Ethel clue. |

### Sekretne Mechanizmy I Obiekty

| Obiekt | Typ | Clue | Uwagi |
| --- | --- | ---: | --- |
| `Globus` | `Globus` | 1 | Wyglada na wazna zagadke srodowiskowa. |
| `Gramofon` | `Globus` | 0 | Podejrzane: typ `Globus` przy gramofonie. Do sprawdzenia w Inspectorze. |
| `Int_EmptyWall` | `EmptyWall` | 1 | Pusta sciana. |
| `Int_HidenWall` | `HindenWall` | 1 | Sekretna sciana; nazwy do poprawy pozniej. |
| `Int_FootPath_LibraryPainting (1)` | `int_LibraryPainting` | 1 | Obraz na scianie. |
| `Int_EntranceDoor` | `Interaction` | 1 | Drzwi wejsciowe. |
| `Interactable` x2 | `Interaction` | 0 | Opis "Uklad sciany". Robocze/niejasne, do weryfikacji. |
| `Npc_01 (2)` | `Interaction` | 0 | Nazwa robocza, do weryfikacji albo usuniecia. |

### Dialogi I Quest 2

| Obiekt | Typ | Clue | Uwagi |
| --- | --- | ---: | --- |
| `Int_VioletDialog` | `VioletDialog` | 1 | Dialog parteru. |
| `Int_SelmaDialog` | `SelmaDialog` | 2 | Dialog parteru. |
| `Int_StairsUp` | `Stairs` | 0 | Glowna bramka na pietro. |
| `Int2_WatsonDialogSherlock` | `WatsonDialogSherlock` | 0 | Nieaktywny na starcie. |
| `Int2_WatsonDialogViolet` | `WatsonDialogViolet` | 0 | Nieaktywny na starcie. |
| `Int2_WatsonDialogViolet_2` | `WatsonDialogViolet_2` | 0 | Aktywny na starcie. Sprawdzic czy tak ma byc. |
| `WatsonScan` | `WatsonScan` | 0 | Mechanika odciagniecia Selmy. |

## Interakcje Pietra / Golden Level 2 W GameLeveL_DEV

| Obiekt | Typ | Clue | Uwagi |
| --- | --- | ---: | --- |
| `lvl2_Int_Book` | `Book` | 1 | Kluczyk w ksiazce. |
| `lvl2_Int_DoorEthel` | `lvl2_Int_DoorEthel` | 0 | Drzwi do pokoju Ethel. |
| `lvl2_Int_EthelWallButton` | `lvl2_Int_EthelWallButton` | 2 | Przycisk w scianie. |
| `lvl2_Int_Mirror` | `lvl2_Int_Mirror` | 0 | Szafka/lustro + lockpick. |
| `lvl2_Int_SelmaDoor` | `lvl2_Int_SelmaDoor` | 0 | Drzwi + lockpick. |
| `Int_DoorSwitcher` | `lvl2_Int_HidenDorrSwitcher` | 1 | Przekladnia; literowka w enumie. |
| `lvl2_Int_Letter_1` | `lvl2_Int_Letter` | 1 | List. |
| `lvl2_Int_Letter_2` | `lvl2_Int_Letter` | 1 | List. |
| `lvl2_Int_Letter_3` | `lvl2_Int_Letter` | 1 | List. |
| `lvl2_Int_Letter_book` | `lvl2_Int_Letter` | 1 | Notatnik. |
| `lvl2_Int_PlayBlocks` | `lvl2_Int_PlayBlock` | 1 | Klocki w pokoju Ethel. |
| `lvl2_Int_PlayBlock 1` | `lvl2_Int_PlayBlock` | 2 | Klocek. |
| `lvl2_Int_PlayBlock 2` | `lvl2_Int_PlayBlock` | 2 | Klocek. |
| `lvl2_Int_PlayBlock 3` | `lvl2_Int_PlayBlock` | 2 | Klocek. |
| `lvl2_Int_Gramophone` | `lvl2_Int_Gramophone` | 0 | Gramofon pietra. |
| `lvl2_Int_hatch` | `lvl2_Int_Hatch` | 1 | Klapa. |
| `lvl2_Int_HatchExit` | `lvl2_Int_HatchExit` | 0 | Zejscie tajnym przejsciem. |
| `lvl2_Int_ExitStairs` | `lvl2_Int_StairsExit` | 0 | Schody na dol. |
| `Czerwona Figurka` | `Int_CzerwonaFigurka` | 0 | Figurka. |
| `Zielona Figurka` | `Int_ZielonaFigurka` | 0 | Figurka. |
| `Niebieska Figurka` | `Int_NiebieskaFigurka` | 0 | Figurka. |
| `lvl2_Int_EthelRoom` | `None` | 1 | Nieaktywny na starcie. Ma clue, ale `InteractionType.None`; sprawdzic czy aktywowany przez inny skrypt. |

## Placeholdery I Teksty Do Czyszczenia

Znalezione w `GameLeveL_DEV.unity`:

- `abc`
- `Lorem ipsum dolor sit amet...`
- `Title`
- `Main Description`
- `Name.`
- `senas` zamiast `seans`
- `zajsciem` zamiast `zajściem`
- `fotografi` zamiast `fotografii`
- `spirutualizmem` zamiast `spirytualizmem`
- `w brew` zamiast `wbrew`
- `wszytsko` zamiast `wszystko`
- `prosze` zamiast `proszę`
- `najpier` zamiast `najpierw`
- `ze opiekun` zamiast `że opiekun`
- `ze Arthur` zamiast `że Arthur`
- `Klecek` / niespojna pisownia `klocek`

Znalezione w assetach/skryptach:

- `ShortDesctiption` w clue assetach listow z LVL2.
- `MAIN GAME DESCIPTION` w `MQ_00_DuchyPrzeszlosci.asset`.
- `Powiniem` w `lvl2_Int_StairsExit.cs`.
- Nazwy robocze/literowki: `Hiden`, `Dorr`, `Fitst`, `interactabePoint`, `firsInteraction`.

## Ryzyka Techniczne Do Naprawy Przed Golden Pass

1. `PlayerController.CheckInteractionArrival()` moze odpalac wiele coroutine `RotateAndPerform()` naraz.
2. `Interactable.AddClue()` nie sprawdza zakresu tablicy `clues`.
3. `ClueManager.AddClue()` nie ma `pendingClues`, wiec szybkie powtorzenie interakcji moze zdublowac powiadomienia.
4. `ClueManager.StartNextQuestFirstInteractions()` wyglada na niedokonczone i moze rzucic NullReference.
5. `lvl2_Int_StairsExit.TryExit()` po starcie `GoDownStairs()` nie robi `return`.
6. `lvl2_Int_StairsExit.lettersCollected` i `lvl2_Int_HatchExit.lettersCollected` sa `static`.
7. `WatsonEagleVisionScanner.OnTriggerExit()` loguje blad dla kazdego collidera bez `Int2_WatsonScan`.
8. Wiele skryptow Q2 zalezy od `GameObject.Find("Watson")`, `GameObject.Find("Sherlock")`, `GameObject.Find("VIOLET_NPC")`, `GameObject.Find("SELMA_NPC")`.

## Priorytet Dalszej Pracy

### Priorytet 1: Stabilizacja Core

- Zabezpieczyc wielokrotne odpalenie interakcji w `PlayerController`.
- Dodac walidacje clue indexow w `Interactable`.
- Dodac `pendingClues` w `ClueManager`.
- Poprawic `lvl2_Int_StairsExit` i liczniki listow.
- Poprawic `WatsonEagleVisionScanner.OnTriggerExit`.

### Priorytet 2: Golden Flow Parteru

- Ustalic finalna kolejnosc clue na parterze.
- Dopiac zagadke: miejsce zbrodni -> tropy -> Selma blokuje schody -> Watson odciaga Selme.
- Zweryfikowac, ktore interakcje sa potrzebne, a ktore sa robocze.
- Usunac lub nazwac poprawnie obiekty `Interactable`, `Npc_01 (2)`, gramofon z typem `Globus`.

### Priorytet 3: Cleanup Fabularny

- Usunac placeholdery.
- Poprawic literowki w tekstach UI, dialogach i clue.
- Ujednolicic nazwy: Ethel, Selma, Lady Edith, Sherlock, Watson.

### Priorytet 4: Cleanup Strukturalny

- Uporzadkowac foldery `New Folder`, `Q2`, `LVL2`.
- Oddzielic skrypty core od level-specific.
- Dopiero pozniej refaktorowac `Interactable` na czystszy model akcji/interfejsu.

