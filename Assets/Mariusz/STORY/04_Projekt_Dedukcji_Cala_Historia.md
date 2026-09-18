# Projekt Dedukcji - pełna historia sprawy

## Cel dokumentu

Ten dokument proponuje kompletny przebieg zakładki **Dedukcja** od początku gry do rozwiązania sprawy. Łączy historię kanoniczną z eksploracją, Idea Pointami, zagadkami, notatkami, dialogami Sherlocka i Watsona oraz eskortowaniem NPC przez Watsona.

Nie jest to jeszcze tekst implementacyjny. Oznaczenia `D0`, `D1` itd. są roboczymi identyfikatorami kroków dedukcji.

## Reguły systemu

- Dedukcja jest jednym rozwijającym się timeline'em podzielonym na trzy akty.
- Gracz widzi tylko ukończone zdania, aktualnie otwarty krok i najbliższe pytania. Dalsza historia pozostaje ukryta.
- Pola **wymagane** muszą zostać uzupełnione poprawnie, aby odsłonić kolejny krok.
- Pola **hipotezy** nie blokują postępu. Pozwalają wskazać aktualnie podejrzaną osobę albo przypuszczalny motyw.
- Lista odpowiedzi zawiera wyłącznie pojęcia zdobyte przez gracza w kategoriach `Osoby`, `Obserwacje` i `Obiekty`.
- Błędne pole wymagane pozostaje otwarte, nie odbiera punktów i nie ujawnia poprawnej odpowiedzi.
- Jedna informacja może pochodzić z kilku NoteData, ale odblokowuje wspólne pojęcie dedukcyjne, np. kilka obserwacji może potwierdzić `UkrytePrzejscie`.
- Rozwiązanie kroku może odblokować nowe zdanie, pytania dialogowe, interakcję w świecie albo rozmowę między NPC.
- Dialog Sherlocka i dialog Watsona mogą prowadzić do tego samego koniecznego faktu, ale powinny mieć inne brzmienie i dostarczać innych informacji pobocznych.
- Watson otrzymuje własne, istotne zadania: badanie medyczne, zdobywanie zeznań, porównywanie relacji i doprowadzanie NPC do wspólnych rozmów.

## Oznaczenia

- **Stałe** - tekst wynikający z intro albo już potwierdzonego faktu.
- **[W: ...]** - pole wymagane.
- **[H: ...]** - pole hipotezy, niewymagane na tym etapie.
- **Istnieje** - treść lub mechanika jest już obecna w projekcie.
- **Rozbudować** - istniejący element wymaga nowej kwestii, notatki lub zdarzenia.
- **Nowe** - proponowany element, którego obecnie nie ma.

# Akt I - Jak dokonano zbrodni?

## Założenie aktu

Gracz ma odtworzyć sposób zabójstwa Lady Edith. Akt nie wymaga jeszcze poznania sprawcy ani motywu. Kończy się potwierdzeniem, że zabójca strzelał zza ściany i wykorzystał przejście otwierane mechanizmem stołu.

## D0. Początek sprawy

**Moment pojawienia:** początek gry, razem z początkowym Description oraz profilami Lady Edith i Madame Selmy.

**Timeline:**

> Ofiarą jest **Lady Edith Ogilvy**. Do zabójstwa doszło podczas seansu w domu **Madame Selmy**. **[H: Nieznany sprawca]** oddał śmiertelny strzał i zniknął **[W: w niewyjaśniony sposób]**.

**Pola:**

- `Sprawca` - hipoteza; początkowo `Nieznany sprawca`, później dowolna odkryta osoba.
- `Sposób zniknięcia` - wymagane; rozwiązanie docelowe: `wykorzystując ukryte przejście`.

**Wiedza startowa:**

- osoba: Lady Edith Ogilvy,
- osoba: Madame Selma,
- zdarzenie: seans spirytystyczny,
- fakt: światła zgasły,
- fakt: padł strzał,
- fakt: nikt nie dostrzegł napastnika.

**Otwarte pytania:**

- Skąd oddano strzał?
- Jak napastnik dostał się do salonu i z niego zniknął?
- Jak dostrzegł ofiarę w ciemności?

## D1. Sposób śmierci

**Moment pojawienia:** po rozpoczęciu badania ciała.

**Timeline:**

> Lady Edith zginęła od **[W: strzału oddanego z dystansu]**. Kula trafiła ją **[W: prosto w serce]**, przeszła na wylot i utkwiła **[W: we framudze okna]**.

**Wymagania gameplayowe:**

- ukończyć poprawną ścieżkę badania `Int_EdithExamBody`,
- doprowadzić Watsona do punktu badania i uzyskać poprawny wynik examinacji,
- zbadać `Int_lv1_WindowBulletCP` albo odpowiadający mu ślad toru kuli.

**Istniejące źródła:**

- notatka `Jak zginęła Lady Edith?`,
- badanie Watsona: rana serca, rana wylotowa, brak prochu,
- kula we framudze okna.

**Rola postaci:**

