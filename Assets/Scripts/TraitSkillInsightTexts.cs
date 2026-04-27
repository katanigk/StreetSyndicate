/// <summary>
/// Shared long-form explanations for core traits and derived skills (identity screen, boss profile, tooltips).
/// </summary>
public static class TraitSkillInsightTexts
{
    public static string GetCoreTraitInsight(CoreTrait trait)
    {
        switch (trait)
        {
            case CoreTrait.Strength:
                return "Physical power and endurance. Strength pushes melee brawls, rough work, and intimidation. Rivals and allies read it as raw threat — it decides how hard you hit and how far you can force a situation.";
            case CoreTrait.Agility:
                return "Speed, reflexes, and body control. Agility feeds stealth, driving, and firearms. When plans go loud or you need to slip heat, this is what keeps you one step ahead.";
            case CoreTrait.Intelligence:
                return "Planning, analysis, and technical sense. Intelligence backs surveillance, logistics, and fine work like locks. It is how you read the board and stack odds before the move.";
            case CoreTrait.Charisma:
                return "Presence, charm, and persuasion. Charisma drives negotiation, leadership, and deception. When words matter more than bullets, this stat opens doors and bends outcomes.";
            case CoreTrait.MentalResilience:
                return "Composure under pressure. Long watches, bad breaks, and rising heat test this first. It keeps your crew steady when the job runs long or the plan frays.";
            case CoreTrait.Determination:
                return "Will to finish what you start. Determination anchors leadership, logistics, and holding position when costs climb. It is the difference between folding and seeing the run through.";
            default:
                return "";
        }
    }

    public static string GetDerivedSkillInsight(DerivedSkill skill)
    {
        switch (skill)
        {
            case DerivedSkill.Brawling:
                return "Unarmed and close combat — fists, elbows, improvised weapons. Draws heavily on raw strength and agility, with grit to stay in the clinch when it gets ugly.";
            case DerivedSkill.Firearms:
                return "Pistols, long guns, and shooting under stress. Steady hands, composure, and grit under fire — agility, nerve, and determination when rounds count.";
            case DerivedSkill.Stealth:
                return "Moving unseen, timing entries, and leaving no trace. Blends quickness, sharp reading of situations, and calm when the pressure climbs.";
            case DerivedSkill.Driving:
                return "Cars, routes, and pursuit. Split-second control, reading traffic and odds, and keeping your head when the chase turns mean — not just foot-on-floor speed.";
            case DerivedSkill.Lockpicking:
                return "Defeating locks and simple security. Patient fingers and a clear head: finesse and focus beat brute force at the keyhole.";
            case DerivedSkill.Surveillance:
                return "Watching targets, patterns, and tells. Mixes analysis, patience on the tail, field instinct, and the stubbornness to stay on a cold watch.";
            case DerivedSkill.Negotiation:
                return "Deals, favors, and terms. Charm, clarity, and reading the room — who blinks first when money and risk are on the table.";
            case DerivedSkill.Intimidation:
                return "Fear as leverage. Physical presence, voice, and ice-cold nerve — you do not always need a gun to own the room.";
            case DerivedSkill.Deception:
                return "Lies, covers, and misdirection. A believable story needs charm, quick thinking, and the mask that never slips when eyes are on you.";
            case DerivedSkill.Logistics:
                return "Moving people, gear, and cash on time. Planning, follow-through, and tight timing — the machine behind every job that cannot afford a slip.";
            case DerivedSkill.Leadership:
                return "Command, morale, and judgment under fire. Presence, resolve, and steadiness when your people need someone to follow into the mess.";
            case DerivedSkill.Medicine:
                return "Treating wounds and stabilizing people under fire; also cooking up compounds and brews for whatever purpose the job needs.";
            case DerivedSkill.Sabotage:
                return "Dismantling, defusing, and installing bombs and charges; Mike can fabricate devices too.";
            case DerivedSkill.Analysis:
                return "Breaking down intel, patterns, and cause and effect. Sharp mind, steady nerve, and the stubbornness to keep digging until the picture holds.";
            case DerivedSkill.Legal:
                return "Rules, filings, and leverage in the system. Knowledge of the book, presence in the room, and ice when the other side pushes.";
            case DerivedSkill.Finance:
                return "Cash flow, books, and who owes what. Numbers, follow-through, and the polish to move money without drawing the wrong eyes.";
            case DerivedSkill.Persuasion:
                return "Moving people without a fight — tone, framing, and timing. Charm and clarity, with the spine to hold the line when the pitch gets tested.";
            default:
                return "";
        }
    }
}
