using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;

namespace Borys.Wetter
{
  internal class W0Abrufen
  {
    private static void Main(string[] args)
    {
      string andereQuelle = "https://metar-taf.com/de/EDVK";
      const string FEHLER = "Fehler:\nDateiname angeben", keyAPI = "666af1e3280edf48be94c5489c4cb18b",
idORT = "3207197";
      string URL = $"http://api.openweathermap.org/data/2.5/weather?id={idORT}&lang=de&units=metric&APPID={keyAPI}";
      Assembly assembly = Assembly.GetExecutingAssembly();
      FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
      string productVersion = fvi.ProductVersion;
      string titel = fvi.FileDescription; //Assemblyinfo -> Titel
      Debug.WriteLine($"{titel} V{productVersion}");
      Console.Title = titel;
      if (args.Length != 1)
      {
        Console.Error.WriteLine(FEHLER);
        Console.Beep(440, 300);
        Console.Beep(880, 200);
        _ = Console.ReadKey();
        throw new Exception(FEHLER);
      }
      Debug.WriteLine($"Ausgabe auf {args[0]}");
      using (WebClient client = new WebClient())
      using (StreamWriter writer = File.CreateText(args[0]))
      {
        string s = client.DownloadString(URL);
        writer.WriteLine(s);
      }
      Debug.WriteLine("fertig");
      _ = Console.ReadKey();
    }
  }
}
