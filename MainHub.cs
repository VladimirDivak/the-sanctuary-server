using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using TheSanctuary;

namespace DrakesBasketballCourtServer
{
    public class MainHub : Hub
    {
        public static DataBaseHandler DBContext = new DataBaseHandler();

        public static int playersCounter = 0;
        public static int roomsCounter = 0;

        public static List<Room> Rooms = new List<Room>();

        public override Task OnConnectedAsync()
        {
            playersCounter++;
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            playersCounter--;
            Room room = Rooms.Find(x => x.GameMode.Players.Keys.Contains(Context.ConnectionId));
            var roomData = JsonConvert.SerializeObject(room);

            if (room != null)
            {
                if (room.IsOnline)
                {
                    // ServerConsole.CreateLogMessage($"Игрок {room.GameMode.Players[Context.ConnectionId].login} покинул игровую комнату во время игры", MessageType.Warning);

                    Rooms.Remove(room);
                    roomsCounter--;
                }
                else
                {
                    if (Context.ConnectionId == room.GameMode.Players.Keys.First())
                    {
                        Rooms.Remove(room);
                        roomsCounter--;
                    }
                    else
                    {
                        if(room.GameMode.Players.Count == room.GameMode.MaxPlayers)
                        {
                            Clients.All.SendAsync("PlayerOnRoomOpen", roomData);
                        }
                        room.RemoveAccount(Context.ConnectionId);
                    }

                }

                Groups.RemoveFromGroupAsync(Context.ConnectionId, room.ID.ToString()).Wait();
                Clients.Group(room.ID.ToString()).SendAsync("PlayerOnDisconnection", Context.ConnectionId).Wait();
            }

            return base.OnDisconnectedAsync(exception);
        }

        public async Task ServerLoginChecker(string login)
        {
            if (DBContext.Check(login))
            {
                // ServerConsole.CreateLogMessage($"Пользователь попытался зарегистрировать существующий логин: {login}", MessageType.Warning);
                await Clients.Caller.SendAsync("PlayerRegistrationException");
            }
        }

        public async Task ServerRegistration(string accountData)
        {
            var account = JsonConvert.DeserializeObject<Account>(accountData);
            // ServerConsole.CreateLogMessage($"Зарегистрирован новый пользователь: {account.login}", MessageType.Default);
            await DBContext.Create(new DBAccountData(account));
        }

        public async Task ServerAuthorzation(string login, string password)
        {
            var Data = (PersonalData)DBContext.Get(login, password);
            var responseData = JsonConvert.SerializeObject(Data);
            if(Data != null)
            {
                // ServerConsole.CreateLogMessage($"Авторизован пользователь: {Data.login}", MessageType.Default);
                await Clients.Caller.SendAsync("PlayerAuthorization", responseData, Context.ConnectionId);
            }
            else await Clients.Caller.SendAsync("PlayerAuthorization", string.Empty, string.Empty);
        }

        public async Task ServerGetTracksData()
        {

            await Clients.Caller.SendAsync("PlayerGetTracksData");
        }



        //////////////////////////////////////////
        // логика работы игровых комнат (лобби) //
        //////////////////////////////////////////
        //////////////////////////////////////////
        //           создание комнат            //
        //////////////////////////////////////////

        public async Task ServerGetRoomsList()
        {
            if (Rooms.Count != 0)
            {
                List<RoomBase> roomsData = new List<RoomBase>();

                foreach (var room in Rooms)
                {
                    var roomBase = (RoomBase)room;
                    roomsData.Add(roomBase);
                }

                var responseData = JsonConvert.SerializeObject(roomsData.ToArray());

                await Clients.Caller.SendAsync("PlayerOnRoomsListRequest", responseData);
            }
            else await Clients.Caller.SendAsync("PlayerOnRoomsListRequest", string.Empty);
        }