- Sherlock inicjuje analizę miejsca i toru pocisku.
- Watson dostarcza koniecznej wiedzy medycznej. To powinien być rozpoznawalny wkład Watsona, a nie kopia interakcji Sherlocka.

**Skutek rozwiązania:**

- odblokowanie pojęć `StrzalZDystansu`, `TorPocisku`, `FramugaOkna`,
- nowe pytania do świadków: co słyszeli i gdzie znajdowali się w chwili strzału,
- pytanie D0 nadal pozostaje nierozwiązane.

## D2. Widoczność w ciemności

**Moment pojawienia:** po zbadaniu lamp i kominka.

**Timeline:**

> W salonie panował mrok, ale **[W: ogień z kominka]** oświetlał Lady Edith. Napastnik mógł dostrzec ofiarę, sam pozostając **[W: poza zasięgiem światła]**.

**Wymagania gameplayowe:**

- wykonać obserwację zgaszonych lamp,
- wykonać obserwację kominka,
- połączyć oba ślady w zagadce Idea Point albo w kroku Dedukcji.

**Istniejące źródła:**

- NoteData `Lampa`,
- NoteData `Kominek`,
- opis, że kominek był jedynym źródłem światła.

**Proponowana delikatna rozbudowa:**

- dodać obserwację `Linia światła`, pokazującą, że blask kominka docierał do miejsca Lady Edith, ale nie do ściany strzeleckiej,
- po rozwiązaniu udostępnić pytanie do Henry'ego i Violet: czy widzieli sylwetkę Edith po zgaszeniu lamp.

**Skutek rozwiązania:**

- odblokowanie pojęcia `SwiatloKominka`,
- potwierdzenie, że ciemność była osłoną sprawcy, a nie przeszkodą w oddaniu strzału.

## D3. Stanowisko strzeleckie i droga ucieczki

**Moment pojawienia:** po odkryciu podejrzanej ściany i rozwiązaniu zagadki Idea Point na parterze.

**Timeline:**

> Tor pocisku prowadzi do **[W: ściany salonu]**. **[H: Nieznany sprawca]** oddał strzał **[W: zza ściany]** i zniknął **[W: wykorzystując ukryte przejście]**.

**Wymagania gameplayowe:**

- odkryć pięć parterowych Idea Pointów,
- zbadać odstający fragment ściany i przeciąg,
- rozwiązać sekwencję Idea Pointów łączącą ciało, okno, światło, stół i ścianę.

**Istniejące źródła:**

- NoteData `Podejrzana ściana`,
- obserwacja toru pocisku,
- `Int_lv1_HiddenWallMask`,
- rozwiązanie `DetectiveSequencePuzzle` na parterze.

**Skutek rozwiązania:**

- w D0 pole sposobu zniknięcia otrzymuje poprawną odpowiedź,
- pojawia się D4,
- odblokowują się pytania dialogowe o ruch stołu, hałas za ścianą i znajomość domu.

## D4. Mechanizm ukrytych drzwi

**Moment pojawienia:** natychmiast po rozwiązaniu D3.

**Timeline:**

> Tajne drzwi otwiera **[W: mechanizm okrągłego stołu]**, wykorzystujący brakujące **[W: zdobione dyski]**. Stół poruszył się tuż przed strzałem, więc przejście mogło być otwarte podczas seansu.

**Wymagania gameplayowe:**

- zbadać `Int_lv1_CircleTable`,
- zdobyć informację, że stół poruszył się tuż przed strzałem,
- odkryć, że brakuje trzech elementów mechanizmu,
- znaleźć czerwony, zielony i niebieski dysk.

**Istniejące źródła:**

- NoteData `Stół`,
- NoteData `Mechanizm w stole`,
- trzy przedmioty funkcjonujące obecnie jako zdobione kostki.

**Zmiana terminologii:**

- we wszystkich nowych tekstach stosować nazwę `zdobione dyski`,
- przed wdrożeniem ujednolicić stare NoteData, dialogi, tooltipy i nazwy ekwipunku, które mówią o kostkach.

**Skutek rozwiązania:**

- odblokowanie możliwości pełnego uruchomienia Easy Table,
- wpis pozostaje hipotezą do chwili fizycznego otwarcia drzwi.

## D5. Potwierdzenie sposobu zbrodni

**Moment pojawienia:** po umieszczeniu trzech dysków i ukończeniu Easy Table.

**Timeline:**

> Zdobione dyski pełniły funkcję kluczy. Umieszczenie ich w **[W: okrągłym stole]** uruchomiło mechanizm i otworzyło **[W: przejście za ścianą salonu]**.

**Wymagania gameplayowe:**

- znaleźć wszystkie trzy dyski,
- umieścić je w Easy Table,
- otworzyć przejście,
- wejść do środka i potwierdzić, że prowadzi za ścianę stanowiska strzeleckiego.

**Istniejące źródła:**

- NoteData `Mechanizm w stole 2`,
- wykonanie `int_lv3_easyTable`,
- fizycznie otwarte przejście.

**Podsumowanie Aktu I:**

