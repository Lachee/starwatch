using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Starwatch.Entities;
using System;

namespace Starwatch.Serializer
{
    class UserAccountsSerializer : JsonConverter
    {

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //Get the list
            AccountList manager = (AccountList)value;
            if (manager == null)
            {
                writer.WriteNull();
                return;
            }

            //Create a object to hold the elements
            JObject accountMapping = new JObject();
            foreach(var acc in manager.Accounts)
            {
                //Create the inner account object
                JObject accobj = new JObject();
                accobj.Add("admin", acc.Value.IsAdmin);
                accobj.Add("password", acc.Value.Password);

                //Add it to the holding element with the key of the account name
                accountMapping.Add(acc.Key, accobj);
            }

            //Write the mapping
            accountMapping.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {

            //CReate a new user object
            AccountList userAccounts = new AccountList();

            //Begin the read
            do
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string username = reader.Value.ToString();
                    Account account = new Account(username);

                    while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                    {
                        //Keep reading until we hit a end
                        if (reader.TokenType == JsonToken.PropertyName)
                        {
                            string property = reader.Value.ToString();
                            if (property == "admin")
                            {
                                account.IsAdmin = reader.ReadAsBoolean().GetValueOrDefault(false);
                            }

                            if (property == "password")
                            {
                                account.Password = reader.ReadAsString();
                            }
                        }
                    }

                    //add the account
                    userAccounts.AddAccount(account, true);
                }

            } while (reader.Read() && reader.TokenType != JsonToken.EndObject);

            userAccounts.UnmarkDirty();
            return userAccounts;
        }

        public override bool CanRead {  get { return true; } }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(AccountList);
        }
    }
}
