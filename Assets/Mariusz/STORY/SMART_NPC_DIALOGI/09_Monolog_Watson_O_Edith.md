# Monolog_Watson_O_Edith

Użycie w scenie: **Int_LadyEdithBody / Sherlock, Int_LadyEdithBody / Watson**  
Źródło: `Assets/Mariusz/Levels/GameLeveL_DEV.unity`, komponent NPCConversation `1554916127562901623`.

> Transkrypcja źródłowa. Zachowano pisownię i treść zapisaną w scenie.

## Parametry rozmowy

- `BulletExamDone`: `false` (EditableBoolParameter)

## Wypowiedzi

### Wypowiedź #0 [ROOT]

**Watson:** Co my tu mamy...

Akcje: -  
Dalej: opcja `#48` (bez warunku); opcja `#64` (bez warunku); opcja `#102` (bez warunku); opcja `#73` (bez warunku); opcja `#54` (bez warunku)

### Wypowiedź #55

**Watson:** Na razie, to wszystko.

Akcje: -  
Dalej: Koniec rozmowy

### Wypowiedź #76

**Watson:** Nasza ofiara nie została postrzelona w głowę. Nie widzę ani dziury wlotowej, ani tym bardziej wylotowej.

Akcje: -  
Dalej: opcja `#99` (bez warunku)

### Wypowiedź #85

**Watson:** Dziura po kuli. Strzał prosto w serce... Lady Edith odeszła nim upadła.

Akcje: EditableSetBoolParamAction `BulletExamDone` = `true`  
Dalej: opcja `#99` (bez warunku)

### Wypowiedź #89

**Watson:** Na ofierze ani w jej pobliżu nie ma żadnych podejrzanych śladów, w tym prochu. Strzał na pewno nie padł z bliska.

Akcje: -  
Dalej: opcja `#99` (bez warunku); opcja `#54` (bez warunku)

### Wypowiedź #103

**Watson:** Ofiara upadła na plecy, co oznacza, że sprawca znajdował się w jakiejś odległości przed ofiarą.

Akcje: -  
Dalej: wypowiedź `#104` (bez warunku)

### Wypowiedź #104

**Watson:** Do tego mamy tu drugą dziurę, to oznacza, że kula przeszła na wylot.

Akcje: -  
Dalej: opcja `#99` (bez warunku)

## Opcje gracza

### Opcja #48

**Tekst:** Głowa

Akcje: -  
Dalej: wypowiedź `#76` (bez warunku)

### Opcja #54

**Tekst:** Zakończ

Akcje: -  
Dalej: wypowiedź `#55` (bez warunku)

### Opcja #64

**Tekst:** Tors

Akcje: -  
Dalej: wypowiedź `#85` (bez warunku)

### Opcja #73

**Tekst:** Otoczenie ofiary

Akcje: -  
Dalej: wypowiedź `#89` (bez warunku)

### Opcja #99

**Tekst:** Dalej

Akcje: -  
Dalej: wypowiedź `#0` (bez warunku)

### Opcja #102

**Tekst:** Plecy

Akcje: -  
Dalej: wypowiedź `#103` (bez warunku)
