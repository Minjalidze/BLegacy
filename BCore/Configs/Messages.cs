using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace BCore.Configs;

public static class Messages
{
    public static Rus RuMessages;
    private static LoadMessages _messagesObject;

    public static void Instantiate()
    {
        _messagesObject = new GameObject().AddComponent<LoadMessages>();
    }

    public static void LoadData()
    {
        var file = File.ReadAllText(@"serverdata\cfg\BCore\messages.rus.cfg");
        RuMessages = JsonConvert.DeserializeObject<Rus>(file);
        Debug.Log("[BCore]: Messages Config Loaded!");
    }
    public class Rus
    {
        public Dictionary<string, string> BodyPart = new()
        {
            { "undefined", "тело" },
            { "head", "голову" },
            { "face", "лицо" },
            { "left eye", "левый глаз" },
            { "right eye", "правый глаз" },
            { "nose", "нос" },
            { "mouth", "рот" },
            { "throat", "горло" },
            { "chest", "грудь" },
            { "torso", "торс" },
            { "body", "тело" },
            { "gut", "живот" },
            { "hip", "бедро" },
            { "left lung", "левое лёгкое" },
            { "right lung", "правое лёгкое" },
            { "left shoulder", "левое плечо" },
            { "right shoulder", "правое плечо" },
            { "left hand", "левую руку" },
            { "right hand", "правую руку" },
            { "left bicep", "левую руку" },
            { "right bicep", "правую руку" },
            { "left wrist", "левую руку" },
            { "right wrist", "правую руку" },
            { "left foot", "левую ногу" },
            { "right foot", "правую ногу" },
            { "left calve", "левую ногу" },
            { "right calve", "правую ногу" }
        };

        public Dictionary<string, string> Names = new()
        {
            { "Chicken", "Цыпленка" },
            { "Rabbit", "Кролика" },
            { "Boar", "Кабана" },
            { "Stag", "Оленя" },
            { "Wolf", "Волка" },
            { "Bear", "Медведя" },
            { "Mutant Wolf", "Волка Мутанта" },
            { "Mutant Bear", "Медведя Мутанта" }
        };

        public Message RuMessage;

        public class Message
        {
            public string AirdropIncoming = "Прибывает снабжение!";
            public string ClanExperienceCrafted = "Ваш клан получил %EXPERIENCE% опыта при создании %ITEM_NAME%.";
            public string ClanExperienceGather = "Ваш клан получил %EXPERIENCE% опыта при сборе %RESOURCE_NAME%.";
            public string ClanExperienceMurder = "Ваш клан получил %EXPERIENCE% опыта от убийства %VICTIM%.";

            public string CommandCantUseHere = "Вы не можете использовать эту команду здесь.";
            public string CommandClanAbbrForbiddenSyntax = "Запрещенные символы в аббревиатуре";
            public string CommandClanAbbrNoAvailable = "Смена аббревиатуры недоступна на данный момент";
            public string CommandClanAbbrNoValue = "Вы должны ввести новое имя аббревиатуры для смены";
            public string CommandClanAbbrSuccess = "Аббревиатура клана изменена на \"%CLAN.ABBR%\".";

            public string CommandClanAbbrTooLongLength = "Имя аббревиатуры очень длинное. Максимальная длина 8 символа";

            public string CommandClanAbbrTooShortLength =
                "Имя аббревиатуры очень короткое. Минимальная длина 2 символа";

            public string CommandClanAlreadyInClan = "Вы уже в клане";

            public string CommandClanCreateForbiddenSyntax = "Запрещенные символы в названии клана";
            public string CommandClanCreateNameAlredyInUse = "Клан с таким названием уже существует";
            public string CommandClanCreateNotEnoughCurrency = "У вас нет %CREATE_COST% для создания клана.";
            public string CommandClanCreateReqEnterName = "Вы должны ввести имя клана для создания";
            public string CommandClanCreateSuccess = "Вы успешно создали клан с названием \"%CLAN.NAME%\".";

            public string CommandClanCreateTooLongLength =
                "Название клана очень длинное. Максимальная длина 32 символов";

            public string CommandClanCreateTooShortLength =
                "Название клана очень короткое. Минимальная длина 3 символов";

