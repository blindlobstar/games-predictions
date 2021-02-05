using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using Akka.Actor;
using Akka.Event;
using DotaPredictions.Models.Steam;
using Polly;
using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.Dota.Internal;
using SteamKit2.Internal;

namespace DotaPredictions.Actors
{
    public class DotaClient : UntypedActor
    {
        #region Messages
        public class ServerSteamIdRequest
        {
            public ServerSteamIdRequest(ulong steamId)
            {
                SteamId = steamId;
            }

            public ulong SteamId { get; private set; } 
        }
        
        public class GameRealTimeStatsRequest
        {
            public GameRealTimeStatsRequest(ulong steamServerId)
            {
                SteamServerId = steamServerId;
            }

            public ulong SteamServerId { get; private set; }
        }

        public class GetMatchDetailsRequest
        {
            public GetMatchDetailsRequest(ulong matchId)
            {
                MatchId = matchId;
            }

            public ulong MatchId { get; private set; }
        }

        public class NoMatchDetails
        {
            public NoMatchDetails(ulong matchId)
            {
                MatchId = matchId;
            }

            public ulong MatchId { get; private set; }
        }

        #endregion

        const int APPID = 570;

        private readonly HttpClient _httpClient;
        private readonly ILoggingAdapter _log = Context.GetLogger();

        private readonly SteamClient _client;
        private readonly SteamUser _user;
        private readonly SteamGameCoordinator _gameCoordinator;
        private readonly CallbackManager _callbackMgr;
        private readonly string _apiKey;
        private readonly string _username;
        private readonly string _password;
        private bool isReady;
        private ulong _requestedMatchId;
        private bool IsConnected { get; set; }

        public DotaClient(SteamClient client, HttpClient httpClient,
            string apiKey, string username, string password)
        {
            _apiKey = apiKey;
            _username = username;
            _password = password;
            _httpClient = httpClient;
            _client = new SteamClient();
            _user = _client.GetHandler<SteamUser>();
            _gameCoordinator = _client.GetHandler<SteamGameCoordinator>();

            // setup callbacks
            _callbackMgr = new CallbackManager(_client);

            _callbackMgr.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            _callbackMgr.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            _callbackMgr.Subscribe<SteamGameCoordinator.MessageCallback>(OnGCMessage);
            _callbackMgr.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
        }

        public static Props Props(SteamClient client, HttpClient httpClient, string apiKey, string username, string password) =>
            Akka.Actor.Props.Create(() => new DotaClient(client, httpClient, apiKey, username, password));

        protected override void PreStart()
        {
            _client.Connect();
            while (!isReady)
            {
                _callbackMgr.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
            isReady = false;
            IsConnected = true;
            _log.Info($"DotaClient[{_username}] is ready!");
        }

        protected override void PostStop()
        {
            _log.Info($"DotaClient[{_username}] is stoped!");
            base.PostStop();
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case "isConnected":
                    Sender.Tell(IsConnected);
                    break;
                case ServerSteamIdRequest request:
                    _log.Info("DotaClient[{1}] ServerSteamIdRequest, steam_id: {0}", request.SteamId, _username);

                    var spectateRequest = new ClientGCMsgProtobuf<CMsgSpectateFriendGame>((uint)EDOTAGCMsg.k_EMsgGCSpectateFriendGame);
                    spectateRequest.Body.live = false;
                    spectateRequest.Body.steam_id = request.SteamId;
                    _gameCoordinator.Send(spectateRequest, APPID);
                    while (!isReady)
                    {
                        _callbackMgr.RunWaitCallbacks(TimeSpan.FromSeconds(1));
                    }
                    isReady = false;
                    break;
                case GameRealTimeStatsRequest request:
                    _log.Info("DotaClient[{1}] GameRealTimeStatsRequest, steam_server_id: {0}", request.SteamServerId, _username);

                    //Dota steam interface frequently sends Bad Request response
                    var policy = Policy.HandleResult<HttpResponseMessage>(msg => !msg.IsSuccessStatusCode)
                        .WaitAndRetryAsync(10, count => TimeSpan.FromMilliseconds(100 * count));
                    
                    var response = policy.ExecuteAsync(() => _httpClient.GetAsync(new Uri(
                            $"http://api.steampowered.com/IDOTA2MatchStats_570/GetRealtimeStats/v1/?key={_apiKey}&server_steam_id={request.SteamServerId}"))).Result;

                    if (!response.IsSuccessStatusCode)
                    {

                    }
                    Sender.Tell(response.Content.ReadFromJsonAsync<RealtimeStats>().Result);
                    break;
                case GetMatchDetailsRequest request:
                    _log.Info("DotaClient[{1}] GetMatchDetailsRequest, match_id: {0}", request.MatchId, _username);
                    
                    var requestMatch = new ClientGCMsgProtobuf<CMsgGCMatchDetailsRequest>((uint)EDOTAGCMsg.k_EMsgGCMatchDetailsRequest);
                    requestMatch.Body.match_id = request.MatchId;

                    _gameCoordinator.Send(requestMatch, APPID);

                    _requestedMatchId = request.MatchId;

                    while (!isReady)
                    {
                        _callbackMgr.RunWaitCallbacks(TimeSpan.FromSeconds(1));
                    }
                    isReady = false;
                    break;
            }
        }

        private void OnConnected(SteamClient.ConnectedCallback callback)
        {
            _user.LogOn(new SteamUser.LogOnDetails
            {
                Username = _username,
                Password = _password,
            });
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            _log.Warning("DotaClient[{0}] Disconnected", _username);
            Context.Stop(Self);
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                _log.Warning($"DotaClient[{_username}] Unable to connect to user: {_username}, status: {callback.Result.ToString()}");
                Context.Stop(Self);
            }

            var playGame = new ClientMsgProtobuf<CMsgClientGamesPlayed>(EMsg.ClientGamesPlayed);

            playGame.Body.games_played.Add(new CMsgClientGamesPlayed.GamePlayed
            {
                game_id = new GameID(APPID),
            });

            _client.Send(playGame);
            
            Thread.Sleep(5000);

            var clientHello = new ClientGCMsgProtobuf<CMsgClientHello>((uint) EGCBaseClientMsg.k_EMsgGCClientHello)
            {
                Body = {engine = ESourceEngine.k_ESE_Source2}
            };
            _gameCoordinator.Send(clientHello, APPID);
        }

        private void OnGCMessage(SteamGameCoordinator.MessageCallback callback)
        {
            switch (callback.EMsg)
            {
                case (uint)EGCBaseClientMsg.k_EMsgGCClientWelcome:
                    isReady = true;
                    break;
                case (uint)EDOTAGCMsg.k_EMsgGCSpectateFriendGameResponse:
                    var msg = new ClientGCMsgProtobuf<CMsgSpectateFriendGameResponse>(callback.Message);
                    Sender.Tell(msg.Body.server_steamid);
                    isReady = true;
                    break;
                case (uint)EDOTAGCMsg.k_EMsgGCMatchDetailsResponse:
                    isReady = true;
                    var matchDetails = new ClientGCMsgProtobuf<CMsgGCMatchDetailsResponse>(callback.Message);
                    var result = (EResult)matchDetails.Body.result;
                    if (result != EResult.OK)
                    {
                        Sender.Tell(new NoMatchDetails(_requestedMatchId));
                        _log.Info("DotaClient[{2}] Unable to request match details: {0}, for MatchId: {1}", result, _requestedMatchId, _username);
                        break;
                    }
                    var match = matchDetails.Body.match;
                    Sender.Tell(match);
                    break;
            }
        }


    }
}