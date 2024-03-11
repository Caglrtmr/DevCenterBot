namespace DevCenterBot
{
    public class BotSettings //: BotSettingsBase
    {
        public string AOAI_ENDPOINT { get; set; }
        public string AOAI_KEY { get; set; }
        public string AOAI_DEPLOYMENTID { get; set; }
        public string SEARCH_INDEX_NAME { get; set; }
        public string SEARCH_SERVICE_NAME { get; set; }
        public string SEARCH_QUERY_KEY { get; set; }

        public string SettingForPrompt { get; set; }
        public string SettingForTemperature { get; set; }
        public string SettingForMaxToken { get; set; }
        public string SettingForTopK { get; set; }
    }
}
