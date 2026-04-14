namespace FamilyBusiness.CityGen.Government.Windows
{
    /// <summary>Batch 13: presentable right-pane action row — not executable yet.</summary>
    public sealed class GovernmentWindowActionModel
    {
        public string ActionKey { get; set; }
        public string DisplayLabel { get; set; }
        public bool IsEnabled { get; set; }
        public string DisabledReason { get; set; }
        public string IconKey { get; set; }
        public string Tooltip { get; set; }
        public string Tags { get; set; }
    }
}