            public string CommandClanDepositNoAmount = "Вы должны ввести сумму для вклада на счет клана";
            public string CommandClanDepositNoEnoughAmount = "У вас нет %DEPOSIT_AMOUNT% для вклада на счет клана";
            public string CommandClanDepositSuccess = "Вы положили %DEPOSIT_AMOUNT% на счет клана";
            public string CommandClanDetailsDisabled = "Отображение получения опыта для клана сейчас выключено.";
            public string CommandClanDetailsEnabled = "Отображение получения опыта для клана сейчас включено.";
            public string CommandClanDetailsSetOff = "Отображение получения опыта для клана выключено.";
            public string CommandClanDetailsSetOn = "Отображение получения опыта для клана включено.";
            public string CommandClanDisbanded = "Ваш клан был расформирован.";
            public string CommandClanDismissIsLeader = "Вы не можете исключить лидера клана";
            public string CommandClanDismissNotInClan = "Игрок %USERNAME% не состоит в клане";
            public string CommandClanDismissNoValue = "Введите имя игрока для исключения из клана";
            public string CommandClanDismissSuccess = "\"%USERNAME%\" исключил вас из клана \"%CLAN.NAME%\"";
            public string CommandClanDismissToDismiss = "You dismiss \"%USERNAME%\" from a clan.";
            public string CommandClanFriendlyFireDisabled = "\"Огонь по своим\" сейчас выключен для клана.";

            public string CommandClanFriendlyFireEnabled = "\"Огонь по своим\" сейчас включен для клана.";

            public string CommandClanFriendlyFireHelp =
                "Используйте <Yes|Y|On|1 / No|N|Off|0> для изменения режима \"Огонь по своим\".";

            public string CommandClanFriendlyFireNoAvailable = "Вы не можете изменить \"Огонь по своим\" для клана.";
            public string CommandClanFriendlyFireToDisable = "\"Огонь по своим\" теперь выключен для клана.";
            public string CommandClanFriendlyFireToEnable = "\"Огонь по своим\" теперь включен для клана.";
            public string CommandClanHostileAccepted = "Теперь ваш клан в войне с %CLAN.NAME% кланом.";
            public string CommandClanHostileCannotWar = "Вы не можете объявить войну клану %CLAN_NAME%";

            public string[] CommandClanHostileDeclare =
            {
                "Ваш лидер клана отправил объявление ВОЙНЫ с кланом %CLAN.NAME%.",
                "И ваш клан ожидает ответа от лидера враждебного клана."
            };

            public string CommandClanHostileDeclared = "Теперь ваш клан в войне с %CLAN.NAME% кланом.";

            public string[] CommandClanHostileDeclinedFrom =
            {
                "Лидер клана %CLAN.NAME% отклонил войну с вашим кланом.",
                "И ваш клан получил опыт и валюту от враждебного клана.", ""
            };

            public string[] CommandClanHostileDeclinedTo =
            {
                "Ваш лидер клана отказался от войны с %CLAN.NAME% кланом. ", "И ваш клан потерял часть опыта и валюты."
            };

            public string CommandClanHostileInWar = "Ваш клан уже в состоянии войны с %CLAN_NAME%";
            public string CommandClanHostileNoAvailable = "Вы не можете объявлять войну кланам.";
            public string CommandClanHostileNoClan = "Клан с названием \"%CLAN_NAME%\" не существует";
            public string CommandClanHostileNoLeader = "Клан \"%CLAN_NAME%\" не имеет лидера";
            public string CommandClanHostileNoValue = "Введите имя клана что бы объявить войну";

            public string CommandClanHostileQuery =
                "Клан %CLAN.NAME% объявляет войну вашему клану. Хотите принять (Y/N)?";

            public string CommandClanHostileQueryBusy =
                "Вы не можете объявить войну клану %CLAN_NAME% на данный момент";

            public string[] CommandClanHostileQueryComment =
            {
                "Если ваш клан отказывается, то ваш клан потеряет некоторое количество опыта и валюты.",
                "После принятия войны, враждебные кланы имеют дополнительные бонусы от убийств друг друга."
            };

            public string CommandClanHouseNoAvailable = "Вы не можете установить дом клана на данный момент";
            public string CommandClanHouseOnlyLeaderHouse = "Вы ДОЛЖНЫ в ВАШЕМ ДОМЕ для этого";

            public string[] CommandClanHouseSuccess =
            {
                "Теперь дом клана находится на %CLAN.LOCATION%, все члены клана",
                "теперь могут перемещаться в клановый дом в любое время."
            };

