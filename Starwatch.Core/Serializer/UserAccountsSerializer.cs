using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Starwatch.Entities;
using System;
using System.Collections.Generic;

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
            //Create the new account list and set its values.
            AccountList acclist = new AccountList();
            acclist.SetAccounts(ReadAccountEnumerator(reader));
            return acclist;
        }

        public override bool CanRead {  get { return true; } }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(AccountList);
        }

        /// <summary>
        /// Enumerates over the reader and creates new accounts. Finishes at the end of the object.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private IEnumerable<Account> ReadAccountEnumerator(JsonReader reader)
        {
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
                    yield return account;
                }

            } while (reader.Read() && reader.TokenType != JsonToken.EndObject);
        }
    }
}
