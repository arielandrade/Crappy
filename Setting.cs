namespace Crappy
{
    public class Setting
    {
        public SettingType Type { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
        public int Minimum { get; set; }
        public int Maximum { get; set; }
        public int Default { get; set; }
    }
}