using Nez.Persistence;
using System.IO;

namespace TeamProject3
{
    public static class JsonExporter
    {
        private static NsonSettings _nsonOptions = new NsonSettings
        {
            PreserveReferencesHandling = true,
            PrettyPrint = true
        };

        private static JsonSettings _jsonOptions = new JsonSettings
        {
            PreserveReferencesHandling = true,
            PrettyPrint = true,
            TypeNameHandling = TypeNameHandling.Auto
        };

        public static void WriteToJson(string fileName, BossSettings bossSettings, bool nsonEnabled = false)
        {
            string data = "";

            if (nsonEnabled)
                data = Nson.ToNson(bossSettings, _nsonOptions);
            else
                data = Json.ToJson(bossSettings, _jsonOptions);

            string exportName = "Content/" + fileName;
            exportName += (nsonEnabled) ? ".nson" : ".json";

            File.WriteAllText(exportName, data);
        }

        public static BossSettings ReadFromJson(string fileName, bool isNson = false)
        {
            BossSettings bossSettings = null;

            fileName = $"Content/{fileName}";
            fileName += (isNson) ? ".nson" : ".json";

            var importData = File.ReadAllText(fileName);

            if (isNson)
            {
                bossSettings = Nson.FromNson<BossSettings>(importData, _nsonOptions);
            }
            else
            {
                bossSettings = Json.FromJson<BossSettings>(importData, _jsonOptions);
            }

            return bossSettings;
        }
    }
}
