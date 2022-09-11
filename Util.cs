namespace Astalo {
    public static class Utils {
        public static IEnumerable<KeyValuePair<string,string>> AsKeyValuePairs(this string[] strArr) {
            foreach (var str in strArr) {
                if (!string.IsNullOrEmpty(str) && str.TrimStart().StartsWith("//")) {
                    continue;
                }

                var equalsIndex = str.IndexOf('=');
                if (equalsIndex < 0) {
                    yield return new KeyValuePair<string, string>(str.Split('=')[0], "");
                } else {
                    yield return new KeyValuePair<string, string>(
                    str.Split('=')[0],
                    str.Substring(equalsIndex + 1, str.Length - equalsIndex - 1)
                    );
                }
            }
        }
    }
}