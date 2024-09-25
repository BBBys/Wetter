using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;



namespace Borys.Wetter
{
  internal class inDB
  {
    /// <summary>
    /// Wetterdaten von Datei in DB
    /// </summary>
    /// <param name="args"></param>
    /// <exception cref="Exception"></exception>
    private static void Main(string[] args)
    {
      string DBHOST = "localhost";
      Dictionary<string, string> WetterDaten = null;
      const string FEHLER = "Fehler:\nDB angeben";
      //const string keyAPI = "666af1e3280edf48be94c5489c4cb18b";
      //const string idORT = "3207197";
      //string URL = $"http://api.openweathermap.org/data/2.5/weather?id={idORT}&lang=de&units=metric&APPID={keyAPI}";
      string WetterText, main, descr, basis, sicht, zeit, ort, feels, temp, druck, regen1h, regen3h, feucht, windv, windr, wolken;
      Assembly assembly = Assembly.GetExecutingAssembly();
      FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
      string productVersion = fvi.ProductVersion;
      string titel = fvi.FileDescription; //Assemblyinfo -> Titel
      Debug.WriteLine($"{titel} V{productVersion}");
      Console.Title = titel;
      DBHOST = args.Length > 1 ? args[1] : "localhost";
      if (args.Length < 1)
      {
        Console.Error.WriteLine(FEHLER);
        Console.Beep(440, 300);
        Console.Beep(880, 200);
        _ = Console.ReadKey();
        throw new Exception(FEHLER);
      }
      using (StreamReader rd = File.OpenText(args[0]))
        WetterText = rd.ReadLine();
      WetterDaten = WText.TeileWetterText(WetterText);
      zeit = WetterDaten["dt"];
      ort = WetterDaten["name"];
      feels = WetterDaten["main/feels_like"];
      temp = WetterDaten["main/temp"];
      druck = WetterDaten["main/sea_level"];
      regen1h = WetterDaten.ContainsKey("regen") ? WetterDaten["regen"] : "0";
      regen3h = "0";
      feucht = WetterDaten["main/humidity"];
      windv = WetterDaten["wind/speed"];
      windr = WetterDaten["wind/deg"];
      wolken = WetterDaten["clouds/all"];
      sicht = WetterDaten["visibility"];
      basis = WetterDaten["base"];
      descr = WetterDaten["weather/description"];
      main = WetterDaten["weather/main"];
      using (MySqlConnection con = DbOps.ConnectToDB(DBHOST))
      using (MySqlCommand SQL = new MySqlCommand(string.Empty, con))
      {
        con.Open();
        SQL.CommandText = $"INSERT INTO messwerte (zeit) VALUES ({zeit});";
        try
        {
          SQL.ExecuteNonQuery();
        }
        catch (MySqlException ex)
        {
          if (!DbExcepts.IfMySQLKeyDoppeltEx(ex))
            throw ex;
        }
        SQL.CommandText =
          $"UPDATE messwerte SET ort={ort},temp={temp},feels={feels} WHERE zeit='{zeit}';";
        SQL.ExecuteNonQuery();
        SQL.CommandText =
          $"UPDATE messwerte " +
          $"SET feucht={feucht},windv={windv},windr={windr},wolken={wolken} WHERE zeit='{zeit}';";
        SQL.ExecuteNonQuery();
        SQL.CommandText =
          $"UPDATE messwerte " +
          $"SET regen1h={regen1h},regen3h={regen3h},sicht={sicht},druck={druck} WHERE zeit='{zeit}';";
        SQL.ExecuteNonQuery();
        SQL.CommandText =
          $"UPDATE messwerte " +
          $"SET main={main},descr={descr},basis={basis} WHERE zeit='{zeit}';";
        SQL.ExecuteNonQuery();
        con.Close();
      }
    }
  }
}
