using Newtonsoft.Json;
using System.Collections.Generic;

namespace Starwatch.Entities
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Account
    {
        public const string Annonymous = "<annonymous>";

        [JsonProperty("name")]
        public string Name { get; private set; }
        
        [JsonProperty("admin")]
        public bool IsAdmin { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        private Account() { this.Name = "unnamed"; }
        public Account(string name)
        {
            this.Name = name;
        }
    }
    
    public class AccountList 
    {
        private Dictionary<string, Account> _accounts = new Dictionary<string, Account>();

        public IReadOnlyDictionary<string, Account> Accounts => _accounts;
        public bool IsDirty { get; private set; }

        public delegate void AccountUpdateEvent(Account account);
        public delegate void AccountRemoveEvent(string accountName);

        public event AccountUpdateEvent OnAccountAdd;
        public event AccountRemoveEvent OnAccountRemove;
        public event AccountUpdateEvent OnAccountUpdate;

        public void AddAccount(Account account, bool force = false)
        {
            IsDirty = true;
            if (force) _accounts[account.Name] = account;
            else _accounts.Add(account.Name, account);

            OnAccountAdd?.Invoke(account);
        }

        public Account GetAccount(string name)
        {
            //Return a null acount since they do note have one
            if (string.IsNullOrWhiteSpace(name) || name.Equals(Account.Annonymous)) return null;

            //Add the account
            Account acc;
            if (!_accounts.TryGetValue(name, out acc)) return null;
            return acc;
        }

        public bool RemoveAccount(Account account) => RemoveAccount(account.Name);
        public bool RemoveAccount(string name)
        {
            if (_accounts.Remove(name))
            {
                OnAccountRemove?.Invoke(name);
                IsDirty = true;
                return true;
            }

            return false;
        }

        public void UpdateAccount(Account account, string password = null, bool? isAdmin = null)
        {
            account.Password = password ?? account.Password;
            account.IsAdmin = isAdmin ?? account.IsAdmin;
            IsDirty = true;
            OnAccountUpdate?.Invoke(account);
        }

        public void UnmarkDirty() { IsDirty = false; }
    }
}