        public async Task ServerOnNewRoomRequest(string login, string modeName)
        {
            if (Rooms.Count == 64)
            {
                // ServerConsole.CreateLogMessage($"Игрок попытался создать комнату, но получил отказ из-за лимита сервера", MessageType.Warning);
                await Clients.Caller.SendAsync("PlayerOnNewRoomCreated", string.Empty);
                return;
            }

            MultiplayerMode mode = 0;
            switch (modeName)
            {
                case "ThirtyThree":
                    mode = MultiplayerMode.ThirtyThree;
                    break;
                case "ThreePointContest":
                    mode = MultiplayerMode.ThreePointContest;
                    break;
            }

            var creatorAccount = (PersonalData)DBContext.Get(login);

            Room newRoom = new Room(Rooms.Count, creatorAccount, Context.ConnectionId, mode);
            Rooms.Add(newRoom);
            roomsCounter++;

            var roomData = JsonConvert.SerializeObject(newRoom);

            await Groups.AddToGroupAsync(Context.ConnectionId, newRoom.ID.ToString());
            await Clients.All.SendAsync("PlayerOnNewRoomCreated", roomData);

            // ServerConsole.CreateLogMessage($"Игрок {creatorAccount.login} создал комнату с режимом {newRoom.GameModeName}", MessageType.Default);
        }

        public async Task ServerOnRoomConnection(string login, string roomIDdata)
        {
            var roomId = JsonConvert.DeserializeObject<int>(roomIDdata);

            var newAccount = (PersonalData)DBContext.Get(login);
            var accountData = JsonConvert.SerializeObject(newAccount);

            Room room = Rooms.Find(x => x.ID == roomId);

            List<bool> playersRaeadyStatus = new List<bool>();
            foreach (var player in room.GameMode.Players.Values) playersRaeadyStatus.Add(player.ReadyStatus);

            var playersAccounts = new List<Player>(room.GameMode.Players.Values);
            var accountsData = new Dictionary<string, PersonalData>();

            for(int i = 0; i < playersAccounts.Count; i++)
            {
                var playerAccount = (PersonalData)DBContext.Get(playersAccounts[i].login);
                var idList = room.GameMode.Players.Keys.ToList();
                accountsData.Add(idList[i], playerAccount);
            }

            var accounts = JsonConvert.SerializeObject(accountsData);
            var readyStatusList = JsonConvert.SerializeObject(playersRaeadyStatus);

            await Clients.Caller.SendAsync("PlayerGetRoomData", room.CreatorSessionID, room.GameModeName, accounts, readyStatusList);
            await Clients.Group(room.ID.ToString()).SendAsync("PlayerOnConnection", Context.ConnectionId, accountData);
            await Groups.AddToGroupAsync(Context.ConnectionId, room.ID.ToString());
            room.AddAccount(newAccount, Context.ConnectionId);

            if (room.GameMode.Players.Count < room.GameMode.MaxPlayers)
                await Clients.All.SendAsync("PlayerOnRoomClosed", room.ID.ToString());
        }

        public async Task ServerOnRoomDisconnection()
        {
            Room room = Rooms.Find(x => x.GameMode.Players.Keys.Contains(Context.ConnectionId));

            if (room != null)
            {
                if (room.IsOnline)
                {
                    // ServerConsole.CreateLogMessage($"Игрок {room.GameMode.Players[Context.ConnectionId].login} покинул игровую комнату во время игры", MessageType.Warning);

                    Rooms.Remove(room);
                    roomsCounter--;
                }
                else
                {
                    if (Context.ConnectionId == room.GameMode.Players.Keys.First())
                    {
                        Rooms.Remove(room);
                        roomsCounter--;
                    }
                    else room.RemoveAccount(Context.ConnectionId);

                }

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.ID.ToString());
                await Clients.Group(room.ID.ToString()).SendAsync("PlayerOnDisconnection", Context.ConnectionId);
            }
        }

