using Nez.Persistence;
using System.IO;

namespace TeamProject3
{
    public static class JsonExporter
    {
        public static void WriteToJson(string fileName, BossSettings bossSettings, bool nsonEnabled = false)
        {
            var nsonOptions = new NsonSettings
            {
                PreserveReferencesHandling = true,
                PrettyPrint = true
            };

            var jsonOptions = new JsonSettings
            {
                PreserveReferencesHandling = true,
                PrettyPrint = true,
                TypeNameHandling = TypeNameHandling.Auto
            };

            string data = "";

            if (nsonEnabled)
                data = Nson.ToNson(bossSettings, nsonOptions);
            else
                data = Json.ToJson(bossSettings, jsonOptions);

            string exportName = "Content/" + fileName;
            exportName += (nsonEnabled) ? ".nson" : ".json";

            File.WriteAllText(exportName, data);
        }
    }
}
