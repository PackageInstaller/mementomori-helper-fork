﻿using System.Reactive.Subjects;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using MementoMori.Ortega.Share;
using MementoMori.Ortega.Share.Data;
using MementoMori.Ortega.Share.Data.ApiInterface;
using MementoMori.Ortega.Share.Data.ApiInterface.Auth;
using MementoMori.Ortega.Share.Data.ApiInterface.Battle;
using MementoMori.Ortega.Share.Data.ApiInterface.Equipment;
using MementoMori.Ortega.Share.Data.ApiInterface.Friend;
using MementoMori.Ortega.Share.Data.ApiInterface.LoginBonus;
using MementoMori.Ortega.Share.Data.ApiInterface.Present;
using MementoMori.Ortega.Share.Data.ApiInterface.User;
using MementoMori.Ortega.Share.Data.ApiInterface.Vip;
using MementoMori.Ortega.Share.Data.DtoInfo;
using MementoMori.Ortega.Share.Data.Equipment;
using MementoMori.Ortega.Share.Enums;
using MementoMori.Ortega.Share.Master;
using MementoMori.Ortega.Share.Master.Data;
using MessagePack;
using Microsoft.Extensions.Options;

namespace MementoMori;

public class MementoMoriFuncs
{
    private Uri _apiHost;
    private Uri _apiAuth = new("https://prd1-auth.mememori-boi.com/api/");

    private readonly BehaviorSubject<RuntimeInfo> _runtimeInfoSubject = new(new RuntimeInfo());
    public IObservable<RuntimeInfo> RuntimeInfoSubject => _runtimeInfoSubject;

    private readonly BehaviorSubject<UserSyncData> _userSyncDataSubject = new(new UserSyncData());
    public IObservable<UserSyncData> UserSyncData => _userSyncDataSubject;

    private readonly RuntimeInfo _runtimeInfo = new();
    private readonly MeMoriHttpClientHandler _meMoriHttpClientHandler;
    private readonly HttpClient _httpClient;
    private readonly HttpClient _unityHttpClient;


    private readonly AuthOption _authOption;

    public MementoMoriFuncs(IOptions<AuthOption> authOption)
    {
        _authOption = authOption.Value;
        _meMoriHttpClientHandler = new MeMoriHttpClientHandler(_authOption.Headers);
        _meMoriHttpClientHandler.OrtegaAccessToken.Subscribe(token =>
        {
            _runtimeInfo.OrtegaAccessToken = token;
            _runtimeInfoSubject.OnNext(_runtimeInfo);
        });
        _meMoriHttpClientHandler.OrtegaMasterVersion.Subscribe(version =>
        {
            _runtimeInfo.OrtegaMasterVersion = version;
            _runtimeInfoSubject.OnNext(_runtimeInfo);
        });

        _httpClient = new HttpClient(_meMoriHttpClientHandler);
        _unityHttpClient = new HttpClient();
        _unityHttpClient.DefaultRequestHeaders.Add("User-Agent", new[] {"UnityPlayer/2021.3.10f1 (UnityWebRequest/1.0, libcurl/7.80.0-DEV)"});
        _unityHttpClient.DefaultRequestHeaders.Add("X-Unity-Version", new[] {"2021.3.10f1"});
    }

    public async Task AuthLogin()
    {
        var reqBody = new LoginRequest()
        {
            ClientKey = _authOption.ClientKey,
            DeviceToken = _authOption.DeviceToken,
            AppVersion = _authOption.AppVersion,
            OSVersion = _authOption.OSVersion,
            ModelName = _authOption.ModelName,
            AdverisementId = _authOption.AdverisementId,
            UserId = _authOption.UserId
        };
        var authLoginResp = await GetResponse<LoginRequest, LoginResponse>(reqBody);
        var playerDataInfo = authLoginResp.PlayerDataInfoList.FirstOrDefault();
        if (playerDataInfo == null) throw new Exception("playerDataInfo is null");

        // get server host
        await AuthGetServerHost(playerDataInfo.WorldId);

        // do login
        var loginPlayerResp = await UserLoginPlayer(playerDataInfo.PlayerId, playerDataInfo.Password);

        await DownloadMasterCatalog();

        var userSyncData = (await UserGetUserData()).UserSyncData;
        _userSyncDataSubject.OnNext(userSyncData);
    }

