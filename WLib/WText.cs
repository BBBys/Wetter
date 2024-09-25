using System;
using System.Collections.Generic;

namespace Borys.Wetter
{
  public class WText
  {
    /// <summary>
    /// abwechselnd Keys und Values aud sem String lesen, 
    /// Dictionary aufbauen,
    /// String immer weiter kürzen
    /// </summary>
    /// <param name="wetterText"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Dictionary<string, string> TeileWetterText(string wetterText)
    {
      bool ok;
      string key, value;
      Dictionary<string, string> wetterDaten = new Dictionary<string, string>();
      try
      {
        while (wetterText.Length > 0)
        {
          (ok, key, wetterText) = GetKey(wetterText);
          if (!ok)
          {
            throw new Exception($"Fehler\nKey\t{key}\nRest\t{wetterText}");
          }

          (ok, value, wetterText) = GetValue(wetterText);
          if (!ok)
          {
            throw new Exception($"Fehler\nValue\t{value}\nRest\t{wetterText}");
          }

          wetterDaten.Add(key, value);
        }
        //auftrennen
        wetterDaten = TrenneAuf("main", wetterDaten);
        wetterDaten = TrenneAuf("wind", wetterDaten);
        wetterDaten = TrenneAuf("clouds", wetterDaten);
        wetterDaten = TrenneAuf("weather", wetterDaten);
      }
      catch (Exception ex) { Console.WriteLine(ex.Message); Console.WriteLine(wetterDaten); }
      return wetterDaten;
    }

    private static Dictionary<string, string> TrenneAuf(
      string überKey,
      Dictionary<string, string> wetterDaten)
    {
      bool ok;
      string überValue, unterKey, unterValue;
      überValue = wetterDaten[überKey];
      _ = wetterDaten.Remove(überKey);
      while (überValue.Length > 0)
      {
        (ok, unterKey, überValue) = GetKey(überValue);
        if (!ok)
        {
          throw new Exception($"Fehler\nKey\t{unterKey}\nRest\t{überValue}");
        }

        (ok, unterValue, überValue) = GetValue(überValue);
        if (!ok)
        {
          throw new Exception($"Fehler\nValue\t{unterValue}\nRest\t{überValue}");
        }

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
        {
          pNachValue = wetterDaten.IndexOf('}', 1);
        }

        if (pNachValue < 0)//vielleich auch das nicht
        {
          pNachValue = wetterDaten.Length;
          pNachEintrag = pNachValue - 1;
        }
        else
        {
          pNachEintrag = pNachValue;
        }

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
      {
        restDaten = wetterDaten.Substring(pVorValue + 1);
      }

      return (true, key, restDaten);
    }
  }
}