> Lady Edith została zastrzelona z dystansu przez osobę ukrywającą się za ścianą salonu. Napastnik wykorzystał przejście otwierane przez mechanizm okrągłego stołu. Ciemność ukryła jego obecność, a światło kominka pozwoliło mu dostrzec ofiarę. Wiem już, jak dokonano zbrodni. Nie wiem jeszcze, kto znał mechanizmy domu ani dlaczego Lady Edith musiała zginąć.

**Nagroda i przejście:**

- odblokowanie pierwszej karty komiksu przedstawiającej rekonstrukcję strzału,
- odblokowanie Aktu II,
- nowe pytanie główne: `Dlaczego Lady Edith została zamordowana?`.

## Dialogi i mechanika Watsona w Akcie I

### Rozmowy pojedyncze

- **Henry:** słyszał przesuwanie ciężkiego mebla i skrzypienie po zgaszeniu świateł. Sherlock pyta o chronologię; Watson pomaga mu oddzielić obserwację od wiary w ducha.
- **Violet:** opisuje pozycję Edith i światło kominka. Sherlock pyta o ofiarę; Watson łagodniej odtwarza przebieg seansu.
- **Arthur:** przyznaje, że wstał od stołu. Sherlock traktuje to jako możliwe alibi sprawcy; Watson zauważa jego poczucie winy i strach.
- **George:** utrzymuje, że czekał na zewnątrz. Obecnie pozostaje to zeznaniem, nie potwierdzonym faktem.
- **Selma:** przyznaje, że stół był elementem seansu, ale nie ujawnia jeszcze pełnej funkcji przejścia.

### Wspólna rozmowa eskortowa Watsona - nowa

**Violet + Henry: rekonstrukcja miejsc przy stole**

- Watson może doprowadzić Violet do Henry'ego po uaktywnieniu pytania `Kto znajdował się przy stole?`.
- Rozmowa ustala pozycję Edith, potwierdza nieobecność Arthura i przypomina, że George deklarował pozostanie na zewnątrz.
- Odblokowuje obserwację `Nieobecni podczas strzału`.
- Nie wskazuje zabójcy, ale dodaje Arthura, Ethel i George'a do listy dostępnych hipotez sprawcy, gdy ich profile zostaną poznane.

# Akt II - Dlaczego Lady Edith musiała zginąć?

## Założenie aktu

Gracz poznaje historię Lady Edith, jej adopcję, poszukiwanie biologicznego ojca oraz działania Selmy. Akt kończy się ustaleniem, że motyw dotyczy sekretu chronionego przez dwór i Secret Service, ale nie ujawnia jeszcze pełnej tożsamości ojca ani zabójcy.

## D6. Cel wizyty Lady Edith

**Moment pojawienia:** początek Aktu II albo po rozmowie z Violet.

**Timeline:**

> Lady Edith przyszła do Madame Selmy, ponieważ chciała **[W: poznać tożsamość biologicznego ojca]**. Złoty pierścień z literą **[W: A]** był jedną z niewielu wskazówek dotyczących jej przeszłości.

**Wymagania gameplayowe:**

- znaleźć pierścień na ciele Edith,
- porozmawiać z Violet o adopcji i celu seansu,
- zapytać Arthura o pierścień.

**Istniejące źródła:**

- NoteData `Pierścień z inicjałem`,
- dialog Violet o adopcji,
- dialog Arthura potwierdzający, że pierścień nie należał do przybranego ojca Edith.

**Skutek rozwiązania:**

- nowe pytania do Selmy o wcześniejsze seanse Edith,
- nowe pytania do George'a o rodzinę Ogilvy,
- odblokowanie tematu `PochodzenieEdith`.

## D7. Rodzina Lady Edith

**Moment pojawienia:** po odnalezieniu dossier i odpowiednich listów na piętrze.

**Timeline:**

> Lady Edith była **[W: adoptowana]** przez Jane Elizabeth Howard i sir Johna Ogilvy'ego. Jej biologiczną matką mogła być **[W: Frances Margaret Howard]**, którą po narodzinach nieślubnego dziecka odesłano **[W: do klasztoru]**. Tożsamość ojca nadal pozostawała **[W: ukryta]**.

**Wymagania gameplayowe:**

- dostać się na piętro,
- znaleźć dossier Lady Edith,
- zebrać trzy dokumenty/listy,
- zestawić treść dossier z relacją Violet.

**Istniejące źródła:**

- List 2 / dossier Edith,
- rozmowy Violet,
- częściowo wypalona informacja o ojcu.

**Proponowana delikatna rozbudowa:**

- dodać portret rodzinny albo fotografię Edith z adopcyjnymi rodzicami jako obiekt,
- dodać ślady spalonej korespondencji przy kominku Selmy; wskazują, że ktoś usuwał nazwisko, ale nie ujawniają jeszcze czy zrobiła to Selma, George czy służby.

**Skutek rozwiązania:**

- pole hipotezy `Biologiczny ojciec Edith` staje się dostępne, lecz niewymagane,
- George otrzymuje nowe pytanie o klasztor i rodzinę Howardów,
- zbyt dokładna odpowiedź George'a tworzy obserwację `George wie więcej, niż powinien`.