            public string[] CommandClanInfo =
            {
                "Название: %CLAN.NAME%",
                "Аббревиатура: %CLAN.ABBR%",
                "Лидер: %CLAN.LEADER.USERNAME%",
                "Члены клана: %CLAN.MEMBERS.COUNT% / %CLAN.MEMBERS.MAX%",
                "Уровень: %CLAN.LEVEL%",
                "Опыт клана: %CLAN.EXPERIENCE%",
                "Баланс валюты: %CLAN.BALANCE%",
                "След. уровень: %CLAN.NEXT_LEVEL%",
                "След. уровень (требуется опыта): %CLAN.NEXT_EXPERIENCE%",
                "След. уровень (требуется валюты): %CLAN.NEXT_CURRENCY%",
                "Сообщения дня: %CLAN.MOTD%",
                "Налог с убийства: %CLAN.TAX%",
                "Локация кланового дома: %CLAN.LOCATION%",
                "(Бонус) Скорость изготовления: +%CLAN.BONUS.CRAFTINGSPEED%",
                "(Бонус) Количество дерева при сборе: +%CLAN.BONUS.GATHERINGWOOD%",
                "(Бонус) Количество камней при сборе: +%CLAN.BONUS.GATHERINGROCK%",
                "(Бонус) Количество ресурсов с животных: +%CLAN.BONUS.GATHERINGANIMAL%",
                "(Бонус) Увеличенная защита членов клана: +%CLAN.BONUS.MEMBERS_DEFENSE%",
                "(Бонус) Увеличенный урон членов клана: +%CLAN.BONUS.MEMBERS_DAMAGE%",
                "(Бонус) Увеличение платы за убийства: +%CLAN.BONUS.MEMBERS_PAYMURDER%"
            };

            public string[] CommandClanInfoAdmin = { "Список членов клана:", "%CLAN.MEMBERS_LIST%" };
            public string CommandClanInviteAlreadyInClan = "Игрок \"%USERNAME%\" уже в клане";
            public string CommandClanInviteAlreadyInvite = "Игрок \"%USERNAME%\" уже приглашен";
            public string CommandClanInviteInviteToJoin = "Вы приглашаете %USERNAME% присоединиться к клану.";
            public string CommandClanInviteJoinAnswerN = "%USERNAME% отказался присоединиться к клану.";
            public string CommandClanInviteJoinAnswerY = "%USERNAME% присоединиться к клану.";
            public string CommandClanInviteJoinQuery = "Вы хотите присоединиться к клану \"%CLAN.NAME%\" (Y/N)?";
            public string CommandClanInviteNoSlots = "В клане нет свободных слотов для приглашения";

            public string CommandClanInviteNoValue = "Введите имя игрока для приглашения в клан";
            public string CommandClanLeaveDisbandBefore = "Вы не можете выйти, распустите клан в начале.";
            public string CommandClanLeaveMemberLeaved = "\"%USERNAME%\" вышел из клан.";
            public string CommandClanLeaveSuccess = "Вы вышли из клана";

            public string CommandClanLevelUpNotEnoughCurrency =
                "Не хватает валюты на балансе клана для повышения уровня";

            public string CommandClanLevelUpNotEnoughExperience = "Не хватает опыта клана для повышения уровня";
            public string CommandClanLevelUpReachedMax = "Ваш клан достиг максимального уровня";

            public string[] CommandClanLevelUpSuccess =
            {
                "Поздравляем! Уровень клана увеличен до %CLAN.LEVEL%.",
                "Слоты членов клана увеличилось до %CLAN.MEMBERS.MAX%",
                "Бонус \"Скорость изготовления\" теперь +%CLAN.BONUS.CRAFTINGSPEED%",
                "Бонус \"Количество дерева при сборе\" теперь +%CLAN.BONUS.GATHERINGWOOD%",
                "Бонус \"Количество камней при сборе\" теперь +%CLAN.BONUS.GATHERINGROCK%",
                "Бонус \"Количество ресурсов с животных\" теперь +%CLAN.BONUS.GATHERINGANIMAL%",
                "Бонус \"Увеличенная защита членов клана\" теперь +%CLAN.BONUS.MEMBERS_DEFENSE%",
                "Бонус \"Увеличенный урон членов клана\" теперь +%CLAN.BONUS.MEMBERS_DAMAGE%",
                "Бонус \"Увеличение платы за убийства\" теперь +%CLAN.BONUS.MEMBERS_PAYMURDER%"
            };

            public string[] CommandClanMembers =
                { "Всего членов клана: %CLAN.MEMBERS.COUNT% / %CLAN.MEMBERS.MAX%", "%CLAN.MEMBERS_LIST%" };