    private async Task AuthGetServerHost(long worldId)
    {
        var req = new GetServerHostRequest() {WorldId = worldId};
        var resp = await GetResponse<GetServerHostRequest, GetServerHostResponse>(req);
        _apiHost = new Uri(resp.ApiHost);
        _runtimeInfo.ApiHost = resp.ApiHost;
        _runtimeInfoSubject.OnNext(_runtimeInfo);
    }

    private async Task DownloadMasterCatalog()
    {
        var url = $"https://cdn-mememori.akamaized.net/master/prd1/version/{_runtimeInfo.OrtegaMasterVersion}/master-catalog";
        var bytes = await _unityHttpClient.GetByteArrayAsync(url);
        var masterBookCatalog = MessagePackSerializer.Deserialize<MasterBookCatalog>(bytes);
        Directory.CreateDirectory("./Master");
        foreach (var (name, info) in masterBookCatalog.MasterBookInfoMap)
        {
            var localPath = $"./Master/{name}";
            if (File.Exists(localPath))
            {
                var md5 = await CalcFileMd5(localPath);
                if (md5 == info.Hash) continue;
                File.Delete(localPath);
            }

            var mbUrl = $"https://cdn-mememori.akamaized.net/master/prd1/version/{_runtimeInfo.OrtegaMasterVersion}/{name}";
            var fileBytes = await _unityHttpClient.GetByteArrayAsync(mbUrl);
            await File.WriteAllBytesAsync(localPath, fileBytes);
        }

        Masters.ItemTable.Load();
        Masters.CharacterTable.Load();
        Masters.TextResourceTable.Load(LanguageType.zhTW);
        Masters.EquipmentTable.Load();
        Masters.SphereTable.Load();
        Masters.DungeonBattleRelicTable.Load();
        Masters.EquipmentSetMaterialTable.Load();
        Masters.TreasureChestTable.Load();
        Masters.LevelLinkTable.Load();
    }

    private async Task<string> CalcFileMd5(string path)
    {
        FileStream file = new FileStream(path, FileMode.Open);

        MD5 md5 = MD5.Create();
        byte[] retVal = await md5.ComputeHashAsync(file);
        file.Close();
        StringBuilder sb = new StringBuilder();
        foreach (byte t in retVal)
        {
            sb.Append(t.ToString("x2"));
        }

        return sb.ToString();
    }

