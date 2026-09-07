# Instrukcje

`GameProgressManager` nie jest systemem zapisu. Jego zadaniem jest zebranie
referencji do wszystkich obiektow `Interactable` w scenie oraz prowadzenie listy
interakcji, ktore zostaly ukonczone.

Kazdy `Interactable` posiada:

- `SaveId` - staly i unikalny identyfikator;
- `IsCompleted` - informacje, czy interakcja zostala ukonczona;
- `MarkCompleted()` - metode oznaczajaca interakcje jako ukonczona i dodajaca jej
  referencje do `GameProgressManager.CompletedInteractions`.

## Dane dla SaveLoadManager

Programista moze pobrac:

- `Interactions` - wszystkie interakcje w scenie;
- `CompletedInteractions` - referencje do ukonczonych interakcji;
- `GetCompletedInteractionIds()` - identyfikatory ukonczonych interakcji;
- `IsInteractionCompleted(saveId)` - stan konkretnej interakcji.

Do pliku JSON nalezy zapisywac `SaveId`, a nie referencje Unity. Po wczytaniu
mozna przekazac zapisane ID do `RestoreInteractionCompletedState(saveId, true)`.

## Odtworzenie stanu po wczytaniu

Przywrocenie `IsCompleted` nie odtwarza automatycznie wygladu obiektu. Jezeli
bedzie to potrzebne, `SaveLoadManager` lub konkretny skrypt interakcji powinien
po wczytaniu dodatkowo ustawic odpowiedni stan, na przyklad ukryc podniesiony
przedmiot, ustawic otwarte drzwi albo wylaczyc zakonczona interakcje.