## D8. Selma i dwór królewski

**Moment pojawienia:** po przeczytaniu listu królowej.

**Timeline:**

> Madame Selma prowadziła prywatne seanse dla **[W: królowej Wiktorii]**, która pragnęła ponownie usłyszeć **[W: księcia Alberta]**. Dzięki temu Selma uzyskała dostęp do sekretów związanych z dworem.

**Wymagania gameplayowe:**

- odnaleźć List 1,
- rozpoznać autora i adresatkę,
- wcześniej znać cel działalności Selmy.

**Istniejące źródła:**

- List 1 od królowej Wiktorii,
- notatki o portrecie królowej,
- rozmowy Selmy i George'a o jej wpływach.

**Skutek rozwiązania:**

- odblokowanie tematu dialogowego `SelmaIDwor`,
- nowa możliwość zapytania George'a, dlaczego mimo pogardy tolerował działalność Selmy,
- pojęcia `KrolowaWiktoria` i `KsiazeAlbert` trafiają do słownika Dedukcji.

## D9. Szantaż i groźba

**Moment pojawienia:** po przeczytaniu listu z groźbą i notatek Selmy.

**Timeline:**

> Selma poznała informację, którą próbowała wykorzystać dla **[W: zysku]**. Zaoferowała **[W: Secret Service]** bezpieczne przechowywanie sekretu, lecz otrzymała **[W: groźbę skierowaną przeciwko niej i jej rodzinie]**.

**Wymagania gameplayowe:**

- znaleźć List 3 albo nadpaloną korespondencję,
- znaleźć notatnik/dossier Selmy,
- połączyć ofertę zapłaty z groźbą nadawcy,
- znać już związek Selmy z dworem.

**Istniejące źródła:**

- List 3,
- notatnik Selmy,
- zapiski o wysokim wynagrodzeniu i zabezpieczeniu,
- dialogi Selmy dotyczące jej renomy i wpływów na dworze.

**Pola hipotezy:**

> **[H: Selma / Lady Edith / inna osoba]** mogła być właściwym celem uciszenia.

To pole pozostaje niewymagane. Na tym etapie gracz nie wie jeszcze, czy zabójca chciał uciszyć Edith, Selmę, czy obie kobiety.

**Skutek rozwiązania:**

- nowe pytania o tożsamość nadawcy groźby,
- możliwość konfrontacji Selmy z listem,
- motyw zostaje zawężony do tajemnicy dotyczącej pochodzenia Edith.

## D10. Zniknięcie Ethel

**Moment pojawienia:** po odkryciu ukrytych drzwi w pokoju Ethel.

**Timeline:**

> Ethel nie ma na piętrze. W jej pokoju znajduje się **[W: kolejne ukryte przejście]**, prowadzące **[W: do piwnicy]**. Dziewczynka mogła znać mechanizmy domu i uciec tą drogą.

**Wymagania gameplayowe:**

- dotrzeć do pokoju Ethel,
- znaleźć obraz lub pamiątkę przedstawiającą Ethel i Selmę bez ojca,
- rozwiązać mechanizm drzwi w pokoju,
- odkryć zejście do piwnicy.

**Istniejące źródła:**

- `Int_lv2_EthelHiddenWallMask`,
- `Int_Lv2_WoodBrickWall`,
- NoteData o ukrytym przejściu i zejściu na dół.

**Proponowana delikatna rozbudowa:**

- obiekt `Portret Selmy i Ethel` otwiera pytanie o ojca dziewczynki,
- osobna obserwacja `Ethel zna przejścia domu`, bez sugerowania jej współudziału.

**Podsumowanie Aktu II:**

> Lady Edith szukała prawdy o swoim pochodzeniu. Ślady prowadzą do Frances Margaret Howard, dworu królewskiego i księcia Alberta, ale najważniejsza część historii została celowo ukryta. Selma odkryła sekret i próbowała go wykorzystać, ściągając na siebie oraz rodzinę groźbę. Motyw zabójstwa wiąże się z informacją chronioną przez królewskie służby. Odpowiedzi należy szukać w piwnicy, do której prowadzi przejście z pokoju Ethel.

**Nagroda i przejście:**

- odblokowanie drugiej karty komiksu: Edith, Selma, królewski list i spalona dokumentacja,
- odblokowanie Aktu III,
- nowe pytanie główne: `Kto wykorzystał przejścia i królewski sekret, aby zamordować Edith?`.

## Dialogi i mechanika Watsona w Akcie II

### Rozmowy pojedyncze

- **Violet:** adopcja Edith, los Frances i pragnienie poznania ojca.
- **Arthur:** pierścień z literą A nie należał do sir Johna; Arthur znał Edith jako jej ochroniarz.
- **Selma:** wcześniejsze sesje Edith, kontakty na dworze i unikanie odpowiedzi o dokumentach.
- **George:** wiedza o klasztorze, grzechu matki i rodzinie Edith; jego wiedza wykracza poza rolę duchownego obecnego przypadkiem.
- **Henry:** relacje Violet, Edith i dworu; potwierdza, że George był opiekunem Violet.

