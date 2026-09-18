# Rozmowa_Watson_Z_Georgem

Użycie w scenie: **Georg_NPC / Watson**  
Źródło: `Assets/Mariusz/Levels/GameLeveL_DEV.unity`, komponent NPCConversation `3977183461483114975`.

> Transkrypcja źródłowa. Zachowano pisownię i treść zapisaną w scenie.

## Parametry rozmowy

- `Selma`: `false` (EditableBoolParameter)

## Wypowiedzi

### Wypowiedź #0 [ROOT]

**Watson:** Ojcie...

Akcje: -  
Dalej: wypowiedź `#59` (bez warunku)

### Wypowiedź #59

**Ojciec George:** Mów mi: Ojcze George. W czym mogę pomóc?

Akcje: -  
Dalej: opcja `#48` (bez warunku)

### Wypowiedź #77

**Ojciec George:** Duchy i magia to zabobony, a Madame Selma je szerzy. Jest artystką sceniczną, która wyciąga pieniądze z kieszeni bogatych.

Akcje: -  
Dalej: opcja `#85` (bez warunku)

### Wypowiedź #79

**Ojciec George:** Niestety, Jej Królewska Mość ufa Madame Selmie. Mogłem ją jedynie ostrzec, lecz nie mogłem zabronić jej przyjścia.

Akcje: EditableSetBoolParamAction `Selma` = `true`  
Dalej: opcja `#54` (bez warunku)

## Opcje gracza

### Opcja #48

**Tekst:** Nie przepada Ojciec za Madame Selmą, prawda?

Akcje: -  
Dalej: wypowiedź `#77` (bez warunku)

### Opcja #54

**Tekst:** Dziękuję, Ojcze. Jeśli pojawią się jeszcze pytania, wrócimy.

Akcje: -  
Dalej: Koniec rozmowy

### Opcja #85

**Tekst:** Mimo to pozwolił Ojciec przyjść tutaj swojej podopiecznej.

Akcje: -  
Dalej: wypowiedź `#79` (bez warunku)
