using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheSanctuary;
using DrakesBasketballCourtServer.Modes;

namespace DrakesBasketballCourtServer
{
    public class Room : RoomBase
    {
        public IMode GameMode { get; private set; }
        public bool IsOnline { get; private set; }

        public Room(int roomId, PersonalData creator, string creatorSessionId, MultiplayerMode gameMode)
        {
            ID = roomId;
            CreatorSessionID = creatorSessionId;

            switch(gameMode)
            {
                case MultiplayerMode.ThirtyThree:
                    GameMode = new ThirtyThreeMode();
                    GameModeName = "Thirty Three";
                    break;
                case MultiplayerMode.ThreePointContest:
                    GameModeName = "Three Point Contest";
                    break;
            }

            AddAccount(creator, creatorSessionId);
        }

        public void AddAccount(PersonalData acc, string sessionId)
        {
            GameMode.Players.Add(sessionId, new Player(acc));
            PlayersCounter++;
        }

        public void RemoveAccount(string sessionId)
        {
            GameMode.Players.Remove(sessionId);
            PlayersCounter--;
        }

        public void Init()
        {
            GameMode.OnGameInitialization(new string[0]);
            IsOnline = true;
        }
    }
}