Każdy temat powinien mieć wersję rozmowy Sherlocka i Watsona. Sherlock naciska na sprzeczności i dowody. Watson wykorzystuje empatię, poczucie winy oraz relacje rodzinne. Obie wersje mogą odblokować ten sam fakt wymagany, ale tylko wykonana wersja zapisuje swój wariant NoteData.

### Wspólna rozmowa eskortowa Watsona - nowa

**Selma + Arthur: tajemnice seansu**

- Watson może doprowadzić Arthura do Selmy albo Selmę do Arthura po odkryciu związku mechanizmu z seansem.
- Osobno oboje unikają pełnej odpowiedzi. Wspólna rozmowa ujawnia, że współpracowali przy efektach seansu i oboje znali ukryte mechanizmy.
- Na tym etapie nie muszą jeszcze przyznać, że Arthur odgrywał głos ojca Edith.
- Rozmowa tworzy obserwację `Selma i Arthur współpracowali podczas seansu` oraz utrzymuje Arthura jako wiarygodnego podejrzanego.

# Akt III - Kto zamordował Lady Edith?

## Założenie aktu

Gracz ma poznać funkcję piwnicy, odnaleźć Ethel, odtworzyć pełną drogę napastnika, odkryć królewski sekret i obalić alibi George'a. Akt kończy się wskazaniem sprawcy, motywu, sposobności oraz roli Selmy i Arthura.

## D11. Mechaniczny dom duchów

**Moment pojawienia:** po uruchomieniu `Int_lv3_ControlUnit`.

**Timeline:**

> Zjawiska w domu nie były nadprzyrodzone. **[W: urządzenia ukryte w piwnicy]** sterowały **[W: światłem, dźwiękiem i ruchomymi elementami]**, nadając seansom Selmy pozór obecności duchów.

**Wymagania gameplayowe:**

- znaleźć i zbadać Control Unit,
- uruchomić co najmniej dwa połączone z nim efekty,
- zestawić je z dźwiękami opisanymi przez świadków.

**Istniejące źródła:**

- `Int_lv3_ControlUnit`,
- story Description o sercu iluzji,
- zeznanie Henry'ego o przesuwaniu mebla i skrzypieniu.

**Skutek rozwiązania:**

- teoria ducha jako sprawcy przestaje być wiarygodna,
- odblokowanie pytań do Selmy o obsługę urządzeń,
- odblokowanie pytania, kto jeszcze znał piwnicę.

## D12. Ktoś próbuje zatrzymać śledztwo

**Moment pojawienia:** po pierwszym uruchomieniu pułapki w piwnicy.

**Timeline:**

> Pułapka nie jest elementem seansu. Ktoś uruchomił **[W: mechanizm zamykający pomieszczenie]**, aby **[W: zatrzymać śledczych w piwnicy]** albo zniszczyć ślady.

**Wymagania gameplayowe:**

- aktywować pułapkę przez manekin, zegar lub specjalną cegłę,
- znaleźć cztery Idea Pointy wyjaśniające mechanizm wyjścia,
- rozwiązać piwniczną sekwencję dedukcyjną.

**Istniejące źródła:**

- `Int_lv3_Manequine`,
- `Int_lv3_ClockClue`,
- `Int_lv3_SpecialBrick`,
- piwniczny puzzle Idea Point.

**Proponowana delikatna rozbudowa:**

- obserwacja `Pułapka została przygotowana niedawno` oparta na świeżych zarysowaniach albo przestawionym elemencie,
- nie wskazuje wykonawcy, ale potwierdza, że ktoś przewidział możliwość odkrycia piwnicy.

## D13. Świadek w piwnicy

**Moment pojawienia:** po pierwszej rozmowie z Ethel.

**Timeline:**

> Przed strzałem Ethel widziała w piwnicy **[W: dużego, obcego mężczyznę]**. Dziewczynka uciekła i ukryła się, dlatego nie wróciła do salonu. Widziana osoba mogła być **[H: Arthur / George / inny odkryty mężczyzna]**.

**Wymagania gameplayowe:**

- odnaleźć Ethel,
- przeprowadzić rozmowę bez przerywania jej ruchem innych postaci,
- uzyskać opis mężczyzny i chronologię: spotkanie nastąpiło przed strzałem.

**Istniejące źródła:**

- dialog Ethel 1,
- profil Ethel,
- jej znajomość ukrytych przejść.

**Skutek rozwiązania:**

- Arthur i George pozostają możliwymi hipotezami,
- zeznanie George'a o pobycie na zewnątrz staje się sprzeczne z nowym tropem,
- odblokowanie pytań o wygląd, ubiór i kierunek ruchu mężczyzny.

## D14. Pełna treść sekretu

**Moment pojawienia:** po zdobyciu i przeczytaniu zabezpieczającego listu Selmy.

**Timeline:**

