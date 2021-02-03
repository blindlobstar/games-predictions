using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DotaPredictions.Models.Steam
{
    public class Match
    {
        [JsonPropertyName("server_steam_id")]
        public long ServerSteamId { get; set; }

        [JsonPropertyName("matchid")]
        public long Matchid { get; set; }

        [JsonPropertyName("timestamp")]
        public int Timestamp { get; set; }

        [JsonPropertyName("game_time")]
        public int GameTime { get; set; }

        [JsonPropertyName("steam_broadcaster_account_ids")]
        public List<int> SteamBroadcasterAccountIds { get; set; }

        [JsonPropertyName("game_mode")]
        public int GameMode { get; set; }

        [JsonPropertyName("league_id")]
        public int LeagueId { get; set; }
    }

    public class Player
    {
        [JsonPropertyName("accountid")]
        public int Accountid { get; set; }

        [JsonPropertyName("playerid")]
        public int Playerid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("team")]
        public int Team { get; set; }

        [JsonPropertyName("heroid")]
        public int Heroid { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("kill_count")]
        public int KillCount { get; set; }

        [JsonPropertyName("death_count")]
        public int DeathCount { get; set; }

        [JsonPropertyName("assists_count")]
        public int AssistsCount { get; set; }

        [JsonPropertyName("denies_count")]
        public int DeniesCount { get; set; }

        [JsonPropertyName("lh_count")]
        public int LhCount { get; set; }

        [JsonPropertyName("gold")]
        public int Gold { get; set; }

        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }
    }

    public class Team
    {
        [JsonPropertyName("team_number")]
        public int TeamNumber { get; set; }

        [JsonPropertyName("team_id")]
        public int TeamId { get; set; }

        [JsonPropertyName("team_name")]
        public string TeamName { get; set; }

        [JsonPropertyName("team_logo")]
        public int TeamLogo { get; set; }

        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("players")]
        public List<Player> Players { get; set; }
    }

    public class Building
    {
        [JsonPropertyName("team")]
        public int Team { get; set; }

        [JsonPropertyName("heading")]
        public double Heading { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("lane")]
        public int Lane { get; set; }

        [JsonPropertyName("tier")]
        public int Tier { get; set; }

        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("destroyed")]
        public bool Destroyed { get; set; }
    }

    public class GraphData
    {
        [JsonPropertyName("graph_gold")]
        public List<int> GraphGold { get; set; }
    }

    public class RealtimeStats
    {
        [JsonPropertyName("match")]
        public Match Match { get; set; }

        [JsonPropertyName("teams")]
        public List<Team> Teams { get; set; }

        [JsonPropertyName("buildings")]
        public List<Building> Buildings { get; set; }

        [JsonPropertyName("graph_data")]
        public GraphData GraphData { get; set; }

        [JsonPropertyName("delta_frame")]
        public bool DeltaFrame { get; set; }
    }
}
