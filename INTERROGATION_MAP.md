# מפת חקירה (מקור אמת מול הקוד)

מסמך זה מתואם ל־`PersonalityQuestionnaire.cs` ו־`MainMenuFlowController.cs`.  
**עקרון מרכזי:** אותו מערך `_draftAnswers` משתמש באותם **אינדקסים** למסלולים שונים — ראו סעיף “כפל משמעות”.

---

## כפל משמעות באינדקסים (למה היה בלגן)

| אינדקס | מסלול רגיל (לילה ≠ “עם שלושה חברים”) | מסלול שיתופי (לילה = אפשרות ראשונה) |
|--------|----------------------------------------|--------------------------------------|
| **2** | `StabbingWhoQuestionIndex` — מי דקר (3 אפשרויות מתוך `ChoiceLabels[2]`) | מסך **שמות חברים** (ידני / אוטומטי) — **לא** תשובת דקירה |
| **3** | פילוסופיה 1 — **6** אפשרויות → מיפוי ל־`ChoiceTraitByIndex` | סיבת בוא לעיר — **4** אפשרויות (טקסטים ב־`DrawCharacterQuestions`) |
| **4** | פילוסופיה 2 — **6** אפשרויות → תכונה | מי נהג / איזה רכב — **4** אפשרויות |
| **5** | פילוסופיה 3 — **6** אפשרויות → תכונה | צבע רכב — **8** אפשרויות (מיפוי ל־`AccentColorLabels`; בconfirm גם מועתק ל־`ColorQuestionIndex` = 6) |

בקוד: `PhilosophyOrCoopSlotFirstIndex` (3) עד `PhilosophyOrCoopSlotLastIndex` (5).  
`CountsTowardTraitPhilosophyPicks` — במסלול שיתופי **לא** סופרים פיקים מ־3–5 לברים / תקרת פוטנציאל מהשאלון.

**מסלול שיתופי ידני:** רצף השאלות הלינארי נגמר באינדקס **5**. אינדקסים **6–12** נשארים בדרך כלל **לא מוגדרים** (`-1`) ולא נשאלים במסך — `ResolveInterrogationOutcome` משתמש ב־`Mathf.Max(0, …)` על תשובות חסרות; שותפים/מיומחים יכולים להיבחר אקראית ב־`SeedStartingCrew` אם אין תשובות.

---

## מיפוי 6 אפשרויות → תכונת ליבה (רק שאלות פילוסופיה 3–5 במסלול רגיל)

| אינדקס בחירה | CoreTrait |
|---------------|-----------|
| 0 | Strength |
| 1 | Agility |
| 2 | Intelligence |
| 3 | Charisma |
| 4 | MentalResilience |
| 5 | Determination |

---

## טבלת כל אינדקסי הטיוטה (0–12)

כל הכותרות באנגלית הן **מ־`QuestionTitles`** אלא אם ה־UI במסלול שיתופי **דורס** (שמות / סיבה / רכב / צבע רכב).

| # | `QuestionTitles` (מקור) | `ChoiceLabels` / הערות |
|---|-------------------------|-------------------------|
| 0 | `Name. What is your name?` | 3 אפשרויות שם; Practice ראו מפה בונוסים |
| 1 | `Well, {NAME}, tell us what you did last night.` | 3 אפשרויות; **0** = מסלול שיתופי |
| 2 | `So, {NAME}, who did the stabbing?` — **או** במסלול שיתופי כותרת דריסה: שמות | רגיל: 3; שיתופי: מצב שמות |
| 3 | `You get one opening in the district...` — **או** שיתופי: למה באתם | רגיל: 6× פילוסופיה; שיתופי: 4 סיבות |
| 4 | `Pressure rises, trust is fragile...` — **או** שיתופי: איך הגעתם | רגיל: 6×; שיתופי: 4 בעלות רכב |
| 5 | `By dawn, what should your crew believe...` — **או** שיתופי: צבע רכב | רגיל: 6×; שיתופי: 8 צבעים |
| 6 | `Favorite color...` | 8 צבעים — **מסלול רגיל**; בשיתופי לרוב הצבע מגיע מ־[5] |
| 7–9 | שלושת השותפים | 6 התמחויות × 3 (`PartnerSpecialtyChoiceLabels`) |
| 10 | `How hot should police interest...` | 4 רמות לחץ (25 + index×20 כשמוגדר) |
| 11 | `On the stabbing case, what statement...` | 3 — תוצאות `ResolveInterrogationOutcome` |
| 12 | `If the file requires one associate name...` | 3 |