> Biologicznym ojcem Lady Edith był **[W: książę Albert]**, a jej matką **[W: Frances Margaret Howard]**. **[W: Secret Service]** ukrył narodziny dziecka nawet przed Albertem i królową. Selma poznała prawdę, próbowała ją wykorzystać, a następnie przygotowała **[W: list zabezpieczający]** oraz kopię zapisaną na **[W: pustym pergaminie]**.

**Wymagania gameplayowe:**

- zdobyć małą skrzynkę,
- odnaleźć klucz ukryty w lalce Ethel,
- otworzyć skrzynkę i przeczytać List Ethel/Selmy,
- połączyć list z pustym pergaminem znalezionym przy Edith,
- zastosować światło lub inne ustalone narzędzie do ujawnienia niewidzialnego atramentu.

**Istniejące źródła:**

- końcowy list przekazany przez Ethel,
- NoteData `Pusty pergamin`,
- NoteData i dialog mówiące o Albercie, Frances oraz Secret Service,
- lalka Ethel i mała skrzynka.

**Skutek rozwiązania:**

- pole `Motyw` otrzymuje wymagane opcje: `ochrona królewskiego sekretu` i `powstrzymanie ujawnienia prawdy`,
- George jako agent Secret Service staje się hipotezą silniejszą, lecz nadal wymaga obalenia alibi,
- Arthur zostaje powiązany z Ethel jako jej ojciec, ale nie zostaje jeszcze całkowicie oczyszczony z podejrzeń.

## D15. Droga mordercy

**Moment pojawienia:** po rozmowie Ethel 2 i pokazaniu obu tras.

**Timeline:**

> Do domu można było wejść niepostrzeżenie przez **[W: zewnętrzne wejście do piwnicy]**. Stamtąd **[W: sieć ukrytych przejść]** prowadziła za ścianę salonu, a drugi skrót pozwalał szybko wrócić **[W: na górę lub przed dom]**.

**Wymagania gameplayowe:**

- pozwolić Ethel pokazać zewnętrzne wejście,
- przejść trasę od wejścia do stanowiska strzeleckiego,
- odkryć skrót prowadzący z piwnicy na górę,
- zestawić trasę z czasem między strzałem a pojawieniem się George'a.

**Istniejące źródła:**

- `Int_lv4_EthelRun`,
- `Int_lv4_EthelEntrance`,
- `Int_Lvl_3_Leadder`,
- końcowy opis po Ethel 2.

**Proponowana delikatna rozbudowa:**

- ślad błota, pyłu lub sadzy przy zewnętrznym wejściu,
- odpowiadający mu dyskretny ślad na obuwiu albo sutannie George'a,
- ślad ma potwierdzać trasę dopiero po odkryciu wejścia; wcześniej nie powinien sam wskazywać sprawcy.

**Skutek rozwiązania:**

- alibi osoby czekającej rzekomo na zewnątrz przestaje być wystarczające,
- pojawia się końcowa konfrontacja dowodów.

## D16. Obalenie alibi

**Moment pojawienia:** po rozwiązaniu D13-D15.

**Timeline:**

> **[W: Ojciec George]** twierdził, że nie wszedł do domu przed strzałem. Miał jednak związek z **[W: Secret Service]**, znał sekret Edith, odpowiadał opisowi świadka i mógł skorzystać z **[W: zewnętrznego wejścia do piwnicy]**. Jego alibi było **[W: fałszywe]**.

**Wymagania gameplayowe:**

- znać zeznanie George'a o pozostaniu na zewnątrz,
- znać opis Ethel,
- odkryć jego związek z Secret Service,
- odtworzyć trasę przez piwnicę,
- posiadać materialne potwierdzenie trasy albo przeprowadzić konfrontację z dwoma świadkami.

**Pola:**

- na tym etapie wcześniejsze pole hipotezy `Sprawca` staje się polem wymaganym,
- jeśli gracz wcześniej wskazał George'a, zachowuje wybór i musi go teraz zatwierdzić dowodami,
- jeśli wskazał inną osobę, może zmienić odpowiedź bez kary.

## D17. Rola Selmy i Arthura

**Moment pojawienia:** przed ostatecznym oskarżeniem.

**Timeline:**

> Selma uruchomiła mechanizm, ponieważ chciała wpuścić przez przejście **[W: Ethel]**, aby pomogła przy sztuczkach seansu. Nie wiedziała, że drogę wykorzysta George. Głos ducha należał do **[W: Arthura]**, który wstał od stołu, aby odegrać ojca Edith. Oboje ukrywali oszustwo, ale **[W: nie współpracowali przy morderstwie]**.

**Wymagania gameplayowe:**

- odkryć mechaniczne źródło zjawisk,
- znać relację Selmy i Ethel,
- znać nieobecność Arthura przy stole,
- przeprowadzić wspólną rozmowę ujawniającą podział ról podczas seansu.

**Skutek rozwiązania:**

- Selma i Arthur zostają oddzieleni od sprawcy zabójstwa,
- gracz posiada pełne: sprawcę, motyw, metodę, drogę i wyjaśnienie pozornie paranormalnych zjawisk,
- odblokowanie końcowego oskarżenia.

## D18. Ostateczna dedukcja

**Timeline:**

