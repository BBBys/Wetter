using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Borys.Wetter
{
  internal class WetterLaden
  {
    //public static readonly System.Globalization.CultureInfo DEZIMALPUNKT = new System.Globalization.CultureInfo("en-US");
    //string str = Convert.ToString(MldEntr, MldHilfe.DEZIMALPUNKT);
    private static void Main(string[] args)
    {
      Dictionary<string, string> WetterDaten = null;
      const string FEHLER = "Fehler:\nDateiname angeben";
      //const string keyAPI = "666af1e3280edf48be94c5489c4cb18b";
      //const string idORT = "3207197";
      //string URL = $"http://api.openweathermap.org/data/2.5/weather?id={idORT}&lang=de&units=metric&APPID={keyAPI}";
      string WetterText, temp, druck, regen, feucht, windv, windr, wolken;
      Assembly assembly = Assembly.GetExecutingAssembly();
      FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
      string productVersion = fvi.ProductVersion;
      string titel = fvi.FileDescription; //Assemblyinfo -> Titel
      Console.WriteLine($"{titel} V{productVersion}");
      Console.Title = titel;
      if (args.Length != 1)
      {
        Console.Error.WriteLine(FEHLER);
        Console.Beep(440, 300);
        Console.Beep(880, 200);
        _ = Console.ReadKey();
        throw new Exception(FEHLER);
      }
      using (StreamReader rd = File.OpenText(args[0]))
        WetterText = rd.ReadLine();
      WetterDaten = Teilen(WetterText);
      /*
       * Ausgabe:temp,druck,regen1,feucht,windv,windr,wolken
      */
      temp = WetterDaten["main/temp"];
      druck = WetterDaten["main/sea_level"];
      regen = WetterDaten.ContainsKey("regen") ? WetterDaten["regen"] : "0";
      feucht = WetterDaten["main/humidity"];
      windv = WetterDaten["wind/speed"];
      windr = WetterDaten["wind/deg"];
      wolken = WetterDaten["clouds/all"];
      Console.WriteLine(temp);
      Console.WriteLine(druck);
      Console.WriteLine(regen);
      Console.WriteLine(feucht);
      Console.WriteLine(windv);
      Console.WriteLine(windr);
      Console.WriteLine(wolken);
      //Console.ReadLine();
    }

    /// <summary>
    /// abwechselnd Keys und Values aud sem String lesen, 
    /// Dictionary aufbauen,
    /// String immer weiter kürzen
    /// </summary>
    /// <param name="wetterText"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static Dictionary<string, string> Teilen(string wetterText)
    {
      bool ok;
      string key, value;
      Dictionary<string, string> wetterDaten = new Dictionary<string, string>();
      while (wetterText.Length > 0)
      {
        (ok, key, wetterText) = GetKey(wetterText);
        if (!ok)
          throw new Exception($"Fehler\nKey\t{key}\nRest\t{wetterText}");
        (ok, value, wetterText) = GetValue(wetterText);
        if (!ok)
          throw new Exception($"Fehler\nValue\t{value}\nRest\t{wetterText}");
        wetterDaten.Add(key, value);
      }
      //auftrennen
      wetterDaten = TrenneAuf("main", wetterDaten);
      wetterDaten = TrenneAuf("wind", wetterDaten);
      wetterDaten = TrenneAuf("clouds", wetterDaten);
      return wetterDaten;
    }

    private static Dictionary<string, string> TrenneAuf(
      string überKey,
      Dictionary<string, string> wetterDaten)
    {
      bool ok;
      string überValue, unterKey, unterValue;
      überValue = wetterDaten[überKey];
      wetterDaten.Remove(überKey);
      while (überValue.Length > 0)
      {
        (ok, unterKey, überValue) = GetKey(überValue);
        if (!ok)
          throw new Exception($"Fehler\nKey\t{unterKey}\nRest\t{überValue}");
        (ok, unterValue, überValue) = GetValue(überValue);
        if (!ok)
          throw new Exception($"Fehler\nValue\t{unterValue}\nRest\t{überValue}");
        wetterDaten.Add($"{überKey}/{unterKey}", unterValue);
      }
      return wetterDaten;
    }

    private static (bool ok, string v, string wetter) GetValue(string wetterDaten)
    {
      int pVorValue, pNachValue, pNachEintrag;
      string value, restDaten;
      if (wetterDaten.StartsWith("[{"))
      {
        pVorValue = 2;
        pNachValue = wetterDaten.IndexOf("}]", pVorValue);
        value = wetterDaten.Substring(pVorValue, pNachValue - pVorValue);
        pNachEintrag = wetterDaten.IndexOf(',', pNachValue + 1);
        if (pNachEintrag != pNachValue + 2)
        {
          Console.WriteLine($"kein Abschluss }}] nach {value}");
          return (false, value, wetterDaten);
        }
      }
      else
      if (wetterDaten.StartsWith("{"))
      {
        pVorValue = 1;
        pNachValue = wetterDaten.IndexOf('}', pVorValue);
        value = wetterDaten.Substring(pVorValue, pNachValue - pVorValue);
        pNachEintrag = wetterDaten.IndexOf(',', pNachValue + 1);
        if (pNachEintrag != pNachValue + 1)
        {
          Console.WriteLine($"kein Abschluss }} nach {value}");
          return (false, value, wetterDaten);
        }
      }
      else
      {
        pNachValue = wetterDaten.IndexOf(',', 1);
        if (pNachValue < 0)//der Letzte hat kein Komma, sondern }
          pNachValue = wetterDaten.IndexOf('}', 1);
        if (pNachValue < 0)//vielleich auch das nicht
        {
          pNachValue = wetterDaten.Length;
          pNachEintrag = pNachValue - 1;
        }
        else
          pNachEintrag = pNachValue;
        value = wetterDaten.Substring(0, pNachValue);
      }
      restDaten = wetterDaten.Substring(pNachEintrag + 1);
      return (true, value, restDaten);
    }

    private static (bool, string, string) GetKey(string wetterDaten)
    {
      int pVorKey, pNachKey, pVorValue;
      string key, restDaten;
      pVorKey = wetterDaten.IndexOf('"');
      pNachKey = wetterDaten.IndexOf('"', pVorKey + 1);
      key = wetterDaten.Substring(pVorKey + 1, pNachKey - pVorKey - 1);
      pVorValue = wetterDaten.IndexOf(':', pNachKey + 1);
      if (pVorValue != pNachKey + 1)
      {
        Console.WriteLine($"kein Wert zu {key}");
        return (false, key, wetterDaten);
      }
      else
        restDaten = wetterDaten.Substring(pVorValue + 1);
      return (true, key, restDaten);
    }
  }
}
