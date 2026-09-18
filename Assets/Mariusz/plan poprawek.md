# Plan poprawek po uwagach promotora

## Cel

Najważniejszym celem jest odzyskanie zaufania gracza do działania gry. Gracz powinien zawsze rozumieć:

- co może zrobić,
- czy wykonana akcja zadziałała,
- jaki jest jej rezultat,
- co powinien zrobić dalej.

Uwagi promotora mogą częściowo dotyczyć starszego buildu. Przed rozpoczęciem poprawek każdą uwagę należy oznaczyć jednym ze statusów:

- **Nadal występuje** - błąd można odtworzyć w aktualnym buildzie.
- **Już naprawione** - problem został usunięty po testowanym buildzie.
- **Nieczytelne** - mechanika działa, ale gracz nie rozumie jej zasad lub rezultatu.
- **Decyzja projektowa** - potrzebna jest zmiana albo świadome pozostawienie rozwiązania.

## Etap 1. Audyt aktualnego buildu

Przeprowadzić pełne przejście gry od początku do końca na nowym zapisie.

Podczas testu zapisywać dla każdego problemu:

- akt i miejsce,
- używaną postać,
- dokładną kolejność działań,
- oczekiwany rezultat,
- rzeczywisty rezultat,
- możliwość powtórzenia problemu,
- status według powyższej klasyfikacji.

Szczególnie sprawdzić:

- przejście pomiędzy wszystkimi aktami,
- możliwość zmiany postaci,
- zapis i wczytywanie,
- wymagane zagadki,
- aktualizowanie Quest Logu i notatnika,
- blokowanie sterowania przez wszystkie panele UI.

## Etap 2. Krytyczna ścieżka gry

Najpierw usunąć problemy, które mogą zatrzymać rozgrywkę lub stworzyć fałszywy ślepy zaułek:

- niezrozumiałe zakończenie Aktu I,
- brak możliwości przełączenia na Watsona po komunikacie gry,
- możliwość przejścia dalej bez rozwiązania puzzla IdeaPoint w piwnicy,
- niedziałające lub zabugowane drzwi,
- niepewne działanie zapisu i wczytywania,
- brak jednoznacznego celu po otwarciu tajnego przejścia,
- niepoprawne aktywowanie kolejnych interakcji.

Kryterium ukończenia: pełne przejście gry jest możliwe bez restartu, walkthrough i ręcznego obchodzenia błędów.

## Etap 3. UI, sterowanie i kamera

Naprawić sytuacje, w których jeden system przyjmuje input przeznaczony dla innego:

- menu opcji nie może nakładać się z notatnikiem,
- kliknięcie w UI nie może wysyłać postaci do świata,
- podczas ustawiania kodu sejfu kółko myszy nie może sterować kamerą,
- podczas inventory nie można otwierać menu w tle,
- Tooltip i Interaction Shader muszą respektować dostęp aktywnej postaci,
- kamera musi kończyć blend i poprawnie kotwiczyć się na celu,
- lupa nie może pozwalać na ekstremalne oddalanie ani oglądanie przez ściany.

Kryterium ukończenia: każde wejście w UI, dialog lub minigrę jednoznacznie przejmuje właściwy input i oddaje go po zamknięciu.

## Etap 4. Czytelność interakcji i POI

Ujednolicić sposób informowania gracza o istotnych obiektach:

- QuestionFX powinny pojawiać się niezawodnie,
- pocisk i pergamin muszą być łatwiejsze do zauważenia,
- obraz związany z pergaminem powinien otrzymać właściwe oznaczenie,
- gracz musi wiedzieć, czy klocek istnieje, został zebrany lub już użyty,
- usunąć teksty zastępcze, np. „Wpisz nazwę obiektu”,
- pojawienie i zniknięcie obiektów powinno mieć czytelny powód i potwierdzenie,
- interakcja niedostępna dla Watsona nie może pokazywać mu Tooltipa ani shadera.

