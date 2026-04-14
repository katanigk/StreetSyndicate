/// <summary>
/// Opening narrative (manual new game): segmented English story before interrogation.
/// </summary>
public static class CharacterOpeningStory
{
    public const string ScreenTitleEn = "The Ashkelton Crossing";

    public const string ScreenSubtitleEn =
        "What happened before they turned on the recorder.";

    public const string ButtonContinueEn = "Continue to interrogation";
    public const string ButtonSkipIntroEn = "Skip intro";
    public const string ButtonBackToSetupEn = "Back to setup";
    public const string ButtonNextEn = "Next";
    public const string ButtonPreviousEn = "Previous";

    /// <summary>Five beats: road & city → home & celebration → the search → the brawl & run → arrest & interrogation.</summary>
    public static readonly string[] StorySegmentsEnglish =
    {
        "You and your three best pals decided to move to Ashkelton — a city bursting with opportunity, but also the kind of place where everyone knows you'd better know how to watch your back. You came there to start over. For real. The kind of fresh start people dream about when they're hanging by a thread — when they've got nothing left to lose, but still something to prove.\n\n" +
        "After scraping together a little cash from wherever you could, the four of you squeezed into the old heap your father left you — a tired, rattling motorcar that was lucky to make the trip at all — and drove for days. Long roads, cheap fuel, aching backs, empty stomachs, and that little flicker of hope inside you whispering that maybe, just maybe, this time it might truly work out.\n\n" +
        "And then, at long last, Ashkelton.\n\n" +
        "The city that opened up before you was unlike anything you'd ever known. Through the cracked window of that car, barely holding itself together, you saw a world you'd only ever seen on the silver screen. Fine restaurants glowing with warm light. Great villas behind iron gates. Gleaming automobiles, expensive suits, women who looked as though they'd stepped straight out of a magazine. But Ashkelton had another side too — the side they don't really show in the pictures. Hard streets. Dark corners. Folks with dead-eyed stares. The kind of places where you understand in a second that if you get in trouble there, nobody's coming to save you.",

        "Still, you kept driving.\n\n" +
        "Somehow, through a good bit of luck — or maybe just fate — you found a place renting out rooms. Since there were four of you, the owner gave you a larger one. Nothing fancy — but big enough to sleep in, and it even had a little office off to the side. Cramped, old, a bit run-down — but it was yours. And in that moment, that was enough.\n\n" +
        "You paid the first week's advance, tossed your bags inside, and did what any four young fellas with more swagger than sense, a bit of money in their pockets, and the taste of victory on their tongues would do.\n\n" +
        "You went out to celebrate.\n\n" +
        "At the bar, you grabbed a table, called over the waitress, and ordered four beers. One round turned into two. Then another. Somewhere along the line a few shots landed on the table too, and before long everything had that warm, blurry feeling to it — the laughter got louder than it ought to, the words started slurring together, your head felt light, and the whole evening seemed like, for once, something in your life had gone the right way.\n\n" +
        "For one brief moment, it truly felt like everything was falling into place.",

        "Then you noticed one of the boys had been gone far too long.\n\n" +
        "At first, you didn't think much of it. Maybe he'd stepped out for some air. Maybe he'd gone to take a leak. Maybe he'd got himself mixed up in some small bit of trouble. But as the minutes dragged on, that feeling in your gut started kicking harder — it no longer felt like nothing.\n\n" +
        "So the three of you got to your feet and went looking for him.\n\n" +
        "The second you stepped out of the bar, you saw the crowd.\n\n" +
        "A small circle of people stood out in the street — close enough to see everything, far enough not to get involved. In the middle stood your friend. Coming toward him were two men, moving slow, with that threatening walk of fellows who've done this sort of thing before — and likely enjoyed every minute of it.\n\n" +
        "You didn't think twice. You rushed right in.",

        "What began with shouting blew up in seconds. Fists flew. Somewhere, glass shattered. Shoes slipped across the pavement. Bodies slammed into one another. It was all noise, chaos, punches, fear, and adrenaline — and then, in a single instant, everything changed.\n\n" +
        "Somebody got stabbed.\n\n" +
        "Nobody stayed to find out who.\n\n" +
        "You ran.\n\n" +
        "You ran from the street. You ran from the shouting. You ran from the blood. You ran from the sirens already starting to sound in your head, whether they were truly there or not. Somewhere along the way, you split up, and each of you was swallowed alone by the dark streets of Ashkelton, driven by nothing but fear and instinct.",

        "By the time you made it back to your lodgings, out of breath and badly shaken, it was already too late.\n\n" +
        "They were waiting for you.\n\n" +
        "You were arrested and taken in for questioning.\n\n" +
        "Now you sit beneath the hard white light of the interrogation room. The walls are bare. The air is still. Every sound feels too loud — the scrape of a chair, the turning of a page, the slow ticking of a clock behind you. Across from you sit a detective and his partner, watching you in silence, weighing every blink, every breath, every word — before you've even said a thing."
    };

    public static int StorySegmentCount => StorySegmentsEnglish.Length;

    /// <summary>Full text (all segments) — for search / future export.</summary>
    public static string BodyEnglish =>
        string.Join("\n\n", StorySegmentsEnglish);
}
