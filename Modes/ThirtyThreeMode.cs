using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TheSanctuary;

namespace DrakesBasketballCourtServer.Modes
{
    public class ThirtyThreeMode : IMode
    {
        public string currentPlayer { get; private set; }
        public int MaxPlayers { get; set; }
        public int MaxScores { get; set; }
        public Dictionary<string, Player> Players { get; set; } = new Dictionary<string, Player>();

        protected readonly Vector3 NET_COORDINATE = new Vector3(12.779f, 0, 0);
        protected readonly Vector3 FREE_THROW_LINE = new Vector3(8.35f, 0, 0);
        protected readonly Vector3 THROW_FORCE;
        protected readonly float MAX_X_COORDINATE = 13f;
        protected readonly float THREE_POINT_LINE_DISTANCE = 6.97f;

        public ThirtyThreeMode()
        {
            MaxPlayers = 5;
            MaxScores = 33;
        }



        public string GetNextPlayer()
        {
            var playersValues = Players.Values.ToList();
            var playersKeys = Players.Keys.ToList();

            var CurrentPlayerIndex = playersKeys.IndexOf(playersKeys.Find(x => x == currentPlayer));

            return currentPlayer != playersKeys.Last()? playersKeys[CurrentPlayerIndex + 1] : playersKeys.First();
        }

        public string[] OnGameInitialization(string[] MethodArgs)
        {
            currentPlayer = Players.First().Key;
            return new string[1] { currentPlayer };
        }

        public PlayerTransform OnPlayerMoving(string playerSessionId, PlayerTransform playerTransformData)
        {
            Players[playerSessionId].SetBallTransform(playerTransformData);
            return playerTransformData;
        }

        public Force OnBallThrowning(string playerSessionId, Force throwForceData)
        {
            string PlayerID = playerSessionId;
            Player player = Players[PlayerID];

            player.SetBallThrowForce(throwForceData);

            return throwForceData;
        }

        public string[] OnBallScoreGetting(string[] MethodArgs)
        {
            string PlayerID = MethodArgs[0];
            Player player = Players[PlayerID];
            Vector3 BallTransform = new Vector3();

            if (player.GameModeScores < 30)
            {
                player.GameModeScores += 3;
                return new string[2] {PlayerID, player.GameModeScores.ToString() };
            }
            else
            {
                if ((NET_COORDINATE - BallTransform).Length() > THREE_POINT_LINE_DISTANCE)
                {
                    player.GameModeScores += 1;
                    if (player.GameModeScores == MaxScores) return null;
                    return new string[2] { PlayerID, player.GameModeScores.ToString() };
                }
                else return new string[1] { GetNextPlayer() };
            }
        }

        public string[] OnBallParketGetting(string[] MethodArgs)
        {
            // 0    -   Player ID
            // 1    -   Collision point vector

            string[] responseData = new string[2]
            {
                GetNextPlayer(),
                MethodArgs[1]
            };
            return responseData;
        }

        public string[] OnGameEnding(string[] MethodArgs)
        {
            // 0    -   Player ID

            Account playerAccount = MainHub.DBContext.Get(Players[MethodArgs[0]].login);
            playerAccount.ThirtyThreeWinsCount++;

            MainHub.DBContext.Update(new DBAccountData(playerAccount)).Wait();

            return new string[1] { MethodArgs[0] };
        }
    }
}