using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.DInspect.Models.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Service.DInspect.Helpers
{
    public static class StaticHelper
    {
        public static object GetPropValue(dynamic rsc, string propName)
        {
            if (!IsNullOrEmpty(rsc))
            {
                JObject jObj = JObject.Parse(JsonConvert.SerializeObject(rsc));
                return jObj[propName];
            }

            return null;
        }

        public static object GetPropValue(dynamic rsc, string key, string propName)
        {
            if (!IsNullOrEmpty(rsc))
            {
                string json = JsonConvert.SerializeObject(rsc);
                JObject jObj = JObject.Parse(json);
                JToken token = JToken.Parse(json);

                var propValue = token.SelectTokens($"$..[?(@.{EnumQuery.Key}=='{key}')]['{propName}']")
                       .Select(x => jObj.SelectToken(x.Path).Value<object>())
                       .FirstOrDefault();

                return propValue;
            }

            return null;
        }

        public static object GetPropValue(dynamic rsc, string propName, string propValue, string propSelected)
        {
            if (!IsNullOrEmpty(rsc))
            {
                string json = JsonConvert.SerializeObject(rsc);
                JObject jObj = JObject.Parse(json);
                JToken token = JToken.Parse(json);

                var propSelectedValue = token.SelectTokens($"$..[?(@.{propName}=='{propValue}')]['{propSelected}']")
                       .Select(x => jObj.SelectToken(x.Path).Value<object>())
                       .FirstOrDefault();

                return propSelectedValue;
            }

            return null;
        }

        public static List<object> GetPropValues(dynamic rsc, string propName, string propValue, string propSelected)
        {
            if (!IsNullOrEmpty(rsc))
            {
                string json = JsonConvert.SerializeObject(rsc);
                JObject jObj = JObject.Parse(json);
                JToken token = JToken.Parse(json);

                var propSelectedValue = token.SelectTokens($"$..[?(@.{propName}=='{propValue}')]['{propSelected}']")
                       .Select(x => jObj.SelectToken(x.Path).Value<object>()).ToList();

                return propSelectedValue;
            }

            return null;
        }

        private static bool IsNullOrEmpty(this JToken token)
        {
            return token == null ||
                   token.Type == JTokenType.Array && !token.HasValues ||
                   token.Type == JTokenType.Object && !token.HasValues ||
                   token.Type == JTokenType.String && token.ToString() == string.Empty ||
                   token.Type == JTokenType.Null;
        }

        public static List<JToken> GetData(dynamic rsc, string propName)
        {
            JToken token = JToken.Parse(JsonConvert.SerializeObject(rsc));
            return token.SelectTokens($"$..{propName}").ToList();
        }

        public static List<JToken> GetData(dynamic rsc, string propName, string propValue)
        {
            JToken jToken = JToken.Parse(JsonConvert.SerializeObject(rsc));
            return jToken.SelectTokens($"$..[?(@.{propName} == '{propValue}')]").ToList();
        }

        public static List<JToken> GetDataNotEqual(dynamic rsc, string propName, string propValue)
        {
            JToken jToken = JToken.Parse(JsonConvert.SerializeObject(rsc));
            return jToken.SelectTokens($"$..[?(@.{propName} != '{propValue}')]").ToList();
        }

        public static object GetParentData(dynamic rsc, string propName, string propValue)
        {
            JToken jToken = JToken.Parse(JsonConvert.SerializeObject(rsc));
            JObject jObj = JObject.Parse(JsonConvert.SerializeObject(rsc));

            return jToken.SelectTokens($"$..[?(@.{propName} == '{propValue}')]")?
                .Select(x => jObj.SelectToken(x.Parent.Parent.Parent.Path).Value<dynamic>())
                .FirstOrDefault();
        }

        public static object GetParentAdjData(dynamic rsc, string propName, string propValue)
        {
            JToken jToken = JToken.Parse(JsonConvert.SerializeObject(rsc));
            JObject jObj = JObject.Parse(JsonConvert.SerializeObject(rsc));

            return jToken.SelectTokens($"$...[?(@.{propName} == '{propValue}')]")?
                .Select(x => jObj.SelectToken(x.Parent.Parent.Path).Value<dynamic>())
                .FirstOrDefault();
        }

        public static object GetParentAdjIndexData(dynamic rsc, string propName, string propValue, int index)
        {
            JToken jToken = JToken.Parse(JsonConvert.SerializeObject(rsc));
            JObject jObj = JObject.Parse(JsonConvert.SerializeObject(rsc));
            var stringIdx = string.Empty;
            for (int i = 0; i < index; i++)
            {
                stringIdx += ".";
            }

            return jToken.SelectTokens($"${stringIdx}[?(@.{propName} == '{propValue}')]")?
                .Select(x => jObj.SelectToken(x.Parent.Parent.Parent.Path).Value<dynamic>())
                .FirstOrDefault();
        }

        public static string GetPropertyName<T>(Expression<Func<T>> propertyExpression)
        {
            return (propertyExpression.Body as MemberExpression).Member.Name;
        }

        public static JArray FilterEqual(this JArray array, string field, string value)
            => new JArray(array.Children().Where(GenerateEqualFilter(field, value)));

        public static JArray FilterMoreThan(this JArray array, string field, DateTime value)
            => new JArray(array.Children().Where(GenerateMoreThanFilter(field, value)));

        public static JArray FilterNotEqual(this JArray array, string field, string value)
            => new JArray(array.Children().Where(GenerateNotEqualFilter(field, value)));

        public static JArray FilterMoreThan(this JArray array, string field, string value)
            => new JArray(array.Children().Where(GenerateMoreThanFilter(field, value)));

        public static JArray FilterLessThan(this JArray array, string field, string value)
            => new JArray(array.Children().Where(GenerateLessThanFilter(field, value)));

        public static JArray FilterMoreEqualsThan(this JArray array, string field, string value)
            => new JArray(array.Children().Where(GenerateMoreEqualsThanFilter(field, value)));

        public static JArray FilterLessEqualsThan(this JArray array, string field, string value)
            => new JArray(array.Children().Where(GenerateLessEqualsThanFilter(field, value)));

        public static JArray FilterIn(this JArray array, string field, List<string> values)
            => new JArray(array.Children().Where(GenerateInFilter(field, values)));

        public static JArray GenerateNotInFilter(this JArray array, string field, List<string> values)
            => new JArray(array.Children().Where(GenerateNotInFilter(field, values)));

        private static Func<JToken, bool> GenerateEqualFilter(string field, string value)
            => (token) => string.Equals(token[field]?.Value<string>(), value);

        private static Func<JToken, bool> GenerateNotEqualFilter(string field, string value)
            => (token) => !string.Equals(token[field]?.Value<string>(), value);

        private static Func<JToken, bool> GenerateInFilter(string field, List<string> values)
            => (token) => values.Any(x => x == token[field]?.Value<string>());

        private static Func<JToken, bool> GenerateNotInFilter(string field, List<string> values)
            => (token) => !values.Any(x => x == token[field]?.Value<string>());

        private static Func<JToken, bool> GenerateMoreThanFilter(string field, dynamic values)
            => (token) => token[field]?.Value<dynamic>() > values;

        private static Func<JToken, bool> GenerateLessThanFilter(string field, dynamic values)
            => (token) => token[field]?.Value<dynamic>() < values;

        private static Func<JToken, bool> GenerateMoreEqualsThanFilter(string field, dynamic values)
           => (token) => token[field]?.Value<dynamic>() >= values;

        private static Func<JToken, bool> GenerateLessEqualsThanFilter(string field, dynamic values)
           => (token) => token[field]?.Value<dynamic>() <= values;

        public static bool TimeBetween(TimeSpan now, TimeSpan start, TimeSpan end)
        {
            if (start < end)
                return start <= now && now <= end;
            return !(end < now && now < start);
        }

        // Return -1 if version1 is smaller, 1 if version2 is smaller, 0 if equal
        public static int VersionCompare(string version1, string version2)
        {
            int vnum1 = 0, vnum2 = 0;

            if (version1.Split(".").Count() != version2.Split(".").Count())
                return -1;

            for (int i = 0, j = 0; i < version1.Length || j < version2.Length;)
            {
                while (i < version1.Length && version1[i] != '.')
                {
                    vnum1 = vnum1 * 10 + (version1[i] - '0');
                    i++;
                }

                while (j < version2.Length && version2[j] != '.')
                {
                    vnum2 = vnum2 * 10 + (version2[j] - '0');
                    j++;
                }

                if (vnum1 > vnum2)
                    return 1;

                if (vnum2 > vnum1)
                    return -1;

                vnum1 = vnum2 = 0;
                i++;
                j++;
            }

            return 0;
        }

    }
}