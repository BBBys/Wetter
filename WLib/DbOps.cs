using MySqlConnector;
using System.Collections.Generic;

namespace Borys.Wetter
{

  public class DbExcepts
  {
    public static bool IfMySQLFeldFehlt(MySqlException ex)
    {
      return ex.Number == 1054;
    }

    public static bool IfMySQLSytaxError(MySqlException ex)
    {
      return ex.Number == 1064;
    }

    public static bool IfMySQLKeyDoppeltEx(MySqlException ex)
    {
      return ex.Number == 1062;
    }

    public static bool IfMySQLTabelleFehltEx(MySqlException ex)
    {
      return ex.Number == 1146;
    }
  }
  public class DbParameter
  {
    /// <summary>
    /// Verbindungsparameter
    /// </summary>                                                               
    internal const string DB = "wetter", DBPORT = "3306", DBUSER = "wetter", DBPWD = "R5-v]pDuahWe*cpX";
    public const string DBTMoW = "monatswerte", DBTTW = "tageswerte", DBTMeW = "messwerte";
  }
  public class DbOps : DbParameter
  {
    public static Dictionary<string, object> GetDictionaryFromReader(MySqlDataReader reader)
    {
      Dictionary<string, object> result = new Dictionary<string, object>();
      for (int i = 0; i < reader.FieldCount; i++)
      {
        result.Add(reader.GetName(i), reader.GetValue(i));
      }

      return result;
    }
    /// <summary>
    /// Verbindung zu DBHOST herstellen
    /// alle anderen Param optional
    /// </summary>
    /// <param name="host">DBHOST</param>
    /// <param name="db"></param>
    /// <param name="dbuser"></param>
    /// <param name="dbport"></param>
    /// <param name="dbpwd"></param>
    /// <returns></returns>
    public static MySqlConnection ConnectToDB(string host,
                                              string db = DB,
                                              string dbuser = DBUSER,
                                              uint dbport = 3306,
                                              string dbpwd = DBPWD)
    {
      return new MySqlConnection(
          $@"Server=   {host};
          database= {db};
          user=     {dbuser};
          port=     {dbport}; 
          password= '{dbpwd}' ");
    }
  }
}