> Lady Edith zamordował **[W: ojciec George]**. Chciał powstrzymać ujawnienie, że jej biologicznym ojcem był **[W: książę Albert]**, oraz ochronić sekret ukrywany przez **[W: Secret Service]**. Wszedł do domu przez **[W: piwnicę]**, przeszedł ukrytymi tunelami i strzelił zza ściany salonu. Ciemność, światło kominka i sztuczki seansu zapewniły mu osłonę. Następnie wrócił ukrytą drogą i udawał, że przez cały czas czekał na zewnątrz.

**Warunek ukończenia gry:**

- wszystkie pola wymagane D16-D18 są poprawne,
- wykonano końcową konfrontację z George'em,
- gracz może zachować dowolne niewymagane hipotezy poboczne, ale timeline oznacza je jako obalone przez końcowe fakty.

**Nagroda:**

- trzecia karta komiksu z pełną rekonstrukcją,
- uruchomienie outro,
- po ogrzaniu telegramu dopisanie epilogu: anonimowym nadawcą był `Moriarty`.

## Wspólne rozmowy eskortowe Watsona w Akcie III - nowe

### Ethel + Arthur: ojciec i córka

- Dostępne po przeczytaniu listu i poznaniu informacji, że Arthur jest ojcem Ethel.
- Arthur potwierdza relację oraz to, że podczas seansu odgrywał głos ojca Edith.
- Ethel potwierdza, że mężczyzna spotkany w piwnicy nie był Arthurem albo opisuje cechę wykluczającą go w wiarygodny sposób.
- Rozmowa oczyszcza Arthura z morderstwa, ale pozostawia jego odpowiedzialność za oszustwo.

### Selma + George: królewski sekret

- Dostępne dopiero po D14-D16.
- Watson doprowadza Selmę do George'a albo inicjuje rozmowę, gdy oboje są obecni przy końcowej konfrontacji.
- Selma rozpoznaje język groźby albo potwierdza, że George wiedział o jej kontaktach z dworem.
- George reaguje wiedzą, której nie powinien ujawnić, i pogłębia sprzeczność w swoim alibi.
- Rozmowa dostarcza społecznego potwierdzenia, ale końcowe oskarżenie nadal wymaga materialnej trasy przez piwnicę.

# Proponowane nowe treści

## Obserwacje

1. **Układ miejsc przy stole** - kto siedział obok Edith i kogo zabrakło po zgaszeniu świateł.
2. **Nieobecni podczas strzału** - Arthur, Ethel i George jako osoby wymagające sprawdzenia.
3. **Linia światła kominka** - ofiara widoczna, stanowisko strzeleckie pozostaje ciemne.
4. **Tor pocisku** - ciało, framuga okna i ściana tworzą jedną linię.
5. **Selma i Arthur współpracowali** - potwierdzenie inscenizacji, nie morderstwa.
6. **George wie więcej, niż powinien** - wiedza o klasztorze, rodzinie Howardów i królewskim sekrecie.
7. **Pułapka została przygotowana** - ktoś przewidział odkrycie piwnicy.
8. **Fałszywe alibi George'a** - zestawienie wejścia, czasu, świadka i śladu materialnego.

## Obiekty

1. **Trzy zdobione dyski** - jedna grupa obiektu z trzema stanami zdobycia.
2. **Plan miejsc przy stole** - może być rekonstrukcją Sherlocka, nie fizycznym przedmiotem.
3. **Portret Selmy i Ethel** - pytanie o nieobecnego ojca.
4. **Spalona korespondencja** - brakująca część dokumentacji Selmy.
5. **Ślad z piwnicy** - błoto, pył lub sadza łącząca drogę z George'em.
6. **System mechanizmów** - zbiorczy profil Control Unit, zegara, manekina i ukrytych drzwi.

## Dialogi do rozbudowy

1. Selma: ruch stołu, funkcja Ethel podczas seansu, listy i kontakty z dworem.
2. Arthur: powód wstania od stołu, pierścień A, współpraca z Selmą i rola głosu ducha.
3. Violet: miejsca przy stole, adopcja Edith, relacje z George'em i królową.
4. Henry: odgłosy mechanizmów, widoczność w salonie i obecność George'a przed seansem.
5. George: klasztor Frances, Secret Service, niechęć do Selmy i deklarowany pobyt na zewnątrz.
6. Ethel: mężczyzna w piwnicy, układ przejść, Arthur jako ojciec oraz przeznaczenie listu.

Każdy temat powinien mieć wariant Sherlocka i Watsona. Nie muszą to być dwa osobne drzewa całej rozmowy. Wystarczą odmienne kwestie otwierające, reakcje NPC i opcjonalne fakty, natomiast oba warianty zapisują wspólne pojęcie wymagane przez Dedukcję.

# Macierz zależności gameplayowych