Każda ważna interakcja powinna mieć cykl:

1. Gracz zauważa obiekt.
2. Rozumie, że obiekt jest istotny.
3. Wykonuje działanie.
4. Otrzymuje jednoznaczne potwierdzenie rezultatu.
5. Wie, jaki jest następny krok.

## Etap 5. Role mechanik detektywistycznych

Nadać każdemu systemowi osobną, zrozumiałą funkcję:

- **Tryb skupienia** - wskazuje pobliskie miejsca warte zbadania.
- **Lupa** - służy do dokładnego badania konkretnych powierzchni i śladów.
- **IdeaPointy** - reprezentują fakty oraz pozwalają formułować wnioski.
- **Notatnik** - przechowuje dowody, zeznania i tok rozumowania Sherlocka.
- **Quest Log** - pokazuje bieżący cel i najbliższe kroki.

Do rozważenia dla lupy:

- ograniczenie działania do przygotowanych obszarów,
- wyraźne wejście w tryb badania,
- zablokowanie detekcji przez ściany,
- jasne podsumowanie odkrytego śladu i jego znaczenia.

Kryterium ukończenia: gracz potrafi jednym zdaniem wyjaśnić, do czego służy każdy z tych systemów.

## Etap 6. Dedukcja i podsumowania

IdeaPointy powinny prowadzić do konkretnych wniosków, a nie tylko do abstrakcyjnego łączenia elementów.

Każde rozwiązanie powinno przedstawiać:

- obserwację,
- możliwą hipotezę,
- wyeliminowane wyjaśnienia,
- ostateczny wniosek Sherlocka,
- wpływ wniosku na dalsze śledztwo.

Po puzzlach na parterze i w piwnicy dodać wyraźne podsumowanie tego, co Sherlock ustalił. Sprawdzić również, czy rozwiązanie puzzla w piwnicy jest wymagane do dalszego przejścia.

## Etap 7. Logika fabularna i immersja

Przeanalizować i poprawić najbardziej widoczne niekonsekwencje:

- Watson nie powinien rozpoznawać rany wylotowej bez wiarygodnego badania ciała.
- Tajne przejście po Akcie I musi dawać rezultat, wskazówkę albo nową drogę.
- Kod sejfu za obrazem wymaga fabularnego uzasadnienia.
- Klocki powinny wyglądać i działać jak części mechanizmu stołu.
- Lockpicking powinien pełnić konkretną funkcję albo zostać ograniczony/usunięty.
- Pojawienie się Watsona z tajnego przejścia musi wynikać z wcześniejszej akcji.
- Teksty blokujące przejście muszą zgadzać się z aktualnym stanem zadania.

## Etap 8. Rola Watsona

Wzmocnić istniejące zastosowania Watsona bez przebudowy całej gry:

- osobne dialogi i informacje,
- medyczne badanie Lady Edith,
- eskortowanie i odciąganie świadków,
- przenoszenie obiektów,
- współpraca przy mechanizmach w piwnicy,
- jasne wskazanie momentów, w których zmiana postaci jest potrzebna.

Osobno przeanalizować czytelność eskorty:

- rozpoczęcie przez `F`,
- wybór osoby,
- wybór miejsca,
- informacja o poprawnym i niepoprawnym miejscu,
- zakończenie eskorty i dalszy cel.

## Etap 9. Quest Log i notatnik

Quest Log powinien pokazywać tylko aktualne, istotne cele. Nie powinien wyglądać jak stale kurcząca się lista całej gry.

Do sprawdzenia:

- spójny poziom szczegółowości celów,
- usuwanie nieaktualnych celów,
- zgodność celów z aktualnym aktem,
- widoczna reakcja na zmianę stanu zadania,
- przydatność notatek w podejmowaniu decyzji,
- powiązanie zeznań i obserwacji z dedukcjami.