            public string CommandClanMotdNoAvailable = "Вы не можете изменить \"Сообщение дня\" для клана";
            public string CommandClanMotdNoValue = "Введите новый текст для \"Сообщения дня\" для клана";
            public string CommandClanMotdSuccess = "Сообщение дня: %CLAN.MOTD%";
            public string CommandClanNoPermissions = "У вас нет прав для этого.";
            public string CommandClanNotAvailable = "Clan feature is not available";
            public string CommandClanNotInClan = "Вы не в клане";

            public string[] CommandClanOnline =
                { "Сейчас в онлайне: %CLAN.ONLINE% / %CLAN.MEMBERS.COUNT%", "%CLAN.ONLINE_LIST%" };

            public string CommandClanPlayerJoined = "Вы присоединились к клану %CLAN.NAME%.";
            public string CommandClanPlayerLeaved = "Вы покинули клан %CLAN.NAME%.";
            public string CommandClanPrivileges = "Ваши привелегии в клане: %MEMBER_PRIV%";
            public string CommandClanPrivilegesMember = "Привелегии %USERNAME%: %MEMBER_PRIV%";
            public string CommandClanPrivilegesNoCanChange = "Вы не можете изменить привелегии лидера клана";
            public string CommandClanPrivilegesNotInClan = "%USERNAME% не состоит в клане.";

            public string CommandClansInfo =
                "%CLAN.NAME% (Уровень: %CLAN.LEVEL%, Лидер: %CLAN.LEADER.USERNAME%, Членов: %CLAN.MEMBERS.COUNT%)";

            public string[] CommandClansList = { "Список кланов сервера:", "%CLANS.LIST%", "Всего: %CLANS.COUNT%" };
            public string CommandClanTaxNoAvailable = "Вы не можете установить налог для клана";
            public string CommandClanTaxNoNumeric = "Неверное числовое значение для налога.";
            public string CommandClanTaxNoValue = "Необходимо ввести новый налог для изменения";
            public string CommandClanTaxSuccess = "Налог в клане теперь состовляет %CLAN.TAX% от платы за убийства.";
            public string CommandClanTaxVeryHigh = "Этот налог очень высок.";
            public string CommandClanTransferNotInClan = "Игрок \"%USERNAME%\" не в вашем клане";
            public string CommandClanTransferNoValue = "Вы должны ввести имя игрока для передачи клана";
            public string CommandClanTransferQuery = "Вы предложили \"%USERNAME%\" стать лидером клана.";
            public string CommandClanTransferQueryAnswerN = "%%USERNAME% отказался от лидерства клана.";
            public string CommandClanTransferQueryAnswerY = "%USERNAME% теперь это лидер клана.";
            public string CommandClanTransferQueryMember = "Вы хотите принять лидерво клана (ACCEPT/N)?";

            public string CommandClanTransferSuccess =
                "Руководство клана изменилось, теперь \"%USERNAME%\" лидер клана.";

            public string CommandClanWarpCountdown = "Вы должны подождать %TIME% чтобы переместиться снова";
            public string CommandClanWarpInterrupt = "Перемещение в клановый дом прервано";

            public string CommandClanWarpNoAvailable = "Перемещение в клановый дом не доступено на данный момент";
            public string CommandClanWarpNoClanHouse = "Ваш клан не имеет кланового дома для телепортации.";
            public string CommandClanWarpNotHere = "Вы не можете переместиться в клановый дом от суда";
            public string CommandClanWarpPrepare = "Вы будете перемещены в клановый дом через %SECONDS% секунд";
            public string CommandClanWarpTimewait = "Вы должны подождать %SECONDS% секунд для перемещения";
            public string CommandClanWarpWarped = "Вы переместились в клановый дом";
            public string CommandClanWithdrawNoAmount = "Вы должны ввести сумму которую вы хотите забрать";
            public string CommandClanWithdrawNoEnoughAmount = "На счету клана нет %WITHDRAW_AMOUNT%";
            public string CommandClanWithdrawSuccess = "Вы забрали %WITHDRAW_AMOUNT% со счета клана.";
            public string CommandClients = "Список игроков был отправлен в консоль.";
            public string CommandDestroyDisabled = "Режим разрушения собственности был выключен";
            public string CommandDestroyEnabled = "Режим разрушения собственности ВКЛЮЧЕН";
            public string CommandDestroyResourceReceived = "Вы получили %ITEMNAME% от разрушения %OBJECT%.";
            public string CommandHomeCountdown = "Необходимо подождать %TIME% для телепортации";
            public string CommandHomeInterrupt = "Телепортация в ваш дом прервана";
            public string[] CommandHomeList = { "Ваши дома (всего: %HOME.COUNT%)", "Дом #%HOME.NUM%: %HOME.POSITION%" };
            public string CommandHomeNoCamp = "У Вас нету дома.";
            public string CommandHomeNoEnoughCurrency = "Не хватает %PRICE% для возвращения в лагерь";

