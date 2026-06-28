namespace DHY.MG.Module.Sys.Dtos
{
    public class MatchSelectDto
    {
        public MatchSelectDto(string key, string val, bool isSelect = false, bool isDefault = false)
        {
            Key = key;
            Val = val;
            IsSelect = isSelect;
            IsDefault = isDefault;
        }

        public string Key { get; set; }

        public string Val { get; set; }

        public bool IsSelect { get; set; }

        public bool IsDefault { get; set; }
    }
}
