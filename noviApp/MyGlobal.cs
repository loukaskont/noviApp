using Microsoft.Data.SqlClient;
using System.Linq;
using System;

namespace noviApp
{
    public class MyGlobal
    {
        public SqlConnection cnn = new SqlConnection("Data Source=**.**.**.**;Initial Catalog=NeaOdos_Protocol;User ID=**;Password=**********; MultipleActiveResultSets=true; Encrypt=false");

        public bool ValidateAFM(String tin)
        {
            if (tin == "")
            {
                return true;
            }
            string afm = tin;
            int _numAFM = 0;
            if (afm.Length != 9 || !int.TryParse(afm, out _numAFM))
                return false;
            else
            {
                double sum = 0;
                int iter = afm.Length - 1;
                afm.ToCharArray().Take(iter).ToList().ForEach(c =>
                {
                    sum += double.Parse(c.ToString()) * Math.Pow(2, iter);
                    iter--;
                });
                if (sum % 11 == double.Parse(afm.Substring(8)) || double.Parse(afm.Substring(8)) == 0)
                    return true;
                else
                    return false;
            }
        }
    }
}