            public string CommandHomeNotHere = "Вы не можете вернуться домой от суда";
            public string CommandHomeReturn = "Вы вернулись к себе домой.";
            public string CommandHomeStart = "Вы будете телепортированы через %TIME% секунд";
            public string CommandHomeWait = "Вам нужно подождать %TIME% секунд перед телепортацией";
            public string CommandItemNoFound = "Item with name \"%USERNAME%\" not found.";

            public string CommandKitCountdown =
                "Необходимо подождать %TIME% прежде чем получить набор \"%KITNAME%\" снова.";

            public string CommandKitNameNoFound = "Набор \"%KITNAME%\" не существует";
            public string CommandKitNotAvailabled = "Набор \"%KITNAME%\" не доступен для Вас";

            public string CommandKitReceived = "Вы получили набор \"%KITNAME%\"";
            public string CommandKitReceivedOnce = "Вы не можете больше получить набор \"%KITNAME%\"";
            public string CommandKitsAvailabled = "Доступные наборы: %KITS%";
            public string CommandKitsNotAvailable = "Наборы не доступны для Вас";
            public string CommandMutePlayerMuted = "%USERNAME% установил молчание для %TARGET% на %TIME%.";
            public string CommandMutePlayerUnuted = "%USERNAME% снял молчание с %TARGET%.";
            public string CommandOnline = "Игроков онлайн: %ONLINE% /%MAXPLAYERS%";
            public string CommandPasswordChanged = "Ваш пароль был успешно изменён на новый!";

            public string CommandPasswordDisplay = "Ваш пароль: %PASSWORD%";

            public string CommandPasswordIsEmpty =
                "Пароль не установлен. Используйте: \"/password <new_password>\" для установки или изменения пароля.";

            public string CommandPasswordNewTooLong = "Новый пароль слишком длинный.";
            public string CommandPasswordNewTooShort = "Новый пароль слишком короткий.";
            public string CommandPlayerNoFound = "Игрок \"%USERNAME%\" не найден.";
            public string CommandPlayers = "Сейчас онлайн %ONLINE% :";
            public string CommandPMFrom = "[PM от]";
            public string CommandPMSelf = "Вы не можете отправлять сообщения сами себе.";
            public string CommandPMTo = "[PM для]";
            public string CommandPvPCountdown = "Данная функция не будет доступна для вас ещё %TIME% минут.";
            public string CommandPvPDisabled = "PvP режим был ВЫКЛЮЧЕН на %TIME% минут.";
            public string CommandPvPEnabled = "PvP режим был ВКЛЮЧЕН.";
            public string CommandPvPNoticeDisabled = "PvP режим был ВЫКЛЮЧЕН для %USERNAME%";
            public string CommandPvPNoticeEnabled = "PvP режим был ВКЛЮЧЕН для %USERNAME%";
            public string CommandPvPNoticeStart = "PvP режим будет отключен для %USERNAME% через %SECONDS% секунд(ы).";
            public string CommandPvPStart = "PvP режим будет отключен через %SECONDS% секунд(ы).";
            public string CommandPvPWait = "Вам необходимо подождать %SECONDS% секунд для отключения PvP режима.";
            public string CommandReplyNobody = "Вам никто не писал";
            public string CommandShareAlready = "Игрок %USERNAME% уже имеет доступ к вашему имуществу!";
            public string CommandShareClient = "%USERNAME% дал вам доступ к своему имуществу";
            public string CommandShareOwner = "Вы дали игроку %USERNAME% доступ к своему имуществу";
            public string CommandShareSelf = "Ваша собственность уже ваша!";
            public string CommandTeleportAlready = "Вам нужно подождать %TIME% секунд предыдущей телепортации";
            public string CommandTeleportConfirmed = "Вы подтвердили телепортацию %USERNAME% к себе.";
            public string CommandTeleportCountdown = "Вам нужно подождать %TIME% чтобы запросить телепортацию";
            public string CommandTeleportInterrupt = "Текущая телепортация была прервана";
            public string CommandTeleportIsConfirm = "%USERNAME% подтвердил вашу телепортацию к себе.";
            public string CommandTeleportNoEnoughCurrency = "Не хватает %PRICE% для телепортации";
            public string CommandTeleportNotCan = "%USERNAME% не может телепортироваться";
            public string CommandTeleportNoTeleport = "Вы не можете телепортироваться к %USERNAME%";
            public string CommandTeleportNotHere = "Вы не можете использовать телепорт здесь";
            public string CommandTeleportOnSelf = "Вы не можете телепортироваться к себе";
            public string CommandTeleportPrepare = "Телепортация произойдет через %TIME% секунд";
            public string CommandTeleportQuery = "%USERNAME% просит телепорт к вам, вы хотите \"ПРИНЯТЬ\"?";