Notatnik powinien być czymś więcej niż archiwum. Powinien pomagać przypomnieć sobie fakty potrzebne do rozmów, zagadek i końcowej dedukcji.

## Etap 10. Piwnica i oprawa końcowych aktów

Po ustabilizowaniu mechanik wykonać osobny przebieg artystyczny piwnicy:

- poprawić oświetlenie w niskim kluczu,
- usunąć światła widoczne poza obszarem rozgrywki,
- poprawić materiały i detale środowiska,
- dodać wyraźne punkty orientacyjne,
- poprawić prowadzenie gracza pomiędzy pomieszczeniami,
- zadbać o widoczność Watsona,
- wizualnie powiązać mechanizmy z inscenizacją „duchów”,
- wyrównać poziom dopracowania z Aktem I.

## Etap 11. Końcowe testy

Wykonać co najmniej trzy pełne przejścia:

1. Gracz postępujący zgodnie z oczekiwaną kolejnością.
2. Gracz eksperymentujący, zmieniający postacie i przerywający sekwencje.
3. Gracz korzystający z zapisu i wczytywania w każdym akcie.

Podczas testów sprawdzić również:

- szybkie wielokrotne klikanie,
- otwieranie paneli w trakcie ruchu i dialogów,
- zmianę postaci przed i po tutorialach,
- ponowne używanie wykonanych interakcji,
- przechodzenie przez akty z brakującymi opcjonalnymi znaleziskami.

## Kolejność realizacji

1. Audyt aktualnego buildu.
2. Krytyczna ścieżka i blokery progresji.
3. Konflikty inputu, UI i kamery.
4. Czytelność interakcji i POI.
5. Rozdzielenie ról lupy, skupienia, IdeaPointów i notatnika.
6. Dedukcje oraz podsumowania Sherlocka.
7. Logika fabularna i immersja.
8. Rola Watsona.
9. Quest Log i notatnik.
10. Artystyczny polish piwnicy i późniejszych aktów.
11. Pełne testy regresji.

## Rejestr audytu

| Problem | Status | Ustalenie |
| --- | --- | --- |
| Błędy kompilacji Unity | Już naprawione | Log Unity z 10.09.2026 nie zawiera bieżących błędów kompilacji. |
| `Wpisz nazwę obiektu` na interakcji Ethel 2 | Już naprawione | Tooltip obiektu `DoorOK2 (2)` zmieniony na `Ukryte przejście`. |
| „Tędy nie przejdę. Muszę poszukać małej Ethel na piętrze.” | Do weryfikacji | Tekst w aktualnej scenie ma poprawną pisownię. Należy odtworzyć moment, w którym pojawia się mimo nieaktualnego stanu zadania. |
| Tooltip i Interaction Shader obiektów niedostępnych Watsonowi | Naprawione, wymaga testu | `MouseTooltipManager` korzysta z `Interactable.CanPlayerInteract()` aktywnej postaci. |
| Kliknięcie elementu UI wysyła postać do świata | Do poprawy | Pierwsza blokada przez `IsPointerOverGameObject()` została cofnięta: pełnoekranowa grafika UI blokowała wszystkie kliknięcia świata. Docelowo trzeba rozróżnić kontrolki UI od dekoracyjnych grafik. |
| Rolka szyfru sejfu jednocześnie zmienia zoom kamery | Naprawione, wymaga testu | `CameraController` ignoruje zoom, gdy kursor znajduje się nad aktywną planszą szyfru. |
| `Escape` przy otwartym ekwipunku otwiera menu pauzy w tle | Potwierdzone | `PauseMenuManager` nie sprawdza `InventoryManager.isInventoryOpen`; należy ustalić pierwszeństwo zamykania paneli. |
| `NullReferenceException` podczas startowego skanowania Watsona | Naprawione, wymaga testu | Inicjalizacja `WatsonEagleVisionScanner` została przeniesiona z `Start()` do `Awake()`; scanner bezpiecznie obsługuje brak opcjonalnych referencji. |