        public async Task ServerOnPlayerReadyStatusChanged(string valueData)
        {
            var value = JsonConvert.DeserializeObject<bool>(valueData);
            var room = Rooms.Find(x => x.GameMode.Players.Keys.Contains(Context.ConnectionId));
            var gameMode = room.GameMode;

            gameMode.Players[Context.ConnectionId].SetGameReadyStatus(value);

            var playersReadyList = gameMode.Players.Values.Where(x => x.ReadyStatus).ToList();
            if (playersReadyList.Count == gameMode.Players.Count)
            {
                await Clients.Group(room.ID.ToString()).SendAsync("PlayerOnGameInitialization",
                    JsonConvert.SerializeObject(gameMode.OnGameInitialization(new string[0])));
                await Clients.All.SendAsync("PlayerOnRoomClosed", room.ID.ToString());
            }
            else await Clients.Group(room.ID.ToString()).SendAsync("PlayerOnReadyStatusChanged",
                Context.ConnectionId);
        }


        //////////////////////////////////////////
        //       события игровых режимов        //
        //////////////////////////////////////////

        public async Task ServerOnPlayerMoving(string ballTransform)
        {
            PlayerTransform ballTransformData = JsonConvert.DeserializeObject<PlayerTransform>(ballTransform);
            string playerId = Context.ConnectionId;
            Room room = Rooms.Find(x => x.GameMode.Players.Keys.Contains(Context.ConnectionId));

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.ID.ToString());
            await Clients.Group(room.ID.ToString()).SendAsync("PlayerOnBallMoving",
                playerId,
                JsonConvert.SerializeObject(room.GameMode.OnPlayerMoving(playerId, ballTransformData)));
            await Groups.AddToGroupAsync(Context.ConnectionId, room.ID.ToString());
        }

        public async Task ServerOnBallThrowning(string throwForce)
        {
            Force throwForceData = JsonConvert.DeserializeObject<Force>(throwForce);
            string playerId = Context.ConnectionId;
            Room room = Rooms.Find(x => x.GameMode.Players.Keys.Contains(playerId));

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.ID.ToString());
            await Clients.Group(room.ID.ToString()).SendAsync("PlayerOnBallThrowing",
                playerId,
                JsonConvert.SerializeObject(room.GameMode.OnBallThrowning(playerId, throwForceData)));
            await Groups.AddToGroupAsync(Context.ConnectionId, room.ID.ToString());
        }

        public async Task ServerOnBallGetScore(string data)
        {
            string[] eventArgs = JsonConvert.DeserializeObject<string[]>(data);

            string playerId = Context.ConnectionId;
            string playerThrowPosition = eventArgs[0];

            Room room = Rooms.Find(x => x.GameMode.Players.Keys.Contains(playerId));

            var modeData = new string[2]
            {
                playerId,
                playerThrowPosition
            };

            var gameModeResponseData = room.GameMode.OnBallScoreGetting(modeData);
            if (gameModeResponseData == null)
            {
                await Clients.Group(room.ID.ToString()).SendAsync("PlayerOnGameEnding", playerId);
            }
            else
            {
                await Clients.Group(room.ID.ToString()).SendAsync("PlayerOnBallScoreGetting", gameModeResponseData);
            }
        }

        public async Task ServerOnBallParketGetting(string data)
        {
            var room = Rooms.Find(x => x.GameMode.Players.Keys.Contains(Context.ConnectionId));
            string[] eventArgs = JsonConvert.DeserializeObject<string[]>(data);

            string playerId = Context.ConnectionId;
            string pointVector = eventArgs[0];

            var modeData = new string[2]
            {
                playerId,
                pointVector
            };

            await Clients.Group(room.ID.ToString()).SendAsync("PlayerOnBallParketGetting",
               JsonConvert.SerializeObject(room.GameMode.OnBallParketGetting(modeData)));
        }

        public async Task ServerOnGameEnding()
        {
            var playerId = Context.ConnectionId;
            var room = Rooms.Find(x => x.GameMode.Players.Keys.Contains(Context.ConnectionId));
            var modeData = new string[1] { playerId };

            room.GameMode.OnGameEnding(modeData);
            Rooms.Remove(room);

            await Clients.Group(room.ID.ToString()).SendAsync("PlayerOnGameEnding", playerId);
        }
    }
}