    /// <summary>
    /// 自动精炼非D级别装备，然后将所有魔装继承到D级别装备
    /// </summary>
    public async Task AutoEquipmentInheritance()
    {
        while (true)
        {
            // 批量精炼
            var castManyResponse = await GetResponse<CastManyRequest, CastManyResponse>(new CastManyRequest()
            {
                RarityFlags = EquipmentRarityFlags.S | EquipmentRarityFlags.A | EquipmentRarityFlags.B | EquipmentRarityFlags.C
            });
            var usersyncData = await UserGetUserData();
            // 找到所有 等级为S、魔装、未装备 的装备
            var equipments = usersyncData.UserSyncData.UserEquipmentDtoInfos.Select(d => new
            {
                Equipment = d, EquipmentMB = Masters.EquipmentTable.GetById(d.EquipmentId)
            });

            var sEquipments = equipments.Where(d =>
                    d.Equipment.CharacterGuid == "" && // 未装备
                    d.Equipment.MatchlessSacredTreasureLv == 1 && // 魔装等级为 1
                    (d.EquipmentMB.RarityFlags & EquipmentRarityFlags.S) != 0 // 稀有度为 S
            );

            if (sEquipments.Count() == 0) break;

            foreach (var grouping in sEquipments.GroupBy(d => d.EquipmentMB.SlotType))
            {
                // 当前能够接受继承的 D 级别装备
                var currentTypeEquips = equipments.Where(d =>
                {
                    return (d.EquipmentMB.RarityFlags & EquipmentRarityFlags.D) != 0 && d.EquipmentMB.SlotType == grouping.Key && d.Equipment.MatchlessSacredTreasureLv == 0;
                });
                var processedDEquips = new List<UserItemDtoInfo>();

                // 还缺多少装备
                var needMoreCount = grouping.Count() - currentTypeEquips.Count();
                if (needMoreCount > 0)
                {
                    // 找到未解封的装备物品
                    var equipItems = usersyncData.UserSyncData.UserItemDtoInfo.Where(d =>
                    {
                        if (d.ItemType != ItemType.Equipment) return false;
                        var equipmentMb = Masters.EquipmentTable.GetById(d.ItemId);
                        if (equipmentMb.SlotType != grouping.Key) return false;
                        if ((equipmentMb.RarityFlags & EquipmentRarityFlags.D) == 0) return false;
                        return true;
                    });
                    foreach (var equipItem in equipItems)
                    {
                        if (needMoreCount <= 0) break;

                        var equipmentMb = Masters.EquipmentTable.GetById(equipItem.ItemId);
                        Console.WriteLine(equipmentMb.Memo);
                        // 找到可以装备的一个角色
                        var userCharacterDtoInfo = usersyncData.UserSyncData.UserCharacterDtoInfos.Where(d =>
                        {
                            var characterMb = Masters.CharacterTable.GetById(d.CharacterId);
                            if ((characterMb.JobFlags & equipmentMb.EquippedJobFlags) == 0) return false; // 装备职业

                            if (d.Level >= equipmentMb.EquipmentLv) return true; // 装备等级

                            if (usersyncData.UserSyncData.UserLevelLinkMemberDtoInfos.Exists(x => d.Guid == x.UserCharacterGuid)
                                && usersyncData.UserSyncData.UserLevelLinkDtoInfo.PartyLevel >= equipmentMb.EquipmentLv) // 角色在等级链接里面并且等级链接大于装备等级
                            {
                                return true;
                            }

                            return false;
                        }).First();


                        // 获取角色某个位置的准备

                        var replacedEquip = usersyncData.UserSyncData.UserEquipmentDtoInfos.Where(d =>
                        {
                            var byId = Masters.EquipmentTable.GetById(d.EquipmentId);
                            return d.CharacterGuid == userCharacterDtoInfo.Guid && byId.SlotType == equipmentMb.SlotType;
                        }).First();

                        // 替换装备
                        var changeEquipmentResponse = await GetResponse<ChangeEquipmentRequest, ChangeEquipmentResponse>(new ChangeEquipmentRequest()
                        {
                            UserCharacterGuid = userCharacterDtoInfo.Guid,
                            EquipmentChangeInfos = new List<EquipmentChangeInfo>()
                            {
                                new()
                                {
                                    EquipmentId = equipItem.ItemId,
                                    EquipmentSlotType = equipmentMb.SlotType,
                                    IsInherit = false
                                }
                            }
                        });

                        // 恢复装备
                        var changeEquipmentResponse1 = await GetResponse<ChangeEquipmentRequest, ChangeEquipmentResponse>(new ChangeEquipmentRequest()
                        {
                            UserCharacterGuid = userCharacterDtoInfo.Guid,
                            EquipmentChangeInfos = new List<EquipmentChangeInfo>()
                            {
                                new()
                                {
                                    EquipmentGuid = replacedEquip.Guid,
                                    EquipmentId = replacedEquip.EquipmentId,
                                    EquipmentSlotType = equipmentMb.SlotType,
                                    IsInherit = false
                                }
                            }
                        });

                        needMoreCount--;
                        processedDEquips.Add(equipItem);
                    }
                }


                // 继承            
                foreach (var x1 in grouping)
                {
                    // 同步数据
                    usersyncData = await UserGetUserData();

                    var userEquipmentDtoInfo = usersyncData.UserSyncData.UserEquipmentDtoInfos.Where(d =>
                    {
                        var equipmentMb = Masters.EquipmentTable.GetById(d.EquipmentId);
                        if (d.MatchlessSacredTreasureLv == 0 // 未被继承的装备 
                            && equipmentMb.SlotType == x1.EquipmentMB.SlotType // 同一个位置 
                            && (equipmentMb.RarityFlags & EquipmentRarityFlags.D) != 0 // 稀有度为 D
                           )
                        {
                            return true;
                        }

                        return false;
                    }).First();

                    var inheritanceEquipmentResponse = await GetResponse<InheritanceEquipmentRequest, InheritanceEquipmentResponse>(new InheritanceEquipmentRequest()
                    {
                        InheritanceEquipmentGuid = userEquipmentDtoInfo.Guid,
                        SourceEquipmentGuid = x1.Equipment.Guid
                    });
                    Console.WriteLine($"继承完成 {x1.EquipmentMB.Memo}=>{userEquipmentDtoInfo.Guid}");
                }
            }
        }
    }


    public async Task<GetDataUriResponse> AuthGetDataUri(string countryCode, long userId)
    {
        var req = new GetDataUriRequest() {CountryCode = countryCode, UserId = userId};
        return await GetResponse<GetDataUriRequest, GetDataUriResponse>(req);
    }

    private async Task<LoginPlayerResponse> UserLoginPlayer(long playerId, string password)
    {
        var req = new LoginPlayerRequest {PlayerId = playerId, Password = password};
        return await GetResponse<LoginPlayerRequest, LoginPlayerResponse>(req);
    }

