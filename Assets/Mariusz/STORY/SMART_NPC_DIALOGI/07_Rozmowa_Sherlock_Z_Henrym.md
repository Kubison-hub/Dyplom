# Rozmowa_Sherlock_Z_Henrym

Użycie w scenie: **Henry_NPC / Sherlock**  
Źródło: `Assets/Mariusz/Levels/GameLeveL_DEV.unity`, komponent NPCConversation `2256184473945961800`.

> Transkrypcja źródłowa. Zachowano pisownię i treść zapisaną w scenie.

## Parametry rozmowy

- `Henry`: `false` (EditableBoolParameter)
- `First`: `true` (EditableBoolParameter)

## Wypowiedzi

### Wypowiedź #0 [ROOT]

**Sir Henry:** Panie Holmes? Dobrze, że tu jesteście. To straszne, co się stało. Do tego moja biedna Violet… Jak mogę pomóc?

Akcje: -  
Dalej: opcja `#48` (bez warunku)

### Wypowiedź #49

**Sir Henry:** To wszystko wydarzyło się tak szybko. W jednej chwili zgasły światła, a następnie stół zaczął dygotać… Powietrze zrobiło się ciężkie od zapachu kadzidła, podłoga zaczęła skrzypieć… naprawdę poczułem czyjąś obecność. A potem ten straszliwy huk.

Akcje: -  
Dalej: opcja `#72` (bez warunku)

### Wypowiedź #55

**Sherlock:** Jeśli będziemy mieć jakieś pytania, wrócimy do pana.

Akcje: -  
Dalej: Koniec rozmowy

### Wypowiedź #58

**Sir Henry:** Tak, bywałem, ale tym razem ten zapach był intensywniejszy, prawie jak w kościele. Może to kwestia negatywnych emocji ducha? A może wyjątkowo blisko mnie przeszedł?

Akcje: -  
Dalej: opcja `#73` (bez warunku)

### Wypowiedź #60

**Sir Henry:** Niezbadane jest życie po śmierci. Kto wie, czy sam ojciec nie chciał zabrać córki do siebie?

Akcje: EditableSetBoolParamAction `Henry` = `true`  
Dalej: opcja `#61` (bez warunku); opcja `#54` (bez warunku)

### Wypowiedź #62

**Sir Henry:** Jak mogę pomóc?

Akcje: -  
Dalej: opcja `#78` (bez warunku); opcja `#64` (bez warunku); opcja `#74` (bez warunku); opcja `#53` (bez warunku); opcja `#54` (bez warunku)

### Wypowiedź #63

**Sir Henry:** To ciotka mojej narzeczonej. Była dla mnie surowa, ale sprawiedliwa. Szanowałem ją. Wiem, że od śmierci swojego ojca była niespokojna i potrzebowała tego seansu.

Akcje: -  
Dalej: opcja `#65` (bez warunku); opcja `#54` (bez warunku)

### Wypowiedź #68

**Sir Henry:** Zaraz przed strzałem, już po tym, jak zgasło światło, usłyszałem dźwięk przesuwania czegoś… może krzesła, może stołu?

Akcje: -  
Dalej: opcja `#69` (bez warunku); opcja `#54` (bez warunku)

### Wypowiedź #70

**Sir Henry:** Oczywiście, sprawa wymaga pilnego wyjaśnienia.

Akcje: -  
Dalej: wypowiedź `#62` (bez warunku)

### Wypowiedź #75

**Sir Henry:** Tutaj? Nie. Wiem jedynie, że opiekun lady Violet nie chciał tu wchodzić, bo jest duchownym. Nie lubi seansów i uważa je za obrazę Boga.

Akcje: -  
Dalej: wypowiedź `#76` (bez warunku)

### Wypowiedź #76

**Sir Henry:** Ale osobiście uważam, że nikt z nas nie chciałby skrzywdzić Lady Edith. Musiał to zrobić albo jej ojciec, tęskniący za córką, albo ktoś obcy.

Akcje: -  
Dalej: opcja `#77` (bez warunku); opcja `#54` (bez warunku)

## Opcje gracza

### Opcja #48

**Tekst:** Mam do pana kilka pytań.

Akcje: -  
Dalej: wypowiedź `#70` (bez warunku)

### Opcja #53

**Tekst:** Czy coś łączyło pana z Lady Edith?

Akcje: -  
Dalej: wypowiedź `#63` (bez warunku)

### Opcja #54

**Tekst:** Dziękuję za współpracę.

Akcje: -  
Dalej: wypowiedź `#55` (bez warunku)

### Opcja #61

**Tekst:** Dobrze… Zmieńmy temat.

Akcje: -  
Dalej: wypowiedź `#62` (bez warunku)

### Opcja #64

**Tekst:** Czy zauważył pan coś nietypowego w chwili strzału?

Akcje: -  
Dalej: wypowiedź `#68` (bez warunku)

### Opcja #65

**Tekst:** Wróćmy do innych tematów.

Akcje: -  
Dalej: wypowiedź `#62` (bez warunku)

### Opcja #69

**Tekst:** Wróćmy do innych tematów.

Akcje: -  
Dalej: wypowiedź `#62` (bez warunku)

### Opcja #72

**Tekst:** Był pan wcześniej na seansach? Kadzidło to coś normalnego.

Akcje: -  
Dalej: wypowiedź `#58` (bez warunku)

### Opcja #73

**Tekst:** …Wierzy pan w duchy?

Akcje: -  
Dalej: wypowiedź `#60` (bez warunku)

### Opcja #74

**Tekst:** Czy widział pan, aby ktoś zachowywał się dziwnie?

Akcje: -  
Dalej: wypowiedź `#75` (bez warunku)

### Opcja #77

**Tekst:** Wróćmy do innych tematów.

Akcje: -  
Dalej: wypowiedź `#62` (bez warunku)

### Opcja #78

**Tekst:** Co może pan powiedzieć o dzisiejszym zdarzeniu?

Akcje: -  
Dalej: wypowiedź `#49` (bez warunku)
