namespace Game.Scripts.Networking.Lobby
{
    public static class BattleRules
    {
        public const string ApiResultWin = "win";
        public const string ApiResultLose = "lose";
        public const string ApiResultDraw = "draw";

        public static BattleEndState EvaluateEnd(ServerRoom serverRoom)
        {
            BattleEndState state = new BattleEndState
            {
                ShouldFinish = false,
                IsDraw = false,
                WinnerTeam = MatchTeam.None
            };

            if (serverRoom == null || serverRoom.isGameFinished)
            {
                return state;
            }

            int redAlive = 0;
            int blueAlive = 0;
            int unassignedAlive = 0;
            int aliveRobots = 0;

            foreach (Player player in serverRoom.GetPlayers())
            {
                if (!IsAliveInBattle(player))
                {
                    continue;
                }

                aliveRobots++;
                if (player.team == MatchTeam.Red)
                {
                    redAlive++;
                }
                else if (player.team == MatchTeam.Blue)
                {
                    blueAlive++;
                }
                else
                {
                    unassignedAlive++;
                }
            }

            state.IsDraw = aliveRobots == 0;
            if (state.IsDraw)
            {
                state.ShouldFinish = true;
                return state;
            }

            if (unassignedAlive == 0 && redAlive > 0 && blueAlive == 0)
            {
                state.ShouldFinish = true;
                state.WinnerTeam = MatchTeam.Red;
            }
            else if (unassignedAlive == 0 && blueAlive > 0 && redAlive == 0)
            {
                state.ShouldFinish = true;
                state.WinnerTeam = MatchTeam.Blue;
            }

            return state;
        }

        public static bool ShouldReceiveEndGame(Player player)
        {
            return player != null
                   && !player.isBot
                   && player.Connection != null
                   && !player.leftBattle
                   && player.playerRoot != null;
        }

        public static EndGameResult GetEndGameResult(Player player, MatchTeam winnerTeam, bool isDraw)
        {
            if (player != null && player.leftBattle)
            {
                return EndGameResult.Lose;
            }

            if (isDraw)
            {
                return EndGameResult.Draw;
            }

            return player != null && player.team == winnerTeam ? EndGameResult.Win : EndGameResult.Lose;
        }

        public static string ToApiResult(EndGameResult result)
        {
            if (result == EndGameResult.Win)
            {
                return ApiResultWin;
            }

            if (result == EndGameResult.Draw)
            {
                return ApiResultDraw;
            }

            return ApiResultLose;
        }

        private static bool IsAliveInBattle(Player player)
        {
            return player != null
                   && !player.leftBattle
                   && player.playerRoot != null
                   && player.playerRoot.health != null
                   && !player.playerRoot.health.IsDead;
        }
    }
}
