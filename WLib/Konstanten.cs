namespace Borys.Wetter
{
  public class Konstanten
  {
    private const long SECPROTAG1 = 24L * 3600L;
    private const long SECPROMONAT1 = (long)((SECPROTAG1 * 30.5) + 0.5);
    public static long SECPROMONAT => SECPROMONAT1;
    public static long SECPROTAG => SECPROTAG1;
    /// <summary>
    /// verwendung in
    ///  str = Convert.ToString(wert, cTextauswertung.DEZIMALPUNKT);
    /// </summary>
    public static readonly System.Globalization.CultureInfo
      DEZIMALPUNKT = new System.Globalization.CultureInfo("en-US");

  }
}
