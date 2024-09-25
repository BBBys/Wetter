using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
namespace Borys.Wetter
{
  internal class Monate
  {
#if DEBUG
    private const int ANZAHLEINZELWERTE = 24 * 31;
#else
    private const int ANZAHLEINZELWERTE = 36 * 31;
#endif

    private static void Main(string[] args)
    {
      const string FEHLER = "Fehler:\nnur DB-Host angeben";
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
          MySqlDataReader reader;
          uint nMw = 0;
          DateTime dtLetzterMonatswert, dtImNächstMon, dtAnfDiesMon;
          long utLetzterMonatswert;
          con.Open();
          SQL.CommandText = $"select max(zeit) from {DbParameter.DBTMoW};";
          reader = SQL.ExecuteReader();
          if (!reader.Read())
          {
            throw new Exception("keine Monatswerte in DB");
          }

          utLetzterMonatswert = reader.GetInt64(0);//UNIX Timestamp: Sekunden seit 1.1.1970
          reader.Close();
          dtLetzterMonatswert = DateTimeOffset.FromUnixTimeSeconds(utLetzterMonatswert).DateTime;
          Console.WriteLine($"letzter Monatswert\t{dtLetzterMonatswert.ToLongDateString()}");
          //ein Tag im nächsten zu berechnenden Monat
          dtImNächstMon = dtLetzterMonatswert.AddMonths(1).Date;
          //erster Tag des aktuellen Monats
          dtAnfDiesMon = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
          do
          {
            DateTime dtAnfNächstMon;
            //erster Tag des nächsten zu berechnenden Monats
            dtAnfNächstMon = new DateTime(dtImNächstMon.Year, dtImNächstMon.Month, 1);
            if (dtImNächstMon < dtAnfDiesMon.Date)
            //das Datum im nächsten Monat liegt vor dem Anfang des aktuellen Monats
            {
              nMw += EinenMonat(dtAnfNächstMon, SQL);
              dtImNächstMon = dtImNächstMon.AddMonths(1).Date;
            }
            else
            {
              Console.WriteLine($"nächster ab\t\t{dtAnfNächstMon.ToLongDateString()}");
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

    /// <summary>
    /// aus Stundenwerten einen Satz Monatsmittelwerte berechnen
    /// </summary>
    /// <param name="dtAnfMon">erster Tag des Zielmonats</param>
    /// <param name="SQL">SQL-Command mit geöffneter DB</param>
    /// <returns>Anzahl der verwendeten Stundenwerte</returns>
    /// <exception cref="Exception">falls Messwerte nicht lesbar</exception>
    private static uint EinenMonat(DateTime dtAnfMon, MySqlCommand SQL)
    {
      MySqlDataReader reader;
      long utAnfang, utEnde;
      uint nMw;
      utAnfang = new DateTimeOffset(dtAnfMon, TimeSpan.Zero).ToUnixTimeSeconds();
      utEnde = new DateTimeOffset(dtAnfMon.AddMonths(1), TimeSpan.Zero).ToUnixTimeSeconds();
      //alt: utEnde = utAnfang + Konstanten.SECPROMONAT;
      Console.WriteLine("*****     für einen Monat:     *****");
      Console.WriteLine(
        $"Berechnung für\t{DateTimeOffset.FromUnixTimeSeconds(utAnfang).DateTime.ToLongDateString()}");
      Console.WriteLine(
        $"\tbis\t{DateTimeOffset.FromUnixTimeSeconds(utEnde).DateTime.ToLongDateString()}");
      SQL.CommandText =
        $"SELECT COUNT(zeit) AS Anzahl FROM {DbParameter.DBTMeW} WHERE zeit BETWEEN {utAnfang} AND {utEnde}";
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
        SQL.CommandText = $"SELECT * FROM {DbParameter.DBTMeW} WHERE zeit BETWEEN {utAnfang} AND {utEnde}";
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
                {
                  ergDouble[item.Key] = ergDouble[item.Key] + v;
                }
                else
                {
                  ergDouble.Add(item.Key, v);
                }

                break;
              case "regen1h":
              case "regen3h":
                //Console.WriteLine($"prüfen: {item.Key} = >{item.Value}<");
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
          $@"INSERT INTO {DbParameter.DBTMoW} 
          (zeit,mintemp,maxtemp,feucht,windv,wolken,druck) 
          values ({zeit},{stmin},{stmax},{feucht},{windv},{wolken},{druck})";
        _ = SQL.ExecuteNonQuery();
      }
      return nMw;
    }
  }
}