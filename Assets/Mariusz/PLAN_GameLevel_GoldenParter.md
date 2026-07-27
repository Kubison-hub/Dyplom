# GameLeveL_DEV - Golden Parter

Ten plik jest robocza mapa Etapu 2. Celem jest doprowadzenie `LEVEL_1_PARTER` w `GameLeveL_DEV` do poziomu Golden Level: gracz ma rozumiec, co bada, dlaczego kolejne interakcje sie odblokowuja i kiedy moze przejsc na pietro.

## Cel parteru

Parter ma byc pierwszym pelnym sledztwem Sherlocka:

1. Ogladamy cialo Lady Edith i uruchamiamy glowne sledztwo.
2. Zbieramy dowody dotyczace strzalu: rana, kula, trajektoria, kominek/swiatlo, zamkniete drzwi.
3. Odkrywamy drugi trop: slady malej Ethel prowadza po domu.
4. Slady i rozmowy kieruja gracza do biblioteki, obrazu i globusa.
5. Mechanizm globusa przesuwa obraz lub ujawnia sekret.
6. Selma blokuje wejscie na gore, wiec Watson musi ja odciagnac.
7. Po odciagnieciu Selmy `Int_StairsUp` przenosi gracza na `LEVEL_2`.

## Golden Path

### 1. Miejsce zbrodni

Interakcje: `Int_LadyEdithBody`, `Int_Edith_BulletHole`, `Int_Edith_Paper`, `Int_Edith_Ring`, `Int_Bullet`, `Int_Trajektoria`, `Int_Kominek`, `Int_EntranceDoor`.

Oczekiwany efekt:
- gracz dostaje komplet podstawowych clue o zabojstwie,
- kolejne interakcje nie powtarzaja sie bez potrzeby,
- dowody sugeruja, ze sprawa nie jest zwyklym napadem.

### 2. Drugi trop

Interakcje: `Int_FootPrints`, `Int_FootPath_*`, `Int_EmptyWall`, `Int_HidenWall`.

Oczekiwany efekt:
- pierwsze slady odblokowuja tryb tropienia,
- sciezki sa czytelne tylko wtedy, gdy ma to sens mechaniczny,
- gracz dostaje trop Ethel i ukrytej czesci domu.

### 3. Biblioteka i mechanizm

Interakcje: `Int_FootPath_LibraryRoom`, `Int_FootPath_LibraryPainting (1)`, `Globus`.

Oczekiwany efekt:
- trop prowadzi gracza do biblioteki,
- globus komunikuje, ze ma mechanizm,
- drugie uzycie globusa uruchamia lockpick,
- sukces przesuwa obraz lub odslania sekret.

### 4. Blokada pietra

Interakcje: `Int_SelmaDialog`, `Int_VioletDialog`, `WatsonScan`, `Int_StairsUp`.

Oczekiwany efekt:
- Selma jasno blokuje schody,
- Watson moze odciagnac Selme po znalezieniu odpowiedniego tropu,
- gracz moze wejsc na pietro tylko w oknie, gdy Selma jest odciagnieta,
- po wejsciu na pietro scena zachowuje stan parteru.

## Do zrobienia

- [ ] Uporzadkowac opisy clue parteru i usunac placeholdery.
- [ ] Poprawic tekst tutoriala sladow w `int_Footsteps.cs`; plik ma obecnie kodowanie, ktore wymaga ostroznej konwersji przed edycja patchem.
- [ ] Zabezpieczyc `Int2_WatsonDialogSherlock.cs`; plik ma obecnie kodowanie, ktore wymaga ostroznej konwersji przed edycja patchem.
- [ ] Sprawdzic, czy kazda interakcja parteru ma poprawny `InteractionType`, `clues[]`, `cardPosition` i VFX.
- [ ] Ujednolicic nazwy obiektow: szczegolnie `Gramofon` z typem `Globus`, `Hiden`, `New Folder`.
- [ ] Potwierdzic, ktore interakcje powinny byc jednorazowe, a ktore powtarzalne.
- [ ] Domknac logike przejscia parter -> pietro -> parter.
- [ ] Zrobic playtest checklist dla przejscia od startu sceny do wejscia na pietro.

