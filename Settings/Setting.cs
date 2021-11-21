namespace Crappy
{
    public class Setting
    {
        public SettingType SettingType { get; set; }
        public OptionType OptionType { get; set; }
        public string Value { get; set; }
        public int? Minimum { get; set; }
        public int? Maximum { get; set; }
        public string Default { get; set; }
    }
}