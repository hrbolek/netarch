namespace WebAppArchitecture.Events;

/// <summary>Fixed reference types transcribed from excel.babel.html, preserving identifiers and display values.</summary>
public static class StandardEventTypes
{
    public sealed record Definition(Guid Id, string Code, string Name, string Abbreviation, string Color);
    public static readonly IReadOnlyList<Definition> All = new Definition[]
    {
  new(Guid.Parse("69ec2b0b-a39d-40df-9cea-e295b36749c9"), "semester", "Semestr", "S", "#7f7f7f"),
  new(Guid.Parse("ac3238a2-a3ca-4f4b-a56b-8ac7c3953aff"), "winter-teaching", "výuka - zimní semestr", "V-ZS", "#ffffff"),
  new(Guid.Parse("78a6f015-b8f4-49c8-b218-7861454cb8e9"), "summer-teaching", "výuka - letní semestr", "V-LS", "#ffffff"),
  new(Guid.Parse("6fef77f1-a580-4e13-a088-30368a95af2f"), "annual-leave", "řádná dovolená", "ŘD", "#d9a441"),
  new(Guid.Parse("7e4187e5-b219-4332-b50e-54411541bba6"), "exam-period", "zkouškové období", "Z", "#f28c28"),
  new(Guid.Parse("79c71457-b926-449a-b227-a3461bf1df4c"), "field-training-theory", "příprava v poli - teorie", "PT", "#8bc34a"),
  new(Guid.Parse("539c45b2-629c-43ca-87cb-db753e7bbab9"), "field-training-practice", "příprava v poli - praxe", "PP", "#7cb342"),
  new(Guid.Parse("83d0a942-6456-4f35-bce8-37c9a08cb966"), "reserve", "rezerva", "R", "#ff1f1f"),
  new(Guid.Parse("5f147c3c-b307-4334-a339-6f4c83f1dad7"), "intensive-english", "intenzivní kurz AJ", "AJ", "#ffff33"),
  new(Guid.Parse("48ff1e53-83c8-48d6-84e6-b63e4f5fa60d"), "physical-training", "kurz TV", "TV", "#7a6000"),
  new(Guid.Parse("1af8ea7e-b280-4202-8efe-1df22763081e"), "professional-practice", "odborná praxe", "OP", "#bdbdbd"),
  new(Guid.Parse("0b624b6c-ed72-4316-b070-8e09f73e531c"), "staff-position-internship", "stáž na systematizovaném místě", "SSM", "#d9d9d9"),
  new(Guid.Parse("cd309340-cae0-4ec2-8adc-f4c61dc5f023"), "aviation-english", "letecká AJ", "LA", "#ffff66"),
  new(Guid.Parse("0f5713ca-7134-4c6a-99a0-7166c689dbfb"), "graduation", "vyřazení", "V", "#ff1f1f"),
  new(Guid.Parse("7edccbad-3c70-49ec-b5ca-b744bec8d234"), "applied-military-technologies", "aplikované vojenské technologie", "AVT", "#00b0f0"),
  new(Guid.Parse("d9494b47-b46c-4ba4-a900-1443751cabce"), "final-exam-preparation", "příprava na SZZ", "PSZZ", "#a64ac9"),
  new(Guid.Parse("eb86a21b-04e2-419c-a3a3-5f21603d349b"), "final-state-exam", "SZZ", "SZZ", "#8e24aa"),
  new(Guid.Parse("4d0d3e3f-4d45-4fc4-95f5-612905cf5823"), "sere-course", "kurz SERE, případně LV, CANI, PARA", "SERE", "#9fd3f2"),
  new(Guid.Parse("4560fca2-533b-45c1-945c-16dcf0935126"), "exams-or-flight-training", "zkouškové období / letecký výcvik", "ŘD/LV", "#d4af37"),
  new(Guid.Parse("3d5c8255-9abc-4d3e-aa25-b3984e5ce1a1"), "diploma-project", "diplomový projekt", "DP", "#d9d9d9")
    };
}
