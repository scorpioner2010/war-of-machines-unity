namespace Game.Scripts.Server
{
    public static class RemoteServerSettings
    {
        public static int MaxPlayersForFindRoom { get; private set; } = 1;
        public static int FindRoomSeconds { get; private set; } = 60;
        public static bool IsLoaded { get; private set; }

        public static void Apply(int maxPlayersForFindRoom, int findRoomSeconds)
        {
            MaxPlayersForFindRoom = maxPlayersForFindRoom > 0 ? maxPlayersForFindRoom : 1;
            FindRoomSeconds = findRoomSeconds > 0 ? findRoomSeconds : 60;
            IsLoaded = true;
        }
    }
}
