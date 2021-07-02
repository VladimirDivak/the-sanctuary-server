using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TheSanctuary;

namespace DrakesBasketballCourtServer
{
    public class DBAccountData : Account
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; private set; }

        public DBAccountData(Account acc)
        {
            login = acc.login;
            password = acc.password;

            baseColor = acc.baseColor;
            linesColor = acc.linesColor;
            usePattern = acc.usePattern;
            patternName = acc.patternName;

            ballThrowsCount = acc.ballThrowsCount;
            ThirtyThreeWinsCount = acc.ThirtyThreeWinsCount;
            ThreePointContestWinsCount = acc.ThreePointContestWinsCount;
            TimerChallengeWinsCount = acc.TimerChallengeWinsCount;
        }
    }
}
