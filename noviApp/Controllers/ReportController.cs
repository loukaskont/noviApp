using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace noviApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReportController : ControllerBase
    {
        [HttpPost]
        public object IpsCountByCountry(String countries)
        {
            String country = "", extraWC = "";
            if (countries != null)
            {
                String[] strings = countries.Split(',');
                for (int i = 0; i < strings.Length; i++)
                {
                    country = country + ",'" + strings[i] + "'";
                }
                if (strings.Length > 0)
                {
                    extraWC = "and c.ThreeLetterCode in (" + country.Trim(',') + ")";
                }
            }
            List<ReportingTable> riportList = new List<ReportingTable>();
            MyGlobal myGlobal = new MyGlobal();
            if (myGlobal.cnn.State == ConnectionState.Closed) { myGlobal.cnn.Open(); }
            String sql = @"SELECT c.Name,(SELECT top 1 i.UpdatedAt from [wf].IPAddresses i where i.CountryId = c.Id order by i.UpdatedAt DESC) as last_update,
                (SELECT COUNT(*) from [wf].IPAddresses t WHERE t.CountryId = c.Id) as rows_count 
                from [wf].Countries c where (SELECT top 1 i.UpdatedAt from [wf].IPAddresses i where i.CountryId = c.Id) is not NULL " + extraWC;
            SqlCommand command = new SqlCommand(sql, myGlobal.cnn);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                ReportingTable ReportingRow = new ReportingTable();
                ReportingRow.CountryName = reader.GetString(0);
                ReportingRow.LastAddressUpdated = reader.GetDateTime(1);
                ReportingRow.AddressesCount = reader.GetInt32(2);
                riportList.Add(ReportingRow);
            }
            myGlobal.cnn.Close();
            return riportList;
        }
    }
}
