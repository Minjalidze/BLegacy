using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RustExtended;
using Newtonsoft.Json;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("Auto Donate", "Romanchik34 (vk.com/romanchik34)", "1.0.0")]
    class RAutoDonate : RustLegacyPlugin
    {
        public const string ChatName = "Автодонат";
        public const int ShopID = 155862;

        public class ItemData
        {
            public int id;
            public int type;
            public int cost;
            public string name;
            public string description;
        }

        [ChatCommand("donate_test")]
        private void cmdDonate(NetUser netuser, string command, string[] args)
        {
            if (args.Length == 0)
            {
                RustServerManagement.Get().StartCoroutine(ShowDonateList(netuser));
                return;
            }

            if (args[0].ToLower() == "buy")
            {
                if (args.Length < 3)
                {
                    rust.SendChatMessage(netuser, ChatName, "Неверная команда. Используйте /donate buy <id> <количество>");
                    return;
                }

                int itemID = 0;
                if (!int.TryParse(args[1], out itemID))
                {
                    rust.SendChatMessage(netuser, ChatName, "В поле \"ID\" введено не целое число");
                    return;
                }

                int itemCount = 0;
                if (!int.TryParse(args[2], out itemCount))
                {
                    rust.SendChatMessage(netuser, ChatName, "В поле \"количество\" введено не целое число");
                    return;
                }

                RustServerManagement.Get().StartCoroutine(GetBuyLink(netuser, itemID, itemCount));
            }
        }

        private IEnumerator ShowDonateList(NetUser netuser)
        {
            WWW www = new WWW("https://rage.hostfun.ru/rageac/trademc.php?action=shop.getItems&shop_id=" + ShopID);
            yield return www;

            string resp = www.text.Replace("{\"response\":{\"categories\":[{\"id\":0,\"name\":\"Без категории\",\"items\":", "").Replace("}]}}", "");

            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.LogError("[RAutoDonate] Failed to get response from trademc: " + www.error);

                rust.SendChatMessage(netuser, ChatName, "Произошла ошибка при запросе информации о донате");
                yield break;
            }

            rust.SendChatMessage(netuser, ChatName, "Доступные предметы к покупке:");

            List<ItemData> items = JsonConvert.DeserializeObject<List<ItemData>>(resp);
            if (items == null || items.Count == 0)
            {
                rust.SendChatMessage(netuser, ChatName, "Произошла ошибка при запросе информации о донате (Предметов нет)");
                yield break;
            }

            Oxide.Core.Interface.GetMod().DataFileSystem.WriteObject("AutoDonate.Data", items);

            foreach (var item in items)
            {
                rust.SendChatMessage(netuser, ChatName, $"ID: {item.id} | Название: {item.name} | Цена: {item.cost}");
            }

            rust.SendChatMessage(netuser, ChatName, "Чтобы купить предмет, используйте /donate buy <id> <количество>");
            rust.SendChatMessage(netuser, ChatName, "После этого вам откроется страница для оплаты товара");
        }

        private IEnumerator GetBuyLink(NetUser netuser, int itemID, int itemCount)
        {
            string item = $"{itemID}:{itemCount}";

            WWW www = new WWW("https://rage.hostfun.ru/rageac/trademc.php?action=shop.buyItems&buyer=" + netuser.userID + "&items=" + item);
            yield return www;

            Puts(www.text);

            string resp = www.text.Replace("{\"response\":", "");
            resp = resp.Substring(0, resp.Length - 1);

            Puts(resp);

            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.LogError("[RAutoDonate] Failed to get response from trademc: " + www.error);

                rust.SendChatMessage(netuser, ChatName, "Произошла ошибка при запросе информации о донате");
                yield break;
            }

            if (Oxide.Core.Interface.GetMod().DataFileSystem.ExistsDatafile("AutoDonate.Data"))
            {
                var items = Oxide.Core.Interface.GetMod().DataFileSystem.ReadObject<List<ItemData>>("AutoDonate.Data");

                if (!items.Exists(f => f.id == itemID))
                {
                    rust.SendChatMessage(netuser, ChatName, "Предмета с указанным ID не существует");
                    yield break;
                }

                var buyResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(resp);
                foreach (var i in buyResponse)
                    Puts(i.Key + " : " + i.Value);

                if (buyResponse == null || !buyResponse.ContainsKey("cart_id") || !buyResponse.ContainsKey("total"))
                {
                    rust.SendChatMessage(netuser, ChatName, "Произошла ошибка при запросе информации о донате (Пустой ответ на покупку)");
                    yield break;
                }

                rust.SendChatMessage(netuser, ChatName, $"ID покупки: {buyResponse["cart_id"]}");
                rust.SendChatMessage(netuser, ChatName, "Через 5 секунд страница для оплаты откроется в вашем браузере");

                timer.Once(5f, () =>
                {
                    netuser.playerClient.networkView.RPC("OpenLink", netuser.playerClient.netPlayer, "https://pay.trademc.org/?cart_id=" + buyResponse["cart_id"]);
                });
            }
            else 
            {
                rust.SendChatMessage(netuser, ChatName, "Не удалось получить информацию о товарах. Напишите /donate и попробуйте снова");
            }
        }
    }
}