| Krok | Główne źródło fizyczne | Źródło dialogowe | Działanie Watsona | Odblokowuje |
|---|---|---|---|---|
| D1 | ciało i kula | badanie Watsona | examinacja medyczna | tor pocisku |
| D2 | lampy i kominek | Violet/Henry | rekonstrukcja widoczności | światło kominka |
| D3 | ściana i Idea Pointy | Henry/Selma | pytania o hałas | ukryte przejście |
| D4-D5 | stół i dyski | Selma/Arthur | zebranie relacji o seansie | otwarcie drzwi |
| D6 | pierścień A | Violet/Arthur | delikatne pytania rodzinne | pochodzenie Edith |
| D7-D9 | trzy listy | Selma/George | konfrontacja z dokumentami | królewski sekret |
| D10 | pokój Ethel | Selma/Arthur | pytanie o Ethel | zejście do piwnicy |
| D11-D12 | Control Unit i pułapka | Henry/Selma | zestawienie zeznań | oszustwo seansu |
| D13-D14 | Ethel, skrzynka i list | Ethel/Arthur | rozmowa z dzieckiem | Albert i świadek |
| D15-D16 | wejście i skrót | Ethel/George | eskortowa konfrontacja | obalone alibi |
| D17-D18 | komplet dowodów | Selma + Arthur + George | zebranie NPC | finał |

# Zasady projektowania dialogów powiązanych z Dedukcją

- Nowa opcja dialogowa pojawia się, gdy gracz ma aktywne pytanie i co najmniej jedną związaną z nim notatkę.
- Pytanie z dowodem wymaga konkretnego obiektu albo NoteData, np. pierścienia, listu lub pergaminu.
- Konfrontacja wymaga dwóch sprzecznych faktów, np. alibi George'a i zeznania Ethel.
- Wybranie opcji dialogowej zapisuje fakt tylko raz, również przy rozmowie powtarzalnej.
- Sherlock i Watson nie muszą zadawać identycznych pytań. Obaj mogą doprowadzić do wspólnego faktu inną drogą.
- Rozmowy eskortowe odblokowują się dopiero po sformułowaniu właściwego pytania w Dedukcji. Samo zestawienie dwóch NPC bez kontekstu nie uruchamia sceny fabularnej.
- Żaden wymagany wniosek nie może zależeć wyłącznie od opcjonalnej, łatwej do przeoczenia rozmowy. Jeśli rozmowa eskortowa jest wymagana, gra musi wyraźnie oznaczyć możliwą parę i zachować możliwość ponowienia próby.

# Docelowy ukończony timeline

Po zakończeniu gry zakładka Dedukcja przedstawia jedną spójną rekonstrukcję:

> Lady Edith Ogilvy została zamordowana podczas seansu w domu Madame Selmy. Zginęła od strzału oddanego z dystansu. Kula trafiła ją w serce, przeszła przez ciało i utkwiła we framudze okna.
>
> Napastnik strzelał zza ściany salonu. Ciemność ukryła jego obecność, a ogień z kominka oświetlił ofiarę. Sprawca wykorzystał ukryte przejście otwierane przez mechanizm okrągłego stołu, którego kluczami były trzy zdobione dyski.
>
> Lady Edith szukała prawdy o swoim pochodzeniu. Była córką Frances Margaret Howard i księcia Alberta, a jej narodziny zostały zatuszowane przez Secret Service. Madame Selma odkryła tę informację i próbowała wykorzystać ją dla zysku, po czym przygotowała list zabezpieczający oraz kopię zapisaną niewidzialnym atramentem.
>
> Zjawiska w domu Selmy były wytwarzane przez mechanizmy ukryte w piwnicy. Selma uruchomiła stół, aby wpuścić Ethel do salonu, a Arthur odgrywał głos ducha ojca Edith. Ich oszustwo stworzyło warunki do zbrodni, lecz nie byli wspólnikami zabójcy.
>
> Ojciec George, działający jako agent Secret Service, znał królewski sekret i chciał zapobiec jego ujawnieniu. Wszedł przez zewnętrzne wejście do piwnicy, przeszedł ukrytymi tunelami, oddał strzał zza ściany, a następnie wrócił inną drogą i stworzył fałszywe alibi. Ethel widziała go w piwnicy, a odkryta trasa oraz pozostałe dowody potwierdziły jej relację.

Po outro dopisywany jest epilog dotyczący telegramu Moriarty'ego, ale nie zmienia on odpowiedzialności George'a za morderstwo.

# Warunki jakości

- Każdy wymagany slot ma co najmniej jedno jednoznaczne źródło wiedzy w grze.
- Każdy akt wykorzystuje eksplorację, dialog oraz mechaniczną zagadkę; żaden nie może zostać rozwiązany wyłącznie przez przeklikiwanie listy.
- Watson ma co najmniej jedno konieczne zadanie specjalistyczne w każdym akcie.
- Hipotezy gracza pozostają edytowalne do chwili, gdy odpowiednie pole stanie się wymagane.
- Ukończenie aktu wynika z rozwiązania Dedukcji, nie tylko z wejścia do kolejnej części mapy.
- Zakończenie Dedukcji i fakty przedstawione w outro muszą być identyczne.
- Nowe ślady wzmacniają istniejącą historię i nie tworzą drugiego sprawcy ani nowego motywu.
