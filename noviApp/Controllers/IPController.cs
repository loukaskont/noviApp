using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Policy;
using System.IO;
using System.Data;
using Microsoft.Data.SqlClient;

namespace noviApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IPController : ControllerBase
    {
        static Dictionary<String, IP_Details> ips = new Dictionary<String, IP_Details>();
        [HttpPost]
        public object Get_IP_Details(String IP, String key_id)
        {
            if (key_id != "1234!!")
            {
                return "You did not provide correct credentials.";
            }
            IP_Details ipDetails = null;
            if (ips.ContainsKey(IP))
            {
                ipDetails = ips[IP];
            }
            else
            {
                ipDetails = getIpDetailsFromDB(IP);
            }
            return ipDetails;
        }

        [HttpGet]
        public String UpdateIps() 
        {
            updateMemoryAndDatabaseFromIp2c();
            return "success";
        }

        private IP_Details getIpDetailsFromDB(String IP)
        {
            IP_Details IP_Details1 = new IP_Details();
            MyGlobal myGlobal = new MyGlobal();
            if (myGlobal.cnn.State == ConnectionState.Closed) { myGlobal.cnn.Open(); }
            String selectIPAddresses = "SELECT a.Id, a.CountryId, a.IP, a.CreatedAt, a.UpdatedAt, c.Name, c.TwoLetterCode, c.ThreeLetterCode, c.CreatedAt from [wf].IPAddresses a left join [wf].Countries c on a.CountryId = c.Id WHERE a.IP = '" + IP + "'";
            SqlCommand commandSelectIPAddresses = new SqlCommand(selectIPAddresses, myGlobal.cnn);
            SqlDataReader readerSelectIPAddresses = commandSelectIPAddresses.ExecuteReader();
            if (readerSelectIPAddresses.Read())
            {
                IP_Details1.Id = readerSelectIPAddresses.GetInt32(0);
                IP_Details1.country = new Country();
                IP_Details1.country.Id = readerSelectIPAddresses.GetInt32(1);
                IP_Details1.IP = readerSelectIPAddresses.GetString(2);
                IP_Details1.CreatedAt = readerSelectIPAddresses.GetDateTime(3);
                IP_Details1.UpdatedAt = readerSelectIPAddresses.GetDateTime(4);
                IP_Details1.country.Name = readerSelectIPAddresses.GetString(5);
                IP_Details1.country.TwoLetterCode = readerSelectIPAddresses.GetString(6);
                IP_Details1.country.ThreeLetterCode = readerSelectIPAddresses.GetString(7);
                IP_Details1.country.CreatedAt = readerSelectIPAddresses.GetDateTime(8);
            }
            else
            {
                IP_Details1 = getIpDetailsFromip2c(IP, 0);
            }
            return IP_Details1;
        }

        private IP_Details getIpDetailsFromip2c(String IP, int IPAddresses_id)
        {
            IP_Details ipDetails = new IP_Details();
            HttpWebRequest httpRequest;
            String uri = "https://ip2c.org/?ip=" + IP;
            httpRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpRequest.Method = "GET";
            httpRequest.ContentType = "application/xml";
            httpRequest.Accept = "application/xml";
            String result = "";
            HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            StreamReader streamReader = new StreamReader(httpResponse.GetResponseStream());
            result = streamReader.ReadToEnd();
            String[] temp1 = result.Split(';');
            ipDetails.IP = IP;
            if (IPAddresses_id > 0) 
            {
                ipDetails.Id = IPAddresses_id;
            }
            ipDetails.country = new Country();
            ipDetails.country.TwoLetterCode = temp1[1];
            ipDetails.country.ThreeLetterCode = temp1[2];
            ipDetails.country.Name = temp1[3];
            ipDetails.country.Id = insertNewCountryIfNotExist(ipDetails);
            updateIPAddresses(ipDetails);
            ips.Add(IP, ipDetails);
            return ipDetails;
        }


        private int insertNewCountryIfNotExist(IP_Details ipDetails)
        {
            MyGlobal myGlobal = new MyGlobal();
            if (myGlobal.cnn.State == ConnectionState.Closed) { myGlobal.cnn.Open(); }
            String sql = "SELECT a.Id from [wf].Countries a where a.TwoLetterCode = '" + ipDetails.country.TwoLetterCode + "' and a.ThreeLetterCode = '" + ipDetails.country.ThreeLetterCode + "'";
            SqlCommand command = new SqlCommand(sql, myGlobal.cnn);
            SqlDataReader reader = command.ExecuteReader();
            int countryID = -1;
            if (!reader.Read())
            {
                String sqlInsert = "INSERT into [wf].Countries(Name, TwoLetterCode,ThreeLetterCode,CreatedAt) values('" + ipDetails.country.Name + "', '" + ipDetails.country.TwoLetterCode + "','" + ipDetails.country.ThreeLetterCode + "',GETDATE()); ; SELECT SCOPE_IDENTITY()";
                SqlCommand command1 = new SqlCommand(sqlInsert, myGlobal.cnn);
                countryID = Convert.ToInt32(command1.ExecuteScalar());
            }
            else
            {
                countryID = reader.GetInt32(0);
            }
            myGlobal.cnn.Close();
            return countryID;
        }

        private void updateIPAddresses(IP_Details ipDetails)
        {
            MyGlobal myGlobal = new MyGlobal();
            if (myGlobal.cnn.State == ConnectionState.Closed) { myGlobal.cnn.Open(); }
            String sql = "SELECT a.Id from [wf].IPAddresses a where a.IP = '" + ipDetails.IP + "'";
            SqlCommand command = new SqlCommand(sql, myGlobal.cnn);
            SqlDataReader reader = command.ExecuteReader();
            if (!reader.Read())
            {
                String sqlInsert = "INSERT into [wf].IPAddresses(CountryId, IP, CreatedAt, UpdatedAt) values(" + ipDetails.country.Id + ", '" + ipDetails.IP + "', getdate(), getdate())";
                SqlCommand command1 = new SqlCommand(sqlInsert, myGlobal.cnn);
                command1.ExecuteScalar();
            }
            else 
            {
                String sqlUpdate = "Update [wf].IPAddresses set CountryId = (select id from [wf].Countries WHERE ThreeLetterCode = '" + ipDetails.country.ThreeLetterCode + "'), UpdatedAt = GETDATE() where Id = " + ipDetails.Id;
                SqlCommand command1 = new SqlCommand(sqlUpdate, myGlobal.cnn);
                command1.ExecuteScalar();
            }
            myGlobal.cnn.Close();
        }

        private void updateMemoryAndDatabaseFromIp2c() 
        {
            int featch = 0, featchNext = 2, allRowsCount = 0, IPAddresses_id = 0;
            String IPAddresses = "";
            MyGlobal myGlobal = new MyGlobal();
            if (myGlobal.cnn.State == ConnectionState.Closed) { myGlobal.cnn.Open(); }
            String sqlCount = "SELECT COUNT(*) as rowsCount from [wf].IPAddresses a";
            SqlCommand commandCount = new SqlCommand(sqlCount, myGlobal.cnn);
            SqlDataReader readerCount = commandCount.ExecuteReader();
            if (readerCount.Read()) 
            {
                allRowsCount = readerCount.GetInt32(0);
            }
            ips.Clear();
            while (featch < allRowsCount)
            {
                if ((featch + featchNext) > allRowsCount) 
                {
                    featchNext = allRowsCount - featch;
                }
                String sql = "SELECT a.Id, a.IP from [wf].IPAddresses a where a.Id is not null order by a.Id DESC OFFSET " + featch + " ROWS FETCH NEXT " + featchNext + " ROWS ONLY";
                SqlCommand command = new SqlCommand(sql, myGlobal.cnn);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    IPAddresses_id = reader.GetInt32(0);
                    IPAddresses = reader.GetString(1);
                    IP_Details IP_Details1 = new IP_Details();
                    IP_Details1.country = new Country();
                    IP_Details1.Id = IPAddresses_id;
                    IP_Details1 = getIpDetailsFromip2c(IPAddresses, IPAddresses_id);
                    ips.Add(IPAddresses, IP_Details1);
                }
                featch = featch + featchNext;
            }
            myGlobal.cnn.Close();
        }



    }
}
