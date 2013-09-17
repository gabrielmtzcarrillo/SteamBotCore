﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using SteamTrade.TradeWebAPI;
using Newtonsoft.Json;
using System.Net;
using SteamKit2;

namespace SteamTrade
{
    public class Offer
    {
        public List<TradeUserAssets> assets { get; set; }
        public List<object> currency { get; set; }
        public bool ready { get; set; }

        public Offer()
        {
            assets = new List<TradeUserAssets>();
            currency = new List<object>();
            ready = true;
        }
    }


    public class TradeOffer
    {
        CookieContainer cookies;
        static string SteamCommunityDomain = "steamcommunity.com";
        static string SteamTradeOfferURL = "http://steamcommunity.com/tradeoffer/new/";

        readonly string sessionId;
        readonly string steamLogin;

        public TradeOfferJson jsonObj;
        public List<TradeUserAssets> itemSlots = new List<TradeUserAssets>();
        public string response;

        SteamID partner;

        public TradeOffer(string sessionId, string steamLogin)
        {
            this.sessionId = Uri.UnescapeDataString(sessionId);
            this.steamLogin = steamLogin;

            cookies = new CookieContainer();

            cookies.Add(new Cookie("sessionid", Uri.UnescapeDataString(this.sessionId), String.Empty, SteamCommunityDomain));
            cookies.Add(new Cookie("steamLogin", steamLogin, String.Empty, SteamCommunityDomain));
            jsonObj = new TradeOfferJson();
        }

        public bool Start(SteamID partner)
        {
            this.partner = partner;
            try
            {
                response = SteamWeb.Fetch(SteamTradeOfferURL + "?partner=" + this.partner.AccountID, "GET", null, cookies);
                return true;
            }
            catch (Exception e)
            {
                response = e.Message;
                return false;
            }
        }

        public bool AddItem(TradeUserAssets item)
        {
            var data = new NameValueCollection();
            int i = 0;
            data.Add("sessionid", Uri.UnescapeDataString(this.sessionId));
            data.Add("appid",""+item.appid);
            data.Add("contextid",""+item.contextid);
            data.Add("itemid", "" + item.assetid);
            
            foreach (TradeUserAssets offereditem in itemSlots)
            {
                i++;
                if (offereditem.assetid == item.assetid)
                    break;
            }

            if (i == 0)
                i = 1;

            data.Add("slot", ""+i);

            try
            {
                response = SteamWeb.Fetch("http://steamcommunity.com/trade/" + partner.ConvertToUInt64() + "/additem/", "POST", data, cookies, true);
                jsonObj.me.assets.Add(item);
                jsonObj.version++;
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public bool MakeOffer(string message = "")
        {
            var data = new NameValueCollection();
            string json = JsonConvert.SerializeObject(jsonObj);
            jsonObj.me.ready = true;

            data.Add("sessionid",Uri.UnescapeDataString(this.sessionId));
            data.Add("partner",partner.ConvertToUInt64().ToString());
            data.Add("tradeoffermessage", message);
            data.Add("json_tradeoffer", json);
            
            try
            {
                response = SteamWeb.Fetch(SteamTradeOfferURL + "send", "POST", data,cookies);
                return true;
            }
            catch (Exception e)
            {
                response = e.Message;
                return false;
            }

        }
    }

    public class TradeOfferJson
    {
        public bool newversion = true;
        public int version = 1;

        public Offer me { get; set; }
        public Offer them { get; set; }

        public TradeOfferJson()
        {
            me = new Offer();
            them = new Offer();
        }
    }
}
