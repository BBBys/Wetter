//using MySql.Data.MySqlClient;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
namespace Borys.Wetter
{
  internal class Tage
  {
#if DEBUG
    private const int ANZAHLEINZELWERTE = 12;
#else
    private const int ANZAHLEINZELWERTE = 48;
#endif

    private static void Main(string[] args)
    {
      const string FEHLER = "Fehler:\nDB angeben";
      Assembly assembly = Assembly.GetExecutingAssembly();
      FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
      string productVersion = fvi.ProductVersion;
      string titel = fvi.FileDescription; //Assemblyinfo -> Titel
      Console.WriteLine($"{titel} V{productVersion}");
      Console.Title = titel;
      string DBHOST = args.Length > 0 ? args[0] : "localhost";
      if (args.Length > 1)
      {
        Console.Error.WriteLine(FEHLER);
        Console.Beep(440, 300);
        Console.Beep(880, 200);
        _ = Console.ReadKey();
        throw new Exception(FEHLER);
      }
      using (MySqlConnection con = DbOps.ConnectToDB(DBHOST))
      using (MySqlCommand SQL = new MySqlCommand(string.Empty, con))
      {
        try
        {
          uint nMw = 0;
          DateTime dtLetzterTw, dtNächster;
          long utLetzterTw;
          con.Open();
          SQL.CommandText = $"select max(zeit) from {DbParameter.DBTTW};";
          MySqlDataReader reader = SQL.ExecuteReader();
          if (!reader.Read())
          { throw new Exception("keine Tageswerte in DB"); }
          utLetzterTw = reader.GetInt64(0);//UNIX Timestamp: Sekunden seit 1.1.1970
          reader.Close();
          dtLetzterTw = DateTimeOffset.FromUnixTimeSeconds(utLetzterTw).DateTime;
          Console.WriteLine($"letzter Wert\t{dtLetzterTw.ToLongDateString()} {dtLetzterTw.ToLongTimeString()}");
          dtNächster = dtLetzterTw.AddDays(1).Date;
          do
          {
            if (dtNächster < DateTime.Now.Date)
            {
              nMw += EinenTag(dtNächster, SQL);
              dtNächster = dtNächster.AddDays(1).Date;
            }
            else
            {
              Console.WriteLine("heute erreicht - Ende");
              break;
            }
          } while (nMw < ANZAHLEINZELWERTE);

        }
        catch (MySqlException ex) { Console.WriteLine(con.ConnectionString); Console.WriteLine(ex.Message); }
        catch (Exception ex) { Console.WriteLine(ex.Message); }
      }

      Console.Beep(440, 300);
      Console.Beep(880, 200);
      _ = Console.ReadKey();
    }

    private static uint EinenTag(DateTime dtNächster, MySqlCommand SQL)
    {
      MySqlDataReader reader;
      long utAnfang, utEnde;
      uint nMw;
      utAnfang = new DateTimeOffset(dtNächster, TimeSpan.Zero).ToUnixTimeSeconds();
      utEnde = utAnfang + Konstanten.SECPROTAG;
      Console.WriteLine("*****     für einen Tag:     *****");
      Console.WriteLine(
        $"Berechnung für\t{DateTimeOffset.FromUnixTimeSeconds(utAnfang).DateTime.ToLongDateString()} {DateTimeOffset.FromUnixTimeSeconds(utAnfang).DateTime.ToLongTimeString()}");
      Console.WriteLine(
        $"\tbis\t{DateTimeOffset.FromUnixTimeSeconds(utEnde).DateTime.ToLongDateString()} {DateTimeOffset.FromUnixTimeSeconds(utEnde).DateTime.ToLongTimeString()}");
      SQL.CommandText = $"SELECT COUNT(zeit) AS Anzahl FROM {DbParameter.DBTMW} WHERE zeit BETWEEN {utAnfang} AND {utEnde}";
      reader = SQL.ExecuteReader();
      if (!reader.Read())
      {
        throw new Exception("Messwerte fehlen");
      }

      nMw = reader.GetUInt32("Anzahl");
      reader.Close();
      Console.WriteLine($"{nMw} Messwerte");
      if (nMw > 0)
      {
        Dictionary<string, double> ergDouble = new Dictionary<string, double>();
        uint n = 0;
        ulong zeit, szeit = 0;
        double tmin = 99.0, tmax = -99.0, rdn;
        string feucht, windv, wolken, druck, stmin, stmax;
        SQL.CommandText = $"SELECT * FROM {DbParameter.DBTMW} WHERE zeit BETWEEN {utAnfang} AND {utEnde}";
        reader = SQL.ExecuteReader();
        while (reader.Read())
        {
          Dictionary<string, object> werte;
          n++;
          werte = DbOps.GetDictionaryFromReader(reader);
          foreach (KeyValuePair<string, object> item in werte)
          {
            double v;
            switch (item.Key)
            {
              case "zeit":
                szeit += Convert.ToUInt64(item.Value);
                break;
              case "temp":
                v = Convert.ToDouble(item.Value);
                if (ergDouble.ContainsKey(item.Key))
                {
                  ergDouble[item.Key] = ergDouble[item.Key] + v;
                  if (v < tmin)
                  {
                    tmin = v;
                  }

                  if (v > tmax)
                  {
                    tmax = v;
                  }
                }
                else
                {
                  ergDouble.Add(item.Key, v);
                  tmin = v;
                  tmax = v;
                }
                break;
              case "druck":
              case "feels":
              case "feucht":
              case "sicht":
              case "windv":
              case "wolken":
                v = Convert.ToDouble(item.Value);
                if (ergDouble.ContainsKey(item.Key))
                { ergDouble[item.Key] = ergDouble[item.Key] + v; }
                else
                { ergDouble.Add(item.Key, v); }
                break;
              case "regen1h":
              case "regen3h":
                Console.WriteLine($"prüfen: {item.Key} = >{item.Value}<");
                break;
              case "basis":
              case "bearbeitet":
              case "descr":
              case "eintrag":
              case "main":
              case "ort":
              case "windr":
                break;
              default:
                Console.WriteLine($"{item.Key}={item.Value}");
                break;
            }//switch
          }//foreach
        }//while
        reader.Close();
        Debug.Assert(n == nMw, "Anzahl stimmt nicht");
        rdn = 1.0 / n;
        zeit = (ulong)(szeit * rdn);
        feucht = Convert.ToString(ergDouble["feucht"] * rdn, Konstanten.DEZIMALPUNKT);
        windv = Convert.ToString(ergDouble["windv"] * rdn, Konstanten.DEZIMALPUNKT);
        wolken = Convert.ToString(ergDouble["wolken"] * rdn, Konstanten.DEZIMALPUNKT);
        druck = Convert.ToString(ergDouble["druck"] * rdn, Konstanten.DEZIMALPUNKT);
        stmin = Convert.ToString(tmin, Konstanten.DEZIMALPUNKT);
        stmax = Convert.ToString(tmax, Konstanten.DEZIMALPUNKT);
        SQL.CommandText =
          $@"INSERT INTO {DbParameter.DBTTW} 
          (zeit,mintemp,maxtemp,feucht,windv,wolken,druck) 
          values ({zeit},{stmin},{stmax},{feucht},{windv},{wolken},{druck})";
        _ = SQL.ExecuteNonQuery();
      }
      return nMw;
    }
  }
}