    public async Task<GetUserDataResponse> UserGetUserData()
    {
        var req = new GetUserDataRequest { };
        return await GetResponse<GetUserDataRequest, GetUserDataResponse>(req);
    }

    public async Task<GetMonthlyLoginBonusInfoResponse> LoginBonusGetMonthlyLoginBonusInfo()
    {
        var req = new GetMonthlyLoginBonusInfoRequest();
        return await GetResponse<GetMonthlyLoginBonusInfoRequest, GetMonthlyLoginBonusInfoResponse>(req);
    }

    /// <summary>
    /// 获取每日登陆奖励
    /// </summary>
    /// <param name="receiveDay"></param>
    /// <returns></returns>
    public async Task<ReceiveDailyLoginBonusResponse> LoginBonusReceiveDailyLoginBonus(int receiveDay)
    {
        var req = new ReceiveDailyLoginBonusRequest() {ReceiveDay = receiveDay};
        return await GetResponse<ReceiveDailyLoginBonusRequest, ReceiveDailyLoginBonusResponse>(req);
    }

    /// <summary>
    /// 获取VIP每日奖励
    /// </summary>
    /// <returns></returns>
    public async Task<GetDailyGiftResponse> VipGetDailyGift()
    {
        var req = new GetDailyGiftRequest();
        return await GetResponse<GetDailyGiftRequest, GetDailyGiftResponse>(req);
    }

    /// <summary>
    /// 一键发送、接收友情点
    /// </summary>
    /// <returns></returns>
    public async Task<BulkTransferFriendPointResponse> FriendBulkTransferFriendPoint()
    {
        var req = new BulkTransferFriendPointRequest();
        return await GetResponse<BulkTransferFriendPointRequest, BulkTransferFriendPointResponse>(req);
    }

    /// <summary>
    /// 获取自动战斗的奖励
    /// </summary>
    /// <returns></returns>
    public async Task<RewardAutoBattleResponse> BattleRewardAutoBattle()
    {
        var req = new RewardAutoBattleRequest();
        return await GetResponse<RewardAutoBattleRequest, RewardAutoBattleResponse>(req);
    }

    /// <summary>
    /// 战斗扫荡
    /// </summary>
    /// <returns></returns>
    public async Task<BossQuickResponse> BattleBossQuick(int questId)
    {
        var req = new BossQuickRequest() {QuestId = questId};
        return await GetResponse<BossQuickRequest, BossQuickResponse>(req);
    }

    /// <summary>
    /// 高速战斗
    /// </summary>
    /// <returns></returns>
    public async Task<QuickResponse> BattleQuick(QuestQuickExecuteType questQuickExecuteType, int quickCount)
    {
        var req = new QuickRequest() {QuestQuickExecuteType = questQuickExecuteType, QuickCount = quickCount};
        return await GetResponse<QuickRequest, QuickResponse>(req);
    }

    public async Task<ReceiveItemResponse> PresentReceiveItem()
    {
        var req = new ReceiveItemRequest() {LanguageType = LanguageType.zhTW};
        return await GetResponse<ReceiveItemRequest, ReceiveItemResponse>(req);
    }

    public async Task<TResp> GetResponse<TReq, TResp>(TReq req)
        where TReq : ApiRequestBase
        where TResp : ApiResponseBase
    {
        var authAttr = typeof(TReq).GetCustomAttribute<OrtegaAuthAttribute>();
        var apiAttr = typeof(TReq).GetCustomAttribute<OrtegaApiAttribute>();
        Uri uri;
        if (authAttr != null)
        {
            uri = new Uri(_apiAuth, authAttr.Uri);
        }
        else if (apiAttr != null)
        {
            uri = new Uri(_apiHost, apiAttr.Uri);
        }
        else
        {
            throw new NotSupportedException();
        }

        // var reqMap = JsonConvert.DeserializeObject<Dictionary<object, object>>(JsonConvert.SerializeObject(req));
        var bytes = MessagePackSerializer.Serialize(req);
        var respMsg = await _httpClient.PostAsync(uri,
            new ByteArrayContent(bytes) {Headers = {{"content-type", "application/json"}}});
        var respBytes = await respMsg.Content.ReadAsByteArrayAsync();
        return MessagePackSerializer.Deserialize<TResp>(respBytes);
        // return JsonConvert.DeserializeObject<TResp>(JsonConvert.SerializeObject(tmp));
    }
}