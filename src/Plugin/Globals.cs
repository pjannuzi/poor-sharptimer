/*
Copyright (C) 2024 Dea Brcka

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        public string compileTimeStamp = new DateTime(CompileTimeStamp.CompileTime, DateTimeKind.Utc).ToString();

        public override string ModuleName => "SharpTimer";
        public override string ModuleVersion => $"0.3.0g";
        public override string ModuleAuthor => "dea https://github.com/deafps/";
        public override string ModuleDescription => "A CS2 Timer Plugin";

        public Dictionary<int, PlayerTimerInfo> playerTimers = [];
        private Dictionary<int, PlayerJumpStats> playerJumpStats = [];
        private Dictionary<int, PlayerReplays> playerReplays = [];
        private Dictionary<int, List<PlayerCheckpoint>> playerCheckpoints = [];
        public Dictionary<int, CCSPlayerController> connectedPlayers = [];
        private Dictionary<int, CCSPlayerController> connectedReplayBots = [];
        private Dictionary<uint, CCSPlayerController> specTargets = [];
        private Dictionary<nint, TriggerPushData> triggerPushData = [];
        private EntityCache? entityCache;
        public Dictionary<string, PlayerRecord>? SortedCachedRecords = [];
        private static readonly HttpClient httpClient = new();

        public static JsonSerializerOptions jsonSerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public string primaryHUDcolor = "green";
        public string secondaryHUDcolor = "orange";
        public string tertiaryHUDcolor = "white";
        public string primaryChatColor = "";
        public char NewLine = '\u2029';
        public string startBeamColor = "";
        public string endBeamColor = "";
        public bool beamColorOverride = false;

        private bool useStageTriggers = false;
        public Vector? currentMapStartTriggerMins = null;
        public Vector? currentMapStartTriggerMaxs = null;

        public Vector? currentRespawnPos = null;
        public QAngle? currentRespawnAng = null;
        public string currentMapStartTrigger = "trigger_startzone";
        public string currentMapEndTrigger = "trigger_endzone";
        public Vector currentMapStartC1 = new(0, 0, 0);
        public Vector currentMapStartC2 = new(0, 0, 0);
        public Vector currentMapEndC1 = new(0, 0, 0);
        public Vector currentMapEndC2 = new(0, 0, 0);
        public Vector? currentEndPos = null;

        private Dictionary<nint, int> cpTriggers = [];
        public int cpTriggerCount;
        private bool useCheckpointTriggers = false;

        private Dictionary<int, Vector?> bonusRespawnPoses = [];
        private Dictionary<int, QAngle?> bonusRespawnAngs = [];
        public string currentBonusStartTrigger = "b1_start";
        public string currentBonusEndTrigger = "b1_end";
        public Vector[] currentBonusStartC1 = new Vector[10];
        public Vector[] currentBonusStartC2 = new Vector[10];
        public Vector[] currentBonusEndC1 = new Vector[10];
        public Vector[] currentBonusEndC2 = new Vector[10];
        public Vector[] currentBonusEndPos = new Vector[10];

        private Dictionary<nint, int> bonusCheckpointTriggers = [];
        private int bonusCheckpointTriggerCount;
        private bool useBonusCheckpointTriggers = false;

        public int[] totalBonuses = new int[11];

        public string[]? currentMapOverrideDisableTelehop = [];
        public string[]? currentMapOverrideMaxSpeedLimit = [];
        public bool currentMapOverrideStageRequirement = false;
        public bool currentMapOverrideTriggerPushFix = false;

        private Dictionary<nint, int> stageTriggers = [];
        private Dictionary<int, Vector?> stageTriggerPoses = [];
        private Dictionary<int, QAngle?> stageTriggerAngs = [];
        public int stageTriggerCount;

        public string? currentMapType = null;
        public int? currentMapTier = null;

        public bool isLinux = true;
        public bool enableDebug = true;
        public bool killServerCommands = true;
        public bool useMySQL = false;
        public bool usePostgres = false;
        public DatabaseType dbType;
        public string dbPath = "";
        public bool enableDb = false;
        public bool ignoreJSON = false;
        public bool enableReplays = false;
        public bool enableSRreplayBot = false;
        public bool startKickingAllFuckingBotsExceptReplayOneIFuckingHateValveDogshitFuckingCompanySmile = false;
        public int maxReplayFrames = 19200;

        public bool globalRanksEnabled = false;
        public bool globalRanksFreePointsEnabled = true;
        public int maxGlobalFreePoints = 20;
        public float? globalPointsMultiplier = 1.0f;
        public int minGlobalPointsForRank = 1000;
        public bool displayChatTags = true;
        public bool displayScoreboardTags = true;
        public string customVIPTag = "VIP";
        //public string vipGifHost = "https://files.catbox.moe";

        public bool useTriggers = true;
        public bool useTriggersAndFakeZones = false;

        public bool respawnEnabled = true;
        public bool respawnEndEnabled = false;

        public bool keysOverlayEnabled = true;
        public bool hudOverlayEnabled = true;

        public bool topEnabled = true;
        public bool rankEnabled = true;
        public bool helpEnabled = true;
        public bool alternativeSpeedometer = false;
        public bool startzoneJumping = true;
        public bool spawnOnRespawnPos = false;
        public bool enableNoclip = false;

        public bool enableStyles = true;
        public bool enableStylePoints = true;

        public bool removeLegsEnabled = false;
        public bool hideAllPlayers = false;
        public bool removeCollisionEnabled = true;
        public bool disableDamage = true;
        public bool use2DSpeed = false;

        public bool cpEnabled = false;
        public bool removeCpRestrictEnabled = false;
        public bool cpOnlyWhenTimerStopped = false;

        public bool connectMsgEnabled = true;
        public bool cmdJoinMsgEnabled = true;
        public bool autosetHostname = false;

        public bool adServerRecordEnabled = true;
        public bool isADServerRecordTimerRunning = false;
        public int adServerRecordTimer = 120;

        public bool adMessagesEnabled = true;
        public bool isADMessagesTimerRunning = false;
        public int adMessagesTimer = 120;

        public int rankHUDTimer = 170;
        public bool isRankHUDTimerRunning = false;

        public bool resetTriggerTeleportSpeedEnabled = false;
        public bool maxStartingSpeedEnabled = true;
        public int maxStartingSpeed = 320;
        public int maxBonusStartingSpeed = 320;

        public bool removeCrouchFatigueEnabled = true;
        public bool goToEnabled = false;
        public bool fovChangerEnabled = true;
        public bool triggerPushFixEnabled = false;
        public int cmdCooldown = 64;
        public float fakeTriggerHeight = 50;
        public bool Box3DZones = false;
        public int altVeloMaxSpeed = 3000;
        public bool forcePlayerSpeedEnabled = false;
        public float forcedPlayerSpeed = 250;
        public int bhopBlockTime = 16;

        public double lowgravPointModifier = 1.1;
        public double sidewaysPointModifier = 1.3;
        public double halfSidewaysPointModifier = 1.3;
        public double onlywPointModifier = 1.33;
        public double onlyaPointModifier = 1.33;
        public double onlysPointModifier = 1.33;
        public double onlydPointModifier = 1.33;
        public double velPointModifier = 1.5;
        public double highgravPointModifier = 1.3;
        public double fastForwardPointModifier = 1.3;

        public bool jumpStatsEnabled = false;
        public float jumpStatsMinDist = 175;
        public float jumpStatsMaxVert = 32;
        public bool movementUnlockerCapEnabled = true;
        public float movementUnlockerCapValue = 250;

        public bool execCustomMapCFG = false;

        public bool sqlCheck = false;

        public bool soundsEnabledByDefault = false;
        public string timerSound = "sounds/ui/counter_beep.vsnd";
        public string respawnSound = "sounds/buttons/button9.vsnd";
        public string cpSound = "sounds/ui/buttonclick.vsnd";
        public string cpSoundError = "sounds/ui/weapon_cant_buy.vsnd";
        public string tpSound = "sounds/buttons/blip1.vsnd";
        public string pbSound = "sounds/buttons/bell1.vsnd";
        public string srSound = "sounds/ui/panorama/round_report_round_won_01.vsnd";
        public bool srSoundAll = true;

        public string? gameDir;
        public string? mySQLpath;
        public string? postgresPath;
        public string? PlayerStatsTable;
        public string? playerRecordsPath;
        public string? currentMapName;
        public string? defaultServerHostname = ConVar.Find("hostname")?.StringValue;

        public bool discordWebhookEnabled = false;
        public string discordWebhookBotName = "SharpTimer";
        public string discordWebhookPFPUrl = "https://cdn.discordapp.com/icons/1196646791450472488/634963a8207fdb1b30bf909d31f05e57.webp";
        public string discordWebhookImageRepoURL = "https://raw.githubusercontent.com/Letaryat/poor-SharpTimerDiscordWebhookMapPics/main/images/";
        public string? discordPBWebhookUrl;
        public string? discordSRWebhookUrl;
        public string? discordPBBonusWebhookUrl;
        public string? discordSRBonusWebhookUrl;
        public string? discordWebhookFooter;
        public int discordWebhookRareGifOdds;
        public string? discordWebhookRareGif;
        public bool discordWebhookPrintSR = false;
        public bool discordWebhookPrintPB = false;

        public string? remoteBhopDataSource = "https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/bhop_.json";
        public string? remoteKZDataSource = "https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/kz_.json";
        public string? remoteSurfDataSource = "https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/surf_.json";
        public string? testerPersonalGifsSource = "https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/tester_bling.json";

        public static string god3Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/god.gif' class=''>";
        public static string god2Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/god.gif' class=''>";
        public static string god1Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/god.gif' class=''>";
        public static string royalty3Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/royal3.png' class=''>";
        public static string royalty2Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/royal2.png' class=''>";
        public static string royalty1Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/royal1.png' class=''>";
        public static string legend3Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/legend3.png' class=''>";
        public static string legend2Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/legend2.png' class=''>";
        public static string legend1Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/legend1.png' class=''>";
        public static string master3Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/master3.png' class=''>";
        public static string master2Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/master2.png' class=''>";
        public static string master1Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/master1.png' class=''>";
        public static string diamond3Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/dia3.png' class=''>";
        public static string diamond2Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/dia2.png' class=''>";
        public static string diamond1Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/dia1.png' class=''>";
        public static string platinum3Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/plat3.png' class=''>";
        public static string platinum2Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/plat2.png' class=''>";
        public static string platinum1Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/plat1.png' class=''>";
        public static string gold3Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/gold3.png' class=''>";
        public static string gold2Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/gold2.png' class=''>";
        public static string gold1Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/gold1.png' class=''>";
        public static string silver3Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/silver3.png' class=''>";
        public static string silver2Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/silver2.png' class=''>";
        public static string silver1Icon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/silver1.png' class=''>";
        public static string unrankedIcon = "<img src='https://raw.githubusercontent.com/Letaryat/poor-SharpTimer/main/remote_data/rank_icons/unranked.png' class=''>";

        public struct WeaponSpeedStats
        {
            public double Running { get; }
            public double Walking { get; }

            public WeaponSpeedStats(double running, double walking)
            {
                Running = running;
                Walking = walking;
            }

            public double GetSpeed(bool isWalking)
            {
                return isWalking ? Walking : Running;
            }
        }

        readonly Dictionary<string, WeaponSpeedStats> weaponSpeedLookup = new()
        {
            {"weapon_glock", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_usp_silencer", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_hkp2000", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_elite", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_p250", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_fiveseven", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_cz75a", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_deagle", new WeaponSpeedStats(230.00, 119.60)},
            {"weapon_revolver", new WeaponSpeedStats(220.00, 114.40)},
            {"weapon_nova", new WeaponSpeedStats(220.00, 114.40)},
            {"weapon_xm1014", new WeaponSpeedStats(215.00, 111.80)},
            {"weapon_sawedoff", new WeaponSpeedStats(210.00, 109.20)},
            {"weapon_mag7", new WeaponSpeedStats(225.00, 117.00)},
            {"weapon_m249", new WeaponSpeedStats(195.00, 101.40)},
            {"weapon_negev", new WeaponSpeedStats(150.00, 78.00)},
            {"weapon_mac10", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_mp7", new WeaponSpeedStats(220.00, 114.40)},
            {"weapon_mp9", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_mp5sd", new WeaponSpeedStats(235.00, 122.20)},
            {"weapon_ump45", new WeaponSpeedStats(230.00, 119.60)},
            {"weapon_p90", new WeaponSpeedStats(230.00, 119.60)},
            {"weapon_bizon", new WeaponSpeedStats(240.00, 124.80)},
            {"weapon_galilar", new WeaponSpeedStats(215.00, 111.80)},
            {"weapon_famas", new WeaponSpeedStats(220.00, 114.40)},
            {"weapon_ak47", new WeaponSpeedStats(215.00, 111.80)},
            {"weapon_m4a4", new WeaponSpeedStats(225.00, 117.00)},
            {"weapon_m4a1_silencer", new WeaponSpeedStats(225.00, 117.00)},
            {"weapon_ssg08", new WeaponSpeedStats(230.00, 119.60)},
            {"weapon_sg556", new WeaponSpeedStats(210.00, 109.20)},
            {"weapon_aug", new WeaponSpeedStats(220.00, 114.40)},
            {"weapon_awp", new WeaponSpeedStats(200.00, 104.00)},
            {"weapon_g3sg1", new WeaponSpeedStats(215.00, 111.80)},
            {"weapon_scar20", new WeaponSpeedStats(215.00, 111.80)},
            {"weapon_molotov", new WeaponSpeedStats(245.00, 127.40)},
            {"weapon_incgrenade", new WeaponSpeedStats(245.00, 127.40)},
            {"weapon_decoy", new WeaponSpeedStats(245.00, 127.40)},
            {"weapon_flashbang", new WeaponSpeedStats(245.00, 127.40)},
            {"weapon_hegrenade", new WeaponSpeedStats(245.00, 127.40)},
            {"weapon_smokegrenade", new WeaponSpeedStats(245.00, 127.40)},
            {"weapon_taser", new WeaponSpeedStats(245.00, 127.40)},
            {"item_healthshot", new WeaponSpeedStats(250.00, 130.00)},
            {"weapon_knife_t", new WeaponSpeedStats(250.00, 130.00)},
            {"weapon_knife", new WeaponSpeedStats(250.00, 130.00)},
            {"weapon_c4", new WeaponSpeedStats(250.00, 130.00)},
            {"no_knife", new WeaponSpeedStats(260.00, 130.56)} //no knife
        };
    }
}