            public string CommandTeleportQueryHelp =
                "Введите \"ACCEPT\" или \"CONFIRM\" в игровом чате, чтобы подтвердить запрос.";

            public string CommandTeleportRefuse = "Вы отказались телепортировать %USERNAME%.";
            public string CommandTeleportRefused = "%USERNAME% отказался телепортировать Вас.";
            public string CommandTeleportTeleportedPlayer = "%USERNAME% телепортирован к Вам";
            public string CommandTeleportTeleportOnPlayer = "Вы телепортированы к %USERNAME%";
            public string CommandTeleportTimewait = "Вам нужно подождать %TIME% секунд перед телепортацией";
            public string CommandTeleportToSelf = "%USERNAME% не может телепортироваться на себя";
            public string CommandTransferAlreadyOwned = "Этот %OBJECT% уже принадлежит %USERNAME%";
            public string CommandTransferAway = "Это слишком далеко";
            public string CommandTransferBuilding = "Строение";
            public string CommandTransferForbidden = "Вы не можете передать %OBJECT%";
            public string CommandTransferNotYourOwned = "Этот %OBJECT% не ваш";
            public string CommandTransferSeeNothing = "Вы не видите ничего для передачи";
            public string CommandTransferSelf = "Вы не можете передать себе же";
            public string CommandTransferThereNothing = "Там нет ничего для передачи";
            public string CommandUnshareAlready = "У игрока %USERNAME% не было доступа к вашему имуществу!";
            public string CommandUnshareClean = "Вы поменяли все замки.";
            public string CommandUnshareClient = "У вас больше нет доступа к имуществу %USERNAME%";
            public string CommandUnshareNotAnyone = "Вы еще никому не давали доступ к вашему имуществу.";
            public string CommandUnshareOwner = "Вы закрыли доступ к своему имуществу для %USERNAME%";
            public string CommandUnshareSelf = "Вы больше не доверяете себе?!";
            public string CommandWho = "Этот %OBJECT.NAME% принадлежит %OBJECT.OWNERNAME%. %OBJECT.CONDITION%";
            public string CommandWhoCannotOwned = "Это не может иметь владельца";
            public string CommandWhoCondition = "Состояние %OBJECT.HEALTH% из %OBJECT.MAXHEALTH%";
            public string CommandWhoNotOwned = "Этот %OBJECT.NAME% не имеет владельца. %OBJECT.CONDITION%";
            public string CommandWhoNotSeeAnything = "Вы ничего не видите";

            public string ConnectNotProtected = "Вы были отключены от сервера. Причина: Ваш клиент без защиты.";

            public string ConnectUsernameAlreadyInUse =
                "Вы были отключены от сервера. Причина: Данный ник уже используется.";

            public string ConnectUsernameBadNameForSteamID =
                "Вы были отключены от сервера. Причина: Неверный ник для данного steam ID.";

            public string ConnectUsernameForbidden = "Вы были отключены от сервера. Причина: Недопустимый ник.";

            public string ConnectUsernameForbiddenLength =
                "Вы были отключены от сервера. Причина: Недопустимая длина вашего ника.";

            public string ConnectUsernameForbiddenSyntax =
                "Вы были отключены от сервера. Причина: В Вашем нике присутствуют запрещенные символы.";

            public string ConnectUsernameNotWhitelist =
                "Вы были отключены от сервера. Причина: Вы не состоите в WhiteList.";

            public string CycleInstantCraftDisabled = "Мгновенное создание предметов выключено.";
            public string CycleInstantCraftEnabled = "Мгновенное создание предметов включено.";
            public string CyclePvPDisabled = "Внимание: PvP режим включен.";
            public string CyclePvPEnabled = "Внимание: PvP режим отключен.";
            public string EconomyBalance = "Ваш баланс: %BALANCE%";

