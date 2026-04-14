using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Large pools for random full names (boss alias, crew, and future NPCs: families, police, lawyers, civilians).
/// </summary>
public static class CharacterNamePools
{
    /// <summary>Female given names (paired with <see cref="FamilyNames"/> for random full names).</summary>
    public static readonly string[] FemaleGivenNames =
    {
        "Sofia", "Elena", "Nadia", "Irina", "Katya", "Anya", "Mila", "Vera", "Olga", "Yelena",
        "Isabella", "Valentina", "Francesca", "Giulia", "Chiara", "Lucia", "Rosa", "Carla", "Silvia", "Renata",
        "Carmen", "Ines", "Pilar", "Beatriz", "Marisol", "Teresa", "Juana", "Adriana", "Catalina", "Esperanza",
        "Camille", "Amelie", "Claire", "Juliette", "Nathalie", "Sabine", "Elise", "Colette", "Simone", "Brigitte",
        "Anna", "Magda", "Zofia", "Ewa", "Kasia", "Agnieszka", "Marta", "Joanna", "Halina", "Danuta",
        "Maeve", "Siobhan", "Fiona", "Brigid", "Niamh", "Orla", "Aoife", "Roisin", "Sinead", "Deirdre",
        "Layla", "Yasmin", "Samira", "Zahra", "Farah", "Leila", "Amira", "Dina", "Salma", "Noor",
        "Aiko", "Yuki", "Keiko", "Mei", "Hana", "Naomi", "Reiko", "Mika", "Sora", "Emi",
        "Charlotte", "Vivienne", "Audrey", "Grace", "Hazel", "Irene", "Janet", "Laura", "Monica", "Nina",
        "Diana", "Eva", "Faye", "Gina", "Helen", "Ingrid", "Jade", "Karen", "Lena", "Maya"
    };

    public static readonly string[] GivenNames =
    {
        "Alex", "Niko", "Marco", "Ramon", "Luca", "Dante", "Milan", "Adrian", "Victor", "Leo",
        "Sergio", "Noah", "Ivan", "Andrei", "Dmitri", "Yuri", "Oleg", "Viktor", "Stefan", "Lukas",
        "Jonas", "Erik", "Henrik", "Lars", "Sven", "Felix", "Bruno", "Enzo", "Matteo", "Giovanni",
        "Paolo", "Antonio", "Salvatore", "Rocco", "Frankie", "Vincent", "Joey", "Tommy", "Jimmy", "Mikey",
        "Danny", "Anthony", "Louie", "Carlos", "Miguel", "Javier", "Diego", "Rafael", "Esteban", "Hector",
        "Manuel", "Pablo", "Marcus", "Damien", "Roland", "Pierre", "Jean", "Antoine", "Bernard", "Hugo",
        "Olivier", "Sebastian", "Klaus", "Dieter", "Wolfgang", "Sasha", "Mikhail", "Nikolai", "Sergei", "Pavel",
        "Alexei", "Cristian", "Eduardo", "Fernando", "Jorge", "Luis", "Rodrigo", "Emilio", "Giancarlo", "Franco",
        "Angelo", "Rico", "Dino", "Tariq", "Malik", "Omar", "Amir", "Elias", "Jonah", "Caleb",
        "Jordan", "Tyler", "Brandon", "Ryan", "Sean", "Patrick", "Connor", "Liam", "Owen", "Nathan"
    };

    public static readonly string[] FamilyNames =
    {
        "Moretti", "Kosta", "Romano", "Varga", "Levin", "Silva", "Navarro", "Petrov", "DeLuca", "Volkov",
        "Mizrahi", "Cohen", "Ricci", "Esposito", "Marino", "Greco", "Ferrari", "Rossi", "Conti", "Lombardi",
        "Santoro", "Caruso", "Bianchi", "Fontana", "Giordano", "Valenti", "Basile", "Orlando", "Pellegrini", "Sorrentino",
        "Benedetti", "Ferretti", "Galliano", "Marchetti", "Vitale", "Kowalski", "Novak", "Horvat", "Jovanovic", "Nikolic",
        "Popovic", "Kozlov", "Sokolov", "Nowak", "Wisniewski", "Kaminski", "Lewandowski", "Zielinski", "Szymanski", "Wozniak",
        "Dvorak", "Novotny", "Prochazka", "Nemec", "Murphy", "Sullivan", "Walsh", "Kelly", "Byrne", "OConnell",
        "Quinn", "Doyle", "Russo", "Gallo", "Martino", "Gaudio", "Longo", "Ferrara", "Costa", "Parise",
        "Fernandez", "Gutierrez", "Morales", "Castillo", "Vargas", "Mendoza", "Herrera", "Jimenez", "Alvarez", "Torres",
        "Ramirez", "Flores", "Aguilar", "Reyes", "Ortiz", "Cruz", "Diaz", "Ramos", "Vega", "Campos",
        "Delgado", "Fuentes", "Iglesias", "Sato", "Tanaka", "Nakamura", "Yamamoto", "Kim", "Park", "Nguyen"
    };

    static CharacterNamePools()
    {
        if (GivenNames.Length != 100 || FamilyNames.Length != 100)
            Debug.LogWarning(
                "[CharacterNamePools] Expected 100 given and 100 family names; counts are " +
                GivenNames.Length + " and " + FamilyNames.Length + ".");
        if (FemaleGivenNames.Length != 100)
            Debug.LogWarning("[CharacterNamePools] Expected 100 female given names; count is " + FemaleGivenNames.Length + ".");
    }

    public static string RandomFullName()
    {
        return RandomFullName(female: false);
    }

    /// <param name="female">Uses <see cref="FemaleGivenNames"/> when true, else <see cref="GivenNames"/>.</param>
    public static string RandomFullName(bool female)
    {
        string g = female
            ? FemaleGivenNames[Random.Range(0, FemaleGivenNames.Length)]
            : GivenNames[Random.Range(0, GivenNames.Length)];
        string f = FamilyNames[Random.Range(0, FamilyNames.Length)];
        return g + " " + f;
    }

    /// <summary>Fills <paramref name="dest"/> with distinct full names (retries if collision).</summary>
    public static void FillDistinctFullNames(string[] dest, int count)
    {
        FillDistinctFullNames(dest, count, female: false);
    }

    /// <summary>Fills <paramref name="dest"/> with distinct full names (retries if collision).</summary>
    public static void FillDistinctFullNames(string[] dest, int count, bool female)
    {
        if (dest == null || count <= 0)
            return;
        count = Mathf.Min(count, dest.Length);
        var used = new HashSet<string>();
        for (int i = 0; i < count; i++)
        {
            string candidate;
            int guard = 0;
            do
            {
                candidate = RandomFullName(female);
                guard++;
            } while (used.Contains(candidate) && guard < 80);

            used.Add(candidate);
            dest[i] = candidate;
        }
    }
}
