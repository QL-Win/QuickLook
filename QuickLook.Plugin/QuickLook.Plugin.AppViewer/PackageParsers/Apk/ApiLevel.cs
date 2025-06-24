using System.Collections.Generic;

namespace QuickLook.Plugin.AppViewer.PackageParsers.Apk;

public class ApiLevel
{
    public string AndroidVersion { get; set; }

    public string APILevel { get; set; }

    public string Codename { get; set; }

    public string Release { get; set; }

    public override string ToString()
    {
        return $"API Level {APILevel} ({AndroidVersion} {Codename} {Release})";
    }

    public static ApiLevel Create(string apiLevel)
    {
        if (apiLevels.TryGetValue(apiLevel, out var level))
        {
            return level;
        }

        return new ApiLevel
        {
            APILevel = apiLevel,
            AndroidVersion = $"API Level {apiLevel}",
            Codename = "Unknown",
            Release = "Unknown"
        };
    }

    private static readonly Dictionary<string, ApiLevel> apiLevels = new()
    {
        ["35"] = new ApiLevel
        {
            APILevel = "35",
            AndroidVersion = "Android 15",
            Codename = "VanillaIceCream",
            Release = "开发中（2025）"
        },
        ["34"] = new ApiLevel
        {
            APILevel = "34",
            AndroidVersion = "Android 14",
            Codename = "UpsideDownCake",
            Release = "2023"
        },
        ["33"] = new ApiLevel
        {
            APILevel = "33",
            AndroidVersion = "Android 13",
            Codename = "Tiramisu",
            Release = "2022"
        },
        ["32"] = new ApiLevel
        {
            APILevel = "32",
            AndroidVersion = "Android 12L",
            Codename = "",
            Release = "2022"
        },
        ["31"] = new ApiLevel
        {
            APILevel = "31",
            AndroidVersion = "Android 12",
            Codename = "Snow Cone",
            Release = "2021"
        },
        ["30"] = new ApiLevel
        {
            APILevel = "30",
            AndroidVersion = "Android 11",
            Codename = "Red Velvet Cake",
            Release = "2020"
        },
        ["29"] = new ApiLevel
        {
            APILevel = "29",
            AndroidVersion = "Android 10",
            Codename = "Quince Tart",
            Release = "2019"
        },
        ["28"] = new ApiLevel
        {
            APILevel = "28",
            AndroidVersion = "Android 9",
            Codename = "Pie",
            Release = "2018"
        },
        ["27"] = new ApiLevel
        {
            APILevel = "27",
            AndroidVersion = "Android 8.1",
            Codename = "Oreo MR1",
            Release = "2017"
        },
        ["26"] = new ApiLevel
        {
            APILevel = "26",
            AndroidVersion = "Android 8.0",
            Codename = "Oreo",
            Release = "2017"
        },
        ["25"] = new ApiLevel
        {
            APILevel = "25",
            AndroidVersion = "Android 7.1",
            Codename = "Nougat MR1",
            Release = "2016"
        },
        ["24"] = new ApiLevel
        {
            APILevel = "24",
            AndroidVersion = "Android 7.0",
            Codename = "Nougat",
            Release = "2016"
        },
        ["23"] = new ApiLevel
        {
            APILevel = "23",
            AndroidVersion = "Android 6.0",
            Codename = "Marshmallow",
            Release = "2015"
        },
        ["22"] = new ApiLevel
        {
            APILevel = "22",
            AndroidVersion = "Android 5.1",
            Codename = "Lollipop MR1",
            Release = "2015"
        },
        ["21"] = new ApiLevel
        {
            APILevel = "21",
            AndroidVersion = "Android 5.0",
            Codename = "Lollipop",
            Release = "2014"
        }
    };
}