            public string EconomyNotAvailable = "Магазин выключен.";
            public string EconomyPlayerDeathFee = "Вы потеряли %DEATHFEE% при смерти от %KILLER%.";
            public string EconomyPlayerDeathPay = "Вы получили %DEATHPAY% от убийства %VICTIM%.";
            public string EconomyPlayerSuicideFee = "Вы потеряли %DEATHFEE% от самоубийства.";
            public string EconomySendHimself = "Вы не можете отпрвить самому себе";
            public string EconomySendNoAmount = "Введите количество валюты для отправки";
            public string EconomySendNoHaveAmount = "У вас нет %SENTAMOUNT% для отправки";
            public string EconomySendSentFromPlayer = "\"%USERNAME%\" прислал вам %SENTAMOUNT%";
            public string EconomySendSentToPlayer = "Вы отправили %SENTAMOUNT% для \"%USERNAME%\"";
            public string EconomyShopBuyItemNotAvailable = "Предмет \"%ITEMNAME%\" недоступен для покупки.";
            public string EconomyShopBuyItemPurchased = "Вы приобрели %ITEMNAME% за %TOTALPRICE%.";

            public string EconomyShopBuyNotAvailable = "Функция покупки не доступна.";
            public string EconomyShopBuyNotEnoughBalance = "У вас нету %TOTALPRICE% для покупки \"%ITEMNAME%\".";

            public string EconomyShopHelp =
                "Используйте \"/shop <название категории|номер>\" для показа вещей в определенной категории.";

            public string EconomyShopListGroup = "%INDEX%. %GROUPNAME% (Категория)";

            public string EconomyShopListItem =
                "%INDEX%. %ITEMNAME% (Цена: %SELLPRICE% за %QUANTITY% шт. Продажа: %BUYPRICE%)";

            public string EconomyShopNotAvailable = "Функция магазина не доступна.";
            public string EconomyShopNoTradeZone = "Вы должны находиться в торговой зоне для покупок.";
            public string EconomyShopSellAllSold = "Вы продали %TOTALAMOUNT% предметов за %TOTALPRICE%";
            public string EconomyShopSellItemNotAvailable = "Предмет \"%ITEMNAME%\" не продается.";
            public string EconomyShopSellItemSold = "Вы продали %ITEMNAME% за %TOTALPRICE%.";
            public string EconomyShopSellNoNothing = "У вас нет предметов для продажи ";
            public string EconomyShopSellNotAvailable = "Функция продажи не доступна.";
            public string EconomyShopSellNotEnoughItem = "В вашем инвентаре нету \"%ITEMNAME%\" для продажи.";
            public string EconomySleeperDeathPay = "Вы получили %DEATHPAY% от убийства тела %VICTIM%.";

            public string[] NoticeConnectedAdminMessage =
            {
                "Добро пожаловать администратор %USERNAME%!",
                "У вас полный доступ к серверу включая консоль.",
                "Текущий онлайн: %ONLINE% /%MAXPLAYERS%"
            };

            public string[] NoticeConnectedPlayerMessage =
            {
                "Добро пожаловать %USERNAME% на %SERVERNAME%\" игры RUST!",
                "Этот сервер под BAnti-Cheat системой. Помните, любые чит программы",
                "будут обнаружены и за их использование вы будете забанены, Приятной игры!",
                "Текущий онлайн: %ONLINE% из %MAXPLAYERS%"
            };

            public string PlayerChatQueryNotAnswer = "%USERNAME% ещё не ответил на предыдущий запрос";
            public string PlayerCraftingBlueprintNotAvailable = "Вам не доступно изготовление этого предмета";
            public string PlayerCraftingBlueprintNotKnown = "Вам не известен этот чертеж";
            public string PlayerCraftingNotAvailable = "Вы не можете создавать предметы здесь.";
            public string[] PlayerDeathBleeding = { "Игрок %VICTIM% умер от потери крови." };
            public string[] PlayerDeathCold = { "Игрок %VICTIM% умер от холода." };
            public string[] PlayerDeathHunger = { "Игрок %VICTIM% умер от голода." };

            public string[] PlayerDeathMurder =
                { "%VICTIM% был убит в %BODYPART% получив %DAMAGE% урона от %KILLER% (%WEAPON%) с %DISTANCE%м." };

            public string[] PlayerDeathNpc = { "%VICTIM% был убит %KILLER%." };
            public string[] PlayerDeathPoison = { "Игрок %VICTIM% умер от отравления." };
            public string[] PlayerDeathRadiation = { "Игрок %VICTIM% умер от радиации." };

