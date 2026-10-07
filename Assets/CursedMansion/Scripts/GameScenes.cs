namespace CursedMansion
{
    /// <summary>
    /// Имена сцен должны совпадать с File → Build Settings и именами .unity файлов.
    /// </summary>
    public static class GameScenes
    {
        public const string Road = "Road";
        public const string HouseScene = "HouseScene";
        public const string Interior = "Interior";
        public const string InsideHouse = "InsideHouse";
        // Арена с волнами монстров (Assets/_GAME): своя логика снаряжения и урона, хуки особняка ей не нужны
        public const string Arena = "SampleScene";
    }
}