## Akceptacja Etapu 2

Etap 2 uznajemy za gotowy, gdy:

- gracz moze przejsc parter bez znajomosci projektu,
- kazda wazna interakcja daje sensowny feedback,
- nie ma placeholderow w widocznym UI,
- przejscie na pietro dziala stabilnie,
- parter zostawia dobry stan gry po powrocie z pietra.

## Rekonstrukcja Tajnego Przejscia

Po rozwiazaniu zagadki punktow idei Sherlock ma zobaczyc rekonstrukcje ruchu sekretnej sciany tylko w Vision Eye. Nie zmienia to fizycznego ukladu levelu ani colliderow.

### Przygotowanie w Hierarchy

Utworz root `HiddenPassage` i ustaw go jako nieaktywny na starcie. Pod nim przygotuj:

- `GhostWall` - kopie tajnej sciany z polprzezroczystym materialem, ustawiona poczatkowo w pozycji zamknietej, zgodnej z prawdziwa sciana;
- `PassageOutline` - opcjonalny zarys wejscia, schodow lub fragmentu przestrzeni za sciana;
- opcjonalne elementy narracyjne, np. zarys regalu albo slad kurzu.

`GhostWall` i `PassageOutline` ustaw na warstwie `Hiden`. Nie dodawaj do nich colliderow. Prawdziwa sciana pozostaje na warstwie `Walls` wraz z obecnym colliderem.

Na root `HiddenPassage` dodaj `HiddenPassageVisionReveal`. Do `Required Puzzle` przypisz ten sam `DetectiveSequencePuzzle`, ktory rozwiazuje zagadke idei. Do `Real Wall Renderers` przypisz wszystkie renderery prawdziwej przesuwalnej sciany. Dodaj Animator do `GhostWall`, przypisz go do `Ghost Wall Animator` i utworz w jego Animator Controller parametr bool `VisionActive`. Animator ma przechodzic z pozycji zamknietej do odsunietej po ustawieniu tego parametru na `true`.

### Polaczenie Z Puzzle

Po sukcesie `DetectiveSequencePuzzle` root `HiddenPassage` zostanie dodany do listy `activateOnSolved`. Aktywuje to przygotowana rekonstrukcje, ale sama widocznosc rendererow bedzie pozniej sterowana przez stan Vision Eye:

- Vision Eye aktywne: ghost i przejscie sa widoczne;
- Vision Eye nieaktywne: ghost i przejscie sa ukryte;
- fizyczna sciana nadal pozostaje w levelu i blokuje wejscie do czasu osobnej zagadki otwierajacej przejscie.

### Warstwy I Kamery

- `Walls` - widoczne dla glownej kamery;
- `Hiden` - widoczne dla kamery nakladkowej Vision Eye oraz `HiddenCluesCamera`;
- `WorldText` - zarezerwowane dla punktow idei i ich linii.

## Tutorial Timeline

`TutorialTimeline` prowadzi pierwszy etap sterowania po zamknieciu tutoriala startowego. Nie wymaga zmian w `TutorialManager`; czeka na jego istniejaca blokade `BlocksWorldInput`, wiec klik zamykajacy okno nie jest liczony jako klik ruchu.

### Konfiguracja

- dodaj `TutorialTimeline` do stalego obiektu systemowego w scenie;
- w `Tutorial Cameras` przypisz oba komponenty `CameraController` Sherlocka i Watsona;
- w `Tutorial Players` przypisz oba komponenty `PlayerController` Sherlocka i Watsona; timeline zatrzyma NavMesh podczas otwartego tutoriala i zwolni ruch dopiero po puszczeniu klikniecia zamykajacego okno;
- po zwolnieniu klikniecia zamykajacego tutorial kamery przechodza na `Narrow`;
- pierwszy pozniejszy klik poza UI ustawia kamery na `Medium`.

### Rozszerzanie

Do zdarzen `On Timeline Started`, `On First World Click` i `On Timeline Completed` mozna w Inspectorze podpiac aktywowanie obiektow, zadania questowe, dialogi lub kolejne tutoriale. Kolejne etapy powinny byc dodawane do tej klasy zamiast rozszerzac `TutorialManager` o logike gameplayu.
