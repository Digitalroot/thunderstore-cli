using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace ThunderstoreCLI.Utils;

public static class SteamUtils
{
    public static string? FindInstallDirectory(string steamAppId)
    {
        var path = GetAcfPath(steamAppId);
        if (path == null)
        {
            return null;
        }

        var folderName = ManifestInstallLocationRegex.Match(File.ReadAllText(path)).Groups[1].Value;

        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "common", folderName));
    }

    public static bool IsProtonGame(string steamAppId)
    {
        var path = GetAcfPath(steamAppId);
        if (path == null)
        {
            throw new ArgumentException($"{steamAppId} is not installed!");
        }

        var source = PlatformOverrideSourceRegex.Match(File.ReadAllText(path)).Groups[1].Value;
        return source switch
        {
            "" => false,
            "linux" => false,
            _ => true
        };
    }

    private static string? GetAcfPath(string steamAppId)
    {
        string? primarySteamApps = FindSteamAppsDirectory();
        if (primarySteamApps == null)
        {
            return null;
        }
        List<string> libraryPaths = new() { primarySteamApps };
        foreach (var file in Directory.EnumerateFiles(primarySteamApps))
        {
            if (!Path.GetFileName(file).Equals("libraryfolders.vdf", StringComparison.OrdinalIgnoreCase))
                continue;
            libraryPaths.AddRange(SteamAppsPathsRegex.Matches(File.ReadAllText(file)).Select(x => x.Groups[1].Value).Select(x => Path.Combine(x, "steamapps")));
            break;
        }

        string acfName = $"appmanifest_{steamAppId}.acf";
        foreach (var library in libraryPaths)
        {
            foreach (var file in Directory.EnumerateFiles(library))
            {
                if (Path.GetFileName(file).Equals(acfName, StringComparison.OrdinalIgnoreCase))
                {
                    return file;
                }
            }
        }
        return null;
    }

    private static readonly Regex SteamAppsPathsRegex = new(@"""path""\s+""(.+)""");
    private static readonly Regex ManifestInstallLocationRegex = new(@"""installdir""\s+""(.+)""");
    private static readonly Regex PlatformOverrideSourceRegex = new(@"""platform_override_source""\s+""(.+)""");

    public static string? FindSteamDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return FindSteamDirectoryWin();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return FindSteamDirectoryOsx();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return FindSteamDirectoryLinux();
        else
            throw new NotSupportedException("Unknown operating system");
    }

    public static string? FindSteamAppsDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return FindSteamAppsDirectoryWin();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return FindSteamAppsDirectoryOsx();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return FindSteamAppsDirectoryLinux();
        else
            throw new NotSupportedException("Unknown operating system");
    }

    [SupportedOSPlatform("Windows")]
    private static string? FindSteamDirectoryWin()
    {
        return Registry.LocalMachine.OpenSubKey(@"Software\WOW6432Node\Valve\Steam", false)?.GetValue("InstallPath") as string;
    }

    private static string? FindSteamAppsDirectoryWin()
    {
        var steamDir = FindSteamDirectory();
        if (steamDir == null)
        {
            return null;
        }
        return Path.Combine(steamDir, "steamapps");
    }

    private static string? FindSteamDirectoryOsx()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library",
            "Application Support",
            "Steam"
        );
    }

    private static string? FindSteamAppsDirectoryOsx()
    {
        var steamDir = FindSteamDirectory();
        if (steamDir == null)
        {
            return null;
        }
        return Path.Combine(steamDir, "steamapps");
    }

    private static string? FindSteamDirectoryLinux()
    {
        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] possiblePaths = {
            Path.Combine(homeDir, ".local", "share", "Steam"),
            Path.Combine(homeDir, ".steam", "steam"),
            Path.Combine(homeDir, ".steam", "root"),
            Path.Combine(homeDir, ".steam"),
            Path.Combine(homeDir, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam"),
            Path.Combine(homeDir, ".var", "app", "com.valvesoftware.Steam", ".steam", "steam"),
            Path.Combine(homeDir, ".var", "app", "com.valvesoftware.Steam", ".steam", "root"),
            Path.Combine(homeDir, ".var", "app", "com.valvesoftware.Steam", ".steam")
        };
        string? steamPath = null;
        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                steamPath = path;
                break;
            }
        }
        return steamPath;
    }

    private static string? FindSteamAppsDirectoryLinux()
    {
        var steamPath = FindSteamDirectory();
        if (steamPath == null)
        {
            return null;
        }

        var possiblePaths = new[]
        {
            Path.Combine(steamPath, "steamapps"), // most distros
            Path.Combine(steamPath, "steam", "steamapps"), // ubuntu apparently
            Path.Combine(steamPath, "root", "steamapps"), // no idea
        };
        string? steamAppsPath = null;
        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                steamAppsPath = path;
                break;
            }
        }

        return steamAppsPath;
    }
}