            public string[] PlayerDeathSuicide = { "Игрок %VICTIM% покончил жизнь самоубийством." };
            public string PlayerInventoryIsFull = "В вашем инвентаре нет свободного места";
            public string PlayerJoin = "Игрок %USERNAME% подключился к серверу.";
            public string PlayerLeave = "Игрок %USERNAME% покинул сервер.";
            public string PlayerMuted = "Вы получили молчание в чате на %TIME%.";
            public string PlayerNoDamageClanMember = "Вы не можете навредить члену клана";
            public string PlayerNoDamageClanMemberOwned = "Вы не можете навредить собственности члена клана";
            public string PlayerNoDamageWithoutPvP = "Вы не можете навредить %VICTIM% без PvP";
            public string PlayerNoDamageWithoutPvPOwned = "Вы не можете навредить собственности %VICTIM% без PvP";
            public string PlayerNoDamageZoneWithoutPvP = "Вы не можете навредить %VICTIM% в зоне без PvP";

            public string PlayerNoDamageZoneWithoutPvPOwned =
                "Вы не можете навредить собственности %VICTIM% в зоне без PvP";

            public string PlayerNoDamageZoneWithSafety = "Вы не можете навредить %VICTIM% в безопасной зоне";

            public string PlayerNoDamageZoneWithSafetyOwned =
                "Вы не можете навредить собственности %VICTIM% в безопасной зоне";

            public string[] PlayerNoticeMurder = { "Вы убили %VICTIM%" };
            public string[] PlayerNoticeNPC = { "Вы убили %VICTIM%" };
            public string PlayerOwnershipContainerProtected = "Этот контейнер пренадлежит %OWNERNAME%.";
            public string PlayerOwnershipLoggerDestroyed = "Игрок [%USERNAME%:%STEAM_ID%] уничтожил %OBJECT%.";
            public string PlayerOwnershipObjectAttacked = "Ваш %OBJECT% пытаются сломать.";
            public string PlayerOwnershipObjectDestroyed = "Ваш %OBJECT% был уничтожен.";
            public string PlayerParalyzed = "Вы парализованы и не можете двинуться.";
            public string PlayerUnmuted = "Ваше молчание прошло, вы можете писать в чат!";
            public string PlayerWarpZoneInterrupt = "Телепортация в %INTO% прервалась";
            public string PlayerWarpZoneStart = "Вы будете перенесены в %INTO% через %SECONDS% секунд";
            public string PlayerWarpZoneTeleported = "Вы телепортировались в \"%ZONE%\"";
            public string ServerRestart = "ВНИМАНИЕ: Через %SECONDS% секунд сервер будет перезагружен.";

            public string ServerShutdown =
                "ВНИМАНИЕ: Через %SECONDS% секунд сервер будет выключен на технические работы.";

            public string ServerWillRestart = "ВНИМАНИЕ: Сервер перезагрузится через %SECONDS% секунд.";
            public string ServerWillShutdown = "ВНИМАНИЕ: Сервер будет выключен через %SECONDS% секунд.";

            public string ServerWorldSaved =
                "[COLOR#2FFF2F]Мир был сохранен, это заняло [COLOR#FFDF2F]%SECONDS%[COLOR#2FFF2F] секунд.";

            public string ServerWorldSaving =
                "[COLOR#FF2F2F]Началось сохранение мира, пожалуйста, оставайтесь там, где вы находитесь...";
        }
    }

    private class LoadMessages : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(Load());
        }

        public void LoadData() => StartCoroutine(Load());
        private IEnumerator Load()
        {
            if (File.Exists(@"serverdata\cfg\BCore\messages.rus.cfg"))
            {
                RuMessages = JsonConvert.DeserializeObject<Rus>(
                    File.ReadAllText(@"serverdata\cfg\BCore\messages.rus.cfg"));
            }
            else
            {
                RuMessages = new Rus
                {
                    RuMessage = new Rus.Message(),
                    Names = new Dictionary<string, string>(),
                    BodyPart = new Dictionary<string, string>()
                };
                var json = JsonConvert.SerializeObject(RuMessages, Formatting.Indented);
                File.WriteAllText(@"serverdata\cfg\BCore\messages.rus.cfg", json);
                Debug.Log("[BCore]: Messages config created!");
            }

            Debug.Log("[BCore]: Messages config initialized!");

            Destroy(gameObject);
            yield break;
        }
    }
}