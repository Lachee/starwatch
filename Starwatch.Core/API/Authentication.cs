/*
START LICENSE DISCLAIMER
Starwatch is a Starbound Server manager with player management, crash recovery and a REST and websocket (live) API. 
Copyright(C) 2020 Lachee

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program. If not, see < https://www.gnu.org/licenses/ >.
END LICENSE DISCLAIMER
*/
using Newtonsoft.Json;
using System;
using System.Security.Principal;
using WebSocketSharp.Net;

namespace Starwatch.API
{
    public class Authentication
    {
        const string SUPERBOT_NAME  = "bot_admin";
        const string SUPERUSER_NAME = "admin";

        /// <summary>
        /// The NET Idenfifer of the authentication
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public IIdentity Identity { get; set; }

        /// <summary>
        /// The name of the authentication
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Access key for the authentication.
        /// </summary>
        private string Password { get; }
        
        /// <summary>
        /// The method which the authentication was established.
        /// </summary>
        public AuthStyle Style { get; }
        public enum AuthStyle
        {
            User,
            Bot,
            Token
        }

        #region meta

        public BotAccount BotAccount { get; private set; }
        public Entities.Account StarboundAccount { get; private set; }

        public AuthToken? Token { get; set; }
        public struct AuthToken
        {
            [JsonProperty("account")]
            public string Account { get; set; }

            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }

            [JsonProperty("expiry")]
            public DateTime Expiry { get; set; }
        }

        #endregion

        #region Level Control

        /// <summary>
        /// Is the authentication a bot?
        /// </summary>
        public bool IsBot => AuthLevel == AuthLevel.Bot || AuthLevel == AuthLevel.SuperBot;

        /// <summary>
        /// Is the authentication a user?
        /// </summary>
        public bool IsUser => AuthLevel == AuthLevel.User || AuthLevel == AuthLevel.Admin || AuthLevel == AuthLevel.SuperUser;
      
        /// <summary>
        /// Is the authentication a admin user?
        /// </summary>
        public bool IsAdmin => AuthLevel == AuthLevel.Admin || AuthLevel == AuthLevel.SuperUser;

        /// <summary>
        /// The type of authentication. Used for permission checking.
        /// </summary>
        public AuthLevel AuthLevel { get; set; }

        #endregion

        #region History

        /// <summary>
        /// The time since the last action
        /// </summary>
        public DateTime LastActionTime { get; private set; }
        
        /// <summary>
        /// The total number of actions performed so far.
        /// </summary>
        public long TotalActionsPerformed { get; private set; }


        /// <summary>
        /// The last action performed by the authentication.
        /// </summary>
        public string LastAction { get; private set; }

        /// <summary>
        /// The time at which the ratelimit will reset
        /// </summary>
        public DateTime RateLimitResetTime => _lastRateLimitTime + TimeSpan.FromMinutes(1);

        private int _requestMinute = 0;
        private DateTime _lastRateLimitTime;

        #endregion

        private Authentication(string name, string password, AuthLevel level, AuthStyle style)
        {
            Name = name;
            Password = password;            
            LastAction = "init";
            LastActionTime = DateTime.UtcNow;
            _lastRateLimitTime = DateTime.UtcNow;

            TotalActionsPerformed = 0;

            BotAccount = null;
            StarboundAccount = null;

            Style = style;
            AuthLevel = level;
        }
        
        public Authentication(BotAccount account) : this(account.Name, account.Password, AuthLevel.Bot, AuthStyle.Bot)
        {
            this.BotAccount = account;
            if (account.IsAdmin) AuthLevel = AuthLevel.SuperBot;
            if (Name.Equals(SUPERBOT_NAME)) AuthLevel = AuthLevel.SuperBot;
        }

        public Authentication(Entities.Account account) : this(account.Name, account.Password, AuthLevel.User, AuthStyle.User)
        {
            this.StarboundAccount = account;
            if (account.IsAdmin) AuthLevel = AuthLevel.Admin;
            if (Name.Equals(SUPERUSER_NAME)) AuthLevel = AuthLevel.SuperUser;
        }

        /// <summary>
        /// Records the last action performed by the authentication, updating its tally, timer and history.
        /// </summary>
        /// <param name="action">The action performed</param>
        public void RecordAction(string action)
        {
            LastAction = action;
            LastActionTime = DateTime.UtcNow;
            TotalActionsPerformed++;
        }

        /// <summary>
        /// Increments the internal ratelimit counter. Automatically called on all requests.
        /// </summary>
        public void IncrementRateLimit() { _requestMinute++; }

        /// <summary>
        /// Gets the number of requests the authentication is allowed to make. Can be negative if they exceeded their limits.
        /// </summary>
        /// <returns></returns>
        public int GetRateLimitRemaining() => GetRateLimit() - _requestMinute;
        
        /// <summary>
        /// Checks the ratelimit and returns how close the connection is to going over.
        /// </summary>
        /// <returns></returns>
        public double CheckRateLimit()
        { 
            //Check if the minute should be restarted
            TimeSpan diff = (DateTime.UtcNow - _lastRateLimitTime);
            if (diff.TotalMinutes >= 1)
            {
                _requestMinute = 0;
                _lastRateLimitTime = DateTime.UtcNow;
            }

            //Return true if we are allowed
            return _requestMinute / (double) GetRateLimit();
        }

        /// <summary>
        /// Gets the number of requests that are allowed to be made per minute
        /// </summary>
        /// <returns></returns>
        public int GetRateLimit()
        {
            switch(AuthLevel)
            {
                default:
                case AuthLevel.User:
                    return 60;

                case AuthLevel.Admin:
                    return 240;

                case AuthLevel.Bot:
                    return 300;

                case AuthLevel.SuperBot:
                    return 500;

                case AuthLevel.SuperUser:
                    return 100000;
            }
        }

        /// <summary>
        /// Gets the credentials for this authentication
        /// </summary>
        /// <param name="context">The identity requesting the credential</param>
        /// <returns></returns>
        public NetworkCredential GetNetworkCredentials(IIdentity context)
        {
            if (context != null && context.Name.StartsWith("token_")) 
            {
                //We are a token identity, so we should supply a token answer
                if (Token.HasValue && DateTime.UtcNow < Token.Value.Expiry)
                    return new NetworkCredential(context.Name, Token.Value.AccessToken);
                
                //This authentication doesnt have a token so you cant login using this method.
                return null;
            }

            //Return the plain credentials
            return new NetworkCredential(Name, Password);
        }

        public override string ToString()
        {
            return $"Auth({Name}, {AuthLevel.ToString()}, {Style.ToString()})[{TotalActionsPerformed}]";
        }
    }
}