טקסטי אפשרויות מלאים לשאלות 0–5 ו־6+ — ב־`PersonalityQuestionnaire.ChoiceLabels` בקוד.

---

## ברים (Physical וכו’) ותקרת פוטנציאל מהשאלון

ב־`BuildProfile`: לכל תכונה **40 + 10 × (מספר פיקים)** לפני `NormalizeTraitsTo100()`.  
פיקים נספרים רק מ־**שאלות 3–5** כש־`CountsTowardTraitPhilosophyPicks` מחזיר true (כלומר **לא** מסלול שיתופי).  
`TraitInterviewPotentialCeiling` = `Clamp(picks + 1, 1, 5)` לכל תכונה.

---

## XP / Practice לבוס (`ApplyInterviewBonusesToProfile`)

כל `CoreTraitProgression.AddPractice` → `TraitToSkillDistribution` → פיצול לבנק **מיומנויות**, לא שדה XP נפרד לליבה.

| מקור | סיכום |
|------|--------|
| שאלה 0 | שתיקה → MR 100; כינוי → Cha 100 (אלא אם כינוי + שתיקה בלילה → MR 100) |
| שאלה 1 | אינדקס 2 → MR 100 + לחץ |
| שאלה 2 (רגיל) | אינדקס 2 → MR 100 |
| מסלול שתיקה/אגרסיבי (דגלים ב־MainMenu) | Cha / Det / MR לפי הקוד |
| שיתופי: `_coopReasonChoice` | 0→Str 50, 1→Det 50, 3→Int 50, 2→הגרלת XP משוקללת (∝1/xp) + תכונה אקראית — **נפרד לבוס ולכל אחד מ־3 השותפים** (`AddPractice` / `CoopPartnerRubricBonusXp`) |
| שיתופי: ערב / בר / שקרים | עד `Charisma`/`MR`/`Agility`/`Strength` + בונוסים ל־`CoopPartnerRubricBonusXp` |

פירוט מלא נשאר בקטעים הרלוונטיים ב־`MainMenuFlowController.ApplyInterviewBonusesToProfile`.

---

## קבועים חדשים בקוד (לקריאה)

- `PhilosophyOrCoopSlotFirstIndex` / `PhilosophyOrCoopSlotLastIndex`
- `CooperativeInterviewLastLinearDraftIndex` (= 5) — סוף רצף הטיוטה בשיתופי
- `IsCooperativeInterviewRoute`, `IsPhilosophyOrCoopSharedSlot`, `CountsTowardTraitPhilosophyPicks`
- `GetImmediateConsequencePreview(..., questionnaireAnswers)` — במסלול שיתופי לא מציג יותר “strengthens Strength” על סיבת נסיעה וכו’

---

## קבצים

| קובץ | תפקיד |
|------|--------|
| `Assets/Scripts/PersonalityQuestionnaire.cs` | אינדקסים, טקסטים, לוגיקת פיקים, תוצאות חקירה, preview |
| `Assets/Scripts/MainMenuFlowController.cs` | UI חקירה, בונוסים, מסלול שיתופי |
| `Assets/Scripts/TraitToSkillDistribution.cs` | פיצול Practice לסקילים |

עדכן מסמך זה אם משנים אינדקסים או מפרידים סוף סוף מערכי טיוטה נפרדים למסלולים.
